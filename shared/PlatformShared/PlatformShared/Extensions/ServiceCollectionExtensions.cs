using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformShared.Services;
using StackExchange.Redis;

namespace PlatformShared.Extensions;

/// <summary>
/// Extension methods for configuring shared platform services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds shared platform services to the DI container
    /// </summary>
    public static IServiceCollection AddPlatformSharedServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Redis for session storage
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "PlatformHost";
        });

        // Add centralized data protection
        services.AddDataProtection()
            .SetApplicationName("PlatformHost")
            .PersistKeysToStackExchangeRedis(
                ConnectionMultiplexer.Connect(redisConnectionString),
                "DataProtection-Keys"
            );

        // Add shared services
        services.AddHttpContextAccessor();
        services.AddHttpClient();

        services.AddScoped<ISessionService, DistributedSessionService>();
        services.AddScoped<ITenantContext, TenantContext>();

        return services;
    }

    /// <summary>
    /// Adds authentication configuration shared across BFF services
    /// </summary>
    public static IServiceCollection AddPlatformAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("Authentication");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie("Cookies", options =>
        {
            options.Cookie.Name = "platform.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.Cookie.Domain = ".platform.local";
            options.ExpireTimeSpan = TimeSpan.FromHours(2);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
        })
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = authConfig["Authority"];
            options.ClientId = authConfig["ClientId"];
            options.ClientSecret = authConfig["ClientSecret"];
            options.ResponseType = "code";
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("platform_api");
            options.UsePkce = true;
            options.SaveTokens = false; // We handle tokens manually
            options.GetClaimsFromUserInfoEndpoint = true;
        });

        services.AddAuthorization();

        return services;
    }
}