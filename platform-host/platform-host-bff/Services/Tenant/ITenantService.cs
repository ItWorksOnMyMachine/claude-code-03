using PlatformBff.Models.Tenant;

namespace PlatformBff.Services.Tenant;

/// <summary>
/// Service for managing tenant selection and context
/// Designed to be extractable to separate microservice
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Get all tenants available to a user
    /// </summary>
    Task<IEnumerable<TenantInfo>> GetAvailableTenantsAsync(string userId);
    
    /// <summary>
    /// Get details of a specific tenant if user has access
    /// </summary>
    Task<TenantInfo?> GetTenantAsync(string userId, Guid tenantId);
    
    /// <summary>
    /// Select a tenant for the current session
    /// </summary>
    Task<Models.Tenant.TenantContext> SelectTenantAsync(string userId, Guid tenantId);
    
    /// <summary>
    /// Validate if a user has access to a tenant
    /// </summary>
    Task<bool> ValidateAccessAsync(string userId, Guid tenantId);
    
    /// <summary>
    /// Check if user is a platform administrator
    /// </summary>
    Task<bool> IsPlatformAdminAsync(string userId);
    
    /// <summary>
    /// Get the platform tenant ID (for admin operations)
    /// </summary>
    Guid GetPlatformTenantId();
}