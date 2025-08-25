using System;

namespace PlatformBff.Services;

public class TenantContext : ITenantContext
{
    private Guid? _currentTenantId;
    private string? _currentUserId;
    
    // Fixed GUID for platform administration tenant
    public static readonly Guid PlatformTenantId = new Guid("00000000-0000-0000-0000-000000000001");

    public Guid? GetCurrentTenantId()
    {
        return _currentTenantId;
    }

    public void SetTenant(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }

    public void ClearTenant()
    {
        _currentTenantId = null;
    }

    public bool IsPlatformTenant()
    {
        return _currentTenantId.HasValue && _currentTenantId.Value == PlatformTenantId;
    }

    public string? GetCurrentUserId()
    {
        return _currentUserId;
    }

    public void SetUserId(string userId)
    {
        _currentUserId = userId;
    }
}