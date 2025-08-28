# Stop All Services - Platform Stack
# Stops all running services including local application services and Docker infrastructure
#
# Usage:
#   .\stop-all.ps1           # Stop all services
#   .\stop-all.ps1 -force    # Force stop all services
#
# Example:
#   .\scripts\stop-all.ps1

param(
    [switch]$force = $false     # Force stop services
)

Write-Host "Stopping all platform services..." -ForegroundColor Yellow

# Stop application services if running
Write-Host "`nStopping application services..." -ForegroundColor Cyan

# Find and stop dotnet processes for our services
$dotnetProcesses = Get-Process | Where-Object { 
    $_.ProcessName -eq "dotnet" -and 
    ($_.CommandLine -like "*platform-host-bff*" -or 
     $_.CommandLine -like "*AuthService*")
}

if ($dotnetProcesses) {
    foreach ($process in $dotnetProcesses) {
        Write-Host "  Stopping $($process.ProcessName) (PID: $($process.Id))..." -ForegroundColor Gray
        Stop-Process -Id $process.Id -Force
    }
} else {
    # Try alternative method - look for processes by window title
    $bffProcess = Get-Process | Where-Object { $_.MainWindowTitle -like "*platform-host-bff*" }
    $authProcess = Get-Process | Where-Object { $_.MainWindowTitle -like "*AuthService*" }
    
    if ($bffProcess) {
        Write-Host "  Stopping Platform BFF..." -ForegroundColor Gray
        Stop-Process -Id $bffProcess.Id -Force
    }
    
    if ($authProcess) {
        Write-Host "  Stopping Auth Service..." -ForegroundColor Gray
        Stop-Process -Id $authProcess.Id -Force
    }
}

# Find and stop Node.js processes for frontend
$nodeProcesses = Get-Process | Where-Object { 
    $_.ProcessName -like "node*" -and 
    $_.Path -like "*platform-host-frontend*"
}

if ($nodeProcesses) {
    foreach ($process in $nodeProcesses) {
        Write-Host "  Stopping Frontend (PID: $($process.Id))..." -ForegroundColor Gray
        Stop-Process -Id $process.Id -Force
    }
}

Write-Host "✓ Application services stopped" -ForegroundColor Green

# Stop Docker infrastructure services
Write-Host "`nStopping Docker infrastructure services..." -ForegroundColor Cyan

# Change to project root directory
Push-Location (Join-Path $PSScriptRoot "..")

try {
    # Check if Docker is running
    docker version | Out-Null
    
    # Stop docker-compose services
    if ($force) {
        docker-compose down -v
        Write-Host "✓ Docker services stopped and volumes removed" -ForegroundColor Green
    } else {
        docker-compose down
        Write-Host "✓ Docker services stopped" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠ Docker is not running or services already stopped" -ForegroundColor Yellow
} finally {
    Pop-Location
}

Write-Host "`n✓ All services stopped successfully!" -ForegroundColor Green