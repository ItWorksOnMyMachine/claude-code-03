using FluentAssertions;
using PlatformShared.Models;
using PlatformShared.Services;
using Xunit;

namespace PlatformShared.Tests.Services;

/// <summary>
/// Tests for the ISessionService interface contract
/// These tests verify the expected behavior without implementation details
/// </summary>
public class ISessionServiceTests
{
    [Fact]
    public void ISessionService_Should_HaveCorrectMethods()
    {
        // Assert - Verify interface contract
        var interfaceType = typeof(ISessionService);

        interfaceType.Should().HaveMethod("StoreTokensAsync", new[] { typeof(string), typeof(TokenData) });
        interfaceType.Should().HaveMethod("GetTokensAsync", new[] { typeof(string) });
        interfaceType.Should().HaveMethod("RefreshTokensAsync", new[] { typeof(string), typeof(string) });
        interfaceType.Should().HaveMethod("RevokeTokensAsync", new[] { typeof(string) });
        interfaceType.Should().HaveMethod("RemoveSessionAsync", new[] { typeof(string) });
        interfaceType.Should().HaveMethod("RemoveSessionDataAsync", new[] { typeof(string), typeof(string) });
        interfaceType.Should().HaveMethod("StoreSessionDataAsync", new[] { typeof(string), typeof(string), typeof(string), typeof(DateTimeOffset?) });
        interfaceType.Should().HaveMethod("GetSessionDataAsync", new[] { typeof(string), typeof(string) });
    }

    [Fact]
    public void ISessionService_Should_BeAnInterface()
    {
        // Assert
        typeof(ISessionService).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void ISessionService_Should_BeInCorrectNamespace()
    {
        // Assert
        typeof(ISessionService).Namespace.Should().Be("PlatformShared.Services");
    }
}

/// <summary>
/// Tests for TokenData model
/// </summary>
public class TokenDataTests
{
    [Fact]
    public void TokenData_Should_HaveCorrectProperties()
    {
        // Arrange & Act
        var tokenData = new TokenData
        {
            AccessToken = "test-access-token"
        };

        // Assert
        tokenData.AccessToken.Should().Be("test-access-token");
        tokenData.RefreshToken.Should().BeNull();
        tokenData.IdToken.Should().BeNull();
        tokenData.ExpiresAt.Should().Be(default);
        tokenData.TokenType.Should().Be("Bearer");
        tokenData.Scopes.Should().BeNull();
    }

    [Fact]
    public void TokenData_IsExpiredOrExpiring_Should_DetectExpiredTokens()
    {
        // Arrange
        var expiredToken = new TokenData
        {
            AccessToken = "test-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10) // Expired 10 minutes ago
        };

        // Act & Assert
        expiredToken.IsExpiredOrExpiring().Should().BeTrue();
    }

    [Fact]
    public void TokenData_IsExpiredOrExpiring_Should_DetectSoonToExpireTokens()
    {
        // Arrange
        var soonToExpireToken = new TokenData
        {
            AccessToken = "test-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(2) // Expires in 2 minutes
        };

        // Act & Assert
        soonToExpireToken.IsExpiredOrExpiring(5).Should().BeTrue(); // Within 5-minute buffer
        soonToExpireToken.IsExpiredOrExpiring(1).Should().BeFalse(); // Outside 1-minute buffer
    }

    [Fact]
    public void TokenData_IsExpiredOrExpiring_Should_HandleValidTokens()
    {
        // Arrange
        var validToken = new TokenData
        {
            AccessToken = "test-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30) // Expires in 30 minutes
        };

        // Act & Assert
        validToken.IsExpiredOrExpiring().Should().BeFalse();
    }
}