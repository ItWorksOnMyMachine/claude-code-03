using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AuthService.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeaderOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeaderOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);

        // Call the next middleware
        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Strict-Transport-Security (HSTS)
        if (_options.EnableHsts && context.Request.IsHttps)
        {
            if (!headers.ContainsKey("Strict-Transport-Security"))
            {
                headers["Strict-Transport-Security"] = $"max-age={_options.HstsMaxAge}; includeSubDomains; preload";
            }
        }

        // X-Content-Type-Options
        if (_options.EnableXContentTypeOptions && !headers.ContainsKey("X-Content-Type-Options"))
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }

        // X-Frame-Options
        if (_options.EnableXFrameOptions && !headers.ContainsKey("X-Frame-Options"))
        {
            headers["X-Frame-Options"] = _options.XFrameOptionsPolicy;
        }

        // X-XSS-Protection
        if (_options.EnableXssProtection && !headers.ContainsKey("X-XSS-Protection"))
        {
            headers["X-XSS-Protection"] = "1; mode=block";
        }

        // Content-Security-Policy
        if (_options.EnableContentSecurityPolicy && !string.IsNullOrEmpty(_options.ContentSecurityPolicy))
        {
            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
            }
        }

        // Referrer-Policy
        if (!headers.ContainsKey("Referrer-Policy"))
        {
            headers["Referrer-Policy"] = _options.ReferrerPolicy;
        }

        // Permissions-Policy (formerly Feature-Policy)
        if (!string.IsNullOrEmpty(_options.PermissionsPolicy) && !headers.ContainsKey("Permissions-Policy"))
        {
            headers["Permissions-Policy"] = _options.PermissionsPolicy;
        }

        // Remove Server header
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetMvc-Version");

        // Cache control for sensitive endpoints
        if (IsSensitiveEndpoint(context.Request.Path))
        {
            if (!headers.ContainsKey("Cache-Control"))
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate, proxy-revalidate";
            }
            if (!headers.ContainsKey("Pragma"))
            {
                headers["Pragma"] = "no-cache";
            }
            if (!headers.ContainsKey("Expires"))
            {
                headers["Expires"] = "0";
            }
        }
    }

    private bool IsSensitiveEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        
        return pathValue.Contains("/auth") ||
               pathValue.Contains("/login") ||
               pathValue.Contains("/logout") ||
               pathValue.Contains("/account") ||
               pathValue.Contains("/token") ||
               pathValue.Contains("/connect") ||
               pathValue.Contains("/api/");
    }
}

public class SecurityHeaderOptions
{
    public bool EnableHsts { get; set; } = true;
    public int HstsMaxAge { get; set; } = 31536000; // 1 year in seconds
    public bool EnableXContentTypeOptions { get; set; } = true;
    public bool EnableXFrameOptions { get; set; } = true;
    public string XFrameOptionsPolicy { get; set; } = "DENY";
    public bool EnableXssProtection { get; set; } = true;
    public bool EnableContentSecurityPolicy { get; set; } = true;
    public string ContentSecurityPolicy { get; set; } = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none';";
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    public string PermissionsPolicy { get; set; } = 
        "accelerometer=(), " +
        "camera=(), " +
        "geolocation=(), " +
        "gyroscope=(), " +
        "magnetometer=(), " +
        "microphone=(), " +
        "payment=(), " +
        "usb=()";
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}