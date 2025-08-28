# Start Dependencies Only - PostgreSQL and Redis
# Starts only the dependency services without the application services
# Useful for local development when running BFF and Auth services from IDE
#
# Parameters:
#   -d      Run in detached mode
#   -fresh  Remove volumes and start fresh
#
# Usage:
#   .\start-deps.ps1          # Start dependencies in foreground
#   .\start-deps.ps1 -d       # Start dependencies in detached mode
#   .\start-deps.ps1 -fresh   # Remove volumes and start fresh
#
# Example:
#   .\scripts\start-deps.ps1 -d

param(
    [switch]$d = $false,        # Run in detached mode
    [switch]$fresh = $false     # Start with fresh volumes
)

# Check if Docker is running
Write-Host "Checking Docker status..." -ForegroundColor Yellow
try {
    docker version | Out-Null
    Write-Host "✓ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker is not running. Please start Docker Desktop and try again." -ForegroundColor Red
    exit 1
}

# Check if docker-compose.yml exists
$composeFile = Join-Path $PSScriptRoot ".." "docker-compose.yml"
if (-not (Test-Path $composeFile)) {
    Write-Host "✗ docker-compose.yml not found at $composeFile" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Found docker-compose.yml" -ForegroundColor Green

# Change to project root directory
Push-Location (Join-Path $PSScriptRoot "..")

try {
    # If fresh start requested, remove volumes
    if ($fresh) {
        Write-Host "`nRemoving existing volumes for fresh start..." -ForegroundColor Yellow
        docker-compose down -v
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Volumes removed" -ForegroundColor Green
        }
    }

    # Build command arguments
    $dockerArgs = @("up")
    if ($d) {
        $dockerArgs += "-d"
        Write-Host "`nStarting dependencies in detached mode..." -ForegroundColor Cyan
    } else {
        Write-Host "`nStarting dependencies in foreground mode (Ctrl+C to stop)..." -ForegroundColor Cyan
    }
    
    # Only start dependency services
    $dockerArgs += "postgres-platform"
    $dockerArgs += "postgres-auth"
    $dockerArgs += "redis"
    
    Write-Host "Services: postgres-platform, postgres-auth, redis`n" -ForegroundColor Gray
    
    # Run docker-compose up for postgres and redis services
    docker-compose @dockerArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✓ Dependencies started successfully!" -ForegroundColor Green
        
        if ($d) {
            Write-Host "`nDependency Service Ports:" -ForegroundColor Cyan
            Write-Host "  PostgreSQL Platform DB: localhost:5432" -ForegroundColor White
            Write-Host "    Database: platform_db" -ForegroundColor Gray
            Write-Host "    Username: platform_user" -ForegroundColor Gray
            Write-Host "    Password: platform_pass" -ForegroundColor Gray
            
            Write-Host "`n  PostgreSQL Auth DB: localhost:5433" -ForegroundColor White
            Write-Host "    Database: auth_db" -ForegroundColor Gray
            Write-Host "    Username: auth_user" -ForegroundColor Gray
            Write-Host "    Password: auth_pass" -ForegroundColor Gray
            
            Write-Host "`n  Redis: localhost:6379" -ForegroundColor White
            Write-Host "    No authentication required for local dev" -ForegroundColor Gray
            
            Write-Host "`nApplication services can now be started locally:" -ForegroundColor Cyan
            Write-Host "  BFF: cd platform-host\platform-host-bff && dotnet run" -ForegroundColor Gray
            Write-Host "  Auth Service: cd auth-service\AuthService && dotnet run" -ForegroundColor Gray
            Write-Host "  Frontend: cd platform-host\platform-host-frontend && npm run dev" -ForegroundColor Gray
            
            Write-Host "`nRun 'docker-compose ps' to see running containers" -ForegroundColor Gray
            Write-Host "Run 'docker-compose down' to stop dependencies" -ForegroundColor Gray
        }
    } else {
        Write-Host "✗ Failed to start dependencies. Check the logs above for errors." -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}