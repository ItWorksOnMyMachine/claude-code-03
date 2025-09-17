using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PlatformBff.Data;
using PlatformBff.Services;
using PlatformBff.Services.Tenant;
using PlatformBff.Repositories;
using StackExchange.Redis;
using PlatformBff.Middleware;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Security.Claims;
using FastEndpoints;
using PlatformShared.Extensions;
using PlatformShared.Services;
using PlatformShared.DataProtection.S3;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
        options.Limits.MinRequestBodyDataRate = new MinDataRate(80, TimeSpan.FromSeconds(10));
        options.Limits.MinResponseDataRate = new MinDataRate(80, TimeSpan.FromSeconds(10));
#if DEBUG
        options.ConfigureEndpoints(builder.Configuration);
#endif
    });
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

// Add FastEndpoints
builder.Services.AddFastEndpoints();
builder.Services.AddHttpContextAccessor();

// Add DbContext with PostgreSQL and tenant context
// Skip database registration in Testing environment (tests will configure their own)
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddDbContext<PlatformDbContext>((serviceProvider, options) =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    });
}

// Note: ITenantContext now provided by PlatformSharedServices

// Add Repositories
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantUserRepository, TenantUserRepository>();

// Add shared platform services (Redis, Data Protection, Session, Tenant Context, Entitlements)
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddPlatformSharedServices(builder.Configuration);
}
else
{
    // Use in-memory services for testing
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddDataProtection()
        .SetApplicationName("PlatformBff");
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<PlatformShared.Services.ITenantContext, PlatformShared.Services.TenantContext>();
    builder.Services.AddScoped<PlatformShared.Services.ISessionService, PlatformShared.Services.DistributedSessionService>();
    builder.Services.AddScoped<PlatformShared.Services.IEntitlementService, PlatformShared.Services.EntitlementService>();
}

// Add Tenant Services
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ITenantAdminService, TenantAdminService>();

// Add Shared Services
builder.Services.AddScoped<PlatformShared.Services.IEntitlementService, PlatformShared.Services.EntitlementService>();

builder.Services.AddDataProtection()
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

builder.Services.AddSingleton<TicketDataFormat>((services) =>
{
    var provider = services.GetDataProtectionProvider();
    var protector = provider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", "Cookies", "v2");
    return new TicketDataFormat(protector);
});

#if DEBUG
var expiresTimeSpan = TimeSpan.FromDays(1);
#else
    var expiresTimeSpan = TimeSpan.FromMinutes(120);
#endif

// Add Authentication services
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = builder.Configuration["AppSettings:CookieName"] ?? "platform.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.CookieManager = new ChunkingCookieManager();
    options.ExpireTimeSpan = expiresTimeSpan;
    options.SlidingExpiration = true;
    options.Cookie.Domain = builder.Configuration["Authentication:CookieDomain"] ?? ".platform.local"; // Set domain if specified
    options.LoginPath = "/api/auth/login";
    options.LogoutPath = "/api/auth/logout";
    options.AccessDeniedPath = "/api/auth/access-denied";

    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = context =>
        {
            if (context.Principal == null)
            {
                context.RejectPrincipal();
                return Task.CompletedTask;
            }

            // Will be used to validate session tokens from Redis
            var sessionId = context.Principal.FindFirst("session_id")?.Value;
            if (string.IsNullOrEmpty(sessionId))
            {
                var claims = context.Principal.Claims.ToList();
                claims.Add(new Claim("session_id", Guid.NewGuid().ToString()));
                var identity = new ClaimsIdentity(claims, context.Principal.Identity.AuthenticationType);
                var principal = new ClaimsPrincipal(identity);
                context.ShouldRenew = true;
                context.ReplacePrincipal(principal);
            }

            return Task.CompletedTask;
        },
    };
});

builder.Services
    .AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .PostConfigure<TicketDataFormat>((opt, tdf) =>
    {
        opt.TicketDataFormat = tdf;
    });

