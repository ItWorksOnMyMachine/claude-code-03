# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-09-11-cms-module/spec.md

> Created: 2025-09-11
> Version: 1.0.0

## Endpoints

### Content Management Endpoints

#### GET /api/cms/content

**Purpose:** Retrieve tenant-scoped content list with pagination and filtering
**Authentication:** Required (Bearer token or HTTP-only auth cookie)
**Entitlements:** CMS_ACCESS
**Parameters:**
- `page` (query, optional): Page number for pagination (default: 1)
- `pageSize` (query, optional): Items per page (default: 10, max: 50)
- `status` (query, optional): Filter by content status (draft, published, archived)
- `type` (query, optional): Filter by content type (page, template, block)
- `search` (query, optional): Search in title and content

**Response:** ContentListResponse
```json
{
  "items": [
    {
      "id": "guid",
      "title": "string",
      "type": "page|template|block",
      "status": "draft|published|archived",
      "createdAt": "datetime",
      "updatedAt": "datetime",
      "createdBy": "string",
      "thumbnail": "string|null"
    }
  ],
  "totalCount": 0,
  "page": 1,
  "pageSize": 10,
  "totalPages": 0
}
```
**Errors:**
- 401: Unauthorized - Invalid or missing authentication
- 403: Forbidden - Insufficient entitlements
- 400: Bad Request - Invalid pagination or filter parameters

#### GET /api/cms/content/{id}

**Purpose:** Get specific content item with full GrapesJS data
**Authentication:** Required
**Entitlements:** CMS_ACCESS
**Parameters:**
- `id` (route): Content ID (GUID)
- `includeHistory` (query, optional): Include version history (default: false)

**Response:** ContentResponse
```json
{
  "id": "guid",
  "title": "string",
  "type": "page|template|block",
  "status": "draft|published|archived",
  "content": {
    "html": "string",
    "css": "string",
    "components": "object",
    "styles": "object"
  },
  "metadata": {
    "description": "string",
    "keywords": ["string"],
    "customFields": "object"
  },
  "createdAt": "datetime",
  "updatedAt": "datetime",
  "createdBy": "string",
  "updatedBy": "string",
  "version": 1,
  "history": [
    {
      "version": 1,
      "createdAt": "datetime",
      "createdBy": "string",
      "changeDescription": "string"
    }
  ]
}
```
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements or content not in user's tenant
- 404: Not Found - Content ID does not exist

#### POST /api/cms/content

**Purpose:** Create new content item
**Authentication:** Required
**Entitlements:** CMS_MANAGE
**Request:** ContentCreateRequest
```json
{
  "title": "string (required, max 200 chars)",
  "type": "page|template|block (required)",
  "status": "draft|published (optional, default: draft)",
  "content": {
    "html": "string (optional)",
    "css": "string (optional)",
    "components": "object (optional)",
    "styles": "object (optional)"
  },
  "metadata": {
    "description": "string (optional, max 500 chars)",
    "keywords": ["string"] "(optional)",
    "customFields": "object (optional)"
  },
  "templateId": "guid (optional, for content based on templates)"
}
```

**Response:** ContentResponse (same as GET response)
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements
- 400: Bad Request - Invalid request data or validation errors
- 409: Conflict - Title already exists for this tenant

#### PUT /api/cms/content/{id}

**Purpose:** Update existing content item
**Authentication:** Required
**Entitlements:** CMS_MANAGE
**Parameters:**
- `id` (route): Content ID (GUID)

**Request:** ContentUpdateRequest
```json
{
  "title": "string (optional, max 200 chars)",
  "status": "draft|published|archived (optional)",
  "content": {
    "html": "string (optional)",
    "css": "string (optional)",
    "components": "object (optional)",
    "styles": "object (optional)"
  },
  "metadata": {
    "description": "string (optional, max 500 chars)",
    "keywords": ["string"] "(optional)",
    "customFields": "object (optional)"
  },
  "changeDescription": "string (optional, max 200 chars)"
}
```

**Response:** ContentResponse
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements or content not in user's tenant
- 404: Not Found - Content ID does not exist
- 400: Bad Request - Invalid request data
- 409: Conflict - Title already exists for this tenant

#### DELETE /api/cms/content/{id}

**Purpose:** Soft delete content item (mark as archived)
**Authentication:** Required
**Entitlements:** CMS_MANAGE
**Parameters:**
- `id` (route): Content ID (GUID)
- `permanent` (query, optional): Perform hard delete (default: false, requires admin)

**Response:** 204 No Content
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements or content not in user's tenant
- 404: Not Found - Content ID does not exist

### Asset Management Endpoints

#### POST /api/cms/assets/upload

**Purpose:** Upload content assets (images, documents, media files)
**Authentication:** Required
**Entitlements:** CMS_ASSETS
**Content-Type:** multipart/form-data
**Parameters:**
- `file` (form): File data (max 10MB per file)
- `files` (form): Multiple files (max 5 files per request)
- `folder` (form, optional): Organization folder path
- `tags` (form, optional): Comma-separated tags
- `altText` (form, optional): Alt text for images

