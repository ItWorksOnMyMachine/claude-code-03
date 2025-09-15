using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace PlatformShared.Services;

/// <summary>
/// Redis-backed implementation of entitlement checking
/// Provides caching and efficient entitlement validation across all BFF services
/// </summary>
public class EntitlementService : IEntitlementService
{
    private readonly ITenantContext _tenantContext;
    private readonly ISessionService _sessionService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<EntitlementService> _logger;

    private const string EntitlementsKeyPrefix = "entitlements:";
    private const int CacheExpirationMinutes = 15;

    public EntitlementService(
        ITenantContext tenantContext,
        ISessionService sessionService,
        IDistributedCache cache,
        ILogger<EntitlementService> logger)
    {
        _tenantContext = tenantContext;
        _sessionService = sessionService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetUserEntitlementsAsync()
    {
        var userId = await _tenantContext.GetCurrentUserId();
        var tenantId = await _tenantContext.GetCurrentTenantIdAsync();

        if (string.IsNullOrEmpty(userId) || !tenantId.HasValue)
        {
            _logger.LogDebug("No user or tenant context available");
            return Array.Empty<string>();
        }

        return await GetUserEntitlementsAsync(userId, tenantId.Value);
    }

    public async Task<IEnumerable<string>> GetUserEntitlementsAsync(string userId, Guid tenantId)
    {
        try
        {
            var cacheKey = $"{EntitlementsKeyPrefix}{tenantId}:{userId}";
            var cachedEntitlements = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedEntitlements))
            {
                var entitlements = JsonSerializer.Deserialize<string[]>(cachedEntitlements);
                _logger.LogDebug("Retrieved cached entitlements for user {UserId} in tenant {TenantId}", userId, tenantId);
                return entitlements ?? Array.Empty<string>();
            }

            // For now, return default entitlements based on user context
            // In a real implementation, this would query a database or external service
            var userEntitlements = await GetDefaultEntitlementsAsync(userId, tenantId);

            // Cache the results
            var entitlementsJson = JsonSerializer.Serialize(userEntitlements);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
            };
            await _cache.SetStringAsync(cacheKey, entitlementsJson, cacheOptions);

            _logger.LogDebug("Loaded and cached entitlements for user {UserId} in tenant {TenantId}", userId, tenantId);
            return userEntitlements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entitlements for user {UserId} in tenant {TenantId}", userId, tenantId);
            return Array.Empty<string>();
        }
    }

    public async Task<bool> HasEntitlementAsync(string entitlement)
    {
        var entitlements = await GetUserEntitlementsAsync();
        return entitlements.Contains(entitlement);
    }

    public async Task<bool> HasAnyEntitlementAsync(params string[] entitlements)
    {
        var userEntitlements = await GetUserEntitlementsAsync();
        return entitlements.Any(e => userEntitlements.Contains(e));
    }

    public async Task<bool> HasAllEntitlementsAsync(params string[] entitlements)
    {
        var userEntitlements = await GetUserEntitlementsAsync();
        return entitlements.All(e => userEntitlements.Contains(e));
    }

    public async Task<IEnumerable<string>> GetMissingEntitlementsAsync(params string[] requiredEntitlements)
    {
        var userEntitlements = await GetUserEntitlementsAsync();
        return requiredEntitlements.Where(e => !userEntitlements.Contains(e));
    }

    public async Task<bool> CanAccessModuleAsync(string moduleName)
    {
        // Module-specific entitlement mapping
        var moduleEntitlements = GetModuleEntitlements(moduleName);
        if (!moduleEntitlements.Any())
        {
            return true; // Module has no entitlement requirements
        }

        return await HasAnyEntitlementAsync(moduleEntitlements.ToArray());
    }

    public async Task<IEnumerable<string>> GetAccessibleModulesAsync()
    {
        var userEntitlements = await GetUserEntitlementsAsync();
        var accessibleModules = new List<string>();

        // Check each known module
        var moduleMap = GetKnownModules();
        foreach (var (moduleName, requiredEntitlements) in moduleMap)
        {
            if (!requiredEntitlements.Any() || requiredEntitlements.Any(e => userEntitlements.Contains(e)))
            {
                accessibleModules.Add(moduleName);
            }
        }

        return accessibleModules;
    }

    public async Task RefreshEntitlementsAsync()
    {
        var userId = await _tenantContext.GetCurrentUserId();
        var tenantId = await _tenantContext.GetCurrentTenantIdAsync();

        if (string.IsNullOrEmpty(userId) || !tenantId.HasValue)
        {
            return;
        }

        var cacheKey = $"{EntitlementsKeyPrefix}{tenantId}:{userId}";
        await _cache.RemoveAsync(cacheKey);

        _logger.LogDebug("Cleared entitlement cache for user {UserId} in tenant {TenantId}", userId, tenantId);

        // Pre-load new entitlements
        await GetUserEntitlementsAsync(userId, tenantId.Value);
    }

    /// <summary>
    /// Get default entitlements for a user (placeholder implementation)
    /// In production, this would query the actual entitlement database
    /// </summary>
    private async Task<string[]> GetDefaultEntitlementsAsync(string userId, Guid tenantId)
    {
        // Check if this is the platform tenant
        if (tenantId == TenantContext.PlatformTenantId)
        {
            return new[]
            {
                PlatformEntitlements.PLATFORM_ACCESS,
                PlatformEntitlements.PLATFORM_ADMIN,
                PlatformEntitlements.TENANT_ADMIN,
                PlatformEntitlements.CMS_ACCESS,
                PlatformEntitlements.CMS_MANAGE,
                PlatformEntitlements.CMS_ASSETS,
                PlatformEntitlements.CMS_TEMPLATES,
                PlatformEntitlements.CMS_PUBLISH,
                PlatformEntitlements.USER_READ,
                PlatformEntitlements.USER_WRITE,
                PlatformEntitlements.TENANT_READ,
                PlatformEntitlements.TENANT_WRITE,
            };
        }

        // For regular tenants, provide basic CMS entitlements
        // In production, this would be based on the user's role and tenant subscription
        return new[]
        {
            PlatformEntitlements.PLATFORM_ACCESS,
            PlatformEntitlements.CMS_ACCESS,
            PlatformEntitlements.CMS_MANAGE,
            PlatformEntitlements.CMS_ASSETS,
        };
    }

    /// <summary>
    /// Get required entitlements for a specific module
    /// </summary>
    private IEnumerable<string> GetModuleEntitlements(string moduleName)
    {
        return moduleName.ToLower() switch
        {
            "cmsmodule" or "cms" => new[] { PlatformEntitlements.CMS_ACCESS },
            "forms" => new[] { PlatformEntitlements.FORMS_ACCESS },
            "admin" => new[] { PlatformEntitlements.ADMIN_ACCESS },
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Get mapping of known modules to their required entitlements
    /// </summary>
    private Dictionary<string, string[]> GetKnownModules()
    {
        return new Dictionary<string, string[]>
        {
            { "cmsModule", new[] { PlatformEntitlements.CMS_ACCESS } },
            { "formsModule", new[] { PlatformEntitlements.FORMS_ACCESS } },
            { "adminModule", new[] { PlatformEntitlements.ADMIN_ACCESS } },
        };
    }
}