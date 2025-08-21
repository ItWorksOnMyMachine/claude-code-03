# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-08-21-multi-tenant-database/spec.md

> Created: 2025-08-21
> Version: 1.0.0

## Endpoints

### GET /api/tenant/current

**Purpose:** Get the current tenant context for the authenticated user
**Parameters:** None (uses session)
**Response:** 
```json
{
  "tenantId": "uuid",
  "tenantName": "Tenant Name",
  "tenantSlug": "tenant-slug",
  "roles": ["Admin"],
  "permissions": ["read", "write", "delete"],
  "isPlatformTenant": false
}
```
**Errors:** 
- 401 Unauthorized - No authenticated user
- 404 Not Found - No tenant selected

### GET /api/tenant/available

**Purpose:** List all tenants available to the current user
**Parameters:** None (uses authenticated user ID)
**Response:**
```json
{
  "tenants": [
    {
      "id": "uuid",
      "name": "Tenant Name",
      "slug": "tenant-slug",
      "displayName": "Tenant Display Name",
      "roles": ["Admin"],
      "lastAccessedAt": "2025-08-21T10:00:00Z"
    }
  ],
  "hasPlatformAccess": false
}
```
**Errors:**
- 401 Unauthorized - No authenticated user

### POST /api/tenant/select

**Purpose:** Select a tenant as the current working context
**Parameters:**
```json
{
  "tenantId": "uuid"
}
```
**Response:**
```json
{
  "success": true,
  "tenant": {
    "id": "uuid",
    "name": "Tenant Name",
    "roles": ["Admin"]
  }
}
```
**Errors:**
- 400 Bad Request - Invalid tenant ID format
- 403 Forbidden - User doesn't have access to this tenant
- 404 Not Found - Tenant doesn't exist

### POST /api/tenant/switch

**Purpose:** Switch to a different tenant context
**Parameters:**
```json
{
  "targetTenantId": "uuid"
}
```
**Response:**
```json
{
  "success": true,
  "previousTenantId": "uuid",
  "currentTenant": {
    "id": "uuid",
    "name": "New Tenant",
    "roles": ["Member"]
  }
}
```
**Errors:**
- 400 Bad Request - Invalid tenant ID
- 403 Forbidden - No access to target tenant

### GET /api/admin/tenants

**Purpose:** List all tenants in the system (requires Admin role in platform tenant)
**Parameters:** 
- page (int, optional): Page number for pagination
- pageSize (int, optional): Items per page (default 20)
- search (string, optional): Search term for name/slug
**Response:**
```json
{
  "tenants": [
    {
      "id": "uuid",
      "name": "Tenant Name",
      "slug": "tenant-slug",
      "isActive": true,
      "userCount": 25,
      "createdAt": "2025-08-01T10:00:00Z"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20
}
```
**Errors:**
- 403 Forbidden - Not an admin in platform tenant

### POST /api/admin/tenant/{tenantId}/impersonate

**Purpose:** Platform tenant admin impersonates a customer tenant for support
**Parameters:** 
- tenantId (UUID, route): Target tenant to impersonate
**Response:**
```json
{
  "success": true,
  "impersonationToken": "token",
  "tenant": {
    "id": "uuid",
    "name": "Customer Tenant"
  },
  "expiresAt": "2025-08-21T12:00:00Z"
}
```
**Errors:**
- 403 Forbidden - Not an admin in platform tenant
- 404 Not Found - Tenant doesn't exist

## Controllers

### TenantController
- `GetCurrentTenant()` - Returns current tenant from context
- `GetAvailableTenants()` - Lists user's accessible tenants
- `SelectTenant(Guid tenantId)` - Sets tenant in session
- `SwitchTenant(Guid targetTenantId)` - Changes active tenant

### AdminTenantController
- `GetAllTenants(int page, int pageSize, string search)` - Lists all system tenants
- `ImpersonateTenant(Guid tenantId)` - Creates impersonation session
- `EndImpersonation()` - Returns to platform tenant context

## Middleware

### TenantContextMiddleware
```csharp
public async Task InvokeAsync(HttpContext context)
{
    // Extract tenant from session/cookie
    var tenantId = GetTenantFromSession(context);
    
    // Set in ITenantContext service
    _tenantContext.SetTenant(tenantId);
    
    // Add to HttpContext.Items for downstream access
    context.Items["TenantId"] = tenantId;
    
    await _next(context);
}
```

## Error Handling

All tenant-related endpoints should return consistent error responses:

```json
{
  "error": {
    "code": "TENANT_ACCESS_DENIED",
    "message": "You do not have access to this tenant",
    "details": {
      "tenantId": "uuid",
      "userId": "auth-user-id"
    }
  }
}
```

Common error codes:
- `TENANT_NOT_FOUND` - Requested tenant doesn't exist
- `TENANT_ACCESS_DENIED` - User lacks permission
- `NO_TENANT_SELECTED` - Operation requires tenant context
- `PLATFORM_ADMIN_REQUIRED` - Requires Admin role in platform tenant