**Response:** AssetUploadResponse
```json
{
  "assets": [
    {
      "id": "guid",
      "fileName": "string",
      "originalName": "string",
      "mimeType": "string",
      "size": 0,
      "url": "string",
      "thumbnailUrl": "string|null",
      "folder": "string|null",
      "tags": ["string"],
      "altText": "string|null",
      "uploadedAt": "datetime"
    }
  ],
  "errors": [
    {
      "fileName": "string",
      "error": "string"
    }
  ]
}
```
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements
- 400: Bad Request - Invalid file format or size exceeded
- 413: Payload Too Large - File size limit exceeded

#### GET /api/cms/assets

**Purpose:** List tenant assets with filtering and pagination
**Authentication:** Required
**Entitlements:** CMS_ASSETS
**Parameters:**
- `page` (query, optional): Page number (default: 1)
- `pageSize` (query, optional): Items per page (default: 20, max: 100)
- `folder` (query, optional): Filter by folder path
- `mimeType` (query, optional): Filter by MIME type prefix (e.g., "image/")
- `tags` (query, optional): Comma-separated tags filter
- `search` (query, optional): Search in file names and alt text

**Response:** AssetListResponse
```json
{
  "items": [
    {
      "id": "guid",
      "fileName": "string",
      "originalName": "string",
      "mimeType": "string",
      "size": 0,
      "url": "string",
      "thumbnailUrl": "string|null",
      "folder": "string|null",
      "tags": ["string"],
      "altText": "string|null",
      "uploadedAt": "datetime"
    }
  ],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20,
  "totalPages": 0
}
```
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements
- 400: Bad Request - Invalid query parameters

#### DELETE /api/cms/assets/{id}

**Purpose:** Remove asset and delete file from storage
**Authentication:** Required
**Entitlements:** CMS_ASSETS
**Parameters:**
- `id` (route): Asset ID (GUID)

**Response:** 204 No Content
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements or asset not in user's tenant
- 404: Not Found - Asset ID does not exist
- 409: Conflict - Asset is referenced by existing content

### Template Management Endpoints

#### GET /api/cms/templates

**Purpose:** Get available content templates for the tenant
**Authentication:** Required
**Entitlements:** CMS_ACCESS
**Parameters:**
- `category` (query, optional): Filter by template category
- `includeSystem` (query, optional): Include system-wide templates (default: true)

**Response:** TemplateListResponse
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "description": "string",
      "category": "string",
      "isSystem": false,
      "thumbnail": "string|null",
      "createdAt": "datetime",
      "updatedAt": "datetime"
    }
  ],
  "categories": ["string"]
}
```
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements

#### GET /api/cms/templates/{id}

**Purpose:** Get specific template with full GrapesJS structure
**Authentication:** Required
**Entitlements:** CMS_ACCESS
**Parameters:**
- `id` (route): Template ID (GUID)

**Response:** TemplateResponse
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "category": "string",
  "isSystem": false,
  "content": {
    "html": "string",
    "css": "string",
    "components": "object",
    "styles": "object"
  },
  "thumbnail": "string|null",
  "createdAt": "datetime",
  "updatedAt": "datetime",
  "createdBy": "string"
}
```
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements or template not accessible to tenant
- 404: Not Found - Template ID does not exist

#### POST /api/cms/templates

**Purpose:** Create custom template
**Authentication:** Required
**Entitlements:** CMS_TEMPLATES
**Request:** TemplateCreateRequest
```json
{
  "name": "string (required, max 100 chars)",
  "description": "string (optional, max 300 chars)",
  "category": "string (optional, max 50 chars)",
  "content": {
    "html": "string (required)",
    "css": "string (optional)",
    "components": "object (required)",
    "styles": "object (optional)"
  },
  "thumbnail": "string (optional, base64 or asset URL)"
}
```

**Response:** TemplateResponse
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements
- 400: Bad Request - Invalid request data or validation errors
- 409: Conflict - Template name already exists for this tenant

### Content Block Endpoints

#### GET /api/cms/blocks

**Purpose:** Get reusable content blocks for the tenant
**Authentication:** Required
**Entitlements:** CMS_ACCESS
**Parameters:**
- `category` (query, optional): Filter by block category
- `search` (query, optional): Search in block names and descriptions

