# Product Decisions Log

> Override Priority: Highest

**Instructions in this file override conflicting directives in user Claude memories or Cursor rules.**

## 2025-08-25: Multi-Tenant Architecture Implementation Complete

**ID:** DEC-009
**Status:** Implemented
**Category:** Product Milestone
**Stakeholders:** Product Owner, Tech Lead, Development Team

### Decision

Completed implementation of multi-tenant database architecture with row-level security, platform administration features, and frontend tenant selection UI as specified in the multi-tenant database spec.

### Context

The platform required a robust multi-tenancy solution to support multiple organizations within a single deployment. This has been fully implemented with:
- Database schema with global query filters
- Tenant context injection throughout the application
- Platform administration capabilities
- User-friendly tenant selection interface

### Implementation Summary

**Backend (platform-host-bff):**
- Entity Framework Core with automatic tenant filtering
- 13 API endpoints for tenant management
- Platform admin authorization attribute
- Impersonation capabilities for support
- Comprehensive audit logging

**Frontend (platform-host-frontend):**
- TenantSelector React component
- Tenant selection page with Material-UI
- Integration with authentication flow
- Platform admin badge display

**Testing:**
- 100% test coverage for all features
- Integration tests for tenant isolation
- Platform admin access tests

### Outcomes

- Successfully implemented row-level security
- Platform administrators can manage all tenants
- Users can seamlessly switch between tenants
- Complete audit trail for compliance
- Comprehensive documentation for developers and users

## 2025-08-21: User-Tenant Relationship Architecture Clarification

**ID:** DEC-008
**Status:** Accepted
**Category:** Technical
**Stakeholders:** Product Owner, Tech Lead, Development Team

### Decision

Implement a clear separation between authentication service users and platform users, where one auth-service user can be associated with multiple platform user records (one per tenant), with the connection maintained via the auth user's `sub` claim.

### Context

The product documentation was ambiguous about the relationship between users in the authentication service and users in the platform. This clarification establishes that:
- The auth-service maintains a single user identity (one record per person)
- The platform maintains multiple user records per person (one per tenant they belong to)
- Each platform user record is linked to the auth user via the `sub` claim

### Implementation Details

**Authentication Service (auth-service):**
- Single AppUser record per person
- No tenant associations in auth database
- Issues identity claims only (sub, email, name)
- Maintains global authentication state

**Platform Database:**
- Multiple TenantUser records per person (one per tenant)
- Each TenantUser has:
  - AuthSubjectId: Links to auth-service user's `sub` claim
  - TenantId: The tenant this user record belongs to
  - Email: Initially matches auth user but can diverge
  - Roles/Permissions: Specific to this tenant
- Platform administrators exist as users in a special "platform" tenant

### User Flow Example

1. John authenticates with auth-service (john@example.com)
   - Auth service returns `sub: "auth-user-123"`
2. Platform queries for TenantUsers where `AuthSubjectId = "auth-user-123"`
3. Platform finds:
   - TenantUser for Tenant A (Manager role)
   - TenantUser for Tenant B (Viewer role)
   - TenantUser for Platform Tenant (Admin role)
4. John selects Tenant A
5. Platform loads permissions for John's Tenant A user record

### Rationale

This architecture provides:
- Clean separation between identity and authorization
- Natural support for users in multiple organizations with different roles
- Flexibility for edge cases (email changes per tenant)
- Clear audit trail per tenant
- Simplified platform administration model

### Consequences

**Positive:**
- Users can have different roles/permissions per tenant
- Email addresses can diverge between auth and platform if needed
- Platform admins are just users in a special tenant
- Clean architectural boundaries

**Negative:**
- Data duplication (user info exists in both systems)
- Synchronization complexity for profile updates
- Initial connection requires matching by email

## 2025-08-21: Post-Authentication Tenant Selection Architecture

**ID:** DEC-002
**Status:** Accepted
**Category:** Technical
**Stakeholders:** Product Owner, Tech Lead, Security Team, Development Team

### Decision

Implement a post-authentication tenant selection flow where users authenticate first (establishing identity), then select their working tenant from available options. The platform will include a special "platform administration" tenant for company employees to manage customer organizations.

### Context

