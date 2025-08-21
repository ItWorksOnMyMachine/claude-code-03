# Technical Specification

This is the technical specification for the spec detailed in @.agent-os/specs/2025-08-21-multi-tenant-database/spec.md

> Created: 2025-08-21
> Version: 1.0.0

## Technical Requirements

### Database Schema Design
- Design PostgreSQL database schema with Tenants, TenantUsers, Roles, and UserRoles tables
- Implement TenantId column on all tenant-scoped tables for row-level isolation
- Design unique constraint on (UserId, TenantId) in TenantUsers to prevent duplicate associations
- Implement special platform tenant with fixed GUID for administrative operations
- Create flexible role-based authorization system with Roles and UserRoles tables
- Support system roles (Admin, Member) and custom roles per tenant
- Add CreatedAt, UpdatedAt, CreatedBy, UpdatedBy audit columns on all entities
- Configure cascade delete rules to maintain referential integrity when removing tenants
- Create database indexes on TenantId columns for query performance optimization
- Implement soft delete pattern with IsDeleted flag and automatic filtering
- Design tenant settings/configuration storage as JSONB column for flexibility

### Entity Framework Core Implementation
- Create Entity Framework Core 9 entities with proper navigation properties and relationships
- Configure DbContext with global query filters using HasQueryFilter for automatic tenant filtering
- Build base repository class with tenant-aware CRUD operations and IgnoreQueryFilters() for admin access
- Implement role-based access checks using UserRoles relationships
- Support permission-based queries through role Permissions JSONB field

### Tenant Context Management
- Implement ITenantContext service to track current tenant from session/claims
- Create TenantContextMiddleware to set tenant context from session for each request

### Testing & Validation
- Build tenant isolation validation tests to ensure query filters work correctly

## Approach

### Multi-Tenant Architecture Pattern
Implement a **single database, shared schema** multi-tenancy pattern with row-level security using TenantId discrimination. This approach provides:
- Cost efficiency through shared infrastructure
- Strong data isolation through automatic query filtering
- Simplified maintenance with single database schema
- Platform-level administration capabilities

### Entity Framework Global Query Filters
Leverage EF Core's global query filters to automatically append `WHERE TenantId = @currentTenant` to all queries, ensuring tenant isolation is enforced at the ORM level rather than requiring manual filtering in every query.

### Tenant Context Injection
Use dependency injection to provide tenant context throughout the application, allowing services to automatically scope their operations to the current tenant without manual tenant ID passing.

## External Dependencies

### Database
- **PostgreSQL 15+**: Primary database engine with JSONB support
- **Entity Framework Core 9**: ORM for .NET integration
- **Npgsql**: PostgreSQL provider for Entity Framework

### .NET Dependencies
- **Microsoft.EntityFrameworkCore.Design**: For migrations and tooling
- **Microsoft.Extensions.DependencyInjection**: For ITenantContext service registration
- **Microsoft.AspNetCore.Http**: For accessing HttpContext in middleware

### Testing Dependencies
- **Microsoft.EntityFrameworkCore.InMemory**: For unit testing with in-memory database
- **xUnit**: Testing framework
- **FluentAssertions**: Assertion library for readable test code