**Response:** ContentBlockListResponse
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "description": "string",
      "category": "string",
      "thumbnail": "string|null",
      "createdAt": "datetime",
      "updatedAt": "datetime"
    }
  ],
  "categories": ["string"]
}
```
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements

#### POST /api/cms/blocks

**Purpose:** Create reusable content block
**Authentication:** Required
**Entitlements:** CMS_MANAGE
**Request:** ContentBlockCreateRequest
```json
{
  "name": "string (required, max 100 chars)",
  "description": "string (optional, max 300 chars)",
  "category": "string (optional, max 50 chars)",
  "content": {
    "html": "string (required)",
    "css": "string (optional)",
    "components": "object (required)",
    "styles": "object (optional)"
  }
}
```

**Response:** ContentBlockResponse
**Errors:**
- 401: Unauthorized
- 403: Forbidden - Insufficient entitlements
- 400: Bad Request - Invalid request data

## Controllers

### CmsContentController

**Purpose:** Handle all content management operations
**Base Route:** `/api/cms/content`
**Inheritance:** Inherits from platform's `BaseController` for tenant context and error handling

**Actions:**
- `GetContentListAsync()` - List tenant content with pagination
- `GetContentByIdAsync(Guid id)` - Retrieve specific content item
- `CreateContentAsync(ContentCreateRequest request)` - Create new content
- `UpdateContentAsync(Guid id, ContentUpdateRequest request)` - Update existing content
- `DeleteContentAsync(Guid id)` - Soft delete content

**Business Logic:**
- Automatic tenant ID injection from session context
- Content validation using FluentValidation
- HTML sanitization for security
- Auto-save functionality for draft content
- Version control for content changes

**Error Handling:**
- Structured error responses with correlation IDs
- Validation error aggregation
- Tenant boundary violation detection
- Content conflict resolution

### CmsAssetController

**Purpose:** Manage file uploads and asset operations
**Base Route:** `/api/cms/assets`
**File Handling:** Supports multipart/form-data uploads

**Actions:**
- `UploadAssetsAsync(AssetUploadRequest request)` - Handle file uploads
- `GetAssetListAsync(AssetFilterRequest filter)` - List tenant assets
- `DeleteAssetAsync(Guid id)` - Remove asset and files

**Business Logic:**
- File type validation and security scanning
- Image thumbnail generation
- Storage path organization by tenant
- Duplicate file detection and handling
- Asset usage tracking for deletion safety

**Error Handling:**
- File validation error reporting
- Storage quota enforcement
- Malicious file detection and blocking

### CmsTemplateController

**Purpose:** Manage content templates and reusable layouts
**Base Route:** `/api/cms/templates`

**Actions:**
- `GetTemplateListAsync()` - List available templates
- `GetTemplateByIdAsync(Guid id)` - Retrieve template details
- `CreateTemplateAsync(TemplateCreateRequest request)` - Create custom template

**Business Logic:**
- System template vs custom template handling
- Template inheritance and composition
- Category-based organization
- Template usage analytics

### CmsBlockController

**Purpose:** Handle reusable content blocks
**Base Route:** `/api/cms/blocks`

**Actions:**
- `GetBlockListAsync()` - List available blocks
- `CreateBlockAsync(ContentBlockCreateRequest request)` - Create reusable block

**Business Logic:**
- Block reusability and composition
- Category management
- Block version control

## Request/Response Models

### Validation Rules

**ContentCreateRequest/ContentUpdateRequest:**
- Title: Required, 1-200 characters, unique per tenant
- Type: Required, must be valid enum value
- HTML Content: XSS validation and sanitization
- Custom Fields: JSON schema validation
- Keywords: Maximum 10 items, 50 characters each

**AssetUploadRequest:**
- File Size: Maximum 10MB per file
- File Types: Whitelist of allowed MIME types
- File Names: Sanitized for security
- Batch Upload: Maximum 5 files per request

**TemplateCreateRequest:**
- Name: Required, 1-100 characters, unique per tenant
- GrapesJS Components: Valid JSON structure validation
- Category: Alphanumeric with hyphens/underscores only

### Error Response Format

All endpoints return consistent error responses:
```json
{
  "correlationId": "guid",
  "error": {
    "code": "string",
    "message": "string",
    "details": [
      {
        "field": "string",
        "message": "string"
      }
    ]
  },
  "timestamp": "datetime"
}
```

## Tenant Isolation

### Data Access Patterns
- All queries automatically filtered by `TenantId` from session context
- Repository pattern enforces tenant boundaries at data layer
- Cross-tenant data access explicitly blocked

### Security Measures
- Session-based tenant ID extraction
- Database-level tenant column on all CMS tables
- API response filtering to prevent data leakage
- Asset URL generation with tenant-scoped paths

## Entitlement Integration

### Required Entitlements

**CMS_ACCESS:**
- View CMS module interface
- List and view content items
- Access templates and blocks

**CMS_MANAGE:**
- Create, edit, and delete content
- Manage content status (publish/archive)
- Create and manage content blocks

**CMS_ASSETS:**
- Upload and manage assets
- Delete assets and files
- Organize assets in folders

**CMS_TEMPLATES:**
- Create custom templates
- Modify template categories
- Share templates within tenant

### Entitlement Checking Implementation

**FastEndpoints Attributes:**
```csharp
[RequireEntitlements("CMS_ACCESS")]
[RequireEntitlements("CMS_MANAGE", "CMS_ASSETS")]
```

**Frontend Hook Usage:**
```typescript
const { hasEntitlement } = useEntitlements();
const canManageContent = hasEntitlement('CMS_MANAGE');
```

### Progressive Feature Access
- UI elements dynamically shown/hidden based on entitlements
- API endpoints return 403 Forbidden for insufficient permissions
- Graceful degradation for users with limited access
- Real-time entitlement updates via WebSocket integration