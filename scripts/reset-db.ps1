# Reset Databases - Drop and Recreate with Seed Data
# WARNING: This will destroy all data in the databases!
# Stops containers, removes volumes, and restarts with fresh databases
#
# Parameters:
#   -force     Skip confirmation prompt
#   -keepRedis Preserve Redis cache data
#
# Usage:
#   .\reset-db.ps1            # Interactive mode (prompts for confirmation)
#   .\reset-db.ps1 -force     # Skip confirmation prompt
#   .\reset-db.ps1 -keepRedis # Reset databases but preserve Redis cache
#
# Example:
#   .\scripts\reset-db.ps1

param(
    [switch]$force = $false,       # Skip confirmation prompt
    [switch]$keepRedis = $false    # Preserve Redis data
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

# Confirmation prompt
if (-not $force) {
    Write-Host "`n⚠️  WARNING: This will destroy all data in the databases!" -ForegroundColor Red
    Write-Host "This action cannot be undone." -ForegroundColor Yellow
    
    $confirmation = Read-Host "Are you sure you want to reset the databases? Type 'yes' to confirm"
    if ($confirmation -ne "yes") {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Change to project root directory
Push-Location (Join-Path $PSScriptRoot "..")

try {
    Write-Host "`nStopping all containers..." -ForegroundColor Yellow
    docker-compose down
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Containers stopped" -ForegroundColor Green
    }
    
    # Remove volumes
    if ($keepRedis) {
        Write-Host "`nRemoving database volumes (keeping Redis)..." -ForegroundColor Yellow
        docker volume rm claude-code-03_postgres_platform_data 2>$null
        docker volume rm claude-code-03_postgres_auth_data 2>$null
    } else {
        Write-Host "`nRemoving all volumes..." -ForegroundColor Yellow
        docker-compose down -v
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Volumes removed" -ForegroundColor Green
    }
    
    Write-Host "`nStarting fresh database containers..." -ForegroundColor Yellow
    docker-compose up -d platform-postgres auth-postgres redis
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Database containers started" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to start database containers" -ForegroundColor Red
        exit 1
    }
    
    # Wait for databases to be ready
    Write-Host "`nWaiting for databases to initialize..." -ForegroundColor Yellow
    $maxAttempts = 30
    $attempt = 0
    $platformReady = $false
    $authReady = $false
    
    while (($attempt -lt $maxAttempts) -and (-not ($platformReady -and $authReady))) {
        $attempt++
        Write-Host "  Checking databases... (attempt $attempt/$maxAttempts)" -ForegroundColor Gray
        
        # Check platform database
        if (-not $platformReady) {
            try {
                docker exec platform-postgres pg_isready -U platformuser -d platformdb 2>$null | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    $platformReady = $true
                    Write-Host "    ✓ Platform database ready" -ForegroundColor Green
                }
            } catch {}
        }
        
        # Check auth database
        if (-not $authReady) {
            try {
                docker exec auth-postgres pg_isready -U authuser -d authdb 2>$null | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    $authReady = $true
                    Write-Host "    ✓ Auth database ready" -ForegroundColor Green
                }
            } catch {}
        }
        
        if (-not ($platformReady -and $authReady)) {
            Start-Sleep -Seconds 2
        }
    }
    
    if ($platformReady -and $authReady) {
        Write-Host "`n✓ Databases initialized and ready!" -ForegroundColor Green
        
        # Run migrations if BFF is available
        Write-Host "`nAttempting to run Entity Framework migrations..." -ForegroundColor Yellow
        $bffPath = Join-Path $PSScriptRoot ".." "platform-host" "platform-host-bff"
        if (Test-Path $bffPath) {
            Push-Location $bffPath
            try {
                dotnet ef database update 2>$null | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✓ Migrations applied successfully" -ForegroundColor Green
                } else {
                    Write-Host "⚠ Could not apply migrations automatically. Run manually when BFF starts." -ForegroundColor Yellow
                }
            } catch {
                Write-Host "⚠ Could not apply migrations automatically. Run manually when BFF starts." -ForegroundColor Yellow
            } finally {
                Pop-Location
            }
        }
        
        Write-Host "`n✅ Database reset complete!" -ForegroundColor Green
        Write-Host "`nDatabases have been reset with seed data:" -ForegroundColor Cyan
        Write-Host "  Platform DB: Contains platform tenant and test data" -ForegroundColor White
        Write-Host "  Auth DB: Contains Duende IdentityServer schema and OAuth clients" -ForegroundColor White
        
        Write-Host "`nDefault test credentials:" -ForegroundColor Cyan
        Write-Host "  Platform Admin: admin@platform.com / Admin123!" -ForegroundColor Gray
        Write-Host "  Test User: user@test.com / Test123!" -ForegroundColor Gray
        
        Write-Host "`nYou can now:" -ForegroundColor Cyan
        Write-Host "  - Start application services: .\scripts\start-all.ps1" -ForegroundColor Gray
        Write-Host "  - Create additional users: .\scripts\create-user.ps1" -ForegroundColor Gray
        Write-Host "  - Check service health: .\scripts\health-check.ps1" -ForegroundColor Gray
    } else {
        Write-Host "✗ Databases failed to initialize within timeout period" -ForegroundColor Red
        Write-Host "Check docker logs for more information:" -ForegroundColor Yellow
        Write-Host "  docker logs platform-postgres" -ForegroundColor Gray
        Write-Host "  docker logs auth-postgres" -ForegroundColor Gray
        exit 1
    }
} finally {
    Pop-Location
}