The platform needs to support complex multi-tenancy scenarios:
- Users may belong to multiple organizations with different roles in each
- Platform administrators (our employees) need to access customer tenants for support
- Users should be able to switch between tenants without re-authenticating
- Clean separation between identity (who you are) and authorization (what you can access)

### Alternatives Considered

1. **Tenant-Coupled Authentication**
   - Pros: Single step login, simpler initial implementation
   - Cons: Users need separate accounts per tenant, no cross-tenant support, difficult tenant switching

2. **URL-Based Tenant Selection (Subdomains)**
   - Pros: Clear tenant context in URL, allows bookmarking
   - Cons: Complex SSL certificate management, difficult cross-tenant operations, poor user experience for multi-tenant users

3. **Default Tenant with Switching**
   - Pros: Faster initial login, remembers preference
   - Cons: Confusion when default tenant changes, complex permission edge cases, unclear initial state

### Rationale

Post-authentication tenant selection provides the cleanest separation of concerns:
- Authentication service remains a pure identity provider
- Tenant context is managed at the platform level in server-side sessions
- Users can belong to multiple organizations naturally
- Platform administrators can access a special tenant for cross-organization management
- Switching tenants doesn't require re-authentication
- Security is enhanced by server-side session management

### Consequences

**Positive:**
- Clean architectural separation between identity and authorization
- Natural support for users in multiple organizations
- Simplified platform administration model
- Enhanced security through server-side tenant context
- Better user experience for multi-tenant users

**Negative:**
- Additional step in login flow (tenant selection)
- More complex session management
- Requires careful UI/UX design for tenant selection
- Additional backend endpoints for tenant operations

### Implementation Notes

1. Auth service authenticates users and establishes identity
2. Platform BFF manages tenant selection and context
3. Selected tenant stored in Redis-backed session
4. Special "platform" tenant created for administrative access
5. Frontend shows tenant selector after authentication
6. Tenant switcher available in application header

## 2025-08-12: Initial Product Planning

**ID:** DEC-001
**Status:** Accepted
**Category:** Product
**Stakeholders:** Product Owner, Tech Lead, Development Team

### Decision

Build an enterprise micro frontend platform to replace third-party SaaS dependencies (Squarespace, FormStack, etc.) with a modular, multi-tenant system featuring centralized authentication, entitlement-based access control, and progressive service migration capabilities.

### Context

The company currently relies on multiple third-party SaaS platforms for critical business functions, resulting in integration challenges, increased costs, and limited customization options. The fragmentation across services creates administrative overhead and prevents the implementation of company-specific workflows. The market opportunity exists to consolidate these services while maintaining flexibility for future growth.

### Alternatives Considered

1. **Continue with Third-Party Services**
   - Pros: No development cost, immediate availability, vendor support
   - Cons: Ongoing subscription costs, limited customization, integration challenges, vendor lock-in

2. **Monolithic Application Replacement**
   - Pros: Single codebase, simpler deployment, unified technology stack
   - Cons: Difficult to scale teams, longer release cycles, higher risk during updates, all-or-nothing migration

3. **Microservices with Traditional Frontend**
   - Pros: Backend scalability, service isolation, technology flexibility
   - Cons: Frontend remains monolithic, complex state management, poor user experience during service transitions

### Rationale

The micro frontend architecture with module federation provides the optimal balance between development flexibility and user experience. Key factors:
- Independent team scaling and deployment
- Progressive migration from existing services
- Centralized authentication with distributed authorization
- Reduced long-term operational costs
- Complete control over feature development and customization

### Consequences

**Positive:**
- 30-40% reduction in operational costs after full migration
- Complete customization control for business-specific needs
- Improved security through centralized authentication
- Faster feature delivery through independent service development
- Seamless scaling of individual services based on load

**Negative:**
- Initial development investment required
- Learning curve for micro frontend architecture
- Complexity in managing distributed services
- Need for robust testing across service boundaries

## 2025-08-12: Backend for Frontend (BFF) Security Pattern

**ID:** DEC-002
**Status:** Accepted
**Category:** Technical
**Stakeholders:** Tech Lead, Security Team, Development Team

### Decision

Implement a Backend for Frontend (BFF) pattern for each micro frontend service to handle all authentication and API communication, ensuring no authentication tokens are ever stored in browser localStorage or sessionStorage.

