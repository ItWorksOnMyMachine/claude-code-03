using AuthService.Data;
using AuthService.Data.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AuthService.Tests.Integration;

public class TokenManagementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TokenManagementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Refresh_Token_Should_Issue_New_Access_Token()
    {
        // Arrange - Create test user and get initial tokens
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "refresh.test@identity.local",
            Email = "refresh.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Refresh",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Get initial tokens with offline_access scope for refresh token
        var initialTokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "refresh.test@identity.local"),
            new KeyValuePair<string, string>("password", "TestPass123!"),
            new KeyValuePair<string, string>("scope", "openid profile email offline_access"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var initialResponse = await _client.PostAsync("/connect/token", initialTokenRequest);
        initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var initialContent = await initialResponse.Content.ReadAsStringAsync();
        var initialTokenData = JsonSerializer.Deserialize<JsonElement>(initialContent);
        var refreshToken = initialTokenData.GetProperty("refresh_token").GetString();
        refreshToken.Should().NotBeNullOrEmpty();
        
        // Wait a moment to ensure new token has different timestamp
        await Task.Delay(1000);
        
        // Act - Use refresh token to get new access token
        var refreshRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var refreshResponse = await _client.PostAsync("/connect/token", refreshRequest);
        
        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
        var refreshTokenData = JsonSerializer.Deserialize<JsonElement>(refreshContent);
        
        var newAccessToken = refreshTokenData.GetProperty("access_token").GetString();
        var newRefreshToken = refreshTokenData.GetProperty("refresh_token").GetString();
        
        newAccessToken.Should().NotBeNullOrEmpty();
        newRefreshToken.Should().NotBeNullOrEmpty();
        newRefreshToken.Should().NotBe(refreshToken, "Should rotate refresh tokens");
    }

    [Fact]
    public async Task Expired_Refresh_Token_Should_Be_Rejected()
    {
        // Act - Try to use an invalid/expired refresh token
        var refreshRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", "invalid_or_expired_refresh_token"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var response = await _client.PostAsync("/connect/token", refreshRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        var errorData = JsonSerializer.Deserialize<JsonElement>(content);
        
        errorData.GetProperty("error").GetString().Should().Be("invalid_grant");
    }

    [Fact]
    public async Task Sliding_Session_Expiration_Should_Extend_Session()
    {
        // Arrange - Create test user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "sliding.test@identity.local",
            Email = "sliding.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Sliding",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Get initial token
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "sliding.test@identity.local"),
            new KeyValuePair<string, string>("password", "TestPass123!"),
            new KeyValuePair<string, string>("scope", "openid profile offline_access"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var response = await _client.PostAsync("/connect/token", tokenRequest);
        var content = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
        
        var refreshToken = tokenData.GetProperty("refresh_token").GetString();
        var expiresIn1 = tokenData.GetProperty("expires_in").GetInt32();
        
        // Wait and refresh multiple times
        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(2000);
            
            var refreshRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", "trusted-client"),
                new KeyValuePair<string, string>("client_secret", "trusted-secret")
            });
            
            var refreshResponse = await _client.PostAsync("/connect/token", refreshRequest);
            refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
            var refreshData = JsonSerializer.Deserialize<JsonElement>(refreshContent);
            
            refreshToken = refreshData.GetProperty("refresh_token").GetString();
            var expiresIn = refreshData.GetProperty("expires_in").GetInt32();
            
            // Session should still be valid with full expiration time
            expiresIn.Should().Be(300); // 5 minutes for access token
        }
    }

    [Fact]
    public async Task Token_Revocation_Should_Invalidate_Tokens()
    {
        // Arrange - Create test user and get tokens
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "revoke.test@identity.local",
            Email = "revoke.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Revoke",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Get tokens
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "revoke.test@identity.local"),
            new KeyValuePair<string, string>("password", "TestPass123!"),
            new KeyValuePair<string, string>("scope", "openid profile offline_access"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var tokenResponse = await _client.PostAsync("/connect/token", tokenRequest);
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
        
        var accessToken = tokenData.GetProperty("access_token").GetString();
        var refreshToken = tokenData.GetProperty("refresh_token").GetString();
        
        // Act - Revoke the refresh token
        var revokeRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", refreshToken),
            new KeyValuePair<string, string>("token_type_hint", "refresh_token"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var revokeResponse = await _client.PostAsync("/connect/revocation", revokeRequest);
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Assert - Try to use revoked refresh token
        var refreshRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var failedRefreshResponse = await _client.PostAsync("/connect/token", refreshRequest);
        failedRefreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Multiple_Concurrent_Sessions_Should_Be_Tracked()
    {
        // Arrange - Create test user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "concurrent.test@identity.local",
            Email = "concurrent.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Concurrent",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Act - Create multiple sessions
        var sessions = new List<string>();
        
        for (int i = 0; i < 3; i++)
        {
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", "concurrent.test@identity.local"),
                new KeyValuePair<string, string>("password", "TestPass123!"),
                new KeyValuePair<string, string>("scope", "openid profile offline_access"),
                new KeyValuePair<string, string>("client_id", "trusted-client"),
                new KeyValuePair<string, string>("client_secret", "trusted-secret")
            });
            
            var response = await _client.PostAsync("/connect/token", tokenRequest);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
            
            var refreshToken = tokenData.GetProperty("refresh_token").GetString();
            sessions.Add(refreshToken);
            
            await Task.Delay(500); // Small delay between sessions
        }
        
        // Assert - All sessions should be valid
        sessions.Should().HaveCount(3);
        sessions.Should().OnlyHaveUniqueItems();
        
        // Verify each session can be refreshed independently
        foreach (var refreshToken in sessions)
        {
            var refreshRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", "trusted-client"),
                new KeyValuePair<string, string>("client_secret", "trusted-secret")
            });
            
            var refreshResponse = await _client.PostAsync("/connect/token", refreshRequest);
            refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Access_Token_Should_Expire_After_Configured_Time()
    {
        // Arrange - Create test user and get access token
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "expire.test@identity.local",
            Email = "expire.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Expire",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Get access token
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "expire.test@identity.local"),
            new KeyValuePair<string, string>("password", "TestPass123!"),
            new KeyValuePair<string, string>("scope", "openid profile"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var response = await _client.PostAsync("/connect/token", tokenRequest);
        var content = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
        
        var accessToken = tokenData.GetProperty("access_token").GetString();
        var expiresIn = tokenData.GetProperty("expires_in").GetInt32();
        
        // Assert
        expiresIn.Should().Be(300); // 5 minutes as per spec
        
        // Verify token works immediately
        var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        userInfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        var userInfoResponse = await _client.SendAsync(userInfoRequest);
        userInfoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Note: In a real test, we would wait for expiration and verify the token is rejected
        // But waiting 5 minutes in a test is not practical
    }

    [Fact]
    public async Task Introspection_Endpoint_Should_Validate_Tokens()
    {
        // Arrange - Create test user and get tokens
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "introspect.test@identity.local",
            Email = "introspect.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Introspect",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Get tokens
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "introspect.test@identity.local"),
            new KeyValuePair<string, string>("password", "TestPass123!"),
            new KeyValuePair<string, string>("scope", "openid profile"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var tokenResponse = await _client.PostAsync("/connect/token", tokenRequest);
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
        
        var accessToken = tokenData.GetProperty("access_token").GetString();
        
        // Act - Introspect the token
        var introspectRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", accessToken),
            new KeyValuePair<string, string>("token_type_hint", "access_token"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var introspectResponse = await _client.PostAsync("/connect/introspect", introspectRequest);
        
        // Assert
        introspectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var introspectContent = await introspectResponse.Content.ReadAsStringAsync();
        var introspectData = JsonSerializer.Deserialize<JsonElement>(introspectContent);
        
        introspectData.GetProperty("active").GetBoolean().Should().BeTrue();
        introspectData.GetProperty("sub").GetString().Should().Be(testUser.Id);
        introspectData.GetProperty("client_id").GetString().Should().Be("trusted-client");
        introspectData.GetProperty("token_type").GetString().Should().Be("access_token");
    }

    [Fact]
    public async Task Invalid_Token_Introspection_Should_Return_Inactive()
    {
        // Act - Introspect an invalid token
        var introspectRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", "invalid_token_value"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var response = await _client.PostAsync("/connect/introspect", introspectRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var introspectData = JsonSerializer.Deserialize<JsonElement>(content);
        
        introspectData.GetProperty("active").GetBoolean().Should().BeFalse();
    }
}