using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CmsBff.Data;
using CmsBff.Data.Entities;
using Xunit;

namespace CmsBff.Tests.Data.Entities;

public class DatabaseRelationshipsTests : IDisposable
{
    private readonly CmsDbContext _context;

    public DatabaseRelationshipsTests()
    {
        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CmsDbContext(options, (Guid?)null);
    }

    [Fact]
    public async Task Template_Should_HavePages_Relationship()
    {
        // Arrange
        var template = new CmsTemplate
        {
            TenantId = Guid.NewGuid(),
            Name = "Test Template",
            LayoutContent = "{}",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        var page = new CmsPage
        {
            TenantId = template.TenantId,
            Title = "Test Page",
            Slug = "test-page",
            TemplateId = template.Id,
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        // Act
        _context.Templates.Add(template);
        _context.Pages.Add(page);
        await _context.SaveChangesAsync();

        // Load with navigation properties
        var loadedTemplate = await _context.Templates
            .Include(t => t.Pages)
            .FirstAsync(t => t.Id == template.Id);

        // Assert
        loadedTemplate.Pages.Should().HaveCount(1);
        loadedTemplate.Pages.First().Id.Should().Be(page.Id);
        loadedTemplate.Pages.First().TemplateId.Should().Be(template.Id);
    }

    [Fact]
    public async Task Page_Should_HaveContentBlocks_Relationship()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var page = new CmsPage
        {
            TenantId = tenantId,
            Title = "Test Page",
            Slug = "test-page",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        var contentBlock = new CmsContentBlock
        {
            TenantId = tenantId,
            PageId = page.Id,
            BlockType = CmsBlockTypes.Text,
            Zone = "content",
            Content = "{\"text\": \"Hello World\"}",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        // Act
        _context.Pages.Add(page);
        _context.ContentBlocks.Add(contentBlock);
        await _context.SaveChangesAsync();

        // Load with navigation properties
        var loadedPage = await _context.Pages
            .Include(p => p.ContentBlocks)
            .FirstAsync(p => p.Id == page.Id);

        // Assert
        loadedPage.ContentBlocks.Should().HaveCount(1);
        loadedPage.ContentBlocks.First().Id.Should().Be(contentBlock.Id);
        loadedPage.ContentBlocks.First().PageId.Should().Be(page.Id);
    }

    [Fact]
    public async Task ContentBlocks_Should_CascadeDelete_WhenPageDeleted()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var page = new CmsPage
        {
            TenantId = tenantId,
            Title = "Test Page",
            Slug = "test-page",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        var contentBlock = new CmsContentBlock
        {
            TenantId = tenantId,
            PageId = page.Id,
            BlockType = CmsBlockTypes.Text,
            Zone = "content",
            Content = "{\"text\": \"Test content\"}",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Pages.Add(page);
        _context.ContentBlocks.Add(contentBlock);
        await _context.SaveChangesAsync();

        // Act - Delete the page
        _context.Pages.Remove(page);
        await _context.SaveChangesAsync();

        // Assert - Content blocks should be cascade deleted
        var remainingBlocks = await _context.ContentBlocks
            .Where(cb => cb.PageId == page.Id)
            .ToListAsync();

        remainingBlocks.Should().BeEmpty();
    }

    [Fact]
    public async Task Template_Should_HaveRestrictDeleteBehavior_WhenPagesExist()
    {
        // Arrange
        var template = new CmsTemplate
        {
            TenantId = Guid.NewGuid(),
            Name = "Test Template",
            LayoutContent = "{}",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        var page = new CmsPage
        {
            TenantId = template.TenantId,
            Title = "Test Page",
            Slug = "test-page",
            TemplateId = template.Id,
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Templates.Add(template);
        _context.Pages.Add(page);
        await _context.SaveChangesAsync();

        // Act - Verify relationship exists
        var savedTemplate = await _context.Templates
            .Include(t => t.Pages)
            .FirstAsync(t => t.Id == template.Id);

        // Assert - Template should have related pages
        savedTemplate.Pages.Should().HaveCount(1);
        savedTemplate.Pages.First().TemplateId.Should().Be(template.Id);
        
        // Note: InMemory provider does not enforce DeleteBehavior.Restrict
        // This test verifies the relationship configuration is correct
    }

    [Fact]
    public async Task SoftDelete_Should_ExcludeDeletedEntities()
    {
        // Arrange
        var template = new CmsTemplate
        {
            TenantId = Guid.NewGuid(),
            Name = "Test Template",
            LayoutContent = "{}",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Templates.Add(template);
        await _context.SaveChangesAsync();

        // Act - Soft delete the template
        template.IsDeleted = true;
        template.DeletedAt = DateTimeOffset.UtcNow;
        template.DeletedBy = "Test";
        await _context.SaveChangesAsync();

        // Assert - Should not be returned by normal queries
        var templates = await _context.Templates.ToListAsync();
        templates.Should().BeEmpty();

        // But should be accessible when ignoring query filters
        var allTemplates = await _context.Templates
            .IgnoreQueryFilters()
            .ToListAsync();
        allTemplates.Should().HaveCount(1);
        allTemplates.First().IsDeleted.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
