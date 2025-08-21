# Product Roadmap

## Phase 0: Already Completed

The following features have been implemented:

### Authentication Service (auth-service)
- [x] Standalone .NET 9 authentication service with Microsoft Identity Framework - `XL`
- [x] Duende IdentityServer with EF Core stores fully configured - `L`
- [x] PostgreSQL database integration for identity and IdentityServer stores - `M`
- [x] User identity model with AppUser and AppRole entities - `M`
- [x] OAuth2 authorization code flow + PKCE implementation - `L`
- [x] Multiple client configurations (platform-bff, test clients, admin clients) - `M`
- [x] Token issuance and validation with configurable lifetimes - `M`
- [x] Refresh token rotation with sliding expiration - `M`
- [x] Progressive account lockout service - `M`
- [x] Custom password validation with history tracking - `M`
- [x] Security audit logging for authentication events - `M`
- [x] Rate limiting middleware - `S`
- [x] Security headers middleware - `S`
- [x] Health check endpoints with database monitoring - `S`
- [x] Admin API endpoints for user and session management - `L`
- [x] MVC views for login/logout/consent flows - `M`
- [x] Docker containerization support - `M`
- [x] Comprehensive test suite with >80% coverage - `L`
- [x] OpenTelemetry integration for monitoring - `M`
- [x] Prometheus metrics exporter - `S`
- [x] Serilog structured logging with PostgreSQL sink - `M`

### Module Federation Platform Host (platform-host)
- [x] Module Federation host setup with ModernJS - `M`
- [x] React 18 with TypeScript application foundation - `M`
- [x] Webpack Module Federation plugin configured for runtime remote loading - `M`
- [x] Shared dependencies configuration (React, MUI, Emotion as singletons) - `S`
- [x] Material-UI theme provider and base styling - `S`
- [x] Application shell with header/sidebar/content layout - `M`
- [x] Error boundaries for remote module isolation - `S`
- [x] Loading and error fallback components - `S`
- [x] RemoteLoader service for dynamic module imports - `M`
- [x] ModuleRegistry service for module management - `M`
- [x] Module discovery utilities with health checking - `M`
- [x] ModuleFederationContext for state management - `M`
- [x] API proxy configuration to platform-bff (port 5000) - `S`
- [x] HMR (Hot Module Replacement) support - `S`
- [x] Production build optimizations with code splitting - `M`
- [x] Environment-specific configuration handling - `S`
- [x] Comprehensive test suite (74 tests passing) - `M`
- [x] Complete README documentation - `S`

## Phase 1: Foundation & Core Platform
**Goal:** Establish the micro frontend host with authentication and basic CMS functionality
**Success Criteria:** Successfully authenticate users and load/display micro frontend services

### Features
- [ ] Multi-tenant database schema and context - `M`
- [ ] Entitlement management system - `M`
- [ ] Dynamic navigation sidebar - `S`
- [ ] CMS micro frontend stub with GrapesJS - `L`

### Dependencies
- PostgreSQL database setup
- AWS infrastructure provisioning
- GitHub repository initialization

## Phase 1.5: Tenant Selection & Platform Integration
**Goal:** Implement post-authentication tenant selection in platform-host/platform-bff
**Success Criteria:** Users authenticate via auth-service (identity only), then select their working tenant in the platform, with platform administrators accessing a special administrative tenant

### Note: Authentication Service Already Completed
The auth-service has been fully implemented with all required features. The remaining work focuses on platform integration and tenant selection.

### Track A: Tenant Selection Implementation (platform-bff/platform-host)
- [ ] Tenant selection backend (platform-bff) - `L`
  - [ ] GET /api/tenant/available endpoint
  - [ ] POST /api/tenant/select endpoint
  - [ ] POST /api/tenant/switch endpoint
  - [ ] Session-based tenant context storage in Redis
  - [ ] Platform tenant detection for admins
- [ ] Tenant selection frontend (platform-host) - `M`
  - [ ] Tenant selection page/component
  - [ ] Available tenants display
  - [ ] Platform admin tenant option (conditional)
  - [ ] Tenant switcher in header
  - [ ] Redirect flow for no-tenant state
- [ ] Session management updates (platform-bff) - `M`
  - [ ] Store selected tenant in Redis session
  - [ ] Update SessionEndpoint for tenant context
  - [ ] Modify TenantContext to use session
  - [ ] Per-tenant claims caching
  - [ ] Remember last selected tenant
