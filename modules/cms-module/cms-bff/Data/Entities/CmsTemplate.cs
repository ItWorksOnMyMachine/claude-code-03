using System.ComponentModel.DataAnnotations;

namespace CmsBff.Data.Entities;

public class CmsTemplate : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public string TemplateContent { get; set; } = string.Empty;
    
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
    public ICollection<CmsContent> Contents { get; set; } = new List<CmsContent>();
}