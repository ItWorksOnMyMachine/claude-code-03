using System;

namespace PlatformBff.Services;

public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID from the context
    /// </summary>
    Guid? GetCurrentTenantId();

    /// <summary>
    /// Sets the current tenant ID in the context
    /// </summary>
    void SetTenant(Guid tenantId);

    /// <summary>
    /// Clears the current tenant from the context
    /// </summary>
    void ClearTenant();

    /// <summary>
    /// Checks if the current tenant is the platform administration tenant
    /// </summary>
    bool IsPlatformTenant();

    /// <summary>
    /// Gets the user ID from the current context
    /// </summary>
    string? GetCurrentUserId();

    /// <summary>
    /// Sets the user ID in the context
    /// </summary>
    void SetUserId(string userId);
}