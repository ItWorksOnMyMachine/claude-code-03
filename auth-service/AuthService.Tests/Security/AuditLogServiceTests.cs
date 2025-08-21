using System;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthService.Tests.Security;

public class AuditLogServiceTests
{
    private readonly AuthDbContext _context;
    private readonly Mock<ILogger<AuditLogService>> _logger;
    private readonly AuditLogService _service;
    public AuditLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AuthDbContext(options);

        _logger = new Mock<ILogger<AuditLogService>>();
        _service = new AuditLogService(_context, _logger.Object);
    }

    [Fact]
    public async Task LogAuthenticationAsync_Should_Create_Audit_Entry()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        // Act
        await _service.LogAuthenticationAsync(
            userId,
            AuthenticationEventType.LoginSuccess,
            ipAddress,
            userAgent);

        // Assert
        var log = await _context.AuthenticationAuditLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.UserId.Should().Be(userId);
        log.EventType.Should().Be(AuthenticationEventType.LoginSuccess);
        log.IpAddress.Should().Be(ipAddress);
        log.UserAgent.Should().Be(userAgent);
        log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LogAuthenticationAsync_Should_Include_Additional_Data()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var additionalData = new { Reason = "Invalid password", AttemptCount = 3 };

        // Act
        await _service.LogAuthenticationAsync(
            userId,
            AuthenticationEventType.LoginFailed,
            "10.0.0.1",
            "TestAgent",
            additionalData);

        // Assert
        var log = await _context.AuthenticationAuditLogs.FirstOrDefaultAsync();
        log!.AdditionalData.Should().Contain("Invalid password");
        log.AdditionalData.Should().Contain("3");
    }

    [Fact]
    public async Task GetUserAuthenticationHistoryAsync_Should_Return_User_Logs()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        // Create logs for target user
        await _service.LogAuthenticationAsync(userId, AuthenticationEventType.LoginSuccess, "1.1.1.1", "Agent1");
        await _service.LogAuthenticationAsync(userId, AuthenticationEventType.LoginFailed, "1.1.1.1", "Agent1");
        await _service.LogAuthenticationAsync(userId, AuthenticationEventType.Logout, "1.1.1.1", "Agent1");

        // Create log for different user
        await _service.LogAuthenticationAsync(otherUserId, AuthenticationEventType.LoginSuccess, "2.2.2.2", "Agent2");

        // Act
        var history = await _service.GetUserAuthenticationHistoryAsync(userId, 10);

        // Assert
        history.Should().HaveCount(3);
        history.All(h => h.UserId == userId).Should().BeTrue();
        history.Should().BeInDescendingOrder(h => h.Timestamp);
    }

    [Fact]
    public async Task GetRecentFailedAttemptsAsync_Should_Return_Failed_Attempts_Within_Window()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var ipAddress = "192.168.1.1";

        // Create failed attempts
        await _service.LogAuthenticationAsync(userId, AuthenticationEventType.LoginFailed, ipAddress, "Agent");
        await Task.Delay(100);
        await _service.LogAuthenticationAsync(userId, AuthenticationEventType.LoginFailed, ipAddress, "Agent");

        // Create old failed attempt (simulate old entry)
        var oldLog = new AuthenticationAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = AuthenticationEventType.LoginFailed,
            IpAddress = ipAddress,
            UserAgent = "Agent",
            Timestamp = DateTime.UtcNow.AddMinutes(-20)
        };
        _context.AuthenticationAuditLogs.Add(oldLog);
        await _context.SaveChangesAsync();

        // Act
        var count = await _service.GetRecentFailedAttemptsAsync(userId, ipAddress, TimeSpan.FromMinutes(15));

        // Assert
        count.Should().Be(2); // Only recent attempts within 15 minutes
    }

    [Fact]
    public async Task GetSuspiciousActivityAsync_Should_Identify_Multiple_Failed_Attempts()
    {
        // Arrange
        var ipAddress = "10.0.0.1";

        // Create multiple failed attempts from same IP
        for (int i = 0; i < 6; i++)
        {
            var userId = Guid.NewGuid().ToString();
            await _service.LogAuthenticationAsync(userId, AuthenticationEventType.LoginFailed, ipAddress, "Agent");
        }

        // Act
        var suspicious = await _service.GetSuspiciousActivityAsync( TimeSpan.FromHours(1));

        // Assert
        suspicious.Should().NotBeEmpty();
        var activity = suspicious.First();
        activity.IpAddress.Should().Be(ipAddress);
        activity.FailedAttempts.Should().Be(6);
    }

    [Fact]
    public async Task LogPasswordChangeAsync_Should_Create_Audit_Entry()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        await _service.LogPasswordChangeAsync(userId, "192.168.1.1", "TestAgent");

        // Assert
        var log = await _context.AuthenticationAuditLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.EventType.Should().Be(AuthenticationEventType.PasswordChanged);
        log.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task LogAccountLockoutAsync_Should_Create_Audit_Entry_With_Duration()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var lockoutDuration = TimeSpan.FromMinutes(30);

        // Act
        await _service.LogAccountLockoutAsync(userId, lockoutDuration, "192.168.1.1", "TestAgent");

        // Assert
        var log = await _context.AuthenticationAuditLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.EventType.Should().Be(AuthenticationEventType.AccountLocked);
        log.AdditionalData.Should().Contain("30");
    }

    [Fact]
    public async Task CleanupOldLogsAsync_Should_Remove_Old_Entries()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Create old logs
        for (int i = 0; i < 5; i++)
        {
            var oldLog = new AuthenticationAuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventType = AuthenticationEventType.LoginSuccess,
                IpAddress = "1.1.1.1",
                UserAgent = "Agent",
                Timestamp = DateTime.UtcNow.AddDays(-100) // 100 days old
            };
            _context.AuthenticationAuditLogs.Add(oldLog);
        }

        // Create recent log
        await _service.LogAuthenticationAsync(userId, AuthenticationEventType.LoginSuccess, "1.1.1.1", "Agent");

        // Act
        var deletedCount = await _service.CleanupOldLogsAsync(90); // Keep 90 days

        // Assert
        deletedCount.Should().Be(5);
        var remainingLogs = await _context.AuthenticationAuditLogs.CountAsync();
        remainingLogs.Should().Be(1);
    }
}