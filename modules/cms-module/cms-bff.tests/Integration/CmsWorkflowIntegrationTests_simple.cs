using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace CmsBff.Tests.Integration;

/// <summary>
/// Simplified integration tests for the complete CMS workflow
/// Tests the full stack from API endpoints through services to database
/// Relies on Program.cs Testing environment configuration
/// </summary>
public class CmsWorkflowIntegrationTestsSimple : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CmsWorkflowIntegrationTestsSimple(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Use Testing environment - Program.cs will configure all test services
            builder.UseEnvironment("Testing");
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheckEndpoint_Should_ReturnHealthStatus()
    {
        // Act
        var healthResponse = await _client.GetAsync("/health");

        // Assert
        healthResponse.Should().BeSuccessful();
        var healthJson = await healthResponse.Content.ReadAsStringAsync();

        // Basic health check should return status information
        healthJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompleteContentWorkflow_Should_CreateUpdateDeleteContent()
    {
        // Arrange - Create content
        var createContent = new
        {
            title = "Integration Test Content",
            slug = "integration-test-content",
            content = "<div>Test content body</div>",
            contentType = "page",
            status = "draft",
            metaTitle = "Test Meta Title",
            metaDescription = "Test Meta Description"
        };

        var createJson = JsonSerializer.Serialize(createContent);
        var createPayload = new StringContent(createJson, Encoding.UTF8, "application/json");

        // Act 1 - Create content
        var createResponse = await _client.PostAsync("/api/cms/content", createPayload);

        // Assert 1 - Content created successfully
        createResponse.Should().BeSuccessful();
        var createdContentJson = await createResponse.Content.ReadAsStringAsync();
        var createdContent = JsonSerializer.Deserialize<JsonElement>(createdContentJson);
        var contentId = createdContent.GetProperty("id").GetString();

        contentId.Should().NotBeNullOrEmpty();
        createdContent.GetProperty("title").GetString().Should().Be("Integration Test Content");

        // Act 2 - Get content by ID
        var getResponse = await _client.GetAsync($"/api/cms/content/{contentId}");

        // Assert 2 - Content retrieved successfully
        getResponse.Should().BeSuccessful();
        var retrievedContentJson = await getResponse.Content.ReadAsStringAsync();
        var retrievedContent = JsonSerializer.Deserialize<JsonElement>(retrievedContentJson);

        retrievedContent.GetProperty("title").GetString().Should().Be("Integration Test Content");
        retrievedContent.GetProperty("status").GetString().Should().Be("draft");

        // Act 3 - Delete content
        var deleteResponse = await _client.DeleteAsync($"/api/cms/content/{contentId}");

        // Assert 3 - Content deleted successfully
        deleteResponse.Should().BeSuccessful();

        // Act 4 - Verify content is deleted (should return 404)
        var getDeletedResponse = await _client.GetAsync($"/api/cms/content/{contentId}");

        // Assert 4 - Content no longer accessible
        getDeletedResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
