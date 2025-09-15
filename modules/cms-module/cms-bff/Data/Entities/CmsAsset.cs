using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmsBff.Data.Entities;

/// <summary>
/// Represents a media asset (image, document, video, etc.) with metadata
/// </summary>
public class CmsAsset : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// URL path for accessing the asset
    /// </summary>
    [MaxLength(500)]
    public string? PublicUrl { get; set; }

    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Asset category: image, document, video, audio, archive, etc.
    /// </summary>
    [MaxLength(50)]
    public string AssetType { get; set; } = "file";

    [MaxLength(255)]
    public string? AltText { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// JSON array of tags for organization and search
    /// Example: ["marketing", "product", "banner"]
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Tags { get; set; } = "[]";

    /// <summary>
    /// Image dimensions (if applicable)
    /// </summary>
    public int? Width { get; set; }
    public int? Height { get; set; }

    /// <summary>
    /// Duration in seconds for video/audio files
    /// </summary>
    public double? Duration { get; set; }

    /// <summary>
    /// Additional metadata specific to the asset type
    /// Example: { "exif": {...}, "processing": {...}, "thumbnails": [...] }
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";

    /// <summary>
    /// Hash of the file content for duplicate detection
    /// </summary>
    [MaxLength(64)]
    public string? ContentHash { get; set; }

    /// <summary>
    /// Whether this asset is publicly accessible
    /// </summary>
    public bool IsPublic { get; set; } = true;

    // IAuditableEntity implementation
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Common asset types for validation and categorization
/// </summary>
public static class CmsAssetTypes
{
    public const string Image = "image";
    public const string Document = "document";
    public const string Video = "video";
    public const string Audio = "audio";
    public const string Archive = "archive";
    public const string Font = "font";
    public const string Other = "other";
}