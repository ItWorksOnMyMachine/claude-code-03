using System;
using Microsoft.AspNetCore.Http;
using PlatformBff.Models;

namespace PlatformBff.Services;

/// <summary>
/// Provides tenant context from session for the current request
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISessionService _sessionService;
    private SessionData? _sessionData;
    private bool _sessionLoaded = false;
    
    // Fixed GUID for platform administration tenant
    public static readonly Guid PlatformTenantId = new Guid("00000000-0000-0000-0000-000000000001");

    public TenantContext(IHttpContextAccessor httpContextAccessor, ISessionService sessionService)
    {
        _httpContextAccessor = httpContextAccessor;
        _sessionService = sessionService;
    }

    private async Task<SessionData?> GetSessionDataAsync()
    {
        if (_sessionLoaded)
            return _sessionData;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        var sessionId = httpContext.Request.Cookies["platform.session"];
        if (string.IsNullOrEmpty(sessionId))
            return null;

        _sessionData = await _sessionService.GetSessionDataAsync(sessionId);
        _sessionLoaded = true;
        return _sessionData;
    }

    public Guid? GetCurrentTenantId()
    {
        var sessionData = GetSessionDataAsync().GetAwaiter().GetResult();
        return sessionData?.SelectedTenantId;
    }

    public void SetTenant(Guid tenantId)
    {
        // This method is kept for backward compatibility but shouldn't be used
        // Tenant selection should go through the TenantController
        throw new NotSupportedException("Use TenantController.SelectTenant to change tenant context");
    }

    public void ClearTenant()
    {
        // This method is kept for backward compatibility but shouldn't be used
        // Tenant clearing should go through the TenantController
        throw new NotSupportedException("Use TenantController.ClearTenantSelection to clear tenant context");
    }

    public bool IsPlatformTenant()
    {
        var tenantId = GetCurrentTenantId();
        return tenantId.HasValue && tenantId.Value == PlatformTenantId;
    }

    public string? GetCurrentUserId()
    {
        var sessionData = GetSessionDataAsync().GetAwaiter().GetResult();
        return sessionData?.UserId;
    }

    public void SetUserId(string userId)
    {
        // This method is kept for backward compatibility but shouldn't be used
        // User ID comes from authentication
        throw new NotSupportedException("User ID is set during authentication and cannot be changed");
    }
}