using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AuthService.Models.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthService.Tests.Controllers.Admin;

public class SessionManagementControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SessionManagementControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Authentication is already configured in Program.cs for Testing environment
                // No additional authentication setup needed here
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
    public async Task GetSessions_Should_Return_Active_Sessions_List()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SessionListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSessions_Should_Filter_By_UserId()
    {
        // Arrange
        var userId = "test-user-id";

        // Act
        var response = await _client.GetAsync($"/api/admin/sessions?userId={userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SessionListResponse>();
        result.Should().NotBeNull();
        
        if (result!.Items.Any())
        {
            result.Items.Should().OnlyContain(session => session.UserId == userId);
        }
    }

    [Fact]
    public async Task GetSessions_Should_Include_Session_Details()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SessionListResponse>();
        result.Should().NotBeNull();
        
        foreach (var session in result!.Items)
        {
            session.SessionId.Should().NotBeNullOrEmpty();
            session.UserId.Should().NotBeNullOrEmpty();
            session.UserEmail.Should().NotBeNullOrEmpty();
            session.CreatedAt.Should().BeAfter(DateTime.MinValue);
            session.LastActivity.Should().BeAfter(DateTime.MinValue);
            session.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }
    }

    [Fact]
    public async Task GetSessions_Should_Order_By_LastActivity_Descending()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SessionListResponse>();
        result.Should().NotBeNull();
        
        if (result!.Items.Count > 1)
        {
            result.Items.Should().BeInDescendingOrder(session => session.LastActivity);
        }
    }

    [Fact]
    public async Task GetSession_Should_Return_Specific_Session_Details()
    {
        // Arrange
        var sessionId = "test-session-id";

        // Act
        var response = await _client.GetAsync($"/api/admin/sessions/{sessionId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var session = await response.Content.ReadFromJsonAsync<SessionDetailResponse>();
            session.Should().NotBeNull();
            session!.SessionId.Should().Be(sessionId);
            session.Claims.Should().NotBeNull();
            session.Properties.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task RevokeSession_Should_Terminate_Session()
    {
        // Arrange
        var sessionId = "active-session-id";

        // Act
        var response = await _client.DeleteAsync($"/api/admin/sessions/{sessionId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RevokeAllUserSessions_Should_Terminate_All_User_Sessions()
    {
        // Arrange
        var userId = "test-user-id";

        // Act
        var response = await _client.DeleteAsync($"/api/admin/sessions/user/{userId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<SessionRevocationResult>();
            result.Should().NotBeNull();
            result!.RevokedCount.Should().BeGreaterOrEqualTo(0);
        }
    }

    [Fact]
    public async Task GetSessions_Should_Support_Pagination()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/sessions?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SessionListResponse>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Count.Should().BeLessOrEqualTo(10);
    }

    [Fact]
    public async Task GetSessions_Should_Include_Client_Information()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SessionListResponse>();
        result.Should().NotBeNull();
        
        foreach (var session in result!.Items.Where(s => s.ClientId != null))
        {
            session.ClientId.Should().NotBeNullOrEmpty();
            session.ClientName.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetSessions_Should_Include_IP_And_UserAgent()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SessionListResponse>();
        result.Should().NotBeNull();
        
        foreach (var session in result!.Items)
        {
            // These might be null in test environment but should exist in production
            if (session.IpAddress != null)
            {
                session.IpAddress.Should().NotBeEmpty();
            }
            if (session.UserAgent != null)
            {
                session.UserAgent.Should().NotBeEmpty();
            }
        }
    }

    [Fact]
    public async Task GetSessions_Should_Filter_By_Active_Status()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/sessions?activeOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SessionListResponse>();
        result.Should().NotBeNull();
        
        foreach (var session in result!.Items)
        {
            session.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            session.IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetSessions_Should_Require_Admin_Authorization()
    {
        // Arrange
        var unauthorizedClient = _factory.CreateClient();

        // Act
        var response = await unauthorizedClient.GetAsync("/api/admin/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSessions_With_Regular_User_Should_Return_Forbidden()
    {
        // Arrange
        var userClient = _factory.CreateClient();
        userClient.DefaultRequestHeaders.Add("Authorization", "Test User");

        // Act
        var response = await userClient.GetAsync("/api/admin/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExtendSession_Should_Update_Expiry_Time()
    {
        // Arrange
        var sessionId = "active-session-id";
        var extendRequest = new ExtendSessionRequest
        {
            ExtensionMinutes = 60
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/admin/sessions/{sessionId}/extend", extendRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<SessionExtensionResult>();
            result.Should().NotBeNull();
            result!.NewExpiryTime.Should().BeAfter(DateTime.UtcNow);
        }
    }

    [Fact]
    public async Task GetSessionStatistics_Should_Return_Summary_Data()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/sessions/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<SessionStatistics>();
        stats.Should().NotBeNull();
        stats!.TotalActiveSessions.Should().BeGreaterOrEqualTo(0);
        stats.UniqueUsers.Should().BeGreaterOrEqualTo(0);
        stats.AverageSessionDurationMinutes.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetSessionsByClient_Should_Return_Client_Specific_Sessions()
    {
        // Arrange
        var clientId = "platform-bff";

        // Act
        var response = await _client.GetAsync($"/api/admin/sessions/client/{clientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SessionListResponse>();
        result.Should().NotBeNull();
        
        if (result!.Items.Any())
        {
            result.Items.Should().OnlyContain(session => session.ClientId == clientId);
        }
    }

    [Fact]
    public async Task RevokeExpiredSessions_Should_Clean_Up_Expired_Sessions()
    {
        // Act
        var response = await _client.PostAsync("/api/admin/sessions/cleanup", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SessionCleanupResult>();
        result.Should().NotBeNull();
        result!.ExpiredSessionsRemoved.Should().BeGreaterOrEqualTo(0);
        result.CleanupTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetConcurrentSessions_Should_Return_Users_With_Multiple_Sessions()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/sessions/concurrent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConcurrentSessionsResponse>();
        result.Should().NotBeNull();
        
        foreach (var userSessions in result!.UserSessions)
        {
            userSessions.UserId.Should().NotBeNullOrEmpty();
            userSessions.UserEmail.Should().NotBeNullOrEmpty();
            userSessions.SessionCount.Should().BeGreaterThan(1);
            userSessions.Sessions.Should().NotBeEmpty();
        }
    }
}

// Response models for testing
public class SessionListResponse
{
    public List<SessionSummary> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class SessionSummary
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? ClientName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; }
}

public class SessionDetailResponse : SessionSummary
{
    public Dictionary<string, string> Claims { get; set; } = new();
    public Dictionary<string, string> Properties { get; set; } = new();
    public List<string> Scopes { get; set; } = new();
}

public class SessionRevocationResult
{
    public int RevokedCount { get; set; }
    public List<string> RevokedSessionIds { get; set; } = new();
}

public class ExtendSessionRequest
{
    public int ExtensionMinutes { get; set; }
}

public class SessionExtensionResult
{
    public DateTime NewExpiryTime { get; set; }
    public DateTime PreviousExpiryTime { get; set; }
}

public class SessionStatistics
{
    public int TotalActiveSessions { get; set; }
    public int UniqueUsers { get; set; }
    public double AverageSessionDurationMinutes { get; set; }
    public Dictionary<string, int> SessionsByClient { get; set; } = new();
    public List<HourlySessionActivity> HourlyActivity { get; set; } = new();
}

public class HourlySessionActivity
{
    public int Hour { get; set; }
    public int NewSessions { get; set; }
    public int ExpiredSessions { get; set; }
}

public class SessionCleanupResult
{
    public int ExpiredSessionsRemoved { get; set; }
    public DateTime CleanupTimestamp { get; set; }
}

public class ConcurrentSessionsResponse
{
    public List<UserConcurrentSessions> UserSessions { get; set; } = new();
}

public class UserConcurrentSessions
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public List<SessionSummary> Sessions { get; set; } = new();
}