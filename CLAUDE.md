# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Global Standards

The following makes reference to ~ which on Windows refers to the value stored in the USERPROFILE environment variable. When interpreting any file reference commands that includes ~, double check that you have the correct user profile information. You can use Bash(echo $USERPROFILE). If you are unsure, ask the user.

### Development Standards

-   **Tech Stack Defaults:** @~/.agent-os/standards/tech-stack.md
-   **Code Style Preferences:** @~/.agent-os/standards/code-style.md
-   **Best Practices Philosophy:** @~/.agent-os/standards/best-practices.md

### Agent OS Instructions

-   **Initialize Products:** @~/.agent-os/instructions/core/plan-product.md
-   **Plan Features:** @~/.agent-os/instructions/core/create-spec.md
-   **Execute Tasks:** @~/.agent-os/instructions/core/execute-tasks.md
-   **Analyze Existing Code:** @~/.agent-os/instructions/core/analyze-product.md

## How These Work Together

1. **Standards** define your universal preferences that apply to all projects
2. **Instructions** guide the agent through Agent OS workflows
3. **Project-specific files** (if present) override these global defaults

## Project Overview

This is a microservices platform implementing Module Federation architecture with a React frontend host and a .NET 9 BFF (Backend for Frontend) using FastEndpoints.

## Product Documentation

-   **Folder with Product Documentation:** @.agent-os/product

## Technology Stack

### Frontend (platform-host)

-   **Framework**: React 18 with TypeScript
-   **Build Tool**: Modern.js with Webpack
-   **Module Federation**: Dynamic remote loading system
-   **UI Library**: Material-UI (MUI) with Emotion styling
-   **State Management**: React Query (TanStack Query)
-   **Authentication**: Cookie-based with JWT support

### Backend (platform-bff)

-   **Framework**: .NET 9 with FastEndpoints
-   **Authentication**: Duende IdentityServer with JWT Bearer
-   **API Documentation**: FastEndpoints.Swagger
-   **Testing**: xUnit with FluentAssertions

## Common Development Commands

### Frontend (platform-host)

```bash
cd platform-host

# Development server (runs on port 3002)
npm run dev

# Production build
npm run build

# Run tests
npm test

# Type checking
npm run type-check

# Linting
npm run lint

# Run specific test file
npm test -- path/to/test.test.tsx
```

### Backend (platform-bff)

```bash
cd platform-bff

# Run development server (runs on port 5000)
dotnet run

# Run tests
dotnet test

# Build project
dotnet build

# Run specific test
dotnet test --filter "FullyQualifiedName~LoginEndpointTests"
```

## Architecture Patterns

### Module Federation Configuration

The host application dynamically loads remote modules at runtime. Configuration is in `platform-host/modern.config.ts`. Remote modules are loaded through the `remoteLoader` service using webpack's ModuleFederationPlugin.

### API Structure

The BFF uses FastEndpoints pattern with:

-   Endpoints organized by feature in `platform-bff/Endpoints/`
-   Request/Response models in `platform-bff/Models/`
-   Each endpoint is a separate class inheriting from `Endpoint<TRequest, TResponse>`

### Authentication Flow

1. Frontend calls `/api/auth/login` endpoint
2. BFF validates credentials via IdentityServer
3. Sets HTTP-only cookie `platform.auth` with session
4. Frontend uses `AuthContext` to manage auth state
5. Protected endpoints require valid JWT or cookie authentication

### Shared Dependencies

Key libraries are marked as singletons in Module Federation to prevent duplication:

-   React & React-DOM
-   Material-UI components
-   React Query
-   Emotion (styling)

### Testing Approach

-   **Frontend**: Jest with React Testing Library, test files in `__tests__` folders
-   **Backend**: xUnit with WebApplicationFactory for integration tests
-   Both use AAA (Arrange-Act-Assert) pattern

### Key Services and Contexts

**Frontend Contexts** (`platform-host/src/contexts/`):

-   `AuthContext`: Authentication state and user management
-   `NavigationContext`: Dynamic navigation based on entitlements
-   `RemoteContext`: Module federation remote loading management

**Frontend Hooks** (`platform-host/src/hooks/`):

-   `useAuthentication`: Auth operations (login/logout)
-   `useEntitlements`: User permission checking
-   `useRemoteModule`: Dynamic module loading
-   `useApiQuery`: Typed API calls with React Query

**Backend Endpoints**:

-   `/api/auth/*`: Authentication operations
-   `/api/federation/*`: Module federation configuration
-   `/api/entitlements/*`: User permissions
-   `/health`: Health check endpoint

### Development Ports

-   Frontend: `http://localhost:3002`
-   BFF API: `http://localhost:5000`
-   Remote modules: Configured dynamically via federation config

## Important Conventions

-   All API responses use camelCase JSON formatting
-   Frontend components use Material-UI theming system
-   Error boundaries wrap remote modules for isolation
-   CORS is configured for local development ports
-   Authentication cookies use Lax SameSite policy for development
