using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlatformBff.Data.Entities;

namespace PlatformBff.Repositories;

public interface ITenantRepository : IBaseRepository<Tenant>
{
    /// <summary>
    /// Get tenant by slug
    /// </summary>
    Task<Tenant?> GetBySlugAsync(string slug);

    /// <summary>
    /// Get all active tenants
    /// </summary>
    Task<IEnumerable<Tenant>> GetActiveTenantsAsync();

    /// <summary>
    /// Get the platform administration tenant
    /// </summary>
    Task<Tenant?> GetPlatformTenantAsync();

    /// <summary>
    /// Check if a slug is available
    /// </summary>
    Task<bool> IsSlugAvailableAsync(string slug, Guid? excludeTenantId = null);

    /// <summary>
    /// Get tenants for a specific user
    /// </summary>
    Task<IEnumerable<Tenant>> GetUserTenantsAsync(string userId);
}