using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CmsBff.Data;
using CmsBff.Data.Entities;
using CmsBff.Services;

namespace CmsBff.Tests.Services;

public class CmsContentServiceTests : IDisposable
{
    private readonly CmsDbContext _context;
    private readonly CmsContentService _service;

    public CmsContentServiceTests()
    {
        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CmsDbContext(options);
        _service = new CmsContentService(_context);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllContent_OrderedByUpdatedAt()
    {
        // Arrange
        var content1 = new CmsContent 
        { 
            Title = "Content 1", 
            Slug = "content-1", 
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        var content2 = new CmsContent 
        { 
            Title = "Content 2", 
            Slug = "content-2", 
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Contents.AddRange(content1, content2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().Title.Should().Be("Content 2"); // Most recently updated first
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsContent()
    {
        // Arrange
        var content = new CmsContent 
        { 
            Title = "Test Content", 
            Slug = "test-content",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Contents.Add(content);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(content.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Content");
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySlugAsync_WithValidSlug_ReturnsContent()
    {
        // Arrange
        var content = new CmsContent 
        { 
            Title = "Test Content", 
            Slug = "test-content",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Contents.Add(content);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBySlugAsync("test-content");

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Content");
    }

    [Fact]
    public async Task CreateAsync_WithValidContent_CreatesAndReturnsContent()
    {
        // Arrange
        var content = new CmsContent 
        { 
            Title = "New Content", 
            Slug = "new-content",
            Content = "Test content body",
            ContentType = "Article",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        // Act
        var result = await _service.CreateAsync(content);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var savedContent = await _context.Contents.FindAsync(result.Id);
        savedContent.Should().NotBeNull();
        savedContent!.Title.Should().Be("New Content");
    }

    [Fact]
    public async Task UpdateAsync_WithValidId_UpdatesAndReturnsContent()
    {
        // Arrange
        var content = new CmsContent 
        { 
            Title = "Original Title", 
            Slug = "original-slug",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Contents.Add(content);
        await _context.SaveChangesAsync();

        var updateContent = new CmsContent 
        { 
            Title = "Updated Title", 
            Slug = "updated-slug",
            Content = "Updated content",
            ContentType = "Page",
            Status = "Published",
            UpdatedBy = "UpdatedUser"
        };

        // Act
        var result = await _service.UpdateAsync(content.Id, updateContent);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Slug.Should().Be("updated-slug");
        result.Content.Should().Be("Updated content");
        result.UpdatedBy.Should().Be("UpdatedUser");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var updateContent = new CmsContent { Title = "Updated Title" };

        // Act
        var result = await _service.UpdateAsync(999, updateContent);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesContentAndReturnsTrue()
    {
        // Arrange
        var content = new CmsContent 
        { 
            Title = "Content to Delete", 
            Slug = "content-to-delete",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Contents.Add(content);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(content.Id);

        // Assert
        result.Should().BeTrue();
        var deletedContent = await _context.Contents.FindAsync(content.Id);
        deletedContent.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsContentWithSpecifiedStatus()
    {
        // Arrange
        var publishedContent = new CmsContent 
        { 
            Title = "Published Content", 
            Slug = "published-content", 
            Status = "Published",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        var draftContent = new CmsContent 
        { 
            Title = "Draft Content", 
            Slug = "draft-content", 
            Status = "Draft",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Contents.AddRange(publishedContent, draftContent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByStatusAsync("Published");

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Published Content");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}