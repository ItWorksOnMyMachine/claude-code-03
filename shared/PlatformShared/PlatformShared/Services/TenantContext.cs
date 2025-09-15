using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PlatformShared.Services;

/// <summary>
/// Provides tenant context from session for the current request
/// Shared implementation across all BFF services
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISessionService _sessionService;

    // Fixed GUID for platform administration tenant
    public static readonly Guid PlatformTenantId = new Guid("00000000-0000-0000-0000-000000000001");

    public TenantContext(IHttpContextAccessor httpContextAccessor, ISessionService sessionService)
    {
        _httpContextAccessor = httpContextAccessor;
        _sessionService = sessionService;
    }

    private async Task<string?> GetSessionDataAsync(string name)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        var sessionId = httpContext.User.FindFirst("session_id")?.Value;
        if (string.IsNullOrEmpty(sessionId))
            return null;

        var sessionDataResult = await _sessionService.GetSessionDataAsync(sessionId, name);

        return sessionDataResult.HasValue ? sessionDataResult.Value : null;
    }

    public async Task<Guid?> GetCurrentTenantIdAsync()
    {
        var sessionData = await GetSessionDataAsync(nameof(PlatformSessionKeys.SelectedTenantId));
        if (sessionData == null)
            return null;

        if (Guid.TryParse(sessionData, out var tenantId))
            return tenantId;

        return null;
    }

    public async Task SetTenant(Guid tenantId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;

        var sessionId = httpContext.User.FindFirst("session_id")?.Value;
        if (string.IsNullOrEmpty(sessionId))
            return;

        await _sessionService.StoreSessionDataAsync(sessionId, nameof(PlatformSessionKeys.SelectedTenantId), tenantId.ToString());
    }

    public async Task ClearTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;

        var sessionId = httpContext.User.FindFirst("session_id")?.Value;
        if (string.IsNullOrEmpty(sessionId))
            return;

        await _sessionService.RemoveSessionDataAsync(sessionId, nameof(PlatformSessionKeys.SelectedTenantId));
    }

    public async Task<bool> IsPlatformTenant()
    {
        var currentTenantId = await GetCurrentTenantIdAsync();
        return currentTenantId == PlatformTenantId;
    }

    public async Task<string?> GetCurrentUserId()
    {
        var sessionData = await GetSessionDataAsync(nameof(PlatformSessionKeys.UserId));
        return sessionData;
    }
}