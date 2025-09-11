using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PlatformBff.Models;
using PlatformBff.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PlatformBff.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration? _configuration;
    private readonly IHttpClientFactory? _httpClientFactory;

    public AuthController(
        ISessionService sessionService,
        ILogger<AuthController> logger,
        IConfiguration? configuration = null,
        IHttpClientFactory? httpClientFactory = null)
    {
        _sessionService = sessionService;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initiate OIDC authentication flow
    /// </summary>
    [HttpPost("login")]
    public Task<IActionResult> Login([FromBody] LoginRequest? request = null)
    {
        var returnUrl = request?.ReturnUrl ?? "/";

        _logger.LogInformation("Login initiated with return URL: {ReturnUrl}", returnUrl);

        // For API calls, return the auth URL instead of Challenge
        if (Request.Headers["Accept"].ToString().Contains("application/json"))
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/signin-oidc",
                Items =
                {
                    ["returnUrl"] = returnUrl
                }
            };

            var authUrl = $"{Request.Scheme}://{Request.Host}/api/auth/challenge?returnUrl={Uri.EscapeDataString(returnUrl)}";

            return Task.FromResult<IActionResult>(Ok(new LoginResponse
            {
                RedirectUrl = authUrl
            }));
        }

        // For browser requests, return Challenge
        var challengeProperties = new AuthenticationProperties
        {
            RedirectUri = "/signin-oidc",
            Items =
            {
                ["returnUrl"] = returnUrl
            }
        };

        return Task.FromResult<IActionResult>(Challenge(challengeProperties, OpenIdConnectDefaults.AuthenticationScheme));
    }

    /// <summary>
    /// Challenge endpoint for browser-based auth flow
    /// </summary>
    [HttpGet("challenge")]
    public IActionResult Challenge(string returnUrl = "/")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/signin-oidc",
            Items =
            {
                ["returnUrl"] = returnUrl
            }
        };

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Terminate user session and revoke tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var sessionId = User.FindFirst("session_id")?.Value;

        if (!string.IsNullOrEmpty(sessionId))
        {
            // Revoke tokens at the authorization server
            await _sessionService.RevokeTokensAsync(sessionId);

            // Remove session from Redis
            await _sessionService.RemoveSessionAsync(sessionId);

            _logger.LogInformation("Session {SessionId} removed and tokens revoked", sessionId);
        }

        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handle OIDC callback after authentication - DISABLED: Using OIDC middleware instead
    /// </summary>
    // [HttpGet("callback")]  // Commented out to prevent conflicts with OIDC middleware
    private async Task<IActionResult> Callback()
    {
        try
        {
            // Authenticate the incoming request
            var authResult = await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("Authentication callback failed: {Error}", authResult.Failure?.Message);
                return Redirect("/login?error=auth_failed");
            }

            var principal = authResult.Principal;
            var sessionId = Guid.NewGuid().ToString();

            // Extract tokens from authentication result
            var accessToken = authResult.Properties?.GetTokenValue(OpenIdConnectParameterNames.AccessToken);
            var refreshToken = authResult.Properties?.GetTokenValue(OpenIdConnectParameterNames.RefreshToken);
            var idToken = authResult.Properties?.GetTokenValue(OpenIdConnectParameterNames.IdToken);
            var expiresAt = authResult.Properties?.GetTokenValue("expires_at");

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("No access token received in callback");
                return Redirect("/login?error=no_token");
            }

            // Parse expiration
            var expiration = DateTime.UtcNow.AddHours(1); // Default
            if (!string.IsNullOrEmpty(expiresAt) && long.TryParse(expiresAt, out var expiresAtUnix))
            {
                expiration = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix).UtcDateTime;
            }

            // Store tokens
            var tokenData = new TokenData
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IdToken = idToken,
                ExpiresAt = expiration
            };

            await _sessionService.StoreTokensAsync(sessionId, tokenData);

            // Extract user information
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        principal.FindFirst("sub")?.Value ??
                        Guid.NewGuid().ToString();

            await _sessionService.StoreSessionDataAsync(sessionId, nameof(PlatformBffSessionKeys.UserId), userId);

            _logger.LogInformation("User {UserId} authenticated successfully", userId);

            // Redirect to return URL - redirect to frontend callback page
            var returnUrl = HttpContext.Items["returnUrl"]?.ToString() ??
                           authResult.Properties?.Items["returnUrl"] ??
                           "/";

            // Redirect to frontend with auth callback indicator
            var frontendUrl = _configuration?["Frontend:Url"] ?? "https://host-fe.platform.local:3002";
            return Redirect($"{frontendUrl}/auth/callback?auth_callback=true&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing authentication callback");
            return Redirect("/login?error=callback_failed");
        }
    }

    /// <summary>
    /// Get current user session information
    /// </summary>
    [HttpGet("session")]
    public async Task<IActionResult> GetSession()
    {
        var IsAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var response = new SessionResponse
        {
            IsAuthenticated = IsAuthenticated,
            User = IsAuthenticated ? new UserInfo
            {
                Id = User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value,
                Email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                Name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                Claims = User.Claims.ToDictionary(c => c.Type, c => c.Value)
            } : null,
            // ExpiresAt = sessionData.ExpiresAt,
            // // Include tenant information if selected
            // SelectedTenant = sessionData.SelectedTenantId.HasValue ? new TenantInfo
            // {
            //     Id = sessionData.SelectedTenantId.Value,
            //     Name = sessionData.SelectedTenantName ?? "Unknown",
            //     UserRoles = sessionData.TenantRoles,
            //     IsPlatformAdmin = sessionData.IsPlatformAdmin
            // } : null
        };

        await Task.CompletedTask;

        return Ok(response);
    }

    /// <summary>
    /// Force refresh of access token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var sessionId = User.FindFirst("session_id")?.Value;

        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized();
        }

        var tokens = await _sessionService.GetTokensAsync(sessionId);
        if (tokens == null || string.IsNullOrEmpty(tokens.RefreshToken))
        {
            _logger.LogWarning("No refresh token available for session {SessionId}", sessionId);
            return StatusCode(500, new ErrorResponse
            {
                Error = "No refresh token available",
                StatusCode = 500
            });
        }

        // TODO: Implement actual token refresh with OIDC provider

        return NoContent();
    }

    /// <summary>
    /// Handle post-logout redirect from auth service
    /// </summary>
    [HttpGet("signout-callback")]
    public IActionResult SignOutCallback()
    {
        return Redirect("/");
    }

    /// <summary>
    /// Handle authentication errors
    /// </summary>
    [HttpGet("error")]
    public IActionResult Error([FromQuery] string? message = null)
    {
        _logger.LogWarning("Authentication error: {Message}", message);

        // Instead of showing error, redirect to frontend with error parameter
        var frontendUrl = _configuration?["Frontend:Url"] ?? "https://host-fe.platform.local:3002";
        return Redirect($"{frontendUrl}/login?error=auth_failed&details={Uri.EscapeDataString(message ?? "")}");
    }
}