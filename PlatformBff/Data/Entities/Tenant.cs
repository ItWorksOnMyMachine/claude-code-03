using System;
using System.Collections.Generic;

namespace PlatformBff.Data.Entities;

public class Tenant : IAuditableEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPlatformTenant { get; set; } = false;
    public string? Settings { get; set; }
    
    // Audit columns
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Navigation properties
    public ICollection<TenantUser> TenantUsers { get; set; } = new List<TenantUser>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}