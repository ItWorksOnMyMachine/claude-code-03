using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private const string TokenKeyPrefix = "session:tokens:";
    private const string DataKeyPrefix = "session:data:";
    private const int DefaultExpirationHours = 2;

    public RedisSessionService(
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<RedisSessionService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector("SessionTokens");
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
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

    public async Task<TokenData?> RefreshTokensAsync(string sessionId, string refreshToken)
    {
        try
        {
            // Get OIDC configuration
            var authority = _configuration["Authentication:Authority"];
            var clientId = _configuration["Authentication:ClientId"];
            var clientSecret = _configuration["Authentication:ClientSecret"];
            
            if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("OIDC configuration missing for token refresh");
                return null;
            }
            
            // Create HTTP client for token endpoint
            var httpClient = _httpClientFactory.CreateClient();
            var tokenEndpoint = $"{authority}/connect/token";
            
            // Prepare refresh token request
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret ?? string.Empty)
            });
            
            // Make token refresh request
            var response = await httpClient.PostAsync(tokenEndpoint, requestContent);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token refresh failed: {Error}", error);
                return null;
            }
            
            // Parse response
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            
            // Extract tokens
            var newTokens = new TokenData
            {
                AccessToken = tokenResponse.GetProperty("access_token").GetString() ?? string.Empty,
                RefreshToken = tokenResponse.TryGetProperty("refresh_token", out var rt) 
                    ? rt.GetString() ?? refreshToken // Use old refresh token if not rotated
                    : refreshToken,
                IdToken = tokenResponse.TryGetProperty("id_token", out var it) 
                    ? it.GetString() 
                    : null,
                ExpiresAt = DateTime.UtcNow.AddSeconds(
                    tokenResponse.TryGetProperty("expires_in", out var exp) 
                        ? exp.GetInt32() 
                        : 3600)
            };
            
            // Store the new tokens
            await StoreTokensAsync(sessionId, newTokens);
            
            // Update session last accessed time
            var sessionData = await GetSessionDataAsync(sessionId);
            if (sessionData != null)
            {
                sessionData.LastAccessedAt = DateTime.UtcNow;
                sessionData.ExpiresAt = newTokens.ExpiresAt;
                await StoreSessionDataAsync(sessionId, sessionData);
            }
            
            _logger.LogInformation("Successfully refreshed tokens for session {SessionId}", sessionId);
            return newTokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh tokens for session {SessionId}", sessionId);
            throw;
        }
    }
    
    public async Task RevokeTokensAsync(string sessionId)
    {
        try
        {
            var tokens = await GetTokensAsync(sessionId);
            if (tokens == null || string.IsNullOrEmpty(tokens.AccessToken))
            {
                _logger.LogDebug("No tokens to revoke for session {SessionId}", sessionId);
                return;
            }
            
            // Get OIDC configuration
            var authority = _configuration["Authentication:Authority"];
            var clientId = _configuration["Authentication:ClientId"];
            var clientSecret = _configuration["Authentication:ClientSecret"];
            
            if (string.IsNullOrEmpty(authority))
            {
                _logger.LogWarning("OIDC authority not configured for token revocation");
                return;
            }
            
            // Create HTTP client for revocation endpoint
            var httpClient = _httpClientFactory.CreateClient();
            var revocationEndpoint = $"{authority}/connect/revocation";
            
            // Revoke access token
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", tokens.AccessToken),
                new KeyValuePair<string, string>("token_type_hint", "access_token"),
                new KeyValuePair<string, string>("client_id", clientId ?? string.Empty),
                new KeyValuePair<string, string>("client_secret", clientSecret ?? string.Empty)
            });
            
            var response = await httpClient.PostAsync(revocationEndpoint, requestContent);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token revocation failed with status {StatusCode}", response.StatusCode);
            }
            
            // Also revoke refresh token if present
            if (!string.IsNullOrEmpty(tokens.RefreshToken))
            {
                requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("token", tokens.RefreshToken),
                    new KeyValuePair<string, string>("token_type_hint", "refresh_token"),
                    new KeyValuePair<string, string>("client_id", clientId ?? string.Empty),
                    new KeyValuePair<string, string>("client_secret", clientSecret ?? string.Empty)
                });
                
                await httpClient.PostAsync(revocationEndpoint, requestContent);
            }
            
            _logger.LogInformation("Revoked tokens for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke tokens for session {SessionId}", sessionId);
            // Don't throw - revocation is best effort
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
    
    public async Task UpdateSessionDataAsync(string sessionId, SessionData sessionData)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                throw new ArgumentException("Session ID is required", nameof(sessionId));
            }
            
            if (sessionData == null)
            {
                throw new ArgumentNullException(nameof(sessionData));
            }
            
            // Update last accessed time
            sessionData.LastAccessedAt = DateTime.UtcNow;
            
            // Store updated session data
            await StoreSessionDataAsync(sessionId, sessionData);
            
            _logger.LogDebug("Updated session data for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session data for session {SessionId}", sessionId);
            throw;
        }
    }
}