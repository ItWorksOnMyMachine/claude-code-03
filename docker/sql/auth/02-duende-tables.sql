-- Duende IdentityServer Database Tables (Simplified for Development)
-- Note: In production, use official Duende migrations

-- Clients table
CREATE TABLE IF NOT EXISTS "Clients" (
    "Id" SERIAL PRIMARY KEY,
    "Enabled" BOOLEAN NOT NULL DEFAULT true,
    "ClientId" VARCHAR(200) NOT NULL UNIQUE,
    "ProtocolType" VARCHAR(200) NOT NULL DEFAULT 'oidc',
    "RequireClientSecret" BOOLEAN NOT NULL DEFAULT true,
    "ClientName" VARCHAR(200),
    "Description" VARCHAR(1000),
    "ClientUri" VARCHAR(2000),
    "LogoUri" VARCHAR(2000),
    "RequireConsent" BOOLEAN NOT NULL DEFAULT false,
    "AllowRememberConsent" BOOLEAN NOT NULL DEFAULT true,
    "AlwaysIncludeUserClaimsInIdToken" BOOLEAN NOT NULL DEFAULT false,
    "RequirePkce" BOOLEAN NOT NULL DEFAULT true,
    "AllowPlainTextPkce" BOOLEAN NOT NULL DEFAULT false,
    "RequireRequestObject" BOOLEAN NOT NULL DEFAULT false,
    "AllowAccessTokensViaBrowser" BOOLEAN NOT NULL DEFAULT false,
    "FrontChannelLogoutUri" VARCHAR(2000),
    "FrontChannelLogoutSessionRequired" BOOLEAN NOT NULL DEFAULT true,
    "BackChannelLogoutUri" VARCHAR(2000),
    "BackChannelLogoutSessionRequired" BOOLEAN NOT NULL DEFAULT true,
    "AllowOfflineAccess" BOOLEAN NOT NULL DEFAULT false,
    "IdentityTokenLifetime" INTEGER NOT NULL DEFAULT 300,
    "AllowedIdentityTokenSigningAlgorithms" VARCHAR(100),
    "AccessTokenLifetime" INTEGER NOT NULL DEFAULT 3600,
    "AuthorizationCodeLifetime" INTEGER NOT NULL DEFAULT 300,
    "ConsentLifetime" INTEGER,
    "AbsoluteRefreshTokenLifetime" INTEGER NOT NULL DEFAULT 2592000,
    "SlidingRefreshTokenLifetime" INTEGER NOT NULL DEFAULT 1296000,
    "RefreshTokenUsage" INTEGER NOT NULL DEFAULT 1,
    "UpdateAccessTokenClaimsOnRefresh" BOOLEAN NOT NULL DEFAULT false,
    "RefreshTokenExpiration" INTEGER NOT NULL DEFAULT 1,
    "AccessTokenType" INTEGER NOT NULL DEFAULT 0,
    "EnableLocalLogin" BOOLEAN NOT NULL DEFAULT true,
    "IncludeJwtId" BOOLEAN NOT NULL DEFAULT true,
    "AlwaysSendClientClaims" BOOLEAN NOT NULL DEFAULT false,
    "ClientClaimsPrefix" VARCHAR(200) DEFAULT 'client_',
    "PairWiseSubjectSalt" VARCHAR(200),
    "Created" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Updated" TIMESTAMP,
    "LastAccessed" TIMESTAMP,
    "UserSsoLifetime" INTEGER,
    "UserCodeType" VARCHAR(100),
    "DeviceCodeLifetime" INTEGER NOT NULL DEFAULT 300,
    "NonEditable" BOOLEAN NOT NULL DEFAULT false
);

