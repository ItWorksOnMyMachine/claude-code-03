# Start All Services - Platform Stack
# Starts all services including databases, cache, auth service, BFF, and frontend
# Since application services are not containerized, this script:
# 1. Starts infrastructure services in Docker (PostgreSQL, Redis)
# 2. Starts application services locally (BFF, Auth Service, Frontend)
#
# Parameters:
#   -d      Run infrastructure in detached mode (application services run in new windows)
#   -build  Rebuild Docker images before starting  
#   -force  Force recreate Docker containers
#
# Usage:
#   .\start-all.ps1           # Start all services
#   .\start-all.ps1 -d        # Start all services in detached/background mode
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
    Write-Host "`nStarting infrastructure services (Docker)..." -ForegroundColor Yellow
    Write-Host "Services: postgres-platform, postgres-auth, redis`n" -ForegroundColor Gray
    
    # Start only infrastructure services in Docker (since app services are commented out)
    $dockerArgs += "postgres-platform", "postgres-auth", "redis"
    
    # Run docker-compose up for infrastructure
    docker-compose @dockerArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✓ Infrastructure services started successfully!" -ForegroundColor Green
        
        # Now start application services locally
        if ($d) {
            Write-Host "`nStarting application services locally..." -ForegroundColor Yellow
            
            # Start Auth Service in new window
            Write-Host "Starting Auth Service..." -ForegroundColor Cyan
            $authPath = Join-Path $PSScriptRoot ".." "auth-service" "AuthService"
            Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$authPath'; Write-Host 'Starting Auth Service on https://login.platform.local:5214' -ForegroundColor Green; dotnet run" -WindowStyle Normal
            
            # Start Platform BFF in new window
            Write-Host "Starting Platform BFF..." -ForegroundColor Cyan
            $bffPath = Join-Path $PSScriptRoot ".." "platform-host" "platform-host-bff"
            Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$bffPath'; Write-Host 'Starting Platform BFF on https://host-bff.platform.local:5086' -ForegroundColor Green; dotnet run" -WindowStyle Normal
            
            # Start Frontend in new window
            Write-Host "Starting Frontend..." -ForegroundColor Cyan
            $frontendPath = Join-Path $PSScriptRoot ".." "platform-host" "platform-host-frontend"
            Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$frontendPath'; Write-Host 'Starting Frontend on https://host-fe.platform.local:3002' -ForegroundColor Green; npm run dev" -WindowStyle Normal
            
            # Give services time to start
            Write-Host "`nWaiting for services to initialize..." -ForegroundColor Yellow
            Start-Sleep -Seconds 5
            
            Write-Host "`n✓ All services starting!" -ForegroundColor Green
            Write-Host "`nService URLs:" -ForegroundColor Cyan
            Write-Host "  Frontend:        https://host-fe.platform.local:3002" -ForegroundColor White
            Write-Host "  Platform BFF:    https://host-bff.platform.local:5086" -ForegroundColor White
            Write-Host "  Auth Service:    https://login.platform.local:5214" -ForegroundColor White
            Write-Host "  PostgreSQL Platform: localhost:5432" -ForegroundColor White
            Write-Host "  PostgreSQL Auth:     localhost:5433" -ForegroundColor White
            Write-Host "  Redis:           localhost:6379" -ForegroundColor White
            
            Write-Host "`nNote: Application services are running in separate windows" -ForegroundColor Yellow
            Write-Host "Close the PowerShell windows to stop application services" -ForegroundColor Gray
            Write-Host "Run 'docker-compose down' to stop infrastructure services" -ForegroundColor Gray
            Write-Host "`nRun '.\scripts\health-check.ps1' to verify all services are healthy" -ForegroundColor Gray
        } else {
            Write-Host "`nInfrastructure services are running in foreground mode." -ForegroundColor Yellow
            Write-Host "To start application services, run this script with -d flag or manually start:" -ForegroundColor Gray
            Write-Host "  Auth Service: cd auth-service\AuthService && dotnet run" -ForegroundColor Gray
            Write-Host "  BFF: cd platform-host\platform-host-bff && dotnet run" -ForegroundColor Gray
            Write-Host "  Frontend: cd platform-host\platform-host-frontend && npm run dev" -ForegroundColor Gray
        }
    } else {
        Write-Host "✗ Failed to start infrastructure services. Check the logs above for errors." -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}