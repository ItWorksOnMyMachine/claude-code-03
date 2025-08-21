using System;
using System.Net;
using System.Threading.Tasks;
using AuthService.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AuthService.Tests.Middleware;

public class RateLimitingMiddlewareTests
{
    private readonly Mock<RequestDelegate> _next;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<RateLimitingMiddleware>> _logger;
    private readonly Mock<IOptions<RateLimitOptions>> _options;
    private readonly RateLimitingMiddleware _middleware;

    public RateLimitingMiddlewareTests()
    {
        _next = new Mock<RequestDelegate>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = new Mock<ILogger<RateLimitingMiddleware>>();
        _options = new Mock<IOptions<RateLimitOptions>>();
        _options.Setup(x => x.Value).Returns(new RateLimitOptions
        {
            RequestsPerMinute = 60,
            RequestsPerHour = 1000,
            EnableIpRateLimiting = true,
            WhitelistedIps = new[] { "127.0.0.1", "::1" }
        });

        _middleware = new RateLimitingMiddleware(_next.Object, _cache, _logger.Object, _options.Object);
    }

    [Fact]
    public async Task InvokeAsync_Should_Allow_Request_Within_Limit()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1");
        // Pre-populate cache with some requests (under limit)
        var minuteKey = $"ip_minute_192.168.1.1_{DateTime.UtcNow:yyyyMMddHHmm}";
        _cache.Set(minuteKey, 5, TimeSpan.FromMinutes(1));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _next.Verify(n => n(context), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_Should_Block_Request_When_Limit_Exceeded()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1");
        // Pre-populate cache with requests exceeding limit
        var minuteKey = $"ip_minute_192.168.1.1_{DateTime.UtcNow:yyyyMMddHHmm}";
        _cache.Set(minuteKey, 61, TimeSpan.FromMinutes(1));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _next.Verify(n => n(context), Times.Never);
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
        context.Response.Headers.Should().ContainKey("Retry-After");
    }

    [Fact]
    public async Task InvokeAsync_Should_Skip_Whitelisted_IPs()
    {
        // Arrange
        var context = CreateHttpContext("127.0.0.1"); // Whitelisted IP
        // Even with cache showing over limit, whitelisted IPs should pass
        var minuteKey = $"ip_minute_127.0.0.1_{DateTime.UtcNow:yyyyMMddHHmm}";
        _cache.Set(minuteKey, 1000, TimeSpan.FromMinutes(1));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _next.Verify(n => n(context), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_Should_Create_Cache_Entry_For_New_IP()
    {
        // Arrange
        var context = CreateHttpContext("10.0.0.1");
        // Don't pre-populate cache for this IP (simulating new IP)

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        // After invocation, cache should have entry for this IP
        var minuteKey = $"ip_minute_10.0.0.1_{DateTime.UtcNow:yyyyMMddHHmm}";
        _cache.TryGetValue(minuteKey, out var value).Should().BeTrue();
        _next.Verify(n => n(context), Times.Once);
    }


    [Fact]
    public async Task InvokeAsync_Should_Handle_X_Forwarded_For_Header()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1");
        context.Request.Headers["X-Forwarded-For"] = "10.0.0.1, 192.168.1.1";

        // Set cache value for the forwarded IP
        var minuteKey = $"ip_minute_10.0.0.1_{DateTime.UtcNow:yyyyMMddHHmm}";
        _cache.Set(minuteKey, 5, TimeSpan.FromMinutes(1));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _next.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Set_Rate_Limit_Headers()
    {
        // Arrange
        var context = CreateHttpContext("192.168.1.1");
        // Set cache value
        var minuteKey = $"ip_minute_192.168.1.1_{DateTime.UtcNow:yyyyMMddHHmm}";
        _cache.Set(minuteKey, 30, TimeSpan.FromMinutes(1));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-RateLimit-Limit");
        context.Response.Headers.Should().ContainKey("X-RateLimit-Remaining");
        context.Response.Headers.Should().ContainKey("X-RateLimit-Reset");
    }

    private HttpContext CreateHttpContext(string ipAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        context.Request.Path = "/test";
        context.Request.Method = "GET";
        context.Response.Body = new System.IO.MemoryStream();
        
        _next.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        
        return context;
    }
}