-- Client Secrets
CREATE TABLE IF NOT EXISTS "ClientSecrets" (
    "Id" SERIAL PRIMARY KEY,
    "ClientId" INTEGER NOT NULL REFERENCES "Clients"("Id") ON DELETE CASCADE,
    "Description" VARCHAR(2000),
    "Value" VARCHAR(4000) NOT NULL,
    "Expiration" TIMESTAMP,
    "Type" VARCHAR(250) NOT NULL DEFAULT 'SharedSecret',
    "Created" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Client Grant Types
CREATE TABLE IF NOT EXISTS "ClientGrantTypes" (
    "Id" SERIAL PRIMARY KEY,
    "ClientId" INTEGER NOT NULL REFERENCES "Clients"("Id") ON DELETE CASCADE,
    "GrantType" VARCHAR(250) NOT NULL,
    UNIQUE("ClientId", "GrantType")
);

-- Client Redirect URIs
CREATE TABLE IF NOT EXISTS "ClientRedirectUris" (
    "Id" SERIAL PRIMARY KEY,
    "ClientId" INTEGER NOT NULL REFERENCES "Clients"("Id") ON DELETE CASCADE,
    "RedirectUri" VARCHAR(2000) NOT NULL,
    UNIQUE("ClientId", "RedirectUri")
);

-- Client Post Logout Redirect URIs
CREATE TABLE IF NOT EXISTS "ClientPostLogoutRedirectUris" (
    "Id" SERIAL PRIMARY KEY,
    "ClientId" INTEGER NOT NULL REFERENCES "Clients"("Id") ON DELETE CASCADE,
    "PostLogoutRedirectUri" VARCHAR(2000) NOT NULL,
    UNIQUE("ClientId", "PostLogoutRedirectUri")
);

-- Client Scopes
CREATE TABLE IF NOT EXISTS "ClientScopes" (
    "Id" SERIAL PRIMARY KEY,
    "ClientId" INTEGER NOT NULL REFERENCES "Clients"("Id") ON DELETE CASCADE,
    "Scope" VARCHAR(200) NOT NULL,
    UNIQUE("ClientId", "Scope")
);

-- Client CORS Origins
CREATE TABLE IF NOT EXISTS "ClientCorsOrigins" (
    "Id" SERIAL PRIMARY KEY,
    "ClientId" INTEGER NOT NULL REFERENCES "Clients"("Id") ON DELETE CASCADE,
    "Origin" VARCHAR(150) NOT NULL,
    UNIQUE("ClientId", "Origin")
);

-- Identity Resources
CREATE TABLE IF NOT EXISTS "IdentityResources" (
    "Id" SERIAL PRIMARY KEY,
    "Enabled" BOOLEAN NOT NULL DEFAULT true,
    "Name" VARCHAR(200) NOT NULL UNIQUE,
    "DisplayName" VARCHAR(200),
    "Description" VARCHAR(1000),
    "Required" BOOLEAN NOT NULL DEFAULT false,
    "Emphasize" BOOLEAN NOT NULL DEFAULT false,
    "ShowInDiscoveryDocument" BOOLEAN NOT NULL DEFAULT true,
    "Created" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Updated" TIMESTAMP,
    "NonEditable" BOOLEAN NOT NULL DEFAULT false
);

-- Identity Resource Claims
CREATE TABLE IF NOT EXISTS "IdentityResourceClaims" (
    "Id" SERIAL PRIMARY KEY,
    "IdentityResourceId" INTEGER NOT NULL REFERENCES "IdentityResources"("Id") ON DELETE CASCADE,
    "Type" VARCHAR(200) NOT NULL,
    UNIQUE("IdentityResourceId", "Type")
);

-- API Scopes
CREATE TABLE IF NOT EXISTS "ApiScopes" (
    "Id" SERIAL PRIMARY KEY,
    "Enabled" BOOLEAN NOT NULL DEFAULT true,
    "Name" VARCHAR(200) NOT NULL UNIQUE,
    "DisplayName" VARCHAR(200),
    "Description" VARCHAR(1000),
    "Required" BOOLEAN NOT NULL DEFAULT false,
    "Emphasize" BOOLEAN NOT NULL DEFAULT false,
    "ShowInDiscoveryDocument" BOOLEAN NOT NULL DEFAULT true,
    "Created" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Updated" TIMESTAMP,
    "LastAccessed" TIMESTAMP,
    "NonEditable" BOOLEAN NOT NULL DEFAULT false
);

-- API Scope Claims
CREATE TABLE IF NOT EXISTS "ApiScopeClaims" (
    "Id" SERIAL PRIMARY KEY,
    "ScopeId" INTEGER NOT NULL REFERENCES "ApiScopes"("Id") ON DELETE CASCADE,
    "Type" VARCHAR(200) NOT NULL,
    UNIQUE("ScopeId", "Type")
);

-- API Resources
CREATE TABLE IF NOT EXISTS "ApiResources" (
    "Id" SERIAL PRIMARY KEY,
    "Enabled" BOOLEAN NOT NULL DEFAULT true,
    "Name" VARCHAR(200) NOT NULL UNIQUE,
    "DisplayName" VARCHAR(200),
    "Description" VARCHAR(1000),
    "AllowedAccessTokenSigningAlgorithms" VARCHAR(100),
    "ShowInDiscoveryDocument" BOOLEAN NOT NULL DEFAULT true,
    "Created" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Updated" TIMESTAMP,
    "LastAccessed" TIMESTAMP,
    "NonEditable" BOOLEAN NOT NULL DEFAULT false
);

-- API Resource Secrets
CREATE TABLE IF NOT EXISTS "ApiResourceSecrets" (
    "Id" SERIAL PRIMARY KEY,
    "ApiResourceId" INTEGER NOT NULL REFERENCES "ApiResources"("Id") ON DELETE CASCADE,
    "Description" VARCHAR(1000),
    "Value" VARCHAR(4000) NOT NULL,
    "Expiration" TIMESTAMP,
    "Type" VARCHAR(250) NOT NULL DEFAULT 'SharedSecret',
    "Created" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- API Resource Scopes
CREATE TABLE IF NOT EXISTS "ApiResourceScopes" (
    "Id" SERIAL PRIMARY KEY,
    "ApiResourceId" INTEGER NOT NULL REFERENCES "ApiResources"("Id") ON DELETE CASCADE,
    "Scope" VARCHAR(200) NOT NULL,
    UNIQUE("ApiResourceId", "Scope")
);

-- API Resource Claims
CREATE TABLE IF NOT EXISTS "ApiResourceClaims" (
    "Id" SERIAL PRIMARY KEY,
    "ApiResourceId" INTEGER NOT NULL REFERENCES "ApiResources"("Id") ON DELETE CASCADE,
    "Type" VARCHAR(200) NOT NULL,
    UNIQUE("ApiResourceId", "Type")
);

-- Persisted Grants (for refresh tokens, authorization codes, etc.)
CREATE TABLE IF NOT EXISTS "PersistedGrants" (
    "Key" VARCHAR(200) PRIMARY KEY,
    "Type" VARCHAR(50) NOT NULL,
    "SubjectId" VARCHAR(200),
    "SessionId" VARCHAR(100),
    "ClientId" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(200),
    "CreationTime" TIMESTAMP NOT NULL,
    "Expiration" TIMESTAMP,
    "ConsumedTime" TIMESTAMP,
    "Data" TEXT NOT NULL
);

-- Device Codes
CREATE TABLE IF NOT EXISTS "DeviceCodes" (
    "UserCode" VARCHAR(200) PRIMARY KEY,
    "DeviceCode" VARCHAR(200) NOT NULL UNIQUE,
    "SubjectId" VARCHAR(200),
    "SessionId" VARCHAR(100),
    "ClientId" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(200),
    "CreationTime" TIMESTAMP NOT NULL,
    "Expiration" TIMESTAMP NOT NULL,
    "Data" TEXT NOT NULL
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS "IX_Clients_ClientId" ON "Clients"("ClientId");
CREATE INDEX IF NOT EXISTS "IX_ClientSecrets_ClientId" ON "ClientSecrets"("ClientId");
CREATE INDEX IF NOT EXISTS "IX_ClientGrantTypes_ClientId" ON "ClientGrantTypes"("ClientId");
CREATE INDEX IF NOT EXISTS "IX_ClientRedirectUris_ClientId" ON "ClientRedirectUris"("ClientId");
CREATE INDEX IF NOT EXISTS "IX_ClientPostLogoutRedirectUris_ClientId" ON "ClientPostLogoutRedirectUris"("ClientId");
CREATE INDEX IF NOT EXISTS "IX_ClientScopes_ClientId" ON "ClientScopes"("ClientId");
CREATE INDEX IF NOT EXISTS "IX_ClientCorsOrigins_ClientId" ON "ClientCorsOrigins"("ClientId");
CREATE INDEX IF NOT EXISTS "IX_IdentityResourceClaims_IdentityResourceId" ON "IdentityResourceClaims"("IdentityResourceId");
CREATE INDEX IF NOT EXISTS "IX_ApiScopeClaims_ScopeId" ON "ApiScopeClaims"("ScopeId");
CREATE INDEX IF NOT EXISTS "IX_ApiResourceSecrets_ApiResourceId" ON "ApiResourceSecrets"("ApiResourceId");
CREATE INDEX IF NOT EXISTS "IX_ApiResourceScopes_ApiResourceId" ON "ApiResourceScopes"("ApiResourceId");
CREATE INDEX IF NOT EXISTS "IX_ApiResourceClaims_ApiResourceId" ON "ApiResourceClaims"("ApiResourceId");
CREATE INDEX IF NOT EXISTS "IX_PersistedGrants_Expiration" ON "PersistedGrants"("Expiration");
CREATE INDEX IF NOT EXISTS "IX_PersistedGrants_SubjectId_ClientId_Type" ON "PersistedGrants"("SubjectId", "ClientId", "Type");
CREATE INDEX IF NOT EXISTS "IX_PersistedGrants_SubjectId_SessionId_Type" ON "PersistedGrants"("SubjectId", "SessionId", "Type");
CREATE INDEX IF NOT EXISTS "IX_DeviceCodes_DeviceCode" ON "DeviceCodes"("DeviceCode");
CREATE INDEX IF NOT EXISTS "IX_DeviceCodes_Expiration" ON "DeviceCodes"("Expiration");

DO $$
BEGIN
    RAISE NOTICE 'Duende IdentityServer tables created successfully.';
END $$;