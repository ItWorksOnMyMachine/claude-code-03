# Health Check - Verify all services are running and healthy
# Checks the health status of all platform services and dependencies
#
# Parameters:
#   -v     Verbose output with response times
#   -json  Output results as JSON
#
# Usage:
#   .\health-check.ps1        # Check all services
#   .\health-check.ps1 -v     # Verbose output with response times
#   .\health-check.ps1 -json  # Output results as JSON
#
# Example:
#   .\scripts\health-check.ps1

param(
    [switch]$v = $false,      # Verbose mode
    [switch]$json = $false    # JSON output
)

$services = @()
$allHealthy = $true

# Function to check service health
function Test-ServiceHealth {
    param(
        [string]$Name,
        [string]$Type,
        [string]$Url,
        [int]$Port,
        [string]$Container = ""
    )
    
    $result = @{
        Name = $Name
        Type = $Type
        Port = $Port
        Status = "Unknown"
        ResponseTime = $null
        Error = $null
    }
    
    try {
        $startTime = Get-Date
        
        switch ($Type) {
            "HTTP" {
                $response = Invoke-WebRequest -Uri $Url -Method GET -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
                if ($response.StatusCode -eq 200) {
                    $result.Status = "Healthy"
                } else {
                    $result.Status = "Unhealthy"
                    $result.Error = "Status code: $($response.StatusCode)"
                }
                $result.ResponseTime = ((Get-Date) - $startTime).TotalMilliseconds
            }
            "Docker" {
                if ($Container) {
                    $containerStatus = docker ps --filter "name=$Container" --format "{{.Status}}" 2>$null
                    if ($containerStatus -match "Up") {
                        $result.Status = "Running"
                    } else {
                        $result.Status = "Stopped"
                        $result.Error = "Container not running"
                    }
                }
            }
            "TCP" {
                $tcpClient = New-Object System.Net.Sockets.TcpClient
                $tcpClient.ReceiveTimeout = 3000
                $tcpClient.SendTimeout = 3000
                try {
                    $tcpClient.Connect("localhost", $Port)
                    if ($tcpClient.Connected) {
                        $result.Status = "Available"
                        $tcpClient.Close()
                    }
                } catch {
                    $result.Status = "Unavailable"
                    $result.Error = "Cannot connect to port $Port"
                }
                $result.ResponseTime = ((Get-Date) - $startTime).TotalMilliseconds
            }
        }
    } catch {
        $result.Status = "Error"
        $result.Error = $_.Exception.Message
        $result.ResponseTime = ((Get-Date) - $startTime).TotalMilliseconds
    }
    
    return $result
}

if (-not $json) {
    Write-Host "`n🔍 Platform Health Check" -ForegroundColor Cyan
    Write-Host "========================" -ForegroundColor Cyan
    Write-Host "Checking all services..." -ForegroundColor Yellow
}

# Check Docker
if (-not $json) {
    Write-Host "`nChecking Docker..." -ForegroundColor Gray
}
try {
    docker version | Out-Null
    $dockerStatus = "Running"
} catch {
    $dockerStatus = "Not Running"
    $allHealthy = $false
}

# Check Database Services (platform_db and auth_db)
$platformDb = Test-ServiceHealth -Name "PostgreSQL Platform (platform_db)" -Type "TCP" -Port 5432 -Container "postgres-platform"
$authDb = Test-ServiceHealth -Name "PostgreSQL Auth (auth_db)" -Type "TCP" -Port 5433 -Container "postgres-auth"
$services += $platformDb
$services += $authDb

# Check Redis
$redis = Test-ServiceHealth -Name "Redis Cache" -Type "TCP" -Port 6379 -Container "redis"
$services += $redis

# Check Application Services
$bff = Test-ServiceHealth -Name "Platform BFF" -Type "HTTP" -Url "https://host-bff.platform.local:5086/health" -Port 5000
$auth = Test-ServiceHealth -Name "Auth Service" -Type "HTTP" -Url "https://login.platform.local:5214/health" -Port 5001
$services += $bff
$services += $auth

# Check Frontend (if running)
$frontend = Test-ServiceHealth -Name "Frontend" -Type "HTTP" -Url "https://host-fe.platform.local:3002" -Port 3002
$services += $frontend

