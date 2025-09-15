using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using CmsBff.Controllers;
using CmsBff.Data.Entities;
using CmsBff.Services;

namespace CmsBff.Tests.Controllers;

public class CmsControllerTests
{
    private readonly Mock<CmsContentService> _mockContentService;
    private readonly Mock<CmsTemplateService> _mockTemplateService;
    private readonly Mock<CmsAssetService> _mockAssetService;
    private readonly CmsController _controller;

    public CmsControllerTests()
    {
        _mockContentService = new Mock<CmsContentService>();
        _mockTemplateService = new Mock<CmsTemplateService>();
        _mockAssetService = new Mock<CmsAssetService>();
        
        _controller = new CmsController(
            _mockContentService.Object,
            _mockTemplateService.Object,
            _mockAssetService.Object);
    }

    [Fact]
    public async Task GetAllContent_ReturnsOkResult_WithContentList()
    {
        // Arrange
        var expectedContent = new List<CmsContent>
        {
            new CmsContent { Id = 1, Title = "Test Content 1" },
            new CmsContent { Id = 2, Title = "Test Content 2" }
        };
        _mockContentService.Setup(s => s.GetAllAsync()).ReturnsAsync(expectedContent);

        // Act
        var result = await _controller.GetAllContent();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult?.Value.Should().BeEquivalentTo(expectedContent);
    }

    [Fact]
    public async Task GetContent_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var contentId = 1;
        var expectedContent = new CmsContent { Id = contentId, Title = "Test Content" };
        _mockContentService.Setup(s => s.GetByIdAsync(contentId)).ReturnsAsync(expectedContent);

        // Act
        var result = await _controller.GetContent(contentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult?.Value.Should().BeEquivalentTo(expectedContent);
    }

    [Fact]
    public async Task GetContent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var contentId = 999;
        _mockContentService.Setup(s => s.GetByIdAsync(contentId)).ReturnsAsync((CmsContent?)null);

        // Act
        var result = await _controller.GetContent(contentId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateContent_WithValidContent_ReturnsCreatedResult()
    {
        // Arrange
        var newContent = new CmsContent { Title = "New Content", Slug = "new-content" };
        var createdContent = new CmsContent { Id = 1, Title = "New Content", Slug = "new-content" };
        _mockContentService.Setup(s => s.CreateAsync(newContent)).ReturnsAsync(createdContent);

        // Act
        var result = await _controller.CreateContent(newContent);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult?.Value.Should().BeEquivalentTo(createdContent);
    }

    [Fact]
    public async Task DeleteContent_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var contentId = 1;
        _mockContentService.Setup(s => s.DeleteAsync(contentId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteContent(contentId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteContent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var contentId = 999;
        _mockContentService.Setup(s => s.DeleteAsync(contentId)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteContent(contentId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}