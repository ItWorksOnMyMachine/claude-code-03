# Start All Services - Platform Stack
# Starts all services including databases, cache, auth service, BFF, and frontend
#
# Parameters:
#   -d      Run in detached mode
#   -build  Rebuild images before starting  
#   -force  Force recreate containers
#
# Usage:
#   .\start-all.ps1           # Start all services in foreground
#   .\start-all.ps1 -d        # Start all services in detached mode
#   .\start-all.ps1 -build    # Rebuild images before starting
#
# Example:
#   .\scripts\start-all.ps1 -d

param(
    [switch]$d = $false,        # Run in detached mode
    [switch]$build = $false,    # Rebuild images before starting
    [switch]$force = $false     # Force recreate containers
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

# Build command arguments
$dockerArgs = @("up")
if ($d) {
    $dockerArgs += "-d"
    Write-Host "Starting services in detached mode..." -ForegroundColor Cyan
} else {
    Write-Host "Starting services in foreground mode (Ctrl+C to stop)..." -ForegroundColor Cyan
}

if ($build) {
    $dockerArgs += "--build"
    Write-Host "Rebuilding images..." -ForegroundColor Cyan
}

if ($force) {
    $dockerArgs += "--force-recreate"
    Write-Host "Force recreating containers..." -ForegroundColor Cyan
}

# Change to project root directory
Push-Location (Join-Path $PSScriptRoot "..")

try {
    Write-Host "`nStarting all services..." -ForegroundColor Yellow
    Write-Host "Services: postgres-platform, postgres-auth, redis, platform-bff, auth-service`n" -ForegroundColor Gray
    
    # Run docker-compose up
    docker-compose @dockerArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✓ All services started successfully!" -ForegroundColor Green
        
        if ($d) {
            Write-Host "`nService URLs:" -ForegroundColor Cyan
            Write-Host "  Platform BFF:    http://localhost:5000" -ForegroundColor White
            Write-Host "  Auth Service:    http://localhost:5001" -ForegroundColor White
            Write-Host "  Frontend:        http://localhost:3002" -ForegroundColor White
            Write-Host "  PostgreSQL Platform: localhost:5432" -ForegroundColor White
            Write-Host "  PostgreSQL Auth:     localhost:5433" -ForegroundColor White
            Write-Host "  Redis:           localhost:6379" -ForegroundColor White
            Write-Host "`nRun '.\scripts\health-check.ps1' to verify all services are healthy" -ForegroundColor Gray
            Write-Host "Run '.\scripts\logs.ps1' to view logs" -ForegroundColor Gray
            Write-Host "Run 'docker-compose down' to stop all services" -ForegroundColor Gray
        }
    } else {
        Write-Host "✗ Failed to start services. Check the logs above for errors." -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}