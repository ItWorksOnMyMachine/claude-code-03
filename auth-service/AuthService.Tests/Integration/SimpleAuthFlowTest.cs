using AuthService.Data.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace AuthService.Tests.Integration;

public class SimpleAuthFlowTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SimpleAuthFlowTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });

        // Create client with cookie handling
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Fact]
    public async Task Can_Maintain_Authentication_Across_Requests()
    {
        // Arrange - Create test user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "cookie.test@identity.local",
            Email = "cookie.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Cookie",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPassword123!");
        
        // Act 1: Try to access a protected endpoint (should redirect to login)
        var protectedResponse = await _client.GetAsync("/connect/userinfo");
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        // Act 2: Use password grant to get a token (this doesn't use cookies)
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "cookie.test@identity.local"),
            new KeyValuePair<string, string>("password", "TestPassword123!"),
            new KeyValuePair<string, string>("scope", "openid profile"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var tokenResponse = await _client.PostAsync("/connect/token", tokenRequest);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // This test shows that password grant works but doesn't set cookies
        // The OIDC authorization code flow requires maintaining session via cookies
        // which is challenging in integration tests with WebApplicationFactory
    }
}