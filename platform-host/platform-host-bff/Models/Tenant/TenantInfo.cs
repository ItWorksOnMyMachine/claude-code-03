namespace PlatformBff.Models.Tenant;

/// <summary>
/// Tenant information DTO
/// </summary>
public class TenantInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsPlatformTenant { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? LogoUrl { get; set; }
    
    /// <summary>
    /// User's role in this tenant (if applicable)
    /// </summary>
    public string? UserRole { get; set; }
}