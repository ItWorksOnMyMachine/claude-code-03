using System.ComponentModel.DataAnnotations;

namespace CmsBff.Data.Entities;

public class CmsAsset : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    [MaxLength(50)]
    public string AssetType { get; set; } = "file"; // image, document, video, etc.
    
    [MaxLength(255)]
    public string? AltText { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public string? Tags { get; set; }
    
    public int? Width { get; set; }
    public int? Height { get; set; }
    
    // Content relationships
    public ICollection<CmsContent> Contents { get; set; } = new List<CmsContent>();

    // IAuditableEntity implementation
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? DeletedBy { get; set; }
}