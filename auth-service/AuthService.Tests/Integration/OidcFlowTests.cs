using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using AuthService.Tests.TestInfrastructure;

namespace AuthService.Tests.Integration;

public class OidcFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public OidcFlowTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _output = output;
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            
            // Configure logging to capture server errors
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Warning);
                logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Information);
                logging.AddProvider(new XunitHostLoggerProvider());
            });
            
            builder.ConfigureServices(services =>
            {
                // Configure antiforgery for testing environment to work with HTTP
                services.Configure<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>(options =>
                {
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
                    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                });
                
                // Configure IdentityServer to use Identity's cookie for authentication
                services.Configure<Duende.IdentityServer.Configuration.IdentityServerOptions>(options =>
                {
                    options.Authentication.CookieAuthenticationScheme = "Identity.Application";
                    options.Authentication.RequireAuthenticatedUserForSignOutMessage = false;
                });
            });
        });
        
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Fact]
    public async Task Discovery_Endpoint_Should_Return_Valid_Configuration()
    {
        // Act
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        dynamic? discovery = JsonConvert.DeserializeObject(content);
        
        Assert.NotNull(discovery);
        Assert.NotNull(discovery!.issuer);
        Assert.NotNull(discovery.authorization_endpoint);
        Assert.NotNull(discovery.token_endpoint);
        Assert.NotNull(discovery.userinfo_endpoint);
        Assert.NotNull(discovery.jwks_uri);
        Assert.Contains("code", (IEnumerable<dynamic>)discovery.response_types_supported);
        Assert.Contains("openid", (IEnumerable<dynamic>)discovery.scopes_supported);
    }

    [Fact]
    public async Task JWKS_Endpoint_Should_Return_Keys()
    {
        // Act
        var response = await _client.GetAsync("/.well-known/openid-configuration/jwks");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        dynamic? jwks = JsonConvert.DeserializeObject(content);
        
        Assert.NotNull(jwks);
        Assert.NotNull(jwks!.keys);
        Assert.NotEmpty((IEnumerable<dynamic>)jwks.keys);
    }

    [Fact]
    public async Task Authorization_Endpoint_Should_Redirect_To_Login_For_Unauthenticated_User()
    {
        // Bind output helper to sink (early so startup logs get flushed)
        XunitHostLogSink.SetTestOutput(_output);
        XunitHostLogSink.FlushTo(_output);

        // Arrange
        var authorizeUrl = "/connect/authorize?" +
            "client_id=platform-bff&" +
            "response_type=code&" +
            "scope=openid profile email&" +
            "redirect_uri=http://localhost:5000/signin-oidc&" +
            "state=test-state&" +
            "code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&" +
            "code_challenge_method=S256";
        
        _output.WriteLine($"Testing authorization URL: {authorizeUrl}");
        
        // Act
        var response = await _client.GetAsync(authorizeUrl);
        XunitHostLogSink.FlushTo(_output);
        
        // Debug: Output comprehensive response information
        _output.WriteLine($"Response Status Code: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"Response Headers:");
        foreach (var header in response.Headers)
        {
            _output.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }
        
        if (response.Headers.Location != null)
        {
            _output.WriteLine($"Location Header: {response.Headers.Location}");
            _output.WriteLine($"Location LocalPath: {response.Headers.Location.LocalPath}");
            _output.WriteLine($"Location Query: {response.Headers.Location.Query}");
        }
        else
        {
            _output.WriteLine("Location Header: null");
        }

        // If we get a server error, read the response body for details
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Server Error Response Body: {errorContent}");
            
            // Also flush any remaining logs
            XunitHostLogSink.FlushTo(_output);
        }
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect, 
            $"Expected redirect but got {response.StatusCode}. Check logs above for server error details.");
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.LocalPath.Should().StartWith("/Account/Login");
        response.Headers.Location.Query.Should().Contain("ReturnUrl");
    }

    [Fact]
    public async Task Login_Page_Should_Be_Accessible()
    {
        // Act
        var response = await _client.GetAsync("/Account/Login");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Sign In");
    }

    [Fact]
    public async Task Token_Endpoint_Should_Reject_Invalid_Client()
    {
        // Arrange
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", "invalid-code"),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:5000/signin-oidc"),
            new KeyValuePair<string, string>("client_id", "invalid-client"),
            new KeyValuePair<string, string>("code_verifier", "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk")
        });
        
        // Act
        var response = await _client.PostAsync("/connect/token", tokenRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        dynamic? error = JsonConvert.DeserializeObject(content);
        
        Assert.NotNull(error);
        Assert.Equal("invalid_client", (string)error!.error);
    }

    [Fact]
    public async Task UserInfo_Endpoint_Should_Require_Authentication()
    {
        // Act
        var response = await _client.GetAsync("/connect/userinfo");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Endpoint_Should_Be_Accessible()
    {
        // Act
        var response = await _client.GetAsync("/connect/endsession");
        
        // Assert
        // Should redirect to logout page or process logout
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Health_Endpoints_Should_Be_Accessible()
    {
        // Act
        var healthResponse = await _client.GetAsync("/health");
        var readyResponse = await _client.GetAsync("/health/ready");
        
        // Assert
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var healthContent = await healthResponse.Content.ReadAsStringAsync();
        var readyContent = await readyResponse.Content.ReadAsStringAsync();
        
        healthContent.Should().Contain("Healthy");
        readyContent.Should().Contain("status");
    }

    [Fact]
    public async Task CORS_Headers_Should_Be_Present_For_Allowed_Origins()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Origin", "https://host-fe.platform.local:3002");
        
        // Act
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        
        // Assert
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("https://host-fe.platform.local:3002");
        response.Headers.Should().ContainKey("Access-Control-Allow-Credentials");
        response.Headers.GetValues("Access-Control-Allow-Credentials").Should().Contain("true");
    }
}