using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PlatformBff.Models;

namespace PlatformBff.Services;

/// <summary>
/// Redis-based implementation of session management with encryption
/// </summary>
public class RedisSessionService : ISessionService
{
    private readonly IDistributedCache _cache;
    private readonly IDataProtector _protector;
    private readonly ILogger<RedisSessionService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private const string TokenKeyPrefix = "session:tokens:";
    private const string DataKeyPrefix = "session:data:";
    private const int DefaultExpirationHours = 2;

    public RedisSessionService(
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<RedisSessionService> logger)
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector("SessionTokens");
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task StoreTokensAsync(string sessionId, TokenData tokens)
    {
        try
        {
            var key = $"{TokenKeyPrefix}{sessionId}";
            var json = JsonSerializer.Serialize(tokens, _jsonOptions);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var encryptedBytes = _protector.Protect(jsonBytes);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = tokens.ExpiresAt,
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };
            
            await _cache.SetAsync(key, encryptedBytes, options);
            _logger.LogDebug("Stored tokens for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store tokens for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<TokenData?> GetTokensAsync(string sessionId)
    {
        try
        {
            var key = $"{TokenKeyPrefix}{sessionId}";
            var encryptedBytes = await _cache.GetAsync(key);
            
            if (encryptedBytes == null || encryptedBytes.Length == 0)
            {
                _logger.LogDebug("No tokens found for session {SessionId}", sessionId);
                return null;
            }
            
            var jsonBytes = _protector.Unprotect(encryptedBytes);
            var json = Encoding.UTF8.GetString(jsonBytes);
            var tokens = JsonSerializer.Deserialize<TokenData>(json, _jsonOptions);
            
            _logger.LogDebug("Retrieved tokens for session {SessionId}", sessionId);
            return tokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tokens for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> IsSessionValidAsync(string sessionId)
    {
        try
        {
            var tokens = await GetTokensAsync(sessionId);
            if (tokens == null)
            {
                return false;
            }
            
            // Check if tokens are expired
            if (tokens.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogDebug("Session {SessionId} has expired tokens", sessionId);
                return false;
            }
            
            // Check if session data exists
            var sessionData = await GetSessionDataAsync(sessionId);
            if (sessionData == null)
            {
                return false;
            }
            
            // Check if session itself is expired
            if (sessionData.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogDebug("Session {SessionId} has expired", sessionId);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task ExtendSessionAsync(string sessionId, TimeSpan extension)
    {
        try
        {
            // Extend token expiration
            var tokens = await GetTokensAsync(sessionId);
            if (tokens != null)
            {
                tokens.ExpiresAt = tokens.ExpiresAt.Add(extension);
                await StoreTokensAsync(sessionId, tokens);
            }
            
            // Extend session data expiration
            var sessionData = await GetSessionDataAsync(sessionId);
            if (sessionData != null)
            {
                sessionData.ExpiresAt = sessionData.ExpiresAt.Add(extension);
                sessionData.LastAccessedAt = DateTime.UtcNow;
                await StoreSessionDataAsync(sessionId, sessionData);
            }
            
            _logger.LogDebug("Extended session {SessionId} by {Extension}", sessionId, extension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extend session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task RemoveSessionAsync(string sessionId)
    {
        try
        {
            var tokenKey = $"{TokenKeyPrefix}{sessionId}";
            var dataKey = $"{DataKeyPrefix}{sessionId}";
            
            await _cache.RemoveAsync(tokenKey);
            await _cache.RemoveAsync(dataKey);
            
            _logger.LogInformation("Removed session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task RefreshTokensAsync(string sessionId, TokenData newTokens)
    {
        try
        {
            await StoreTokensAsync(sessionId, newTokens);
            
            // Update session last accessed time
            var sessionData = await GetSessionDataAsync(sessionId);
            if (sessionData != null)
            {
                sessionData.LastAccessedAt = DateTime.UtcNow;
                await StoreSessionDataAsync(sessionId, sessionData);
            }
            
            _logger.LogDebug("Refreshed tokens for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh tokens for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task StoreSessionDataAsync(string sessionId, SessionData sessionData)
    {
        try
        {
            var key = $"{DataKeyPrefix}{sessionId}";
            var json = JsonSerializer.Serialize(sessionData, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = sessionData.ExpiresAt,
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };
            
            await _cache.SetAsync(key, bytes, options);
            _logger.LogDebug("Stored session data for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store session data for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<SessionData?> GetSessionDataAsync(string sessionId)
    {
        try
        {
            var key = $"{DataKeyPrefix}{sessionId}";
            var bytes = await _cache.GetAsync(key);
            
            if (bytes == null || bytes.Length == 0)
            {
                _logger.LogDebug("No session data found for session {SessionId}", sessionId);
                return null;
            }
            
            var json = Encoding.UTF8.GetString(bytes);
            var sessionData = JsonSerializer.Deserialize<SessionData>(json, _jsonOptions);
            
            _logger.LogDebug("Retrieved session data for session {SessionId}", sessionId);
            return sessionData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve session data for session {SessionId}", sessionId);
            return null;
        }
    }
}