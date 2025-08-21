using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

namespace AuthService.Monitoring;

public static class MonitoringConfiguration
{
    // Custom metrics
    private static readonly Counter LoginAttempts = Metrics.CreateCounter(
        "authservice_login_attempts_total",
        "Total number of login attempts",
        new[] { "status", "client_id" });
    
    private static readonly Counter TokensIssued = Metrics.CreateCounter(
        "authservice_tokens_issued_total",
        "Total number of tokens issued",
        new[] { "grant_type", "client_id" });
    
    private static readonly Gauge ActiveSessions = Metrics.CreateGauge(
        "authservice_active_sessions",
        "Number of active user sessions",
        new[] { "client_id" });
    
    private static readonly Histogram AuthenticationDuration = Metrics.CreateHistogram(
        "authservice_authentication_duration_seconds",
        "Duration of authentication operations",
        new[] { "operation" });
    
    private static readonly Counter FailedAuthentications = Metrics.CreateCounter(
        "authservice_failed_authentications_total",
        "Total number of failed authentication attempts",
        new[] { "reason" });
    
    private static readonly Counter AccountLockouts = Metrics.CreateCounter(
        "authservice_account_lockouts_total",
        "Total number of account lockouts");
    
    private static readonly Gauge DatabaseConnections = Metrics.CreateGauge(
        "authservice_database_connections",
        "Number of active database connections");
    
    private static readonly Counter RateLimitHits = Metrics.CreateCounter(
        "authservice_rate_limit_hits_total",
        "Total number of rate limit violations",
        new[] { "endpoint" });

    public static void ConfigureMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        var monitoringConfig = configuration.GetSection("Monitoring");
        
        if (monitoringConfig.GetValue<bool>("Metrics:Enabled"))
        {
            ConfigureMetrics(services, monitoringConfig);
        }
        
        if (monitoringConfig.GetValue<bool>("Tracing:Enabled"))
        {
            ConfigureTracing(services, monitoringConfig);
        }
        
        // Add custom telemetry service
        services.AddSingleton<ITelemetryService, TelemetryService>();
        
        // Add health checks with detailed reporting
        ConfigureHealthChecks(services, configuration);
    }
    
    private static void ConfigureMetrics(IServiceCollection services, IConfigurationSection config)
    {
        // Configure OpenTelemetry metrics
        services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(
                            serviceName: config["ServiceName"] ?? "AuthService",
                            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("AuthService.Metrics")
                    .AddPrometheusExporter();
                
                if (config["IncludeOpenTelemetry"] == "true")
                {
                    var endpoint = config["ExporterEndpoint"];
                    if (!string.IsNullOrEmpty(endpoint))
                    {
                        builder.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(endpoint);
                            options.Protocol = OtlpExportProtocol.Grpc;
                        });
                    }
                }
            });
    }
    
    private static void ConfigureTracing(IServiceCollection services, IConfigurationSection config)
    {
        // Configure OpenTelemetry tracing
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(
                            serviceName: config["ServiceName"] ?? "AuthService",
                            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) =>
                        {
                            // Don't trace health check endpoints
                            var path = httpContext.Request.Path.Value;
                            return !path.StartsWith("/health") && !path.StartsWith("/metrics");
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource("AuthService.Tracing")
                    .SetSampler(new TraceIdRatioBasedSampler(
                        config.GetValue<double>("SamplingRatio", 0.1)));
                
                var endpoint = config["ExporterEndpoint"];
                if (!string.IsNullOrEmpty(endpoint))
                {
                    builder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(endpoint);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
                }
                
                // Add console exporter for development
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    builder.AddConsoleExporter();
                }
            });
    }
    
    private static void ConfigureHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();
        
        // Database health check
        var dbTimeout = configuration.GetValue<TimeSpan>("Monitoring:HealthChecks:DatabaseTimeout", TimeSpan.FromSeconds(30));
        healthChecksBuilder.AddDbContextCheck<Data.AuthDbContext>(
            name: "database",
            tags: new[] { "ready", "db" },
            customTestQuery: async (context, cancellationToken) =>
            {
                var canConnect = await context.Database.CanConnectAsync(cancellationToken);
                return canConnect;
            });
        
        // Redis health check
        var redisConnection = configuration["Redis:Configuration"];
        if (!string.IsNullOrEmpty(redisConnection))
        {
            var redisTimeout = configuration.GetValue<TimeSpan>("Monitoring:HealthChecks:RedisTimeout", TimeSpan.FromSeconds(10));
            healthChecksBuilder.AddRedis(
                redisConnection,
                name: "redis",
                tags: new[] { "ready", "cache" },
                timeout: redisTimeout);
        }
        
        // Custom health check for IdentityServer
        healthChecksBuilder.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
            name: "identityserver",
            factory: sp => new IdentityServerHealthCheck(sp),
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            tags: new[] { "ready", "auth" }));
        
        // Memory health check
        healthChecksBuilder.AddProcessAllocatedMemoryHealthCheck(
            maximumMegabytesAllocated: 1000,
            name: "memory",
            tags: new[] { "performance" });
        
        // Disk space health check
        healthChecksBuilder.AddDiskStorageHealthCheck(
            setup: options =>
            {
                options.AddDrive("/", 1024); // 1GB minimum free space
            },
            name: "disk",
            tags: new[] { "infrastructure" });
    }
    
    // Metric recording methods
    public static void RecordLoginAttempt(string status, string clientId)
    {
        LoginAttempts.WithLabels(status, clientId).Inc();
    }
    
    public static void RecordTokenIssued(string grantType, string clientId)
    {
        TokensIssued.WithLabels(grantType, clientId).Inc();
    }
    
    public static void UpdateActiveSessions(string clientId, double count)
    {
        ActiveSessions.WithLabels(clientId).Set(count);
    }
    
    public static IDisposable MeasureAuthenticationDuration(string operation)
    {
        return AuthenticationDuration.WithLabels(operation).NewTimer();
    }
    
    public static void RecordFailedAuthentication(string reason)
    {
        FailedAuthentications.WithLabels(reason).Inc();
    }
    
    public static void RecordAccountLockout()
    {
        AccountLockouts.Inc();
    }
    
    public static void UpdateDatabaseConnections(double count)
    {
        DatabaseConnections.Set(count);
    }
    
    public static void RecordRateLimitHit(string endpoint)
    {
        RateLimitHits.WithLabels(endpoint).Inc();
    }
}

