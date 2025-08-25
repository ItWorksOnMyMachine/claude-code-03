using System.ComponentModel.DataAnnotations;

namespace PlatformBff.Models.Tenant;

/// <summary>
/// DTO for creating a new tenant
/// </summary>
public class CreateTenantDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Url]
    public string? LogoUrl { get; set; }
    
    /// <summary>
    /// Initial admin user for the tenant
    /// </summary>
    public string? InitialAdminEmail { get; set; }
}