using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthService.Certificates;

public static class CertificateConfiguration
{
    public static void ConfigureSigningCredentials(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var identityServerBuilder = services.AddIdentityServer();
        
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            // Use temporary development certificate
            identityServerBuilder.AddDeveloperSigningCredential();
        }
        else
        {
            // Use production certificate
            var certificateConfig = configuration.GetSection("IdentityServer:SigningCertificate");
            var certificateType = certificateConfig["Type"];
            
            switch (certificateType?.ToLower())
            {
                case "file":
                    LoadCertificateFromFile(identityServerBuilder, certificateConfig);
                    break;
                    
                case "store":
                    LoadCertificateFromStore(identityServerBuilder, certificateConfig);
                    break;
                    
                case "keyvault":
                    LoadCertificateFromKeyVault(identityServerBuilder, certificateConfig);
                    break;
                    
                default:
                    throw new InvalidOperationException($"Unknown certificate type: {certificateType}. Supported types: File, Store, KeyVault");
            }
        }
    }
    
    private static void LoadCertificateFromFile(IIdentityServerBuilder builder, IConfigurationSection config)
    {
        var filePath = config["FilePath"];
        var password = config["Password"];
        
        if (string.IsNullOrEmpty(filePath))
            throw new InvalidOperationException("Certificate file path is required");
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Certificate file not found: {filePath}");
        
        var certificate = string.IsNullOrEmpty(password)
            ? new X509Certificate2(filePath)
            : new X509Certificate2(filePath, password, X509KeyStorageFlags.MachineKeySet);
        
        builder.AddSigningCredential(certificate);
    }
    
    private static void LoadCertificateFromStore(IIdentityServerBuilder builder, IConfigurationSection config)
    {
        var storeName = config["StoreName"] ?? "My";
        var storeLocation = config["StoreLocation"] ?? "LocalMachine";
        var thumbprint = config["Thumbprint"];
        var subject = config["Subject"];
        
        if (string.IsNullOrEmpty(thumbprint) && string.IsNullOrEmpty(subject))
            throw new InvalidOperationException("Certificate thumbprint or subject is required");
        
        var store = new X509Store(
            Enum.Parse<StoreName>(storeName), 
            Enum.Parse<StoreLocation>(storeLocation));
        
        try
        {
            store.Open(OpenFlags.ReadOnly);
            
            X509Certificate2Collection certificates;
            
            if (!string.IsNullOrEmpty(thumbprint))
            {
                certificates = store.Certificates.Find(
                    X509FindType.FindByThumbprint, 
                    thumbprint, 
                    validOnly: true);
            }
            else
            {
                certificates = store.Certificates.Find(
                    X509FindType.FindBySubjectName, 
                    subject, 
                    validOnly: true);
            }
            
            if (certificates.Count == 0)
                throw new InvalidOperationException("Certificate not found in store");
            
            var certificate = certificates[0];
            builder.AddSigningCredential(certificate);
        }
        finally
        {
            store.Close();
        }
    }
    
    private static void LoadCertificateFromKeyVault(IIdentityServerBuilder builder, IConfigurationSection config)
    {
        // Azure Key Vault integration
        var vaultUrl = config["VaultUrl"];
        var certificateName = config["CertificateName"];
        var clientId = config["ClientId"];
        var clientSecret = config["ClientSecret"];
        var tenantId = config["TenantId"];
        
        if (string.IsNullOrEmpty(vaultUrl) || string.IsNullOrEmpty(certificateName))
            throw new InvalidOperationException("KeyVault URL and certificate name are required");
        
        // Note: This would require Azure.Security.KeyVault.Certificates package
        // Implementation would use Azure SDK to retrieve certificate from Key Vault
        
        throw new NotImplementedException("Azure Key Vault integration requires additional packages. Install Azure.Security.KeyVault.Certificates and implement retrieval logic.");
    }
    
    public static X509Certificate2 GenerateSelfSignedCertificate(string subjectName, int validDays = 365)
    {
        var distinguishedName = new X500DistinguishedName($"CN={subjectName}");
        
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new CertificateRequest(
            distinguishedName, 
            rsa, 
            HashAlgorithmName.SHA256, 
            RSASignaturePadding.Pkcs1);
        
        // Add extensions
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, 
                critical: true));
        
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection 
                { 
                    new Oid("1.3.6.1.5.5.7.3.1"), // Server Authentication
                    new Oid("1.3.6.1.5.5.7.3.2")  // Client Authentication
                }, 
                critical: true));
        
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        
        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), 
            DateTimeOffset.UtcNow.AddDays(validDays));
        
        return new X509Certificate2(
            certificate.Export(X509ContentType.Pfx, ""), 
            "", 
            X509KeyStorageFlags.MachineKeySet);
    }
    
    public static void ValidateCertificate(X509Certificate2 certificate, ILogger logger)
    {
        // Check expiration
        if (certificate.NotAfter < DateTime.UtcNow)
        {
            logger.LogError("Certificate has expired on {ExpirationDate}", certificate.NotAfter);
            throw new InvalidOperationException("Certificate has expired");
        }
        
        if (certificate.NotAfter < DateTime.UtcNow.AddDays(30))
        {
            logger.LogWarning("Certificate will expire soon on {ExpirationDate}", certificate.NotAfter);
        }
        
        // Check if it has a private key
        if (!certificate.HasPrivateKey)
        {
            logger.LogError("Certificate does not have a private key");
            throw new InvalidOperationException("Certificate must have a private key for signing");
        }
        
        // Check key size
        if (certificate.PublicKey.Key.KeySize < 2048)
        {
            logger.LogWarning("Certificate key size is less than 2048 bits");
        }
        
        logger.LogInformation(
            "Using certificate: Subject={Subject}, Thumbprint={Thumbprint}, Expires={Expiration}",
            certificate.Subject,
            certificate.Thumbprint,
            certificate.NotAfter);
    }
}

public class CertificateOptions
{
    public string Type { get; set; } = "File";
    public string? FilePath { get; set; }
    public string? Password { get; set; }
    public string? StoreName { get; set; }
    public string? StoreLocation { get; set; }
    public string? Thumbprint { get; set; }
    public string? Subject { get; set; }
    public string? VaultUrl { get; set; }
    public string? CertificateName { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }
}