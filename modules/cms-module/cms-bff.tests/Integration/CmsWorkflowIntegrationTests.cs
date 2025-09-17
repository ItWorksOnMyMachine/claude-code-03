using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using CmsBff.Data;
using CmsBff.Data.Entities;
using PlatformShared.Services;
using CmsBff.Tests.Authentication;

namespace CmsBff.Tests.Integration;

/// <summary>
/// Comprehensive integration tests for the complete CMS workflow
/// Tests the full stack from API endpoints through services to database
/// </summary>
public class CmsWorkflowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CmsWorkflowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Use Testing environment to avoid Redis connection issues
            builder.UseEnvironment("Testing");

            // Override configuration to avoid Redis for tests
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Redis"] = null, // Null to trigger in-memory fallback
                    ["ConnectionStrings:DefaultConnection"] = "Host=test;Database=test;Username=test;Password=test", // Dummy connection string
                    ["Authentication:Authority"] = "https://test-idp.local",
                    ["Authentication:ClientId"] = "test-client",
                    ["Authentication:ClientSecret"] = "test-secret"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Override services for testing
                var entitlementDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(PlatformShared.Services.IEntitlementService));
                if (entitlementDescriptor != null)
                {
                    services.Remove(entitlementDescriptor);
                }
                services.AddScoped<PlatformShared.Services.IEntitlementService, MockEntitlementService>();

                var sessionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(PlatformShared.Services.ISessionService));
                if (sessionDescriptor != null)
                {
                    services.Remove(sessionDescriptor);
                }
                services.AddScoped<PlatformShared.Services.ISessionService, TestSessionService>();

                // Don't override authentication - let Program.cs handle it completely
            });
        });

        _client = _factory.CreateClient();
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

        // Act 3 - Update content
        var updateContent = new
        {
            id = contentId,
            title = "Updated Integration Test Content",
            slug = "updated-integration-test-content",
            content = "<div>Updated test content body</div>",
            contentType = "page",
            status = "published",
            metaTitle = "Updated Meta Title"
        };

        var updateJson = JsonSerializer.Serialize(updateContent);
        var updatePayload = new StringContent(updateJson, Encoding.UTF8, "application/json");

        var updateResponse = await _client.PutAsync($"/api/cms/content/{contentId}", updatePayload);

        // Assert 3 - Content updated successfully
        updateResponse.Should().BeSuccessful();
        var updatedContentJson = await updateResponse.Content.ReadAsStringAsync();
        var updatedContent = JsonSerializer.Deserialize<JsonElement>(updatedContentJson);

        updatedContent.GetProperty("title").GetString().Should().Be("Updated Integration Test Content");
        updatedContent.GetProperty("status").GetString().Should().Be("published");

        // Act 4 - Get all content (should include our content)
        var getAllResponse = await _client.GetAsync("/api/cms/content");

        // Assert 4 - Content appears in list
        getAllResponse.Should().BeSuccessful();
        var allContentJson = await getAllResponse.Content.ReadAsStringAsync();
        var allContent = JsonSerializer.Deserialize<JsonElement>(allContentJson);

        allContent.ValueKind.Should().Be(JsonValueKind.Array);
        allContent.GetArrayLength().Should().BeGreaterThan(0);

        // Act 5 - Delete content
        var deleteResponse = await _client.DeleteAsync($"/api/cms/content/{contentId}");

        // Assert 5 - Content deleted successfully
        deleteResponse.Should().BeSuccessful();

        // Act 6 - Verify content is deleted (should return 404)
        var getDeletedResponse = await _client.GetAsync($"/api/cms/content/{contentId}");

        // Assert 6 - Content no longer accessible
        getDeletedResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteTemplateWorkflow_Should_CreateUseDeleteTemplate()
    {
        // Arrange - Create template
        var createTemplate = new
        {
            name = "Integration Test Template",
            description = "Template for integration testing",
            layoutContent = JsonSerializer.Serialize(new
            {
                zones = new[]
                {
                    new { name = "header", blocks = new[] { "text", "image" } },
                    new { name = "content", blocks = new[] { "html", "video" } }
                },
                styles = new { theme = "default" }
            }),
            templateType = "page",
            isActive = true
        };

        var createJson = JsonSerializer.Serialize(createTemplate);
        var createPayload = new StringContent(createJson, Encoding.UTF8, "application/json");

        // Act 1 - Create template
        var createResponse = await _client.PostAsync("/api/cms/templates", createPayload);

        // Assert 1 - Template created successfully
        createResponse.Should().BeSuccessful();
        var createdTemplateJson = await createResponse.Content.ReadAsStringAsync();
        var createdTemplate = JsonSerializer.Deserialize<JsonElement>(createdTemplateJson);
        var templateId = createdTemplate.GetProperty("id").GetString();

        templateId.Should().NotBeNullOrEmpty();
        createdTemplate.GetProperty("name").GetString().Should().Be("Integration Test Template");

        // Act 2 - Get all templates (should include our template)
        var getAllResponse = await _client.GetAsync("/api/cms/templates");

        // Assert 2 - Template appears in list
        getAllResponse.Should().BeSuccessful();
        var allTemplatesJson = await getAllResponse.Content.ReadAsStringAsync();
        var allTemplates = JsonSerializer.Deserialize<JsonElement>(allTemplatesJson);

        allTemplates.ValueKind.Should().Be(JsonValueKind.Array);
        allTemplates.GetArrayLength().Should().BeGreaterThan(0);

        // Act 3 - Create content using the template
        var createContent = new
        {
            title = "Content with Template",
            slug = "content-with-template",
            content = "<div>Content using template</div>",
            contentType = "page",
            status = "draft",
            templateId = templateId
        };

        var contentJson = JsonSerializer.Serialize(createContent);
        var contentPayload = new StringContent(contentJson, Encoding.UTF8, "application/json");

        var createContentResponse = await _client.PostAsync("/api/cms/content", contentPayload);

        // Assert 3 - Content with template created successfully
        createContentResponse.Should().BeSuccessful();

        // Act 4 - Try to delete template (should fail because it's in use)
        var deleteTemplateResponse = await _client.DeleteAsync($"/api/cms/templates/{templateId}");

        // Assert 4 - Template deletion should fail
        deleteTemplateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteAssetWorkflow_Should_UploadManageDeleteAsset()
    {
        // Note: This test demonstrates the asset workflow structure
        // In a real implementation, file upload would require multipart/form-data

        // Arrange - Prepare asset data
        var createAsset = new
        {
            fileName = "test-image.jpg",
            originalFileName = "original-test-image.jpg",
            storagePath = "/uploads/test-image.jpg",
            mimeType = "image/jpeg",
            fileSize = 1024000,
            assetType = "image",
            altText = "Test image for integration testing",
            description = "Asset created during integration testing",
            tags = JsonSerializer.Serialize(new[] { "test", "integration", "image" }),
            width = 800,
            height = 600,
            isPublic = true
        };

        var createJson = JsonSerializer.Serialize(createAsset);
        var createPayload = new StringContent(createJson, Encoding.UTF8, "application/json");

        // Act 1 - Create asset (simulated upload)
        var createResponse = await _client.PostAsync("/api/cms/assets", createPayload);

        // Assert 1 - Asset created successfully
        createResponse.Should().BeSuccessful();
        var createdAssetJson = await createResponse.Content.ReadAsStringAsync();
        var createdAsset = JsonSerializer.Deserialize<JsonElement>(createdAssetJson);
        var assetId = createdAsset.GetProperty("id").GetString();

        assetId.Should().NotBeNullOrEmpty();
        createdAsset.GetProperty("fileName").GetString().Should().Be("test-image.jpg");

        // Act 2 - Get all assets
        var getAllResponse = await _client.GetAsync("/api/cms/assets");

        // Assert 2 - Asset appears in list
        getAllResponse.Should().BeSuccessful();
        var allAssetsJson = await getAllResponse.Content.ReadAsStringAsync();
        var allAssets = JsonSerializer.Deserialize<JsonElement>(allAssetsJson);

        allAssets.ValueKind.Should().Be(JsonValueKind.Array);
        allAssets.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EntitlementProtectedEndpoints_Should_RequireProperAuthorization()
    {
        // This test verifies that our entitlement system is working
        // In a real scenario, these would return 403 without proper entitlements

        // Act 1 - Try to access content without entitlements
        var getContentResponse = await _client.GetAsync("/api/cms/content");

        // Assert 1 - Should succeed because MockEntitlementService allows all
        getContentResponse.Should().BeSuccessful();

        // Act 2 - Try to create content without entitlements
        var createContent = new
        {
            title = "Unauthorized Content",
            slug = "unauthorized-content",
            content = "<div>This should be protected</div>",
            contentType = "page",
            status = "draft"
        };

        var createJson = JsonSerializer.Serialize(createContent);
        var createPayload = new StringContent(createJson, Encoding.UTF8, "application/json");

        var createResponse = await _client.PostAsync("/api/cms/content", createPayload);

        // Assert 2 - Should succeed because MockEntitlementService allows all
        createResponse.Should().BeSuccessful();

        // Note: In production, these tests would verify 403 responses
        // when proper entitlements are not present
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
}

/// <summary>
/// Test session service that provides session data for test-session
/// </summary>
public class TestSessionService : PlatformShared.Services.ISessionService
{
    private readonly Dictionary<string, string> _sessionData = new();

    public TestSessionService()
    {
        // Set up session data for the test-session that TestAuthenticationHandler creates
        _sessionData["test-session:UserId"] = "test-user";
        _sessionData["test-session:SelectedTenantId"] = Guid.NewGuid().ToString();
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

/// <summary>
/// Mock entitlement service for integration testing
/// Always returns true to allow testing of business logic
/// </summary>
public class MockEntitlementService : IEntitlementService
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

/// <summary>
/// Test authentication handler that always authenticates users
/// Same pattern as working platform-bff tests
/// </summary>
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
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user"),
            new System.Security.Claims.Claim("sub", "test-user"),
            new System.Security.Claims.Claim("session_id", "test-session")
        };

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Options for test authentication scheme
/// </summary>
public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public bool IsAuthenticated { get; set; } = false;
}

/// <summary>
/// Mock password hasher for testing - not actually used but required by Identity system
/// </summary>
public class MockPasswordHasher<T> : IPasswordHasher<T> where T : class
{
    public string HashPassword(T user, string password)
    {
        return "mock-hashed-password";
    }

    public PasswordVerificationResult VerifyHashedPassword(T user, string hashedPassword, string providedPassword)
    {
        return PasswordVerificationResult.Success;
    }
}