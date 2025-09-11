using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PlatformBff.Controllers;
using PlatformBff.Models;
using PlatformBff.Services;
using PlatformBff.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace PlatformBff.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;
    private readonly DefaultHttpContext _httpContext;

    public AuthControllerTests()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _authServiceMock = new Mock<IAuthenticationService>();
        _loggerMock = new Mock<ILogger<AuthController>>();

        _httpContext = new DefaultHttpContext();
        _httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(_authServiceMock.Object)
            .BuildServiceProvider();

        _controller = new AuthController(_sessionServiceMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            }
        };
    }

    [Fact]
    public async Task Login_Should_Return_Challenge_Result()
    {
        // Arrange
        var request = new LoginRequest { ReturnUrl = "/dashboard" };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, challengeResult.AuthenticationSchemes[0]);
        Assert.Equal("/signin-oidc", challengeResult.Properties.RedirectUri);
        Assert.Equal("/dashboard", challengeResult.Properties.Items["returnUrl"]);
    }

    [Fact]
    public async Task Login_Should_Use_Default_ReturnUrl_When_Not_Provided()
    {
        // Act
        var result = await _controller.Login(null);

        // Assert
        var challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal("/signin-oidc", challengeResult.Properties.RedirectUri);
        Assert.Equal("/", challengeResult.Properties.Items["returnUrl"]);
    }

    [Fact]
    public async Task Logout_Should_Clear_Session_And_Return_SignOut()
    {
        // Arrange
        var sessionId = "test-session-id";
        var claims = new[]
        {
            new Claim("session_id", sessionId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _httpContext.User = principal;

        // Act
        var result = await _controller.Logout();

        // Assert
        _sessionServiceMock.Verify(x => x.RevokeTokensAsync(sessionId), Times.Once);
        _sessionServiceMock.Verify(x => x.RemoveSessionAsync(sessionId), Times.Once);
        var signOutResult = Assert.IsType<SignOutResult>(result);
        Assert.Contains(OpenIdConnectDefaults.AuthenticationScheme, signOutResult.AuthenticationSchemes);
        Assert.Contains(CookieAuthenticationDefaults.AuthenticationScheme, signOutResult.AuthenticationSchemes);
    }

    [Fact]
    public async Task Session_Should_Return_User_Info_When_Authenticated()
    {
        // Arrange
        var userId = "user-123";
        var userEmail = "user@example.com";
        var userName = "John Doe";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, userEmail),
            new Claim(ClaimTypes.Name, userName)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = await _controller.GetSession();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var sessionResponse = Assert.IsType<SessionResponse>(okResult.Value);
        Assert.NotNull(sessionResponse);
        Assert.True(sessionResponse.IsAuthenticated);
        Assert.NotNull(sessionResponse.User);
        Assert.Equal(userId, sessionResponse.User.Id);
        Assert.Equal(userEmail, sessionResponse.User.Email);
        Assert.Equal(userName, sessionResponse.User.Name);
    }

    [Fact]
    public async Task Session_Should_Return_Unauthenticated_Response_When_Not_Authenticated()
    {
        // Arrange - User.Identity.IsAuthenticated is false by default

        // Act
        var result = await _controller.GetSession();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var sessionResponse = Assert.IsType<SessionResponse>(okResult.Value);
        Assert.NotNull(sessionResponse);
        Assert.False(sessionResponse.IsAuthenticated);
        Assert.Null(sessionResponse.User);
    }

    // [Fact] // Disabled: Callback method moved to OIDC middleware events
    // public async Task Callback_Should_Store_Tokens_And_Redirect_DISABLED()
    // {
    //     // Arrange
    //     var sessionId = Guid.NewGuid().ToString();
    //     var returnUrl = "/dashboard";

    //     var claims = new[]
    //     {
    //         new Claim(ClaimTypes.NameIdentifier, "user-123"),
    //         new Claim(ClaimTypes.Email, "user@example.com"),
    //         new Claim(ClaimTypes.Name, "John Doe")
    //     };

    //     var identity = new ClaimsIdentity(claims, "Test");
    //     var principal = new ClaimsPrincipal(identity);
    //     _httpContext.User = principal;

    //     var authProperties = new AuthenticationProperties();
    //     authProperties.StoreTokens(new[]
    //     {
    //         new AuthenticationToken { Name = "access_token", Value = "test_access_token" },
    //         new AuthenticationToken { Name = "refresh_token", Value = "test_refresh_token" },
    //         new AuthenticationToken { Name = "id_token", Value = "test_id_token" },
    //         new AuthenticationToken { Name = "expires_at", Value = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString() }
    //     });
    //     authProperties.Items["returnUrl"] = returnUrl;

    //     var authTicket = new AuthenticationTicket(principal, authProperties, OpenIdConnectDefaults.AuthenticationScheme);
    //     var authResult = AuthenticateResult.Success(authTicket);

    //     _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), OpenIdConnectDefaults.AuthenticationScheme))
    //         .ReturnsAsync(authResult);

    //     _httpContext.Items["returnUrl"] = returnUrl;

    //     // Act
    //     var result = await _controller.Callback();

    //     // Assert
    //     _sessionServiceMock.Verify(x => x.StoreTokensAsync(It.IsAny<string>(), It.IsAny<TokenData>()), Times.Once);
    //     _sessionServiceMock.Verify(x => x.StoreSessionDataAsync(It.IsAny<string>(), It.IsAny<SessionData>()), Times.Once);

    //     var redirectResult = Assert.IsType<RedirectResult>(result);
    //     // Should redirect to frontend callback page with returnUrl as query parameter
    //     var expectedUrl = $"https://host-fe.platform.local:3002/auth/callback?auth_callback=true&returnUrl={Uri.EscapeDataString(returnUrl)}";
    //     Assert.Equal(expectedUrl, redirectResult.Url);
    // }

    [Fact]
    public async Task Refresh_Should_Update_Tokens_When_Valid_Session()
    {
        // Arrange
        var sessionId = "test-session-id";
        var claims = new[]
        {
            new Claim("session_id", sessionId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _httpContext.User = principal;

        var existingTokens = new TokenData
        {
            AccessToken = "old_access_token",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _sessionServiceMock.Setup(x => x.GetTokensAsync(sessionId))
            .ReturnsAsync(existingTokens);

        // Act
        var result = await _controller.RefreshToken();

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Refresh_Should_Return_Unauthorized_When_No_Session()
    {
        // Arrange
        _httpContext.Request.Cookies = new TestRequestCookieCollection();

        // Act
        var result = await _controller.RefreshToken();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }
}