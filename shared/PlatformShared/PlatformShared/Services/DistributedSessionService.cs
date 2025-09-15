using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlatformShared.Models;

namespace PlatformShared.Services;

/// <summary>
/// Redis-based implementation of session management with encryption
/// Shared across all BFF services for consistent session handling
/// </summary>
public class DistributedSessionService : ISessionService
{
    private readonly IDistributedCache _cache;
    private readonly IDataProtector _protector;
    private readonly ILogger<DistributedSessionService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string TokenKeyPrefix = "session:tokens:";
    private const string DataKeyPrefix = "session:platform-bff:";
    private const int DefaultExpirationHours = 2;

    public DistributedSessionService(
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<DistributedSessionService> logger,
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
            var tokenJson = JsonSerializer.Serialize(tokens, _jsonOptions);
            var protectedTokenJson = _protector.Protect(tokenJson);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(DefaultExpirationHours)
            };

            await _cache.SetStringAsync(TokenKeyPrefix + sessionId, protectedTokenJson, cacheOptions);
            _logger.LogDebug("Tokens stored for session {SessionId}", sessionId);
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
            var protectedTokenJson = await _cache.GetStringAsync(TokenKeyPrefix + sessionId);
            if (string.IsNullOrEmpty(protectedTokenJson))
            {
                _logger.LogDebug("No tokens found for session {SessionId}", sessionId);
                return null;
            }

            var tokenJson = _protector.Unprotect(protectedTokenJson);
            return JsonSerializer.Deserialize<TokenData>(tokenJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tokens for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<TokenData?> RefreshTokensAsync(string sessionId, string refreshToken)
    {
        try
        {
            var authServerUrl = _configuration["Authentication:Authority"];
            var clientId = _configuration["Authentication:ClientId"];
            var clientSecret = _configuration["Authentication:ClientSecret"];

            var httpClient = _httpClientFactory.CreateClient();

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var response = await httpClient.PostAsync($"{authServerUrl}/connect/token", tokenRequest);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var newTokens = new TokenData
                {
                    AccessToken = tokenResponse.GetProperty("access_token").GetString()!,
                    RefreshToken = tokenResponse.TryGetProperty("refresh_token", out var refreshProp)
                        ? refreshProp.GetString()
                        : refreshToken,
                    TokenType = tokenResponse.TryGetProperty("token_type", out var typeProp)
                        ? typeProp.GetString()
                        : "Bearer",
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.GetProperty("expires_in").GetInt32())
                };

                await StoreTokensAsync(sessionId, newTokens);
                _logger.LogDebug("Tokens refreshed for session {SessionId}", sessionId);
                return newTokens;
            }

            _logger.LogWarning("Token refresh failed for session {SessionId}: {StatusCode}",
                sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh tokens for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task RevokeTokensAsync(string sessionId)
    {
        try
        {
            await _cache.RemoveAsync(TokenKeyPrefix + sessionId);
            _logger.LogDebug("Tokens revoked for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke tokens for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task RemoveSessionAsync(string sessionId)
    {
        try
        {
            // Remove tokens
            await _cache.RemoveAsync(TokenKeyPrefix + sessionId);

            // Remove session data by pattern (this is simplified - in production you'd want to track keys)
            await _cache.RemoveAsync(DataKeyPrefix + sessionId + ":*");

            _logger.LogDebug("Session removed {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task RemoveSessionDataAsync(string sessionId, string name)
    {
        try
        {
            await _cache.RemoveAsync($"{DataKeyPrefix}{sessionId}:{name}");
            _logger.LogDebug("Session data removed for session {SessionId}, key {Name}", sessionId, name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove session data for session {SessionId}, key {Name}", sessionId, name);
            throw;
        }
    }

    public async Task StoreSessionDataAsync(string sessionId, string name, string data, DateTimeOffset? expiresAt = null)
    {
        try
        {
            var protectedData = _protector.Protect(data);
            var cacheOptions = new DistributedCacheEntryOptions();

            if (expiresAt.HasValue)
            {
                cacheOptions.AbsoluteExpiration = expiresAt.Value;
            }
            else
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(DefaultExpirationHours);
            }

            await _cache.SetStringAsync($"{DataKeyPrefix}{sessionId}:{name}", protectedData, cacheOptions);
            _logger.LogDebug("Session data stored for session {SessionId}, key {Name}", sessionId, name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store session data for session {SessionId}, key {Name}", sessionId, name);
            throw;
        }
    }

    public async Task<HasValueOrMissingResult<string>> GetSessionDataAsync(string sessionId, string name)
    {
        try
        {
            var protectedData = await _cache.GetStringAsync($"{DataKeyPrefix}{sessionId}:{name}");
            if (string.IsNullOrEmpty(protectedData))
            {
                _logger.LogDebug("No session data found for session {SessionId}, key {Name}", sessionId, name);
                return HasValueOrMissingResult<string>.SetMissing();
            }

            var data = _protector.Unprotect(protectedData);
            return HasValueOrMissingResult<string>.SetValue(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve session data for session {SessionId}, key {Name}", sessionId, name);
            return HasValueOrMissingResult<string>.SetMissing();
        }
    }
}