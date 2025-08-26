# View Logs - Tail logs from Docker containers
# Aggregates logs from all or specific containers with filtering options
#
# Parameters:
#   -service  : Specific service to show logs for (optional)
#   -tail     : Number of recent lines to show (default: 100)
#   -follow   : Follow log output in real-time
#   -since    : Show logs since timestamp (e.g., "2m" for last 2 minutes)
#
# Usage:
#   .\logs.ps1                           # Show last 100 lines from all services
#   .\logs.ps1 -follow                   # Follow all logs in real-time
#   .\logs.ps1 -service platform-bff     # Show logs for specific service
#   .\logs.ps1 -tail 500 -since 5m       # Show last 500 lines from last 5 minutes
#   .\logs.ps1 -service redis -follow    # Follow Redis logs only
#
# Example:
#   .\scripts\logs.ps1 -service platform-bff -follow

param(
    [string]$service = "",     # Service name filter
    [int]$tail = 100,          # Number of lines to show
    [switch]$follow = $false,  # Follow logs in real-time
    [string]$since = ""        # Time filter
)

# Check if Docker is running
try {
    docker version | Out-Null
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

# Change to project root directory
Push-Location (Join-Path $PSScriptRoot "..")

try {
    # Build docker-compose command
    $args = @("logs")
    
    # Add tail parameter
    if ($tail -gt 0) {
        $args += "--tail"
        $args += $tail
    }
    
    # Add follow parameter
    if ($follow) {
        $args += "-f"
    }
    
    # Add since parameter
    if ($since) {
        $args += "--since"
        $args += $since
    }
    
    # Add service filter
    if ($service) {
        # Validate service exists
        $validServices = @(
            "postgres-platform",
            "postgres-auth",
            "redis",
            "platform-bff",
            "auth-service"
        )
        
        if ($service -notin $validServices) {
            Write-Host "✗ Invalid service name: $service" -ForegroundColor Red
            Write-Host "Valid services:" -ForegroundColor Yellow
            foreach ($validService in $validServices) {
                Write-Host "  - $validService" -ForegroundColor Gray
            }
            exit 1
        }
        
        $args += $service
        
        if ($follow) {
            Write-Host "Following logs for $service (Ctrl+C to stop)..." -ForegroundColor Cyan
        } else {
            Write-Host "Showing last $tail lines for $service..." -ForegroundColor Cyan
        }
    } else {
        if ($follow) {
            Write-Host "Following logs for all services (Ctrl+C to stop)..." -ForegroundColor Cyan
            Write-Host "Services: postgres-platform, postgres-auth, redis, platform-bff, auth-service" -ForegroundColor Gray
        } else {
            Write-Host "Showing last $tail lines from all services..." -ForegroundColor Cyan
            Write-Host "Services: postgres-platform, postgres-auth, redis, platform-bff, auth-service" -ForegroundColor Gray
        }
    }
    
    if ($since) {
        Write-Host "Filtering logs since: $since" -ForegroundColor Gray
    }
    
    Write-Host "" # Empty line before logs
    Write-Host "─" * 80 -ForegroundColor DarkGray
    
    # Execute docker-compose logs
    docker-compose $args
    
    if (-not $follow) {
        Write-Host "─" * 80 -ForegroundColor DarkGray
        Write-Host "`n📝 Log Tips:" -ForegroundColor Cyan
        Write-Host "  • Use -follow to see real-time logs" -ForegroundColor Gray
        Write-Host "  • Use -service [name] to filter by service" -ForegroundColor Gray
        Write-Host "  • Use -tail [number] to see more/fewer lines" -ForegroundColor Gray
        Write-Host "  • Use -since [time] for time-based filtering (e.g., '10m', '1h')" -ForegroundColor Gray
        Write-Host "`nExamples:" -ForegroundColor Yellow
        Write-Host "  .\scripts\logs.ps1 -follow" -ForegroundColor Gray
        Write-Host "  .\scripts\logs.ps1 -service postgres-platform -tail 200" -ForegroundColor Gray
        Write-Host "  .\scripts\logs.ps1 -since 5m -follow" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Error running docker-compose logs: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}