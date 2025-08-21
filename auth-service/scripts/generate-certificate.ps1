# PowerShell script to generate production signing certificate for IdentityServer

param(
    [Parameter(Mandatory=$true)]
    [string]$SubjectName = "CN=auth.platform.com",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "../certs",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "",
    
    [Parameter(Mandatory=$false)]
    [int]$ValidityDays = 365,
    
    [Parameter(Mandatory=$false)]
    [string]$KeySize = "2048"
)

Write-Host "Generating signing certificate for IdentityServer..." -ForegroundColor Green

# Create output directory if it doesn't exist
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
    Write-Host "Created directory: $OutputPath"
}

$certPath = Join-Path $OutputPath "signing-certificate.pfx"
$cerPath = Join-Path $OutputPath "signing-certificate.cer"

# Generate certificate using PowerShell
$cert = New-SelfSignedCertificate `
    -Subject $SubjectName `
    -KeyAlgorithm RSA `
    -KeyLength $KeySize `
    -KeyUsage DigitalSignature, KeyEncipherment `
    -KeyExportPolicy Exportable `
    -NotBefore (Get-Date) `
    -NotAfter (Get-Date).AddDays($ValidityDays) `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -FriendlyName "IdentityServer Signing Certificate"

Write-Host "Certificate created with thumbprint: $($cert.Thumbprint)" -ForegroundColor Yellow

# Export certificate with private key (PFX)
if ([string]::IsNullOrEmpty($Password)) {
    Write-Host "Warning: Exporting certificate without password protection!" -ForegroundColor Red
    $Password = Read-Host -Prompt "Enter password for certificate (or press Enter for no password)" -AsSecureString
    
    if ($Password.Length -eq 0) {
        $Password = ConvertTo-SecureString -String "TempPassword123!" -Force -AsPlainText
        Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $Password | Out-Null
        
        # Re-export without password (not recommended for production)
        $tempCert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
        $tempCert.Import($certPath, $Password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)
        [System.IO.File]::WriteAllBytes($certPath, $tempCert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx))
    } else {
        Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $Password | Out-Null
    }
} else {
    $securePassword = ConvertTo-SecureString -String $Password -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $securePassword | Out-Null
}

# Export public key only (CER)
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

# Display certificate information
Write-Host "`nCertificate Details:" -ForegroundColor Cyan
Write-Host "  Subject: $($cert.Subject)"
Write-Host "  Thumbprint: $($cert.Thumbprint)"
Write-Host "  Serial Number: $($cert.SerialNumber)"
Write-Host "  Not Before: $($cert.NotBefore)"
Write-Host "  Not After: $($cert.NotAfter)"
Write-Host "  Key Size: $KeySize bits"
Write-Host "`nFiles Generated:" -ForegroundColor Cyan
Write-Host "  Private Key (PFX): $certPath"
Write-Host "  Public Key (CER): $cerPath"

# Optionally remove from store
$remove = Read-Host "`nRemove certificate from Windows certificate store? (y/n)"
if ($remove -eq 'y') {
    Remove-Item -Path "Cert:\CurrentUser\My\$($cert.Thumbprint)"
    Write-Host "Certificate removed from store." -ForegroundColor Green
} else {
    Write-Host "Certificate remains in store at: Cert:\CurrentUser\My\$($cert.Thumbprint)" -ForegroundColor Yellow
}

Write-Host "`nCertificate generation complete!" -ForegroundColor Green
Write-Host "Remember to:" -ForegroundColor Yellow
Write-Host "  1. Store the PFX file securely"
Write-Host "  2. Update appsettings.Production.json with the certificate path"
Write-Host "  3. Set the certificate password as an environment variable"
Write-Host "  4. Consider using Azure Key Vault or similar for production"