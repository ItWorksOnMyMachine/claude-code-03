# Database Schema

This is the database schema implementation for the spec detailed in @.agent-os/specs/2025-08-25-developer-environment-setup/spec.md

> Created: 2025-08-25
> Version: 1.0.0

## Platform Database Initialization

### Schema Creation
```sql
-- Executed via Entity Framework migrations
-- Migrations already exist in platform-host-bff/Migrations/
```

### Test Data Seeding
```sql
-- Insert platform tenant
INSERT INTO "Tenants" ("Id", "Name", "Slug", "DisplayName", "IsActive", "IsPlatformTenant", "CreatedAt", "UpdatedAt")
VALUES 
  ('00000000-0000-0000-0000-000000000001', 'Platform', 'platform', 'Platform Administration', true, true, NOW(), NOW());

-- Insert test customer tenants
INSERT INTO "Tenants" ("Id", "Name", "Slug", "DisplayName", "IsActive", "IsPlatformTenant", "CreatedAt", "UpdatedAt")
VALUES 
  ('11111111-1111-1111-1111-111111111111', 'Acme Corp', 'acme-corp', 'Acme Corporation', true, false, NOW(), NOW()),
  ('22222222-2222-2222-2222-222222222222', 'Test Company', 'test-company', 'Test Company Inc', true, false, NOW(), NOW());

-- Insert roles
INSERT INTO "Roles" ("Id", "TenantId", "Name", "Description", "IsSystemRole", "CreatedAt", "UpdatedAt")
VALUES
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'Admin', 'Platform Administrator', true, NOW(), NOW()),
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', 'Admin', 'Tenant Administrator', true, NOW(), NOW()),
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', 'User', 'Regular User', true, NOW(), NOW()),
  (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'Admin', 'Tenant Administrator', true, NOW(), NOW()),
  (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'User', 'Regular User', true, NOW(), NOW());
```

### Test User Associations
```sql
-- Note: Users are created in auth database
-- These are tenant associations only

-- Platform admin user
INSERT INTO "TenantUsers" ("TenantId", "UserId", "Email", "JoinedAt", "IsActive")
VALUES 
  ('00000000-0000-0000-0000-000000000001', 'auth-admin-user-id', 'admin@platform.local', NOW(), true);

-- Regular user with multiple tenant access
INSERT INTO "TenantUsers" ("TenantId", "UserId", "Email", "JoinedAt", "IsActive")
VALUES 
  ('11111111-1111-1111-1111-111111111111', 'auth-test-user-id', 'user@test.local', NOW(), true),
  ('22222222-2222-2222-2222-222222222222', 'auth-test-user-id', 'user@test.local', NOW(), true);
```

## Auth Database Initialization

### Duende IdentityServer Schema
```sql
-- Created automatically by Duende migrations
-- dotnet ef database update -c ConfigurationDbContext
-- dotnet ef database update -c PersistedGrantDbContext
-- dotnet ef database update -c ApplicationDbContext
```

### Test Users
```sql
-- Note: Users must be created through Identity framework
-- Cannot directly insert due to password hashing
-- Will be handled by initialization script using UserManager

-- Test users to create:
-- admin@platform.local / Password123! (Platform admin)
-- user@test.local / Password123! (Regular user)
-- demo@test.local / Password123! (Demo user)
```

### OAuth Clients
```sql
-- Platform BFF client configuration
-- Created via ConfigurationDbContext seeding
-- ClientId: platform-bff
-- ClientSecret: [generated]
-- RedirectUris: http://localhost:5000/signin-oidc
-- PostLogoutRedirectUris: http://localhost:5000/signout-callback-oidc
-- AllowedScopes: openid, profile, email, offline_access
```

## Docker Initialization Scripts

### init-platform-db.sh
```bash
#!/bin/bash
# Wait for PostgreSQL to be ready
# Run EF migrations
# Execute test data SQL scripts
# Verify data insertion
```

### init-auth-db.sh
```bash
#!/bin/bash
# Wait for PostgreSQL to be ready
# Run Duende migrations
# Create test users via API endpoint
# Seed OAuth client configurations
```

## Volume Mounts

### Platform Database
- ./docker/sql/platform-init.sql:/docker-entrypoint-initdb.d/01-init.sql
- ./docker/sql/platform-seed.sql:/docker-entrypoint-initdb.d/02-seed.sql

### Auth Database
- ./docker/sql/auth-init.sql:/docker-entrypoint-initdb.d/01-init.sql
- Auth seeding handled by application on startup

## Environment Variables

### Platform Database
- POSTGRES_DB=platformdb
- POSTGRES_USER=platformuser
- POSTGRES_PASSWORD=DevPassword123!

### Auth Database
- POSTGRES_DB=authdb
- POSTGRES_USER=authuser
- POSTGRES_PASSWORD=DevPassword123!

### Connection Strings
- Platform: "Host=localhost;Port=5432;Database=platformdb;Username=platformuser;Password=DevPassword123!"
- Auth: "Host=localhost;Port=5433;Database=authdb;Username=authuser;Password=DevPassword123!"