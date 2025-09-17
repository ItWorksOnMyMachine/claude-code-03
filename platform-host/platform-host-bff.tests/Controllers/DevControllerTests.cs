using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using PlatformBff.Data;
using StackExchange.Redis;
using Moq;
using PlatformShared.Services;

namespace PlatformBff.Tests.Controllers;

/// <summary>
/// Tests for DevController focusing on the DevelopmentOnlyAttribute behavior.
/// Note: These tests verify that development endpoints are properly restricted
/// in production environments. They do not test the actual functionality of
/// endpoints that require external services (auth service, etc).
/// </summary>
public class DevControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static int _testCounter = 0;

    public DevControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient(bool isDevelopment = true)
    {
        var testId = System.Threading.Interlocked.Increment(ref _testCounter);
        var dbName = $"DevControllerTest_{testId}";

        return _factory.WithWebHostBuilder(builder =>
        {
            // Use Testing environment to avoid Redis connection
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

                // Add mock Redis connection for DevController
                var mockRedis = new Mock<IConnectionMultiplexer>();
                var mockDatabase = new Mock<IDatabase>();
                mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                    .Returns(mockDatabase.Object);
                mockDatabase.Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
                    .ReturnsAsync(TimeSpan.FromMilliseconds(1));

                services.AddSingleton(mockRedis.Object);


                // Add required services for middleware
                services.AddDistributedMemoryCache();
                services.AddHttpContextAccessor();
                services.AddHttpClient();

                // Register shared services interfaces
                services.AddScoped<PlatformShared.Services.ITenantContext, PlatformShared.Services.TenantContext>();
                services.AddScoped<PlatformShared.Services.ISessionService, PlatformShared.Services.DistributedSessionService>();
                services.AddScoped<PlatformShared.Services.IEntitlementService, PlatformShared.Services.EntitlementService>();

                // Register BFF-specific session service for TokenRefreshMiddleware
                // Register BFF-specific tenant context for TenantContextMiddleware
                var mockTenantContext = new Mock<PlatformBff.Services.ITenantContext>();
                services.AddScoped<PlatformBff.Services.ITenantContext>(_ => mockTenantContext.Object);
                var mockSessionService = new Mock<PlatformBff.Services.ISessionService>();
                services.AddScoped<PlatformBff.Services.ISessionService>(_ => mockSessionService.Object);

                // Override IHostEnvironment to simulate Development or Production
                services.AddSingleton<IHostEnvironment>(new TestHostEnvironment
                {
                    EnvironmentName = isDevelopment ? "Development" : "Production",
                    ApplicationName = "PlatformBff",
                    ContentRootPath = Directory.GetCurrentDirectory()
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task DevEndpoints_InProduction_ReturnForbidden()
    {
        // Arrange
        var client = CreateTestClient(isDevelopment: false);
        var endpoints = new[]
        {
            "/dev/health/all",
            "/dev/config/verify"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"Endpoint {endpoint} should return 403 Forbidden in production");
        }
    }

    [Fact]
    public async Task DevEndpoints_InDevelopment_DoNotReturnForbidden()
    {
        // Arrange
        var client = CreateTestClient(isDevelopment: true);

        // Test an endpoint that doesn't require external services
        var endpoint = "/dev/config/verify";

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert - should NOT be forbidden (might be other status codes)
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            $"Endpoint {endpoint} should be accessible in development environment");
    }

    [Fact]
    public async Task PostEndpoints_InProduction_AreNotAccessible()
    {
        // Arrange
        var client = CreateTestClient(isDevelopment: false);

        // Test POST endpoints - in production, these should either return Forbidden
        // or fail validation (BadRequest) but never execute successfully
        var postEndpoints = new (string endpoint, object payload)[]
        {
            ("/dev/users/create", new { email = "test@test.com", password = "Test123!" }),
            ("/dev/users/assign-tenant", new { userId = "123", tenantId = Guid.NewGuid() }),
            ("/dev/database/reset", new { target = "platform", seed = true })
        };

        // Act & Assert
        foreach (var (endpoint, payload) in postEndpoints)
        {
            var response = await client.PostAsJsonAsync(endpoint, payload);

            // In production, dev endpoints should not succeed
            response.IsSuccessStatusCode.Should().BeFalse(
                $"POST endpoint {endpoint} should not be accessible in production");

            // They should return either Forbidden (blocked by attribute) 
            // or BadRequest (validation failed after attribute check)
            var acceptableStatuses = new[] { HttpStatusCode.Forbidden, HttpStatusCode.BadRequest };
            acceptableStatuses.Should().Contain(response.StatusCode,
                $"POST endpoint {endpoint} should return Forbidden or BadRequest in production");
        }
    }

    [Fact]
    public async Task DevelopmentOnlyAttribute_BlocksHttpMethods_InProduction()
    {
        // Arrange
        var client = CreateTestClient(isDevelopment: false);
        var testEndpoint = "/dev/health/all";

        // Act & Assert - Test different HTTP methods
        // GET should be blocked with Forbidden
        var getResponse = await client.GetAsync(testEndpoint);
        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "GET should be blocked");

        // Other HTTP methods might return MethodNotAllowed since they're not defined on the controller
        // but they still shouldn't succeed
        var postResponse = await client.PostAsJsonAsync(testEndpoint, new { });
        postResponse.IsSuccessStatusCode.Should().BeFalse("POST should not succeed");

        var putResponse = await client.PutAsJsonAsync(testEndpoint, new { });
        putResponse.IsSuccessStatusCode.Should().BeFalse("PUT should not succeed");

        var deleteResponse = await client.DeleteAsync(testEndpoint);
        deleteResponse.IsSuccessStatusCode.Should().BeFalse("DELETE should not succeed");
    }

    [Fact]
    public async Task ForbiddenResponse_IncludesErrorMessage_InProduction()
    {
        // Arrange
        var client = CreateTestClient(isDevelopment: false);

        // Act
        var response = await client.GetAsync("/dev/health/all");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        content.Should().Contain("development environment",
            "Response should indicate this is a development-only endpoint");
    }

    // Test helper class to override IHostEnvironment
    private class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "PlatformBff";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}