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

        // Seed platform tenant if it doesn't exist
        var platformTenantId = new Guid("00000000-0000-0000-0000-000000000001");
        var platformTenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == platformTenantId);

        if (platformTenant == null)
        {
            platformTenant = new Tenant
            {
                Id = platformTenantId,
                Name = "Platform Administration",
                Slug = "platform-admin",
                DisplayName = "Platform Administration",
                IsPlatformTenant = true,
                IsActive = true,
                CreatedBy = "system",
                Settings = """{"type": "platform", "features": ["cross-tenant-access", "admin-tools"]}""",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            context.Tenants.Add(platformTenant);
            await context.SaveChangesAsync();

            // Create default roles for platform tenant
            var adminRole = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = platformTenantId,
                Name = "Admin",
                DisplayName = "Administrator",
                Description = "Full platform administration access",
                IsSystemRole = true,
                Permissions = """["*"]""",
                CreatedBy = "system",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var supportRole = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = platformTenantId,
                Name = "Support",
                DisplayName = "Support Agent",
                Description = "Customer support access",
                IsSystemRole = true,
                Permissions = """["tenant.view", "tenant.impersonate", "user.view"]""",
                CreatedBy = "system",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            context.Roles.AddRange(adminRole, supportRole);
            await context.SaveChangesAsync();
        }

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

                // Create default roles for demo tenant
                var tenantAdminRole = new Role
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

                var memberRole = new Role
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

                context.Roles.AddRange(tenantAdminRole, memberRole);
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