authBuilder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.SignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    options.Authority = builder.Configuration["Authentication:Authority"] ?? "https://login.platform.local:5214";
    options.ClientId = builder.Configuration["Authentication:ClientId"] ?? "platform-bff";
    options.ClientSecret = builder.Configuration["Authentication:ClientSecret"] ?? "platform-bff-secret";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.UsePkce = true;
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.RequireHttpsMetadata = !builder.Environment.IsEnvironment("Testing");

    // Scopes
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("offline_access");

    // Configure events
    options.Events = new OpenIdConnectEvents
    {
        OnMessageReceived = context =>
        {
            // Debug state parameter - safely check both query and form
            var state = context.Request.Query["state"].FirstOrDefault();
            var code = context.Request.Query["code"].FirstOrDefault();

            // Only try to read form if it's a POST with proper content type
            if (context.Request.Method == "POST" &&
                context.Request.HasFormContentType &&
                context.Request.Form != null)
            {
                state = state ?? context.Request.Form["state"].FirstOrDefault();
                code = code ?? context.Request.Form["code"].FirstOrDefault();
            }

            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
            logger?.LogInformation(
                "OIDC Callback received: Method={Method}, State={State}, Code={Code}, HasState={HasState}, Path={Path}",
                context.Request.Method,
                state?.Substring(0, Math.Min(50, state?.Length ?? 0)) + "...",
                code?.Substring(0, Math.Min(10, code?.Length ?? 0)) + "...",
                !string.IsNullOrEmpty(state),
                context.Request.Path);

            // If this is a duplicate callback or missing state, handle gracefully
            if (string.IsNullOrEmpty(state))
            {
                logger?.LogWarning("Callback with missing state - likely duplicate request");

                // Check if user is already authenticated - if so, redirect to frontend
                if (context.HttpContext.User?.Identity?.IsAuthenticated == true)
                {
                    logger?.LogInformation("User already authenticated, redirecting to frontend");
                    context.HandleResponse();
                    var frontendUrl = context.HttpContext.RequestServices.GetService<IConfiguration>()?["Frontend:Url"] ?? "https://host-fe.platform.local:3002";
                    context.Response.Redirect($"{frontendUrl}/auth/callback?auth_callback=true&returnUrl=/");
                    return Task.CompletedTask;
                }

                // If not authenticated and no state, something is wrong - redirect to login
                context.HandleResponse();
                var frontendUrl2 = context.HttpContext.RequestServices.GetService<IConfiguration>()?["Frontend:Url"] ?? "https://host-fe.platform.local:3002";
                context.Response.Redirect($"{frontendUrl2}/login?error=invalid_state");
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        },
        OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
            logger?.LogError("OIDC RemoteFailure: {Error}", context.Failure?.Message);

            // Use the error endpoint which will redirect to frontend gracefully
            context.Response.Redirect($"/api/auth/error?message={Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed")}");
            context.HandleResponse();
            return Task.CompletedTask;
        },
    };
});

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PlatformAdmin", policy =>
        policy.Requirements.Add(new PlatformBff.Authorization.PlatformAdminRequirement()));
});

// Add authorization handlers
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    PlatformBff.Authorization.PlatformAdminAuthorizationHandler>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins("https://host-fe.platform.local:3002") // platform-host
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevelopmentPolicy");
}

if (app.Environment.EnvironmentName != "Testing")
{
    try
    {
        await DatabaseSeeder.InitializeDatabaseAsync(app.Services, app.Environment);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize database");
        // In production, log but continue - database might be handled externally
    }
}

app.UseRouting();

// Add authentication middleware
app.UseAuthentication();

// Add token refresh middleware AFTER authentication
app.UseMiddleware<PlatformBff.Middleware.TokenRefreshMiddleware>();

app.UseAuthorization();

app.UseTenantContext(); // Add tenant context middleware after authentication
app.MapControllers();
app.UseFastEndpoints();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .WithName("HealthCheck");

// Redis connectivity test endpoint (development only)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/test/redis", async (IConnectionMultiplexer? redis) =>
    {
        if (redis == null)
        {
            return Results.Ok(new { status = "not_configured", message = "Redis is not configured, using in-memory cache" });
        }

        try
        {
            var db = redis.GetDatabase();
            var key = "test:ping";
            var value = DateTime.UtcNow.ToString("O");

            // Set a test value
            await db.StringSetAsync(key, value, TimeSpan.FromSeconds(10));

            // Read it back
            var result = await db.StringGetAsync(key);

            // Check server info
            var endpoints = redis.GetEndPoints();
            var server = redis.GetServer(endpoints.First());
            var ping = await db.PingAsync();

            return Results.Ok(new
            {
                status = "connected",
                message = "Redis is operational",
                test_value = result.ToString(),
                ping_ms = ping.TotalMilliseconds,
                endpoint = endpoints.First().ToString()
            });
        }
        catch (Exception ex)
        {
            return Results.Ok(new
            {
                status = "error",
                message = "Redis connection failed",
                error = ex.Message
            });
        }
    })
    .WithName("RedisTest");
}

app.Run();

public partial class Program { } // Make Program accessible for testing