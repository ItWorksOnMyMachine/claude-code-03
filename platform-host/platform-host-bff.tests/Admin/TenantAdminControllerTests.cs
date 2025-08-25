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
using PlatformBff.Tests.Authentication;
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

                // Mock session service
                var sessionService = new TestSessionService(isPlatformAdmin);
                services.AddSingleton<ISessionService>(sessionService);
                
                // Mock tenant service for platform admin check
                var tenantServiceMock = new Mock<ITenantService>();
                tenantServiceMock.Setup(x => x.IsPlatformAdminAsync(It.IsAny<string>()))
                    .ReturnsAsync(isPlatformAdmin);
                services.AddScoped<ITenantService>(_ => tenantServiceMock.Object);
                
                // Add test authentication handler to bypass auth
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
        tenant!.Name.Should().Be("Test Tenant");
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

    [Fact]
    public async Task ImpersonateTenant_WithValidId_StartsImpersonation()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        // Create a tenant first
        var createDto = new CreateTenantDto
        {
            Name = "Tenant To Impersonate",
            Description = "Test Description"
        };
        var createResponse = await client.PostAsJsonAsync("/api/admin/tenants", createDto);
        var tenant = await createResponse.Content.ReadFromJsonAsync<PlatformBff.Models.Tenant.TenantInfo>(_jsonOptions);

        // Act
        var response = await client.PostAsync($"/api/admin/tenant/{tenant!.Id}/impersonate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Now impersonating tenant");
    }

    [Fact]
    public async Task StopImpersonation_WhenImpersonating_StopsSuccessfully()
    {
        // Arrange
        var factory = CreateFactory(isPlatformAdmin: true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "platform.session=test-session");

        // Start impersonation first
        var createDto = new CreateTenantDto
        {
            Name = "Tenant To Stop Impersonating",
            Description = "Test Description"
        };
        var createResponse = await client.PostAsJsonAsync("/api/admin/tenants", createDto);
        var tenant = await createResponse.Content.ReadFromJsonAsync<PlatformBff.Models.Tenant.TenantInfo>(_jsonOptions);
        await client.PostAsync($"/api/admin/tenant/{tenant!.Id}/impersonate", null);

        // Act
        var response = await client.PostAsync("/api/admin/tenant/stop-impersonation", null);

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
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test Admin"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-admin-user"),
                new System.Security.Claims.Claim("sub", "test-admin-user")
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions { }

    // Test session service implementation for testing
    private class TestSessionService : ISessionService
    {
        private readonly bool _isPlatformAdmin;
        private readonly Dictionary<string, SessionData> _sessions = new();
        private readonly Dictionary<string, TokenData> _tokens = new();

        public TestSessionService(bool isPlatformAdmin)
        {
            _isPlatformAdmin = isPlatformAdmin;
            
            // Create test session
            _sessions["test-session"] = new SessionData
            {
                SessionId = "test-session",
                UserId = "test-admin-user",
                Email = "admin@platform.com",
                IsPlatformAdmin = _isPlatformAdmin,
                SelectedTenantId = _isPlatformAdmin ? Guid.Parse("00000000-0000-0000-0000-000000000001") : null,
                SelectedTenantName = _isPlatformAdmin ? "Platform" : null,
                IsImpersonating = false
            };
            
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

        public Task<bool> IsSessionValidAsync(string sessionId)
        {
            return Task.FromResult(_sessions.ContainsKey(sessionId));
        }

        public Task ExtendSessionAsync(string sessionId, TimeSpan extension)
        {
            // For testing, we don't need to track expiry
            return Task.CompletedTask;
        }

        public Task RemoveSessionAsync(string sessionId)
        {
            _sessions.Remove(sessionId);
            _tokens.Remove(sessionId);
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

        public Task StoreSessionDataAsync(string sessionId, SessionData sessionData)
        {
            _sessions[sessionId] = sessionData;
            return Task.CompletedTask;
        }

        public Task<SessionData?> GetSessionDataAsync(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        public Task UpdateSessionDataAsync(string sessionId, SessionData sessionData)
        {
            _sessions[sessionId] = sessionData;
            return Task.CompletedTask;
        }
    }
}