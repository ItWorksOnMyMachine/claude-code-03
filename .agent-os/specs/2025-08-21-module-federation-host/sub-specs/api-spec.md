# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-08-21-module-federation-host/spec.md

> Created: 2025-08-21
> Version: 1.0.0

## Endpoints

### GET /api/federation/modules

**Purpose:** Retrieve the list of available micro frontend modules and their configuration
**Parameters:** None (uses session context for user entitlements)
**Response:** 
```json
{
  "modules": [
    {
      "name": "cms",
      "remoteEntry": "http://localhost:3003/remoteEntry.js",
      "exposedModule": "./App",
      "displayName": "Content Management",
      "icon": "description",
      "route": "/cms",
      "enabled": true
    }
  ]
}
```
**Errors:** 
- 401: Unauthorized (no valid session)
- 500: Internal server error

### GET /api/federation/config

**Purpose:** Get the Module Federation configuration including shared dependencies and runtime settings
**Parameters:** None
**Response:**
```json
{
  "shared": {
    "react": { "singleton": true, "requiredVersion": "^18.0.0" },
    "react-dom": { "singleton": true, "requiredVersion": "^18.0.0" },
    "@mui/material": { "singleton": true, "requiredVersion": "^5.0.0" }
  },
  "runtimeConfig": {
    "publicPath": "auto",
    "enableHMR": true
  }
}
```
**Errors:**
- 500: Internal server error

### GET /api/health

**Purpose:** Health check endpoint for the platform host application
**Parameters:** None
**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-08-21T10:00:00Z",
  "version": "1.0.0"
}
```
**Errors:** None (always returns 200)

## Controllers

### FederationController

**Location:** `platform-bff/Endpoints/Federation/`
**Responsibilities:**
- Module discovery and configuration
- Runtime federation settings
- User entitlement filtering

**Key Methods:**
- `GetModulesEndpoint`: Returns available modules filtered by user entitlements
- `GetConfigEndpoint`: Returns Module Federation runtime configuration

### HealthController

**Location:** `platform-bff/Endpoints/Health/`
**Responsibilities:**
- Application health monitoring
- Version information
- System status checks

**Key Methods:**
- `HealthCheckEndpoint`: Returns application health status