// Custom telemetry service
public interface ITelemetryService
{
    void TrackEvent(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null);
    void TrackException(Exception exception, Dictionary<string, string>? properties = null);
    void TrackDependency(string dependencyType, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success);
    void TrackMetric(string name, double value, Dictionary<string, string>? properties = null);
}

public class TelemetryService : ITelemetryService
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    
    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger;
        _activitySource = new ActivitySource("AuthService.Tracing");
        _meter = new Meter("AuthService.Metrics");
    }
    
    public void TrackEvent(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null)
    {
        using var activity = _activitySource.StartActivity(eventName);
        
        if (activity != null)
        {
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    activity.SetTag(prop.Key, prop.Value);
                }
            }
            
            if (metrics != null)
            {
                foreach (var metric in metrics)
                {
                    activity.SetTag($"metric.{metric.Key}", metric.Value);
                }
            }
        }
        
        _logger.LogInformation("Event: {EventName} Properties: {@Properties} Metrics: {@Metrics}", 
            eventName, properties, metrics);
    }
    
    public void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        Activity.Current?.RecordException(exception);
        Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.Message);
        
        if (properties != null && Activity.Current != null)
        {
            foreach (var prop in properties)
            {
                Activity.Current.SetTag($"exception.{prop.Key}", prop.Value);
            }
        }
        
        _logger.LogError(exception, "Exception tracked with properties: {@Properties}", properties);
    }
    
    public void TrackDependency(string dependencyType, string dependencyName, string data, 
        DateTimeOffset startTime, TimeSpan duration, bool success)
    {
        using var activity = _activitySource.StartActivity($"dependency.{dependencyType}");
        
        if (activity != null)
        {
            activity.SetTag("dependency.type", dependencyType);
            activity.SetTag("dependency.name", dependencyName);
            activity.SetTag("dependency.data", data);
            activity.SetTag("dependency.duration", duration.TotalMilliseconds);
            activity.SetTag("dependency.success", success);
            activity.SetStatus(success ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        }
        
        _logger.LogInformation("Dependency: {Type}/{Name} Duration: {Duration}ms Success: {Success}", 
            dependencyType, dependencyName, duration.TotalMilliseconds, success);
    }
    
    public void TrackMetric(string name, double value, Dictionary<string, string>? properties = null)
    {
        var counter = _meter.CreateCounter<double>(name);
        counter.Add(value);
        
        _logger.LogInformation("Metric: {Name} = {Value} Properties: {@Properties}", 
            name, value, properties);
    }
}

// Custom health check for IdentityServer
public class IdentityServerHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    
    public IdentityServerHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            
            // Check if IdentityServer services are registered
            var identityServerService = scope.ServiceProvider.GetService<Duende.IdentityServer.Services.IIdentityServerInteractionService>();
            
            if (identityServerService == null)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "IdentityServer services not available");
            }
            
            // Additional checks could be added here
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                "IdentityServer is operational");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "IdentityServer health check failed",
                exception: ex);
        }
    }
}