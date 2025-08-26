using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using PlatformBff.Models.Dev;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using PlatformBff.Data;
using StackExchange.Redis;
using Moq;

namespace PlatformBff.Tests.Controllers;

public class DevControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static int _testCounter = 0;

    public DevControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateDevClient()
    {
        var testId = System.Threading.Interlocked.Increment(ref _testCounter);
        var dbName = $"DevControllerTest_{testId}";
        
        return _factory.WithWebHostBuilder(builder =>
        {
            // Use Testing environment to avoid Redis connection
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override to simulate Development environment for DevelopmentOnlyAttribute
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development"
                });
            });
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
                
                // Override IHostEnvironment to report Development
                services.AddSingleton<IHostEnvironment>(new TestHostEnvironment 
                { 
                    EnvironmentName = "Development",
                    ApplicationName = "PlatformBff",
                    ContentRootPath = Directory.GetCurrentDirectory()
                });
            });
        }).CreateClient();
    }

    private HttpClient CreateProdClient()
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
                
                // Override IHostEnvironment to report Production
                services.AddSingleton<IHostEnvironment>(new TestHostEnvironment 
                { 
                    EnvironmentName = "Production",
                    ApplicationName = "PlatformBff",
                    ContentRootPath = Directory.GetCurrentDirectory()
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetHealthAll_InDevelopment_ReturnsHealthStatus()
    {
        // Arrange
        var client = CreateDevClient();

        // Act
        var response = await client.GetAsync("/dev/health/all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();
        result.Should().NotBeNull();
        result!.Overall.Should().NotBeNullOrEmpty();
        result.Services.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHealthAll_InProduction_ReturnsForbidden()
    {
        // Arrange
        var client = CreateProdClient();

        // Act
        var response = await client.GetAsync("/dev/health/all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_WithValidData_CreatesUser()
    {
        // Arrange
        var client = CreateDevClient();
        var request = new CreateTestUserRequest
        {
            Email = "test@example.local",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            Roles = new[] { "User" }
        };

        // Act
        var response = await client.PostAsJsonAsync("/dev/users/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreateTestUserResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task CreateUser_InProduction_ReturnsForbidden()
    {
        // Arrange
        var client = CreateProdClient();
        var request = new CreateTestUserRequest
        {
            Email = "test@example.local",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            Roles = new[] { "User" }
        };

        // Act
        var response = await client.PostAsJsonAsync("/dev/users/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignTenant_WithValidData_AssignsUserToTenant()
    {
        // Arrange
        var client = CreateDevClient();
        var request = new AssignTenantRequest
        {
            UserId = "test-user-id",
            Email = "test@example.local",
            TenantId = Guid.NewGuid(),
            Roles = new[] { "Admin" }
        };

        // Act
        var response = await client.PostAsJsonAsync("/dev/users/assign-tenant", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AssignTenantResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResetDatabase_WithValidTarget_ResetsDatabase()
    {
        // Arrange
        var client = CreateDevClient();
        var request = new ResetDatabaseRequest
        {
            Target = "platform",
            Seed = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/dev/database/reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResetDatabaseResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Seeded.Should().BeTrue();
    }

    [Fact]
    public async Task ResetDatabase_InProduction_ReturnsForbidden()
    {
        // Arrange
        var client = CreateProdClient();
        var request = new ResetDatabaseRequest
        {
            Target = "all",
            Seed = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/dev/database/reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VerifyConfig_InDevelopment_ReturnsConfiguration()
    {
        // Arrange
        var client = CreateDevClient();

        // Act
        var response = await client.GetAsync("/dev/config/verify");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConfigVerificationResponse>();
        result.Should().NotBeNull();
        result!.Environment.Should().Be("Development");
        result.Valid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyConfig_InProduction_ReturnsForbidden()
    {
        // Arrange
        var client = CreateProdClient();

        // Act
        var response = await client.GetAsync("/dev/config/verify");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AllDevEndpoints_WithDevelopmentOnlyAttribute_BlockedInProduction()
    {
        // Arrange
        var prodClient = CreateProdClient();
        var endpoints = new[]
        {
            "/dev/health/all",
            "/dev/config/verify"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await prodClient.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"Endpoint {endpoint} should be blocked in production");
        }
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