# Determine overall health
foreach ($service in $services) {
    if ($service.Status -in @("Error", "Unhealthy", "Stopped", "Unavailable")) {
        $allHealthy = $false
    }
}

if ($json) {
    # JSON Output
    $output = @{
        timestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
        docker = $dockerStatus
        services = $services
        overall_health = if ($allHealthy) { "Healthy" } else { "Unhealthy" }
    }
    $output | ConvertTo-Json -Depth 3
} else {
    # Console Output
    Write-Host "`n📊 Service Status:" -ForegroundColor Cyan
    Write-Host "─────────────────" -ForegroundColor Gray
    
    # Docker Status
    $dockerIcon = if ($dockerStatus -eq "Running") { "✅" } else { "❌" }
    $dockerColor = if ($dockerStatus -eq "Running") { "Green" } else { "Red" }
    Write-Host "$dockerIcon Docker: $dockerStatus" -ForegroundColor $dockerColor
    
    Write-Host "`n🔌 Dependencies:" -ForegroundColor Cyan
    foreach ($service in $services[0..2]) {
        $icon = switch ($service.Status) {
            "Healthy" { "✅" }
            "Available" { "✅" }
            "Running" { "✅" }
            default { "❌" }
        }
        
        $color = switch ($service.Status) {
            "Healthy" { "Green" }
            "Available" { "Green" }
            "Running" { "Green" }
            default { "Red" }
        }
        
        $statusText = "$icon $($service.Name) (port $($service.Port)): $($service.Status)"
        if ($v -and $service.ResponseTime) {
            $statusText += " [$('{0:N0}' -f $service.ResponseTime)ms]"
        }
        
        Write-Host $statusText -ForegroundColor $color
        
        if ($service.Error -and $v) {
            Write-Host "    Error: $($service.Error)" -ForegroundColor Yellow
        }
    }
    
    Write-Host "`n🚀 Applications:" -ForegroundColor Cyan
    foreach ($service in $services[3..5]) {
        $icon = switch ($service.Status) {
            "Healthy" { "✅" }
            "Available" { "✅" }
            "Running" { "✅" }
            default { "❌" }
        }
        
        $color = switch ($service.Status) {
            "Healthy" { "Green" }
            "Available" { "Green" }
            "Running" { "Green" }
            default { "Red" }
        }
        
        $statusText = "$icon $($service.Name) (port $($service.Port)): $($service.Status)"
        if ($v -and $service.ResponseTime) {
            $statusText += " [$('{0:N0}' -f $service.ResponseTime)ms]"
        }
        
        Write-Host $statusText -ForegroundColor $color
        
        if ($service.Error -and $v) {
            Write-Host "    Error: $($service.Error)" -ForegroundColor Yellow
        }
    }
    
    # Overall Status
    Write-Host "`n📈 Overall Status:" -ForegroundColor Cyan
    if ($allHealthy) {
        Write-Host "✅ All services are healthy!" -ForegroundColor Green
        Write-Host "`nPlatform is ready at:" -ForegroundColor Cyan
        Write-Host "  Frontend: https://host-fe.platform.local:3002" -ForegroundColor White
        Write-Host "  BFF API: https://host-bff.platform.local:5086" -ForegroundColor White
        Write-Host "  Auth: https://login.platform.local:5214" -ForegroundColor White
    } else {
        Write-Host "⚠️ Some services are not healthy" -ForegroundColor Yellow
        
        $unhealthyServices = $services | Where-Object { $_.Status -in @("Error", "Unhealthy", "Stopped", "Unavailable") }
        if ($unhealthyServices.Count -gt 0) {
            Write-Host "`n🔧 Action Required:" -ForegroundColor Yellow
            foreach ($service in $unhealthyServices) {
                Write-Host "  - Fix $($service.Name): $($service.Error)" -ForegroundColor Gray
            }
            
            Write-Host "`n💡 Troubleshooting:" -ForegroundColor Cyan
            Write-Host "  1. Start all services: .\scripts\start-all.ps1" -ForegroundColor Gray
            Write-Host "  2. Check logs: .\scripts\logs.ps1" -ForegroundColor Gray
            Write-Host "  3. Reset if needed: .\scripts\reset-db.ps1" -ForegroundColor Gray
        }
    }
}

# Exit with appropriate code
if ($allHealthy) {
    exit 0
} else {
    exit 1
}