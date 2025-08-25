using System;
using Xunit;
using FluentAssertions;
using PlatformBff.Services;
using PlatformBff.Models;
using PlatformBff.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Threading.Tasks;

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
    public void TenantContext_Should_Return_TenantId_From_Session()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var sessionData = new SessionData
        {
            SessionId = "test-session",
            UserId = "test-user",
            SelectedTenantId = expectedTenantId
        };
        
        var httpContext = new DefaultHttpContext();
        var cookies = new TestRequestCookieCollection();
        cookies.Add("platform.session", "test-session");
        httpContext.Request.Cookies = cookies;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync("test-session"))
            .ReturnsAsync(sessionData);

        // Act
        var actualTenantId = _tenantContext.GetCurrentTenantId();

        // Assert
        actualTenantId.Should().Be(expectedTenantId);
    }

    [Fact]
    public void TenantContext_Should_Return_Null_When_No_Session()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var tenantId = _tenantContext.GetCurrentTenantId();

        // Assert
        tenantId.Should().BeNull();
    }
    
    [Fact]
    public void TenantContext_Should_Return_Null_When_No_Cookie()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Cookies = new TestRequestCookieCollection();
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var tenantId = _tenantContext.GetCurrentTenantId();

        // Assert
        tenantId.Should().BeNull();
    }
    
    [Fact]
    public void TenantContext_Should_Return_Null_When_Session_Has_No_Tenant()
    {
        // Arrange
        var sessionData = new SessionData
        {
            SessionId = "test-session",
            UserId = "test-user",
            SelectedTenantId = null
        };
        
        var httpContext = new DefaultHttpContext();
        var cookies = new TestRequestCookieCollection();
        cookies.Add("platform.session", "test-session");
        httpContext.Request.Cookies = cookies;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync("test-session"))
            .ReturnsAsync(sessionData);

        // Act
        var tenantId = _tenantContext.GetCurrentTenantId();

        // Assert
        tenantId.Should().BeNull();
    }

    [Fact]
    public void TenantContext_SetTenant_Should_Throw_NotSupportedException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _tenantContext.SetTenant(tenantId));
    }
    
    [Fact]
    public void TenantContext_ClearTenant_Should_Throw_NotSupportedException()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _tenantContext.ClearTenant());
    }
    
    [Fact]
    public void TenantContext_SetUserId_Should_Throw_NotSupportedException()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _tenantContext.SetUserId("test-user"));
    }
    
    [Fact]
    public void TenantContext_Should_Identify_Platform_Tenant()
    {
        // Arrange
        var platformTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var sessionData = new SessionData
        {
            SessionId = "test-session",
            UserId = "admin-user",
            SelectedTenantId = platformTenantId
        };
        
        var httpContext = new DefaultHttpContext();
        var cookies = new TestRequestCookieCollection();
        cookies.Add("platform.session", "test-session");
        httpContext.Request.Cookies = cookies;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync("test-session"))
            .ReturnsAsync(sessionData);

        // Act
        var isPlatformTenant = _tenantContext.IsPlatformTenant();

        // Assert
        isPlatformTenant.Should().BeTrue();
    }
    
    [Fact]
    public void TenantContext_Should_Return_UserId_From_Session()
    {
        // Arrange
        var expectedUserId = "test-user-123";
        var sessionData = new SessionData
        {
            SessionId = "test-session",
            UserId = expectedUserId,
            SelectedTenantId = Guid.NewGuid()
        };
        
        var httpContext = new DefaultHttpContext();
        var cookies = new TestRequestCookieCollection();
        cookies.Add("platform.session", "test-session");
        httpContext.Request.Cookies = cookies;
        
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync("test-session"))
            .ReturnsAsync(sessionData);

        // Act
        var actualUserId = _tenantContext.GetCurrentUserId();

        // Assert
        actualUserId.Should().Be(expectedUserId);
    }
}