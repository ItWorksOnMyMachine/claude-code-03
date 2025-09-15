using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmsBff.Data.Entities;

/// <summary>
/// Represents a content block within a page with positioning and content data
/// </summary>
public class CmsContentBlock : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    /// <summary>
    /// The page this content block belongs to
    /// </summary>
    [Required]
    public Guid PageId { get; set; }

    /// <summary>
    /// Type of content block (text, image, video, html, button, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string BlockType { get; set; } = string.Empty;

    /// <summary>
    /// Zone where this block is positioned (header, content, sidebar, footer, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Zone { get; set; } = string.Empty;

    /// <summary>
    /// Sort order within the zone (lower numbers appear first)
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// JSON content data for the block
    /// Structure varies by BlockType:
    /// - text: { "content": "...", "style": {...} }
    /// - image: { "src": "...", "alt": "...", "width": 100, "height": 100 }
    /// - video: { "src": "...", "poster": "...", "controls": true }
    /// - html: { "content": "..." }
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Content { get; set; } = "{}";

    /// <summary>
    /// Block-specific styling and configuration
    /// Example: { "css": {...}, "responsive": {...}, "animation": {...} }
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Configuration { get; set; } = "{}";

    /// <summary>
    /// Whether this block is currently active/visible
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional display name for the block (for admin interface)
    /// </summary>
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    // IAuditableEntity implementation
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public CmsPage Page { get; set; } = null!;
}

/// <summary>
/// Common content block types for validation and UI
/// </summary>
public static class CmsBlockTypes
{
    public const string Text = "text";
    public const string Html = "html";
    public const string Image = "image";
    public const string Video = "video";
    public const string Audio = "audio";
    public const string Button = "button";
    public const string Link = "link";
    public const string Gallery = "gallery";
    public const string Form = "form";
    public const string Embed = "embed";
    public const string Divider = "divider";
    public const string Spacer = "spacer";
}