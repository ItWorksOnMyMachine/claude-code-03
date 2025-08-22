using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Threading;
using System.Threading.Tasks;

namespace PlatformBff.Tests.Authentication;

/// <summary>
/// A static configuration manager that returns a pre-configured OpenIdConnectConfiguration
/// without making any network calls. Used for testing OIDC authentication without requiring
/// an actual identity provider.
/// </summary>
public class StaticConfigurationManager<T> : IConfigurationManager<T> where T : class
{
    private readonly T _configuration;

    public StaticConfigurationManager(T configuration)
    {
        _configuration = configuration;
    }

    public Task<T> GetConfigurationAsync(CancellationToken cancel)
    {
        return Task.FromResult(_configuration);
    }

    public void RequestRefresh()
    {
        // No-op - static configuration doesn't need refreshing
    }
}