using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PlatformBff.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(LoginCallback), "Auth");
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl,
            Items = { ["returnUrl"] = returnUrl ?? "/" }
        };
        
        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> LoginCallback()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        if (!authenticateResult.Succeeded)
        {
            return BadRequest("Authentication failed");
        }

        var returnUrl = authenticateResult.Properties?.Items["returnUrl"] ?? "/";
        
        // In production, redirect to the frontend application
        // For now, return user info for testing
        return Ok(new
        {
            authenticated = true,
            user = new
            {
                id = authenticateResult.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                name = authenticateResult.Principal?.FindFirst(ClaimTypes.Name)?.Value,
                email = authenticateResult.Principal?.FindFirst(ClaimTypes.Email)?.Value,
                username = authenticateResult.Principal?.FindFirst("preferred_username")?.Value
            },
            returnUrl
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // Sign out of both cookie and OIDC
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("session")]
    public IActionResult GetSession()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new
            {
                authenticated = false
            });
        }

        return Ok(new
        {
            authenticated = true,
            user = new
            {
                id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                name = User.FindFirst(ClaimTypes.Name)?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value,
                username = User.FindFirst("preferred_username")?.Value
            },
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        return Forbid("Access denied");
    }

    [HttpGet("error")]
    public IActionResult Error(string? message = null)
    {
        _logger.LogError("Authentication error: {Message}", message);
        return BadRequest(new { error = "Authentication error", message });
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        // Get the current authentication result
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        if (!authenticateResult.Succeeded)
        {
            return Unauthorized();
        }

        var refreshToken = authenticateResult.Properties?.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { error = "No refresh token available" });
        }

        // Token refresh will be implemented with the SessionService
        // For now, just return success
        return Ok(new { message = "Token refresh not yet implemented" });
    }

    [HttpGet("test-protected")]
    [Authorize]
    public IActionResult TestProtected()
    {
        return Ok(new
        {
            message = "This is a protected endpoint",
            user = User.Identity?.Name,
            authenticated = User.Identity?.IsAuthenticated
        });
    }
}