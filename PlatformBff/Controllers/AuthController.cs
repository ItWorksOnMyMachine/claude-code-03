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
        
        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl,
            Items =
            {
                ["returnUrl"] = returnUrl
            }
        };
        
        return Task.FromResult<IActionResult>(Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme));
    }

    /// <summary>
    /// Terminate user session and revoke tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var sessionId = Request.Cookies["platform.session"];
        
        if (!string.IsNullOrEmpty(sessionId))
        {
            await _sessionService.RemoveSessionAsync(sessionId);
            _logger.LogInformation("Session {SessionId} removed", sessionId);
        }
        
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handle OIDC callback after authentication
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
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
            
            var sessionData = new SessionData
            {
                SessionId = sessionId,
                UserId = userId,
                Username = principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.FindFirst("name")?.Value,
                Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value,
                ExpiresAt = expiration,
                Claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString()
            };
            
            await _sessionService.StoreSessionDataAsync(sessionId, sessionData);
            
            // Set session cookie
            Response.Cookies.Append("platform.session", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = expiration,
                IsEssential = true
            });
            
            _logger.LogInformation("User {UserId} authenticated successfully", userId);
            
            // Redirect to return URL
            var returnUrl = HttpContext.Items["returnUrl"]?.ToString() ?? 
                           authResult.Properties?.Items["returnUrl"] ?? 
                           "/";
            
            return Redirect(returnUrl);
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
        var sessionId = Request.Cookies["platform.session"];
        
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized();
        }
        
        var isValid = await _sessionService.IsSessionValidAsync(sessionId);
        if (!isValid)
        {
            Response.Cookies.Delete("platform.session");
            return Unauthorized();
        }
        
        var sessionData = await _sessionService.GetSessionDataAsync(sessionId);
        if (sessionData == null)
        {
            return Unauthorized();
        }
        
        var response = new SessionResponse
        {
            IsAuthenticated = true,
            User = new UserInfo
            {
                Id = sessionData.UserId,
                Email = sessionData.Email,
                Name = sessionData.Username,
                Claims = sessionData.Claims ?? new Dictionary<string, string>()
            },
            ExpiresAt = sessionData.ExpiresAt
        };
        
        return Ok(response);
    }

    /// <summary>
    /// Force refresh of access token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var sessionId = Request.Cookies["platform.session"];
        
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized();
        }
        
        var isValid = await _sessionService.IsSessionValidAsync(sessionId);
        if (!isValid)
        {
            Response.Cookies.Delete("platform.session");
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
        
        try
        {
            // TODO: Implement actual token refresh with OIDC provider
            // For now, we'll just extend the session
            await _sessionService.ExtendSessionAsync(sessionId, TimeSpan.FromHours(1));
            
            var response = new RefreshTokenResponse
            {
                Success = true,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token for session {SessionId}", sessionId);
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "Token refresh failed",
                Details = ex.Message,
                StatusCode = 500 
            });
        }
    }

    /// <summary>
    /// Handle post-logout redirect from auth service
    /// </summary>
    [HttpGet("signout-callback")]
    public IActionResult SignOutCallback()
    {
        return Redirect("/");
    }
}