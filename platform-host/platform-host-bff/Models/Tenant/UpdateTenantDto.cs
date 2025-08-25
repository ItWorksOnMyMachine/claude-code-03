using System.ComponentModel.DataAnnotations;

namespace PlatformBff.Models.Tenant;

/// <summary>
/// DTO for updating tenant information
/// </summary>
public class UpdateTenantDto
{
    [StringLength(100, MinimumLength = 3)]
    public string? Name { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Url]
    public string? LogoUrl { get; set; }
    
    public bool? IsActive { get; set; }
}