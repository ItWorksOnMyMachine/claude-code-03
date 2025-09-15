using Microsoft.EntityFrameworkCore;
using CmsBff.Data.Entities;
using PlatformShared.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CmsBff.Data;

public class CmsDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;
    private readonly Guid? _currentTenantId;

    public CmsDbContext(DbContextOptions<CmsDbContext> options)
        : base(options)
    {
    }

    public CmsDbContext(DbContextOptions<CmsDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
        _currentTenantId = tenantContext?.GetCurrentTenantIdAsync().GetAwaiter().GetResult();
    }

    // Constructor for testing with explicit tenant ID
    public CmsDbContext(DbContextOptions<CmsDbContext> options, Guid? currentTenantId)
        : base(options)
    {
        _currentTenantId = currentTenantId;
    }

    public DbSet<CmsContent> Contents => Set<CmsContent>();
    public DbSet<CmsTemplate> Templates => Set<CmsTemplate>();
    public DbSet<CmsAsset> Assets => Set<CmsAsset>();
    public DbSet<CmsPage> Pages => Set<CmsPage>();
    public DbSet<CmsContentBlock> ContentBlocks => Set<CmsContentBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure CmsContent
        modelBuilder.Entity<CmsContent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.MetaTitle).HasMaxLength(60);
            entity.Property(e => e.MetaDescription).HasMaxLength(160);

            entity.HasIndex(e => new { e.TenantId, e.Slug })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.TenantId)
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.Status)
                .HasFilter("\"IsDeleted\" = false");

            // Configure relationship with CmsTemplate (legacy support)
            entity.HasOne(e => e.Template)
                  .WithMany()
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Note: Asset relationships are now handled through CmsContentBlock entities

            // Global query filter for soft delete and tenant isolation
            entity.HasQueryFilter(e => !e.IsDeleted &&
                (_currentTenantId == null || e.TenantId == _currentTenantId));
        });

        // Configure CmsTemplate
        modelBuilder.Entity<CmsTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TemplateType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.LayoutContent).HasColumnType("jsonb");

            entity.HasIndex(e => new { e.TenantId, e.Name })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.TenantId)
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.IsActive)
                .HasFilter("\"IsDeleted\" = false");

            // Global query filter for soft delete and tenant isolation
            entity.HasQueryFilter(e => !e.IsDeleted &&
                (_currentTenantId == null || e.TenantId == _currentTenantId));
        });

        // Configure CmsAsset
        modelBuilder.Entity<CmsAsset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.PublicUrl).HasMaxLength(500);
            entity.Property(e => e.MimeType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AssetType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.AltText).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Tags).HasColumnType("jsonb");
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.Property(e => e.ContentHash).HasMaxLength(64);

            entity.HasIndex(e => e.TenantId)
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.AssetType)
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.FileName)
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.ContentHash)
                .HasFilter("\"IsDeleted\" = false");

            // Global query filter for soft delete and tenant isolation
            entity.HasQueryFilter(e => !e.IsDeleted &&
                (_currentTenantId == null || e.TenantId == _currentTenantId));
        });

        // Configure CmsPage
        modelBuilder.Entity<CmsPage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(255).IsRequired();
            entity.Property(e => e.MetaTitle).HasMaxLength(60);
            entity.Property(e => e.MetaDescription).HasMaxLength(160);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.FeaturedImageUrl).HasMaxLength(500);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");

            // Unique constraint on slug per tenant
            entity.HasIndex(e => new { e.TenantId, e.Slug })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.TenantId)
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.Status)
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.SortOrder)
                .HasFilter("\"IsDeleted\" = false");

            // Foreign key relationships
            entity.HasOne(e => e.Template)
                  .WithMany(t => t.Pages)
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ParentPage)
                  .WithMany(p => p.ChildPages)
                  .HasForeignKey(e => e.ParentPageId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Global query filter for soft delete and tenant isolation
            entity.HasQueryFilter(e => !e.IsDeleted &&
                (_currentTenantId == null || e.TenantId == _currentTenantId));
        });

        // Configure CmsContentBlock
        modelBuilder.Entity<CmsContentBlock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PageId).IsRequired();
            entity.Property(e => e.BlockType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Zone).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.Content).HasColumnType("jsonb");
            entity.Property(e => e.Configuration).HasColumnType("jsonb");

            entity.HasIndex(e => e.TenantId)
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => new { e.PageId, e.Zone, e.SortOrder })
                .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.BlockType)
                .HasFilter("\"IsDeleted\" = false");

            // Foreign key relationship with cascade delete
            entity.HasOne(e => e.Page)
                  .WithMany(p => p.ContentBlocks)
                  .HasForeignKey(e => e.PageId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Global query filter for soft delete and tenant isolation
            entity.HasQueryFilter(e => !e.IsDeleted &&
                (_currentTenantId == null || e.TenantId == _currentTenantId));
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    if (entry.Entity.IsDeleted && !entry.Entity.DeletedAt.HasValue)
                    {
                        entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
                    }
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    if (entry.Entity.IsDeleted && !entry.Entity.DeletedAt.HasValue)
                    {
                        entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
                    }
                    break;
            }
        }

        return base.SaveChanges();
    }
}