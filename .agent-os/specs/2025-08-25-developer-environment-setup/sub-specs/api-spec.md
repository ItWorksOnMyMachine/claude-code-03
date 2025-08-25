# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-08-25-developer-environment-setup/spec.md

> Created: 2025-08-25
> Version: 1.0.0

## Development Support Endpoints

### GET /dev/health/all

**Purpose:** Comprehensive health check for all services
**Parameters:** None
**Response:**
```json
{
  "timestamp": "2025-08-25T10:00:00Z",
  "services": {
    "auth-service": {
      "status": "healthy",
      "url": "http://localhost:5001",
      "database": "connected",
      "version": "1.0.0"
    },
    "platform-bff": {
      "status": "healthy", 
      "url": "http://localhost:5000",
      "database": "connected",
      "redis": "connected",
      "version": "1.0.0"
    },
    "platform-frontend": {
      "status": "healthy",
      "url": "http://localhost:3002",
      "version": "1.0.0"
    },
    "postgres-platform": {
      "status": "healthy",
      "port": 5432,
      "databases": ["platformdb"]
    },
    "postgres-auth": {
      "status": "healthy",
      "port": 5433,
      "databases": ["authdb"]
    },
    "redis": {
      "status": "healthy",
      "port": 6379,
      "keys": 0
    }
  },
  "overall": "healthy"
}
```
**Errors:** 
- 503 Service Unavailable - One or more services unhealthy

### POST /dev/users/create

**Purpose:** Create test user for development (auth-service endpoint)
**Parameters:**
```json
{
  "email": "test@example.local",
  "password": "Password123!",
  "firstName": "Test",
  "lastName": "User",
  "roles": ["User"]
}
```
**Response:**
```json
{
  "userId": "auth-user-id",
  "email": "test@example.local",
  "created": true,
  "message": "User created successfully"
}
```
**Errors:**
- 400 Bad Request - Invalid email or password format
- 409 Conflict - User already exists
- 403 Forbidden - Only available in development environment

### POST /dev/users/assign-tenant

**Purpose:** Assign test user to tenant (platform-bff endpoint)
**Parameters:**
```json
{
  "userId": "auth-user-id",
  "email": "test@example.local",
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "roles": ["Admin"]
}
```
**Response:**
```json
{
  "success": true,
  "tenantUser": {
    "tenantId": "11111111-1111-1111-1111-111111111111",
    "userId": "auth-user-id",
    "email": "test@example.local",
    "roles": ["Admin"]
  }
}
```
**Errors:**
- 404 Not Found - User or tenant not found
- 409 Conflict - User already assigned to tenant
- 403 Forbidden - Only available in development environment

### POST /dev/database/reset

**Purpose:** Reset and reseed development databases
**Parameters:**
```json
{
  "target": "all|platform|auth",
  "seed": true
}
```
**Response:**
```json
{
  "success": true,
  "databases": ["platformdb", "authdb"],
  "migrations": 15,
  "seeded": true,
  "message": "Databases reset and seeded successfully"
}
```
**Errors:**
- 403 Forbidden - Only available in development environment
- 500 Internal Server Error - Database reset failed

### GET /dev/config/verify

**Purpose:** Verify all configuration settings are correct
**Parameters:** None
**Response:**
```json
{
  "environment": "Development",
  "configurations": {
    "auth": {
      "authority": "http://localhost:5001",
      "clientId": "platform-bff",
      "redirectUri": "http://localhost:5000/signin-oidc"
    },
    "database": {
      "platform": "connected",
      "auth": "connected"
    },
    "redis": {
      "connection": "localhost:6379",
      "status": "connected"
    },
    "cors": {
      "origins": ["http://localhost:3002", "http://localhost:5000"]
    }
  },
  "valid": true
}
```
**Errors:**
- 500 Internal Server Error - Configuration issues detected

## Docker Compose Service Endpoints

### Internal Service Discovery

Services communicate using Docker network hostnames:
- auth-service: http://auth-service:80
- platform-bff: http://platform-bff:80
- postgres-platform: postgres-platform:5432
- postgres-auth: postgres-auth:5432
- redis: redis:6379

### External Access Ports

Development access from host machine:
- Frontend: http://localhost:3002
- Platform BFF: http://localhost:5000
- Auth Service: http://localhost:5001
- Platform DB: localhost:5432
- Auth DB: localhost:5433
- Redis: localhost:6379

## Environment-Specific Behavior

All `/dev/*` endpoints are only available when:
- Environment = Development
- IsDevelopment() returns true
- Protected by DevelopmentOnlyAttribute

```csharp
[ApiController]
[Route("dev")]
[DevelopmentOnly] // Custom attribute that returns 403 in non-dev
public class DevController : ControllerBase
{
    // Development support endpoints
}
```

## Health Check Integration

Standard health check endpoints remain available:
- GET /health - Basic health check
- GET /health/ready - Readiness probe
- GET /health/live - Liveness probe

Development health endpoints provide additional detail:
- GET /dev/health/all - Comprehensive multi-service check
- GET /dev/health/dependencies - Check all external dependencies