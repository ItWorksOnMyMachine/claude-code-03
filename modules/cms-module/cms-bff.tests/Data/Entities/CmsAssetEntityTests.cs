using CmsBff.Data.Entities;
using System.ComponentModel.DataAnnotations;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace CmsBff.Tests.Data.Entities;

public class CmsAssetEntityTests
{
    [Fact]
    public void CmsAsset_Should_HaveCorrectDefaultValues()
    {
        // Arrange & Act
        var asset = new CmsAsset();

        // Assert
        asset.Should().NotBeNull();
        asset.Id.Should().NotBe(Guid.Empty);
        asset.TenantId.Should().Be(Guid.Empty);
        asset.FileName.Should().Be(string.Empty);
        asset.OriginalFileName.Should().Be(string.Empty);
        asset.StoragePath.Should().Be(string.Empty);
        asset.PublicUrl.Should().BeNull();
        asset.MimeType.Should().Be(string.Empty);
        asset.FileSize.Should().Be(0);
        asset.AssetType.Should().Be("file");
        asset.AltText.Should().BeNull();
        asset.Description.Should().BeNull();
        asset.Tags.Should().Be("[]");
        asset.Width.Should().BeNull();
        asset.Height.Should().BeNull();
        asset.Duration.Should().BeNull();
        asset.Metadata.Should().Be("{}");
        asset.ContentHash.Should().BeNull();
        asset.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void CmsAsset_Should_ImplementIAuditableEntity()
    {
        // Arrange
        var asset = new CmsAsset();

        // Act & Assert
        asset.Should().BeAssignableTo<IAuditableEntity>();
        asset.CreatedAt.Should().Be(default);
        asset.UpdatedAt.Should().Be(default);
        asset.DeletedAt.Should().BeNull();
        asset.IsDeleted.Should().BeFalse();
        asset.CreatedBy.Should().BeNull();
        asset.UpdatedBy.Should().BeNull();
        asset.DeletedBy.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CmsAsset_FileName_Should_BeRequired(string invalidFileName)
    {
        // Arrange
        var asset = new CmsAsset { FileName = invalidFileName };
        var context = new ValidationContext(asset);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(asset, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("FileName"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CmsAsset_OriginalFileName_Should_BeRequired(string invalidOriginalFileName)
    {
        // Arrange
        var asset = new CmsAsset { OriginalFileName = invalidOriginalFileName };
        var context = new ValidationContext(asset);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(asset, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("OriginalFileName"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CmsAsset_StoragePath_Should_BeRequired(string invalidStoragePath)
    {
        // Arrange
        var asset = new CmsAsset { StoragePath = invalidStoragePath };
        var context = new ValidationContext(asset);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(asset, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("StoragePath"));
    }

    [Fact]
    public void CmsAsset_Tags_Should_ContainValidJSONArray()
    {
        // Arrange
        var asset = new CmsAsset();
        var tags = new[] { "marketing", "product", "banner" };
        var jsonTags = JsonSerializer.Serialize(tags);

        // Act
        asset.Tags = jsonTags;

        // Assert
        asset.Tags.Should().NotBeEmpty();

        // Verify it's valid JSON array
        var deserializedTags = JsonSerializer.Deserialize<string[]>(asset.Tags);
        deserializedTags.Should().NotBeNull();
        deserializedTags.Should().HaveCount(3);
        deserializedTags.Should().Contain("marketing");
        deserializedTags.Should().Contain("product");
        deserializedTags.Should().Contain("banner");
    }

    [Fact]
    public void CmsAsset_Metadata_Should_ContainValidJSON()
    {
        // Arrange
        var asset = new CmsAsset();
        var metadata = new
        {
            exif = new { camera = "Canon EOS", iso = 200 },
            processing = new { resized = true, optimized = true },
            thumbnails = new[] { "thumb_150.jpg", "thumb_300.jpg" }
        };
        var jsonMetadata = JsonSerializer.Serialize(metadata);

        // Act
        asset.Metadata = jsonMetadata;

        // Assert
        asset.Metadata.Should().NotBeEmpty();

        // Verify it's valid JSON
        var deserializedMetadata = JsonSerializer.Deserialize<object>(asset.Metadata);
        deserializedMetadata.Should().NotBeNull();
    }

    [Fact]
    public void CmsAsset_Should_SupportImageDimensions()
    {
        // Arrange
        var asset = new CmsAsset();

        // Act
        asset.Width = 1920;
        asset.Height = 1080;

        // Assert
        asset.Width.Should().Be(1920);
        asset.Height.Should().Be(1080);
    }

    [Fact]
    public void CmsAsset_Should_SupportMediaDuration()
    {
        // Arrange
        var asset = new CmsAsset();

        // Act
        asset.Duration = 125.5; // 2 minutes 5.5 seconds

        // Assert
        asset.Duration.Should().Be(125.5);
    }

    [Fact]
    public void CmsAsset_Should_SupportFileSize()
    {
        // Arrange
        var asset = new CmsAsset();

        // Act
        asset.FileSize = 1048576; // 1MB

        // Assert
        asset.FileSize.Should().Be(1048576);
    }

    [Fact]
    public void CmsAsset_Should_SupportContentHash()
    {
        // Arrange
        var asset = new CmsAsset();
        var hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        asset.ContentHash = hash;

        // Assert
        asset.ContentHash.Should().Be(hash);
        asset.ContentHash.Should().HaveLength(64);
    }

    [Fact]
    public void CmsAsset_Should_SupportPublicPrivateAccess()
    {
        // Arrange
        var publicAsset = new CmsAsset();
        var privateAsset = new CmsAsset();

        // Act
        publicAsset.IsPublic = true;
        privateAsset.IsPublic = false;

        // Assert
        publicAsset.IsPublic.Should().BeTrue();
        privateAsset.IsPublic.Should().BeFalse();
    }

    [Fact]
    public void CmsAsset_Should_SupportTenantIsolation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var asset = new CmsAsset();

        // Act
        asset.TenantId = tenantId;

        // Assert
        asset.TenantId.Should().Be(tenantId);
        asset.TenantId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CmsAsset_Should_SupportAssetTypes()
    {
        // Arrange
        var asset = new CmsAsset();

        // Act & Assert
        asset.AssetType = CmsAssetTypes.Image;
        asset.AssetType.Should().Be("image");

        asset.AssetType = CmsAssetTypes.Video;
        asset.AssetType.Should().Be("video");

        asset.AssetType = CmsAssetTypes.Document;
        asset.AssetType.Should().Be("document");
    }
}

/// <summary>
/// Tests for the CmsAssetTypes static class
/// </summary>
public class CmsAssetTypesTests
{
    [Fact]
    public void CmsAssetTypes_Should_HaveAllExpectedTypes()
    {
        // Assert
        CmsAssetTypes.Image.Should().Be("image");
        CmsAssetTypes.Document.Should().Be("document");
        CmsAssetTypes.Video.Should().Be("video");
        CmsAssetTypes.Audio.Should().Be("audio");
        CmsAssetTypes.Archive.Should().Be("archive");
        CmsAssetTypes.Font.Should().Be("font");
        CmsAssetTypes.Other.Should().Be("other");
    }

    [Fact]
    public void CmsAssetTypes_Should_BeStringConstants()
    {
        // Act & Assert - Verify they're all strings
        typeof(CmsAssetTypes).GetFields()
            .Where(f => f.IsStatic && f.IsLiteral)
            .All(f => f.FieldType == typeof(string))
            .Should().BeTrue();
    }
}