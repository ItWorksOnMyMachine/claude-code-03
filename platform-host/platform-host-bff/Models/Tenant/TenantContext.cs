namespace PlatformBff.Models.Tenant;

/// <summary>
/// Current tenant context for a user session
/// </summary>
public class TenantContext
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public bool IsPlatformTenant { get; set; }
    public List<string> UserRoles { get; set; } = new();
    public DateTime SelectedAt { get; set; }
    
    /// <summary>
    /// Check if user has admin role in current tenant
    /// </summary>
    public bool IsAdmin => UserRoles.Contains("Admin") || UserRoles.Contains("TenantAdmin");
    
    /// <summary>
    /// Check if this is platform admin context
    /// </summary>
    public bool IsPlatformAdmin => IsPlatformTenant && UserRoles.Contains("Admin");
}