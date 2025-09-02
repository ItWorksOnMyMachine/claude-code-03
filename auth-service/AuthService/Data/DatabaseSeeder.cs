using AuthService.Data.Entities;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

        // Migrate IdentityServer Configuration (only if using Entity Framework stores)
        var configContext = services.GetService<ConfigurationDbContext>();
        if (configContext != null)
        {
            await configContext.Database.MigrateAsync();
        }

        // Migrate IdentityServer Operational (only if using Entity Framework stores)
        var grantContext = services.GetService<PersistedGrantDbContext>();
        if (grantContext != null)
        {
            await grantContext.Database.MigrateAsync();
        }
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
        // Only seed if using Entity Framework stores
        var context = services.GetService<ConfigurationDbContext>();
        if (context == null)
        {
            return; // In-memory stores are used, skip database seeding
        }

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
                    ClientSecrets = { new Secret("DevClientSecret123!".Sha256()) },
                    
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RequireClientSecret = true,
                    RequireConsent = false,
                    
                    RedirectUris = {
                        // "http://localhost:5000/signin-oidc",
                        // "http://localhost:5000/callback",
                        // "https://localhost:5001/signin-oidc",
                        // "http://localhost:5086/signin-oidc",
                        // "http://localhost:5086/callback",
                        "https://host-bff.platform.local:5086/signin-oidc",
                        "https://host-bff.platform.local:5086/callback",
                    },
                    PostLogoutRedirectUris = {
                        // "http://localhost:5000/signout-callback-oidc",
                        // "http://localhost:5000/",
                        // "https://localhost:5001/signout-callback-oidc",
                        // "http://localhost:5086/signout-callback-oidc",
                        // "http://localhost:5086/",
                        "https://host-bff.platform.local:5086/signout-callback-oidc",
                        "https://host-bff.platform.local:5086/",
                    },
                    AllowedCorsOrigins = {
                        // "http://localhost:5000",
                        // "https://localhost:5001",
                        // "http://localhost:5086",
                         "https://host-bff.platform.local:5086",
                    },
                    
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "offline_access",
                        "platform.api"
                    },
                    
                    AllowOfflineAccess = true,
                    RefreshTokenUsage = TokenUsage.ReUse,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    SlidingRefreshTokenLifetime = 3600 * 24 * 30, // 30 days
                    
                    AccessTokenLifetime = 3600, // 1 hour
                    IdentityTokenLifetime = 3600, // 1 hour
                    AllowAccessTokensViaBrowser = false
                },
                
                // Platform Frontend Client (SPA)
                new Client
                {
                    ClientId = "platform-frontend",
                    ClientName = "Platform Frontend",
                    RequireClientSecret = false,
                    
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RequireConsent = false,
                    
                    RedirectUris = { 
                        "https://host-fe.platform.local:3002/callback",
                        "https://host-fe.platform.local:3002/silent-renew",
                        "https://host-fe.platform.local:3002/",
                        "http://localhost:3006/callback",
                        "http://localhost:3006/silent-renew",
                        "http://localhost:3006/"
                    },
                    PostLogoutRedirectUris = { 
                        "https://host-fe.platform.local:3002/",
                        "https://host-fe.platform.local:3002/logout",
                        "http://localhost:3006/",
                        "http://localhost:3006/logout"
                    },
                    AllowedCorsOrigins = { 
                        "https://host-fe.platform.local:3002",
                        "http://localhost:3000",
                        "http://localhost:3006"
                    },
                    
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "offline_access",
                        "platform.api"
                    },
                    
                    AllowOfflineAccess = true,
                    AllowAccessTokensViaBrowser = true,
                    AccessTokenLifetime = 900, // 15 minutes
                },
                
                // Development test client
                new Client
                {
                    ClientId = "test-client",
                    ClientName = "Test Client",
                    ClientSecrets = { new Secret("TestSecret123!".Sha256()) },
                    
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
                    RequireConsent = false,
                    RequirePkce = false,
                    
                    RedirectUris = { 
                        "http://localhost/callback",
                        "https://localhost/callback"
                    },
                    
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "offline_access",
                        "platform.api"
                    },
                    
                    AllowOfflineAccess = true,
                    AccessTokenLifetime = 3600
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
                new IdentityResources.Email(),
                new IdentityResources.Phone(),
                new IdentityResources.Address()
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
                new ApiScope("platform.api", "Platform API", new[] { "tenant_id", "role", "permission" }),
                new ApiScope("platform.read", "Platform Read", new[] { "tenant_id" }),
                new ApiScope("platform.write", "Platform Write", new[] { "tenant_id" }),
                new ApiScope("platform.admin", "Platform Admin", new[] { "tenant_id", "role" })
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
                new ApiResource("platform", "Platform API")
                {
                    Description = "Main Platform API resource",
                    Scopes = { "platform.api", "platform.read", "platform.write", "platform.admin" },
                    UserClaims = { "tenant_id", "role", "permission", JwtClaimTypes.Name, JwtClaimTypes.Email }
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