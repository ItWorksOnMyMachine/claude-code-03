using System;
using System.Threading.Tasks;
using PlatformBff.Models;

namespace PlatformBff.Services;

/// <summary>
/// Service for managing authentication sessions and token storage in Redis
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Stores authentication tokens for a session
    /// </summary>
    Task StoreTokensAsync(string sessionId, TokenData tokens);
    
    /// <summary>
    /// Retrieves authentication tokens for a session
    /// </summary>
    Task<TokenData?> GetTokensAsync(string sessionId);
    
    /// <summary>
    /// Checks if a session is valid and not expired
    /// </summary>
    Task<bool> IsSessionValidAsync(string sessionId);
    
    /// <summary>
    /// Extends the expiration time of a session
    /// </summary>
    Task ExtendSessionAsync(string sessionId, TimeSpan extension);
    
    /// <summary>
    /// Removes a session and its associated tokens
    /// </summary>
    Task RemoveSessionAsync(string sessionId);
    
    /// <summary>
    /// Refreshes tokens using the provided refresh token
    /// </summary>
    Task<TokenData?> RefreshTokensAsync(string sessionId, string refreshToken);
    
    /// <summary>
    /// Revokes tokens for a session (used during logout)
    /// </summary>
    Task RevokeTokensAsync(string sessionId);
    
    /// <summary>
    /// Stores session metadata (user info, tenant, etc.)
    /// </summary>
    Task StoreSessionDataAsync(string sessionId, SessionData sessionData);
    
    /// <summary>
    /// Retrieves session metadata
    /// </summary>
    Task<SessionData?> GetSessionDataAsync(string sessionId);
}