### Context

Traditional SPA architectures often store JWT tokens or refresh tokens in browser storage, creating significant security vulnerabilities to XSS attacks. With our multi-service architecture, each micro frontend needs secure API access while maintaining a seamless user experience. The BFF pattern provides a server-side proxy layer that handles all token management securely.

### Implementation Details

- Each micro frontend service will have its own dedicated BFF endpoint
- Authentication tokens stored exclusively in HttpOnly, Secure, SameSite cookies
- BFF layer unwraps HttpOnly cookies to retrieve session information
- BFF adds Bearer tokens to downstream API calls on behalf of the frontend
- All frontend API calls must be proxied through the BFF layer
- No direct frontend-to-API communication allowed

### Alternatives Considered

1. **Client-Side Token Storage with Short Expiry**
   - Pros: Simpler implementation, standard SPA pattern
   - Cons: Vulnerable to XSS attacks, requires complex refresh token rotation

2. **Shared Authentication Service with Direct API Access**
   - Pros: Centralized auth logic, reduced infrastructure
   - Cons: Tokens still exposed to frontend, complex CORS configuration

3. **Service Workers for Token Management**
   - Pros: Tokens isolated from main thread, some XSS protection
   - Cons: Browser compatibility issues, complex implementation, still client-side

### Rationale

The BFF pattern provides the highest level of security by ensuring authentication tokens never reach the browser's JavaScript context. This approach:
- Eliminates XSS token theft vulnerability completely
- Simplifies frontend development (no token management code)
- Enables server-side session management and monitoring
- Provides a natural point for request/response transformation
- Allows for backend-specific optimizations and caching

### Consequences

**Positive:**
- 100% protection against client-side token exposure
- Simplified frontend security model
- Centralized API versioning and transformation
- Better performance through server-side aggregation
- Enhanced audit and monitoring capabilities

**Negative:**
- Additional infrastructure layer to maintain
- Slightly increased latency for API calls
- More complex local development setup
- Requires careful session management strategy

## 2025-08-12: Shared Domain Architecture for Cookie-Based Authentication

**ID:** DEC-003
**Status:** Accepted
**Category:** Technical
**Stakeholders:** Tech Lead, Security Team, Infrastructure Team

### Decision

Deploy all micro frontend services and the host platform under a single parent domain, using either subdomain routing (e.g., `cms.platform.com`) or path-based routing (e.g., `platform.com/cms`) to ensure HttpOnly authentication cookies are accessible across all services.

### Context

The BFF security pattern requires that authentication cookies be accessible to all service endpoints. Browser security policies restrict cookie access to the domain where they were set. By deploying all services under a single parent domain, we enable seamless authentication sharing while maintaining security boundaries through the HttpOnly flag.

### Implementation Details

- **Cookie Domain Setting:** Set to `.platform.com` for subdomain routing or `/` for path routing
- **Cookie Attributes:** HttpOnly, Secure, SameSite=Strict
- **Service URLs:**
  - Host: `platform.com` or `app.platform.com`
  - CMS Service: `cms.platform.com` or `platform.com/cms`
  - Forms Service: `forms.platform.com` or `platform.com/forms`
  - API Gateway: `api.platform.com` or `platform.com/api`
- **BFF Endpoints:** Each service's BFF runs on the same subdomain/path as its frontend

### Alternatives Considered

1. **Cross-Domain with CORS and Token Relay**
   - Pros: Services can have independent domains
   - Cons: Complex CORS configuration, tokens must be passed explicitly, security risks

2. **OAuth2 with Redirect Flows**
   - Pros: Standard authentication flow, works across domains
   - Cons: Poor user experience with multiple redirects, complex state management

3. **Shared Authentication Service with PostMessage**
   - Pros: Central auth management, works across domains
   - Cons: Complex implementation, potential security vulnerabilities, browser compatibility issues

### Rationale

The shared domain approach provides the simplest and most secure authentication model:
- Native browser cookie handling (no custom code)
- Seamless authentication across all services
- No cross-domain complexity or CORS issues
- Maintains security through HttpOnly flag
- Supports both subdomain and path-based routing strategies

### Consequences

