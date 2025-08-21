using System;

namespace AuthService.Data.Entities;

public class PasswordHistory
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public virtual AppUser? User { get; set; }
}