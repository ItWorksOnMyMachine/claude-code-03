using System.Threading.Tasks;
using AuthService.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AuthService.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private readonly Mock<RequestDelegate> _next;
    private readonly Mock<IOptions<SecurityHeaderOptions>> _options;
    private readonly SecurityHeadersMiddleware _middleware;

    public SecurityHeadersMiddlewareTests()
    {
        _next = new Mock<RequestDelegate>();
        _options = new Mock<IOptions<SecurityHeaderOptions>>();
        _options.Setup(x => x.Value).Returns(new SecurityHeaderOptions
        {
            EnableHsts = true,
            EnableXContentTypeOptions = true,
            EnableXFrameOptions = true,
            EnableXssProtection = true,
            EnableContentSecurityPolicy = true,
            ContentSecurityPolicy = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';"
        });

        _middleware = new SecurityHeadersMiddleware(_next.Object, _options.Object);
    }

    [Fact]
    public async Task InvokeAsync_Should_Add_Security_Headers()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        _next.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-Content-Type-Options");
        context.Response.Headers["X-Content-Type-Options"].Should().Contain("nosniff");

        context.Response.Headers.Should().ContainKey("X-Frame-Options");
        context.Response.Headers["X-Frame-Options"].Should().Contain("DENY");

        context.Response.Headers.Should().ContainKey("X-XSS-Protection");
        context.Response.Headers["X-XSS-Protection"].Should().Contain("1; mode=block");

        context.Response.Headers.Should().ContainKey("Content-Security-Policy");
        context.Response.Headers.Should().ContainKey("Referrer-Policy");
        context.Response.Headers["Referrer-Policy"].Should().Contain("strict-origin-when-cross-origin");

        _next.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Add_HSTS_Header_For_HTTPS()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        _next.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Strict-Transport-Security");
        context.Response.Headers["Strict-Transport-Security"].ToString().Should().Be("max-age=31536000; includeSubDomains; preload");
    }

    [Fact]
    public async Task InvokeAsync_Should_Not_Add_HSTS_For_HTTP()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        _next.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().NotContainKey("Strict-Transport-Security");
    }

    [Fact]
    public async Task InvokeAsync_Should_Remove_Server_Header()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Headers["Server"] = "Kestrel";
        _next.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().NotContainKey("Server");
    }

    [Fact]
    public async Task InvokeAsync_Should_Add_Permissions_Policy()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _next.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Permissions-Policy");
        var expectedPolicy = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
        context.Response.Headers["Permissions-Policy"].ToString().Should().Be(expectedPolicy);
    }

    [Fact]
    public async Task InvokeAsync_Should_Not_Override_Existing_Headers()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN"; // Pre-existing value
        _next.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["X-Frame-Options"].Should().Contain("SAMEORIGIN");
    }

    [Fact]
    public async Task InvokeAsync_Should_Add_Cache_Control_For_Sensitive_Endpoints()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auth/login";
        _next.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Cache-Control");
        context.Response.Headers["Cache-Control"].ToString().Should().Be("no-store, no-cache, must-revalidate, proxy-revalidate");

        context.Response.Headers.Should().ContainKey("Pragma");
        context.Response.Headers["Pragma"].ToString().Should().Be("no-cache");
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_Options_Disabled()
    {
        // Arrange
        _options.Setup(x => x.Value).Returns(new SecurityHeaderOptions
        {
            EnableHsts = false,
            EnableXContentTypeOptions = false,
            EnableXFrameOptions = false,
            EnableXssProtection = false,
            EnableContentSecurityPolicy = false
        });

        var middleware = new SecurityHeadersMiddleware(_next.Object, _options.Object);
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        _next.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().NotContainKey("Strict-Transport-Security");
        context.Response.Headers.Should().NotContainKey("X-Content-Type-Options");
        context.Response.Headers.Should().NotContainKey("X-Frame-Options");
        context.Response.Headers.Should().NotContainKey("X-XSS-Protection");
        context.Response.Headers.Should().NotContainKey("Content-Security-Policy");
    }
}