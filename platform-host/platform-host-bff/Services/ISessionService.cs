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
    /// Refreshes tokens using the provided refresh token
    /// </summary>
    Task<TokenData?> RefreshTokensAsync(string sessionId, string refreshToken);

    /// <summary>
    /// Revokes tokens for a session (used during logout)
    /// </summary>
    Task RevokeTokensAsync(string sessionId);

    /// <summary>
    /// Removes all session data
    /// </summary>
    Task RemoveSessionAsync(string sessionId);

    /// <summary>
    /// Removes specific session data
    /// </summary>
    Task RemoveSessionDataAsync(string sessionId, string name);

    /// <summary>
    /// Stores session metadata (user info, tenant, etc.)
    /// </summary>
    Task StoreSessionDataAsync(string sessionId, string name, string data, DateTimeOffset? expiresAt = null);

    /// <summary>
    /// Retrieves session metadata
    /// </summary>
    Task<HasValueOrMissingResult<string>> GetSessionDataAsync(string sessionId, string name);
}

public enum PlatformBffSessionKeys
{
    // SessionId,
    UserId,
    // Username,
    // Email,
    SelectedTenantId,
    // SelectedTenantName,
    // TenantRoles,
    // ExpiresAt,
    // IsPlatformAdmin,
}