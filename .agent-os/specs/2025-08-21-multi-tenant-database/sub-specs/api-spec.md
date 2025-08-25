# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-08-21-multi-tenant-database/spec.md

> Created: 2025-08-21
> Updated: 2025-08-25
> Version: 1.1.0

## Table of Contents
- [Tenant Management Endpoints](#tenant-management-endpoints)
- [Platform Admin Endpoints](#platform-admin-endpoints)
- [Controllers](#controllers)
- [Middleware](#middleware)
- [Error Handling](#error-handling)

## Tenant Management Endpoints

### GET /api/tenant/current

**Purpose:** Get the current tenant context for the authenticated user
**Authentication:** Required
**Parameters:** None (uses session)
**Response:** 
```json
{
  "tenantId": "uuid",
  "tenantName": "Tenant Name",
  "isPlatformTenant": false,
  "userRoles": ["Admin", "User"],
  "selectedAt": "2025-08-25T10:00:00Z",
  "isImpersonating": false,
  "isAdmin": true,
  "isPlatformAdmin": false
}
```
**Errors:** 
- 401 Unauthorized - No authenticated user
- 404 Not Found - No tenant selected

### GET /api/tenant/available

**Purpose:** List all tenants available to the current user
**Authentication:** Required
**Parameters:** None (uses authenticated user ID from session)
**Response:**
```json
{
  "tenants": [
    {
      "id": "uuid",
      "name": "Tenant Name",
      "displayName": "Tenant Display Name",
      "isActive": true,
      "isPlatformTenant": false
    }
  ]
}
```
**Errors:**
- 401 Unauthorized - No authenticated user
- 500 Internal Server Error - Database connection failure

### POST /api/tenant/select

**Purpose:** Select a tenant as the current working context
**Authentication:** Required
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
    "tenantId": "uuid",
    "tenantName": "Tenant Name",
    "isPlatformTenant": false,
    "userRoles": ["Admin"],
    "selectedAt": "2025-08-25T10:00:00Z"
  }
}
```
**Errors:**
- 400 Bad Request - Invalid or missing tenant ID
- 401 Unauthorized - No authenticated session
- 403 Forbidden - User doesn't have access to this tenant
- 404 Not Found - Tenant doesn't exist or is inactive

### POST /api/tenant/switch

**Purpose:** Alias for /api/tenant/select - switches to a different tenant context
**Authentication:** Required
**Parameters:**
```json
{
  "tenantId": "uuid"
}
```
**Response:** Same as /api/tenant/select
**Errors:** Same as /api/tenant/select

### POST /api/tenant/clear

**Purpose:** Clear the current tenant selection
**Authentication:** Required
**Parameters:** None
**Response:**
```json
{
  "success": true,
  "message": "Tenant selection cleared"
}
```
**Errors:**
- 401 Unauthorized - No authenticated session

## Platform Admin Endpoints

All platform admin endpoints require the `PlatformAdmin` authorization attribute and are only accessible to users with Admin role in the platform tenant.

### GET /api/admin/tenants

**Purpose:** List all tenants in the system with pagination
**Authentication:** Required (Platform Admin)
**Parameters:** 
- `page` (int, optional): Page number for pagination (default: 1)
- `pageSize` (int, optional): Items per page (default: 20, max: 100)
**Response:**
```json
[
  {
    "id": "uuid",
    "name": "Tenant Name",
    "displayName": "Tenant Display Name",
    "isActive": true,
    "isPlatformTenant": false,
    "createdAt": "2025-08-01T10:00:00Z",
    "userCount": 25
  }
]
```
**Errors:**
- 401 Unauthorized - No authenticated session
- 403 Forbidden - Not a platform admin
- 500 Internal Server Error - Database error

### POST /api/admin/tenants

**Purpose:** Create a new tenant
**Authentication:** Required (Platform Admin)
**Parameters:**
```json
{
  "name": "Tenant Name",
  "description": "Optional description"
}
```
**Response:**
```json
{
  "id": "uuid",
  "name": "Tenant Name",
  "displayName": "Tenant Name",
  "isActive": true,
  "createdAt": "2025-08-25T10:00:00Z"
}
```
**Status Code:** 201 Created
**Errors:**
- 400 Bad Request - Missing or invalid tenant name
- 403 Forbidden - Not a platform admin
- 409 Conflict - Tenant with same name already exists

### GET /api/admin/tenant/{id}

**Purpose:** Get detailed statistics for a specific tenant
**Authentication:** Required (Platform Admin)
**Parameters:** 
- `id` (UUID, route): Tenant ID
**Response:**
```json
{
  "tenantId": "uuid",
  "name": "Tenant Name",
  "userCount": 25,
  "createdAt": "2025-08-01T10:00:00Z",
  "lastActivityAt": "2025-08-25T09:30:00Z",
  "isActive": true,
  "settings": {}
}
```
**Errors:**
- 403 Forbidden - Not a platform admin
- 404 Not Found - Tenant doesn't exist

### PUT /api/admin/tenant/{id}

**Purpose:** Update tenant information
**Authentication:** Required (Platform Admin)
**Parameters:**
- `id` (UUID, route): Tenant ID
```json
{
  "name": "Updated Name",
  "description": "Updated description"
}
```
**Status Code:** 204 No Content
**Errors:**
- 403 Forbidden - Not a platform admin
- 404 Not Found - Tenant doesn't exist

### POST /api/admin/tenant/{id}/deactivate

**Purpose:** Deactivate a tenant (soft delete)
**Authentication:** Required (Platform Admin)
**Parameters:** 
- `id` (UUID, route): Tenant ID
**Response:**
```json
{
  "success": true,
  "message": "Tenant deactivated"
}
```
**Errors:**
- 400 Bad Request - Cannot deactivate platform tenant
- 403 Forbidden - Not a platform admin
- 404 Not Found - Tenant doesn't exist

### POST /api/admin/tenant/{id}/users

**Purpose:** Add a user to a tenant with a specific role
**Authentication:** Required (Platform Admin)
**Parameters:** 
- `id` (UUID, route): Target tenant ID
- `userId` (string, query): User ID to add
- `email` (string, query): User email
- `role` (string, query, optional): Role to assign (default: "User")
**Response:**
```json
{
  "success": true,
  "message": "User added to tenant"
}
```
**Errors:**
- 400 Bad Request - Missing user ID
- 403 Forbidden - Not a platform admin
- 404 Not Found - Tenant doesn't exist
- 409 Conflict - User already assigned to tenant

### POST /api/admin/tenant/{id}/impersonate

**Purpose:** Platform admin impersonates a customer tenant for support
**Authentication:** Required (Platform Admin)
**Parameters:** 
- `id` (UUID, route): Target tenant to impersonate
**Response:**
```json
{
  "success": true,
  "tenantId": "uuid",
  "tenantName": "Customer Tenant",
  "message": "Now impersonating tenant: Customer Tenant"
}
```
**Side Effects:**
- Updates session with impersonation flag
- Sets impersonation expiry (1 hour)
- Logs admin action for audit
**Errors:**
- 401 Unauthorized - No active session
- 403 Forbidden - Not a platform admin
- 404 Not Found - Tenant doesn't exist

### POST /api/admin/tenant/stop-impersonation

**Purpose:** Stop impersonating a tenant and return to platform admin context
**Authentication:** Required (Platform Admin)
**Parameters:** None
**Response:**
```json
{
  "success": true,
  "message": "Stopped impersonating tenant"
}
```
**Side Effects:**
- Clears tenant selection from session
- Removes impersonation flag
- Logs admin action for audit
**Errors:**
- 400 Bad Request - Not currently impersonating
- 401 Unauthorized - No active session
- 403 Forbidden - Not a platform admin

## Controllers

### TenantController
Handles tenant selection and context management for regular users.

**Methods:**
- `GetCurrentTenant()` - Returns current tenant from session context
- `GetAvailableTenants()` - Lists user's accessible tenants from TenantUsers table
- `SelectTenant(Guid tenantId)` - Sets tenant in session and creates TenantContext
- `SwitchTenant(Guid tenantId)` - Alias for SelectTenant
- `ClearTenant()` - Removes tenant selection from session

### TenantAdminController
Handles platform administration operations for managing all tenants.

**Methods:**
- `GetAllTenants(int page, int pageSize)` - Lists all system tenants with pagination
- `CreateTenant(CreateTenantDto dto)` - Creates new tenant with unique slug
- `GetTenant(Guid id)` - Gets tenant statistics and details
- `UpdateTenant(Guid id, UpdateTenantDto dto)` - Updates tenant information
- `DeactivateTenant(Guid id)` - Soft deletes a tenant
- `AddUserToTenant(Guid id, string userId, string email, string role)` - Assigns user to tenant
- `ImpersonateTenant(Guid id)` - Creates impersonation session for support
- `StopImpersonation()` - Ends impersonation and returns to platform context

## Middleware

### TenantContextMiddleware
Automatically sets tenant context for each request based on session data.

```csharp
public class TenantContextMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip for non-authenticated requests
        if (!context.User.Identity.IsAuthenticated)
        {
            await _next(context);
            return;
        }

        // Extract session from cookie
        var sessionId = context.Request.Cookies["platform.session"];
        if (!string.IsNullOrEmpty(sessionId))
        {
            var sessionData = await _sessionService.GetSessionDataAsync(sessionId);
            if (sessionData?.SelectedTenantId.HasValue == true)
            {
                _tenantContext.SetTenant(sessionData.SelectedTenantId.Value);
                _tenantContext.SetUserId(sessionData.UserId);
                
                // Add to HttpContext.Items for downstream access
                context.Items["TenantId"] = sessionData.SelectedTenantId.Value;
                context.Items["UserId"] = sessionData.UserId;
                context.Items["IsImpersonating"] = sessionData.IsImpersonating;
            }
        }
        
        await _next(context);
    }
}
```

## Authorization

### PlatformAdminAttribute
Custom authorization attribute that verifies platform admin privileges.

```csharp
public class PlatformAdminAttribute : TypeFilterAttribute
{
    public PlatformAdminAttribute() : base(typeof(PlatformAdminFilter)) { }
}

public class PlatformAdminFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Extract session and verify platform admin status
        var sessionId = context.HttpContext.Request.Cookies["platform.session"];
        var sessionData = await _sessionService.GetSessionDataAsync(sessionId);
        
        if (!sessionData?.IsPlatformAdmin == true)
        {
            context.Result = new ForbidResult();
        }
    }
}
```

## Error Handling

All tenant-related endpoints return consistent error responses using the `ErrorResponse` model:

```json
{
  "error": "Error message",
  "statusCode": 400,
  "details": {
    "field": "Additional context"
  }
}
```

### Common Error Codes:
- **400 Bad Request** - Invalid request parameters or data
- **401 Unauthorized** - No authenticated session
- **403 Forbidden** - Insufficient permissions for operation
- **404 Not Found** - Resource (tenant/user) doesn't exist
- **409 Conflict** - Operation conflicts with existing data
- **500 Internal Server Error** - Unexpected server error

### Audit Logging:
Platform admin actions trigger warning-level logs for audit purposes:
- Tenant creation/modification/deletion
- User assignment to tenants
- Impersonation start/stop
- Cross-tenant queries

## Session Data Model

The session stores tenant context and user information:

```csharp
public class SessionData
{
    public string SessionId { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; }
    public Guid? SelectedTenantId { get; set; }
    public string? SelectedTenantName { get; set; }
    public List<string> TenantRoles { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public bool IsImpersonating { get; set; }
    public DateTimeOffset? ImpersonationExpiresAt { get; set; }
}
```

## Database Models

### Tenant Entity
```csharp
public class Tenant : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string DisplayName { get; set; }
    public bool IsActive { get; set; }
    public bool IsPlatformTenant { get; set; }
    public string? Settings { get; set; }
    
    // Audit columns
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
```

### TenantUser Entity
```csharp
public class TenantUser
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation
    public Tenant Tenant { get; set; }
    public ICollection<UserRole> UserRoles { get; set; }
}
```

## Testing

### Integration Test Coverage
- ✅ Tenant selection and switching
- ✅ Platform admin tenant management
- ✅ User-tenant assignments
- ✅ Impersonation workflow
- ✅ Authorization enforcement
- ✅ Cross-tenant isolation
- ✅ Session persistence

### Unit Test Coverage
- ✅ TenantService business logic
- ✅ TenantAdminService operations
- ✅ TenantContext state management
- ✅ Repository tenant filtering
- ✅ Middleware request processing