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

## CMS Module Tables

### Templates Table
```sql
CREATE TABLE Templates (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL,
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    Layout JSONB NOT NULL, -- JSON structure defining layout zones
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    UpdatedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    DeletedAt TIMESTAMPTZ,
    DeletedBy VARCHAR(255),
    CONSTRAINT FK_Templates_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Templates_TenantId_Name UNIQUE (TenantId, Name)
);

CREATE INDEX idx_templates_tenantid ON Templates(TenantId) WHERE IsDeleted = false;
CREATE INDEX idx_templates_active ON Templates(IsActive) WHERE IsDeleted = false;
CREATE INDEX idx_templates_name ON Templates(Name) WHERE IsDeleted = false;
```

### Pages Table
```sql
CREATE TABLE Pages (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Slug VARCHAR(255) NOT NULL,
    MetaTitle VARCHAR(255),
    MetaDescription VARCHAR(500),
    TemplateId UUID NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0: Draft, 1: Published, 2: Archived
    PublishedAt TIMESTAMPTZ,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    UpdatedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    DeletedAt TIMESTAMPTZ,
    DeletedBy VARCHAR(255),
    CONSTRAINT FK_Pages_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Pages_Templates FOREIGN KEY (TemplateId) 
        REFERENCES Templates(Id) ON DELETE RESTRICT,
    CONSTRAINT UQ_Pages_TenantId_Slug UNIQUE (TenantId, Slug),
    CONSTRAINT CK_Pages_Status CHECK (Status IN (0, 1, 2))
);

CREATE INDEX idx_pages_tenantid ON Pages(TenantId) WHERE IsDeleted = false;
CREATE INDEX idx_pages_slug ON Pages(Slug) WHERE IsDeleted = false;
CREATE INDEX idx_pages_status ON Pages(Status) WHERE IsDeleted = false;
CREATE INDEX idx_pages_published ON Pages(PublishedAt) WHERE Status = 1 AND IsDeleted = false;
CREATE INDEX idx_pages_templateid ON Pages(TemplateId) WHERE IsDeleted = false;
```

### ContentBlocks Table
```sql
CREATE TABLE ContentBlocks (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL,
    PageId UUID NOT NULL,
    BlockType VARCHAR(50) NOT NULL, -- text, image, video, html, etc.
    Zone VARCHAR(100) NOT NULL, -- layout zone identifier
    SortOrder INT NOT NULL DEFAULT 0,
    Content JSONB, -- JSON content based on block type
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    UpdatedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    DeletedAt TIMESTAMPTZ,
    DeletedBy VARCHAR(255),
    CONSTRAINT FK_ContentBlocks_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ContentBlocks_Pages FOREIGN KEY (PageId) 
        REFERENCES Pages(Id) ON DELETE CASCADE
);

CREATE INDEX idx_contentblocks_tenantid ON ContentBlocks(TenantId) WHERE IsDeleted = false;
CREATE INDEX idx_contentblocks_pageid ON ContentBlocks(PageId) WHERE IsDeleted = false;
CREATE INDEX idx_contentblocks_page_zone ON ContentBlocks(PageId, Zone, SortOrder) WHERE IsDeleted = false;
CREATE INDEX idx_contentblocks_blocktype ON ContentBlocks(BlockType) WHERE IsDeleted = false;
CREATE INDEX idx_contentblocks_active ON ContentBlocks(IsActive) WHERE IsDeleted = false;
```

### Assets Table
```sql
CREATE TABLE Assets (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL,
    FileName VARCHAR(255) NOT NULL,
    OriginalFileName VARCHAR(255) NOT NULL,
    MimeType VARCHAR(100) NOT NULL,
    FileSize BIGINT NOT NULL,
    StoragePath VARCHAR(500) NOT NULL,
    AltText VARCHAR(255),
    Tags JSONB, -- JSON array of tags
    IsActive BOOLEAN NOT NULL DEFAULT true,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy VARCHAR(255),
    UpdatedBy VARCHAR(255),
    IsDeleted BOOLEAN NOT NULL DEFAULT false,
    DeletedAt TIMESTAMPTZ,
    DeletedBy VARCHAR(255),
    CONSTRAINT FK_Assets_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE
);

CREATE INDEX idx_assets_tenantid ON Assets(TenantId) WHERE IsDeleted = false;
CREATE INDEX idx_assets_mimetype ON Assets(MimeType) WHERE IsDeleted = false;
CREATE INDEX idx_assets_filename ON Assets(FileName) WHERE IsDeleted = false;
CREATE INDEX idx_assets_active ON Assets(IsActive) WHERE IsDeleted = false;
CREATE INDEX idx_assets_created ON Assets(CreatedAt) WHERE IsDeleted = false;
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

## Migration Scripts

### Initial Platform Setup
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

### CMS Module Migration
```sql
-- Migration: Add CMS tables
BEGIN;

-- Create Templates table (must be created before Pages)
CREATE TABLE Templates (
    -- [Full table definition as above]
);

-- Create Pages table (must be created before ContentBlocks)
CREATE TABLE Pages (
    -- [Full table definition as above]
);

-- Create ContentBlocks table
CREATE TABLE ContentBlocks (
    -- [Full table definition as above]
);

-- Create Assets table
CREATE TABLE Assets (
    -- [Full table definition as above]
);

-- Insert default template for each existing tenant
INSERT INTO Templates (TenantId, Name, Description, Layout, CreatedBy)
SELECT 
    t.Id,
    'Default Page Template',
    'Basic page template with header, content, and footer zones',
    '{"zones": [{"name": "header", "label": "Header"}, {"name": "content", "label": "Main Content"}, {"name": "footer", "label": "Footer"}]}'::JSONB,
    'system'
