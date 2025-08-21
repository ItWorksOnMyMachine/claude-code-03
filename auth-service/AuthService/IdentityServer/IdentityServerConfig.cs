using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace AuthService.IdentityServer;

/// <summary>
/// Configuration for IdentityServer clients, resources, and scopes
/// </summary>
public static class IdentityServerConfig
{
    /// <summary>
    /// Gets the identity resources (user claims)
    /// </summary>
    public static IEnumerable<IdentityResource> IdentityResources =>
        new List<IdentityResource>
        {
            new IdentityResources.OpenId(), // Required for OpenID Connect
            new IdentityResources.Profile(), // User profile claims
            new IdentityResources.Email(), // Email claims
            new IdentityResource(
                name: "tenant",
                displayName: "Tenant Information",
                userClaims: new[] { "tenant_id", "organization", "tenant_subdomain" }
            ),
            new IdentityResource(
                name: "roles",
                displayName: "User Roles",
                userClaims: new[] { "role" }
            )
        };

    /// <summary>
    /// Gets the API scopes (what clients can access)
    /// </summary>
    public static IEnumerable<ApiScope> ApiScopes =>
        new List<ApiScope>
        {
            new ApiScope("api", "Platform API", new[] 
            { 
                "tenant_id", 
                "organization", 
                "role" 
            }),
            new ApiScope("api.read", "Read access to Platform API"),
            new ApiScope("api.write", "Write access to Platform API"),
            new ApiScope("admin", "Administrative access", new[]
            {
                "tenant_id",
                "organization",
                "role",
                "admin_level"
            })
        };

    /// <summary>
    /// Gets the API resources
    /// </summary>
    public static IEnumerable<ApiResource> ApiResources =>
        new List<ApiResource>
        {
            new ApiResource("platform-api", "Platform API")
            {
                Scopes = { "api", "api.read", "api.write" },
                UserClaims = { "tenant_id", "organization", "role" }
            },
            new ApiResource("admin-api", "Administrative API")
            {
                Scopes = { "admin" },
                UserClaims = { "tenant_id", "organization", "role", "admin_level" },
                RequireResourceIndicator = true
            }
        };

    /// <summary>
    /// Gets the clients (applications that can request tokens)
    /// </summary>
    public static IEnumerable<Client> GetClients(bool isDevelopment = true)
    {
        var clients = new List<Client>();

        // Platform BFF Client (as specified in the spec)
        clients.Add(new Client
        {
            ClientId = "platform-bff",
            ClientName = "Platform Backend for Frontend",
            Description = "The main platform BFF that handles authentication for the frontend",
            
            // Use authorization code flow with PKCE (as per spec)
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = true,
            ClientSecrets = 
            {
                new Secret(isDevelopment 
                    ? "development-secret".Sha256() 
                    : "production-secret-should-be-replaced".Sha256())
            },
            
            // Token configuration (as per spec)
            AccessTokenLifetime = 300, // 5 minutes
            AuthorizationCodeLifetime = 300, // 5 minutes
            IdentityTokenLifetime = 300, // 5 minutes
            
            // Refresh token configuration (as per spec - 1 hour sliding)
            AllowOfflineAccess = true,
            RefreshTokenUsage = TokenUsage.OneTimeOnly, // Rotation
            RefreshTokenExpiration = TokenExpiration.Sliding,
            SlidingRefreshTokenLifetime = 3600, // 1 hour
            AbsoluteRefreshTokenLifetime = 86400, // 24 hours max
            
            // Allowed scopes
            AllowedScopes = 
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                IdentityServerConstants.StandardScopes.Email,
                IdentityServerConstants.StandardScopes.OfflineAccess,
                "tenant",
                "roles",
                "api",
                "api.read",
                "api.write"
            },
            
            // Redirect URIs for the BFF
            RedirectUris = isDevelopment
                ? new[] 
                { 
                    "http://localhost:5000/signin-oidc",
                    "https://localhost:5001/signin-oidc",
                    "http://localhost:3002/auth/callback" // Frontend callback
                }
                : new[] 
                { 
                    "https://platform.example.com/signin-oidc" 
                },
            
            // Post logout redirect URIs
            PostLogoutRedirectUris = isDevelopment
                ? new[] 
                { 
                    "http://localhost:5000/signout-callback-oidc",
                    "https://localhost:5001/signout-callback-oidc",
                    "http://localhost:3002/"
                }
                : new[] 
                { 
                    "https://platform.example.com/signout-callback-oidc" 
                },
            
            // CORS origins for the frontend
            AllowedCorsOrigins = isDevelopment
                ? new[] 
                { 
                    "http://localhost:3002", // Frontend
                    "http://localhost:5000", // BFF
                    "https://localhost:5001" // BFF HTTPS
                }
                : new[] 
                { 
                    "https://platform.example.com" 
                },
            
            // Additional settings
            AllowPlainTextPkce = false, // Require S256
            RequireConsent = false, // Trusted first-party client
            AlwaysIncludeUserClaimsInIdToken = false,
            UpdateAccessTokenClaimsOnRefresh = true
        });

