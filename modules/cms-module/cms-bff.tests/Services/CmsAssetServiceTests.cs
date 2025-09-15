using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CmsBff.Data;
using CmsBff.Data.Entities;
using CmsBff.Services;
using Xunit;

namespace CmsBff.Tests.Services;

public class CmsAssetServiceTests : IDisposable
{
    private readonly CmsDbContext _context;
    private readonly CmsAssetService _service;

    public CmsAssetServiceTests()
    {
        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CmsDbContext(options);
        _service = new CmsAssetService(_context);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAssets_OrderedByCreatedAt()
    {
        // Arrange
        var asset1 = new CmsAsset 
        { 
            FileName = "old-file.jpg", 
            OriginalFileName = "old-file.jpg",
            FilePath = "/uploads/old-file.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024,
            AssetType = "Image",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        var asset2 = new CmsAsset 
        { 
            FileName = "new-file.jpg", 
            OriginalFileName = "new-file.jpg",
            FilePath = "/uploads/new-file.jpg",
            MimeType = "image/jpeg",
            FileSize = 2048,
            AssetType = "Image",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().FileName.Should().Be("new-file.jpg"); // Most recently created first
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsAsset()
    {
        // Arrange
        var asset = new CmsAsset 
        { 
            FileName = "test-file.pdf", 
            OriginalFileName = "test-file.pdf",
            FilePath = "/uploads/test-file.pdf",
            MimeType = "application/pdf",
            FileSize = 5120,
            AssetType = "Document",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(asset.Id);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("test-file.pdf");
        result.AssetType.Should().Be("Document");
    }

    [Fact]
    public async Task CreateAsync_WithValidAsset_CreatesAndReturnsAsset()
    {
        // Arrange
        var asset = new CmsAsset 
        { 
            FileName = "new-image.png", 
            OriginalFileName = "my-image.png",
            FilePath = "/uploads/new-image.png",
            MimeType = "image/png",
            FileSize = 3072,
            AssetType = "Image",
            AltText = "Test image",
            Description = "A test image file",
            Tags = "test,image",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        // Act
        var result = await _service.CreateAsync(asset);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var savedAsset = await _context.Assets.FindAsync(result.Id);
        savedAsset.Should().NotBeNull();
        savedAsset!.FileName.Should().Be("new-image.png");
        savedAsset.AltText.Should().Be("Test image");
    }

    [Fact]
    public async Task UpdateAsync_WithValidId_UpdatesMetadataOnly()
    {
        // Arrange
        var asset = new CmsAsset 
        { 
            FileName = "original-file.jpg", 
            OriginalFileName = "original-file.jpg",
            FilePath = "/uploads/original-file.jpg",
            MimeType = "image/jpeg",
            FileSize = 2048,
            AssetType = "Image",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        var updateAsset = new CmsAsset 
        { 
            AltText = "Updated alt text",
            Description = "Updated description",
            Tags = "updated,tags",
            UpdatedBy = "UpdatedUser"
        };

        // Act
        var result = await _service.UpdateAsync(asset.Id, updateAsset);

        // Assert
        result.Should().NotBeNull();
        result!.AltText.Should().Be("Updated alt text");
        result.Description.Should().Be("Updated description");
        result.Tags.Should().Be("updated,tags");
        result.UpdatedBy.Should().Be("UpdatedUser");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // File properties should remain unchanged
        result.FileName.Should().Be("original-file.jpg");
        result.FileSize.Should().Be(2048);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesAssetAndReturnsTrue()
    {
        // Arrange
        var asset = new CmsAsset 
        { 
            FileName = "asset-to-delete.jpg", 
            OriginalFileName = "asset-to-delete.jpg",
            FilePath = "/uploads/asset-to-delete.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024,
            AssetType = "Image",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(asset.Id);

        // Assert
        result.Should().BeTrue();
        var deletedAsset = await _context.Assets.FindAsync(asset.Id);
        deletedAsset.Should().BeNull();
    }

    [Fact]
    public async Task GetByTypeAsync_ReturnsAssetsOfSpecifiedType()
    {
        // Arrange
        var imageAsset = new CmsAsset 
        { 
            FileName = "image.jpg", 
            OriginalFileName = "image.jpg",
            FilePath = "/uploads/image.jpg",
            MimeType = "image/jpeg",
            FileSize = 2048,
            AssetType = "Image",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        var documentAsset = new CmsAsset 
        { 
            FileName = "document.pdf", 
            OriginalFileName = "document.pdf",
            FilePath = "/uploads/document.pdf",
            MimeType = "application/pdf",
            FileSize = 5120,
            AssetType = "Document",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Assets.AddRange(imageAsset, documentAsset);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByTypeAsync("Image");

        // Assert
        result.Should().HaveCount(1);
        result.First().FileName.Should().Be("image.jpg");
        result.First().AssetType.Should().Be("Image");
    }

    [Fact]
    public async Task SearchByTagsAsync_ReturnsAssetsContainingTag()
    {
        // Arrange
        var asset1 = new CmsAsset 
        { 
            FileName = "tagged-asset1.jpg", 
            OriginalFileName = "tagged-asset1.jpg",
            FilePath = "/uploads/tagged-asset1.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024,
            AssetType = "Image",
            Tags = "nature,landscape,outdoor",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        var asset2 = new CmsAsset 
        { 
            FileName = "tagged-asset2.jpg", 
            OriginalFileName = "tagged-asset2.jpg",
            FilePath = "/uploads/tagged-asset2.jpg",
            MimeType = "image/jpeg",
            FileSize = 2048,
            AssetType = "Image",
            Tags = "portrait,indoor",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        var asset3 = new CmsAsset 
        { 
            FileName = "untagged-asset.jpg", 
            OriginalFileName = "untagged-asset.jpg",
            FilePath = "/uploads/untagged-asset.jpg",
            MimeType = "image/jpeg",
            FileSize = 1536,
            AssetType = "Image",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Assets.AddRange(asset1, asset2, asset3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchByTagsAsync("nature");

        // Assert
        result.Should().HaveCount(1);
        result.First().FileName.Should().Be("tagged-asset1.jpg");
    }

    [Fact]
    public async Task GetTotalFileSizeAsync_ReturnsSumOfAllAssetSizes()
    {
        // Arrange
        var asset1 = new CmsAsset 
        { 
            FileName = "file1.jpg", 
            OriginalFileName = "file1.jpg",
            FilePath = "/uploads/file1.jpg",
            MimeType = "image/jpeg",
            FileSize = 1024,
            AssetType = "Image",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };
        var asset2 = new CmsAsset 
        { 
            FileName = "file2.pdf", 
            OriginalFileName = "file2.pdf",
            FilePath = "/uploads/file2.pdf",
            MimeType = "application/pdf",
            FileSize = 5120,
            AssetType = "Document",
            CreatedBy = "Test",
            UpdatedBy = "Test"
        };

        _context.Assets.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTotalFileSizeAsync();

        // Assert
        result.Should().Be(6144); // 1024 + 5120
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}