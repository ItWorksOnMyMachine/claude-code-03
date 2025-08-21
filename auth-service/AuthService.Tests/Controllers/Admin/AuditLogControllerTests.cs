using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AuthService.Data.Entities;
using AuthService.Models.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthService.Tests.Controllers.Admin;

public class AuditLogControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuditLogControllerTests(WebApplicationFactory<Program> factory)
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
    public async Task GetAuditLogs_Should_Return_Paginated_Log_List()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/audit-logs?page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogListResponse>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetAuditLogs_Should_Filter_By_UserId()
    {
        // Arrange
        var userId = "test-user-id";

        // Act
        var response = await _client.GetAsync($"/api/admin/audit-logs?userId={userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogListResponse>();
        result.Should().NotBeNull();
        
        if (result!.Items.Any())
        {
            result.Items.Should().OnlyContain(log => log.UserId == userId);
        }
    }

    [Fact]
    public async Task GetAuditLogs_Should_Filter_By_EventType()
    {
        // Arrange
        var eventType = AuthenticationEventType.LoginSuccess.ToString();

        // Act
        var response = await _client.GetAsync($"/api/admin/audit-logs?eventType={eventType}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogListResponse>();
        result.Should().NotBeNull();
        
        if (result!.Items.Any())
        {
            result.Items.Should().OnlyContain(log => log.EventType == eventType);
        }
    }

    [Fact]
    public async Task GetAuditLogs_Should_Filter_By_Date_Range()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync(
            $"/api/admin/audit-logs?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogListResponse>();
        result.Should().NotBeNull();
        
        if (result!.Items.Any())
        {
            var startDateTime = DateTime.Parse(startDate);
            var endDateTime = DateTime.Parse(endDate).AddDays(1); // Include entire end day
            
            result.Items.Should().OnlyContain(log => 
                log.Timestamp >= startDateTime && log.Timestamp < endDateTime);
        }
    }

    [Fact]
    public async Task GetAuditLogs_Should_Support_Multiple_Filters()
    {
        // Arrange
        var userId = "test-user-id";
        var eventType = AuthenticationEventType.LoginFailed.ToString();
        var startDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync(
            $"/api/admin/audit-logs?userId={userId}&eventType={eventType}&startDate={startDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogListResponse>();
        result.Should().NotBeNull();
        
        if (result!.Items.Any())
        {
            result.Items.Should().OnlyContain(log => 
                log.UserId == userId && 
                log.EventType == eventType &&
                log.Timestamp >= DateTime.Parse(startDate));
        }
    }

    [Fact]
    public async Task GetAuditLogs_Should_Order_By_Timestamp_Descending()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogListResponse>();
        result.Should().NotBeNull();
        
        if (result!.Items.Count > 1)
        {
            result.Items.Should().BeInDescendingOrder(log => log.Timestamp);
        }
    }

    [Fact]
    public async Task GetAuditLogs_Should_Include_Failure_Details()
    {
        // Arrange
        var eventType = AuthenticationEventType.LoginFailed.ToString();

        // Act
        var response = await _client.GetAsync($"/api/admin/audit-logs?eventType={eventType}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogListResponse>();
        result.Should().NotBeNull();
        
        var failedLogs = result!.Items.Where(log => log.EventType == eventType);
        foreach (var log in failedLogs)
        {
            log.Success.Should().BeFalse();
            log.FailureReason.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetAuditLogs_Should_Require_Admin_Authorization()
    {
        // Arrange
        var unauthorizedClient = _factory.CreateClient();

        // Act
        var response = await unauthorizedClient.GetAsync("/api/admin/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuditLogs_With_Regular_User_Should_Return_Forbidden()
    {
        // Arrange
        var userClient = _factory.CreateClient();
        userClient.DefaultRequestHeaders.Add("Authorization", "Test User");

        // Act
        var response = await userClient.GetAsync("/api/admin/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAuditLogs_Should_Validate_Page_Parameters()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/audit-logs?page=0&pageSize=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.ToLower().Should().Contain("page");
    }

    [Fact]
    public async Task GetAuditLogs_Should_Limit_Maximum_PageSize()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/audit-logs?pageSize=1000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuditLogListResponse>();
        result.Should().NotBeNull();
        result!.PageSize.Should().BeLessOrEqualTo(100); // Maximum allowed page size
    }

    [Fact]
    public async Task GetAuditLogStatistics_Should_Return_Summary_Data()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/audit-logs/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<AuditLogStatistics>();
        stats.Should().NotBeNull();
        stats!.TotalLogins.Should().BeGreaterOrEqualTo(0);
        stats.FailedLoginAttempts.Should().BeGreaterOrEqualTo(0);
        stats.UniqueUsers.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetSuspiciousActivity_Should_Return_Anomalies()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/audit-logs/suspicious");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var activities = await response.Content.ReadFromJsonAsync<List<SuspiciousActivity>>();
        activities.Should().NotBeNull();
        
        foreach (var activity in activities!)
        {
            activity.IpAddress.Should().NotBeNullOrEmpty();
            activity.FailedAttempts.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task ExportAuditLogs_Should_Return_CSV_File()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync(
            $"/api/admin/audit-logs/export?startDate={startDate}&endDate={endDate}&format=csv");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        response.Content.Headers.ContentDisposition.Should().NotBeNull();
        response.Content.Headers.ContentDisposition!.FileName.Should().Contain("audit-logs");
    }
}

// Response models for testing
public class AuditLogListResponse
{
    public List<AuditLogEntry> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? AdditionalData { get; set; }
}

public class AuditLogStatistics
{
    public int TotalLogins { get; set; }
    public int FailedLoginAttempts { get; set; }
    public int UniqueUsers { get; set; }
    public int AccountLockouts { get; set; }
    public Dictionary<string, int> EventTypeCounts { get; set; } = new();
    public List<HourlyActivity> HourlyActivity { get; set; } = new();
}

public class HourlyActivity
{
    public int Hour { get; set; }
    public int LoginCount { get; set; }
    public int FailureCount { get; set; }
}

public class SuspiciousActivity
{
    public string IpAddress { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTime FirstAttempt { get; set; }
    public DateTime LastAttempt { get; set; }
    public List<string> TargetedUsers { get; set; } = new();
}