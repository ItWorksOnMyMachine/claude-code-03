# Development Workflows

Common development workflows and tasks for the Platform Host project.

## Table of Contents

1. [Daily Development Workflow](#daily-development-workflow)
2. [Feature Development](#feature-development)
3. [Database Workflows](#database-workflows)
4. [Testing Workflows](#testing-workflows)
5. [Debugging Workflows](#debugging-workflows)
6. [Multi-Tenant Development](#multi-tenant-development)
7. [Module Federation Workflows](#module-federation-workflows)
8. [Release Workflow](#release-workflow)

## Daily Development Workflow

### Starting Your Day

```powershell
# 1. Pull latest changes
git pull origin main

# 2. Start dependencies only (for local dev)
.\scripts\start-deps.ps1 -d

# 3. Check service health
.\scripts\health-check.ps1

# 4. Start development servers
# Terminal 1 - Backend
cd platform-host\platform-host-bff
dotnet watch run

# Terminal 2 - Frontend
cd platform-host\platform-host-frontend
npm run dev
```

### End of Day

```powershell
# 1. Run tests before committing
npm test
cd platform-host\platform-host-bff && dotnet test

# 2. Check for linting issues
cd platform-host\platform-host-frontend
npm run lint

# 3. Commit changes
git add .
git commit -m "feat: description of changes"

# 4. Stop services
docker-compose down
```

## Feature Development

### Creating a New API Endpoint

#### 1. Create the Endpoint

```csharp
// platform-host/platform-host-bff/Endpoints/Features/MyFeatureEndpoint.cs
public class MyFeatureEndpoint : Endpoint<MyFeatureRequest, MyFeatureResponse>
{
    public override void Configure()
    {
        Post("/api/features/my-feature");
        Policies("RequireAuthenticated");
    }

    public override async Task<MyFeatureResponse> ExecuteAsync(
        MyFeatureRequest req, 
        CancellationToken ct)
    {
        // Implementation
        return new MyFeatureResponse { Success = true };
    }
}
```

#### 2. Create Request/Response Models

```csharp
// platform-host/platform-host-bff/Models/MyFeatureRequest.cs
public class MyFeatureRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class MyFeatureResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}
```

#### 3. Add Validator

```csharp
// platform-host/platform-host-bff/Validators/MyFeatureValidator.cs
public class MyFeatureValidator : Validator<MyFeatureRequest>
{
    public MyFeatureValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3);
    }
}
```

#### 4. Test the Endpoint

```csharp
// platform-host/platform-host-bff.Tests/MyFeatureEndpointTests.cs
public class MyFeatureEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task MyFeature_Should_Return_Success()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new MyFeatureRequest { Name = "Test" };

        // Act
        var response = await client.PostAsJsonAsync("/api/features/my-feature", request);

        // Assert
        response.Should().BeSuccessful();
    }
}
```

### Creating a New React Component

#### 1. Create Component

```typescript
// platform-host/platform-host-frontend/src/components/MyComponent.tsx
import React from 'react';
import { Box, Typography } from '@mui/material';

interface MyComponentProps {
  title: string;
  onAction?: () => void;
}

export const MyComponent: React.FC<MyComponentProps> = ({ title, onAction }) => {
  return (
    <Box>
      <Typography variant="h4">{title}</Typography>
      <Button onClick={onAction}>Action</Button>
    </Box>
  );
};
```

#### 2. Create Hook for API Call

```typescript
// platform-host/platform-host-frontend/src/hooks/useMyFeature.ts
import { useMutation } from '@tanstack/react-query';
import { apiClient } from '../services/apiClient';

export const useMyFeature = () => {
  return useMutation({
    mutationFn: async (data: MyFeatureRequest) => {
      const response = await apiClient.post('/api/features/my-feature', data);
      return response.data;
    },
    onSuccess: (data) => {
      console.log('Feature executed successfully', data);
    },
  });
};
```

#### 3. Write Tests

```typescript
// platform-host/platform-host-frontend/src/components/__tests__/MyComponent.test.tsx
import { render, screen, fireEvent } from '@testing-library/react';
import { MyComponent } from '../MyComponent';

describe('MyComponent', () => {
  it('should render title', () => {
    render(<MyComponent title="Test Title" />);
    expect(screen.getByText('Test Title')).toBeInTheDocument();
  });

  it('should call onAction when button clicked', () => {
    const onAction = jest.fn();
    render(<MyComponent title="Test" onAction={onAction} />);
    
    fireEvent.click(screen.getByText('Action'));
    expect(onAction).toHaveBeenCalled();
  });
});
```

## Database Workflows

### Adding a New Table

#### 1. Create Migration Script

```sql
-- docker/sql/platform/migrations/001_add_features_table.sql
BEGIN;

CREATE TABLE IF NOT EXISTS features (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    is_enabled BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_features_tenant ON features(tenant_id);
CREATE INDEX idx_features_name ON features(name);

COMMIT;
```

#### 2. Apply Migration

```powershell
# Connect to database
docker exec -it postgres-platform psql -U platform_user -d platform_db

# Run migration
\i /docker/sql/platform/migrations/001_add_features_table.sql

# Verify
\dt features
```

### Seeding Test Data

#### 1. Create Seed Script

```sql
-- docker/sql/platform/seeds/features_seed.sql
INSERT INTO features (tenant_id, name, description) VALUES
    ((SELECT id FROM tenants WHERE name = 'Default Tenant'), 'Feature1', 'Test feature 1'),
    ((SELECT id FROM tenants WHERE name = 'Default Tenant'), 'Feature2', 'Test feature 2');
```

#### 2. Run Seed

```powershell
docker exec -it postgres-platform psql -U platform_user -d platform_db -f /docker/sql/platform/seeds/features_seed.sql
```

### Database Backup and Restore

#### Backup

```powershell
# Backup platform database
docker exec postgres-platform pg_dump -U platform_user platform_db > backup_platform_$(date +%Y%m%d).sql

# Backup auth database
docker exec postgres-auth pg_dump -U auth_user auth_db > backup_auth_$(date +%Y%m%d).sql
```

#### Restore

```powershell
# Restore platform database
docker exec -i postgres-platform psql -U platform_user platform_db < backup_platform_20240101.sql

# Restore auth database
docker exec -i postgres-auth psql -U auth_user auth_db < backup_auth_20240101.sql
```

## Testing Workflows

### Running All Tests

```powershell
# Frontend tests
cd platform-host\platform-host-frontend
npm test
npm run test:coverage

# Backend tests
cd platform-host\platform-host-bff
dotnet test --logger "console;verbosity=detailed"

# Integration tests
npm run test:integration

# E2E tests (requires all services running)
npm run test:e2e
```

### Testing a Specific Feature

```powershell
# Test specific frontend component
npm test MyComponent

# Test specific backend endpoint
dotnet test --filter "FullyQualifiedName~MyFeatureEndpointTests"

# Test with debugging
dotnet test --logger "console;verbosity=detailed" --filter "MyFeature"
```

### Performance Testing

```powershell
# Load testing with k6
k6 run tests/performance/load-test.js

# Stress testing
k6 run --vus 100 --duration 5m tests/performance/stress-test.js
```

## Debugging Workflows

### Backend Debugging

#### Visual Studio Code

```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug BFF",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/platform-host/platform-host-bff/bin/Debug/net9.0/platform-host-bff.dll",
      "args": [],
      "cwd": "${workspaceFolder}/platform-host/platform-host-bff",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:5000"
      }
    }
  ]
}
```

### Frontend Debugging

#### Browser DevTools

```javascript
// Add debugger statements
const MyComponent = () => {
  debugger; // Execution will pause here
  
  const handleClick = () => {
    console.log('Button clicked');
    debugger;
  };
  
  return <button onClick={handleClick}>Click me</button>;
};
```

#### VS Code Chrome Debugging

```json
// .vscode/launch.json
{
  "type": "chrome",
  "request": "launch",
  "name": "Debug Frontend",
  "url": "http://localhost:3002",
  "webRoot": "${workspaceFolder}/platform-host/platform-host-frontend",
  "sourceMaps": true
}
```

### Database Query Debugging

```sql
-- Enable query logging
docker exec -it postgres-platform psql -U platform_user -d platform_db

-- Set log level
SET log_statement = 'all';
SET log_duration = on;

-- Check slow queries
SELECT query, calls, mean_exec_time 
FROM pg_stat_statements 
ORDER BY mean_exec_time DESC 
LIMIT 10;
```

## Multi-Tenant Development

### Creating Tenant-Aware Features

#### 1. Add Tenant Context

```csharp
public class TenantContext
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; }
}

// In endpoint
public override async Task<Response> ExecuteAsync(Request req, CancellationToken ct)
{
    var tenantId = User.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
    // Use tenantId in queries
}
```

#### 2. Tenant-Scoped Queries

```csharp
public async Task<List<Feature>> GetFeaturesForTenant(Guid tenantId)
{
    return await _context.Features
        .Where(f => f.TenantId == tenantId)
        .ToListAsync();
}
```

### Testing Multi-Tenant Scenarios

```powershell
# Create test users for different tenants
.\scripts\create-user.ps1 -email "user1@tenant1.com" -password "Test123!" -tenant "tenant1"
.\scripts\create-user.ps1 -email "user2@tenant2.com" -password "Test123!" -tenant "tenant2"

# Test tenant isolation
# Login as user1, create data
# Login as user2, verify data is not visible
```

## Module Federation Workflows

### Adding a New Remote Module

#### 1. Create Remote Application

```bash
# Create new remote
npx create-mf-app
# Name: remote-feature
# Port: 3004
# Framework: react
# Language: typescript
```

#### 2. Configure Remote

```javascript
// remote-feature/webpack.config.js
module.exports = {
  name: 'remoteFeature',
  exposes: {
    './Feature': './src/Feature',
  },
  shared: {
    react: { singleton: true },
    'react-dom': { singleton: true },
  },
};
```

#### 3. Update Host Configuration

```javascript
// platform-host-frontend/modern.config.ts
export default defineConfig({
  runtime: {
    router: true,
    state: true,
  },
  deploy: {
    microFrontend: {
      moduleFederation: {
        remotes: {
          remoteFeature: 'remoteFeature@http://localhost:3004/remoteEntry.js',
        },
      },
    },
  },
});
```

#### 4. Load Remote Module

```typescript
// platform-host-frontend/src/components/RemoteLoader.tsx
const RemoteFeature = React.lazy(() => 
  import('remoteFeature/Feature').catch(() => ({
    default: () => <div>Remote module failed to load</div>
  }))
);

export const FeatureContainer = () => (
  <ErrorBoundary>
    <Suspense fallback={<CircularProgress />}>
      <RemoteFeature />
    </Suspense>
  </ErrorBoundary>
);
```

## Release Workflow

### Pre-Release Checklist

```powershell
# 1. Run all tests
npm test
cd platform-host\platform-host-bff && dotnet test

# 2. Check code coverage
npm run test:coverage

# 3. Run linting
npm run lint
dotnet format

# 4. Update documentation
# - Update CHANGELOG.md
# - Update API documentation
# - Update README if needed

# 5. Build production artifacts
npm run build
dotnet publish -c Release
```

### Creating a Release

```powershell
# 1. Create release branch
git checkout -b release/v1.0.0

# 2. Update version numbers
# package.json, *.csproj files

# 3. Build and test
npm run build:prod
dotnet build -c Release

# 4. Tag release
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0

# 5. Create Docker images
docker build -t platform-frontend:v1.0.0 ./platform-host/platform-host-frontend
docker build -t platform-bff:v1.0.0 ./platform-host/platform-host-bff
```

### Post-Release

```powershell
# 1. Deploy to staging
docker-compose -f docker-compose.staging.yml up -d

# 2. Run smoke tests
npm run test:smoke

# 3. Monitor logs
.\scripts\logs.ps1 -follow

# 4. Create release notes
# Document:
# - New features
# - Bug fixes
# - Breaking changes
# - Migration steps
```

## Continuous Integration Workflow

### GitHub Actions Example

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v2
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '9.0.x'
          
      - name: Setup Node
        uses: actions/setup-node@v2
        with:
          node-version: '20'
          
      - name: Install dependencies
        run: |
          npm ci
          dotnet restore
          
      - name: Run tests
        run: |
          npm test
          dotnet test
          
      - name: Build
        run: |
          npm run build
          dotnet build -c Release
```

## Performance Optimization Workflow

### Frontend Optimization

```bash
# Analyze bundle size
npm run analyze

# Optimize imports
# Use dynamic imports for large components
const HeavyComponent = React.lazy(() => import('./HeavyComponent'));

# Enable production mode
NODE_ENV=production npm run build
```

### Backend Optimization

```csharp
// Use async/await properly
public async Task<IEnumerable<Data>> GetDataAsync()
{
    // Good: Parallel execution
    var task1 = GetFromDatabase1Async();
    var task2 = GetFromDatabase2Async();
    await Task.WhenAll(task1, task2);
    
    // Use pagination
    return await _context.Data
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}
```

### Database Optimization

```sql
-- Add indexes for frequently queried columns
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_tenants_subdomain ON tenants(subdomain);

-- Analyze query performance
EXPLAIN ANALYZE SELECT * FROM large_table WHERE condition = true;

-- Vacuum and analyze tables
VACUUM ANALYZE users;
```

## Monitoring Workflow

### Application Monitoring

```powershell
# Check application health
curl http://localhost:5000/health
curl http://localhost:5001/health

# Monitor resource usage
docker stats

# Check application logs
.\scripts\logs.ps1 -service platform-bff -tail 100
```

### Database Monitoring

```sql
-- Check active connections
SELECT count(*) FROM pg_stat_activity;

-- Monitor slow queries
SELECT query, calls, total_exec_time, mean_exec_time
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 10;

-- Check table sizes
SELECT schemaname, tablename, pg_size_pretty(pg_total_relation_size(tablename::regclass))
FROM pg_tables
WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
ORDER BY pg_total_relation_size(tablename::regclass) DESC;
```

## Security Workflow

### Security Scanning

```powershell
# Scan for vulnerable packages
npm audit
dotnet list package --vulnerable

# Fix vulnerabilities
npm audit fix
dotnet add package [PackageName] --version [SafeVersion]

# Security headers check
# Ensure all responses include security headers
```

### Secret Management

```powershell
# Never commit secrets
# Use environment variables
# Add to .gitignore:
.env
.env.local
appsettings.Development.json

# Use secret management tools
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=..."
```

This comprehensive workflows document covers the most common development scenarios and provides clear, actionable steps for each workflow.