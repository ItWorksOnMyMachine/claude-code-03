using System;

namespace PlatformBff.Models;

/// <summary>
/// Represents authentication tokens stored in a session
/// </summary>
public class TokenData
{
    public required string AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? TokenType { get; set; } = "Bearer";
    public string[]? Scopes { get; set; }
    
    /// <summary>
    /// Checks if the access token is expired or will expire soon
    /// </summary>
    /// <param name="bufferMinutes">Minutes before expiration to consider token as expiring</param>
    public bool IsExpiredOrExpiring(int bufferMinutes = 5)
    {
        return DateTime.UtcNow.AddMinutes(bufferMinutes) >= ExpiresAt;
    }
}