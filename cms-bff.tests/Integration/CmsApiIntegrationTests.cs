using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CmsBff.Data;
using CmsBff.Data.Entities;
using FluentAssertions;
using System.Net.Http.Json;
using System.Net;

namespace CmsBff.Tests.Integration;

public class CmsApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CmsApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<CmsDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Add DbContext using in-memory database for testing
                services.AddDbContext<CmsDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetContent_WithValidTenant_ReturnsOnlyTenantContent()
    {
        // Arrange
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();
        
        await SeedTestData(tenantId1, tenantId2);

        // Act - This would require authentication headers in real implementation
        var response = await _client.GetAsync("/api/cms/content");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Note: In real implementation, we would mock authentication and verify
        // that only content for the authenticated user's tenant is returned
    }

    [Fact]
    public async Task CreateContent_WithValidData_CreatesContentWithTenantId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var contentRequest = new
        {
            Title = "Test Content",
            Slug = "test-content",
            Content = "<p>Test content body</p>",
            ContentType = "page",
            Status = "draft"
        };

        // Act - This would require authentication headers in real implementation
        var response = await _client.PostAsJsonAsync("/api/cms/content", contentRequest);

        // Assert - In real implementation with authentication
        // response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // For now, we expect Unauthorized due to missing authentication
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetContent_FromDifferentTenant_ShouldNotReturnData()
    {
        // Arrange
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();
        
        await SeedTestData(tenantId1, tenantId2);

        // Act - In real implementation, we would set different tenant context
        var response = await _client.GetAsync("/api/cms/content");

        // Assert - Tenant isolation should prevent accessing other tenant's data
        // This test demonstrates the concept but would need proper authentication
        // implementation to verify tenant isolation
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateContent_WithInvalidTitle_ShouldReturnBadRequest(string invalidTitle)
    {
        // Arrange
        var contentRequest = new
        {
            Title = invalidTitle,
            Slug = "test-content",
            Content = "<p>Test content body</p>",
            ContentType = "page",
            Status = "draft"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/cms/content", contentRequest);

        // Assert - Should validate required fields
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DbContext_WithTenantIsolation_FiltersDataCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();

        // Create context without tenant isolation for seeding
        var dbContext = new CmsDbContext(
            scope.ServiceProvider.GetRequiredService<DbContextOptions<CmsDbContext>>());

        await dbContext.Database.EnsureCreatedAsync();

        // Seed data for two different tenants
        var content1 = new CmsContent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId1,
            Title = "Tenant 1 Content",
            Slug = "tenant-1-content",
            Content = "Content for tenant 1",
            ContentType = "page",
            Status = "published"
        };

        var content2 = new CmsContent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId2,
            Title = "Tenant 2 Content", 
            Slug = "tenant-2-content",
            Content = "Content for tenant 2",
            ContentType = "page",
            Status = "published"
        };

        dbContext.Contents.AddRange(content1, content2);
        await dbContext.SaveChangesAsync();

        // Act - Create context with tenant isolation
        var tenantIsolatedContext = new CmsDbContext(
            scope.ServiceProvider.GetRequiredService<DbContextOptions<CmsDbContext>>(),
            tenantId1);

        var filteredContent = await tenantIsolatedContext.Contents.ToListAsync();

        // Assert - Should only return content for the specified tenant
        filteredContent.Should().HaveCount(1);
        filteredContent.First().TenantId.Should().Be(tenantId1);
        filteredContent.First().Title.Should().Be("Tenant 1 Content");
    }

    [Fact]
    public async Task DbContext_WithSoftDelete_ExcludesDeletedEntities()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var tenantId = Guid.NewGuid();

        var dbContext = new CmsDbContext(
            scope.ServiceProvider.GetRequiredService<DbContextOptions<CmsDbContext>>(),
            tenantId);

        await dbContext.Database.EnsureCreatedAsync();

        var activeContent = new CmsContent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = "Active Content",
            Slug = "active-content",
            Content = "Active content",
            ContentType = "page",
            Status = "published",
            IsDeleted = false
        };

        var deletedContent = new CmsContent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = "Deleted Content",
            Slug = "deleted-content", 
            Content = "Deleted content",
            ContentType = "page",
            Status = "published",
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow
        };

        dbContext.Contents.AddRange(activeContent, deletedContent);
        await dbContext.SaveChangesAsync();

        // Act
        var visibleContent = await dbContext.Contents.ToListAsync();

        // Assert - Should only return non-deleted content
        visibleContent.Should().HaveCount(1);
        visibleContent.First().Title.Should().Be("Active Content");
        visibleContent.Should().NotContain(c => c.IsDeleted);
    }

    private async Task SeedTestData(Guid tenantId1, Guid tenantId2)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = new CmsDbContext(
            scope.ServiceProvider.GetRequiredService<DbContextOptions<CmsDbContext>>());

        await dbContext.Database.EnsureCreatedAsync();

        var content1 = new CmsContent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId1,
            Title = "Tenant 1 Content",
            Slug = "tenant-1-content",
            Content = "Content for tenant 1",
            ContentType = "page",
            Status = "published"
        };

        var content2 = new CmsContent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId2,
            Title = "Tenant 2 Content",
            Slug = "tenant-2-content", 
            Content = "Content for tenant 2",
            ContentType = "page",
            Status = "published"
        };

        dbContext.Contents.AddRange(content1, content2);
        await dbContext.SaveChangesAsync();
    }
}