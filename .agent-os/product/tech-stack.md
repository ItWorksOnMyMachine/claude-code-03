# Technical Stack

## Frontend
- **Application Framework:** ModernJS with Module Federation
- **JavaScript Framework:** React (latest stable)
- **Language:** TypeScript
- **CSS Framework:** TailwindCSS 4.0+
- **UI Component Library:** Material UI (MUI)
- **Fonts Provider:** Google Fonts (self-hosted)
- **Icon Library:** Lucide React
- **CMS Editor:** GrapesJS

## Backend
- **Application Framework:** .NET 9 with MinimalAPIs and FastEndpoints
- **Language:** C#
- **API Strategy:** REST APIs with OpenAPI 3+ schemas
- **Architecture Pattern:** Backend for Frontend (BFF) for each micro frontend service

## Authentication Service (Separate Service)
- **Service Type:** Standalone authentication service (identity only, no tenant context)
- **Framework:** .NET 9 with Microsoft Identity Framework
- **OAuth/OIDC Provider:** Duende IdentityServer with EF Core stores
- **Database:** Dedicated PostgreSQL instance for all auth data:
  - Identity data (users only - no tenant associations)
  - Configuration store (clients, resources, scopes)
  - Operational store (grants, device codes, persisted grants)
- **User Model:** Standard IdentityUser (tenant associations managed by platform)
- **Authentication Flow:** OAuth2 Authorization Code + PKCE
- **Session Storage:** Duende Operational Store in PostgreSQL (NOT Redis)
- **Token Strategy:** 
  - JWTs issued by auth service (identity claims only)
  - Refresh tokens and reference tokens in Operational Store
  - Automatic cleanup via TokenCleanupService
- **Domain:** `auth.platform.com`
- **Features:**
  - User authentication (identity verification only)
  - Progressive account lockout
  - Password policies (global, not per-tenant)
  - Future: MFA, social logins, SAML

## Platform BFF Authentication Integration
- **Authentication Scheme:** Cookie Authentication (default)
- **Challenge Scheme:** OpenID Connect
- **OIDC Client:** Platform BFF acts as confidential client
- **Tenant Selection:** Post-authentication tenant selection flow
  - User authenticates (identity only)
  - Platform presents available tenants
  - Selected tenant stored in Redis session
  - Platform admin tenant available for company employees
- **Token Caching:** Redis for BFF-specific session data
  - Maps browser cookies to access/refresh tokens
  - Stores selected tenant context
  - Caches enriched claims per tenant
  - Stores BFF application state
- **Cookie Configuration:**
  - Name: `platform.auth`
  - HttpOnly: true
  - Secure: true
  - SameSite: Lax
  - Domain: `.platform.com`
- **Session Management:** 
  - BFF session in Redis (includes tenant context)
  - Auth sessions in Duende Operational Store
  - Token refresh handled via Duende token endpoint
  - Tenant switching without re-authentication

## Database
- **Database System:** PostgreSQL 17+
- **ORM:** Entity Framework Core 9 with standard Query Filters
- **Database Hosting:** Aurora Serverless V2 on AWS
- **Multi-tenant Strategy:** Shared database with row-level security
- **Tenant Structure:**
  - Organizations table (tenants)
  - TenantUsers table (user-tenant associations)
  - PlatformAdmins table (platform tenant members)
  - Special platform tenant for administrative access
- **Tenant Isolation:** EF Core query filters with repository pattern for access control
- **Admin Access:** Support for single-tenant, multi-tenant, and all-tenant access patterns via IgnoreQueryFilters()

## Testing
- **Frontend Testing:** Jest + React Testing Library
- **Backend Testing:** xUnit + Moq
- **E2E Testing:** Playwright
- **Code Coverage Target:** >80%

## Infrastructure
- **Application Hosting:** AWS ECS/Fargate (multiple services)
  - Authentication Service container
  - Platform BFF container  
  - Platform Host (React) container
  - Future micro frontend service containers
- **Database Hosting:** 
  - Aurora Serverless V2 (Platform database)
  - Aurora Serverless V2 (Authentication database - includes all Duende stores)
- **Cache Layer:** Amazon ElastiCache (Redis) for BFF application cache only
  - NOT for auth sessions (those are in Duende Operational Store)
  - Used for BFF token caching and application state
- **Asset Hosting:** Amazon S3
- **CDN:** CloudFront
- **Load Balancing:** Application Load Balancer with path/host routing
- **Deployment Solution:** GitHub Actions with Terraform
- **Code Repository URL:** [To be determined]

## Domain & Routing Architecture
- **Domain Strategy:** Single parent domain with service routing
- **Service Domains:**
  - Authentication: `auth.platform.com`
  - Platform BFF: `api.platform.com`
  - React Host: `app.platform.com`
  - CMS Service: `cms.platform.com`
  - Forms Service: `forms.platform.com`
- **Cookie Scope:** HttpOnly auth cookies set at parent domain level
- **Cookie Domain:** `.platform.com` (enables cross-subdomain access)
- **Service Access:** All services share the same HttpOnly auth cookie via domain scope
- **OIDC Flow:** Auth redirects handled between auth.platform.com and consuming services

## Security Architecture
- **Authentication Flow:** 
  - User authenticates with auth service via OIDC
  - Auth service issues JWT tokens to Platform BFF only
  - BFF stores tokens server-side, linked to session
  - Browser receives only HttpOnly session cookie
- **Token Isolation:** JWTs NEVER sent to browser - exist only in auth service and BFF
- **Cookie Security:**
  - HttpOnly: Prevents JavaScript access
  - Secure: HTTPS only
  - SameSite: CSRF protection
  - Domain-scoped: Shared across subdomains
- **API Communication:** 
  - Frontend calls BFF endpoints
  - BFF validates session cookie
  - BFF retrieves cached tokens
  - BFF adds Bearer token to backend API calls
- **Session Management:** 
  - Auth service maintains sessions in Duende Operational Store (PostgreSQL)
  - BFF maintains token cache in Redis (NOT auth sessions)
  - Duende handles persisted grants and reference tokens
  - Automatic token refresh before expiry via Duende
- **CSRF Protection:** 
  - SameSite cookie attributes
  - Anti-forgery tokens for state-changing operations
  - PKCE for OAuth flows
- **Security Guarantees:** 
  - Zero token exposure to browser JavaScript
  - Isolated authentication service perimeter
  - Encrypted token storage in BFF sessions
  - Complete audit trail of authentication events

## Module Federation
- **Host Framework:** ModernJS
- **Remote Loading:** Dynamic imports with runtime configuration
- **Shared Dependencies:** React, React-DOM, authentication context
- **Communication:** Event-based messaging between micro frontends
- **BFF Integration:** Each micro frontend service has its own BFF endpoint