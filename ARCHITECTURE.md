# Platform Architecture

## Overview

The Platform Host is a modern multi-tenant SaaS platform built using microservices architecture with Module Federation for the frontend and Backend-for-Frontend (BFF) pattern for API orchestration.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Client Browser                            │
└──────────────────────────────┬──────────────────────────────────────┘
                               │ HTTPS
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     React Module Federation Host                    │
│                     (host-fe.platform.local:3002)                   │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐     │
│  │   Shell    │  │  Remote 1  │  │  Remote 2  │  │  Remote N  │     │
│  │  (Host)    │  │  (Module)  │  │  (Module)  │  │  (Module)  │     │
│  └────────────┘  └────────────┘  └────────────┘  └────────────┘     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │ HTTP/REST
                               ▼
┌────────────────────────────────────────────────────────────────────┐
│                      Platform BFF (.NET 9)                         │
│                 (host-bff.platform.local:5086)                     │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │  FastEndpoints  │  Auth  │  Caching  │  Rate Limiting      │    │
│  └────────────────────────────────────────────────────────────┘    │
└─────────┬────────────────────┬────────────────────┬────────────────┘
          │                    │                    │
          ▼                    ▼                    ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│   Auth Service   │  │  PostgreSQL      │  │     Redis        │
│  (Duende IS6)    │  │   Databases      │  │     Cache        │
│ (login.platform  │  │  Platform: 5432  │  │ (localhost:6379) │
│  .local:5214)    │  │  Auth: 5433      │  │                  │
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

## Core Components

### Frontend Layer

#### React Module Federation Host
- **Technology**: React 18, TypeScript, Material-UI
- **Build Tool**: Modern.js with Webpack 5
- **Purpose**: Dynamically loads and orchestrates micro-frontends
- **Key Features**:
  - Runtime module loading
  - Shared dependencies (React, MUI, etc.)
  - Error boundaries for module isolation
  - Dynamic navigation based on entitlements

#### Module Federation Configuration
```javascript
// Shared dependencies prevent duplication
shared: {
  react: { singleton: true, requiredVersion: "^18.0.0" },
  "react-dom": { singleton: true },
  "@mui/material": { singleton: true },
  "@tanstack/react-query": { singleton: true }
}
```

### Backend Layer

#### Platform BFF (Backend for Frontend)
- **Technology**: .NET 9, FastEndpoints
- **Purpose**: API aggregation and frontend-specific logic
- **Responsibilities**:
  - Authentication/Authorization
  - API composition
  - Response formatting
  - Session management
  - Rate limiting

#### Authentication Service
- **Technology**: Duende IdentityServer 6
- **Protocol**: OAuth 2.0 / OpenID Connect
- **Features**:
  - JWT token issuance
  - Multi-tenant support
  - SSO capabilities
  - Refresh token rotation

### Data Layer

#### PostgreSQL Databases

**Platform Database (5432)**
```sql
Schemas:
├── public      # Shared platform data
├── tenant      # Tenant-specific data
└── audit       # Audit logs

Key Tables:
- tenants       # Tenant registry
- users         # User accounts
- roles         # Role definitions
- permissions   # Permission matrix
- user_tenants  # User-tenant associations
```

**Auth Database (5433)**
```sql
Tables:
- Clients              # OAuth clients
- ApiResources         # Protected APIs
- IdentityResources    # User claims
- PersistedGrants      # Tokens/consent
- Keys                 # Signing keys
```

#### Redis Cache
- **Purpose**: Session storage, distributed cache
- **Configuration**: Persistence disabled for dev
- **Key Patterns**:
  - `session:{sessionId}` - User sessions
  - `tenant:{tenantId}` - Tenant config cache
  - `user:{userId}` - User profile cache

## Design Patterns

### 1. Multi-Tenant Architecture

**Tenant Isolation Strategy**: Hybrid
- **Database**: Schema separation per tenant
- **Application**: Tenant context injection
- **Cache**: Key prefixing with tenant ID

```csharp
// Tenant resolution middleware
public class TenantMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = ResolveTenant(context);
        context.Items["TenantId"] = tenantId;
        await _next(context);
    }
}
```

### 2. Module Federation Pattern

**Dynamic Module Loading**:
```javascript
// Runtime module import
const RemoteModule = React.lazy(() => 
  loadRemoteModule({
    url: 'https://cms-fe.platform.local:3003/remoteEntry.js',
    scope: 'remoteApp',
    module: './Component'
  })
);
```

### 3. BFF Pattern

