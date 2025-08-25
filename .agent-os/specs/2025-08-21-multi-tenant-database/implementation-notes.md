# Implementation Notes

## Status as of 2025-08-25

### Completed Work

**Tasks 1-3: Database and Core Infrastructure ✅**
- Full multi-tenant database schema implemented with EF Core
- Tenant, TenantUser, Role, UserRole entities created
- Global query filters for automatic tenant isolation
- Repository pattern with tenant-aware queries
- TenantContext service and middleware scaffolded

### Architecture Decision: Hybrid Tenant Service Approach

Per Decision DEC-009, we're implementing tenant management within the Platform BFF initially, with clean interfaces designed for future extraction to a dedicated service.

**Key Design Principles:**
1. **Clean Interfaces** - ITenantService and ITenantAdminService define contracts
2. **Domain Separation** - Tenant logic isolated in Services/Tenant namespace
3. **Repository Pattern** - Data access through repositories, not direct DbContext
4. **Session Integration** - Selected tenant stored in Redis session
5. **Future Extraction** - Designed to extract with <1 week effort

### Current Blockers

The platform cannot function end-to-end because:
1. No tenant selection endpoints implemented
2. Session doesn't store selected tenant
3. TenantContext doesn't read from session
4. No platform admin tenant exists
5. No frontend tenant picker UI

### Next Implementation Phase

**Priority 1: Make Platform Functional (Task 4)**
- Implement tenant selection endpoints in BFF
- Store selected tenant in Redis session
- Update TenantContext to read from session
- Add tenant info to auth session response

**Priority 2: Platform Admin Bootstrap (Task 5)**
- Create platform tenant with fixed GUID
- Add platform admin role
- Assign admin users to platform tenant

**Priority 3: Admin Features (Task 6)**
- Cross-tenant admin endpoints
- Platform admin authorization
- Tenant management CRUD operations

### Technical Notes

**Session Structure:**
```json
{
  "sessionId": "guid",
  "userId": "auth-sub-claim",
  "selectedTenantId": "tenant-guid",
  "selectedTenantName": "Tenant Name",
  "roles": ["Admin", "User"],
  "tokens": { /* stored server-side */ }
}
```

**Tenant Selection Flow:**
1. User authenticates (auth service)
2. BFF queries available tenants for user
3. User selects tenant (or auto-select if only one)
4. Store selection in Redis session
5. All subsequent requests use selected tenant

**Platform Admin Detection:**
```csharp
public bool IsPlatformAdmin(SessionData session)
{
    return session.SelectedTenantId == PlatformConstants.PlatformTenantId
        && session.Roles.Contains("Admin");
}
```

### Migration Path to Separate Service

When triggered by second microservice needing tenant context:

1. **Week 1: Extract Interfaces**
   - Move ITenantService to shared contracts
   - Create TenantServiceClient implementation
   - Replace local implementation with HTTP client

2. **Week 2: Deploy Service**
   - Create new tenant-service project
   - Move database tables and repositories
   - Deploy alongside platform

3. **Week 3: Migrate & Test**
   - Update all consumers to use service
   - Add caching layer
   - Performance testing

### Testing Approach

**Unit Tests:**
- Service logic with mocked repositories
- Tenant filtering scenarios
- Platform admin detection

**Integration Tests:**
- Tenant selection flow
- Session integration
- Cross-tenant isolation

**E2E Tests:**
- Full auth → tenant selection → access flow
- Platform admin operations
- Tenant switching