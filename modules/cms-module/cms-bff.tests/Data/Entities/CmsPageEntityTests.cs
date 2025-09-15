using CmsBff.Data.Entities;
using System.ComponentModel.DataAnnotations;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace CmsBff.Tests.Data.Entities;

public class CmsPageEntityTests
{
    [Fact]
    public void CmsPage_Should_HaveCorrectDefaultValues()
    {
        // Arrange & Act
        var page = new CmsPage();

        // Assert
        page.Should().NotBeNull();
        page.Id.Should().NotBe(Guid.Empty);
        page.TenantId.Should().Be(Guid.Empty); // Default value
        page.Title.Should().Be(string.Empty);
        page.Slug.Should().Be(string.Empty);
        page.MetaTitle.Should().BeNull();
        page.MetaDescription.Should().BeNull();
        page.MetaKeywords.Should().BeNull();
        page.Status.Should().Be(CmsPageStatus.Draft);
        page.PublishedAt.Should().BeNull();
        page.FeaturedImageUrl.Should().BeNull();
        page.TemplateId.Should().BeNull();
        page.Metadata.Should().Be("{}");
        page.SortOrder.Should().Be(0);
        page.ShowInNavigation.Should().BeTrue();
        page.ParentPageId.Should().BeNull();
    }

    [Fact]
    public void CmsPage_Should_ImplementIAuditableEntity()
    {
        // Arrange
        var page = new CmsPage();

        // Act & Assert
        page.Should().BeAssignableTo<IAuditableEntity>();
        page.CreatedAt.Should().Be(default);
        page.UpdatedAt.Should().Be(default);
        page.DeletedAt.Should().BeNull();
        page.IsDeleted.Should().BeFalse();
        page.CreatedBy.Should().BeNull();
        page.UpdatedBy.Should().BeNull();
        page.DeletedBy.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CmsPage_Title_Should_BeRequired(string invalidTitle)
    {
        // Arrange
        var page = new CmsPage { Title = invalidTitle };
        var context = new ValidationContext(page);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(page, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Title"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CmsPage_Slug_Should_BeRequired(string invalidSlug)
    {
        // Arrange
        var page = new CmsPage { Slug = invalidSlug };
        var context = new ValidationContext(page);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(page, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Slug"));
    }

    [Fact]
    public void CmsPage_Should_SupportPageStatus()
    {
        // Arrange
        var page = new CmsPage();

        // Act & Assert
        page.Status = CmsPageStatus.Published;
        page.Status.Should().Be(CmsPageStatus.Published);

        page.Status = CmsPageStatus.Archived;
        page.Status.Should().Be(CmsPageStatus.Archived);

        page.Status = CmsPageStatus.Draft;
        page.Status.Should().Be(CmsPageStatus.Draft);
    }

    [Fact]
    public void CmsPage_Should_SupportPublishedTimestamp()
    {
        // Arrange
        var page = new CmsPage();
        var publishedTime = DateTimeOffset.UtcNow;

        // Act
        page.Status = CmsPageStatus.Published;
        page.PublishedAt = publishedTime;

        // Assert
        page.PublishedAt.Should().Be(publishedTime);
        page.Status.Should().Be(CmsPageStatus.Published);
    }

    [Fact]
    public void CmsPage_Metadata_Should_ContainValidJSON()
    {
        // Arrange
        var page = new CmsPage();
        var metadata = new
        {
            seo = new { robots = "index,follow" },
            social = new { ogTitle = "Test Page" },
            custom = new { category = "blog" }
        };
        var jsonContent = JsonSerializer.Serialize(metadata);

        // Act
        page.Metadata = jsonContent;

        // Assert
        page.Metadata.Should().NotBeEmpty();

        // Verify it's valid JSON
        var deserializedContent = JsonSerializer.Deserialize<object>(page.Metadata);
        deserializedContent.Should().NotBeNull();
    }

    [Fact]
    public void CmsPage_Should_SupportHierarchicalStructure()
    {
        // Arrange
        var parentPage = new CmsPage
        {
            Id = Guid.NewGuid(),
            Title = "Parent Page",
            Slug = "parent"
        };

        var childPage = new CmsPage
        {
            Title = "Child Page",
            Slug = "child",
            ParentPageId = parentPage.Id
        };

        // Act & Assert
        childPage.ParentPageId.Should().Be(parentPage.Id);
        parentPage.ChildPages.Add(childPage);
        parentPage.ChildPages.Should().Contain(childPage);
    }

    [Fact]
    public void CmsPage_Should_HaveNavigationProperties()
    {
        // Arrange & Act
        var page = new CmsPage();

        // Assert
        page.Template.Should().BeNull();
        page.ParentPage.Should().BeNull();
        page.ChildPages.Should().NotBeNull();
        page.ChildPages.Should().BeEmpty();
        page.ContentBlocks.Should().NotBeNull();
        page.ContentBlocks.Should().BeEmpty();
    }

    [Fact]
    public void CmsPage_Should_SupportSortOrder()
    {
        // Arrange
        var page1 = new CmsPage { Title = "First", SortOrder = 1 };
        var page2 = new CmsPage { Title = "Second", SortOrder = 2 };

        // Act & Assert
        page1.SortOrder.Should().BeLessThan(page2.SortOrder);
    }

    [Fact]
    public void CmsPage_Should_SupportNavigationVisibility()
    {
        // Arrange
        var page = new CmsPage();

        // Act
        page.ShowInNavigation = false;

        // Assert
        page.ShowInNavigation.Should().BeFalse();
    }

    [Fact]
    public void CmsPage_Should_SupportTenantIsolation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var page = new CmsPage();

        // Act
        page.TenantId = tenantId;

        // Assert
        page.TenantId.Should().Be(tenantId);
        page.TenantId.Should().NotBe(Guid.Empty);
    }
}

/// <summary>
/// Tests for the CmsPageStatus enumeration
/// </summary>
public class CmsPageStatusTests
{
    [Fact]
    public void CmsPageStatus_Should_HaveCorrectValues()
    {
        // Assert
        ((int)CmsPageStatus.Draft).Should().Be(0);
        ((int)CmsPageStatus.Published).Should().Be(1);
        ((int)CmsPageStatus.Archived).Should().Be(2);
    }

    [Fact]
    public void CmsPageStatus_Should_SupportAllValues()
    {
        // Act & Assert
        var allStatuses = Enum.GetValues<CmsPageStatus>();
        allStatuses.Should().HaveCount(3);
        allStatuses.Should().Contain(CmsPageStatus.Draft);
        allStatuses.Should().Contain(CmsPageStatus.Published);
        allStatuses.Should().Contain(CmsPageStatus.Archived);
    }
}