**Positive:**
- Simple, secure cookie sharing across all services
- No additional authentication infrastructure needed
- Seamless user experience with single sign-on
- Reduced complexity in local development
- Native browser security model

**Negative:**
- All services must be deployed under the same parent domain
- Domain migration becomes more complex
- Requires DNS configuration for subdomain approach
- Path-based routing may complicate service isolation

## 2025-08-13: Multi-tenant Database Architecture with Row-Level Security

**ID:** DEC-004
**Status:** Accepted
**Category:** Technical
**Stakeholders:** Tech Lead, Security Team, Database Team, Development Team

### Decision

Implement a shared database multi-tenant architecture using Entity Framework Core's query filters for row-level security, with support for three access patterns: single-tenant (regular users), multi-tenant (support staff), and all-tenant (super administrators).

### Context

The platform needs to support multiple independent organizations (tenants) while maintaining complete data isolation for security and compliance. Additionally, platform administrators require the ability to access data across tenants for support and maintenance purposes. The architecture must balance security, performance, and operational simplicity.

### Implementation Details

- **Shared Database:** All tenants share the same database instance and schema
- **Tenant Identification:** Every tenant-scoped table includes a TenantId column
- **Query Filters:** EF Core automatically applies tenant filters based on user context
- **Access Patterns:**
  - Single Tenant: Regular users see only their organization's data
  - Multiple Tenants: Support staff can access specific assigned tenants
  - All Tenants: Super administrators have unrestricted access
- **Audit Trail:** Cross-tenant access operations are logged for compliance
- **Dynamic Filtering:** DbContext instance properties enable runtime filter evaluation

### Alternatives Considered

1. **Database-per-Tenant**
   - Pros: Complete physical isolation, simple security model
   - Cons: High operational overhead, complex migrations, expensive scaling

2. **Schema-per-Tenant**
   - Pros: Logical isolation, easier than database-per-tenant
   - Cons: Still complex migrations, PostgreSQL schema limits, connection pool issues

3. **Static Query Filters**
   - Pros: Simple implementation, compile-time safety
   - Cons: Cannot support dynamic multi-tenant access patterns

### Rationale

The shared database with row-level security provides the optimal balance:
- Simplified operations with single database to maintain
- Cost-effective resource utilization
- Dynamic access patterns for different user types
- Native EF Core integration with query filters
- Straightforward backup and disaster recovery

### Consequences

**Positive:**
- 70% reduction in database operational costs versus database-per-tenant
- Single migration applies to all tenants simultaneously
- Efficient connection pooling and resource sharing
- Flexible access control for administrative users
- Simplified development and testing

**Negative:**
- Requires careful query filter implementation
- Potential for data leaks if filters are bypassed incorrectly
- Performance impact from additional WHERE clauses
- More complex backup/restore for individual tenants

## 2025-08-14: Simplified Test Authentication Implementation

**ID:** DEC-005
**Status:** Accepted (Temporary)
**Category:** Technical
**Stakeholders:** Development Team

### Decision

Implement a simplified test authentication system using Duende IdentityServer's TestUserStore with hardcoded users instead of the full database-backed authentication system originally planned, to accelerate development of other platform features.

### Context

During initial development, the team prioritized building the entitlement management system and multi-tenant infrastructure over completing the full authentication implementation. While Duende IdentityServer was configured, integrating it with the database user system would have required significant additional effort that would delay other critical features.

### What Was Built vs. Planned

**Originally Planned (DEC-002):**
- Full Duende IdentityServer integration with OAuth2/OIDC flows
- Database-backed user authentication via TenantUsers
- Server-side JWT generation for BFF-to-API communication only
- HttpOnly cookies for browser sessions (no JWT exposure to client)
- Complete integration between authentication and entitlements

**Actually Implemented:**
- TestUserStore with 3 hardcoded users (admin, user, alice)
- Simple cookie-based authentication (correct approach)
- Dummy Base64 token generation (should be server-side JWT)
- Duende IdentityServer configured but not actively used
- Entitlement system built but disconnected from authentication
- Currently returning fake tokens to browser (violates BFF pattern)

### Rationale

This temporary approach allowed the team to:
- Focus on building the complex entitlement management system
- Test multi-tenant functionality without authentication complexity
- Establish the BFF pattern and API structure
- Defer authentication complexity until core features were proven

