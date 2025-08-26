using Microsoft.EntityFrameworkCore;
using PlatformBff.Data.Entities;
using System;
using System.Threading.Tasks;

namespace PlatformBff.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(PlatformDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Platform tenant seeding is handled by PlatformTenantSeeder.SeedAsync
        // to avoid duplication and ensure consistent fixed GUIDs

        // Seed a demo tenant if needed (for development)
        if (DbContextExtensions.HostingEnvironment?.IsDevelopment() ?? false)
        {
            var demoTenantId = new Guid("00000000-0000-0000-0000-000000000002");
            var demoTenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == demoTenantId);

            if (demoTenant == null)
            {
                demoTenant = new Tenant
                {
                    Id = demoTenantId,
                    Name = "Demo Company",
                    Slug = "demo-company",
                    DisplayName = "Demo Company Inc.",
                    IsPlatformTenant = false,
                    IsActive = true,
                    CreatedBy = "system",
                    Settings = """{"plan": "enterprise", "users": 100}""",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                context.Tenants.Add(demoTenant);
                await context.SaveChangesAsync();

                // Create default roles for demo tenant if they don't exist
                var tenantAdminRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.TenantId == demoTenantId && r.Name == "Admin");
                
                if (tenantAdminRole == null)
                {
                    tenantAdminRole = new Role
                    {
                        Id = Guid.NewGuid(),
                        TenantId = demoTenantId,
                        Name = "Admin",
                        DisplayName = "Administrator",
                        Description = "Tenant administration access",
                        IsSystemRole = true,
                        Permissions = """["users.manage", "settings.manage", "billing.view"]""",
                        CreatedBy = "system",
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    context.Roles.Add(tenantAdminRole);
                }

                var memberRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.TenantId == demoTenantId && r.Name == "Member");
                
                if (memberRole == null)
                {
                    memberRole = new Role
                    {
                        Id = Guid.NewGuid(),
                        TenantId = demoTenantId,
                        Name = "Member",
                        DisplayName = "Member",
                        Description = "Standard member access",
                        IsSystemRole = true,
                        Permissions = """["profile.edit", "content.view"]""",
                        CreatedBy = "system",
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    context.Roles.Add(memberRole);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}

// Extension to check environment
public static class DbContextExtensions
{
    public static IHostEnvironment? HostingEnvironment { get; set; }
}