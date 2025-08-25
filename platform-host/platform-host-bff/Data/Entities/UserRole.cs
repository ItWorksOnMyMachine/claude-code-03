using System;

namespace PlatformBff.Data.Entities;

public class UserRole : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantUserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public string? AssignedBy { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; } // Optional: for temporary role assignments
    
    // Audit columns
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Navigation properties
    public TenantUser TenantUser { get; set; } = null!;
    public Role Role { get; set; } = null!;
}