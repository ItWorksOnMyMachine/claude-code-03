using AuthService.Data.Entities;
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IdentityModel;

namespace AuthService.Data;

public static class DatabaseSeeder
{
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        // Apply migrations
        await MigrateDatabasesAsync(services);

        // Seed data
        await SeedIdentityDataAsync(services);
        await SeedIdentityServerDataAsync(services);
    }

    private static async Task MigrateDatabasesAsync(IServiceProvider services)
    {
        // Migrate AuthDbContext
        var authContext = services.GetRequiredService<AuthDbContext>();
        await authContext.Database.MigrateAsync();

        // Migrate IdentityServer Configuration
        var configContext = services.GetRequiredService<ConfigurationDbContext>();
        await configContext.Database.MigrateAsync();

        // Migrate IdentityServer Operational
        var grantContext = services.GetRequiredService<PersistedGrantDbContext>();
        await grantContext.Database.MigrateAsync();
    }

    private static async Task SeedIdentityDataAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AuthDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();

        // Seed roles
        var roles = new[] { "Admin", "User", "Manager" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new AppRole 
                { 
                    Name = roleName,
                    Description = $"Default {roleName} role",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Seed default users        
        // Admin user
        if (await userManager.FindByEmailAsync("admin@identity.local") == null)
        {
            var adminUser = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "admin",
                Email = "admin@identity.local",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User",
                DisplayName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim(JwtClaimTypes.Name, "Admin User"));
                await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim(JwtClaimTypes.GivenName, "Admin"));
                await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim(JwtClaimTypes.FamilyName, "User"));
            }
        }

        // Regular user
        if (await userManager.FindByEmailAsync("user@identity.local") == null)
        {
            var regularUser = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "testuser",
                Email = "user@identity.local",
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "User",
                DisplayName = "Test User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(regularUser, "User123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(regularUser, "User");
                await userManager.AddClaimAsync(regularUser, new System.Security.Claims.Claim(JwtClaimTypes.Name, "Test User"));
                await userManager.AddClaimAsync(regularUser, new System.Security.Claims.Claim(JwtClaimTypes.GivenName, "Test"));
                await userManager.AddClaimAsync(regularUser, new System.Security.Claims.Claim(JwtClaimTypes.FamilyName, "User"));
            }
        }
    }

    private static async Task SeedIdentityServerDataAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ConfigurationDbContext>();

        // Seed Clients
        if (!await context.Clients.AnyAsync())
        {
            var clients = new List<Client>
            {
                // Platform BFF client
                new Client
                {
                    ClientId = "platform-bff",
                    ClientName = "Platform BFF",
                    ClientSecrets = { new Secret("platform-bff-secret".Sha256()) },
                    
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RequireClientSecret = true,
                    
                    RedirectUris = { "http://localhost:5000/signin-oidc" },
                    PostLogoutRedirectUris = { "http://localhost:5000/signout-callback-oidc" },
                    AllowedCorsOrigins = { "http://localhost:5000" },
                    
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "platform-api"
                    },
                    
                    AllowOfflineAccess = true,
                    RefreshTokenUsage = TokenUsage.ReUse,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    SlidingRefreshTokenLifetime = 3600 * 24 * 30, // 30 days
                    
                    AccessTokenLifetime = 3600, // 1 hour
                    IdentityTokenLifetime = 3600, // 1 hour
                },
                
                // Development test client
                new Client
                {
                    ClientId = "test-client",
                    ClientName = "Test Client",
                    ClientSecrets = { new Secret("test-secret".Sha256()) },
                    
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    
                    AllowedScopes = new List<string>
                    {
                        "platform-api"
                    }
                }
            };

            foreach (var client in clients)
            {
                context.Clients.Add(client.ToEntity());
            }
            
            await context.SaveChangesAsync();
        }

        // Seed Identity Resources
        if (!await context.IdentityResources.AnyAsync())
        {
            var identityResources = new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            };

            foreach (var resource in identityResources)
            {
                context.IdentityResources.Add(resource.ToEntity());
            }
            
            await context.SaveChangesAsync();
        }

        // Seed API Scopes
        if (!await context.ApiScopes.AnyAsync())
        {
            var apiScopes = new List<ApiScope>
            {
                new ApiScope("platform-api", "Platform API", new List<string> { JwtClaimTypes.Name, JwtClaimTypes.Email })
            };

            foreach (var scope in apiScopes)
            {
                context.ApiScopes.Add(scope.ToEntity());
            }
            
            await context.SaveChangesAsync();
        }

        // Seed API Resources
        if (!await context.ApiResources.AnyAsync())
        {
            var apiResources = new List<ApiResource>
            {
                new ApiResource("platform-api-resource", "Platform API Resource")
                {
                    Scopes = { "platform-api" },
                    UserClaims = { JwtClaimTypes.Name, JwtClaimTypes.Email }
                }
            };

            foreach (var resource in apiResources)
            {
                context.ApiResources.Add(resource.ToEntity());
            }
            
            await context.SaveChangesAsync();
        }
    }
}