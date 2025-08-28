using Microsoft.EntityFrameworkCore;
using PlatformBff.Data.Entities;

namespace PlatformBff.Data;

/// <summary>
/// Consolidated database seeder for the Platform BFF
/// Handles all database initialization and seeding
/// </summary>
public static class DatabaseSeeder
{
    // Fixed GUIDs for consistent seeding
    public static readonly Guid PLATFORM_TENANT_ID = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid PLATFORM_ADMIN_ROLE_ID = Guid.Parse("00000000-0000-0000-0000-000000000010");
    public static readonly Guid PLATFORM_SUPPORT_ROLE_ID = Guid.Parse("00000000-0000-0000-0000-000000000011");
    public static readonly Guid DEMO_TENANT_ID = Guid.Parse("00000000-0000-0000-0000-000000000002");

    /// <summary>
    /// Initialize database with migrations and seed data
    /// </summary>
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider, IHostEnvironment environment)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();

        try
        {
            // Apply migrations
            logger?.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger?.LogInformation("Database migrations applied successfully");

            // Seed data
            await SeedPlatformTenantAsync(context);
            
            if (environment.IsDevelopment())
            {
                await SeedDemoTenantAsync(context);
                await AssignDevelopmentAdminAsync(context);
            }

            logger?.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error initializing database");
            throw;
        }
    }

    /// <summary>
    /// Seeds the platform administration tenant and roles
    /// </summary>
    private static async Task SeedPlatformTenantAsync(PlatformDbContext context)
    {
        // Check if platform tenant exists
        var platformTenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == PLATFORM_TENANT_ID);

        if (platformTenant == null)
        {
            platformTenant = new Tenant
            {
                Id = PLATFORM_TENANT_ID,
                Name = "platform-admin",
                Slug = "platform-admin",
                DisplayName = "Platform Administration",
                IsPlatformTenant = true,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System",
                Settings = null
            };

            context.Tenants.Add(platformTenant);
            await context.SaveChangesAsync();
        }

        // Seed platform admin role
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
                Permissions = """["*"]""", // Full permissions
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            };

            context.Roles.Add(adminRole);
        }

        // Seed platform support role
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
                Permissions = """["tenants.view", "users.view", "logs.view", "reports.view"]""",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            };

            context.Roles.Add(supportRole);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds a demo tenant for development environment
    /// </summary>
    private static async Task SeedDemoTenantAsync(PlatformDbContext context)
    {
        var demoTenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == DEMO_TENANT_ID);

        if (demoTenant == null)
        {
            demoTenant = new Tenant
            {
                Id = DEMO_TENANT_ID,
                Name = "Demo Company",
                Slug = "demo-company",
                DisplayName = "Demo Company Inc.",
                IsPlatformTenant = false,
                IsActive = true,
                CreatedBy = "System",
                Settings = """{"plan": "enterprise", "users": 100}""",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            context.Tenants.Add(demoTenant);
            await context.SaveChangesAsync();

            // Create demo tenant roles
            await CreateTenantRolesAsync(context, DEMO_TENANT_ID);
        }
    }

    /// <summary>
    /// Creates standard roles for a tenant
    /// </summary>
    private static async Task CreateTenantRolesAsync(PlatformDbContext context, Guid tenantId)
    {
        var roles = new[]
        {
            new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Admin",
                DisplayName = "Administrator",
                Description = "Tenant administration access",
                IsSystemRole = true,
                Permissions = """["users.manage", "settings.manage", "billing.view", "reports.view"]""",
                CreatedBy = "System",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Manager",
                DisplayName = "Manager",
                Description = "Management access",
                IsSystemRole = true,
                Permissions = """["users.view", "reports.view", "content.manage"]""",
                CreatedBy = "System",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Member",
                DisplayName = "Member",
                Description = "Standard member access",
                IsSystemRole = true,
                Permissions = """["profile.edit", "content.view"]""",
                CreatedBy = "System",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        foreach (var role in roles)
        {
            var existingRole = await context.Roles
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == role.Name);

            if (existingRole == null)
            {
                context.Roles.Add(role);
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Assigns the development admin user to platform admin role
    /// </summary>
    private static async Task AssignDevelopmentAdminAsync(PlatformDbContext context)
    {
        // This is the admin user from auth service
        const string adminUserId = "e4ee8e51-0279-4c19-8f36-8f7b616e9f09";
        const string adminEmail = "admin@identity.local";

        await AssignUserToPlatformAdminAsync(context, adminUserId, adminEmail);
    }

    /// <summary>
    /// Assigns a user to the platform admin tenant with admin role
    /// </summary>
    public static async Task AssignUserToPlatformAdminAsync(PlatformDbContext context, string userId, string email)
    {
        // Check if user already exists in platform tenant
        var existingUser = await context.TenantUsers
            .FirstOrDefaultAsync(tu =>
                tu.UserId == userId &&
                tu.TenantId == PLATFORM_TENANT_ID);

        if (existingUser == null)
        {
            // Create tenant user
            existingUser = new TenantUser
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

            context.TenantUsers.Add(existingUser);
            await context.SaveChangesAsync();
        }

        // Ensure user has admin role
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
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "System"
            });

            await context.SaveChangesAsync();
        }
    }
}