using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PlatformBff.Services;
using PlatformBff.Services.Tenant;

namespace PlatformBff.Authorization;

/// <summary>
/// Authorization attribute that requires the user to be a platform administrator
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PlatformAdminAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get session service
        var sessionService = context.HttpContext.RequestServices.GetService<ISessionService>();
        if (sessionService == null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        // Get session ID from cookie
        var sessionId = context.HttpContext.Request.Cookies["platform.session"];
        if (string.IsNullOrEmpty(sessionId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get session data
        var sessionData = await sessionService.GetSessionDataAsync(sessionId);
        if (sessionData == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user is platform admin
        if (!sessionData.IsPlatformAdmin)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Additionally verify with tenant service
        var tenantService = context.HttpContext.RequestServices.GetService<ITenantService>();
        if (tenantService != null)
        {
            var isPlatformAdmin = await tenantService.IsPlatformAdminAsync(sessionData.UserId);
            if (!isPlatformAdmin)
            {
                context.Result = new ForbidResult();
                return;
            }
        }

        // User is authorized as platform admin
    }
}

/// <summary>
/// Policy-based authorization requirement for platform administrators
/// </summary>
public class PlatformAdminRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Authorization handler for platform admin requirement
/// </summary>
public class PlatformAdminAuthorizationHandler : AuthorizationHandler<PlatformAdminRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISessionService _sessionService;
    private readonly ITenantService _tenantService;

    public PlatformAdminAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        ISessionService sessionService,
        ITenantService tenantService)
    {
        _httpContextAccessor = httpContextAccessor;
        _sessionService = sessionService;
        _tenantService = tenantService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformAdminRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return;
        }

        // Get session ID from cookie
        var sessionId = httpContext.Request.Cookies["platform.session"];
        if (string.IsNullOrEmpty(sessionId))
        {
            context.Fail();
            return;
        }

        // Get session data
        var sessionData = await _sessionService.GetSessionDataAsync(sessionId);
        if (sessionData == null)
        {
            context.Fail();
            return;
        }

        // Check if user is platform admin
        if (sessionData.IsPlatformAdmin)
        {
            // Verify with tenant service
            var isPlatformAdmin = await _tenantService.IsPlatformAdminAsync(sessionData.UserId);
            if (isPlatformAdmin)
            {
                context.Succeed(requirement);
                return;
            }
        }

        context.Fail();
    }
}