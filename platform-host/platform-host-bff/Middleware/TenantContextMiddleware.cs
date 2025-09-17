using Microsoft.AspNetCore.Http;
using PlatformBff.Models;
using PlatformBff.Services;
using SharedServices = PlatformShared.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PlatformBff.Middleware;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ISessionService sessionService)
    {
        // Try to get tenant from session first
        Guid? tenantId = null;
        string? userId = null;

        // Extract session ID from authentication cookie
        var sessionId = context.User?.FindFirst("session_id")?.Value;
        
        if (!string.IsNullOrEmpty(sessionId))
        {
            // Get tenant from custom session service
            var tenantResult = await sessionService.GetSessionDataAsync(sessionId, nameof(SharedServices.PlatformSessionKeys.SelectedTenantId));
            if (tenantResult != null && tenantResult.HasValue && Guid.TryParse(tenantResult.Value, out var parsedTenantId))
            {
                tenantId = parsedTenantId;
            }

            // Get user from custom session service
            var userResult = await sessionService.GetSessionDataAsync(sessionId, nameof(SharedServices.PlatformSessionKeys.UserId));
            if (userResult != null && userResult.HasValue)
            {
                userId = userResult.Value;
            }
        }

        // Fallback to claims if no session
        if (!tenantId.HasValue && context.User?.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("TenantId") 
                ?? context.User.FindFirst("tenant_id")
                ?? context.User.FindFirst(ClaimTypes.GroupSid); // Alternative claim

            if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var claimTenantId))
            {
                tenantId = claimTenantId;
            }

            // Get user ID from claims
            if (string.IsNullOrEmpty(userId))
            {
                var userClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirst("sub")
                    ?? context.User.FindFirst("user_id");

                userId = userClaim?.Value;
            }
        }

        // Set tenant in context service
        if (tenantId.HasValue)
        {
            tenantContext.SetTenant(tenantId.Value);
            
            // Also add to HttpContext.Items for easy access
            context.Items["TenantId"] = tenantId.Value;
        }

        // Set user ID if available (only in HttpContext.Items)
        // TenantContext gets user ID from session/authentication directly
        if (!string.IsNullOrEmpty(userId))
        {
            context.Items["UserId"] = userId;
        }

        // Call the next middleware in the pipeline
        await _next(context);
    }
}

// Extension method to register the middleware
public static class TenantContextMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantContextMiddleware>();
    }
}