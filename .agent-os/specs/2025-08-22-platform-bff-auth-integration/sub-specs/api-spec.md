# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-08-22-platform-bff-auth-integration/spec.md

> Created: 2025-08-22
> Version: 1.0.0

## Endpoints

### POST /api/auth/login

**Purpose:** Initiate OIDC authentication flow
**Parameters:** 
```json
{
  "returnUrl": "/dashboard" // Optional, defaults to "/"
}
```
**Response:** 
```json
{
  "redirectUrl": "https://auth.platform.com/connect/authorize?..."
}
```
**Errors:** 
- 400 Bad Request - Invalid return URL

### POST /api/auth/logout

**Purpose:** Terminate user session and revoke tokens
**Parameters:** None (uses session cookie)
**Response:** 
```json
{
  "success": true,
  "redirectUrl": "/"
}
```
**Errors:** 
- 401 Unauthorized - No active session

### GET /api/auth/callback

**Purpose:** Handle OIDC callback after authentication
**Parameters:** 
- code (query): Authorization code from auth service
- state (query): CSRF protection state
**Response:** Redirect to original return URL or home
**Errors:** 
- 400 Bad Request - Invalid code or state
- 500 Internal Server Error - Token exchange failed

### GET /api/auth/session

**Purpose:** Get current user session information
**Parameters:** None (uses session cookie)
**Response:** 
```json
{
  "isAuthenticated": true,
  "user": {
    "id": "user-123",
    "email": "user@example.com",
    "name": "John Doe",
    "claims": {
      "sub": "user-123",
      "email": "user@example.com",
      "name": "John Doe"
    }
  },
  "expiresAt": "2025-08-22T12:00:00Z"
}
```
**Errors:** 
- 401 Unauthorized - No active session

### POST /api/auth/refresh

**Purpose:** Force refresh of access token
**Parameters:** None (uses session cookie)
**Response:** 
```json
{
  "success": true,
  "expiresAt": "2025-08-22T13:00:00Z"
}
```
**Errors:** 
- 401 Unauthorized - No active session
- 500 Internal Server Error - Refresh failed

### GET /api/auth/signout-callback

**Purpose:** Handle post-logout redirect from auth service
**Parameters:** 
- post_logout_redirect_uri (query): Where to redirect after logout
**Response:** Redirect to home or specified URL
**Errors:** None (always redirects)

## Authentication Flow

### Login Flow
1. Client calls POST /api/auth/login
2. BFF returns OIDC authorization URL
3. Client redirects to auth service
4. User authenticates at auth service
5. Auth service redirects to /api/auth/callback
6. BFF exchanges code for tokens
7. BFF stores tokens in Redis session
8. BFF sets secure session cookie
9. BFF redirects to original return URL

### Logout Flow
1. Client calls POST /api/auth/logout
2. BFF clears Redis session
3. BFF revokes tokens at auth service
4. BFF clears session cookie
5. BFF returns logout redirect URL
6. Client redirects to home or login

### Token Refresh Flow
1. On each API request, middleware checks token expiry
2. If token expires within 5 minutes, refresh automatically
3. BFF uses refresh token to get new access token
4. BFF updates Redis session with new tokens
5. Request continues with refreshed token

## Security Considerations

### CSRF Protection
- State parameter in OIDC flow
- SameSite cookie attribute
- Anti-forgery tokens for state-changing operations

### Token Storage
- Tokens encrypted at rest in Redis
- Session cookies use ASP.NET Core Data Protection
- Tokens never sent to browser

### Session Security
- HttpOnly cookies prevent JavaScript access
- Secure flag ensures HTTPS only
- SameSite=Lax prevents CSRF
- Sliding expiration extends active sessions
- Absolute timeout prevents indefinite sessions

## Error Handling

All authentication errors return consistent format:

```json
{
  "error": {
    "code": "AUTHENTICATION_FAILED",
    "message": "Unable to authenticate user",
    "details": {
      "reason": "Invalid credentials"
    }
  }
}
```

Common error codes:
- `AUTHENTICATION_FAILED` - Login failed
- `SESSION_EXPIRED` - Session has expired
- `TOKEN_REFRESH_FAILED` - Unable to refresh token
- `INVALID_STATE` - CSRF validation failed
- `LOGOUT_FAILED` - Unable to complete logout