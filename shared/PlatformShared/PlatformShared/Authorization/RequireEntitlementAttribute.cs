using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlatformShared.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PlatformShared.Authorization;

/// <summary>
/// Authorization attribute that requires specific entitlements for API access
/// Can be applied to controllers or individual endpoints
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireEntitlementAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _requiredEntitlements;
    private readonly bool _requireAll;

    /// <summary>
    /// Initialize with required entitlements
    /// </summary>
    /// <param name="entitlements">List of required entitlements</param>
    /// <param name="requireAll">If true, user must have ALL entitlements. If false, user needs ANY entitlement</param>
    public RequireEntitlementAttribute(params string[] entitlements) : this(false, entitlements)
    {
    }

    /// <summary>
    /// Initialize with required entitlements and logic mode
    /// </summary>
    /// <param name="requireAll">If true, user must have ALL entitlements. If false, user needs ANY entitlement</param>
    /// <param name="entitlements">List of required entitlements</param>
    public RequireEntitlementAttribute(bool requireAll, params string[] entitlements)
    {
        _requiredEntitlements = entitlements ?? throw new ArgumentNullException(nameof(entitlements));
        _requireAll = requireAll;

        if (_requiredEntitlements.Length == 0)
        {
            throw new ArgumentException("At least one entitlement must be specified", nameof(entitlements));
        }
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var entitlementService = context.HttpContext.RequestServices.GetRequiredService<IEntitlementService>();

        try
        {
            bool hasAccess = _requireAll
                ? await entitlementService.HasAllEntitlementsAsync(_requiredEntitlements)
                : await entitlementService.HasAnyEntitlementAsync(_requiredEntitlements);

            if (!hasAccess)
            {
                var missingEntitlements = await entitlementService.GetMissingEntitlementsAsync(_requiredEntitlements);

                context.Result = new ObjectResult(new
                {
                    error = "Insufficient entitlements",
                    required = _requiredEntitlements,
                    missing = missingEntitlements,
                    requireAll = _requireAll
                })
                {
                    StatusCode = 403
                };

                return;
            }

            await next();
        }
        catch (Exception ex)
        {
            // Log error but don't expose internal details
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireEntitlementAttribute>>();
            logger?.LogError(ex, "Error checking entitlements for endpoint {Endpoint}", context.ActionDescriptor.DisplayName);

            context.Result = new ObjectResult(new
            {
                error = "Entitlement check failed"
            })
            {
                StatusCode = 500
            };
        }
    }
}

/// <summary>
/// Specialized attribute for CMS-related endpoints
/// </summary>
public class RequireCmsAccessAttribute : RequireEntitlementAttribute
{
    public RequireCmsAccessAttribute() : base(PlatformEntitlements.CMS_ACCESS)
    {
    }
}

/// <summary>
/// Specialized attribute for CMS management operations
/// </summary>
public class RequireCmsManageAttribute : RequireEntitlementAttribute
{
    public RequireCmsManageAttribute() : base(PlatformEntitlements.CMS_MANAGE)
    {
    }
}

/// <summary>
/// Specialized attribute for CMS asset operations
/// </summary>
public class RequireCmsAssetsAttribute : RequireEntitlementAttribute
{
    public RequireCmsAssetsAttribute() : base(PlatformEntitlements.CMS_ASSETS)
    {
    }
}

/// <summary>
/// Specialized attribute for CMS template operations
/// </summary>
public class RequireCmsTemplatesAttribute : RequireEntitlementAttribute
{
    public RequireCmsTemplatesAttribute() : base(PlatformEntitlements.CMS_TEMPLATES)
    {
    }
}

/// <summary>
/// Specialized attribute for platform administration
/// </summary>
public class RequirePlatformAdminAttribute : RequireEntitlementAttribute
{
    public RequirePlatformAdminAttribute() : base(PlatformEntitlements.PLATFORM_ADMIN)
    {
    }
}