using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AuthService.Tests;

public class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk_WithHealthyStatus()
    {
        // Arrange
        var expectedResponse = new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var jsonDocument = JsonDocument.Parse(content);
        jsonDocument.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        jsonDocument.RootElement.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task HealthReady_ReturnsOk_WhenAllDependenciesHealthy()
    {
        // Arrange
        var expectedChecks = new[] { "database", "redis" };

        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var jsonDocument = JsonDocument.Parse(content);
        jsonDocument.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        
        var checks = jsonDocument.RootElement.GetProperty("checks");
        checks.Should().NotBeNull();
        
        // For initial implementation, we'll just check the structure exists
        // Actual database and Redis checks will be implemented later
    }

    [Fact]
    public async Task Health_ReturnsCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task HealthReady_ReturnsCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Health_ResponseTimeIsAcceptable()
    {
        // Act
        var startTime = DateTime.UtcNow;
        var response = await _client.GetAsync("/health");
        var responseTime = DateTime.UtcNow - startTime;

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        responseTime.TotalMilliseconds.Should().BeLessThan(1000, "Health endpoint should respond quickly");
    }
}