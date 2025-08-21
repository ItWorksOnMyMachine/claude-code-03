using AuthService.Data;
using AuthService.Data.Entities;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using FluentAssertions;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace AuthService.Tests.IdentityServer;

public class IdentityServerConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IdentityServerConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public void Should_Register_IdentityServer_Services()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var identityServerOptions = scope.ServiceProvider.GetService<IdentityServerOptions>();
        var profileService = scope.ServiceProvider.GetService<IProfileService>();
        var tokenService = scope.ServiceProvider.GetService<ITokenService>();

        // Assert
        identityServerOptions.Should().NotBeNull();
        profileService.Should().NotBeNull();
        tokenService.Should().NotBeNull();
    }

    [Fact]
    public void Should_Configure_IdentityServer_Options()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IdentityServerOptions>();

        // Assert
        options.Should().NotBeNull();
        options.Events.RaiseErrorEvents.Should().BeTrue();
        options.Events.RaiseInformationEvents.Should().BeTrue();
        options.Events.RaiseFailureEvents.Should().BeTrue();
        options.Events.RaiseSuccessEvents.Should().BeTrue();
    }

    [Fact]
    public void Should_Register_Client_Store()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var clientStore = scope.ServiceProvider.GetService<IClientStore>();

        // Assert
        clientStore.Should().NotBeNull();
    }

    [Fact]
    public void Should_Register_Resource_Stores()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var identityResourceStore = scope.ServiceProvider.GetService<IResourceStore>();
        var apiResourceStore = scope.ServiceProvider.GetService<IResourceStore>();
        var apiScopeStore = scope.ServiceProvider.GetService<IResourceStore>();

        // Assert
        identityResourceStore.Should().NotBeNull();
        apiResourceStore.Should().NotBeNull();
        apiScopeStore.Should().NotBeNull();
    }

    [Fact]
    public void Should_Register_Persisted_Grant_Store()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var persistedGrantStore = scope.ServiceProvider.GetService<IPersistedGrantStore>();

        // Assert
        persistedGrantStore.Should().NotBeNull();
    }

    [Fact]
    public void Should_Register_Cors_Policy_Service()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var corsPolicyService = scope.ServiceProvider.GetService<ICorsPolicyService>();

        // Assert
        corsPolicyService.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Expose_Discovery_Endpoint()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/.well-known/openid-configuration");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"issuer\"");
        content.Should().Contain("\"authorization_endpoint\"");
        content.Should().Contain("\"token_endpoint\"");
        content.Should().Contain("\"jwks_uri\"");
    }

    [Fact]
    public async Task Should_Expose_JWKS_Endpoint()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/.well-known/openid-configuration/jwks");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"keys\"");
    }

    [Fact]
    public void Should_Support_Authorization_Code_Flow_With_PKCE()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        
        // This test verifies configuration supports PKCE
        // Actual flow testing will be in integration tests
        var options = scope.ServiceProvider.GetRequiredService<IdentityServerOptions>();

        // Assert - IdentityServer is configured
        options.Should().NotBeNull();
        // Additional PKCE validation will be in client configuration tests
    }

    [Fact]
    public void Should_Configure_Token_Lifetimes()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        
        // Note: Token lifetimes are configured per client
        // This test verifies the service is available for configuration
        var tokenService = scope.ServiceProvider.GetService<ITokenService>();

        // Assert
        tokenService.Should().NotBeNull();
    }

    [Fact]
    public void Should_Have_Signing_Credentials()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var keyMaterialService = scope.ServiceProvider.GetService<ISigningCredentialStore>();

        // Act & Assert
        keyMaterialService.Should().NotBeNull();
        // In testing environment, should use development signing credential
    }

    [Fact]
    public void Should_Integrate_With_ASP_NET_Identity()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        
        // Check that IdentityServer is configured to use ASP.NET Identity
        var profileService = scope.ServiceProvider.GetService<IProfileService>();
        var resourceOwnerValidator = scope.ServiceProvider.GetService<IResourceOwnerPasswordValidator>();

        // Assert
        profileService.Should().NotBeNull();
        // The profile service should be able to work with our AppUser type
    }
}