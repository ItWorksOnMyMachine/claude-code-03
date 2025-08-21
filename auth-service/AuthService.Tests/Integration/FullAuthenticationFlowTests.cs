using AuthService.Data;
using AuthService.Data.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Xunit;
using Xunit.Abstractions;
using AuthService.Tests.TestInfrastructure;

namespace AuthService.Tests.Integration;

public class FullAuthenticationFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    
    public FullAuthenticationFlowTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _output = output;
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Keep Testing environment for proper test configuration
            builder.UseEnvironment("Testing");
            
            // Configure logging
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Warning);
                logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Information);
                logging.AddProvider(new XunitHostLoggerProvider());
            });
            
            // Configure services to support OIDC flow in tests
            builder.ConfigureServices(services =>
            {
                // Configure IdentityServer to use Identity's cookie for authentication
                services.Configure<Duende.IdentityServer.Configuration.IdentityServerOptions>(options =>
                {
                    // Use Identity.Application cookie for IdentityServer authentication
                    options.Authentication.CookieAuthenticationScheme = "Identity.Application";
                    options.Authentication.RequireAuthenticatedUserForSignOutMessage = false;
                });
            });
        });

        // Create client that preserves cookies between requests
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true  // This ensures cookies are preserved
        });
    }

    [Fact]
    public async Task Complete_OIDC_Authorization_Code_Flow_Should_Work()
    {
    // Bind output helper to sink (early so startup logs get flushed)
    XunitHostLogSink.SetTestOutput(_output);
    XunitHostLogSink.FlushTo(_output);

    // Arrange - Create test user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "oidc.test@identity.local",
            Email = "oidc.test@identity.local",
            EmailConfirmed = true,
            FirstName = "OIDC",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPassword123!");
        
        // Step 1: Initiate authorization request
        // Note: In WebApplicationFactory tests, use http://localhost without port
        var authorizeUrl = "/connect/authorize?" +
            "client_id=test-client&" +
            "redirect_uri=http://localhost/test-callback&" +
            "response_type=code&" +
            "scope=openid profile&" +
            "state=xyz123&" +
            "code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&" +
            "code_challenge_method=S256";
        
    var authorizeResponse = await _client.GetAsync(authorizeUrl);
    XunitHostLogSink.FlushTo(_output);
        
        // Should redirect to login page
        authorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var loginUrl = authorizeResponse.Headers.Location?.ToString();
        loginUrl.Should().Contain("/Account/Login");
        
        // Step 2: Perform login
        var loginPageResponse = await _client.GetAsync(loginUrl);
        loginPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Extract anti-forgery token from login page
        var loginContent = await loginPageResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(loginContent);
        
        // Debug: Check if we found a token
        if (string.IsNullOrEmpty(token))
        {
            throw new Exception($"Could not find anti-forgery token in login page. Login page content (first 500 chars):\n{loginContent.Substring(0, Math.Min(500, loginContent.Length))}");
        }
        
        // Extract ReturnUrl from the login URL query string
        if (string.IsNullOrEmpty(loginUrl))
        {
            throw new Exception("loginUrl was null/empty after authorize redirect");
        }
        var loginUri = new Uri(loginUrl, UriKind.RelativeOrAbsolute);
        if (!loginUri.IsAbsoluteUri)
        {
            // For WebApplicationFactory tests, use the same base as the client
            loginUri = new Uri(_client.BaseAddress ?? new Uri("http://localhost"), loginUrl);
        }
        var returnUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(loginUri.Query)["ReturnUrl"].FirstOrDefault() ?? "";
        
        // Submit login form
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "oidc.test@identity.local"),
            new KeyValuePair<string, string>("Password", "TestPassword123!"),
            new KeyValuePair<string, string>("RememberLogin", "false"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("ReturnUrl", returnUrl)
        });
        
    var loginResponse = await _client.PostAsync("/Account/Login", loginData);
    XunitHostLogSink.FlushTo(_output);

        // Should redirect back to authorize endpoint after successful login
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var authorizeRedirect = loginResponse.Headers.Location?.ToString();
        authorizeRedirect.Should().Contain("/connect/authorize/callback");
        
        
        // Step 3: Follow authorization callback
    var callbackResponse = await _client.GetAsync(authorizeRedirect);
    XunitHostLogSink.FlushTo(_output);

        // Should redirect to client redirect_uri with authorization code
        callbackResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var clientRedirect = callbackResponse.Headers.Location?.ToString();
        clientRedirect.Should().StartWith("http://localhost/test-callback");
        clientRedirect.Should().Contain("code=");
        clientRedirect.Should().Contain("state=xyz123");
        
        // Extract authorization code
        if (string.IsNullOrEmpty(clientRedirect))
        {
            throw new Exception("clientRedirect was null/empty after callback");
        }
        var uri = new Uri(clientRedirect);
        var queryParams = QueryHelpers.ParseQuery(uri.Query);
        var authCode = queryParams["code"].FirstOrDefault();
        authCode.Should().NotBeNullOrEmpty();
        
        // Step 4: Exchange authorization code for tokens
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authCode ?? string.Empty),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost/test-callback"), // Must match exactly!
            new KeyValuePair<string, string>("client_id", "test-client"),
            new KeyValuePair<string, string>("client_secret", "test-secret"),
            new KeyValuePair<string, string>("code_verifier", "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk")
        });
        
    var tokenResponse = await _client.PostAsync("/connect/token", tokenRequest);
    XunitHostLogSink.FlushTo(_output);
        
        // Debug: Output token response if it failed
        if (tokenResponse.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"Token exchange failed with status {tokenResponse.StatusCode}: {errorContent}");
        }
        
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
        
        tokenData.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
        tokenData.GetProperty("id_token").GetString().Should().NotBeNullOrEmpty();
        tokenData.GetProperty("token_type").GetString().Should().Be("Bearer");
        tokenData.GetProperty("expires_in").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Password_Grant_Flow_Should_Work_For_Trusted_Clients()
    {
        // Arrange - Create test user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "password.test@identity.local",
            Email = "password.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Password",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "SecurePass123!");
        
        // Act - Request token using password grant
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "password.test@identity.local"),
            new KeyValuePair<string, string>("password", "SecurePass123!"),
            new KeyValuePair<string, string>("scope", "openid profile email api"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var response = await _client.PostAsync("/connect/token", tokenRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
        
        tokenData.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
        tokenData.GetProperty("token_type").GetString().Should().Be("Bearer");
        tokenData.GetProperty("expires_in").GetInt32().Should().Be(300); // 5 minutes
    }

    [Fact]
    public async Task Client_Credentials_Flow_Should_Work_For_Machine_To_Machine()
    {
        // Act - Request token using client credentials
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "api"),
            new KeyValuePair<string, string>("client_id", "machine-client"),
            new KeyValuePair<string, string>("client_secret", "machine-secret")
        });
        
        var response = await _client.PostAsync("/connect/token", tokenRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
        
        tokenData.GetProperty("access_token").GetString().Should().NotBeNullOrEmpty();
        tokenData.GetProperty("token_type").GetString().Should().Be("Bearer");
        tokenData.TryGetProperty("id_token", out _).Should().BeFalse(); // No ID token for client credentials
    }

    [Fact]
    public async Task Invalid_Credentials_Should_Return_Error()
    {
        // Act - Request token with invalid credentials
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "invalid@identity.local"),
            new KeyValuePair<string, string>("password", "WrongPassword"),
            new KeyValuePair<string, string>("scope", "openid"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var response = await _client.PostAsync("/connect/token", tokenRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        var errorData = JsonSerializer.Deserialize<JsonElement>(content);
        
        errorData.GetProperty("error").GetString().Should().Be("invalid_grant");
    }

    [Fact]
    public async Task Locked_Account_Should_Prevent_Authentication()
    {
        // Arrange - Create and lock test user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "locked.test@identity.local",
            Email = "locked.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Locked",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "ValidPass123!");
        await userManager.SetLockoutEndDateAsync(testUser, DateTimeOffset.UtcNow.AddHours(1));
        
        // Act - Try to authenticate with locked account
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "locked.test@identity.local"),
            new KeyValuePair<string, string>("password", "ValidPass123!"),
            new KeyValuePair<string, string>("scope", "openid"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var response = await _client.PostAsync("/connect/token", tokenRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        // Security best practice: don't reveal why authentication failed
        content.Should().Contain("invalid_grant");
    }

    [Fact]
    public async Task Inactive_User_Should_Not_Authenticate()
    {
        // Arrange - Create inactive user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "inactive.test@identity.local",
            Email = "inactive.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Inactive",
            LastName = "Test",
            IsActive = false // User is inactive
        };
        
        await userManager.CreateAsync(testUser, "ValidPass123!");
        
        // Act - Try to authenticate with inactive account
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "inactive.test@identity.local"),
            new KeyValuePair<string, string>("password", "ValidPass123!"),
            new KeyValuePair<string, string>("scope", "openid"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var response = await _client.PostAsync("/connect/token", tokenRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Discovery_Document_Should_Be_Accessible()
    {
        // Act
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var discovery = JsonSerializer.Deserialize<JsonElement>(content);
        
        discovery.GetProperty("issuer").GetString().Should().Be("http://localhost");
        discovery.GetProperty("authorization_endpoint").GetString().Should().Contain("/connect/authorize");
        discovery.GetProperty("token_endpoint").GetString().Should().Contain("/connect/token");
        discovery.GetProperty("userinfo_endpoint").GetString().Should().Contain("/connect/userinfo");
        discovery.GetProperty("jwks_uri").GetString().Should().Contain("/.well-known/openid-configuration/jwks");
        
        // Verify supported flows
        var responseTypes = discovery.GetProperty("response_types_supported").EnumerateArray()
            .Select(x => x.GetString()).ToList();
        responseTypes.Should().Contain("code");
        responseTypes.Should().Contain("token");
        responseTypes.Should().Contain("id_token");
        
        var grantTypes = discovery.GetProperty("grant_types_supported").EnumerateArray()
            .Select(x => x.GetString()).ToList();
        grantTypes.Should().Contain("authorization_code");
        grantTypes.Should().Contain("client_credentials");
        grantTypes.Should().Contain("refresh_token");
    }

    [Fact]
    public async Task JWKS_Endpoint_Should_Return_Public_Keys()
    {
        // Act
        var response = await _client.GetAsync("/.well-known/openid-configuration/jwks");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var jwks = JsonSerializer.Deserialize<JsonElement>(content);
        
        jwks.GetProperty("keys").GetArrayLength().Should().BeGreaterThan(0);
        
        var firstKey = jwks.GetProperty("keys")[0];
        firstKey.GetProperty("kty").GetString().Should().NotBeNullOrEmpty();
        firstKey.GetProperty("use").GetString().Should().Be("sig");
        firstKey.GetProperty("kid").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UserInfo_Endpoint_Should_Return_User_Claims()
    {
        // Arrange - Create test user and get access token
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "userinfo.test@identity.local",
            Email = "userinfo.test@identity.local",
            EmailConfirmed = true,
            FirstName = "UserInfo",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Get access token
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "userinfo.test@identity.local"),
            new KeyValuePair<string, string>("password", "TestPass123!"),
            new KeyValuePair<string, string>("scope", "openid profile email"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var tokenResponse = await _client.PostAsync("/connect/token", tokenRequest);
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
        var accessToken = tokenData.GetProperty("access_token").GetString();
        
        // Act - Call userinfo endpoint
        var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        userInfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        var userInfoResponse = await _client.SendAsync(userInfoRequest);
        
        // Assert
        userInfoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<JsonElement>(userInfoContent);
        
        // Debug: Output the actual userInfo response
        _output.WriteLine($"UserInfo response: {userInfoContent}");
        
        userInfo.GetProperty("sub").GetString().Should().Be(testUser.Id);
        userInfo.GetProperty("email").GetString().Should().Be("userinfo.test@identity.local");
        userInfo.GetProperty("name").GetString().Should().Be("UserInfo Test");
    }

    private string ExtractAntiForgeryToken(string html)
    {
        // Look for the hidden input field with the anti-forgery token
        var tokenStart = html.IndexOf("name=\"__RequestVerificationToken\"");
        if (tokenStart == -1)
        {
            // No anti-forgery token found, might be disabled in test environment
            return string.Empty;
        }
        
        // Find the value attribute
        var valueStart = html.IndexOf("value=\"", tokenStart);
        if (valueStart == -1)
        {
            return string.Empty;
        }
        
        valueStart += 7; // Length of 'value="'
        var valueEnd = html.IndexOf("\"", valueStart);
        
        if (valueEnd == -1)
        {
            return string.Empty;
        }
        
        return html.Substring(valueStart, valueEnd - valueStart);
    }

    // Simple ILoggerProvider to pipe server logs into the current xUnit test output
}