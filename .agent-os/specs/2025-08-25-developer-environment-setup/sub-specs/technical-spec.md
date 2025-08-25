# Technical Specification

This is the technical specification for the spec detailed in @.agent-os/specs/2025-08-25-developer-environment-setup/spec.md

> Created: 2025-08-25
> Version: 1.0.0

## Technical Requirements

### Docker Compose Setup
- Multi-service orchestration with docker-compose.yml
- PostgreSQL 17+ container for platform database (port 5432)
- PostgreSQL 17+ container for auth database (port 5433)
- Redis 7+ container for session storage (port 6379)
- Network configuration for inter-service communication
- Volume mounts for data persistence
- Health checks for service readiness
- Environment variable injection

### Database Initialization
- SQL scripts for schema creation
- Entity Framework migrations for platform database
- Duende IdentityServer schema for auth database
- Test data seeding scripts including:
  - Platform tenant (ID: 00000000-0000-0000-0000-000000000001)
  - Test customer tenants
  - Test users with known passwords
  - User-tenant associations
  - Admin role assignments
- Automatic execution on container startup

### Service Configuration
- Auth service configuration for local development
- Platform BFF configuration with local URLs
- Frontend proxy configuration to backend services
- CORS settings for localhost origins
- Cookie configuration for local development
- OAuth client configuration with redirect URIs

### Development Scripts
- start-all.sh/ps1 - Start all services
- start-deps.sh/ps1 - Start only dependencies
- reset-db.sh/ps1 - Reset and reseed databases
- create-user.sh/ps1 - Create test user with parameters
- health-check.sh/ps1 - Verify all services are running
- logs.sh/ps1 - Tail logs from all containers

### Environment Files
- .env.example with all required variables documented
- docker.env for container environment
- Service-specific .env files for local development
- Clear documentation of each variable's purpose
- Sensible defaults for local development

### Documentation Structure
- README.md at repository root with quick start
- docs/DEVELOPER_SETUP.md with detailed instructions
- docs/ARCHITECTURE.md with system overview and diagrams
- docs/TROUBLESHOOTING.md for common issues
- docs/WORKFLOWS.md for development scenarios

## Approach

The technical implementation will focus on creating a containerized development environment that mirrors production architecture while remaining simple to set up and maintain. The approach emphasizes:

1. **Infrastructure as Code**: All environment configuration defined in version-controlled files
2. **Service Isolation**: Each service runs in its own container with defined interfaces
3. **Data Persistence**: Database volumes persist across container restarts
4. **Automated Setup**: Minimal manual configuration required after initial clone
5. **Developer Experience**: Fast startup times and clear feedback on service health

## External Dependencies

- **Docker Desktop** - Container runtime for local development
  - **Justification:** Industry standard for containerized development environments
  - **Version:** Latest stable release
  
- **Docker Compose** - Multi-container orchestration
  - **Justification:** Simplifies complex service dependencies
  - **Version:** v2.x (included with Docker Desktop)