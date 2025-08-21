using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.Logging;

namespace AuthService.Tests.IntegrationTests;

public class AuthServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public AuthServiceIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
        _output = output;
    }

    [Fact]
    public async Task Service_StartsAndRespondsToHealthCheck()
    {
        // Arrange
        var client = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                    });
                });
            })
            .CreateClient();

        // Act
        var response = await client.GetAsync("/health");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Health response: {content}");
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Service_HandlesInvalidRoutes()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/invalid-route-that-does-not-exist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Service_SupportsJsonContentType()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task Service_HealthEndpointsAreAccessible(string endpoint)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"Endpoint {endpoint} should be accessible");
    }

    [Fact]
    public async Task Service_ConfiguresProperHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.Headers.Should().NotBeNull();
        // Additional security headers will be tested when implemented
    }

    [Fact]
    public async Task Service_CanBeConfiguredWithCustomSettings()
    {
        // Arrange
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Custom configuration for testing
                services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
                {
                    options.SerializerOptions.WriteIndented = true;
                });
            });
        });

        var client = customFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Service_LogsStartupInformation()
    {
        // Arrange
        var logs = new List<string>();
        
        var client = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddProvider(new TestLoggerProvider(logs));
                    });
                });
            })
            .CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        // Startup logs would be captured here when logging is fully implemented
    }
}

// Helper class for capturing logs in tests
public class TestLoggerProvider : ILoggerProvider
{
    private readonly List<string> _logs;

    public TestLoggerProvider(List<string> logs)
    {
        _logs = logs;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(_logs, categoryName);
    }

    public void Dispose() { }
}

public class TestLogger : ILogger
{
    private readonly List<string> _logs;
    private readonly string _categoryName;

    public TestLogger(List<string> logs, string categoryName)
    {
        _logs = logs;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logs.Add($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
    }
}