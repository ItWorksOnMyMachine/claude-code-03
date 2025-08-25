namespace PlatformBff.Models.Tenant;

/// <summary>
/// Response for getting current tenant context
/// </summary>
public class CurrentTenantResponse
{
    public bool HasSelectedTenant { get; set; }
    public TenantContext? Tenant { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Response for getting available tenants
/// </summary>
public class AvailableTenantsResponse
{
    public IEnumerable<TenantInfo> Tenants { get; set; } = new List<TenantInfo>();
    public Guid? CurrentTenantId { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Response for tenant selection/switch operations
/// </summary>
public class TenantSelectionResponse
{
    public bool Success { get; set; }
    public TenantContext? Tenant { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Response for clearing tenant selection
/// </summary>
public class ClearTenantResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}