FROM Tenants t
WHERE t.IsDeleted = false
  AND NOT EXISTS (
      SELECT 1 FROM Templates tm 
      WHERE tm.TenantId = t.Id 
        AND tm.Name = 'Default Page Template' 
        AND tm.IsDeleted = false
  );

COMMIT;
```

### CMS Permissions Migration
```sql
-- Add CMS-related permissions to existing roles
UPDATE Roles 
SET Permissions = COALESCE(Permissions, '[]'::JSONB) || '[
    "cms.pages.view",
    "cms.pages.create", 
    "cms.pages.edit",
    "cms.pages.delete",
    "cms.templates.view",
    "cms.templates.create",
    "cms.templates.edit",
    "cms.templates.delete",
    "cms.assets.view",
    "cms.assets.upload",
    "cms.assets.delete"
]'::JSONB
WHERE Name IN ('Admin', 'ContentManager') 
  AND IsDeleted = false;

-- Add read-only CMS permissions for members
UPDATE Roles 
SET Permissions = COALESCE(Permissions, '[]'::JSONB) || '[
    "cms.pages.view",
    "cms.assets.view"
]'::JSONB
WHERE Name = 'Member' 
  AND IsDeleted = false;
```

## Entity Relationships

### CMS Module Relationships
```
Tenants (1) -----> (N) Templates
Tenants (1) -----> (N) Pages  
Tenants (1) -----> (N) ContentBlocks
Tenants (1) -----> (N) Assets

Templates (1) -----> (N) Pages
Pages (1) -----> (N) ContentBlocks
```

### Multi-Tenant Isolation
All CMS tables include `TenantId` columns with foreign key constraints ensuring complete data isolation:
- **Templates.TenantId**: Isolates page templates by tenant
- **Pages.TenantId**: Ensures pages belong to specific tenants
- **ContentBlocks.TenantId**: Segregates content blocks by tenant
- **Assets.TenantId**: Isolates media assets by tenant

## Indexes and Performance

### Required Indexes
- All TenantId columns must have indexes for query performance
- Composite indexes on (TenantId, IsDeleted) for filtered queries
- Unique constraints should include TenantId where appropriate

### CMS-Specific Performance Optimizations
- **Pages.Slug**: Indexed for fast URL resolution
- **Pages.Status + PublishedAt**: Composite index for published page queries
- **ContentBlocks.PageId + Zone + SortOrder**: Optimizes content block retrieval for page rendering
- **Assets.MimeType**: Enables efficient filtering by file type
- **Assets.FileName**: Supports file search functionality

### Query Patterns
```sql
-- Tenant-scoped page query (automatic via EF Core)
SELECT * FROM Pages 
WHERE TenantId = @currentTenantId 
  AND IsDeleted = false 
  AND Status = 1;

-- Page with content blocks query
SELECT p.*, cb.* 
FROM Pages p
LEFT JOIN ContentBlocks cb ON p.Id = cb.PageId
WHERE p.TenantId = @currentTenantId 
  AND p.Slug = @slug
  AND p.IsDeleted = false 
  AND p.Status = 1
  AND (cb.IsDeleted = false OR cb.Id IS NULL)
ORDER BY cb.Zone, cb.SortOrder;

-- Cross-tenant admin query
SELECT p.*, t.Name as TenantName, tm.Name as TemplateName
FROM Pages p
JOIN Tenants t ON p.TenantId = t.Id
JOIN Templates tm ON p.TemplateId = tm.Id
WHERE p.IsDeleted = false;
```

## Constraints and Rules

### Data Integrity
1. **Referential Integrity**: All TenantId foreign keys use CASCADE DELETE
2. **Unique Constraints**: Include TenantId in unique constraints for tenant-scoped uniqueness
3. **Check Constraints**: Ensure valid Status values for Pages (0, 1, 2)
4. **Template Dependencies**: Pages cannot be deleted if referenced by Templates (RESTRICT)
5. **Soft Delete**: Never hard delete records, use IsDeleted flag instead

### Business Rules Enforced
- Pages must belong to valid tenants and use valid templates
- Content blocks must belong to valid pages within the same tenant
- Published pages must have unique slugs within each tenant
- Assets are tenant-isolated with proper file size validation
- Template layouts must be valid JSON structures

### Cascade Behaviors
- **Tenants → All CMS tables**: CASCADE DELETE (tenant deletion removes all CMS data)
- **Templates → Pages**: RESTRICT (prevent template deletion if pages use it)
- **Pages → ContentBlocks**: CASCADE DELETE (page deletion removes content blocks)

## Storage Considerations

### Large Data Fields
- **Templates.Layout**: JSONB structure, expected size < 50KB
- **ContentBlocks.Content**: JSONB content, expected size < 500KB per block
- **Assets.StoragePath**: References to file system or cloud storage
- **Assets.Tags**: JSONB array for flexible tagging and search

### JSON Schema Examples
```sql
-- Template Layout JSON structure
{
  "zones": [
    {"name": "header", "label": "Header", "maxBlocks": 1},
    {"name": "content", "label": "Main Content", "maxBlocks": null},
    {"name": "sidebar", "label": "Sidebar", "maxBlocks": 5},
    {"name": "footer", "label": "Footer", "maxBlocks": 1}
  ],
  "settings": {
    "responsive": true,
    "allowedBlockTypes": ["text", "image", "video"]
  }
}

-- ContentBlock Content JSON (text block example)
{
  "type": "text",
  "data": {
    "html": "<p>Rich text content</p>",
    "format": "html"
  },
  "style": {
    "alignment": "left",
    "fontSize": "medium"
  }
}

-- Asset Tags JSON
["marketing", "hero-image", "homepage"]
```