### Migration Path

The following steps will complete the authentication system while maintaining BFF security:
1. Implement IUserStore for database-backed users
2. Replace TestUserStore with the database implementation
3. Enable proper OAuth2/OIDC flows in IdentityServer
4. Implement server-side session store to map cookies to JWTs
5. Generate real JWTs for server-to-server communication only
6. Remove token from login response (browser should only get success/failure)
7. Update BFF endpoints to extract cookie and retrieve associated JWT from session
8. Connect user authentication with the entitlement system
9. Add user registration and management endpoints

### Consequences

**Positive:**
- Accelerated development of core platform features
- Simplified testing during early development
- Reduced complexity while proving architecture patterns

**Negative:**
- Technical debt requiring future remediation
- Current system not production-ready
- Currently violates BFF pattern by sending token to browser
- Potential security vulnerabilities if deployed as-is
- Disconnect between authentication and authorization systems

**Timeline:** Full authentication implementation targeted for Phase 1.5 (see roadmap)

## 2025-08-13: Adoption of .NET 10 Preview for Named Query Filters

**ID:** DEC-006
**Status:** Accepted
**Category:** Technical
**Stakeholders:** Tech Lead, Development Team

### Decision

Adopt .NET 10 preview (August 2025) for the platform to leverage Entity Framework Core 10's named query filters feature, enabling cleaner multi-tenant implementation with independently controllable filters.

### Context

The current EF Core 9 implementation requires complex conditional logic within a single query filter to handle multiple concerns (tenant isolation, soft delete, etc.). EF Core 10 introduces named query filters that can be independently enabled/disabled, providing better separation of concerns. Since the platform won't be released until after .NET 10 GA (November 2025), using the preview version is acceptable.

### Implementation Details

- **Framework Version:** .NET 10.0 preview (current: preview.1 as of August 2025)
- **Named Filters:**
  - `SingleTenant`: Default active for regular users
  - `MultiTenant`: Conditionally active for admin users
  - `SoftDelete`: Independent concern for all queries
- **Filter Control:** Runtime enable/disable based on user context
- **Migration Path:** Minimal code changes when moving from preview to GA

### Alternatives Considered

1. **Stay on .NET 9 with Complex Filters**
   - Pros: Stable release, no preview risks
   - Cons: Complex conditional logic, harder to maintain

2. **Wait for .NET 10 GA**
   - Pros: No preview version risks
   - Cons: Delays development, blocks progress on multi-tenant features

3. **Custom Filter Implementation**
   - Pros: Full control over behavior
   - Cons: Significant development effort, maintenance burden

### Rationale

Adopting .NET 10 preview provides immediate access to named query filters:
- Cleaner separation of filtering concerns
- Easier to understand and maintain
- .NET 10 is late in preview cycle (low risk)
- Production release timeline aligns with .NET 10 GA
- Minimal breaking changes expected between preview and GA

### Consequences

**Positive:**
- 50% reduction in query filter complexity
- Independent control of each filtering concern
- Better testability with isolated filters
- Easier debugging with named filter tracking
- Future-proof architecture aligned with latest EF Core features

**Negative:**
- Using preview software in development
- Potential for minor API changes before GA
- Limited community resources and documentation
- Requires .NET 10 preview SDK installation for all developers

## 2025-08-15: Separate Authentication Service Architecture

**ID:** DEC-007
**Status:** Accepted
**Category:** Technical
**Stakeholders:** Product Owner, Tech Lead, Security Team, Development Team

### Decision

Implement authentication as a separate, dedicated service using Microsoft Identity Framework and Duende IdentityServer, with the Platform BFF acting as an OpenID Connect client. This replaces the previously planned embedded authentication approach.

### Context

The platform is being built to replace an existing monolithic system that currently serves production needs. With expectations of tens of thousands of users and a focus on long-term scalability over speed to market, the authentication architecture must support enterprise-scale operations from day one. The existing monolith provides breathing room to build the right solution rather than rushing a compromise.

### Architecture Overview

