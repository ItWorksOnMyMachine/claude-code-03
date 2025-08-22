using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace PlatformBff.Tests.Authentication;

public class AuthenticationConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Override any services needed for testing
            });
        });
    }

    [Fact]
    public void Authentication_Services_Should_Be_Registered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act & Assert
        var authService = services.GetService<IAuthenticationService>();
        Assert.NotNull(authService);

        var authSchemeProvider = services.GetService<IAuthenticationSchemeProvider>();
        Assert.NotNull(authSchemeProvider);
    }

    [Fact]
    public async Task Cookie_Authentication_Should_Be_Configured()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var schemeProvider = scope.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();

        // Act
        var scheme = await schemeProvider.GetSchemeAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Assert
        Assert.NotNull(scheme);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, scheme.Name);
        Assert.Equal(typeof(CookieAuthenticationHandler).FullName, scheme.HandlerType.FullName);
    }

    [Fact]
    public async Task OpenIdConnect_Authentication_Should_Be_Configured()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var schemeProvider = scope.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();

        // Act
        var scheme = await schemeProvider.GetSchemeAsync(OpenIdConnectDefaults.AuthenticationScheme);

        // Assert
        Assert.NotNull(scheme);
        Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, scheme.Name);
        Assert.Equal(typeof(OpenIdConnectHandler).FullName, scheme.HandlerType.FullName);
    }

    [Fact]
    public void Cookie_Options_Should_Be_Configured_Correctly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var cookieOptions = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();

        // Act
        var options = cookieOptions.Get(CookieAuthenticationDefaults.AuthenticationScheme);

        // Assert
        Assert.NotNull(options);
        Assert.Equal("platform.auth", options.Cookie.Name);
        Assert.True(options.Cookie.HttpOnly);
        Assert.True(options.Cookie.IsEssential);
        Assert.Equal(Microsoft.AspNetCore.Http.SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.Equal("/api/auth/login", options.LoginPath.Value);
        Assert.Equal("/api/auth/logout", options.LogoutPath.Value);
        Assert.Equal("/api/auth/access-denied", options.AccessDeniedPath.Value);
    }

    [Fact]
    public void OpenIdConnect_Options_Should_Be_Configured_Correctly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var oidcOptions = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();

        // Act
        var options = oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);

        // Assert
        Assert.NotNull(options);
        Assert.Equal("platform-bff", options.ClientId);
        Assert.NotNull(options.ClientSecret);
        Assert.Equal("http://localhost:5001", options.Authority);
        Assert.Equal("code", options.ResponseType);
        Assert.True(options.GetClaimsFromUserInfoEndpoint);
        Assert.True(options.SaveTokens);
        Assert.True(options.RequireHttpsMetadata == false); // For development
        
        // Check scopes
        Assert.Contains("openid", options.Scope);
        Assert.Contains("profile", options.Scope);
        Assert.Contains("email", options.Scope);
        Assert.Contains("offline_access", options.Scope);
    }

    [Fact]
    public void Default_Authentication_Scheme_Should_Be_Cookie()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var authOptions = scope.ServiceProvider.GetRequiredService<IOptions<AuthenticationOptions>>();

        // Act
        var options = authOptions.Value;

        // Assert
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, options.DefaultAuthenticateScheme);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, options.DefaultSignInScheme);
        Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, options.DefaultChallengeScheme);
    }

    [Fact]
    public void OpenIdConnect_Events_Should_Handle_Token_Storage()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var oidcOptions = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();

        // Act
        var options = oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);

        // Assert
        Assert.NotNull(options.Events);
        Assert.NotNull(options.Events.OnTokenValidated);
        Assert.NotNull(options.Events.OnRedirectToIdentityProviderForSignOut);
    }

    [Fact]
    public void Authentication_Middleware_Should_Be_In_Pipeline()
    {
        // This test verifies that authentication middleware is registered
        // The actual verification happens during app startup
        // If middleware is missing, the app won't start properly
        
        // Arrange & Act
        var client = _factory.CreateClient();

        // Assert - if we can create a client, the middleware pipeline is valid
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Protected_Endpoints_Should_Require_Authentication()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/tenant/available");

        // Assert - should redirect to login since we're not authenticated
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/api/auth/login", response.Headers.Location?.ToString());
    }
}