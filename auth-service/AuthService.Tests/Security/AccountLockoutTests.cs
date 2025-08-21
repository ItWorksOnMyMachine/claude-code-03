using System;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Identity;
using AuthService.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AuthService.Tests.Security;

public class AccountLockoutTests
{
    private readonly AuthDbContext _context;
    private readonly Mock<ILogger<AccountLockoutService>> _logger;
    private readonly Mock<IOptions<AuthService.Security.LockoutOptions>> _lockoutOptions;
    private readonly AccountLockoutService _service;
    private readonly AppUser _testUser;

    public AccountLockoutTests()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AuthDbContext(options);

        _logger = new Mock<ILogger<AccountLockoutService>>();
        _lockoutOptions = new Mock<IOptions<AuthService.Security.LockoutOptions>>();
        _lockoutOptions.Setup(x => x.Value).Returns(new AuthService.Security.LockoutOptions
        {
            MaxFailedAttempts = 5,
            InitialLockoutMinutes = 5,
            LockoutMultiplier = 2,
            MaxLockoutMinutes = 1440 // 24 hours
        });

        _service = new AccountLockoutService(_context, _logger.Object, _lockoutOptions.Object);

        // Create test user
        _testUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "test@example.com",
            AccessFailedCount = 0,
            LockoutEnd = null
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task RecordFailedAttempt_Should_Increment_Failed_Count()
    {
        // Act
        await _service.RecordFailedAttemptAsync(_testUser.Id);

        // Assert
        var user = await _context.Users.FindAsync(_testUser.Id);
        user!.AccessFailedCount.Should().Be(1);
        user.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task RecordFailedAttempt_Should_Lock_Account_After_Max_Attempts()
    {
        // Arrange
        _testUser.AccessFailedCount = 4;
        await _context.SaveChangesAsync();

        // Act
        await _service.RecordFailedAttemptAsync(_testUser.Id);

        // Assert
        var user = await _context.Users.FindAsync(_testUser.Id);
        user!.AccessFailedCount.Should().Be(5);
        user.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RecordFailedAttempt_Should_Apply_Progressive_Lockout()
    {
        // Arrange
        _testUser.AccessFailedCount = 5;
        _testUser.ConsecutiveLockouts = 1;
        await _context.SaveChangesAsync();

        // Act
        await _service.RecordFailedAttemptAsync(_testUser.Id);

        // Assert
        var user = await _context.Users.FindAsync(_testUser.Id);
        user!.LockoutEnd.Should().NotBeNull();
        // Second lockout should be 10 minutes (5 * 2)
        user.LockoutEnd.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(5));
        user.ConsecutiveLockouts.Should().Be(2);
    }

    [Fact]
    public async Task RecordFailedAttempt_Should_Not_Exceed_Max_Lockout_Duration()
    {
        // Arrange
        _testUser.AccessFailedCount = 5;
        _testUser.ConsecutiveLockouts = 10; // Very high to test max limit
        await _context.SaveChangesAsync();

        // Act
        await _service.RecordFailedAttemptAsync(_testUser.Id);

        // Assert
        var user = await _context.Users.FindAsync(_testUser.Id);
        user!.LockoutEnd.Should().NotBeNull();
        // Should not exceed 24 hours (1440 minutes)
        user.LockoutEnd.Should().BeBefore(DateTimeOffset.UtcNow.AddMinutes(1441));
    }

    [Fact]
    public async Task ResetFailedAttempts_Should_Clear_Failed_Count()
    {
        // Arrange
        _testUser.AccessFailedCount = 3;
        await _context.SaveChangesAsync();

        // Act
        await _service.ResetFailedAttemptsAsync(_testUser.Id);

        // Assert
        var user = await _context.Users.FindAsync(_testUser.Id);
        user!.AccessFailedCount.Should().Be(0);
    }

    [Fact]
    public async Task IsLockedOut_Should_Return_True_When_Locked()
    {
        // Arrange
        _testUser.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsLockedOutAsync(_testUser.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsLockedOut_Should_Return_False_When_Lockout_Expired()
    {
        // Arrange
        _testUser.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsLockedOutAsync(_testUser.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetTimeUntilUnlock_Should_Return_Remaining_Time()
    {
        // Arrange
        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
        _testUser.LockoutEnd = lockoutEnd;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTimeUntilUnlockAsync(_testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Value.TotalMinutes.Should().BeApproximately(15, 1);
    }
}