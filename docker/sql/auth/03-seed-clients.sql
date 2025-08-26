-- Seed OAuth Clients for Development
-- Creates the necessary OAuth clients for the platform

BEGIN;

-- Platform BFF Client (Server-side web application)
INSERT INTO "Clients" ("ClientId", "ClientName", "Description", "RequireConsent", "AllowOfflineAccess", "RequirePkce", "AllowAccessTokensViaBrowser")
VALUES ('platform-bff', 'Platform BFF', 'Backend for Frontend service', false, true, true, false)
ON CONFLICT ("ClientId") DO UPDATE SET
    "ClientName" = EXCLUDED."ClientName",
    "Updated" = CURRENT_TIMESTAMP;

-- Get the client ID for foreign key relationships
DO $$
DECLARE
    bff_client_id INTEGER;
    frontend_client_id INTEGER;
    test_client_id INTEGER;
BEGIN
    -- Platform BFF Client
    SELECT "Id" INTO bff_client_id FROM "Clients" WHERE "ClientId" = 'platform-bff';
    
    -- Add client secret (hashed value of 'DevClientSecret123!' - in production use proper hashing)
    INSERT INTO "ClientSecrets" ("ClientId", "Value", "Type", "Description")
    VALUES (bff_client_id, 'DevClientSecret123!', 'SharedSecret', 'Development secret')
    ON CONFLICT DO NOTHING;
    
    -- Add grant types
    INSERT INTO "ClientGrantTypes" ("ClientId", "GrantType")
    VALUES 
        (bff_client_id, 'authorization_code'),
        (bff_client_id, 'refresh_token')
    ON CONFLICT DO NOTHING;
    
    -- Add redirect URIs
    INSERT INTO "ClientRedirectUris" ("ClientId", "RedirectUri")
    VALUES 
        (bff_client_id, 'http://localhost:5000/signin-oidc'),
        (bff_client_id, 'http://localhost:5000/callback'),
        (bff_client_id, 'https://localhost:5001/signin-oidc')
    ON CONFLICT DO NOTHING;
    
    -- Add post logout redirect URIs
    INSERT INTO "ClientPostLogoutRedirectUris" ("ClientId", "PostLogoutRedirectUri")
    VALUES 
        (bff_client_id, 'http://localhost:5000/signout-callback-oidc'),
        (bff_client_id, 'http://localhost:5000/'),
        (bff_client_id, 'https://localhost:5001/signout-callback-oidc')
    ON CONFLICT DO NOTHING;
    
    -- Add scopes
    INSERT INTO "ClientScopes" ("ClientId", "Scope")
    VALUES 
        (bff_client_id, 'openid'),
        (bff_client_id, 'profile'),
        (bff_client_id, 'email'),
        (bff_client_id, 'offline_access'),
        (bff_client_id, 'platform.api')
    ON CONFLICT DO NOTHING;
    
    -- Add CORS origins
    INSERT INTO "ClientCorsOrigins" ("ClientId", "Origin")
    VALUES 
        (bff_client_id, 'http://localhost:5000'),
        (bff_client_id, 'https://localhost:5001')
    ON CONFLICT DO NOTHING;
END $$;

-- Platform Frontend Client (SPA)
INSERT INTO "Clients" ("ClientId", "ClientName", "Description", "RequireConsent", "RequireClientSecret", 
                       "AllowOfflineAccess", "RequirePkce", "AllowAccessTokensViaBrowser", "AccessTokenLifetime")
VALUES ('platform-frontend', 'Platform Frontend', 'React SPA application', false, false, true, true, true, 900)
ON CONFLICT ("ClientId") DO UPDATE SET
    "ClientName" = EXCLUDED."ClientName",
    "Updated" = CURRENT_TIMESTAMP;

DO $$
DECLARE
    frontend_client_id INTEGER;
