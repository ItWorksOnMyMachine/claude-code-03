using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using CmsBff.Data;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace CmsBff.Endpoints;

/// <summary>
/// Health check response model
/// </summary>
public class HealthCheckResponse
{
    public required string Status { get; set; }
    public required DateTime Timestamp { get; set; }
    public required string Version { get; set; }
    public required string Service { get; set; }
    public required Dictionary<string, string> Dependencies { get; set; }
    public int UptimeSeconds { get; set; }
}

/// <summary>
/// Health check endpoint for monitoring and load balancer health checks
/// </summary>
[HttpGet("/health")]
[AllowAnonymous]
public class HealthCheckEndpoint : EndpointWithoutRequest<HealthCheckResponse>
{
    private readonly CmsDbContext _context;
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public HealthCheckEndpoint(CmsDbContext context)
    {
        _context = context;
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var dependencies = new Dictionary<string, string>();

            // Check database connectivity
            try
            {
                await _context.Database.CanConnectAsync(ct);
                dependencies["database"] = "healthy";
            }
            catch (Exception ex)
            {
                dependencies["database"] = $"unhealthy: {ex.Message}";
            }

            // Check if any critical tables exist
            try
            {
                var contentCount = await _context.Contents.CountAsync(ct);
                dependencies["cms_content_table"] = $"accessible ({contentCount} records)";
            }
            catch (Exception ex)
            {
                dependencies["cms_content_table"] = $"inaccessible: {ex.Message}";
            }

            // Get version information
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            var uptime = (int)(DateTime.UtcNow - StartTime).TotalSeconds;

            var response = new HealthCheckResponse
            {
                Status = dependencies.Values.Any(v => v.Contains("unhealthy")) ? "unhealthy" : "healthy",
                Timestamp = DateTime.UtcNow,
                Version = version,
                Service = "CMS BFF",
                Dependencies = dependencies,
                UptimeSeconds = uptime
            };

            // Return appropriate status code
            if (response.Status == "healthy")
            {
                await SendOkAsync(response, ct);
            }
            else
            {
                HttpContext.Response.StatusCode = 503; // Service Unavailable
                await SendAsync(response, cancellation: ct);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Health check failed");

            var errorResponse = new HealthCheckResponse
            {
                Status = "unhealthy",
                Timestamp = DateTime.UtcNow,
                Version = "unknown",
                Service = "CMS BFF",
                Dependencies = new Dictionary<string, string> { { "health_check", $"failed: {ex.Message}" } },
                UptimeSeconds = (int)(DateTime.UtcNow - StartTime).TotalSeconds
            };

            HttpContext.Response.StatusCode = 503;
            await SendAsync(errorResponse, cancellation: ct);
        }
    }
}