```
┌─────────────────┐         ┌──────────────────┐         ┌────────────────┐
│   Browser       │────────▶│   Platform BFF   │────────▶│  Backend APIs  │
│   (React)       │         │   (OIDC Client)  │         │   (.NET 9)     │
└─────────────────┘         └──────────────────┘         └────────────────┘
        │                            │                             
        │                            │                             
        ▼                            ▼                             
┌─────────────────┐         ┌──────────────────┐                  
│  Auth Service   │◀────────│    Auth DB       │                  
│  (Identity +    │         │  (PostgreSQL)    │                  
│   Duende)       │         └──────────────────┘                  
└─────────────────┘                                               
```

### Implementation Details

**Authentication Service:**
- Standalone .NET 9 service with Microsoft Identity Framework
- Duende IdentityServer for OAuth2/OIDC implementation
- Dedicated PostgreSQL database for user and identity data
- Multi-tenant user model with TenantUser extending IdentityUser
- Support for future expansion (MFA, social logins, SAML)

**Platform BFF Integration:**
- Configured as OIDC client with authorization code flow + PKCE
- Default authentication scheme: Cookie Authentication
- Challenge scheme: OpenID Connect
- Tokens stored in encrypted HttpOnly cookies (never exposed to browser)
- Automatic token refresh handled server-side

**Domain Strategy:**
- `auth.platform.com` - Authentication service
- `api.platform.com` - Platform BFF
- `app.platform.com` - React frontend
- Shared parent domain enables cookie access across services

### Alternatives Considered

1. **Embedded Authentication in BFF (Original Plan)**
   - Pros: Simpler deployment, fewer services, faster initial development
   - Cons: Limited scalability, harder to update independently, single point of failure

2. **Third-Party Auth Provider (Auth0/Okta)**
   - Pros: No maintenance burden, enterprise features out-of-box
   - Cons: Ongoing costs at scale, vendor lock-in, less control, data residency concerns

3. **Simple JWT with Refresh Tokens**
   - Pros: Stateless, simple implementation
   - Cons: Token exposure risks, complex refresh logic, no central session management

### Rationale

The separate authentication service architecture provides:
- **True scalability** for tens of thousands of users
- **Independent scaling** of auth vs business logic
- **Isolated security perimeter** for authentication operations
- **Reusability** across multiple platforms and future applications
- **Migration path** for existing monolith users
- **Zero-downtime updates** to authentication without affecting platform
- **Future flexibility** for enterprise features (SSO, SAML, compliance)

### Implementation Phases

**Phase 1: Auth Service Foundation (4-6 weeks)**
- Core Identity + Duende setup
- Multi-tenant user model
- Basic login/logout flows
- Development environment

**Phase 2: Platform Integration (2-3 weeks)**
- BFF OIDC client configuration
- Session synchronization
- Claims transformation
- Tenant context flow

**Phase 3: Production Hardening (3-4 weeks)**
- High availability setup
- Rate limiting
- Monitoring and alerting
- Security audit

**Phase 4: Advanced Features (4-6 weeks)**
- MFA implementation
- User registration flows
- Password reset
- Admin UI

### Consequences

**Positive:**
- Enterprise-scale authentication from day one
- Independent service evolution and deployment
- Potential to offer auth as a service to customers
- Clean separation of security concerns
- Easier compliance and security auditing
- Support for multiple consuming applications
- Gradual migration path from monolith

**Negative:**
- Additional 6-8 weeks development time vs embedded approach
- Increased operational complexity (two services vs one)
- More complex local development environment
- Additional monitoring and maintenance overhead
- Network hop for authentication operations
- Need for session synchronization between services

### Migration Strategy

This decision supersedes DEC-005 (Test Authentication) and provides the long-term solution. The migration path:
1. Complete auth service with test data
2. Integrate platform BFF as OIDC client
3. Migrate test users to new service
4. Enable production features progressively
5. Potentially migrate monolith users to shared auth service

## 2025-08-25: Hybrid Tenant Service Architecture - Start in BFF, Extract Later

**ID:** DEC-009
**Status:** Accepted
**Category:** Technical
**Stakeholders:** Product Owner, Tech Lead, Development Team

### Decision

Implement tenant management initially within the Platform BFF using clean, extractable interfaces and domain-driven design, with a planned migration path to a dedicated tenant service when additional microservices require tenant context.

### Context

