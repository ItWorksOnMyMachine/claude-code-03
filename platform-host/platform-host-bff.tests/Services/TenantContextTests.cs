using System;
using Xunit;
using FluentAssertions;
using PlatformBff.Services;
using PlatformBff.Models;
using PlatformBff.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace PlatformBff.Tests.Services;

public class TenantContextTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly TenantContext _tenantContext;

    public TenantContextTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _sessionServiceMock = new Mock<ISessionService>();
        _tenantContext = new TenantContext(_httpContextAccessorMock.Object, _sessionServiceMock.Object);
    }

    [Fact]
    public async Task TenantContext_Should_Return_TenantId_From_Session()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var sessionId = "test-session-123";

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim("session_id", sessionId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId)))
            .ReturnsAsync(HasValueOrMissingResult<string>.SetValue(expectedTenantId.ToString()));

        // Act
        var actualTenantId = await _tenantContext.GetCurrentTenantIdAsync();

        // Assert
        actualTenantId.Should().Be(expectedTenantId);
    }

    [Fact]
    public async Task TenantContext_Should_Return_Null_When_No_Session()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var tenantId = await _tenantContext.GetCurrentTenantIdAsync();

        // Assert
        tenantId.Should().BeNull();
    }

    [Fact]
    public async Task TenantContext_Should_Return_Null_When_No_SessionId_Claim()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        // User with no session_id claim
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var tenantId = await _tenantContext.GetCurrentTenantIdAsync();

        // Assert
        tenantId.Should().BeNull();
    }

    [Fact]
    public async Task TenantContext_Should_Return_Null_When_Session_Has_No_Tenant()
    {
        // Arrange - session has no tenant selected
        var sessionId = "test-session-123";
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim("session_id", sessionId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId)))
            .ReturnsAsync(HasValueOrMissingResult<string>.SetMissing()); // No tenant selected

        // Act
        var tenantId = await _tenantContext.GetCurrentTenantIdAsync();

        // Assert
        tenantId.Should().BeNull();
    }

    [Fact]
    public async Task TenantContext_SetTenant_Should_Throw_NotSupportedException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => _tenantContext.SetTenant(tenantId));
    }

    [Fact]
    public async Task TenantContext_ClearTenant_Should_Throw_NotSupportedException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => _tenantContext.ClearTenant());
    }


    [Fact]
    public async Task TenantContext_Should_Identify_Platform_Tenant()
    {
        // Arrange
        var platformTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var sessionId = "test-session-123";

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim("session_id", sessionId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId)))
            .ReturnsAsync(HasValueOrMissingResult<string>.SetValue(platformTenantId.ToString()));

        // Act
        var isPlatformTenant = await _tenantContext.IsPlatformTenant();

        // Assert
        isPlatformTenant.Should().BeTrue();
    }

    [Fact]
    public async Task TenantContext_Should_Return_UserId_From_Session()
    {
        // Arrange
        var expectedUserId = "test-user-123";
        var sessionId = "test-session-123";

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim("session_id", sessionId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(sessionId, nameof(PlatformBffSessionKeys.UserId)))
            .ReturnsAsync(HasValueOrMissingResult<string>.SetValue(expectedUserId));

        // Act
        var actualUserId = await _tenantContext.GetCurrentUserId();

        // Assert
        actualUserId.Should().Be(expectedUserId);
    }
}