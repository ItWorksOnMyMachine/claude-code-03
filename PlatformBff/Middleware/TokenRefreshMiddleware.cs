using Microsoft.Extensions.Configuration;
using PlatformBff.Models;
using PlatformBff.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PlatformBff.Middleware;

public class TokenRefreshMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISessionService _sessionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenRefreshMiddleware> _logger;
    private static readonly HashSet<string> ExcludedPaths = new()
    {
        "/health",
        "/api/auth/login",
        "/api/auth/callback",
        "/api/auth/logout",
        "/api/auth/signout-callback"
    };

    public TokenRefreshMiddleware(
        RequestDelegate next,
        ISessionService sessionService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TokenRefreshMiddleware> logger)
    {
        _next = next;
        _sessionService = sessionService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip token refresh for excluded paths
        if (ExcludedPaths.Contains(context.Request.Path.Value?.ToLower() ?? string.Empty))
        {
            await _next(context);
            return;
        }

        // Check for session cookie
        var sessionId = context.Request.Cookies["platform.session"];
        if (string.IsNullOrEmpty(sessionId))
        {
            await _next(context);
            return;
        }

        try
        {
            // Get current tokens
            var tokens = await _sessionService.GetTokensAsync(sessionId);
            if (tokens == null || string.IsNullOrEmpty(tokens.RefreshToken))
            {
                await _next(context);
                return;
            }

            // Check if token needs refresh (within 5 minutes of expiry)
            var timeUntilExpiry = tokens.ExpiresAt - DateTime.UtcNow;
            if (timeUntilExpiry.TotalMinutes > 5)
            {
                await _next(context);
                return;
            }

            _logger.LogInformation("Token expiring in {Minutes} minutes, initiating refresh for session {SessionId}", 
                timeUntilExpiry.TotalMinutes, sessionId);

            // Attempt token refresh with retry logic
            TokenData? newTokens = null;
            int maxRetries = 3;
            int retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    newTokens = await _sessionService.RefreshTokensAsync(sessionId, tokens.RefreshToken);
                    break; // Success, exit retry loop
                }
                catch (HttpRequestException ex) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "Token refresh attempt {Attempt} failed for session {SessionId}, retrying...", 
                        retryCount, sessionId);
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * retryCount)); // Exponential backoff
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Token refresh failed for session {SessionId}", sessionId);
                    break; // Non-retryable error
                }
            }

            if (newTokens != null)
            {
                // Store the new tokens
                await _sessionService.StoreTokensAsync(sessionId, newTokens);
                
                // Extend session expiry
                var sessionExtension = newTokens.ExpiresAt - DateTime.UtcNow;
                await _sessionService.ExtendSessionAsync(sessionId, sessionExtension);
                
                _logger.LogInformation("Successfully refreshed tokens for session {SessionId}", sessionId);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't break the request pipeline
            _logger.LogError(ex, "Error in token refresh middleware for session {SessionId}", sessionId);
        }

        await _next(context);
    }
}