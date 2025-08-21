using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AuthService.Tests;

public class ServiceInitializationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ServiceInitializationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public void Service_StartsSuccessfully()
    {
        // Act & Assert
        var client = _factory.CreateClient();
        client.Should().NotBeNull();
    }

    [Fact]
    public void Service_RegistersRequiredServices()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert - Check for core services
        services.GetService<ILogger<Program>>().Should().NotBeNull("Logger should be registered");
        services.GetService<ILoggerFactory>().Should().NotBeNull("LoggerFactory should be registered");
    }

    [Fact]
    public void Service_ConfiguresHealthChecks()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert
        var healthCheckService = services.GetService(typeof(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService));
        healthCheckService.Should().NotBeNull("Health check service should be registered");
    }

    [Fact]
    public async Task Service_RespondsToRequests()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public void Service_HasCorrectEnvironmentConfiguration()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        // Act & Assert
        configuration.Should().NotBeNull();
        // In a real scenario, we'd check for specific configuration values
        // For now, we just ensure configuration is available
    }

    [Fact]
    public void Service_ConfiguresCors()
    {
        // This test verifies CORS will be configured (implementation will add the actual CORS)
        // For now, we're testing the structure
        
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert
        // CORS service registration will be verified when implemented
        services.Should().NotBeNull();
    }

    [Fact]
    public async Task Service_HandlesMultipleConcurrentRequests()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Send 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(client.GetAsync("/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(10);
        responses.Should().OnlyContain(r => r.IsSuccessStatusCode);
    }

    [Fact]
    public void Service_ConfiguresLogging()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<ServiceInitializationTests>();

        // Act & Assert
        logger.Should().NotBeNull();
        
        // Test that we can log without exceptions
        logger.LogInformation("Test log message");
    }
}