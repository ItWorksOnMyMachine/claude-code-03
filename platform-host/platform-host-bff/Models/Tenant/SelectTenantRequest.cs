using System.ComponentModel.DataAnnotations;

namespace PlatformBff.Models.Tenant;

/// <summary>
/// Request to select a tenant for the current session
/// </summary>
public class SelectTenantRequest
{
    [Required]
    public Guid TenantId { get; set; }
}