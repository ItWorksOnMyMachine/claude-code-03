using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmsBff.Data.Entities;

/// <summary>
/// Represents a content page with metadata and status tracking
/// </summary>
public class CmsPage : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? MetaTitle { get; set; }

    [MaxLength(160)]
    public string? MetaDescription { get; set; }

    public string? MetaKeywords { get; set; }

    /// <summary>
    /// Page status: 0=Draft, 1=Published, 2=Archived
    /// </summary>
    public CmsPageStatus Status { get; set; } = CmsPageStatus.Draft;

    /// <summary>
    /// When the page was published (null if never published)
    /// </summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// Featured image for the page
    /// </summary>
    public string? FeaturedImageUrl { get; set; }

    /// <summary>
    /// Template used for this page layout
    /// </summary>
    public Guid? TemplateId { get; set; }

    /// <summary>
    /// JSON metadata for additional page configuration
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";

    /// <summary>
    /// Page ordering for navigation (lower numbers appear first)
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this page appears in navigation menus
    /// </summary>
    public bool ShowInNavigation { get; set; } = true;

    /// <summary>
    /// Parent page for hierarchical organization
    /// </summary>
    public Guid? ParentPageId { get; set; }

    // IAuditableEntity implementation
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public CmsTemplate? Template { get; set; }
    public CmsPage? ParentPage { get; set; }
    public ICollection<CmsPage> ChildPages { get; set; } = new List<CmsPage>();
    public ICollection<CmsContentBlock> ContentBlocks { get; set; } = new List<CmsContentBlock>();
}

/// <summary>
/// Enumeration for page status values
/// </summary>
public enum CmsPageStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}