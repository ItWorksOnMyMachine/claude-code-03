using System;
using System.Collections.Generic;

namespace PlatformBff.Models;

/// <summary>
/// Represents session metadata stored in Redis
/// </summary>
public class SessionData
{
    public required string SessionId { get; set; }
    public required string UserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public Guid? CurrentTenantId { get; set; }
    public List<Guid> AvailableTenants { get; set; } = new();
    public Dictionary<string, string> Claims { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}