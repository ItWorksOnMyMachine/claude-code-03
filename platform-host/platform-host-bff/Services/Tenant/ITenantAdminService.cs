using PlatformBff.Models.Tenant;

namespace PlatformBff.Services.Tenant;

/// <summary>
/// Administrative service for cross-tenant operations
/// Only accessible to platform administrators
/// </summary>
public interface ITenantAdminService
{
    /// <summary>
    /// Get all tenants in the system (admin only)
    /// </summary>
    Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(int page = 1, int pageSize = 20);
    
    /// <summary>
    /// Create a new tenant
    /// </summary>
    Task<TenantInfo> CreateTenantAsync(CreateTenantDto dto);
    
    /// <summary>
    /// Update tenant information
    /// </summary>
    Task<TenantInfo> UpdateTenantAsync(Guid tenantId, UpdateTenantDto dto);
    
    /// <summary>
    /// Deactivate a tenant (soft delete)
    /// </summary>
    Task<bool> DeactivateTenantAsync(Guid tenantId);
    
    /// <summary>
    /// Assign a user to a tenant with specific role
    /// </summary>
    Task<bool> AssignUserToTenantAsync(Guid tenantId, string userId, string email, string role);
    
    /// <summary>
    /// Remove a user from a tenant
    /// </summary>
    Task<bool> RemoveUserFromTenantAsync(Guid tenantId, string userId);
    
    /// <summary>
    /// Get all users in a tenant
    /// </summary>
    Task<IEnumerable<TenantUserInfo>> GetTenantUsersAsync(Guid tenantId);
    
    /// <summary>
    /// Impersonate a tenant (switch context as admin)
    /// </summary>
    Task<Models.Tenant.TenantContext> ImpersonateTenantAsync(string adminUserId, Guid tenantId);
    
    /// <summary>
    /// Overload for controller compatibility
    /// </summary>
    Task<ImpersonationContext> ImpersonateTenantAsync(Guid tenantId, string adminUserId);
    
    /// <summary>
    /// Overload for controller compatibility
    /// </summary>
    Task<TenantInfo> CreateTenantAsync(CreateTenantRequest request);
    
    /// <summary>
    /// Overload for controller compatibility
    /// </summary>
    Task<bool> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request);
    
    /// <summary>
    /// Overload for controller compatibility
    /// </summary>
    Task<TenantUserInfo> AddUserToTenantAsync(Guid tenantId, AddUserToTenantRequest request);
    
    /// <summary>
    /// Get tenant statistics
    /// </summary>
    Task<TenantStatistics> GetTenantStatisticsAsync(Guid tenantId);
}