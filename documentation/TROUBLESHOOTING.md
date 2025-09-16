# Troubleshooting Guide

Common issues and solutions for the Platform Host development environment.

## Table of Contents

1. [Docker Issues](#docker-issues)
2. [Database Problems](#database-problems)
3. [Service Connection Errors](#service-connection-errors)
4. [Build and Compilation Issues](#build-and-compilation-issues)
5. [Authentication Problems](#authentication-problems)
6. [Performance Issues](#performance-issues)
7. [Development Environment Issues](#development-environment-issues)

## Docker Issues

### Docker Desktop Not Running

**Error:**
```
Error: Docker is not running. Please start Docker Desktop and try again.
```

**Solution:**
```powershell
# Windows
# 1. Start Docker Desktop from Start Menu
# 2. Wait for Docker icon in system tray to be stable
# 3. Verify Docker is running:
docker version

# macOS
open -a Docker
# Wait for Docker to fully start

# Linux
sudo systemctl start docker
sudo systemctl enable docker
```

### Docker Compose Not Found

**Error:**
```
docker-compose: command not found
```

**Solution:**
```bash
# Docker Desktop includes docker-compose
# For Linux, install separately:
sudo apt-get update
sudo apt-get install docker-compose-plugin

# Or use docker compose (v2 syntax):
docker compose up
```

### Container Fails to Start

**Error:**
```
Error: Container postgres-platform failed to start
```

**Solution:**
```powershell
# 1. Check container logs
docker logs postgres-platform

# 2. Remove and recreate containers
docker-compose down -v
docker-compose up -d

# 3. Check for port conflicts
netstat -an | findstr :5432
```

### Out of Disk Space

**Error:**
```
no space left on device
```

**Solution:**
```powershell
# Clean up Docker resources
docker system prune -a --volumes

# Check disk usage
docker system df

# Remove unused images
docker image prune -a

# Clean build cache
docker builder prune
```

## Database Problems

### Database Connection Refused

**Error:**
```
Connection to localhost:5432 refused
```

**Solution:**
```powershell
# 1. Check if container is running
docker ps | findstr postgres

# 2. Check health status
.\scripts\health-check.ps1

# 3. Restart database containers
docker-compose restart postgres-platform postgres-auth

# 4. Check firewall settings
# Windows: Allow PostgreSQL through firewall
# Linux: sudo ufw allow 5432/tcp
```

### Database Initialization Failed

**Error:**
```
ERROR: relation "tenants" does not exist
```

**Solution:**
```powershell
# Reset databases with fresh initialization
.\scripts\reset-db.ps1 -force

# Manually run init scripts
docker exec -it postgres-platform psql -U platform_user -d platform_db -f /docker-entrypoint-initdb.d/01-init.sql
```

### Authentication Failed

**Error:**
```
FATAL: password authentication failed for user "platform_user"
```

**Solution:**
```powershell
# 1. Check environment variables
cat .env | findstr POSTGRES

# 2. Ensure .env matches docker-compose.yml
# 3. Reset with correct credentials
docker-compose down -v
docker-compose up -d
```

### Port Already in Use

**Error:**
```
Error: Port 5432 is already allocated
```

**Solution:**
```powershell
# Windows - Find process using port
netstat -ano | findstr :5432
taskkill /PID <process_id> /F

# macOS/Linux
lsof -i :5432
kill -9 <process_id>

# Or change port in .env:
POSTGRES_PLATFORM_PORT=5433
```

## Service Connection Errors

### BFF Cannot Start

**Error:**
```
Unable to bind to https://host-bff.platform.local:5086 on the IPv4 loopback interface
```

**Solution:**
```powershell
# 1. Check for port conflicts
netstat -an | findstr :5000

# 2. Kill conflicting process or change port
# In .env:
BFF_PORT=5002

# 3. For development, use different launch profile
cd platform-host\platform-host-bff
dotnet run --urls "https://host-bff.platform.local:5086"
```

### Frontend Build Errors

**Error:**
```
Module not found: Can't resolve '@mui/material'
```

**Solution:**
```bash
# 1. Clean install dependencies
cd platform-host/platform-host-frontend
rm -rf node_modules package-lock.json
npm install

# 2. Clear npm cache
npm cache clean --force
npm install

# 3. Check Node version
node --version  # Should be 18+
```

### Redis Connection Failed

**Error:**
```
StackExchange.Redis.RedisConnectionException: No connection available
```

**Solution:**
```powershell
# 1. Check Redis container
docker ps | findstr redis

# 2. Test Redis connection
docker exec -it redis redis-cli ping
# Should return: PONG

# 3. Check Redis password in .env
REDIS_PASSWORD=DevRedisPass123!

# 4. Restart Redis
docker-compose restart redis
```

## Build and Compilation Issues

### .NET SDK Not Found

**Error:**
```
The SDK 'Microsoft.NET.Sdk.Web' specified could not be found
```

**Solution:**
```powershell
# 1. Install .NET 9 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0

# 2. Verify installation
dotnet --list-sdks

# 3. Set global.json if needed
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestFeature"
  }
}
```

### NuGet Package Restore Failed

**Error:**
```
Unable to load the service index for source https://api.nuget.org/v3/index.json
```

**Solution:**
```powershell
# 1. Clear NuGet cache
dotnet nuget locals all --clear

# 2. Check proxy settings
# Add to NuGet.config if behind proxy:
<configuration>
  <config>
    <add key="http_proxy" value="http://proxy.company.com:8080" />
  </config>
</configuration>

# 3. Use offline packages
dotnet restore --packages .\.nuget\packages
```

### TypeScript Compilation Errors

**Error:**
```
TS2307: Cannot find module 'react' or its corresponding type declarations
```

**Solution:**
```bash
# 1. Install TypeScript types
npm install --save-dev @types/react @types/react-dom

# 2. Check tsconfig.json
{
  "compilerOptions": {
    "moduleResolution": "node",
    "esModuleInterop": true,
    "jsx": "react-jsx"
  }
}

# 3. Restart TypeScript service in IDE
```

## Authentication Problems

### Login Fails

**Error:**
```
Invalid username or password
```

**Solution:**
```powershell
# 1. Verify test user exists
docker exec -it postgres-platform psql -U platform_user -d platform_db -c "SELECT email FROM users;"

# 2. Create test user if missing
.\scripts\create-user.ps1 -email "admin@platform.local" -password "Admin123!" -tenant "default"

# 3. Check Auth Service is running
.\scripts\health-check.ps1
```

### Token Validation Failed

**Error:**
```
Bearer error="invalid_token", error_description="The signature is invalid"
```

**Solution:**
```csharp
# 1. Ensure consistent signing keys
# Check appsettings.json in both BFF and Auth Service

# 2. Clear development certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust

# 3. Synchronize system time (if using VMs)
```

### Session Expired

**Error:**
```
Your session has expired. Please login again.
```

**Solution:**
```javascript
// 1. Check Redis for session storage
docker exec -it redis redis-cli
> KEYS session:*

// 2. Increase session timeout in BFF
services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
});

// 3. Check cookie settings
```

## Performance Issues

### Slow Container Startup

**Problem:** Containers take too long to start

**Solution:**
```powershell
# 1. Allocate more resources to Docker
# Docker Desktop > Settings > Resources
# RAM: 8GB minimum
# CPUs: 4 minimum

# 2. Use cached volumes in docker-compose.yml
volumes:
  - ./src:/app:cached

# 3. Disable real-time antivirus scanning for Docker folders
```

### High Memory Usage

**Problem:** Docker using excessive memory

**Solution:**
```powershell
# 1. Limit container memory
docker-compose.yml:
  postgres-platform:
    mem_limit: 1g

# 2. Clear unused resources
docker system prune -a --volumes

# 3. Restart Docker Desktop
```

### Database Queries Slow

**Problem:** Application responds slowly

**Solution:**
```sql
-- 1. Check for missing indexes
EXPLAIN ANALYZE SELECT * FROM users WHERE email = 'test@example.com';

-- 2. Add indexes
CREATE INDEX idx_users_email ON users(email);

-- 3. Analyze tables
ANALYZE users;

-- 4. Check connection pool settings
```

## Development Environment Issues

### Hot Reload Not Working

**Problem:** Changes not reflected without restart

**Solution:**
```bash
# Frontend - ensure webpack dev server running
cd platform-host/platform-host-frontend
npm run dev  # Not npm run build

# Backend - use dotnet watch
cd platform-host/platform-host-bff
dotnet watch run

# Check file watching limits (Linux)
echo fs.inotify.max_user_watches=524288 | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

### CORS Errors

**Error:**
```
Access to fetch at 'https://host-bff.platform.local:5086' from origin 'https://host-fe.platform.local:3002' has been blocked by CORS policy
```

**Solution:**
```csharp
// In Program.cs (BFF)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development",
        policy =>
        {
            policy.WithOrigins("https://host-fe.platform.local:3002")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

app.UseCors("Development");
```

### Module Federation Errors

**Error:**
```
Uncaught Error: Module "./Component" does not exist in container
```

**Solution:**
```javascript
// 1. Check remote module is running
// Should be accessible at: http://host-fe.platform.local:3003/remoteEntry.js

// 2. Verify exposes in remote's webpack config
exposes: {
  './Component': './src/Component'
}

// 3. Clear module federation cache
localStorage.clear();
window.location.reload();
```

### Environment Variables Not Loading

**Problem:** Application using wrong configuration

**Solution:**
```powershell
# 1. Check .env file exists
ls -la | findstr .env

# 2. Source order matters
# Priority: .env.local > .env

# 3. Restart after changes
docker-compose down
docker-compose up -d

# 4. Verify in container
docker exec -it platform-bff printenv | findstr DATABASE
```

## Quick Fixes Reference

### Reset Everything
```powershell
# Complete reset
docker-compose down -v
docker system prune -a --volumes
.\scripts\reset-db.ps1 -force
.\scripts\start-all.ps1 -d
```

### Check System Health
```powershell
# Comprehensive health check
.\scripts\health-check.ps1 -v
docker-compose ps
docker-compose logs --tail=50
```

### Common Port Changes
```env
# .env file
BFF_PORT=5002
AUTH_SERVICE_PORT=5003
FRONTEND_PORT=3003
POSTGRES_PLATFORM_PORT=5434
POSTGRES_AUTH_PORT=5435
REDIS_PORT=6380
```

### Emergency Cleanup
```bash
# Kill all Docker processes
docker kill $(docker ps -q)
docker rm $(docker ps -a -q)
docker rmi $(docker images -q)
docker volume rm $(docker volume ls -q)
```

## Getting Help

If issues persist:

1. **Check Logs**:
   ```powershell
   .\scripts\logs.ps1 -service <service-name>
   docker-compose logs --tail=100
   ```

2. **Enable Debug Mode**:
   ```env
   # .env
   DEBUG=true
   LOG_LEVEL=Debug
   ```

3. **Gather Diagnostics**:
   ```powershell
   docker version
   docker-compose version
   dotnet --info
   node --version
   npm --version
   ```

4. **Contact Support**:
   - Include error messages
   - Provide diagnostic output
   - Describe steps to reproduce

## CMS Module Issues

### CMS Module Not Loading

**Error:**
```
CMS module failed to load or shows error boundary
```

**Solutions:**
1. **Check Module Federation Configuration**:
   ```bash
   # Verify CMS module is running
   curl https://cms.platform.local:3003/health

   # Check remote entry is accessible
   curl https://cms.platform.local:3003/remoteEntry.js
   ```

2. **Verify Entitlements**:
   ```bash
   # Check if user has CMS_ACCESS entitlement
   curl -X GET "https://host-bff.platform.local:5086/api/entitlements" \
     -H "Cookie: platform.auth=<session-cookie>"

   # Should include "CMS_ACCESS" in entitlements array
   ```

3. **Check CORS Configuration**:
   ```bash
   # Verify CORS headers allow platform host access
   curl -H "Origin: https://host-fe.platform.local:3002" \
     -H "Access-Control-Request-Method: GET" \
     -X OPTIONS https://cms.platform.local:3003/remoteEntry.js
   ```

### GrapesJS Editor Not Initializing

**Error:**
```
Editor container is empty or shows error
```

**Solutions:**
1. **Check GrapesJS Dependencies**:
   ```bash
   cd modules/cms-module/cms-frontend
   npm list grapesjs grapesjs-react

   # Reinstall if needed
   npm install grapesjs@^0.21.10 grapesjs-react@^3.0.2
   ```

2. **Verify Editor Container**:
   ```typescript
   // Check if editor ref is properly attached
   useEffect(() => {
     if (editorRef.current && !editorInstance.current) {
       console.log('Editor container:', editorRef.current);
       // GrapesJS initialization code
     }
   }, []);
   ```

3. **CSS Loading Issues**:
   ```typescript
   // Ensure GrapesJS CSS is properly imported
   import 'grapesjs/dist/css/grapes.min.css';

   // Check browser developer tools for CSS loading errors
   ```

### CMS API Authentication Errors

**Error:**
```
401 Unauthorized or 403 Forbidden on CMS endpoints
```

**Solutions:**
1. **Check Session Cookie**:
   ```bash
   # Verify platform.auth cookie is present and valid
   # Check browser developer tools -> Application -> Cookies
   ```

2. **Verify Tenant Context**:
   ```bash
   # Check if tenant is selected
   curl -X GET "https://host-bff.platform.local:5086/api/tenant/current" \
     -H "Cookie: platform.auth=<session-cookie>"
   ```

3. **Check Entitlement Service**:
   ```bash
   # Test entitlement endpoint directly
   curl -X POST "https://host-bff.platform.local:5086/api/entitlements/check" \
     -H "Content-Type: application/json" \
     -H "Cookie: platform.auth=<session-cookie>" \
     -d '{"entitlement":"CMS_ACCESS"}'
   ```

### Asset Upload Failures

**Error:**
```
Asset upload fails or returns error
```

**Solutions:**
1. **Check File Size Limits**:
   ```bash
   # Verify file is within size limits (typically 10MB)
   # Check Content-Length header doesn't exceed server limits
   ```

2. **Verify MIME Type Support**:
   ```typescript
   // Check if file type is supported
   const supportedTypes = ['image/jpeg', 'image/png', 'image/gif', 'application/pdf'];
   if (!supportedTypes.includes(file.type)) {
     throw new Error('Unsupported file type');
   }
   ```

3. **Storage Path Issues**:
   ```bash
   # Ensure upload directory exists and is writable
   # Check disk space availability
   ```

### Database Migration Issues

**Error:**
```
CMS tables not found or migration errors
```

**Solutions:**
1. **Run CMS Migrations**:
   ```bash
   cd modules/cms-module/cms-bff
   dotnet ef database update

   # Or create migration if needed
   dotnet ef migrations add "CmsModuleSetup"
   ```

2. **Check Database Connection**:
   ```bash
   # Test CMS BFF database connection
   cd modules/cms-module/cms-bff
   dotnet ef database update --verbose
   ```

3. **Verify Entity Framework Configuration**:
   ```csharp
   // Check CmsDbContext is properly configured
   // Verify connection string in appsettings.json
   ```

### Module Federation Development Issues

**Error:**
```
Shared dependencies conflicts or version mismatches
```

**Solutions:**
1. **Check Shared Dependencies**:
   ```javascript
   // modules/cms-module/cms-frontend/module-federation.config.ts
   shared: {
     react: { singleton: true, eager: true },
     'react-dom': { singleton: true, eager: true },
     '@mui/material': { singleton: true, eager: true },
     // Ensure versions match between host and remote
   }
   ```

2. **Verify Module Isolation**:
   ```javascript
   // CMS-specific dependencies should NOT be shared
   shared: {
     'grapesjs': false,
     'grapesjs-react': false,
     'dompurify': false,
   }
   ```

3. **Clear Module Cache**:
   ```bash
   # Clear webpack cache
   rm -rf node_modules/.cache
   rm -rf dist
   npm run build
   ```

### Entitlement-Related Issues

**Error:**
```
"You don't have permission to access the CMS module" or missing UI elements
```

**Solutions:**
1. **Check User Entitlements**:
   ```bash
   # Verify user has required entitlements
   curl -X GET "https://host-bff.platform.local:5086/api/entitlements"

   # Should include: ["CMS_ACCESS", "CMS_MANAGE", "CMS_ASSETS"]
   ```

2. **Refresh Entitlement Cache**:
   ```typescript
   // In React component
   const { refresh } = useEntitlements();
   await refresh(); // Clears cache and reloads entitlements
   ```

3. **Check Platform vs Regular Tenant**:
   ```bash
   # Platform admin users get different entitlements
   # Regular tenant users have limited CMS access
   # Verify correct tenant is selected
   ```

## CMS Module Health Checks

### Verifying CMS Module Health

```bash
# Backend health check
curl https://cms.platform.local:5001/health

# Frontend health check
curl https://cms.platform.local:3003/health

# Expected response:
{
  "status": "healthy",
  "service": "CMS BFF",
  "dependencies": {
    "database": "healthy",
    "cms_content_table": "accessible (X records)"
  }
}
```