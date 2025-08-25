# Tenant Management User Guide

> Version: 1.0.0  
> Updated: 2025-08-25

## Table of Contents
1. [Overview](#overview)
2. [For Regular Users](#for-regular-users)
3. [For Platform Administrators](#for-platform-administrators)
4. [Troubleshooting](#troubleshooting)
5. [Security Considerations](#security-considerations)

## Overview

The Multi-Tenant Platform provides secure, isolated environments for different organizations (tenants) while sharing the same application infrastructure. This guide covers how to work with tenants as both a regular user and as a platform administrator.

### Key Concepts

- **Tenant**: An isolated organization or company within the platform
- **Platform Tenant**: The special administrative tenant for platform management
- **Tenant Selection**: The process of choosing which tenant context to work in
- **Impersonation**: Platform admin feature to temporarily access a customer tenant for support
- **Cross-Tenant Access**: Ability for platform admins to manage all tenants

## For Regular Users

### Logging In and Tenant Selection

When you first log into the platform:

1. **Authentication**: After successful login, you'll be authenticated but not yet in any tenant context
2. **Tenant Selection Required**: Navigate to the tenant selection page or use the tenant selector in the header
3. **Available Tenants**: You'll see a list of all tenants you have access to

### Viewing Available Tenants

To see which tenants you can access:

```http
GET /api/tenant/available
```

This returns all tenants where you have an active membership, including:
- Tenant name and display name
- Your roles in each tenant
- Whether the tenant is currently active

### Selecting a Tenant

To start working in a specific tenant:

```http
POST /api/tenant/select
{
  "tenantId": "00000000-0000-0000-0000-000000000002"
}
```

Once selected:
- All your actions will be scoped to this tenant
- You'll only see data from this tenant
- Your permissions are based on your roles in this tenant

### Switching Between Tenants

If you have access to multiple tenants, you can switch between them:

```http
POST /api/tenant/switch
{
  "tenantId": "00000000-0000-0000-0000-000000000003"
}
```

The platform will:
- Save your current tenant context
- Switch to the new tenant
- Load your roles and permissions for the new tenant

### Viewing Current Tenant Context

To check which tenant you're currently working in:

```http
GET /api/tenant/current
```

This returns:
- Current tenant ID and name
- Your roles in this tenant
- Whether you're being impersonated by an admin
- When the tenant was selected

### Frontend Integration

In the React frontend, use the `TenantSelector` component:

```jsx
import { TenantSelector } from '@/components/TenantSelector';

// In your component
<TenantSelector 
  onTenantSelect={(tenantId) => handleTenantSelection(tenantId)}
  currentTenantId={currentTenant?.id}
  showHeader={true}
/>
```

The component handles:
- Fetching available tenants
- Displaying tenant cards with visual indicators
- Highlighting the currently selected tenant
- Showing platform tenant with special badge
- Error handling for failed selections

## For Platform Administrators

Platform administrators have special privileges to manage all tenants in the system. You must be logged in with Admin role in the Platform Tenant (ID: `00000000-0000-0000-0000-000000000001`).

### Accessing Admin Features

All admin endpoints require platform admin authorization and are prefixed with `/api/admin/`.

### Listing All Tenants

View all tenants in the system with pagination:

```http
GET /api/admin/tenants?page=1&pageSize=20
```

Returns comprehensive tenant information including:
- Tenant details (name, slug, display name)
- Active/inactive status
- User count
- Creation and last activity dates

### Creating New Tenants

Create a new customer tenant:

```http
POST /api/admin/tenants
{
  "name": "Acme Corporation",
  "description": "Enterprise customer account"
}
```

The system will:
- Generate a unique slug (e.g., "acme-corporation")
- Set up the tenant with default settings
- Enable the tenant immediately
- Log the creation for audit purposes

### Managing Tenant Status

#### Deactivate a Tenant
Soft-delete a tenant (data preserved but access blocked):

```http
POST /api/admin/tenant/{tenantId}/deactivate
```

Deactivation effects:
- Users cannot log into this tenant
- All tenant data is preserved
- Can be reactivated later if needed
- Audit log entry created

#### Update Tenant Information

```http
PUT /api/admin/tenant/{tenantId}
{
  "name": "Updated Company Name",
  "description": "Updated description"
}
```

### User Management

#### Adding Users to Tenants

Assign a user to a tenant with a specific role:

```http
POST /api/admin/tenant/{tenantId}/users?userId=user123&email=user@example.com&role=Admin
```

Available roles:
- `User`: Basic access
- `Admin`: Tenant administration
- `ReadOnly`: View-only access
- Custom roles as defined by the tenant

#### Viewing Tenant Statistics

Get detailed information about a specific tenant:

```http
GET /api/admin/tenant/{tenantId}
```

Returns:
- Total user count
- Active user count
- Storage usage
- Last activity timestamp
- Custom settings

### Impersonation for Support

Platform admins can temporarily access a customer tenant to provide support:

#### Start Impersonation

```http
POST /api/admin/tenant/{tenantId}/impersonate
```

When impersonating:
- Your session switches to the target tenant
- You have full access within that tenant
- All actions are logged with impersonation flag
- Session expires after 1 hour
- UI shows impersonation indicator

#### Stop Impersonation

Return to platform admin context:

```http
POST /api/admin/tenant/stop-impersonation
```

This will:
- Clear the tenant selection
- Return you to platform tenant context
- Log the end of impersonation session

### Audit and Compliance

All platform admin actions are logged at WARNING level for audit trails:

```log
[WARNING] Platform admin user123 created tenant 00000000-0000-0000-0000-000000000004 (Acme Corporation)
[WARNING] Platform admin user123 started impersonating tenant 00000000-0000-0000-0000-000000000004
[WARNING] Platform admin user123 deactivated tenant 00000000-0000-0000-0000-000000000005
```

### Best Practices for Platform Admins

1. **Use Impersonation Sparingly**: Only for legitimate support requests
2. **Document Actions**: Keep records of why admin actions were taken
3. **Regular Audits**: Review inactive tenants periodically
4. **User Assignment**: Verify user identity before granting tenant access
5. **Communication**: Notify customers before major changes to their tenant

## Troubleshooting

### Common Issues

#### "No Tenant Selected" Error

**Problem**: Trying to access tenant-scoped resources without selecting a tenant  
**Solution**: Call `/api/tenant/select` with a valid tenant ID first

#### "Access Denied" When Selecting Tenant

**Problem**: User doesn't have membership in the requested tenant  
**Solution**: Platform admin must add user to tenant using `/api/admin/tenant/{id}/users`

#### Impersonation Session Expired

**Problem**: Impersonation sessions expire after 1 hour  
**Solution**: Stop impersonation and start a new session if still needed

#### Cannot See Platform Admin Options

**Problem**: Admin menu items not visible  
**Solution**: Ensure you have Admin role in the Platform Tenant (check `/api/tenant/current`)

### Frontend Issues

#### Tenant Selector Not Loading

Check browser console for:
- Network errors fetching `/api/tenant/available`
- Authentication cookie issues
- CORS problems in development

#### Selected Tenant Not Persisting

Verify:
- Session cookie is being set correctly
- Backend session storage is working
- No middleware clearing the selection

## Security Considerations

### Data Isolation

- **Row-Level Security**: All database queries automatically filtered by TenantId
- **Global Query Filters**: Entity Framework applies tenant filters at ORM level
- **Repository Pattern**: BaseRepository enforces tenant context
- **No Cross-Tenant Queries**: Except for platform admins with explicit bypass

### Authentication & Authorization

- **Session-Based**: Tenant selection stored in server-side session
- **Cookie Security**: HTTP-only, Secure, SameSite cookies
- **Role Verification**: Roles checked per-tenant, not globally
- **Impersonation Limits**: Time-boxed, logged, admin-only

### Audit Requirements

All sensitive operations are logged:
- Tenant creation/modification/deletion
- User assignments
- Impersonation start/stop
- Cross-tenant queries by admins
- Failed authorization attempts

### Platform Tenant Protection

The platform tenant (ID: `00000000-0000-0000-0000-000000000001`):
- Cannot be deleted or deactivated
- Has special initialization on first run
- Only platform employees should have Admin role
- Used for system-wide management only

## API Quick Reference

### User Endpoints
- `GET /api/tenant/current` - Get current tenant context
- `GET /api/tenant/available` - List available tenants
- `POST /api/tenant/select` - Select a tenant
- `POST /api/tenant/switch` - Switch tenants
- `POST /api/tenant/clear` - Clear selection

### Admin Endpoints (Platform Admin Only)
- `GET /api/admin/tenants` - List all tenants
- `POST /api/admin/tenants` - Create tenant
- `GET /api/admin/tenant/{id}` - Get tenant details
- `PUT /api/admin/tenant/{id}` - Update tenant
- `POST /api/admin/tenant/{id}/deactivate` - Deactivate tenant
- `POST /api/admin/tenant/{id}/users` - Add user to tenant
- `POST /api/admin/tenant/{id}/impersonate` - Start impersonation
- `POST /api/admin/tenant/stop-impersonation` - Stop impersonation

## Support

For additional help:
- Review the [API Specification](./api-spec.md)
- Check the [Technical Architecture](./technical-spec.md)
- Contact platform administration team
- Submit issues through the support portal