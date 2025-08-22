using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PlatformBff.Repositories;

public interface IBaseRepository<T> where T : class
{
    /// <summary>
    /// Get entity by ID
    /// </summary>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all entities (filtered by current tenant)
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(bool ignoreQueryFilters = false);

    /// <summary>
    /// Get entities matching a predicate
    /// </summary>
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate, bool ignoreQueryFilters = false);

    /// <summary>
    /// Get a single entity matching a predicate
    /// </summary>
    Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate, bool ignoreQueryFilters = false);

    /// <summary>
    /// Get paged results
    /// </summary>
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, 
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool ascending = true);

    /// <summary>
    /// Add a new entity
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Add multiple entities
    /// </summary>
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Update an entity
    /// </summary>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Update multiple entities
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Soft delete an entity
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid id);

    /// <summary>
    /// Hard delete an entity (use with caution)
    /// </summary>
    Task<bool> HardDeleteAsync(Guid id);

    /// <summary>
    /// Check if entity exists
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Get count of entities
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    /// <summary>
    /// Save changes to database
    /// </summary>
    Task<int> SaveChangesAsync();
}

/// <summary>
/// Paged result container
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}