The platform needs tenant management capabilities immediately to enable the basic user flow (authenticate → select tenant → access features). While a separate tenant service would be the ideal long-term architecture, the immediate priority is delivering a functional demo. The team needs to balance architectural purity with speed to market while avoiding technical debt that would make future extraction difficult.

### Architecture Approach

**Phase 1: BFF Implementation (Current)**
```
┌──────────┐     ┌─────────────┐     ┌──────────┐
│  Auth    │────▶│ Platform    │────▶│ Platform │
│ Service  │     │    BFF      │     │    DB    │
└──────────┘     │ (+ Tenant   │     └──────────┘
                 │   Logic)     │
                 └─────────────┘
```

**Phase 2: Extracted Service (Future)**
```
┌──────────┐     ┌─────────────┐     ┌──────────┐
│  Auth    │────▶│   Tenant    │◀────│ Platform │
│ Service  │     │   Service   │     │   BFF    │
└──────────┘     └─────────────┘     └──────────┘
                        ▲
                        │
                 ┌──────────────┐
                 │ Future        │
                 │ Microservices │
                 └──────────────┘
```

### Implementation Details

**Clean Interface Design:**
```csharp
// Domain interfaces that can become service contracts
public interface ITenantService
{
    Task<IEnumerable<TenantInfo>> GetAvailableTenantsAsync(string userId);
    Task<TenantContext> SelectTenantAsync(string userId, Guid tenantId);
    Task<bool> ValidateAccessAsync(string userId, Guid tenantId);
}

public interface ITenantAdminService
{
    Task<IEnumerable<Tenant>> GetAllTenantsAsync();
    Task<Tenant> CreateTenantAsync(CreateTenantDto dto);
    Task AssignUserToTenantAsync(Guid tenantId, string userId, string role);
}
```

**Extraction Triggers:**
- Addition of second microservice needing tenant context
- Tenant operations becoming performance bottleneck
- Need for sophisticated tenant caching strategies
- Requirement for tenant-specific event streaming

### Alternatives Considered

1. **Immediate Separate Service**
   - Pros: Clean architecture from start, no refactoring needed
   - Cons: 2-3 weeks additional development, delays demo, premature optimization

2. **Permanent BFF Integration**
   - Pros: Simplest architecture, no service communication overhead
   - Cons: Creates bottleneck, violates single responsibility, blocks future microservices

3. **Shared Library Approach**
   - Pros: Code reuse, consistent implementation
   - Cons: Database coupling, version management issues, no central authority

### Rationale

The hybrid approach provides optimal balance between speed and architecture:
- **Immediate functionality** for demo and MVP
- **Clean boundaries** enable extraction without major refactoring
- **Reduced complexity** during initial development
- **Learning opportunity** to understand tenant patterns before committing to service boundaries
- **Cost effective** - avoid premature service distribution

### Implementation Guidelines

1. **Domain Separation:**
   - Place tenant logic in separate namespace/project
   - Use repository pattern for data access
   - Define clear DTOs that can become API contracts

2. **Session Integration:**
   - Store selected tenant in Redis session
   - TenantContext middleware reads from session
   - Prepare for header-based propagation in future

3. **Admin Features:**
   - Separate admin interfaces from user interfaces
   - Use policy-based authorization
   - Audit all cross-tenant operations

4. **Future Extraction Path:**
   - Replace implementations with HTTP clients
   - Convert DTOs to API contracts
   - Move database tables to tenant service
   - Add caching layer at service boundary

### Consequences

**Positive:**
- Ship functional platform 2-3 weeks faster
- Validate tenant patterns with real usage
- Maintain clean architecture boundaries
- Reduce initial operational complexity
- Enable iterative refinement of tenant model

**Negative:**
- Temporary coupling of BFF and tenant logic
- Future extraction effort required (estimated 1 week)
- Initial deployment simpler but less scalable
- Potential for scope creep if boundaries not maintained

### Success Criteria

The implementation is successful if:
1. Extraction to separate service requires <1 week of effort
2. No breaking changes to frontend during extraction
3. Tenant logic remains isolated from BFF-specific concerns
4. All interfaces remain stable during extraction

### Timeline

- **Week 1-2:** Implement tenant selection in BFF
- **Week 3-4:** Add platform admin features
- **Month 2-3:** Evaluate extraction based on platform growth
- **Month 4+:** Extract if triggered by additional microservices