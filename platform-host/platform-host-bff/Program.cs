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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
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

// Add Tenant Context
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Add Repositories
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantUserRepository, TenantUserRepository>();

// Add Redis for distributed caching
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
        ConnectionMultiplexer.Connect(redisConnection));
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "PlatformBff";
    });
    
    // Configure Data Protection with Redis for distributed key storage
    var redis = ConnectionMultiplexer.Connect(redisConnection);
    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
        .SetApplicationName("PlatformBff");
}
else
{
    // Fallback to in-memory cache if Redis is not configured
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddDataProtection()
        .SetApplicationName("PlatformBff");
}

// Add Session Service for token management
builder.Services.AddScoped<ISessionService, RedisSessionService>();

// Add Tenant Services
builder.Services.AddScoped<ITenantService, TenantService>();
// TODO: Add ITenantAdminService when implemented

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "platform.session";
});

// Add Authentication services
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "platform.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.LoginPath = "/api/auth/login";
    options.LogoutPath = "/api/auth/logout";
    options.AccessDeniedPath = "/api/auth/access-denied";
    
    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {
            // Will be used to validate session tokens from Redis
            var sessionId = context.Properties.GetTokenValue("session_id");
            if (string.IsNullOrEmpty(sessionId))
            {
                context.RejectPrincipal();
            }
        }
    };
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = builder.Configuration["Authentication:Authority"] ?? "http://localhost:5001";
    options.ClientId = builder.Configuration["Authentication:ClientId"] ?? "platform-bff";
    options.ClientSecret = builder.Configuration["Authentication:ClientSecret"] ?? "platform-bff-secret";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing");
    
    // Scopes
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("offline_access");
    
    // Map claims
    options.ClaimActions.MapJsonKey("preferred_username", "preferred_username");
    options.ClaimActions.MapJsonKey("email", "email");
    options.ClaimActions.MapJsonKey("name", "name");
    
    // Configure events
    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = async context =>
        {
            // Store tokens in Redis after successful authentication
            var sessionId = Guid.NewGuid().ToString();
            context.Properties!.SetString("session_id", sessionId);
            
            // Token storage will be implemented with ISessionService
            await Task.CompletedTask;
        },
        OnRedirectToIdentityProviderForSignOut = context =>
        {
            // Clear session from Redis on sign out
            var sessionId = context.Properties?.GetString("session_id");
            if (!string.IsNullOrEmpty(sessionId))
            {
                // Session cleanup will be implemented with ISessionService
            }
            return Task.CompletedTask;
        },
        OnRemoteFailure = context =>
        {
            context.Response.Redirect("/api/auth/error?message=" + context.Failure?.Message);
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
});

// Add Authorization
builder.Services.AddAuthorization();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3002") // platform-host
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
    
    // Seed database in development (skip in Testing environment)
    if (app.Environment.EnvironmentName != "Testing")
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            DbContextExtensions.HostingEnvironment = app.Environment;
            await DatabaseSeeder.SeedAsync(context);
            
            // Seed platform tenant
            await PlatformBff.Data.SeedData.PlatformTenantSeeder.SeedAsync(context);
            
            // In development, make the test admin user a platform admin
            // This is the user from auth service with sub "e4ee8e51-0279-4c19-8f36-8f7b616e9f09"
            await PlatformBff.Data.SeedData.PlatformTenantSeeder.AssignPlatformAdminAsync(
                context, 
                "e4ee8e51-0279-4c19-8f36-8f7b616e9f09", // admin user from auth service
                "admin@platform.local"
            );
        }
    }
}
else if (app.Environment.EnvironmentName != "Testing")
{
    // In production, still need to ensure platform tenant exists (skip in Testing)
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await PlatformBff.Data.SeedData.PlatformTenantSeeder.SeedAsync(context);
    }
}

app.UseRouting();
app.UseSession(); // Add session middleware

// Add authentication middleware
app.UseAuthentication();

// Add token refresh middleware AFTER authentication
app.UseMiddleware<PlatformBff.Middleware.TokenRefreshMiddleware>();

app.UseAuthorization();

app.UseTenantContext(); // Add tenant context middleware after authentication
app.MapControllers();

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