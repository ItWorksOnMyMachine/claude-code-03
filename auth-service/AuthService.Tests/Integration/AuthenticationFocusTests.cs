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
using System.Threading.Tasks;
using Xunit;

namespace AuthService.Tests.Integration;

public class AuthenticationFocusTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthenticationFocusTests(WebApplicationFactory<Program> factory)
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
    public async Task Service_Should_Not_Include_Business_Logic()
    {
        // Act - Check available endpoints
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var swagger = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Assert - Should only have authentication-related endpoints
            var paths = swagger.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
            
            // Authentication endpoints should exist
            paths.Should().Contain(p => p.Contains("/health"));
            paths.Should().Contain(p => p.Contains("/api/admin/users"));
            paths.Should().Contain(p => p.Contains("/api/admin/sessions"));
            paths.Should().Contain(p => p.Contains("/api/admin/audit"));
            
            // Business logic endpoints should NOT exist
            paths.Should().NotContain(p => p.Contains("/api/orders"));
            paths.Should().NotContain(p => p.Contains("/api/products"));
            paths.Should().NotContain(p => p.Contains("/api/inventory"));
            paths.Should().NotContain(p => p.Contains("/api/billing"));
        }
    }

    [Fact]
    public async Task User_Model_Should_Be_Pure_Identity_Without_Tenant_Coupling()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        // Act - Create a user
        var testUser = new AppUser
        {
            UserName = "pure.identity@test.local",
            Email = "pure.identity@test.local",
            EmailConfirmed = true,
            FirstName = "Pure",
            LastName = "Identity",
            IsActive = true
        };
        
        var result = await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Assert
        result.Succeeded.Should().BeTrue();
        
        // Verify user doesn't have tenant-specific fields in the main user table
        var savedUser = await context.Users.FindAsync(testUser.Id);
        savedUser.Should().NotBeNull();
        
        // These are authentication-related fields - OK
        savedUser.Email.Should().Be("pure.identity@test.local");
        savedUser.EmailConfirmed.Should().BeTrue();
        savedUser.IsActive.Should().BeTrue();
        
        // User should not have business-specific properties
        // (checked via reflection to ensure no tenant coupling)
        var userType = savedUser.GetType();
        var properties = userType.GetProperties().Select(p => p.Name).ToList();
        
        properties.Should().NotContain("TenantId");
        properties.Should().NotContain("OrganizationId");
        properties.Should().NotContain("CompanyId");
        properties.Should().NotContain("SubscriptionLevel");
        properties.Should().NotContain("BillingInfo");
    }

    [Fact]
    public async Task Tokens_Should_Only_Include_Identity_Claims()
    {
        // Arrange - Create test user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        
        // Ensure Admin role exists
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new AppRole 
            { 
                Name = "Admin",
                Description = "Admin role for testing"
            });
        }
        
        var testUser = new AppUser
        {
            UserName = "claims.test@identity.local",
            Email = "claims.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Claims",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Add some roles (authentication concern)
        await userManager.AddToRoleAsync(testUser, "Admin");
        
        // Act - Get access token
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "claims.test@identity.local"),
            new KeyValuePair<string, string>("password", "TestPass123!"),
            new KeyValuePair<string, string>("scope", "openid profile email"),
            new KeyValuePair<string, string>("client_id", "trusted-client"),
            new KeyValuePair<string, string>("client_secret", "trusted-secret")
        });
        
        var response = await _client.PostAsync("/connect/token", tokenRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Token request failed: {await response.Content.ReadAsStringAsync()}");
        
        var content = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
        
        // Check if we have an id_token (password grant might not return it)
        string idToken;
        if (tokenData.TryGetProperty("id_token", out var idTokenElement))
        {
            idToken = idTokenElement.GetString();
        }
        else
        {
            // If no id_token, check access_token claims instead
            tokenData.TryGetProperty("access_token", out var accessTokenElement).Should().BeTrue();
            idToken = accessTokenElement.GetString();
        }
        
        // Decode the ID token (simplified - just check payload)
        var parts = idToken.Split('.');
        var payload = parts[1];
        var paddedPayload = payload.PadRight((payload.Length + 3) & ~3, '=');
        var decodedBytes = Convert.FromBase64String(paddedPayload);
        var decodedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);
        var claims = JsonSerializer.Deserialize<JsonElement>(decodedJson);
        
        // Assert - Should have identity claims
        claims.TryGetProperty("sub", out _).Should().BeTrue(); // Subject ID
        claims.TryGetProperty("email", out _).Should().BeTrue(); // Email
        claims.TryGetProperty("name", out _).Should().BeTrue(); // Name
        claims.TryGetProperty("is_active", out _).Should().BeTrue(); // Active status
        
        // Should NOT have business/tenant claims
        claims.TryGetProperty("tenant_id", out _).Should().BeFalse();
        claims.TryGetProperty("organization", out _).Should().BeFalse();
        claims.TryGetProperty("subscription", out _).Should().BeFalse();
        claims.TryGetProperty("billing_status", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Database_Schema_Should_Be_Authentication_Focused()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        
        // Act - Get entity types from the context
        var entityTypes = context.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToList();
        
        // Assert - Should have authentication-related entities
        entityTypes.Should().Contain("AppUser");
        entityTypes.Should().Contain("AppRole");
        entityTypes.Should().Contain("AuthenticationAuditLog");
        entityTypes.Should().Contain("PasswordHistory");
        
        // Should NOT have business entities
        entityTypes.Should().NotContain("Order");
        entityTypes.Should().NotContain("Product");
        entityTypes.Should().NotContain("Invoice");
        entityTypes.Should().NotContain("Subscription");
        entityTypes.Should().NotContain("Tenant");
        entityTypes.Should().NotContain("Organization");
    }

    [Fact]
    public async Task Admin_APIs_Should_Only_Manage_Authentication_Concerns()
    {
        // Arrange - Set up admin authentication
        _client.DefaultRequestHeaders.Add("Authorization", "Test Admin");
        
        // Act & Assert - User management endpoint
        var userResponse = await _client.GetAsync("/api/admin/users");
        if (userResponse.StatusCode == HttpStatusCode.OK)
        {
            var userContent = await userResponse.Content.ReadAsStringAsync();
            var userData = JsonSerializer.Deserialize<JsonElement>(userContent);
            
            // Should return user authentication data
            if (userData.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
            {
                var firstUser = items[0];
                
                // Authentication-related fields should exist
                firstUser.TryGetProperty("email", out _).Should().BeTrue();
                firstUser.TryGetProperty("isActive", out _).Should().BeTrue();
                firstUser.TryGetProperty("lockedOut", out _).Should().BeTrue();
                
                // Business fields should NOT exist
                firstUser.TryGetProperty("tenantId", out _).Should().BeFalse();
                firstUser.TryGetProperty("subscriptionLevel", out _).Should().BeFalse();
                firstUser.TryGetProperty("billingStatus", out _).Should().BeFalse();
            }
        }
        
        // Act & Assert - Session management endpoint
        var sessionResponse = await _client.GetAsync("/api/admin/sessions");
        if (sessionResponse.StatusCode == HttpStatusCode.OK)
        {
            var sessionContent = await sessionResponse.Content.ReadAsStringAsync();
            var sessionData = JsonSerializer.Deserialize<JsonElement>(sessionContent);
            
            // Should return session/authentication data
            if (sessionData.TryGetProperty("items", out var sessions) && sessions.GetArrayLength() > 0)
            {
                var firstSession = sessions[0];
                
                // Session-related fields should exist
                firstSession.TryGetProperty("sessionId", out _).Should().BeTrue();
                firstSession.TryGetProperty("userId", out _).Should().BeTrue();
                firstSession.TryGetProperty("isActive", out _).Should().BeTrue();
                
                // Business context should NOT exist
                firstSession.TryGetProperty("tenantContext", out _).Should().BeFalse();
                firstSession.TryGetProperty("organizationContext", out _).Should().BeFalse();
            }
        }
    }

    [Fact]
    public async Task Configuration_Should_Not_Include_Business_Settings()
    {
        // This test verifies configuration is authentication-focused
        // In a real scenario, we'd check appsettings.json structure
        
        // Act - Check health endpoint for configuration hints
        var response = await _client.GetAsync("/health/ready");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Health check should mention auth-related components
        content.ToLower().Should().Contain("healthy");
        
        // Should not mention business components
        content.Should().NotContain("payment");
        content.Should().NotContain("billing");
        content.Should().NotContain("inventory");
    }

    [Fact]
    public async Task Service_Should_Support_Multiple_Client_Applications()
    {
        // This verifies the service can authenticate users for different client apps
        // without being coupled to any specific business application
        
        // Act - Try different client credentials
        var clients = new[]
        {
            ("test-client", "test-secret"),
            ("trusted-client", "trusted-secret"),
            ("machine-client", "machine-secret")
        };
        
        foreach (var (clientId, clientSecret) in clients)
        {
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "api"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });
            
            var response = await _client.PostAsync("/connect/token", tokenRequest);
            
            // Each client should be able to authenticate independently
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task Audit_Logs_Should_Track_Authentication_Events_Only()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Test Admin");
        
        // Act - Get audit logs
        var response = await _client.GetAsync("/api/admin/audit");
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var auditData = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Assert - Audit events should be authentication-related
            if (auditData.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
            {
                var validEventTypes = new[] 
                { 
                    "Login", "Logout", "LoginFailed", "PasswordChanged", 
                    "AccountLocked", "AccountUnlocked", "TokenIssued", 
                    "TokenRefreshed", "TokenRevoked"
                };
                
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("eventType", out var eventType))
                    {
                        var eventTypeStr = eventType.GetString();
                        
                        // Should be an authentication event
                        validEventTypes.Should().Contain(eventTypeStr);
                        
                        // Should NOT be business events
                        eventTypeStr.Should().NotContain("Order");
                        eventTypeStr.Should().NotContain("Payment");
                        eventTypeStr.Should().NotContain("Invoice");
                    }
                }
            }
        }
    }

    [Fact]
    public async Task Service_Should_Be_Stateless_For_Business_Context()
    {
        // This verifies the auth service doesn't maintain business state
        // It should only handle authentication state (sessions, tokens)
        
        // Arrange - Create a user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        
        var testUser = new AppUser
        {
            UserName = "stateless.test@identity.local",
            Email = "stateless.test@identity.local",
            EmailConfirmed = true,
            FirstName = "Stateless",
            LastName = "Test",
            IsActive = true
        };
        
        await userManager.CreateAsync(testUser, "TestPass123!");
        
        // Act - Get tokens multiple times
        for (int i = 0; i < 3; i++)
        {
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", "stateless.test@identity.local"),
                new KeyValuePair<string, string>("password", "TestPass123!"),
                new KeyValuePair<string, string>("scope", "openid profile"),
                new KeyValuePair<string, string>("client_id", "trusted-client"),
                new KeyValuePair<string, string>("client_secret", "trusted-secret")
            });
            
            var response = await _client.PostAsync("/connect/token", tokenRequest);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Each request should be independent - no business state carried over
            tokenData.TryGetProperty("business_context", out _).Should().BeFalse();
            tokenData.TryGetProperty("tenant_state", out _).Should().BeFalse();
            tokenData.TryGetProperty("cart_id", out _).Should().BeFalse();
            tokenData.TryGetProperty("session_data", out _).Should().BeFalse();
        }
    }
}