        // Development test client for integration testing
        if (isDevelopment)
        {
            clients.Add(new Client
            {
                ClientId = "test-client",
                ClientName = "Integration Test Client",
                
                // Allow both code and client credentials for testing
                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                RequirePkce = true,
                RequireClientSecret = true,
                ClientSecrets = 
                {
                    new Secret("test-secret".Sha256())
                },
                
                // Short token lifetime for testing
                AccessTokenLifetime = 60,
                
                // Test scopes
                AllowedScopes = 
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "api",
                    "api.read"
                },
                
                // Test redirect URIs
                RedirectUris = 
                { 
                    "http://localhost:5000/test-callback",
                    "http://localhost/test-callback", // For WebApplicationFactory tests
                    "https://oauth.pstmn.io/v1/callback" // Postman
                },
                
                PostLogoutRedirectUris = 
                { 
                    "http://localhost:5000/test-logout" 
                },
                
                AllowedCorsOrigins = 
                { 
                    "http://localhost:5000" 
                }
            });

            // Trusted client for password grant testing
            clients.Add(new Client
            {
                ClientId = "trusted-client",
                ClientName = "Trusted Test Client",
                Description = "Client for testing password grant and refresh tokens",
                
                // Allow password grant and refresh tokens
                AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
                RequireClientSecret = true,
                ClientSecrets = 
                {
                    new Secret("trusted-secret".Sha256())
                },
                
                // Token configuration
                AccessTokenLifetime = 300, // 5 minutes
                
                // Refresh token configuration
                AllowOfflineAccess = true,
                RefreshTokenUsage = TokenUsage.OneTimeOnly,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 3600, // 1 hour
                AbsoluteRefreshTokenLifetime = 86400, // 24 hours
                
                // Allowed scopes
                AllowedScopes = 
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OfflineAccess,
                    "api",
                    "api.read",
                    "api.write"
                },
                
                // Additional settings
                UpdateAccessTokenClaimsOnRefresh = true
            });

            // Machine-to-machine client for client credentials flow
            clients.Add(new Client
            {
                ClientId = "machine-client",
                ClientName = "Machine Client",
                Description = "Service-to-service authentication client",
                
                // Only allow client credentials flow
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                RequireClientSecret = true,
                ClientSecrets = 
                {
                    new Secret("machine-secret".Sha256())
                },
                
                // Token configuration
                AccessTokenLifetime = 300, // 5 minutes
                
                // Allowed scopes - no user scopes for machine clients
                AllowedScopes = 
                {
                    "api",
                    "api.read",
                    "api.write"
                }
            });

            // Admin client for development
            clients.Add(new Client
            {
                ClientId = "admin-client",
                ClientName = "Administrative Client",
                
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = true,
                ClientSecrets = 
                {
                    new Secret("admin-secret".Sha256())
                },
                
                AccessTokenLifetime = 1800, // 30 minutes for admin
                
                AllowOfflineAccess = true,
                RefreshTokenUsage = TokenUsage.OneTimeOnly,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 7200, // 2 hours for admin
                
                AllowedScopes = 
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OfflineAccess,
                    "tenant",
                    "roles",
                    "api",
                    "api.read",
                    "api.write",
                    "admin"
                },
                
                RedirectUris = 
                { 
                    "http://localhost:5000/admin/callback" 
                },
                
                PostLogoutRedirectUris = 
                { 
                    "http://localhost:5000/admin" 
                }
            });
        }

        return clients;
    }
}