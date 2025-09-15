using System;
using Microsoft.AspNetCore.Http;

namespace CmsBff.Services;

/// <summary>
/// Provides tenant context from session for the current request
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Fixed GUID for platform administration tenant
    public static readonly Guid PlatformTenantId = new Guid("00000000-0000-0000-0000-000000000001");

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Guid?> GetCurrentTenantIdAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        // Try to get tenant ID from claims first
        var tenantIdClaim = httpContext.User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
            return tenantId;

        // Try to get from session
        var sessionTenantId = httpContext.Session.GetString("selected_tenant_id");
        if (!string.IsNullOrEmpty(sessionTenantId) && Guid.TryParse(sessionTenantId, out var sessionTenant))
            return sessionTenant;

        return null;
    }

    public async Task SetTenant(Guid tenantId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Session.SetString("selected_tenant_id", tenantId.ToString());
        }
    }

    public async Task ClearTenant()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Session.Remove("selected_tenant_id");
        }
    }

    public async Task<bool> IsPlatformTenant()
    {
        var currentTenantId = await GetCurrentTenantIdAsync();
        return currentTenantId == PlatformTenantId;
    }

    public async Task<string?> GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        // Get user ID from the name identifier claim
        return httpContext.User.FindFirst("sub")?.Value ?? 
               httpContext.User.FindFirst("user_id")?.Value;
    }
}