using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CmsBff.Data;
using CmsBff.Data.Entities;
using CmsBff.Services;

namespace CmsBff.Tests.Services;

public class CmsTemplateServiceTests : IDisposable
{
    private readonly CmsDbContext _context;
    private readonly CmsTemplateService _service;

    public CmsTemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CmsDbContext(options);
        _service = new CmsTemplateService(_context);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTemplates_OrderedByName()
    {
        // Arrange
        var template1 = new CmsTemplate 
        { 
            Name = "Z Template", 
            TemplateType = "Article",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        var template2 = new CmsTemplate 
        { 
            Name = "A Template", 
            TemplateType = "Page",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Templates.AddRange(template1, template2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("A Template"); // Alphabetically first
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsTemplateWithContents()
    {
        // Arrange
        var template = new CmsTemplate 
        { 
            Name = "Test Template", 
            TemplateType = "Article",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();

        var content = new CmsContent 
        { 
            Title = "Content using template", 
            Slug = "content-with-template", 
            TemplateId = template.Id,
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Contents.Add(content);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(template.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Template");
        result.Contents.Should().HaveCount(1);
        result.Contents.First().Title.Should().Be("Content using template");
    }

    [Fact]
    public async Task CreateAsync_WithValidTemplate_CreatesAndReturnsTemplate()
    {
        // Arrange
        var template = new CmsTemplate 
        { 
            Name = "New Template", 
            Description = "Test template description",
            TemplateContent = "<html><body>{{content}}</body></html>",
            TemplateType = "Page",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        // Act
        var result = await _service.CreateAsync(template);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var savedTemplate = await _context.Templates.FindAsync(result.Id);
        savedTemplate.Should().NotBeNull();
        savedTemplate!.Name.Should().Be("New Template");
    }

    [Fact]
    public async Task UpdateAsync_WithValidId_UpdatesAndReturnsTemplate()
    {
        // Arrange
        var template = new CmsTemplate 
        { 
            Name = "Original Template", 
            TemplateType = "Article",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();

        var updateTemplate = new CmsTemplate 
        { 
            Name = "Updated Template", 
            Description = "Updated description",
            TemplateContent = "<div>{{content}}</div>",
            TemplateType = "Page",
            IsActive = false,
            UpdatedBy = "UpdatedUser"
        };

        // Act
        var result = await _service.UpdateAsync(template.Id, updateTemplate);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Template");
        result.Description.Should().Be("Updated description");
        result.TemplateType.Should().Be("Page");
        result.IsActive.Should().BeFalse();
        result.UpdatedBy.Should().Be("UpdatedUser");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DeleteAsync_WithTemplateNotInUse_DeletesTemplateAndReturnsTrue()
    {
        // Arrange
        var template = new CmsTemplate 
        { 
            Name = "Template to Delete", 
            TemplateType = "Article",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(template.Id);

        // Assert
        result.Should().BeTrue();
        var deletedTemplate = await _context.Templates.FindAsync(template.Id);
        deletedTemplate.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithTemplateInUse_ReturnsFalseAndDoesNotDelete()
    {
        // Arrange
        var template = new CmsTemplate 
        { 
            Name = "Template in Use", 
            TemplateType = "Article",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();

        var content = new CmsContent 
        { 
            Title = "Content using template", 
            Slug = "content-with-template", 
            TemplateId = template.Id,
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Contents.Add(content);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(template.Id);

        // Assert
        result.Should().BeFalse();
        var templateStillExists = await _context.Templates.FindAsync(template.Id);
        templateStillExists.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActiveTemplatesAsync_ReturnsOnlyActiveTemplates()
    {
        // Arrange
        var activeTemplate = new CmsTemplate 
        { 
            Name = "Active Template", 
            TemplateType = "Article", 
            IsActive = true,
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        var inactiveTemplate = new CmsTemplate 
        { 
            Name = "Inactive Template", 
            TemplateType = "Page", 
            IsActive = false,
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Templates.AddRange(activeTemplate, inactiveTemplate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveTemplatesAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active Template");
    }

    [Fact]
    public async Task GetByTypeAsync_ReturnsActiveTemplatesOfSpecifiedType()
    {
        // Arrange
        var articleTemplate = new CmsTemplate 
        { 
            Name = "Article Template", 
            TemplateType = "Article", 
            IsActive = true,
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        var pageTemplate = new CmsTemplate 
        { 
            Name = "Page Template", 
            TemplateType = "Page", 
            IsActive = true,
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        var inactiveArticleTemplate = new CmsTemplate 
        { 
            Name = "Inactive Article Template", 
            TemplateType = "Article", 
            IsActive = false,
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Templates.AddRange(articleTemplate, pageTemplate, inactiveArticleTemplate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByTypeAsync("Article");

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Article Template");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}