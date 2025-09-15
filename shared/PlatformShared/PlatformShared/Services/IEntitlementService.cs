using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlatformShared.Services;

/// <summary>
/// Service for checking user entitlements across the platform
/// Shared across all BFF services and frontend modules
/// </summary>
public interface IEntitlementService
{
    /// <summary>
    /// Get all entitlements for the current user in the current tenant
    /// </summary>
    Task<IEnumerable<string>> GetUserEntitlementsAsync();

    /// <summary>
    /// Get all entitlements for a specific user in a specific tenant
    /// </summary>
    Task<IEnumerable<string>> GetUserEntitlementsAsync(string userId, Guid tenantId);

    /// <summary>
    /// Check if the current user has a specific entitlement
    /// </summary>
    Task<bool> HasEntitlementAsync(string entitlement);

    /// <summary>
    /// Check if the current user has any of the specified entitlements
    /// </summary>
    Task<bool> HasAnyEntitlementAsync(params string[] entitlements);

    /// <summary>
    /// Check if the current user has all of the specified entitlements
    /// </summary>
    Task<bool> HasAllEntitlementsAsync(params string[] entitlements);

    /// <summary>
    /// Get entitlements that are missing for the current user
    /// </summary>
    Task<IEnumerable<string>> GetMissingEntitlementsAsync(params string[] requiredEntitlements);

    /// <summary>
    /// Check if a specific module is accessible to the current user
    /// </summary>
    Task<bool> CanAccessModuleAsync(string moduleName);

    /// <summary>
    /// Get all accessible modules for the current user
    /// </summary>
    Task<IEnumerable<string>> GetAccessibleModulesAsync();

    /// <summary>
    /// Refresh entitlements cache for the current user
    /// </summary>
    Task RefreshEntitlementsAsync();
}

/// <summary>
/// Common entitlements used across the platform
/// </summary>
public static class PlatformEntitlements
{
    // Platform-wide entitlements
    public const string PLATFORM_ACCESS = "PLATFORM_ACCESS";
    public const string PLATFORM_ADMIN = "PLATFORM_ADMIN";
    public const string TENANT_ADMIN = "TENANT_ADMIN";

    // CMS Module entitlements
    public const string CMS_ACCESS = "CMS_ACCESS";
    public const string CMS_MANAGE = "CMS_MANAGE";
    public const string CMS_ASSETS = "CMS_ASSETS";
    public const string CMS_TEMPLATES = "CMS_TEMPLATES";
    public const string CMS_PUBLISH = "CMS_PUBLISH";

    // User Management entitlements
    public const string USER_READ = "USER_READ";
    public const string USER_WRITE = "USER_WRITE";
    public const string USER_DELETE = "USER_DELETE";

    // Tenant Management entitlements
    public const string TENANT_READ = "TENANT_READ";
    public const string TENANT_WRITE = "TENANT_WRITE";
    public const string TENANT_DELETE = "TENANT_DELETE";

    // Forms Module entitlements (future)
    public const string FORMS_ACCESS = "FORMS_ACCESS";
    public const string FORMS_MANAGE = "FORMS_MANAGE";
    public const string FORMS_ANALYTICS = "FORMS_ANALYTICS";

    // Admin Panel entitlements (future)
    public const string ADMIN_ACCESS = "ADMIN_ACCESS";
    public const string ADMIN_ANALYTICS = "ADMIN_ANALYTICS";
    public const string ADMIN_SYSTEM = "ADMIN_SYSTEM";
}