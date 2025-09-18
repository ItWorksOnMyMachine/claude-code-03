using Microsoft.EntityFrameworkCore;
using CmsBff.Data;
using CmsBff.Services;
using FastEndpoints;
using FastEndpoints.Swagger;
using PlatformShared.Extensions;
using PlatformShared.Services;
using PlatformShared.Middleware;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Server.Kestrel.Core;

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

// Add FastEndpoints
builder.Services.AddFastEndpoints();

// Add environment-specific services
if (builder.Environment.IsEnvironment("Testing"))
{
    // For testing environment, use in-memory database and services to avoid Redis dependency
    builder.Services.AddDbContext<CmsDbContext>(options =>
        options.UseInMemoryDatabase("CmsIntegrationTestDb"));

    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddDataProtection()
        .SetApplicationName("CmsBff");
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient();

    // Add authentication and authorization for testing
    builder.Services.AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });
    builder.Services.AddAuthorization();

    // Use simple test services that dont have complex dependencies
    builder.Services.AddScoped<ITenantContext, TestTenantContext>();
    builder.Services.AddScoped<ISessionService, TestSessionService>();
    builder.Services.AddScoped<IEntitlementService, TestEntitlementService>();
}
else
{
    // Production environment uses PostgreSQL database and shared platform services with Redis
    builder.Services.AddDbContext<CmsDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add shared platform services (Redis, Data Protection, Session, Tenant Context)
    builder.Services.AddPlatformSharedServices(builder.Configuration);

    // Add shared platform authentication for non-testing environments
    builder.Services.AddPlatformAuthentication(builder.Configuration);
}

// Add CMS services
builder.Services.AddScoped<CmsContentService>();
builder.Services.AddScoped<CmsAssetService>();
builder.Services.AddScoped<CmsTemplateService>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins("https://host-fe.platform.local:3002", "https://cms.platform.local:3003")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
    app.UseSwaggerGen();
}

app.UseHttpsRedirection();

// Add authentication and authorization middleware for all environments
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(c =>
{
});

// Ensure database is created (only for non-testing environments to avoid conflicts)
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        context.Database.EnsureCreated();
    }
}

app.Run();

// Simple test authentication handler for testing environment
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // For testing, always return a successful authentication with a test user
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("tenant", "test-tenant"),
            new Claim("session_id", "test-session")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// Make Program class accessible for testing
public partial class Program { }

// Simple test tenant context that doesnt have complex dependencies
public class TestTenantContext : ITenantContext
{
    public Task<string?> GetCurrentUserId()
    {
        return Task.FromResult<string?>("test-user");
    }

    public Task<Guid?> GetCurrentTenantIdAsync()
    {
        return Task.FromResult<Guid?>(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }


    public Task SetTenant(Guid tenantId)
    {
        return Task.CompletedTask;
    }

    public Task ClearTenant()
    {
        return Task.CompletedTask;
    }

    public Task<bool> IsPlatformTenant()
    {
        return Task.FromResult(false);
    }
}

// Simple test session service for testing
public class TestSessionService : ISessionService
{
    private readonly Dictionary<string, string> _sessionData = new();

    public TestSessionService()
    {
        // Set up session data for the test-session that TestAuthenticationHandler creates
        _sessionData["test-session:UserId"] = "test-user";
        _sessionData["test-session:SelectedTenantId"] = "00000000-0000-0000-0000-000000000001";
    }

    public Task<PlatformShared.Models.HasValueOrMissingResult<string>> GetSessionDataAsync(string sessionId, string name)
    {
        _sessionData.TryGetValue($"{sessionId}:{name}", out var sessionData);
        return Task.FromResult(sessionData != null
            ? PlatformShared.Models.HasValueOrMissingResult<string>.SetValue(sessionData)
            : PlatformShared.Models.HasValueOrMissingResult<string>.SetMissing());
    }

    public Task StoreSessionDataAsync(string sessionId, string name, string data, DateTimeOffset? expiresAt = null)
    {
        _sessionData[$"{sessionId}:{name}"] = data;
        return Task.CompletedTask;
    }

    // Other ISessionService methods - minimal implementations for testing
    public Task StoreTokensAsync(string sessionId, PlatformShared.Models.TokenData tokens) => Task.CompletedTask;
    public Task<PlatformShared.Models.TokenData?> GetTokensAsync(string sessionId) => Task.FromResult<PlatformShared.Models.TokenData?>(null);
    public Task<PlatformShared.Models.TokenData?> RefreshTokensAsync(string sessionId, string refreshToken) => Task.FromResult<PlatformShared.Models.TokenData?>(null);
    public Task RevokeTokensAsync(string sessionId) => Task.CompletedTask;
    public Task RemoveSessionAsync(string sessionId) => Task.CompletedTask;
    public Task RemoveSessionDataAsync(string sessionId, string name)
    {
        _sessionData.Remove($"{sessionId}:{name}");
        return Task.CompletedTask;
    }
}

// Simple test entitlement service for testing
public class TestEntitlementService : IEntitlementService
{
    public Task<IEnumerable<string>> GetUserEntitlementsAsync()
    {
        return Task.FromResult<IEnumerable<string>>(new[]
        {
            PlatformEntitlements.CMS_ACCESS,
            PlatformEntitlements.CMS_MANAGE,
            PlatformEntitlements.CMS_ASSETS,
            PlatformEntitlements.CMS_TEMPLATES,
            PlatformEntitlements.CMS_PUBLISH
        });
    }

    public Task<IEnumerable<string>> GetUserEntitlementsAsync(string userId, Guid tenantId)
    {
        return GetUserEntitlementsAsync();
    }

    public Task<bool> HasEntitlementAsync(string entitlement)
    {
        return Task.FromResult(true);
    }

    public Task<bool> HasAnyEntitlementAsync(params string[] entitlements)
    {
        return Task.FromResult(true);
    }

    public Task<bool> HasAllEntitlementsAsync(params string[] entitlements)
    {
        return Task.FromResult(true);
    }

    public Task<IEnumerable<string>> GetMissingEntitlementsAsync(params string[] requiredEntitlements)
    {
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task<bool> CanAccessModuleAsync(string moduleName)
    {
        return Task.FromResult(true);
    }

    public Task<IEnumerable<string>> GetAccessibleModulesAsync()
    {
        return Task.FromResult<IEnumerable<string>>(new[] { "cmsModule" });
    }

    public Task RefreshEntitlementsAsync()
    {
        return Task.CompletedTask;
    }
}
