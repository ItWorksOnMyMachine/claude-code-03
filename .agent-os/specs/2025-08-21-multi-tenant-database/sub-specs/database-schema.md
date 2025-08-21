# Database Schema

This is the database schema implementation for the spec detailed in @.agent-os/specs/2025-08-21-multi-tenant-database/spec.md

> Created: 2025-08-21
> Version: 1.0.0

## Core Tables

### Tenants Table
```sql
CREATE TABLE Tenants (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(255) NOT NULL,
    Slug VARCHAR(100) NOT NULL UNIQUE,
    DisplayName VARCHAR(255) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    IsPlatformTenant BOOLEAN NOT NULL DEFAULT false,
    Settings JSONB,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    UpdatedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    DeletedAt TIMESTAMPTZ,
    DeletedBy VARCHAR(255)
);

CREATE INDEX idx_tenants_slug ON Tenants(Slug) WHERE IsDeleted = false;
CREATE INDEX idx_tenants_active ON Tenants(IsActive) WHERE IsDeleted = false;
CREATE UNIQUE INDEX idx_platform_tenant ON Tenants(IsPlatformTenant) WHERE IsPlatformTenant = true AND IsDeleted = false;
```

### Roles Table
```sql
CREATE TABLE Roles (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL,
    Name VARCHAR(100) NOT NULL,
    DisplayName VARCHAR(255) NOT NULL,
    Description TEXT,
    IsSystemRole BOOLEAN NOT NULL DEFAULT false, -- Built-in roles like Admin, Member
    Permissions JSONB, -- Array of permission strings
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    UpdatedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    DeletedAt TIMESTAMPTZ,
    DeletedBy VARCHAR(255),
    CONSTRAINT FK_Roles_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Roles_TenantId_Name UNIQUE (TenantId, Name)
);

CREATE INDEX idx_roles_tenantid ON Roles(TenantId) WHERE IsDeleted = false;
CREATE INDEX idx_roles_name ON Roles(Name) WHERE IsDeleted = false;
CREATE INDEX idx_roles_system ON Roles(IsSystemRole) WHERE IsDeleted = false;
```

### TenantUsers Table
```sql
CREATE TABLE TenantUsers (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId VARCHAR(450) NOT NULL, -- From auth service
    TenantId UUID NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    JoinedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastAccessedAt TIMESTAMPTZ,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    UpdatedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    DeletedAt TIMESTAMPTZ,
    DeletedBy VARCHAR(255),
    CONSTRAINT FK_TenantUsers_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_TenantUsers_UserId_TenantId UNIQUE (UserId, TenantId)
);

CREATE INDEX idx_tenantusers_userid ON TenantUsers(UserId) WHERE IsDeleted = false;
CREATE INDEX idx_tenantusers_tenantid ON TenantUsers(TenantId) WHERE IsDeleted = false;
```

### UserRoles Table
```sql
CREATE TABLE UserRoles (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantUserId UUID NOT NULL,
    RoleId UUID NOT NULL,
    AssignedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    AssignedBy VARCHAR(255),
    ExpiresAt TIMESTAMPTZ, -- Optional: for temporary role assignments
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    UpdatedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    DeletedAt TIMESTAMPTZ,
    DeletedBy VARCHAR(255),
    CONSTRAINT FK_UserRoles_TenantUsers FOREIGN KEY (TenantUserId) 
        REFERENCES TenantUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) 
        REFERENCES Roles(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_UserRoles_TenantUserId_RoleId UNIQUE (TenantUserId, RoleId)
);

CREATE INDEX idx_userroles_tenantuserid ON UserRoles(TenantUserId) WHERE IsDeleted = false;
CREATE INDEX idx_userroles_roleid ON UserRoles(RoleId) WHERE IsDeleted = false;
CREATE INDEX idx_userroles_expiry ON UserRoles(ExpiresAt) WHERE IsDeleted = false AND ExpiresAt IS NOT NULL;
```

## Tenant-Scoped Table Pattern

All business domain tables must include TenantId:

```sql
CREATE TABLE ExampleEntity (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL,
    -- Entity specific columns
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    -- Standard audit columns
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    UpdatedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    DeletedAt TIMESTAMPTZ,
    DeletedBy VARCHAR(255),
    CONSTRAINT FK_ExampleEntity_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE
);

CREATE INDEX idx_exampleentity_tenantid ON ExampleEntity(TenantId) WHERE IsDeleted = false;
```

## Migration Script

```sql
-- Create platform tenant (fixed GUID)
INSERT INTO Tenants (
    Id,
    Name,
    Slug,
    DisplayName,
    IsPlatformTenant,
    IsActive,
    CreatedBy,
    Settings
) VALUES (
    '00000000-0000-0000-0000-000000000001'::UUID,
    'Platform Administration',
    'platform-admin',
    'Platform Administration',
    true,
    true,
    'system',
    '{"type": "platform", "features": ["cross-tenant-access", "admin-tools"]}'::JSONB
) ON CONFLICT DO NOTHING;
```

## Indexes and Performance

### Required Indexes
- All TenantId columns must have indexes for query performance
- Composite indexes on (TenantId, IsDeleted) for filtered queries
- Unique constraints should include TenantId where appropriate

### Query Patterns
```sql
-- Tenant-scoped query (automatic via EF Core)
SELECT * FROM ExampleEntity 
WHERE TenantId = @currentTenantId AND IsDeleted = false;

-- Cross-tenant admin query
SELECT e.*, t.Name as TenantName 
FROM ExampleEntity e
JOIN Tenants t ON e.TenantId = t.Id
WHERE e.IsDeleted = false;
```

## Constraints and Rules

1. **Referential Integrity**: All TenantId foreign keys use CASCADE DELETE
2. **Unique Constraints**: Include TenantId in unique constraints for tenant-scoped uniqueness
3. **Check Constraints**: Ensure IsPlatformTenant is unique when true
4. **Default Values**: IsDeleted defaults to false, timestamps use CURRENT_TIMESTAMP
5. **Soft Delete**: Never hard delete records, use IsDeleted flag instead