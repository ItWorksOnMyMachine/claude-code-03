using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlatformBff.Data;
using PlatformBff.Data.Entities;
using PlatformBff.Services;

namespace PlatformBff.Repositories;

public class TenantUserRepository : BaseRepository<TenantUser>, ITenantUserRepository
{
    public TenantUserRepository(PlatformDbContext context, ITenantContext tenantContext) 
        : base(context, tenantContext)
    {
    }

    public async Task<TenantUser?> GetUserTenantMembershipAsync(string userId, Guid tenantId)
    {
        return await _dbSet
            .Include(tu => tu.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(tu => 
                tu.UserId == userId && 
                tu.TenantId == tenantId && 
                !tu.IsDeleted);
    }

    public async Task<IEnumerable<TenantUser>> GetTenantUsersAsync(Guid tenantId)
    {
        return await _dbSet
            .Where(tu => tu.TenantId == tenantId && !tu.IsDeleted)
            .Include(tu => tu.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(tu => tu.JoinedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TenantUser>> GetActiveTenantUsersAsync(Guid tenantId)
    {
        return await _dbSet
            .Where(tu => 
                tu.TenantId == tenantId && 
                tu.IsActive && 
                !tu.IsDeleted)
            .Include(tu => tu.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(tu => tu.JoinedAt)
            .ToListAsync();
    }

    public async Task<bool> IsUserInTenantAsync(string userId, Guid tenantId)
    {
        return await _dbSet
            .AnyAsync(tu => 
                tu.UserId == userId && 
                tu.TenantId == tenantId && 
                tu.IsActive && 
                !tu.IsDeleted);
    }

    public async Task<TenantUser> AddUserToTenantAsync(string userId, Guid tenantId)
    {
        // Check if user already exists (including soft-deleted)
        var existingUser = await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(tu => 
                tu.UserId == userId && 
                tu.TenantId == tenantId);

        if (existingUser != null)
        {
            // Reactivate if soft-deleted
            if (existingUser.IsDeleted)
            {
                existingUser.IsDeleted = false;
                existingUser.DeletedAt = null;
                existingUser.DeletedBy = null;
                existingUser.IsActive = true;
                existingUser.JoinedAt = DateTimeOffset.UtcNow;
                existingUser.UpdatedAt = DateTimeOffset.UtcNow;
                existingUser.UpdatedBy = _tenantContext.GetCurrentUserId();
                
                _dbSet.Update(existingUser);
                await SaveChangesAsync();
                return existingUser;
            }
            
            return existingUser;
        }

        // Create new tenant user
        var tenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            IsActive = true,
            JoinedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = _tenantContext.GetCurrentUserId()
        };

        await _dbSet.AddAsync(tenantUser);
        await SaveChangesAsync();
        return tenantUser;
    }

    public async Task<bool> RemoveUserFromTenantAsync(string userId, Guid tenantId)
    {
        var tenantUser = await GetUserTenantMembershipAsync(userId, tenantId);
        if (tenantUser == null)
            return false;

        tenantUser.IsActive = false;
        tenantUser.IsDeleted = true;
        tenantUser.DeletedAt = DateTimeOffset.UtcNow;
        tenantUser.DeletedBy = _tenantContext.GetCurrentUserId();

        _dbSet.Update(tenantUser);
        await SaveChangesAsync();
        return true;
    }

    public async Task UpdateLastAccessedAsync(string userId, Guid tenantId)
    {
        var tenantUser = await GetUserTenantMembershipAsync(userId, tenantId);
        if (tenantUser != null)
        {
            tenantUser.LastAccessedAt = DateTimeOffset.UtcNow;
            _dbSet.Update(tenantUser);
            await SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Role>> GetUserRolesInTenantAsync(string userId, Guid tenantId)
    {
        var tenantUser = await _dbSet
            .Include(tu => tu.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(tu => 
                tu.UserId == userId && 
                tu.TenantId == tenantId && 
                !tu.IsDeleted);

        if (tenantUser == null)
            return new List<Role>();

        return tenantUser.UserRoles
            .Where(ur => !ur.IsDeleted && 
                        (!ur.ExpiresAt.HasValue || ur.ExpiresAt > DateTimeOffset.UtcNow))
            .Select(ur => ur.Role)
            .Where(r => !r.IsDeleted)
            .ToList();
    }
}