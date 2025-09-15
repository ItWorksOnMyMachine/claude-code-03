using System;
using System.Threading.Tasks;

namespace PlatformShared.Services;

/// <summary>
/// Service for managing tenant context across BFF services
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID from the context
    /// </summary>
    Task<Guid?> GetCurrentTenantIdAsync();

    /// <summary>
    /// Sets the current tenant ID in the context
    /// </summary>
    Task SetTenant(Guid tenantId);

    /// <summary>
    /// Clears the current tenant from the context
    /// </summary>
    Task ClearTenant();

    /// <summary>
    /// Checks if the current tenant is the platform administration tenant
    /// </summary>
    Task<bool> IsPlatformTenant();

    /// <summary>
    /// Gets the user ID from the current context
    /// </summary>
    Task<string?> GetCurrentUserId();
}