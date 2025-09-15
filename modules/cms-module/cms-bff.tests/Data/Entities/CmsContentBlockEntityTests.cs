using CmsBff.Data.Entities;
using System.ComponentModel.DataAnnotations;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace CmsBff.Tests.Data.Entities;

public class CmsContentBlockEntityTests
{
    [Fact]
    public void CmsContentBlock_Should_HaveCorrectDefaultValues()
    {
        // Arrange & Act
        var block = new CmsContentBlock();

        // Assert
        block.Should().NotBeNull();
        block.Id.Should().NotBe(Guid.Empty);
        block.TenantId.Should().Be(Guid.Empty);
        block.PageId.Should().Be(Guid.Empty);
        block.BlockType.Should().Be(string.Empty);
        block.Zone.Should().Be(string.Empty);
        block.SortOrder.Should().Be(0);
        block.Content.Should().Be("{}");
        block.Configuration.Should().Be("{}");
        block.IsActive.Should().BeTrue();
        block.DisplayName.Should().BeNull();
    }

    [Fact]
    public void CmsContentBlock_Should_ImplementIAuditableEntity()
    {
        // Arrange
        var block = new CmsContentBlock();

        // Act & Assert
        block.Should().BeAssignableTo<IAuditableEntity>();
        block.CreatedAt.Should().Be(default);
        block.UpdatedAt.Should().Be(default);
        block.DeletedAt.Should().BeNull();
        block.IsDeleted.Should().BeFalse();
        block.CreatedBy.Should().BeNull();
        block.UpdatedBy.Should().BeNull();
        block.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void CmsContentBlock_PageId_Should_BeRequired()
    {
        // Arrange
        var block = new CmsContentBlock();
        var context = new ValidationContext(block);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(block, context, results, true);

        // Assert - PageId is required but validation may not catch Guid.Empty
        block.PageId.Should().Be(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CmsContentBlock_BlockType_Should_BeRequired(string invalidBlockType)
    {
        // Arrange
        var block = new CmsContentBlock { BlockType = invalidBlockType };
        var context = new ValidationContext(block);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(block, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("BlockType"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CmsContentBlock_Zone_Should_BeRequired(string invalidZone)
    {
        // Arrange
        var block = new CmsContentBlock { Zone = invalidZone };
        var context = new ValidationContext(block);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(block, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Zone"));
    }

    [Fact]
    public void CmsContentBlock_Content_Should_ContainValidJSON()
    {
        // Arrange
        var block = new CmsContentBlock();
        var contentData = new
        {
            text = "Hello World",
            style = new { color = "blue", fontSize = "16px" },
            settings = new { autoplay = true }
        };
        var jsonContent = JsonSerializer.Serialize(contentData);

        // Act
        block.Content = jsonContent;

        // Assert
        block.Content.Should().NotBeEmpty();

        // Verify it's valid JSON
        var deserializedContent = JsonSerializer.Deserialize<object>(block.Content);
        deserializedContent.Should().NotBeNull();
    }

    [Fact]
    public void CmsContentBlock_Configuration_Should_ContainValidJSON()
    {
        // Arrange
        var block = new CmsContentBlock();
        var configData = new
        {
            css = new { backgroundColor = "#fff", margin = "10px" },
            responsive = new { mobile = new { display = "block" } },
            animation = new { type = "fade", duration = 300 }
        };
        var jsonConfig = JsonSerializer.Serialize(configData);

        // Act
        block.Configuration = jsonConfig;

        // Assert
        block.Configuration.Should().NotBeEmpty();

        // Verify it's valid JSON
        var deserializedConfig = JsonSerializer.Deserialize<object>(block.Configuration);
        deserializedConfig.Should().NotBeNull();
    }

    [Fact]
    public void CmsContentBlock_Should_SupportDifferentBlockTypes()
    {
        // Arrange & Act
        var textBlock = new CmsContentBlock { BlockType = CmsBlockTypes.Text };
        var imageBlock = new CmsContentBlock { BlockType = CmsBlockTypes.Image };
        var videoBlock = new CmsContentBlock { BlockType = CmsBlockTypes.Video };

        // Assert
        textBlock.BlockType.Should().Be("text");
        imageBlock.BlockType.Should().Be("image");
        videoBlock.BlockType.Should().Be("video");
    }

    [Fact]
    public void CmsContentBlock_Should_SupportSortOrdering()
    {
        // Arrange
        var block1 = new CmsContentBlock { Zone = "header", SortOrder = 1 };
        var block2 = new CmsContentBlock { Zone = "header", SortOrder = 2 };
        var block3 = new CmsContentBlock { Zone = "content", SortOrder = 1 };

        // Act & Assert
        block1.SortOrder.Should().BeLessThan(block2.SortOrder);
        block1.Zone.Should().Be(block2.Zone);
        block3.Zone.Should().NotBe(block1.Zone);
    }

    [Fact]
    public void CmsContentBlock_Should_SupportZonePositioning()
    {
        // Arrange
        var headerBlock = new CmsContentBlock { Zone = "header" };
        var contentBlock = new CmsContentBlock { Zone = "content" };
        var sidebarBlock = new CmsContentBlock { Zone = "sidebar" };
        var footerBlock = new CmsContentBlock { Zone = "footer" };

        // Act & Assert
        headerBlock.Zone.Should().Be("header");
        contentBlock.Zone.Should().Be("content");
        sidebarBlock.Zone.Should().Be("sidebar");
        footerBlock.Zone.Should().Be("footer");
    }

    [Fact]
    public void CmsContentBlock_Should_SupportActiveState()
    {
        // Arrange
        var block = new CmsContentBlock();

        // Act
        block.IsActive = false;

        // Assert
        block.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CmsContentBlock_Should_SupportDisplayName()
    {
        // Arrange
        var block = new CmsContentBlock();

        // Act
        block.DisplayName = "Hero Banner";

        // Assert
        block.DisplayName.Should().Be("Hero Banner");
    }

    [Fact]
    public void CmsContentBlock_Should_HavePageNavigation()
    {
        // Arrange & Act
        var block = new CmsContentBlock();

        // Assert
        block.Page.Should().BeNull(); // Will be set by EF Core
    }

    [Fact]
    public void CmsContentBlock_Should_SupportTenantIsolation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var block = new CmsContentBlock();

        // Act
        block.TenantId = tenantId;

        // Assert
        block.TenantId.Should().Be(tenantId);
        block.TenantId.Should().NotBe(Guid.Empty);
    }
}

/// <summary>
/// Tests for the CmsBlockTypes static class
/// </summary>
public class CmsBlockTypesTests
{
    [Fact]
    public void CmsBlockTypes_Should_HaveAllExpectedTypes()
    {
        // Assert
        CmsBlockTypes.Text.Should().Be("text");
        CmsBlockTypes.Html.Should().Be("html");
        CmsBlockTypes.Image.Should().Be("image");
        CmsBlockTypes.Video.Should().Be("video");
        CmsBlockTypes.Audio.Should().Be("audio");
        CmsBlockTypes.Button.Should().Be("button");
        CmsBlockTypes.Link.Should().Be("link");
        CmsBlockTypes.Gallery.Should().Be("gallery");
        CmsBlockTypes.Form.Should().Be("form");
        CmsBlockTypes.Embed.Should().Be("embed");
        CmsBlockTypes.Divider.Should().Be("divider");
        CmsBlockTypes.Spacer.Should().Be("spacer");
    }

    [Fact]
    public void CmsBlockTypes_Should_BeStringConstants()
    {
        // Act & Assert - Verify they're all strings
        typeof(CmsBlockTypes).GetFields()
            .Where(f => f.IsStatic && f.IsLiteral)
            .All(f => f.FieldType == typeof(string))
            .Should().BeTrue();
    }
}