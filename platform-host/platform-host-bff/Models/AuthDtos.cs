using System;
using System.Collections.Generic;

namespace PlatformBff.Models;

/// <summary>
/// Request DTO for login endpoint
/// </summary>
public class LoginRequest
{
    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Response DTO for login endpoint
/// </summary>
public class LoginResponse
{
    public required string RedirectUrl { get; set; }
}

/// <summary>
/// Response DTO for logout endpoint
/// </summary>
public class LogoutResponse
{
    public bool Success { get; set; }
    public string RedirectUrl { get; set; } = "/";
}

/// <summary>
/// Response DTO for session endpoint
/// </summary>
public class SessionResponse
{
    public bool IsAuthenticated { get; set; }
    public UserInfo? User { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public TenantInfo? SelectedTenant { get; set; }
}

/// <summary>
/// Tenant information in session response
/// </summary>
public class TenantInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> UserRoles { get; set; } = new();
    public bool IsPlatformAdmin { get; set; }
}

/// <summary>
/// User information DTO
/// </summary>
public class UserInfo
{
    public required string Id { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public Dictionary<string, string> Claims { get; set; } = new();
}

/// <summary>
/// Response DTO for token refresh endpoint
/// </summary>
public class RefreshTokenResponse
{
    public bool Success { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Generic error response DTO
/// </summary>
public class ErrorResponse
{
    public string Error { get; set; } = "An error occurred";
    public string? Details { get; set; }
    public int StatusCode { get; set; }
}