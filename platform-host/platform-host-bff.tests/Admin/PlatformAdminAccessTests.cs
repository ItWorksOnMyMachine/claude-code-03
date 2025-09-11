using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PlatformBff.Data;
using PlatformBff.Models;
using PlatformBff.Data.Entities;
using PlatformBff.Models.Tenant;
using PlatformBff.Services;
using PlatformBff.Services.Tenant;
using PlatformBff.Tests.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text.Encodings.Web;
using Xunit;

namespace PlatformBff.Tests.Admin;

public class PlatformAdminAccessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static int _testCounter = 0;

    public PlatformAdminAccessTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateFactory(Guid? selectedTenantId = null, bool isPlatformAdmin = false)
    {
        var testId = System.Threading.Interlocked.Increment(ref _testCounter);
        var dbName = $"PlatformAdminTest_{testId}";

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Replace DbContext with in-memory database
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PlatformDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<PlatformDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.EnableSensitiveDataLogging();
                });

                // Configure test session
                var sessionService = new TestSessionService(selectedTenantId, isPlatformAdmin);
                services.AddSingleton<ISessionService>(sessionService);

                // Mock tenant service for platform admin check
                var tenantServiceMock = new Mock<ITenantService>();
                tenantServiceMock.Setup(x => x.IsPlatformAdminAsync(It.IsAny<string>()))
                    .ReturnsAsync(isPlatformAdmin);
                services.AddScoped<ITenantService>(_ => tenantServiceMock.Object);

                // Configure tenant context
                services.AddScoped<ITenantContext, TestTenantContext>();

                // Add authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });

                // Configure static OIDC configuration to avoid network calls
                services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    // Create a static OIDC configuration
                    var config = new OpenIdConnectConfiguration
                    {
                        Issuer = "https://test-idp.local",
                        AuthorizationEndpoint = "https://test-idp.local/connect/authorize",
                        TokenEndpoint = "https://test-idp.local/connect/token",
                        UserInfoEndpoint = "https://test-idp.local/connect/userinfo",
                        JwksUri = "https://test-idp.local/.well-known/jwks.json",
                        EndSessionEndpoint = "https://test-idp.local/connect/endsession"
                    };

                    // Use static configuration manager to prevent metadata fetching
                    options.Configuration = config;
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(config);

                    // Ensure events are initialized
                    options.Events ??= new OpenIdConnectEvents();
                });
            });
        });
    }

    [Fact]
    public async Task PlatformAdmin_CanAccessAllTenantData_WithoutSelectingTenant()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: true);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        // Seed test data
        var platformTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var customerTenant1 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Customer 1",
            Slug = "customer-1",
            DisplayName = "Customer 1 Company",
            IsActive = true
        };
        var customerTenant2 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Customer 2",
            Slug = "customer-2",
            DisplayName = "Customer 2 Company",
            IsActive = true
        };

        dbContext.Tenants.AddRange(customerTenant1, customerTenant2);
        await dbContext.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        // Act
        var response = await client.GetAsync("/api/admin/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenants = await response.Content.ReadFromJsonAsync<List<PlatformBff.Models.Tenant.TenantInfo>>();
        tenants.Should().NotBeNull();
        tenants!.Should().HaveCountGreaterOrEqualTo(2);
        tenants!.Should().Contain(t => t.Name == "Customer 1" && t.DisplayName == "Customer 1 Company");
        tenants!.Should().Contain(t => t.Name == "Customer 2" && t.DisplayName == "Customer 2 Company");
    }


    [Fact]
    public async Task PlatformAdmin_CanCreateAndManageTenantsAcrossSystem()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        // Act 1: Create multiple tenants
        var tenant1Dto = new CreateTenantDto { Name = "Tenant A", Description = "First tenant" };
        var tenant2Dto = new CreateTenantDto { Name = "Tenant B", Description = "Second tenant" };

        var response1 = await client.PostAsJsonAsync("/api/admin/tenants", tenant1Dto);
        var response2 = await client.PostAsJsonAsync("/api/admin/tenants", tenant2Dto);

        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        var tenant1 = await response1.Content.ReadFromJsonAsync<PlatformBff.Models.Tenant.TenantInfo>();
        var tenant2 = await response2.Content.ReadFromJsonAsync<PlatformBff.Models.Tenant.TenantInfo>();

        // Act 2: Get all tenants with pagination
        var pageResponse = await client.GetAsync("/api/admin/tenants?page=1&pageSize=10");
        pageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var allTenants = await pageResponse.Content.ReadFromJsonAsync<List<PlatformBff.Models.Tenant.TenantInfo>>();
        allTenants.Should().NotBeNull();
        allTenants!.Should().Contain(t => t.Name == "tenant-a" && t.DisplayName == "Tenant A");
        allTenants!.Should().Contain(t => t.Name == "tenant-b" && t.DisplayName == "Tenant B");

        // Act 3: Deactivate one tenant
        var deactivateResponse = await client.PostAsync($"/api/admin/tenant/{tenant1!.Id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 4: Verify deactivation
        var tenantStatusResponse = await client.GetAsync("/api/admin/tenants");
        var updatedTenants = await tenantStatusResponse.Content.ReadFromJsonAsync<List<PlatformBff.Models.Tenant.TenantInfo>>();
        var deactivatedTenant = updatedTenants!.FirstOrDefault(t => t.Id == tenant1.Id);
        deactivatedTenant.Should().NotBeNull();
        deactivatedTenant!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task NonPlatformAdmin_CannotAccessAdminEndpoints()
    {
        // Arrange
        var regularTenantId = Guid.NewGuid();
        var factory = CreateFactory(selectedTenantId: regularTenantId, isPlatformAdmin: false);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        // Act & Assert - Try various admin endpoints
        var endpoints = new[]
        {
            ("/api/admin/tenants", "GET"),
            ($"/api/admin/tenant/{Guid.NewGuid()}/deactivate", "POST")
        };

        foreach (var (endpoint, method) in endpoints)
        {
            var response = method == "POST"
                ? await client.PostAsync(endpoint, null)
                : await client.GetAsync(endpoint);
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
            // Endpoint should be protected: $endpoint
        }
    }

    [Fact]
    public async Task PlatformAdmin_CanAssignUsersToAnyTenant()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: true);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        // Create test tenants
        var tenant1 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tenant for User Assignment 1",
            Slug = "tenant-user-1",
            DisplayName = "Tenant for User Assignment 1",
            IsActive = true
        };
        var tenant2 = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tenant for User Assignment 2",
            Slug = "tenant-user-2",
            DisplayName = "Tenant for User Assignment 2",
            IsActive = true
        };
        dbContext.Tenants.AddRange(tenant1, tenant2);
        await dbContext.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        // Act: Assign same user to multiple tenants
        var userId = "user-123";
        var email = "user@example.com";

        var response1 = await client.PostAsync(
            $"/api/admin/tenant/{tenant1.Id}/users?userId={userId}&email={email}&role=User", null);
        var response2 = await client.PostAsync(
            $"/api/admin/tenant/{tenant2.Id}/users?userId={userId}&email={email}&role=Admin", null);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user is assigned to both tenants
        using var verifyScope = factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var userTenants = await verifyContext.TenantUsers
            .Where(tu => tu.UserId == userId)
            .ToListAsync();

        userTenants.Should().HaveCount(2);
        userTenants.Should().Contain(tu => tu.TenantId == tenant1.Id);
        userTenants.Should().Contain(tu => tu.TenantId == tenant2.Id);
    }

    // Helper classes
    private class TestSessionService : ISessionService
    {
        private readonly Guid? _selectedTenantId;
        private readonly bool _isPlatformAdmin;
        private readonly Dictionary<string, string> _sessionData = new();

        public TestSessionService(Guid? selectedTenantId, bool isPlatformAdmin)
        {
            _selectedTenantId = selectedTenantId;
            _isPlatformAdmin = isPlatformAdmin;

            // Initialize test session
            _sessionData[$"test-session:{nameof(PlatformBffSessionKeys.UserId)}"] = "test-user";
            _sessionData[$"test-session:{nameof(PlatformBffSessionKeys.SelectedTenantId)}"] = _selectedTenantId?.ToString() ?? (_isPlatformAdmin ? Guid.Parse("00000000-0000-0000-0000-000000000001").ToString() : null);
        }

        public Task<HasValueOrMissingResult<string>> GetSessionDataAsync(string sessionId, string name)
        {
            _sessionData.TryGetValue($"{sessionId}:{name}", out var sessionData);
            return Task.FromResult(sessionData != null
                ? HasValueOrMissingResult<string>.SetValue(sessionData.GetType().GetProperty(name)?.GetValue(sessionData)?.ToString() ?? "")
                : HasValueOrMissingResult<string>.SetMissing());
        }

        public Task StoreSessionDataAsync(string sessionId, string name, string data, DateTimeOffset? expiresAt = null)
        {
            _sessionData[$"{sessionId}:{name}"] = data;
            return Task.CompletedTask;
        }

        public Task RemoveSessionAsync(string sessionId)
        {
            var keysToRemove = _sessionData.Keys.Where(k => k.StartsWith(sessionId + ":"));
            foreach (var key in keysToRemove)
                _sessionData.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveSessionDataAsync(string sessionId, string name)
        {
            _sessionData.Remove($"test-session:{sessionId}:{name}");
            return Task.CompletedTask;
        }

        // Token methods (not used in these tests)
        public Task StoreTokensAsync(string sessionId, TokenData tokens) => Task.CompletedTask;
        public Task<TokenData?> GetTokensAsync(string sessionId) => Task.FromResult<TokenData?>(null);
        public Task<TokenData?> RefreshTokensAsync(string sessionId, string refreshToken) => Task.FromResult<TokenData?>(null);
        public Task RevokeTokensAsync(string sessionId) => Task.CompletedTask;
    }

    private class TestTenantContext : ITenantContext
    {
        private readonly IServiceProvider _serviceProvider;
        private Guid? _currentTenantId;
        private string? _currentUserId;

        public TestTenantContext(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<Guid?> GetCurrentTenantIdAsync()
        {
            if (_currentTenantId.HasValue)
                return _currentTenantId;

            var httpContext = _serviceProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()?.HttpContext;

            var sessionId = httpContext?.User.FindFirst("session_id")?.Value;
            if (string.IsNullOrEmpty(sessionId))
            {
                return null;
            }

            var sessionService = _serviceProvider.GetRequiredService<ISessionService>();
            var SelectedTenantIdResult = await sessionService.GetSessionDataAsync(sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId));
            return SelectedTenantIdResult.HasValue && Guid.TryParse(SelectedTenantIdResult.Value, out var tenantId)
                ? tenantId
                : null;
        }

        public Task SetTenant(Guid tenantId)
        {
            _currentTenantId = tenantId;
            return Task.CompletedTask;
        }

        public Task ClearTenant()
        {
            _currentTenantId = null;
            return Task.CompletedTask;
        }

        public async Task<bool> IsPlatformTenant()
        {
            var tenantId = await GetCurrentTenantIdAsync();
            return tenantId.HasValue && tenantId.Value == Guid.Parse("00000000-0000-0000-0000-000000000001");
        }

        public async Task<string?> GetCurrentUserId()
        {
            if (!string.IsNullOrEmpty(_currentUserId))
                return _currentUserId;

            var httpContext = _serviceProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()?.HttpContext;

            var sessionId = httpContext?.User.FindFirst("session_id")?.Value;
            if (string.IsNullOrEmpty(sessionId))
            {
                return null;
            }

            var sessionService = _serviceProvider.GetRequiredService<ISessionService>();
            var userIdResult = await sessionService.GetSessionDataAsync(sessionId, nameof(PlatformBffSessionKeys.UserId));
            return userIdResult.HasValue ? userIdResult.Value : null;
        }

        public Task SetUserId(string userId)
        {
            _currentUserId = userId;
            return Task.CompletedTask;
        }
    }

    private class TestAuthenticationHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<TestAuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(
            Microsoft.Extensions.Options.IOptionsMonitor<TestAuthenticationSchemeOptions> options,
            Microsoft.Extensions.Logging.ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user"),
                new System.Security.Claims.Claim("sub", "test-user")
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");

            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
        }
    }

    private class TestAuthenticationSchemeOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions { }
}
