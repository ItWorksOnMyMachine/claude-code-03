using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<RateLimitOptions> options)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.EnableIpRateLimiting)
        {
            await _next(context);
            return;
        }

        var ipAddress = GetClientIpAddress(context);
        
        // Check if IP is whitelisted
        if (_options.WhitelistedIps?.Contains(ipAddress) == true)
        {
            await _next(context);
            return;
        }

        // Apply IP-based rate limiting
        var ipLimitExceeded = await CheckIpRateLimitAsync(ipAddress, context);
        if (ipLimitExceeded)
        {
            await WriteRateLimitResponseAsync(context);
            return;
        }

        await _next(context);
    }

    private async Task<bool> CheckIpRateLimitAsync(string ipAddress, HttpContext context)
    {
        var minuteKey = $"ip_minute_{ipAddress}_{DateTime.UtcNow:yyyyMMddHHmm}";
        var hourKey = $"ip_hour_{ipAddress}_{DateTime.UtcNow:yyyyMMddHH}";

        var minuteCount = await IncrementCounterAsync(minuteKey, TimeSpan.FromMinutes(1));
        var hourCount = await IncrementCounterAsync(hourKey, TimeSpan.FromHours(1));

        // Set rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = _options.RequestsPerMinute.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, _options.RequestsPerMinute - minuteCount).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString();

        if (minuteCount > _options.RequestsPerMinute)
        {
            _logger.LogWarning("IP {IpAddress} exceeded per-minute rate limit ({Count}/{Limit})", 
                ipAddress, minuteCount, _options.RequestsPerMinute);
            return true;
        }

        if (hourCount > _options.RequestsPerHour)
        {
            _logger.LogWarning("IP {IpAddress} exceeded hourly rate limit ({Count}/{Limit})", 
                ipAddress, hourCount, _options.RequestsPerHour);
            return true;
        }

        return false;
    }


    private async Task<int> IncrementCounterAsync(string key, TimeSpan expiration)
    {
        var count = await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = expiration;
            return await Task.FromResult(0);
        });

        count++;
        _cache.Set(key, count, expiration);
        return count;
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for X-Forwarded-For header (when behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(ip => ip.Trim())
                                 .ToList();
            if (ips.Any())
            {
                return ips.First(); // Use the first IP (original client)
            }
        }

        // Check X-Real-IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to remote IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private async Task WriteRateLimitResponseAsync(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.Headers["Retry-After"] = "60"; // Retry after 60 seconds
        
        await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
    }
}

public class RateLimitOptions
{
    public bool EnableIpRateLimiting { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
    public string[]? WhitelistedIps { get; set; }
    public string[]? EndpointOverrides { get; set; } // Specific endpoints with different limits
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}