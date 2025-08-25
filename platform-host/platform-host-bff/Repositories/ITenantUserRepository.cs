using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlatformBff.Data.Entities;

namespace PlatformBff.Repositories;

public interface ITenantUserRepository : IBaseRepository<TenantUser>
{
    /// <summary>
    /// Get user's membership in a specific tenant
    /// </summary>
    Task<TenantUser?> GetUserTenantMembershipAsync(string userId, Guid tenantId);

    /// <summary>
    /// Get all users in a tenant
    /// </summary>
    Task<IEnumerable<TenantUser>> GetTenantUsersAsync(Guid tenantId);

    /// <summary>
    /// Get all active users in a tenant
    /// </summary>
    Task<IEnumerable<TenantUser>> GetActiveTenantUsersAsync(Guid tenantId);

    /// <summary>
    /// Check if user is member of tenant
    /// </summary>
    Task<bool> IsUserInTenantAsync(string userId, Guid tenantId);

    /// <summary>
    /// Add user to tenant
    /// </summary>
    Task<TenantUser> AddUserToTenantAsync(string userId, Guid tenantId);

    /// <summary>
    /// Remove user from tenant (soft delete)
    /// </summary>
    Task<bool> RemoveUserFromTenantAsync(string userId, Guid tenantId);

    /// <summary>
    /// Update last accessed time for user in tenant
    /// </summary>
    Task UpdateLastAccessedAsync(string userId, Guid tenantId);

    /// <summary>
    /// Get user's roles in a tenant
    /// </summary>
    Task<IEnumerable<Role>> GetUserRolesInTenantAsync(string userId, Guid tenantId);
}