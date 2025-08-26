using System.ComponentModel.DataAnnotations;

namespace PlatformBff.Models.Dev;

public class HealthCheckResponse
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, ServiceHealthStatus> Services { get; set; } = new();
    public string Overall { get; set; } = "healthy";
}

public class ServiceHealthStatus
{
    public string Status { get; set; } = "healthy";
    public string? Url { get; set; }
    public int? Port { get; set; }
    public string? Database { get; set; }
    public string? Redis { get; set; }
    public string? Version { get; set; }
    public List<string>? Databases { get; set; }
    public int? Keys { get; set; }
}

public class CreateTestUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string[] Roles { get; set; } = Array.Empty<string>();
}

public class CreateTestUserResponse
{
    public bool Success { get; set; }
    public string? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class AssignTenantRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public Guid TenantId { get; set; }

    public string[] Roles { get; set; } = Array.Empty<string>();
}

public class AssignTenantResponse
{
    public bool Success { get; set; }
    public TenantUserInfo? TenantUser { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class TenantUserInfo
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public class ResetDatabaseRequest
{
    [Required]
    [RegularExpression("^(all|platform|auth)$")]
    public string Target { get; set; } = "all";

    public bool Seed { get; set; } = true;
}

public class ResetDatabaseResponse
{
    public bool Success { get; set; }
    public List<string> Databases { get; set; } = new();
    public int Migrations { get; set; }
    public bool Seeded { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ConfigVerificationResponse
{
    public string Environment { get; set; } = string.Empty;
    public ConfigurationDetails Configurations { get; set; } = new();
    public bool Valid { get; set; }
}

public class ConfigurationDetails
{
    public AuthConfig Auth { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public RedisConfig Redis { get; set; } = new();
    public CorsConfig Cors { get; set; } = new();
}

public class AuthConfig
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}

public class DatabaseConfig
{
    public string Platform { get; set; } = "disconnected";
    public string Auth { get; set; } = "disconnected";
}

public class RedisConfig
{
    public string Connection { get; set; } = string.Empty;
    public string Status { get; set; } = "disconnected";
}

public class CorsConfig
{
    public List<string> Origins { get; set; } = new();
}