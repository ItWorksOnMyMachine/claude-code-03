using Microsoft.AspNetCore.Identity;

namespace AuthService.Data.Entities;

public class AppUser : IdentityUser, IAuditableEntity
{
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastPasswordChangeAt { get; set; }
    
    // Additional profile fields
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    
    // Account status
    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }
    public DateTime? LastLoginDate { get; set; }
    
    // Password management
    public int FailedPasswordAttempts { get; set; }
    public DateTime? PasswordExpiresAt { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? PasswordChangeRequiredDate { get; set; }
    
    // Lockout tracking
    public int ConsecutiveLockouts { get; set; }
    
    // Session management
    public string? LastSessionId { get; set; }
    public string? LastIpAddress { get; set; }
    public string? LastUserAgent { get; set; }
}