**API Aggregation**:
```csharp
public class DashboardEndpoint : Endpoint<EmptyRequest, DashboardResponse>
{
    public override async Task<DashboardResponse> ExecuteAsync()
    {
        // Aggregate data from multiple services
        var tasks = new[]
        {
            GetUserStats(),
            GetTenantMetrics(),
            GetSystemHealth()
        };
        
        await Task.WhenAll(tasks);
        return BuildResponse(tasks);
    }
}
```

### 4. Repository Pattern

```csharp
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}
```

## Security Architecture

### Authentication Flow

```
1. User Login
   Browser → BFF → Auth Service
   
2. Token Issuance
   Auth Service → JWT Token → BFF
   
3. Session Creation
   BFF → Session Cookie → Browser
   BFF → Session Data → Redis
   
4. Subsequent Requests
   Browser → Cookie → BFF
   BFF → Validate Session → Process Request
```

### Security Headers
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

### API Security
- JWT Bearer Authentication
- Role-Based Access Control (RBAC)
- API Rate Limiting
- Request/Response validation

## Scalability Considerations

### Horizontal Scaling
- **Frontend**: CDN distribution, lazy loading
- **BFF**: Stateless, load balanced
- **Database**: Read replicas, connection pooling
- **Cache**: Redis cluster support

### Performance Optimizations
- **Frontend**:
  - Code splitting
  - Tree shaking
  - Bundle optimization
  - Service worker caching
  
- **Backend**:
  - Response caching
  - Database query optimization
  - Async/await patterns
  - Connection pooling

### Monitoring Points
- Application Performance Monitoring (APM)
- Distributed tracing
- Health check endpoints
- Metrics collection

## Development Patterns

### Frontend State Management
```typescript
// React Query for server state
const { data, isLoading } = useQuery({
  queryKey: ['user', userId],
  queryFn: () => fetchUser(userId),
  staleTime: 5 * 60 * 1000, // 5 minutes
});

// Context for client state
const AuthContext = createContext<AuthState>();
const NavigationContext = createContext<NavState>();
```

### Backend Endpoint Structure
```csharp
public class CreateUserEndpoint : Endpoint<CreateUserRequest, UserResponse>
{
    public override void Configure()
    {
        Post("/api/users");
        Policies("RequireAdmin");
        Validator<CreateUserValidator>();
    }

    public override async Task<UserResponse> ExecuteAsync(
        CreateUserRequest req, 
        CancellationToken ct)
    {
        // Business logic here
    }
}
```

### Testing Strategy
- **Unit Tests**: Core business logic
- **Integration Tests**: API endpoints
- **E2E Tests**: Critical user flows
- **Performance Tests**: Load testing

## Deployment Architecture

### Container Strategy
```yaml
services:
  frontend:
    build: ./platform-host-frontend
    environment:
      - NODE_ENV=production
    
  bff:
    build: ./platform-host-bff
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    
  auth:
    build: ./auth-service
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```

### Environment Configuration
- **Development**: Local Docker containers
- **Staging**: Kubernetes cluster
- **Production**: Multi-region deployment

## Technology Stack Summary

| Layer | Technology | Purpose |
|-------|-----------|---------|
| Frontend | React 18 | UI Framework |
| | TypeScript | Type Safety |
| | Material-UI | Component Library |
| | Module Federation | Micro-frontends |
| | React Query | Server State |
| API Gateway | .NET 9 BFF | API Orchestration |
| | FastEndpoints | Endpoint Framework |
| Authentication | Duende IS6 | Identity Provider |
| Database | PostgreSQL 17 | Data Persistence |
| Cache | Redis 7 | Session/Cache |
| Container | Docker | Containerization |
| Orchestration | Docker Compose | Local Development |

## Future Considerations

### Planned Enhancements
1. GraphQL federation for complex queries
2. Event-driven architecture with message bus
3. Kubernetes orchestration for production
4. Service mesh for inter-service communication
5. Observability stack (Prometheus, Grafana, Jaeger)

### Potential Optimizations
1. Database sharding for scale
2. CDN integration for static assets
3. Edge computing for global distribution
4. WebAssembly modules for performance
5. Server-side rendering for SEO

## References

- [Module Federation Documentation](https://webpack.js.org/concepts/module-federation/)
- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [Duende IdentityServer](https://duendesoftware.com/products/identityserver)
- [PostgreSQL Multi-Tenant Patterns](https://www.postgresql.org/docs/current/ddl-schemas.html)
- [Redis Best Practices](https://redis.io/docs/manual/patterns/)