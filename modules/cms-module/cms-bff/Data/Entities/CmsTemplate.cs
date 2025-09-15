using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmsBff.Data.Entities;

public class CmsTemplate : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// JSON structure defining layout zones, blocks, and configuration
    /// Example: { "zones": [{"name": "header", "blocks": ["text", "image"]}], "styles": {...} }
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string LayoutContent { get; set; } = "{}";

    [MaxLength(50)]
    public string TemplateType { get; set; } = "page";

    public bool IsActive { get; set; } = true;

    public string? PreviewImage { get; set; }

    public int UsageCount { get; set; } = 0;

    // IAuditableEntity implementation
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public ICollection<CmsPage> Pages { get; set; } = new List<CmsPage>();
}