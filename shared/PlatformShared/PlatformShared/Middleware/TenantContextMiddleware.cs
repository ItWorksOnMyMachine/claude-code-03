using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PlatformShared.Services;
using System.Security.Claims;

namespace PlatformShared.Middleware;

/// <summary>
/// Middleware that sets up tenant context from authentication session
/// Shared across all BFF services to ensure consistent tenant handling
/// </summary>
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantContextMiddleware> _logger;

    public TenantContextMiddleware(RequestDelegate next, ILogger<TenantContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
    {
        try
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                await SetupTenantContextAsync(context, sessionService);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up tenant context for request {RequestPath}", context.Request.Path);
            // Continue processing even if tenant context setup fails
        }

        await _next(context);
    }

    private async Task SetupTenantContextAsync(HttpContext context, ISessionService sessionService)
    {
        var sessionId = context.User.FindFirst("session_id")?.Value;
        if (string.IsNullOrEmpty(sessionId))
        {
            _logger.LogDebug("No session ID found in claims for authenticated user");
            return;
        }

        // Get user ID and tenant ID from session
        var userIdResult = await sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.UserId));
        var tenantIdResult = await sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.SelectedTenantId));

        if (userIdResult.HasValue && !string.IsNullOrEmpty(userIdResult.Value))
        {
            // Add user ID to claims if not already present
            if (!context.User.HasClaim("user_id", userIdResult.Value))
            {
                var identity = (ClaimsIdentity)context.User.Identity!;
                identity.AddClaim(new Claim("user_id", userIdResult.Value));
            }
        }

        if (tenantIdResult.HasValue && !string.IsNullOrEmpty(tenantIdResult.Value))
        {
            // Add tenant ID to claims if not already present
            if (!context.User.HasClaim("tenant_id", tenantIdResult.Value))
            {
                var identity = (ClaimsIdentity)context.User.Identity!;
                identity.AddClaim(new Claim("tenant_id", tenantIdResult.Value));

                _logger.LogDebug("Tenant context set for user {UserId} in tenant {TenantId}",
                    userIdResult.Value ?? "unknown", tenantIdResult.Value);
            }
        }
        else
        {
            _logger.LogDebug("No tenant selected for authenticated user {UserId}",
                userIdResult.Value ?? "unknown");
        }
    }
}