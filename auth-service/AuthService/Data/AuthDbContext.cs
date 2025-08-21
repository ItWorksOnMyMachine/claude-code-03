using AuthService.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class AuthDbContext : IdentityDbContext<AppUser, AppRole, string>
{
    public DbSet<AuthenticationAuditLog> AuthenticationAuditLogs { get; set; } = null!;
    public DbSet<PasswordHistory> PasswordHistories { get; set; } = null!;

    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure table names
        builder.Entity<AppUser>().ToTable("Users", "auth");
        builder.Entity<AppRole>().ToTable("Roles", "auth");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "auth");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "auth");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "auth");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "auth");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "auth");

        // Configure AppUser entity
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.CreatedAt)
                .IsRequired();
            
            entity.Property(u => u.UpdatedAt);
            
            entity.Property(u => u.LastLoginAt);
            
            entity.Property(u => u.LastPasswordChangeAt);
        });
        
        // Configure AppRole entity
        builder.Entity<AppRole>(entity =>
        {
            entity.Property(r => r.CreatedAt)
                .IsRequired();
            
            entity.Property(r => r.UpdatedAt);
            
            entity.Property(r => r.IsSystemRole)
                .HasDefaultValue(false);
        });

        // Configure AuthenticationAuditLog entity
        builder.Entity<AuthenticationAuditLog>(entity =>
        {
            entity.ToTable("AuthenticationAuditLogs", "auth");
            
            entity.HasKey(a => a.Id);
            
            entity.Property(a => a.UserId)
                .IsRequired()
                .HasMaxLength(450);
            
            entity.Property(a => a.EventType)
                .IsRequired();
            
            entity.Property(a => a.Timestamp)
                .IsRequired();
            
            entity.Property(a => a.IpAddress)
                .HasMaxLength(45); // IPv6 max length
            
            entity.Property(a => a.UserAgent)
                .HasMaxLength(500);
            
            entity.Property(a => a.SessionId)
                .HasMaxLength(100);
            
            entity.Property(a => a.FailureReason)
                .HasMaxLength(500);
            
            entity.Property(a => a.AdditionalData)
                .HasColumnType("jsonb"); // PostgreSQL JSON type
            
            entity.HasIndex(a => a.UserId)
                .HasDatabaseName("IX_AuthenticationAuditLogs_UserId");
            
            entity.HasIndex(a => a.Timestamp)
                .HasDatabaseName("IX_AuthenticationAuditLogs_Timestamp");
            
            entity.HasIndex(a => new { a.UserId, a.Timestamp })
                .HasDatabaseName("IX_AuthenticationAuditLogs_User_Timestamp");
            
            // Navigation properties
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PasswordHistory entity
        builder.Entity<PasswordHistory>(entity =>
        {
            entity.ToTable("PasswordHistories", "auth");
            
            entity.HasKey(p => p.Id);
            
            entity.Property(p => p.UserId)
                .IsRequired()
                .HasMaxLength(450);
            
            entity.Property(p => p.PasswordHash)
                .IsRequired();
            
            entity.Property(p => p.CreatedAt)
                .IsRequired();
            
            entity.HasIndex(p => p.UserId)
                .HasDatabaseName("IX_PasswordHistories_UserId");
            
            entity.HasIndex(p => new { p.UserId, p.CreatedAt })
                .HasDatabaseName("IX_PasswordHistories_UserId_CreatedAt");
            
            // Navigation property
            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (IAuditableEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}