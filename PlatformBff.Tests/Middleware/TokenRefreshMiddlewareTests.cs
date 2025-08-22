using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PlatformBff.Middleware;
using PlatformBff.Models;
using PlatformBff.Services;
using PlatformBff.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace PlatformBff.Tests.Middleware;

public class TokenRefreshMiddlewareTests
{
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<TokenRefreshMiddleware>> _loggerMock;
    private readonly TokenRefreshMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public TokenRefreshMiddlewareTests()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<TokenRefreshMiddleware>>();
        
        _httpContext = new DefaultHttpContext();
        
        _middleware = new TokenRefreshMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            _sessionServiceMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Should_Skip_When_No_Session_Cookie()
    {
        // Arrange
        _httpContext.Request.Cookies = new TestRequestCookieCollection();

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _sessionServiceMock.Verify(x => x.GetTokensAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Skip_When_Tokens_Not_Near_Expiry()
    {
        // Arrange
        var sessionId = "test-session-id";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            ["platform.session"] = sessionId
        });
        
        var tokens = new TokenData
        {
            AccessToken = "valid_token",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Not near expiry
        };
        
        _sessionServiceMock.Setup(x => x.GetTokensAsync(sessionId))
            .ReturnsAsync(tokens);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _sessionServiceMock.Verify(x => x.RefreshTokensAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Refresh_When_Token_Near_Expiry()
    {
        // Arrange
        var sessionId = "test-session-id";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            ["platform.session"] = sessionId
        });
        
        var tokens = new TokenData
        {
            AccessToken = "expiring_token",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(3) // Near expiry (< 5 minutes)
        };
        
        _sessionServiceMock.Setup(x => x.GetTokensAsync(sessionId))
            .ReturnsAsync(tokens);
        
        _sessionServiceMock.Setup(x => x.RefreshTokensAsync(sessionId, tokens.RefreshToken))
            .ReturnsAsync(new TokenData
            {
                AccessToken = "new_token",
                RefreshToken = "new_refresh_token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _sessionServiceMock.Verify(x => x.RefreshTokensAsync(sessionId, tokens.RefreshToken), Times.Once);
    }

    [Fact]
    public async Task Should_Continue_When_Refresh_Fails()
    {
        // Arrange
        var sessionId = "test-session-id";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            ["platform.session"] = sessionId
        });
        
        var tokens = new TokenData
        {
            AccessToken = "expiring_token",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(3)
        };
        
        _sessionServiceMock.Setup(x => x.GetTokensAsync(sessionId))
            .ReturnsAsync(tokens);
        
        _sessionServiceMock.Setup(x => x.RefreshTokensAsync(sessionId, tokens.RefreshToken))
            .ThrowsAsync(new Exception("Refresh failed"));

        // Act
        var exception = await Record.ExceptionAsync(() => _middleware.InvokeAsync(_httpContext));

        // Assert
        Assert.Null(exception); // Should not throw, just log and continue
    }

    [Fact]
    public async Task Should_Skip_Refresh_When_No_Refresh_Token()
    {
        // Arrange
        var sessionId = "test-session-id";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            ["platform.session"] = sessionId
        });
        
        var tokens = new TokenData
        {
            AccessToken = "expiring_token",
            RefreshToken = null, // No refresh token
            ExpiresAt = DateTime.UtcNow.AddMinutes(3)
        };
        
        _sessionServiceMock.Setup(x => x.GetTokensAsync(sessionId))
            .ReturnsAsync(tokens);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _sessionServiceMock.Verify(x => x.RefreshTokensAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Update_Session_Expiry_After_Successful_Refresh()
    {
        // Arrange
        var sessionId = "test-session-id";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            ["platform.session"] = sessionId
        });
        
        var tokens = new TokenData
        {
            AccessToken = "expiring_token",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(3)
        };
        
        var newTokens = new TokenData
        {
            AccessToken = "new_token",
            RefreshToken = "new_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        
        _sessionServiceMock.Setup(x => x.GetTokensAsync(sessionId))
            .ReturnsAsync(tokens);
        
        _sessionServiceMock.Setup(x => x.RefreshTokensAsync(sessionId, tokens.RefreshToken))
            .ReturnsAsync(newTokens);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _sessionServiceMock.Verify(x => x.StoreTokensAsync(sessionId, newTokens), Times.Once);
        _sessionServiceMock.Verify(x => x.ExtendSessionAsync(sessionId, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task Should_Implement_Retry_Logic_On_Failure()
    {
        // Arrange
        var sessionId = "test-session-id";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            ["platform.session"] = sessionId
        });
        
        var tokens = new TokenData
        {
            AccessToken = "expiring_token",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(3)
        };
        
        var attempts = 0;
        _sessionServiceMock.Setup(x => x.GetTokensAsync(sessionId))
            .ReturnsAsync(tokens);
        
        _sessionServiceMock.Setup(x => x.RefreshTokensAsync(sessionId, tokens.RefreshToken))
            .ReturnsAsync(() =>
            {
                attempts++;
                if (attempts < 2)
                {
                    throw new HttpRequestException("Network error");
                }
                return new TokenData
                {
                    AccessToken = "new_token",
                    RefreshToken = "new_refresh_token",
                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                };
            });

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _sessionServiceMock.Verify(x => x.RefreshTokensAsync(sessionId, tokens.RefreshToken), Times.Exactly(2));
    }

    [Fact]
    public async Task Should_Handle_Refresh_Token_Rotation()
    {
        // Arrange
        var sessionId = "test-session-id";
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            ["platform.session"] = sessionId
        });
        
        var oldTokens = new TokenData
        {
            AccessToken = "old_access_token",
            RefreshToken = "old_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(3)
        };
        
        var rotatedTokens = new TokenData
        {
            AccessToken = "new_access_token",
            RefreshToken = "rotated_refresh_token", // New refresh token (rotation)
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        
        _sessionServiceMock.Setup(x => x.GetTokensAsync(sessionId))
            .ReturnsAsync(oldTokens);
        
        _sessionServiceMock.Setup(x => x.RefreshTokensAsync(sessionId, oldTokens.RefreshToken))
            .ReturnsAsync(rotatedTokens);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _sessionServiceMock.Verify(x => x.StoreTokensAsync(sessionId, It.Is<TokenData>(t => 
            t.RefreshToken == "rotated_refresh_token" && 
            t.AccessToken == "new_access_token")), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Refresh_For_Excluded_Paths()
    {
        // Arrange
        var sessionId = "test-session-id";
        _httpContext.Request.Path = "/health"; // Excluded path
        _httpContext.Request.Cookies = new TestRequestCookieCollection(new Dictionary<string, string>
        {
            ["platform.session"] = sessionId
        });
        
        var tokens = new TokenData
        {
            AccessToken = "expiring_token",
            RefreshToken = "refresh_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(3)
        };
        
        _sessionServiceMock.Setup(x => x.GetTokensAsync(sessionId))
            .ReturnsAsync(tokens);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _sessionServiceMock.Verify(x => x.GetTokensAsync(It.IsAny<string>()), Times.Never);
    }
}