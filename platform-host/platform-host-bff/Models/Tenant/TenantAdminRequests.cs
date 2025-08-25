using System.ComponentModel.DataAnnotations;

namespace PlatformBff.Models.Tenant;

/// <summary>
/// Request for creating a new tenant (admin)
/// </summary>
public class CreateTenantRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string DisplayName { get; set; } = string.Empty;

    public string? Settings { get; set; }
}

/// <summary>
/// Request for updating tenant information (admin)
/// </summary>
public class UpdateTenantRequest
{
    [StringLength(100, MinimumLength = 3)]
    public string? DisplayName { get; set; }

    public string? Settings { get; set; }

    public bool? IsActive { get; set; }
}

/// <summary>
/// Request for adding a user to a tenant (admin)
/// </summary>
public class AddUserToTenantRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string RoleName { get; set; } = "User";
}

/// <summary>
/// Tenant statistics for admin view
/// </summary>
public class TenantStatistics
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int AdminUsers { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastActivity { get; set; }
}

/// <summary>
/// Impersonation context for admin troubleshooting
/// </summary>
public class ImpersonationContext
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string ImpersonatingUserId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}