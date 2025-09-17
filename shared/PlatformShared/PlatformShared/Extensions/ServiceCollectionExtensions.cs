using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
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
        services.AddScoped<IEntitlementService, EntitlementService>();

        return services;
    }

    /// <summary>
    /// Adds authentication configuration shared across BFF services
    /// </summary>
    public static IServiceCollection AddPlatformAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
#if DEBUG
        var expiresTimeSpan = TimeSpan.FromDays(1);
#else
    var expiresTimeSpan = TimeSpan.FromMinutes(120);
#endif

        var authConfig = configuration.GetSection("Authentication");

        services.AddDataProtection()
            // This isn't ideal, but for local development we'll use file system storage
            // In production, consider using a more robust solution like AWS S3 or Azure Blob Storage
            .PersistKeysToFileSystem(new DirectoryInfo(@"C:\\temp\\platform-keys"))
            // Example for S3 (uncomment and configure as needed):
            // .PersistKeysToS3(new S3XmlRepositoryConfiguration
            // {
            //     BucketName = builder.Configuration["AppSettings:DocumentRootBucketName"],
            //     KeyId = builder.Configuration["AppSettings:DocumentRootBucketKey"],
            //     Prefix = "app-data/shared/asp-keys"
            // })
            .SetApplicationName("Platform");

        services.AddSingleton<TicketDataFormat>((services) =>
        {
            var provider = services.GetDataProtectionProvider();
            var protector = provider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", "Cookies", "v2");
            return new TicketDataFormat(protector);
        });

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
        })
        .AddCookie("Cookies", options =>
        {
            options.Cookie.Name = configuration["AppSettings:CookieName"] ?? "platform.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = expiresTimeSpan;
            options.SlidingExpiration = true;
            options.Cookie.Domain = configuration["AppSettings:CookieDomain"] ?? ".platform.local";
            options.CookieManager = new ChunkingCookieManager();
            options.LoginPath = new PathString("/");
            options.Cookie.Expiration = null;
        });

        services
            .AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .PostConfigure<TicketDataFormat>((opt, tdf) =>
            {
                opt.TicketDataFormat = tdf;
            });

        services.AddAuthorization();

        return services;
    }
}