- [ ] Platform administration setup (platform-bff) - `L`
  - [ ] Create platform tenant in database
  - [ ] Platform admin role assignments
  - [ ] Cross-tenant query capabilities
  - [ ] Admin-specific navigation items
  - [ ] Audit logging for admin actions

### Track B: Platform BFF Integration with Auth Service (platform-bff)
- [ ] OIDC client configuration - `M`
  - [ ] Configure BFF as OIDC client to auth-service
  - [ ] Cookie authentication as default scheme
  - [ ] OpenID Connect as challenge scheme
  - [ ] Authority configuration pointing to auth-service
- [ ] Claims transformation updates - `M`
  - [ ] Defer tenant claims until selection
  - [ ] User identity claims only from auth-service
  - [ ] Tenant-specific claims post-selection from platform DB
  - [ ] Dynamic claims enrichment based on selected tenant
- [ ] Migration from test auth - `M`
  - [ ] Remove TestUserStore from platform-bff
  - [ ] Update authentication endpoints to use OIDC
  - [ ] Migrate test users to auth-service database
  - [ ] Update frontend auth flow in platform-host
  - [ ] Remove dummy token generation from platform-bff

### Dependencies
- Phase 1 completion
- Redis cache infrastructure (for BFF only, not auth)
- Docker development environment
- Additional AWS resources (second RDS instance for auth DB)

### Architecture Notes
**Service Separation:** Authentication runs as a completely independent service at `auth.platform.com`, with the Platform BFF acting as an OIDC client. 

**Tenant Selection Flow:** 
1. User authenticates with auth service (identity only)
2. Platform BFF receives authenticated user without tenant context
3. User is presented with available tenants (including platform admin tenant if applicable)
4. Selected tenant is stored in server-side session
5. All subsequent requests include tenant context from session

This separation enables:
- Users belonging to multiple organizations
- Switching tenants without re-authentication
- Platform administrators accessing special administrative features
- Clean separation of identity from authorization

## Phase 1.6: Advanced Authentication Features
**Goal:** Enhance authentication service with production-ready features
**Success Criteria:** Enterprise-grade authentication with MFA, user self-service, and compliance features

### Features
- [ ] Multi-factor authentication - `L`
  - [ ] TOTP authenticator app support
  - [ ] Email-based OTP
  - [ ] Backup codes
  - [ ] MFA enforcement per tenant
- [ ] User self-service - `L`
  - [ ] User registration with email verification
  - [ ] Password reset via email
  - [ ] Profile management UI
  - [ ] Security settings management
- [ ] Social login providers - `M`
  - [ ] Google OAuth integration
  - [ ] Microsoft Azure AD
  - [ ] Optional: GitHub, LinkedIn
- [ ] Enterprise features - `L`
  - [ ] SAML 2.0 support
  - [ ] Active Directory integration
  - [ ] Single Sign-On (SSO)
  - [ ] Session policies per tenant
- [ ] Compliance and audit - `M`
  - [ ] Comprehensive audit logging
  - [ ] GDPR compliance tools
  - [ ] Password history tracking
  - [ ] Login anomaly detection

### Dependencies
- Phase 1.5 completion
- Email service provider (SendGrid/SES)
- SMS provider for phone verification (optional)
- Security audit and penetration testing

## Phase 2: Enhanced CMS & Service Integration
**Goal:** Full-featured CMS replacement and micro service communication
**Success Criteria:** Complete CMS functionality matching Squarespace capabilities

### Features
- [ ] Advanced GrapesJS templates and blocks - `L`
- [ ] Asset management with S3 integration - `M`
- [ ] Publishing workflow with preview - `M`
- [ ] Submenu registration API - `S`
- [ ] Inter-service messaging system - `M`
- [ ] Role-based feature toggles - `S`

### Dependencies
- Phase 1 completion
- S3 bucket configuration
- CloudFront CDN setup

## Phase 3: Forms Service & Customer Portal
**Goal:** Replace FormStack functionality and enable customer self-service
**Success Criteria:** Customers can create and manage forms with response tracking

### Features
- [ ] Forms micro frontend service - `XL`
- [ ] Form builder interface - `L`
- [ ] Response collection and analytics - `M`
- [ ] Customer portal with SSO - `M`
- [ ] Unique URL generation for anonymous access - `S`
- [ ] Email notification system - `M`

### Dependencies
- Phase 2 completion
- Email service provider integration
- Enhanced security audit