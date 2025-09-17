using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using PlatformBff.Data;
using PlatformBff.Models;
using PlatformBff.Models.Tenant;
using PlatformBff.Services;
using PlatformBff.Services.Tenant;
using static PlatformBff.Services.PlatformBffSessionKeys;
using PlatformBff.Tests.Authentication;
using SharedModels = PlatformShared.Models;
using Xunit;

namespace PlatformBff.Tests.Admin;

public class TenantAdminControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static int _testCounter = 0;

    public TenantAdminControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateFactory(bool isPlatformAdmin = false)
    {
        // Use a unique but stable database name for this factory instance
        var testId = Interlocked.Increment(ref _testCounter);
        var dbName = $"TestDb_{testId}";

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PlatformDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Replace database with in-memory using the stable name
                services.AddDbContext<PlatformDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    // Ensure the in-memory database uses the same instance
                    options.EnableSensitiveDataLogging(); // For debugging
                });

                // Use fake session service (not mock) like working PlatformAdminAccessTests
                var sessionService = new TestSessionService(isPlatformAdmin);
                services.AddSingleton<ISessionService>(sessionService);

                // Mock tenant service for platform admin check
                var tenantServiceMock = new Mock<ITenantService>();
                tenantServiceMock.Setup(x => x.IsPlatformAdminAsync(It.IsAny<string>()))
                    .ReturnsAsync(isPlatformAdmin);
                services.AddScoped<ITenantService>(_ => tenantServiceMock.Object);

                // Add required services for middleware (same as DevControllerTests)
                services.AddDistributedMemoryCache();
                services.AddHttpContextAccessor();
                services.AddHttpClient();

                // Register shared services interfaces
                services.AddScoped<PlatformShared.Services.ITenantContext, PlatformShared.Services.TenantContext>();
                services.AddScoped<PlatformShared.Services.ISessionService, PlatformShared.Services.DistributedSessionService>();
                services.AddScoped<PlatformShared.Services.IEntitlementService, PlatformShared.Services.EntitlementService>();

                // Register BFF-specific interfaces for middleware (use real implementation like working tests)
                services.AddScoped<PlatformBff.Services.ITenantContext, TestTenantContext>();
                var mockBffSessionService = new Mock<PlatformBff.Services.ISessionService>();
                services.AddScoped<PlatformBff.Services.ISessionService>(_ => mockBffSessionService.Object);

                // Add test authentication handler to bypass auth
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options =>
                {
                    options.IsAuthenticated = true;
                });

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
    public async Task GetAllTenants_WithoutPlatformAdmin_ReturnsUnauthorized()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: false);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        // Act
        var response = await client.GetAsync("/api/admin/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllTenants_WithPlatformAdmin_ReturnsSuccess()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        // Act
        var response = await client.GetAsync("/api/admin/tenants");

        // Assert - debug to see what's happening
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Status: {response.StatusCode}");
            Console.WriteLine($"Response Content: {errorContent}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateTenant_WithValidData_CreatesSuccessfully()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        var createDto = new CreateTenantDto
        {
            Name = "Test Tenant",
            Description = "Test Description"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/tenants", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var tenant = await response.Content.ReadFromJsonAsync<PlatformBff.Models.Tenant.TenantInfo>(_jsonOptions);
        tenant.Should().NotBeNull();
        tenant!.Name.Should().Be("test-tenant");
        tenant!.DisplayName.Should().Be("Test Tenant");
    }

    [Fact]
    public async Task CreateTenant_WithDuplicateName_ReturnsConflict()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        var createDto = new CreateTenantDto
        {
            Name = "Duplicate Tenant",
            Description = "Test Description"
        };

        // Act - Create first tenant
        var response1 = await client.PostAsJsonAsync("/api/admin/tenants", createDto);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Try to create duplicate
        var response2 = await client.PostAsJsonAsync("/api/admin/tenants", createDto);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeactivateTenant_WithValidId_DeactivatesSuccessfully()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        // Create a tenant first
        var createDto = new CreateTenantDto
        {
            Name = "Tenant To Deactivate",
            Description = "Test Description"
        };
        var createResponse = await client.PostAsJsonAsync("/api/admin/tenants", createDto);
        var tenant = await createResponse.Content.ReadFromJsonAsync<PlatformBff.Models.Tenant.TenantInfo>(_jsonOptions);

        // Act
        var response = await client.PostAsync($"/api/admin/tenant/{tenant!.Id}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateTenant_WithValidData_UpdatesSuccessfully()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        // Create a tenant first
        var createDto = new CreateTenantDto
        {
            Name = "Original Name",
            Description = "Original Description"
        };
        var createResponse = await client.PostAsJsonAsync("/api/admin/tenants", createDto);
        var tenant = await createResponse.Content.ReadFromJsonAsync<PlatformBff.Models.Tenant.TenantInfo>(_jsonOptions);

        var updateDto = new UpdateTenantDto
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/admin/tenant/{tenant!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignUserToTenant_WithValidData_AssignsSuccessfully()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        // Create a tenant first
        var createDto = new CreateTenantDto
        {
            Name = "Tenant For User",
            Description = "Test Description"
        };
        var createResponse = await client.PostAsJsonAsync("/api/admin/tenants", createDto);
        var tenant = await createResponse.Content.ReadFromJsonAsync<PlatformBff.Models.Tenant.TenantInfo>(_jsonOptions);

        // Act
        var response = await client.PostAsync(
            $"/api/admin/tenant/{tenant!.Id}/users?userId=test-user-123&email=test@example.com&role=Admin",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }


    // Test authentication handler for bypassing auth in tests
    public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Options.IsAuthenticated)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test Admin"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-admin-user"),
                new System.Security.Claims.Claim("sub", "test-admin-user"),
                new System.Security.Claims.Claim("session_id", "test-session")
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public bool IsAuthenticated { get; set; } = false;
    }

    // Test session service implementation for testing
    private class TestSessionService : ISessionService
    {
        private readonly bool _isPlatformAdmin;
        private readonly Dictionary<string, string> _sessionData = new();
        private readonly Dictionary<string, TokenData> _tokens = new();

        public TestSessionService(bool isPlatformAdmin)
        {
            _isPlatformAdmin = isPlatformAdmin;

            // Use the old session key format that works in PlatformAdminAccessTests
            _sessionData[$"test-session:{nameof(PlatformBffSessionKeys.UserId)}"] = "test-admin-user";
            var tenantId = _isPlatformAdmin ? Guid.Parse("00000000-0000-0000-0000-000000000001").ToString() : Guid.NewGuid().ToString();
            _sessionData[$"test-session:{nameof(PlatformBffSessionKeys.SelectedTenantId)}"] = tenantId;

            _tokens["test-session"] = new TokenData
            {
                AccessToken = "test-token",
                RefreshToken = "test-refresh",
                IdToken = "test-id-token",
                TokenType = "Bearer"
            };
        }

        public Task StoreTokensAsync(string sessionId, TokenData tokens)
        {
            _tokens[sessionId] = tokens;
            return Task.CompletedTask;
        }

        public Task<TokenData?> GetTokensAsync(string sessionId)
        {
            _tokens.TryGetValue(sessionId, out var tokens);
            return Task.FromResult(tokens);
        }

        public Task RemoveSessionAsync(string sessionId)
        {
            var keysToRemove = _sessionData.Keys.Where(k => k.StartsWith(sessionId + ":"));
            foreach (var key in keysToRemove)
                _sessionData.Remove(key);

            _tokens.Remove(sessionId);
            return Task.CompletedTask;
        }

        public Task RemoveSessionDataAsync(string sessionId, string name)
        {
            _sessionData.Remove($"test-session:{sessionId}:{name}");
            return Task.CompletedTask;
        }

        public Task<TokenData?> RefreshTokensAsync(string sessionId, string refreshToken)
        {
            if (_tokens.ContainsKey(sessionId))
            {
                var newTokens = new TokenData
                {
                    AccessToken = "refreshed-token",
                    RefreshToken = "new-refresh-token",
                    IdToken = "new-id-token",
                    TokenType = "Bearer"
                };
                _tokens[sessionId] = newTokens;
                return Task.FromResult<TokenData?>(newTokens);
            }
            return Task.FromResult<TokenData?>(null);
        }

        public Task RevokeTokensAsync(string sessionId)
        {
            _tokens.Remove(sessionId);
            return Task.CompletedTask;
        }

        public Task<HasValueOrMissingResult<string>> GetSessionDataAsync(string sessionId, string name)
        {
            _sessionData.TryGetValue($"{sessionId}:{name}", out var sessionData);
            return Task.FromResult(sessionData != null
                ? HasValueOrMissingResult<string>.SetValue(sessionData)
                : HasValueOrMissingResult<string>.SetMissing());
        }

        public Task StoreSessionDataAsync(string sessionId, string name, string data, DateTimeOffset? expiresAt = null)
        {
            _sessionData[$"{sessionId}:{name}"] = data;
            return Task.CompletedTask;
        }
    }

    // Working TestTenantContext implementation from PlatformAdminAccessTests
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
    }
}