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
        var sessionData = await GetSessionDataAsync(nameof(PlatformBffSessionKeys.SelectedTenantId));
        if (sessionData == null)
            return null;

        if (Guid.TryParse(sessionData, out var tenantId))
            return tenantId;

        return null;
    }

    public Task SetTenant(Guid tenantId)
    {
        // This method is kept for backward compatibility but shouldn't be used
        // Tenant selection should go through the TenantController
        throw new NotSupportedException("Use TenantController.SelectTenant to change tenant context");
    }

    public Task ClearTenant()
    {
        // This method is kept for backward compatibility but shouldn't be used
        // Tenant clearing should go through the TenantController
        throw new NotSupportedException("Use TenantController.ClearTenantSelection to clear tenant context");
    }

    public async Task<bool> IsPlatformTenant()
    {
        var tenantId = await GetCurrentTenantIdAsync();
        return tenantId.HasValue && tenantId.Value == PlatformTenantId;
    }

    public async Task<string?> GetCurrentUserId()
    {
        var userId = await GetSessionDataAsync(nameof(PlatformBffSessionKeys.UserId));
        return userId;
    }
}