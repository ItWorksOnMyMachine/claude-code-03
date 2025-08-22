# Technical Specification

This is the technical specification for the spec detailed in @.agent-os/specs/2025-08-22-platform-bff-auth-integration/spec.md

> Created: 2025-08-22
> Version: 1.0.0

## Technical Requirements

### OIDC Client Configuration
- Configure platform-bff as a confidential OIDC client in auth-service
- Set up redirect URIs for login callback (http://localhost:5000/signin-oidc)
- Configure post-logout redirect URI (http://localhost:5000/signout-callback-oidc)
- Enable authorization code flow with PKCE
- Configure client secrets securely in appsettings

### Authentication Middleware Setup
- Configure Cookie Authentication as default scheme
- Configure OpenID Connect as challenge scheme
- Set cookie name to "platform.auth"
- Enable HttpOnly, Secure, and SameSite=Lax for cookies
- Configure sliding expiration for session cookies
- Set up authority URL pointing to auth-service (http://localhost:5001)

### Redis Session Storage
- Implement distributed session storage using Redis
- Store access tokens, refresh tokens, and ID tokens server-side
- Map session cookies to token storage
- Implement token encryption at rest
- Configure session timeout and cleanup policies

### Token Management
- Implement automatic token refresh before expiry
- Handle refresh token rotation
- Store token expiry times for proactive refresh
- Implement token revocation on logout
- Add retry logic for token refresh failures

### Authentication Endpoints
- POST /api/auth/login - Initiate OIDC authentication flow
- POST /api/auth/logout - Clear session and revoke tokens
- GET /api/auth/callback - Handle OIDC callback
- GET /api/auth/session - Return current user info (no tokens)
- GET /api/auth/refresh - Force token refresh

### Claims Transformation
- Extract user ID (sub claim) from ID token
- Extract email and name claims
- Defer tenant-specific claims until tenant selection
- Store basic identity claims in session

### Security Headers
- Add X-Frame-Options: DENY
- Add X-Content-Type-Options: nosniff
- Add X-XSS-Protection: 1; mode=block
- Configure CORS for platform-host origin

## Approach

### Token Isolation Strategy
Implement complete token isolation by storing all tokens server-side in Redis. The browser receives only a session cookie that maps to the server-side token storage. This ensures tokens are never exposed to client-side JavaScript, preventing XSS token theft.

### Automatic Refresh Strategy
Implement proactive token refresh by checking token expiry on each request. When a token is within 5 minutes of expiry, automatically refresh it using the stored refresh token. This ensures seamless user experience without authentication interruptions.

### Session Management
Use Redis for distributed session storage to support horizontal scaling. Each session cookie maps to a Redis key containing encrypted tokens and user metadata. Implement sliding expiration to extend sessions on activity.

## External Dependencies

### NuGet Packages
- **Microsoft.AspNetCore.Authentication.OpenIdConnect** - OIDC authentication handler
- **Microsoft.AspNetCore.Authentication.Cookies** - Cookie authentication
- **Microsoft.Extensions.Caching.StackExchangeRedis** - Redis distributed cache
- **IdentityModel** - OIDC/OAuth2 client library
- **Microsoft.AspNetCore.DataProtection.StackExchangeRedis** - Redis data protection

### Infrastructure
- **Redis** - Required for session storage (can use Docker for development)
- **Auth Service** - Must be running at configured authority URL

### Configuration
- **OIDC Settings** - Client ID, Client Secret, Authority URL
- **Redis Connection** - Connection string for Redis instance
- **Cookie Settings** - Domain, expiration, security settings