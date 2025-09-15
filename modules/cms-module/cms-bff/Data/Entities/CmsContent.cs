using System.ComponentModel.DataAnnotations;

namespace CmsBff.Data.Entities;

public class CmsContent : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;
    
    [Required] 
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string ContentType { get; set; } = "page";
    
    [MaxLength(20)]
    public string Status { get; set; } = "draft";
    
    public Guid? TemplateId { get; set; }
    
    [MaxLength(60)]
    public string? MetaTitle { get; set; }
    
    [MaxLength(160)]
    public string? MetaDescription { get; set; }
    
    public string? Tags { get; set; }
    
    public string? FeaturedImage { get; set; }
    
    public DateTime? PublishedAt { get; set; }

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
    public ICollection<CmsAsset> Assets { get; set; } = new List<CmsAsset>();
}