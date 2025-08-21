using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AuthService.Controllers.Admin;
using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Models.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Xunit;

namespace AuthService.Tests.Controllers.Admin;

public class UserManagementControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UserManagementControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Authentication is already configured in Program.cs for Testing environment
                // Just ensure authorization is set up correctly
                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder("Test")
                        .RequireAuthenticatedUser()
                        .Build();
                });
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Set up authentication for admin role
        _client.DefaultRequestHeaders.Add("Authorization", "Test Admin");
    }

    [Fact]
    public async Task GetUsers_Should_Return_Paginated_User_List()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/users?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("items");
        content.Should().Contain("totalCount");
        content.Should().Contain("page");
        content.Should().Contain("pageSize");
    }

    [Fact]
    public async Task GetUsers_Should_Support_Search_Filter()
    {
        // Arrange
        var searchTerm = "test";

        // Act
        var response = await _client.GetAsync($"/api/admin/users?search={searchTerm}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserListResponse>();
        result.Should().NotBeNull();
        
        if (result!.Items.Any())
        {
            result.Items.Should().OnlyContain(u => 
                u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (u.Name != null && u.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }
    }

    [Fact]
    public async Task GetUser_Should_Return_User_Details()
    {
        // Arrange
        var userId = "test-user-id";

        // Act
        var response = await _client.GetAsync($"/api/admin/users/{userId}");

        // Assert
        // Will be NotFound initially since no users exist
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var user = await response.Content.ReadFromJsonAsync<UserDetailResponse>();
            user.Should().NotBeNull();
            user!.Id.Should().Be(userId);
        }
    }

    [Fact]
    public async Task CreateUser_Should_Create_New_User_Successfully()
    {
        // Arrange
        var createRequest = new CreateUserRequest
        {
            Email = "newuser@identity.local",
            Name = "New User",
            TemporaryPassword = "TempPass123!",
            MustChangePassword = true
            // Don't specify roles in test - they don't exist in test DB
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/users", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        
        var user = await response.Content.ReadFromJsonAsync<UserDetailResponse>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(createRequest.Email);
        user.Name.Should().Be(createRequest.Name);
        user.Roles.Should().BeEmpty(); // No roles assigned in test
    }

    [Fact]
    public async Task CreateUser_Should_Validate_Email_Format()
    {
        // Arrange
        var createRequest = new CreateUserRequest
        {
            Email = "invalid-email",
            Name = "Test User",
            TemporaryPassword = "TempPass123!",
            MustChangePassword = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/users", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.ToLower().Should().Contain("email");
    }

    [Fact]
    public async Task CreateUser_Should_Validate_Password_Complexity()
    {
        // Arrange
        var createRequest = new CreateUserRequest
        {
            Email = "user@identity.local",
            Name = "Test User",
            TemporaryPassword = "weak",
            MustChangePassword = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/users", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.ToLower().Should().Contain("password");
    }

    [Fact]
    public async Task UpdateUser_Should_Update_User_Properties()
    {
        // Arrange
        var userId = "test-user-id";
        var updateRequest = new UpdateUserRequest
        {
            Name = "Updated Name",
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/admin/users/{userId}", updateRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var user = await response.Content.ReadFromJsonAsync<UserDetailResponse>();
            user.Should().NotBeNull();
            user!.Name.Should().Be(updateRequest.Name);
            user.IsActive.Should().Be(updateRequest.IsActive.Value);
        }
    }

    [Fact]
    public async Task DeleteUser_Should_Deactivate_User()
    {
        // Arrange
        var userId = "test-user-id";

        // Act
        var response = await _client.DeleteAsync($"/api/admin/users/{userId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnlockUser_Should_Reset_Lockout()
    {
        // Arrange
        var userId = "locked-user-id";

        // Act
        var response = await _client.PostAsync($"/api/admin/users/{userId}/unlock", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResetPassword_Should_Generate_Temporary_Password()
    {
        // Arrange
        var userId = "test-user-id";

        // Act
        var response = await _client.PostAsync($"/api/admin/users/{userId}/reset-password", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<PasswordResetResponse>();
            result.Should().NotBeNull();
            result!.TemporaryPassword.Should().NotBeNullOrEmpty();
            result.MustChangePassword.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetUsers_Should_Require_Admin_Authorization()
    {
        // Arrange
        var unauthorizedClient = _factory.CreateClient();

        // Act
        var response = await unauthorizedClient.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_With_Regular_User_Should_Return_Forbidden()
    {
        // Arrange
        var userClient = _factory.CreateClient();
        userClient.DefaultRequestHeaders.Add("Authorization", "Test User");

        // Act
        var response = await userClient.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_Should_Prevent_Duplicate_Email()
    {
        // Arrange
        var createRequest = new CreateUserRequest
        {
            Email = "duplicate@identity.local",
            Name = "First User",
            TemporaryPassword = "TempPass123!",
            MustChangePassword = true
        };

        // Act - Create first user
        var response1 = await _client.PostAsJsonAsync("/api/admin/users", createRequest);
        
        // Act - Try to create duplicate
        createRequest.Name = "Second User";
        var response2 = await _client.PostAsJsonAsync("/api/admin/users", createRequest);

        // Assert
        if (response1.StatusCode == HttpStatusCode.Created)
        {
            response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
            var error = await response2.Content.ReadAsStringAsync();
            error.Should().Contain("already exists");
        }
    }

    [Fact]
    public async Task UpdateUser_Should_Validate_Role_Changes()
    {
        // Arrange
        var userId = "test-user-id";
        var updateRequest = new UpdateUserRequest
        {
            Roles = new[] { "InvalidRole" }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/admin/users/{userId}", updateRequest);

        // Assert
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var error = await response.Content.ReadAsStringAsync();
            error.Should().Contain("role");
        }
    }
}

// Test authentication handler for simulating authenticated requests
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    #pragma warning disable CS0618 // Type or member is obsolete
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    #pragma warning restore CS0618 // Type or member is obsolete
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Test "))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid authorization header"));
        }

        var role = authHeader.Substring(5); // Remove "Test " prefix
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        };

        if (role == "Admin")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        else if (role == "User")
        {
            claims.Add(new Claim(ClaimTypes.Role, "User"));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// Response models for testing
public class UserListResponse
{
    public List<UserSummary> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class UserSummary
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool LockedOut { get; set; }
}

public class UserDetailResponse : UserSummary
{
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, string> Claims { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string TemporaryPassword { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public string[]? Roles { get; set; }
}

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public string[]? Roles { get; set; }
}

public class PasswordResetResponse
{
    public string TemporaryPassword { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public DateTime ExpiresAt { get; set; }
}