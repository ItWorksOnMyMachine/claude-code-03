using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PlatformBff.Models;
using PlatformBff.Services;
using PlatformBff.Tests.Helpers;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PlatformBff.Tests.Authentication;

public class SessionServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly Mock<ILogger<DistributedSessionService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly ISessionService _sessionService;
    private readonly JsonSerializerOptions _jsonOptions;

    public SessionServiceTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _dataProtectionProvider = new TestDataProtectionProvider();
        _loggerMock = new Mock<ILogger<DistributedSessionService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _sessionService = new DistributedSessionService(
            _cacheMock.Object,
            _dataProtectionProvider,
            _loggerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task StoreTokens_Should_Save_Tokens_To_Cache()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var tokens = new TokenData
        {
            AccessToken = "test_access_token",
            RefreshToken = "test_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        await _sessionService.StoreTokensAsync(sessionId, tokens);

        // Assert
        _cacheMock.Verify(x => x.SetAsync(
            It.Is<string>(key => key == $"session:tokens:{sessionId}"),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetTokens_Should_Retrieve_Tokens_From_Cache()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var tokens = new TokenData
        {
            AccessToken = "test_access_token",
            RefreshToken = "test_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var json = JsonSerializer.Serialize(tokens, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        _cacheMock.Setup(x => x.GetAsync(
            It.Is<string>(key => key == $"session:tokens:{sessionId}"),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(bytes);

        // Act
        var result = await _sessionService.GetTokensAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tokens.AccessToken, result.AccessToken);
        Assert.Equal(tokens.RefreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task GetTokens_Should_Return_Null_When_Session_Not_Found()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        _cacheMock.Setup(x => x.GetAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync((byte[])null!);

        // Act
        var result = await _sessionService.GetTokensAsync(sessionId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveSession_Should_Delete_From_Cache()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Act
        await _sessionService.RemoveSessionAsync(sessionId);

        // Assert
        _cacheMock.Verify(x => x.RemoveAsync(
            It.Is<string>(key => key == $"session:tokens:{sessionId}"),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _cacheMock.Verify(x => x.RemoveAsync(
            It.Is<string>(key => key == $"session:data:{sessionId}"),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task RefreshTokens_Should_Call_Token_Endpoint_And_Store_New_Tokens()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var refreshToken = "test_refresh_token";

        // Setup configuration
        _configurationMock.Setup(x => x["Authentication:Authority"]).Returns("https://auth.example.com");
        _configurationMock.Setup(x => x["Authentication:ClientId"]).Returns("test-client");
        _configurationMock.Setup(x => x["Authentication:ClientSecret"]).Returns("test-secret");

        // Setup HTTP client mock
        var httpClient = new HttpClient(new TestHttpMessageHandler(async (request, cancellationToken) =>
        {
            var responseContent = JsonSerializer.Serialize(new
            {
                access_token = "new_access_token",
                refresh_token = "new_refresh_token",
                expires_in = 3600
            });
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent)
            };
        }));

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _sessionService.RefreshTokensAsync(sessionId, refreshToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new_access_token", result.AccessToken);
        Assert.Equal("new_refresh_token", result.RefreshToken);

        _cacheMock.Verify(x => x.SetAsync(
            It.Is<string>(key => key == $"session:tokens:{sessionId}"),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}