BEGIN
    SELECT "Id" INTO frontend_client_id FROM "Clients" WHERE "ClientId" = 'platform-frontend';
    
    -- Add grant types for SPA
    INSERT INTO "ClientGrantTypes" ("ClientId", "GrantType")
    VALUES 
        (frontend_client_id, 'authorization_code'),
        (frontend_client_id, 'refresh_token')
    ON CONFLICT DO NOTHING;
    
    -- Add redirect URIs for SPA
    INSERT INTO "ClientRedirectUris" ("ClientId", "RedirectUri")
    VALUES 
        (frontend_client_id, 'http://localhost:3002/callback'),
        (frontend_client_id, 'http://localhost:3002/silent-renew'),
        (frontend_client_id, 'http://localhost:3002/')
    ON CONFLICT DO NOTHING;
    
    -- Add post logout redirect URIs for SPA
    INSERT INTO "ClientPostLogoutRedirectUris" ("ClientId", "PostLogoutRedirectUri")
    VALUES 
        (frontend_client_id, 'http://localhost:3002/'),
        (frontend_client_id, 'http://localhost:3002/logout')
    ON CONFLICT DO NOTHING;
    
    -- Add scopes for SPA
    INSERT INTO "ClientScopes" ("ClientId", "Scope")
    VALUES 
        (frontend_client_id, 'openid'),
        (frontend_client_id, 'profile'),
        (frontend_client_id, 'email'),
        (frontend_client_id, 'offline_access'),
        (frontend_client_id, 'platform.api')
    ON CONFLICT DO NOTHING;
    
    -- Add CORS origins for SPA
    INSERT INTO "ClientCorsOrigins" ("ClientId", "Origin")
    VALUES 
        (frontend_client_id, 'http://localhost:3002'),
        (frontend_client_id, 'http://localhost:3000')
    ON CONFLICT DO NOTHING;
END $$;

-- Test Client (for integration testing)
INSERT INTO "Clients" ("ClientId", "ClientName", "Description", "RequireConsent", "AllowOfflineAccess", 
                       "RequirePkce", "AccessTokenLifetime")
VALUES ('test-client', 'Test Client', 'Client for integration testing', false, true, false, 3600)
ON CONFLICT ("ClientId") DO UPDATE SET
    "ClientName" = EXCLUDED."ClientName",
    "Updated" = CURRENT_TIMESTAMP;

DO $$
DECLARE
    test_client_id INTEGER;
BEGIN
    SELECT "Id" INTO test_client_id FROM "Clients" WHERE "ClientId" = 'test-client';
    
    -- Add client secret for testing
    INSERT INTO "ClientSecrets" ("ClientId", "Value", "Type", "Description")
    VALUES (test_client_id, 'TestSecret123!', 'SharedSecret', 'Test client secret')
    ON CONFLICT DO NOTHING;
    
    -- Add grant types for testing
    INSERT INTO "ClientGrantTypes" ("ClientId", "GrantType")
    VALUES 
        (test_client_id, 'client_credentials'),
        (test_client_id, 'password'),
        (test_client_id, 'authorization_code'),
        (test_client_id, 'refresh_token')
    ON CONFLICT DO NOTHING;
    
    -- Add redirect URIs for testing
    INSERT INTO "ClientRedirectUris" ("ClientId", "RedirectUri")
    VALUES 
        (test_client_id, 'http://localhost/callback'),
        (test_client_id, 'https://localhost/callback')
    ON CONFLICT DO NOTHING;
    
    -- Add scopes for testing
    INSERT INTO "ClientScopes" ("ClientId", "Scope")
    VALUES 
        (test_client_id, 'openid'),
        (test_client_id, 'profile'),
        (test_client_id, 'email'),
        (test_client_id, 'offline_access'),
        (test_client_id, 'platform.api')
    ON CONFLICT DO NOTHING;
END $$;

-- Insert Identity Resources
INSERT INTO "IdentityResources" ("Name", "DisplayName", "Description", "Required", "Emphasize")
VALUES 
    ('openid', 'Your user identifier', 'Your unique identifier', true, false),
    ('profile', 'User profile', 'Your user profile information (name, etc.)', false, true),
    ('email', 'Your email address', 'Your email address', false, true),
    ('phone', 'Your phone number', 'Your phone number', false, false),
    ('address', 'Your address', 'Your postal address', false, false)
ON CONFLICT ("Name") DO NOTHING;

