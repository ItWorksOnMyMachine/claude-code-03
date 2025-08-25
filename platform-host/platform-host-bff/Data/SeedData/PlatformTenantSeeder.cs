using Microsoft.EntityFrameworkCore;
using PlatformBff.Data.Entities;

namespace PlatformBff.Data.SeedData;

/// <summary>
/// Seeds the platform administration tenant and initial admin roles
/// </summary>
public static class PlatformTenantSeeder
{
    // Fixed GUID for platform administration tenant
    public static readonly Guid PLATFORM_TENANT_ID = Guid.Parse("00000000-0000-0000-0000-000000000001");
    
    // Fixed GUIDs for platform roles
    public static readonly Guid PLATFORM_ADMIN_ROLE_ID = Guid.Parse("00000000-0000-0000-0000-000000000010");
    public static readonly Guid PLATFORM_SUPPORT_ROLE_ID = Guid.Parse("00000000-0000-0000-0000-000000000011");
    
    public static async Task SeedAsync(PlatformDbContext context)
    {
        // Check if platform tenant already exists
        var platformTenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == PLATFORM_TENANT_ID);
            
        if (platformTenant == null)
        {
            // Create platform tenant
            platformTenant = new Tenant
            {
                Id = PLATFORM_TENANT_ID,
                Name = "platform-admin",
                Slug = "platform-admin",
                DisplayName = "Platform Administration",
                IsPlatformTenant = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                Settings = null
            };
            
            context.Tenants.Add(platformTenant);
            await context.SaveChangesAsync();
        }
        
        // Check if platform admin role exists
        var adminRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Id == PLATFORM_ADMIN_ROLE_ID);
            
        if (adminRole == null)
        {
            adminRole = new Role
            {
                Id = PLATFORM_ADMIN_ROLE_ID,
                TenantId = PLATFORM_TENANT_ID,
                Name = "Admin",
                DisplayName = "Administrator",
                Description = "Platform administrator with full system access",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            
            context.Roles.Add(adminRole);
        }
        
        // Check if platform support role exists
        var supportRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Id == PLATFORM_SUPPORT_ROLE_ID);
            
        if (supportRole == null)
        {
            supportRole = new Role
            {
                Id = PLATFORM_SUPPORT_ROLE_ID,
                TenantId = PLATFORM_TENANT_ID,
                Name = "Support",
                DisplayName = "Support Staff",
                Description = "Platform support staff with read-only cross-tenant access",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            
            context.Roles.Add(supportRole);
        }
        
        await context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Assigns a user to the platform admin tenant with admin role
    /// </summary>
    public static async Task AssignPlatformAdminAsync(PlatformDbContext context, string userId, string email)
    {
        // Check if user already exists in platform tenant
        var existingUser = await context.TenantUsers
            .FirstOrDefaultAsync(tu => 
                tu.UserId == userId && 
                tu.TenantId == PLATFORM_TENANT_ID);
                
        if (existingUser != null)
        {
            // User already exists, ensure they have admin role
            var hasAdminRole = await context.UserRoles
                .AnyAsync(ur => 
                    ur.TenantUserId == existingUser.Id && 
                    ur.RoleId == PLATFORM_ADMIN_ROLE_ID);
                    
            if (!hasAdminRole)
            {
                context.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    TenantUserId = existingUser.Id,
                    RoleId = PLATFORM_ADMIN_ROLE_ID,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                });
                
                await context.SaveChangesAsync();
            }
            
            return;
        }
        
        // Create new platform admin user
        var tenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            TenantId = PLATFORM_TENANT_ID,
            UserId = userId,
            IsActive = true,
            JoinedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "System"
        };
        
        context.TenantUsers.Add(tenantUser);
        
        // Assign admin role
        context.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            TenantUserId = tenantUser.Id,
            RoleId = PLATFORM_ADMIN_ROLE_ID,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        });
        
        await context.SaveChangesAsync();
    }
}