namespace PlatformBff.Models.Tenant;

/// <summary>
/// User information within a tenant context
/// </summary>
public class TenantUserInfo
{
    public Guid Id { get; set; }
    public string AuthSubjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}