-- Add claims to identity resources
DO $$
BEGIN
    -- OpenID claims
    INSERT INTO "IdentityResourceClaims" ("IdentityResourceId", "Type")
    SELECT ir."Id", 'sub' FROM "IdentityResources" ir WHERE ir."Name" = 'openid'
    ON CONFLICT DO NOTHING;
    
    -- Profile claims
    INSERT INTO "IdentityResourceClaims" ("IdentityResourceId", "Type")
    SELECT ir."Id", claim FROM "IdentityResources" ir,
    (VALUES ('name'), ('family_name'), ('given_name'), ('middle_name'), ('nickname'),
            ('preferred_username'), ('profile'), ('picture'), ('website'), ('gender'),
            ('birthdate'), ('zoneinfo'), ('locale'), ('updated_at')) AS claims(claim)
    WHERE ir."Name" = 'profile'
    ON CONFLICT DO NOTHING;
    
    -- Email claims
    INSERT INTO "IdentityResourceClaims" ("IdentityResourceId", "Type")
    SELECT ir."Id", claim FROM "IdentityResources" ir,
    (VALUES ('email'), ('email_verified')) AS claims(claim)
    WHERE ir."Name" = 'email'
    ON CONFLICT DO NOTHING;
    
    -- Phone claims
    INSERT INTO "IdentityResourceClaims" ("IdentityResourceId", "Type")
    SELECT ir."Id", claim FROM "IdentityResources" ir,
    (VALUES ('phone_number'), ('phone_number_verified')) AS claims(claim)
    WHERE ir."Name" = 'phone'
    ON CONFLICT DO NOTHING;
    
    -- Address claims
    INSERT INTO "IdentityResourceClaims" ("IdentityResourceId", "Type")
    SELECT ir."Id", 'address' FROM "IdentityResources" ir WHERE ir."Name" = 'address'
    ON CONFLICT DO NOTHING;
END $$;

-- Insert API Scopes
INSERT INTO "ApiScopes" ("Name", "DisplayName", "Description", "Required", "Emphasize")
VALUES 
    ('platform.api', 'Platform API', 'Access to the Platform API', false, false),
    ('platform.read', 'Platform Read', 'Read access to platform resources', false, false),
    ('platform.write', 'Platform Write', 'Write access to platform resources', false, false),
    ('platform.admin', 'Platform Admin', 'Administrative access to platform', false, true)
ON CONFLICT ("Name") DO NOTHING;

-- Add claims to API scopes
DO $$
BEGIN
    INSERT INTO "ApiScopeClaims" ("ScopeId", "Type")
    SELECT s."Id", claim FROM "ApiScopes" s,
    (VALUES ('tenant_id'), ('role'), ('permission')) AS claims(claim)
    WHERE s."Name" = 'platform.api'
    ON CONFLICT DO NOTHING;
END $$;

-- Insert API Resources
INSERT INTO "ApiResources" ("Name", "DisplayName", "Description")
VALUES 
    ('platform', 'Platform API', 'Main Platform API resource')
ON CONFLICT ("Name") DO NOTHING;

-- Add scopes to API resources
DO $$
BEGIN
    INSERT INTO "ApiResourceScopes" ("ApiResourceId", "Scope")
    SELECT r."Id", scope FROM "ApiResources" r,
    (VALUES ('platform.api'), ('platform.read'), ('platform.write'), ('platform.admin')) AS scopes(scope)
    WHERE r."Name" = 'platform'
    ON CONFLICT DO NOTHING;
    
    -- Add claims to API resource
    INSERT INTO "ApiResourceClaims" ("ApiResourceId", "Type")
    SELECT r."Id", claim FROM "ApiResources" r,
    (VALUES ('tenant_id'), ('role'), ('permission'), ('name'), ('email')) AS claims(claim)
    WHERE r."Name" = 'platform'
    ON CONFLICT DO NOTHING;
END $$;

COMMIT;

DO $$
BEGIN
    RAISE NOTICE 'OAuth clients and resources seeded successfully.';
    RAISE NOTICE '';
    RAISE NOTICE 'Development Clients:';
    RAISE NOTICE '  - platform-bff (Secret: DevClientSecret123!)';
    RAISE NOTICE '  - platform-frontend (Public SPA client)';
    RAISE NOTICE '  - test-client (Secret: TestSecret123!)';
    RAISE NOTICE '';
    RAISE NOTICE 'Available Scopes:';
    RAISE NOTICE '  - openid, profile, email, offline_access';
    RAISE NOTICE '  - platform.api, platform.read, platform.write, platform.admin';
END $$;