#if DEBUG

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PlatformShared.Middleware;

public class EndpointConfiguration
{
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Scheme { get; set; }
    public string? StoreName { get; set; }
    public string? StoreLocation { get; set; }
    public string? FilePath { get; set; }
    public string? Password { get; set; }
}

public static class KestrelMiddleware
{
    public static void ConfigureEndpoints(this KestrelServerOptions options, IConfigurationRoot configuration)
    {
        var environment = options.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

        var endpoints = configuration.GetSection("HttpServer:Endpoints")
            .GetChildren()
            .ToDictionary(section => section.Key, section =>
            {
                var endpoint = new EndpointConfiguration();
                section.Bind(endpoint);
                return endpoint;
            });

        foreach (var endpoint in endpoints)
        {
            var config = endpoint.Value;
            var port = config.Port ?? (config.Scheme == "https" ? 443 : 80);

            var ipAddresses = new List<IPAddress>();
            if (config.Host?.StartsWith("local") == true)
            {
                ipAddresses.Add(IPAddress.IPv6Loopback);
                ipAddresses.Add(IPAddress.Loopback);
            }
            else if (IPAddress.TryParse(config.Host, out var address))
            {
                ipAddresses.Add(address);
            }
            else
            {
                ipAddresses.Add(IPAddress.IPv6Any);
            }

            foreach (var address in ipAddresses)
            {
                options.Listen(address, port,
                    listenOptions =>
                    {
                        if (config.Scheme == "https")
                        {
                            if (TryLoadCertificate(config, environment, out var certificate))
                            {
                                listenOptions.UseHttps(certificate);
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine($"Certificate not found for host {config.Host}, server not listening on: {config.Scheme}://{address}:{config.Port}.");
                                Console.ResetColor();
                            }
                        }
                    });
            }
        }
    }

    private static bool TryLoadCertificate(EndpointConfiguration config, IWebHostEnvironment environment, [NotNullWhen(true)] out X509Certificate2? certificate)
    {
        ArgumentNullException.ThrowIfNull(config.Host);

        certificate = null;
        if (config.StoreName != null && config.StoreLocation != null)
        {
            using (var store = new X509Store(config.StoreName, Enum.Parse<StoreLocation>(config.StoreLocation)))
            {
                store.Open(OpenFlags.ReadOnly);
                certificate = FindCertificate(config.Host, environment, certificate, store);

                if (certificate == null)
                {
                    var parts = config.Host.Split('.');
                    while (parts.Length > 2 && certificate == null)
                    {
                        parts = parts.Skip(1).ToArray();
                        var wildcard = "*." + string.Join('.', parts);
                        certificate = FindCertificate(wildcard, environment, certificate, store);
                    }
                }
            }
        }

        if (certificate == null && config.FilePath != null && config.Password != null)
        {
            certificate = new X509Certificate2(config.FilePath, config.Password);
        }

        return certificate != null;
    }

    private static X509Certificate2? FindCertificate(string hostName, IWebHostEnvironment environment, X509Certificate2? certificate, X509Store store)
    {
        var certificates = store.Certificates.Find(
                            X509FindType.FindBySubjectName,
                            hostName,
                            validOnly: !environment.IsDevelopment());

        if (certificates.Count > 0)
        {
            certificate = certificates[0];
        }

        if (certificate == null)
        {
            foreach (var cert in store.Certificates)
            {
                try
                {
                    // Skip if validOnly is true and certificate is not valid
                    if (!environment.IsDevelopment() && !cert.Verify())
                    {
                        continue;
                    }

                    // Look for Subject Alternative Name extension
                    var sanExtension = cert.Extensions
                        .Cast<X509Extension>()
                        .FirstOrDefault(ext => ext.Oid?.Value == "2.5.29.17"); // SAN OID

                    if (sanExtension != null)
                    {
                        var asnData = sanExtension.RawData;
                        var sanString = sanExtension.Format(true);

                        // Check if our target name is in the SAN string
                        if (sanString.Contains(hostName, StringComparison.OrdinalIgnoreCase))
                        {
                            certificate = cert;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception if needed, continue searching
                    Console.WriteLine($"Error processing certificate {cert.Subject}: {ex.Message}");
                }
            }
        }

        return certificate;
    }
}

#endif