using Microsoft.AspNetCore.Identity;

namespace AuthService.Data.Entities;

/// <summary>
/// Custom role entity for the identity provider
/// </summary>
public class AppRole : IdentityRole
{
    /// <summary>
    /// Description of the role
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Indicates if this is a system role that cannot be deleted
    /// </summary>
    public bool IsSystemRole { get; set; }
    
    /// <summary>
    /// When the role was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the role was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}