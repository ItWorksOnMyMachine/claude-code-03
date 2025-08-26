# Create Test User - Creates a new user via the development API
# Creates a test user in the auth service and assigns them to a tenant
#
# Parameters:
#   -email     : User's email address (required)
#   -password  : User's password (optional, defaults to Test123!)
#   -tenant    : Tenant ID or name to assign user to (optional)
#   -role      : Role to assign (Admin/User, defaults to User)
#
# Usage:
#   .\create-user.ps1 -email user@example.com
#   .\create-user.ps1 -email admin@company.com -password SecurePass123! -role Admin
#   .\create-user.ps1 -email user@test.com -tenant "test-tenant" -role User
#
# Example:
#   .\scripts\create-user.ps1 -email developer@test.com

param(
    [Parameter(Mandatory=$true)]
    [string]$email,
    
    [string]$password = "Test123!",
    
    [string]$tenant = "",
    
    [ValidateSet("Admin", "User")]
    [string]$role = "User"
)

# Validate email format
if ($email -notmatch "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$") {
    Write-Host "✗ Invalid email format: $email" -ForegroundColor Red
    Write-Host "Please provide a valid email address (e.g., user@example.com)" -ForegroundColor Yellow
    exit 1
}

# Validate password complexity
if ($password.Length -lt 8) {
    Write-Host "✗ Password must be at least 8 characters long" -ForegroundColor Red
    exit 1
}

if ($password -notmatch "[A-Z]" -or $password -notmatch "[a-z]" -or $password -notmatch "[0-9]" -or $password -notmatch "[^a-zA-Z0-9]") {
    Write-Host "✗ Password must contain uppercase, lowercase, number, and special character" -ForegroundColor Red
    Write-Host "Example: Test123!" -ForegroundColor Yellow
    exit 1
}

Write-Host "Creating user account..." -ForegroundColor Yellow
Write-Host "  Email: $email" -ForegroundColor Gray
Write-Host "  Role: $role" -ForegroundColor Gray
if ($tenant) {
    Write-Host "  Tenant: $tenant" -ForegroundColor Gray
}

# Check if BFF service is running
$bffUrl = "http://localhost:5000"
$healthUrl = "$bffUrl/health"

Write-Host "`nChecking BFF service availability..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri $healthUrl -Method GET -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ BFF service is running" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ BFF service is not running at $bffUrl" -ForegroundColor Red
    Write-Host "Please start the BFF service first:" -ForegroundColor Yellow
    Write-Host "  .\scripts\start-all.ps1" -ForegroundColor Gray
    Write-Host "  OR" -ForegroundColor Gray
    Write-Host "  cd platform-host\platform-host-bff && dotnet run" -ForegroundColor Gray
    exit 1
}

# Create user via development API
$createUserUrl = "$bffUrl/api/dev/users/create"
$body = @{
    email = $email
    password = $password
    role = $role
}

if ($tenant) {
    $body.tenantId = $tenant
}

$jsonBody = $body | ConvertTo-Json

Write-Host "`nSending request to create user..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri $createUserUrl -Method POST -Body $jsonBody -ContentType "application/json" -UseBasicParsing -ErrorAction Stop
    
    if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 201) {
        $responseData = $response.Content | ConvertFrom-Json
        
        Write-Host "`n✅ User created successfully!" -ForegroundColor Green
        Write-Host "`nUser Details:" -ForegroundColor Cyan
        Write-Host "  Email: $($responseData.email)" -ForegroundColor White
        Write-Host "  User ID: $($responseData.userId)" -ForegroundColor White
        Write-Host "  Role: $($responseData.role)" -ForegroundColor White
        
        if ($responseData.tenantName) {
            Write-Host "  Tenant: $($responseData.tenantName)" -ForegroundColor White
        }
        
        Write-Host "`nLogin credentials:" -ForegroundColor Cyan
        Write-Host "  Username: $email" -ForegroundColor Gray
        Write-Host "  Password: $password" -ForegroundColor Gray
        
        Write-Host "`nYou can now:" -ForegroundColor Cyan
        Write-Host "  - Login at: http://localhost:3002/login" -ForegroundColor Gray
        Write-Host "  - Use the API with these credentials" -ForegroundColor Gray
        Write-Host "  - Assign to additional tenants if needed" -ForegroundColor Gray
    }
} catch {
    $errorBody = $_.ErrorDetails.Message
    if ($errorBody) {
        $errorData = $errorBody | ConvertFrom-Json -ErrorAction SilentlyContinue
        if ($errorData.error) {
            Write-Host "✗ Failed to create user: $($errorData.error)" -ForegroundColor Red
        } else {
            Write-Host "✗ Failed to create user: $errorBody" -ForegroundColor Red
        }
    } else {
        Write-Host "✗ Failed to create user" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Red
    }
    
    # Check if it's a duplicate user error
    if ($_ -match "duplicate|exists|conflict") {
        Write-Host "`nUser with email '$email' may already exist" -ForegroundColor Yellow
        Write-Host "Try a different email address or reset the database:" -ForegroundColor Gray
        Write-Host "  .\scripts\reset-db.ps1" -ForegroundColor Gray
    }
    
    exit 1
}