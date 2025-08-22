using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlatformBff.Data;
using PlatformBff.Data.Entities;
using PlatformBff.Services;

namespace PlatformBff.Repositories;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly PlatformDbContext _context;
    protected readonly ITenantContext _tenantContext;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(PlatformDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
        _dbSet = _context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        // Build a query that respects both soft delete and tenant filters
        var query = _dbSet.AsQueryable();
        
        // For entities with TenantId property, apply tenant filter
        if (typeof(T).GetProperty("TenantId") != null)
        {
            var currentTenantId = _tenantContext.GetCurrentTenantId();
            if (currentTenantId.HasValue)
            {
                query = query.Where(e => EF.Property<Guid>(e, "TenantId") == currentTenantId.Value);
            }
        }
        
        // For entities with IsDeleted property, filter out soft deleted items
        if (typeof(T).GetProperty("IsDeleted") != null)
        {
            query = query.Where(e => !EF.Property<bool>(e, "IsDeleted"));
        }
        
        // Apply the ID filter
        return await query.SingleOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(bool ignoreQueryFilters = false)
    {
        var query = _dbSet.AsQueryable();
        
        if (ignoreQueryFilters && CanIgnoreFilters())
        {
            query = query.IgnoreQueryFilters();
        }
        else
        {
            // Apply manual filters when global filters aren't working properly
            query = ApplyTenantAndSoftDeleteFilters(query);
        }

        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate, bool ignoreQueryFilters = false)
    {
        var query = _dbSet.Where(predicate);
        
        if (ignoreQueryFilters && CanIgnoreFilters())
        {
            query = query.IgnoreQueryFilters();
        }
        else
        {
            // Apply manual filters when global filters aren't working properly
            query = ApplyTenantAndSoftDeleteFilters(query);
        }

        return await query.ToListAsync();
    }

    public virtual async Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate, bool ignoreQueryFilters = false)
    {
        var query = _dbSet.Where(predicate);
        
        if (ignoreQueryFilters && CanIgnoreFilters())
        {
            query = query.IgnoreQueryFilters();
        }
        else
        {
            // Apply manual filters when global filters aren't working properly
            query = ApplyTenantAndSoftDeleteFilters(query);
        }

        return await query.FirstOrDefaultAsync();
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(
        int pageNumber, 
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool ascending = true)
    {
        var query = _dbSet.AsQueryable();
        
        // Apply tenant and soft delete filters
        query = ApplyTenantAndSoftDeleteFilters(query);

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync();

        if (orderBy != null)
        {
            query = ascending 
                ? query.OrderBy(orderBy) 
                : query.OrderByDescending(orderBy);
        }

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        // Set tenant ID if the entity has TenantId property
        SetTenantId(entity);
        
        await _dbSet.AddAsync(entity);
        await SaveChangesAsync();
        return entity;
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        
        // Set tenant ID for each entity
        foreach (var entity in entityList)
        {
            SetTenantId(entity);
        }
        
        await _dbSet.AddRangeAsync(entityList);
        await SaveChangesAsync();
        return entityList;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateRangeAsync(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
        await SaveChangesAsync();
    }

    public virtual async Task<bool> SoftDeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        // Check if entity implements IAuditableEntity
        if (entity is IAuditableEntity auditableEntity)
        {
            auditableEntity.IsDeleted = true;
            auditableEntity.DeletedAt = DateTimeOffset.UtcNow;
            auditableEntity.DeletedBy = _tenantContext.GetCurrentUserId();
            
            _dbSet.Update(entity);
            await SaveChangesAsync();
            return true;
        }

        // If not soft-deletable, perform hard delete
        return await HardDeleteAsync(id);
    }

    public virtual async Task<bool> HardDeleteAsync(Guid id)
    {
        var entity = await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
            
        if (entity == null)
            return false;

        _dbSet.Remove(entity);
        await SaveChangesAsync();
        return true;
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        var query = ApplyTenantAndSoftDeleteFilters(_dbSet.AsQueryable());
        return await query.AnyAsync(predicate);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var query = ApplyTenantAndSoftDeleteFilters(_dbSet.AsQueryable());
        return predicate == null 
            ? await query.CountAsync() 
            : await query.CountAsync(predicate);
    }

    public virtual async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Check if the current user can ignore query filters (platform admin)
    /// </summary>
    protected virtual bool CanIgnoreFilters()
    {
        return _tenantContext.IsPlatformTenant();
    }

    /// <summary>
    /// Set tenant ID on entity if it has TenantId property
    /// </summary>
    protected virtual void SetTenantId(T entity)
    {
        var tenantIdProperty = entity.GetType().GetProperty("TenantId");
        if (tenantIdProperty != null && tenantIdProperty.PropertyType == typeof(Guid))
        {
            var currentTenantId = _tenantContext.GetCurrentTenantId();
            if (currentTenantId.HasValue)
            {
                tenantIdProperty.SetValue(entity, currentTenantId.Value);
            }
        }
    }
    
    /// <summary>
    /// Apply tenant and soft delete filters manually when global filters aren't working
    /// </summary>
    protected virtual IQueryable<T> ApplyTenantAndSoftDeleteFilters(IQueryable<T> query)
    {
        // Apply tenant filter if entity has TenantId property
        if (typeof(T).GetProperty("TenantId") != null)
        {
            var currentTenantId = _tenantContext.GetCurrentTenantId();
            if (currentTenantId.HasValue)
            {
                query = query.Where(e => EF.Property<Guid>(e, "TenantId") == currentTenantId.Value);
            }
        }
        
        // Apply soft delete filter if entity has IsDeleted property
        if (typeof(T).GetProperty("IsDeleted") != null)
        {
            query = query.Where(e => !EF.Property<bool>(e, "IsDeleted"));
        }
        
        return query;
    }
}

/// <summary>
/// Base repository for entities that don't require tenant isolation
/// </summary>
public class NonTenantRepository<T> : BaseRepository<T> where T : class
{
    public NonTenantRepository(PlatformDbContext context, ITenantContext tenantContext) 
        : base(context, tenantContext)
    {
    }

    protected override void SetTenantId(T entity)
    {
        // Don't set tenant ID for non-tenant entities
    }

    protected override bool CanIgnoreFilters()
    {
        // Non-tenant entities don't need filter checks
        return true;
    }
}