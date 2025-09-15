using CmsBff.Data.Entities;
using System.ComponentModel.DataAnnotations;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace CmsBff.Tests.Data.Entities;

public class CmsTemplateEntityTests
{
    [Fact]
    public void CmsTemplate_Should_HaveCorrectProperties()
    {
        // Arrange & Act
        var template = new CmsTemplate();

        // Assert
        template.Should().NotBeNull();
        template.Id.Should().NotBe(Guid.Empty);
        template.TenantId.Should().Be(Guid.Empty); // Default value
        template.Name.Should().Be(string.Empty);
        template.Description.Should().BeNull();
        template.LayoutContent.Should().Be("{}"); // Default JSON
        template.TemplateType.Should().Be("page");
        template.IsActive.Should().BeTrue();
        template.PreviewImage.Should().BeNull();
        template.UsageCount.Should().Be(0);
    }

    [Fact]
    public void CmsTemplate_Should_ImplementIAuditableEntity()
    {
        // Arrange
        var template = new CmsTemplate();

        // Act & Assert
        template.Should().BeAssignableTo<IAuditableEntity>();
        template.CreatedAt.Should().Be(default);
        template.UpdatedAt.Should().Be(default);
        template.DeletedAt.Should().BeNull();
        template.IsDeleted.Should().BeFalse();
        template.CreatedBy.Should().BeNull();
        template.UpdatedBy.Should().BeNull();
        template.DeletedBy.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CmsTemplate_Name_Should_BeRequired(string invalidName)
    {
        // Arrange
        var template = new CmsTemplate { Name = invalidName };
        var context = new ValidationContext(template);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(template, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void CmsTemplate_Name_Should_NotExceedMaxLength()
    {
        // Arrange
        var longName = new string('A', 101); // 101 characters (max is 100)
        var template = new CmsTemplate { Name = longName };
        var context = new ValidationContext(template);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(template, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void CmsTemplate_LayoutContent_Should_ContainValidJSON()
    {
        // Arrange
        var template = new CmsTemplate();
        var layoutData = new
        {
            zones = new[]
            {
                new { name = "header", blocks = new[] { "text", "image" } },
                new { name = "content", blocks = new[] { "html", "video" } }
            },
            styles = new { theme = "default" }
        };
        var jsonContent = JsonSerializer.Serialize(layoutData);

        // Act
        template.LayoutContent = jsonContent;

        // Assert
        template.LayoutContent.Should().NotBeEmpty();

        // Verify it's valid JSON
        var deserializedContent = JsonSerializer.Deserialize<object>(template.LayoutContent);
        deserializedContent.Should().NotBeNull();
    }

    [Fact]
    public void CmsTemplate_Should_HaveNavigationProperties()
    {
        // Arrange & Act
        var template = new CmsTemplate();

        // Assert
        template.Pages.Should().NotBeNull();
        template.Pages.Should().BeEmpty();
        template.Pages.Should().BeAssignableTo<ICollection<CmsPage>>();
    }

    [Fact]
    public void CmsTemplate_Should_TrackUsageCorrectly()
    {
        // Arrange
        var template = new CmsTemplate();

        // Act
        template.UsageCount = 5;

        // Assert
        template.UsageCount.Should().Be(5);
    }

    [Fact]
    public void CmsTemplate_Should_SupportSoftDelete()
    {
        // Arrange
        var template = new CmsTemplate
        {
            Name = "Test Template",
            IsActive = true
        };

        // Act
        template.IsDeleted = true;
        template.DeletedAt = DateTimeOffset.UtcNow;
        template.DeletedBy = "test-user";

        // Assert
        template.IsDeleted.Should().BeTrue();
        template.DeletedAt.Should().NotBeNull();
        template.DeletedBy.Should().Be("test-user");
    }

    [Fact]
    public void CmsTemplate_Should_SupportTenantIsolation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var template = new CmsTemplate();

        // Act
        template.TenantId = tenantId;

        // Assert
        template.TenantId.Should().Be(tenantId);
        template.TenantId.Should().NotBe(Guid.Empty);
    }
}