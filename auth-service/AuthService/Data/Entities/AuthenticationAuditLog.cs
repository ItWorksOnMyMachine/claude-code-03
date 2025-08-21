using System;

namespace AuthService.Data.Entities;

public class AuthenticationAuditLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public AuthenticationEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public string? ClientId { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? AdditionalData { get; set; } // JSON serialized additional data
    
    // Navigation properties
    public virtual AppUser? User { get; set; }
}

public enum AuthenticationEventType
{
    LoginSuccess,
    LoginFailed,
    Logout,
    PasswordChanged,
    PasswordResetRequested,
    PasswordReset,
    AccountLocked,
    AccountUnlocked,
    TokenRefreshed,
    TokenRevoked,
    MfaEnabled,
    MfaDisabled,
    MfaChallengeSuccess,
    MfaChallengeFailed,
    SessionExpired,
    SuspiciousActivity
}