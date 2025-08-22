using Microsoft.EntityFrameworkCore;
using PlatformBff.Data.Entities;
using PlatformBff.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlatformBff.Data;

public class PlatformDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;
    private readonly Guid? _currentTenantId;

    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) 
        : base(options)
    {
    }

    public PlatformDbContext(DbContextOptions<PlatformDbContext> options, ITenantContext? tenantContext = null) 
        : base(options)
    {
        _tenantContext = tenantContext;
        _currentTenantId = tenantContext?.GetCurrentTenantId();
    }

    // Constructor for testing with explicit tenant ID
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options, Guid? currentTenantId) 
        : base(options)
    {
        _currentTenantId = currentTenantId;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Settings).HasColumnType("jsonb");
            
            entity.HasIndex(e => e.Slug)
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            
            entity.HasIndex(e => e.IsActive)
                .HasFilter("\"IsDeleted\" = false");
            
            entity.HasIndex(e => e.IsPlatformTenant)
                .IsUnique()
                .HasFilter("\"IsPlatformTenant\" = true AND \"IsDeleted\" = false");

            // Global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // TenantUser configuration
        modelBuilder.Entity<TenantUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.TenantUsers)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.UserId, e.TenantId })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            
            entity.HasIndex(e => e.UserId)
                .HasFilter("\"IsDeleted\" = false");
            
            entity.HasIndex(e => e.TenantId)
                .HasFilter("\"IsDeleted\" = false");

            // Global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Permissions).HasColumnType("jsonb");
            
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Roles)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.TenantId, e.Name })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            
            entity.HasIndex(e => e.Name)
                .HasFilter("\"IsDeleted\" = false");
            
            entity.HasIndex(e => e.IsSystemRole)
                .HasFilter("\"IsDeleted\" = false");

            // Global query filter for soft delete and tenant isolation
            entity.HasQueryFilter(e => !e.IsDeleted && 
                (_currentTenantId == null || e.TenantId == _currentTenantId));
        });

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AssignedBy).HasMaxLength(255);
            
            entity.HasOne(e => e.TenantUser)
                .WithMany(tu => tu.UserRoles)
                .HasForeignKey(e => e.TenantUserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.TenantUserId, e.RoleId })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            
            entity.HasIndex(e => e.TenantUserId)
                .HasFilter("\"IsDeleted\" = false");
            
            entity.HasIndex(e => e.RoleId)
                .HasFilter("\"IsDeleted\" = false");
            
            entity.HasIndex(e => e.ExpiresAt)
                .HasFilter("\"IsDeleted\" = false AND \"ExpiresAt\" IS NOT NULL");

            // Global query filter for soft delete
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Apply tenant-scoped query filters for business entities
        // Note: This would be expanded for other tenant-scoped entities
        // Example pattern for future entities:
        // modelBuilder.Entity<ExampleEntity>()
        //     .HasQueryFilter(e => !e.IsDeleted && 
        //         (_currentTenantId == null || e.TenantId == _currentTenantId));
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