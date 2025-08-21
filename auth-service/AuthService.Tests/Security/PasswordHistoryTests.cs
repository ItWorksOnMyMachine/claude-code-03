using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AuthService.Tests.Security;

public class PasswordHistoryTests
{
    private readonly AuthDbContext _context;
    private readonly Mock<IPasswordHasher<AppUser>> _passwordHasher;
    private readonly Mock<ILogger<PasswordHistoryService>> _logger;
    private readonly Mock<IOptions<PasswordHistoryOptions>> _options;
    private readonly PasswordHistoryService _service;
    private readonly AppUser _testUser;

    public PasswordHistoryTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AuthDbContext(dbOptions);

        _passwordHasher = new Mock<IPasswordHasher<AppUser>>();
        _logger = new Mock<ILogger<PasswordHistoryService>>();
        _options = new Mock<IOptions<PasswordHistoryOptions>>();
        _options.Setup(x => x.Value).Returns(new PasswordHistoryOptions
        {
            HistoryCount = 5,
            MinimumPasswordAge = TimeSpan.FromDays(1),
            MaximumPasswordAge = TimeSpan.FromDays(90)
        });

        _service = new PasswordHistoryService(_context, _passwordHasher.Object, _logger.Object, _options.Object);

        // Create test user
        _testUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "test@example.com",
            PasswordHash = "current_hash"
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task AddPasswordToHistoryAsync_Should_Store_Password_Hash()
    {
        // Arrange
        var passwordHash = "new_password_hash";

        // Act
        await _service.AddPasswordToHistoryAsync(_testUser.Id, passwordHash);

        // Assert
        var history = await _context.PasswordHistories
            .Where(p => p.UserId == _testUser.Id)
            .FirstOrDefaultAsync();

        history.Should().NotBeNull();
        history!.PasswordHash.Should().Be(passwordHash);
        history.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AddPasswordToHistoryAsync_Should_Maintain_History_Limit()
    {
        // Arrange
        // Add 6 passwords to history (more than the limit of 5)
        for (int i = 1; i <= 6; i++)
        {
            await _service.AddPasswordToHistoryAsync(_testUser.Id, $"password_hash_{i}");
        }

        // Act
        var histories = await _context.PasswordHistories
            .Where(p => p.UserId == _testUser.Id)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        // Assert
        histories.Should().HaveCount(5); // Should only keep last 5
        histories[0].PasswordHash.Should().Be("password_hash_2"); // Oldest should be #2
        histories[4].PasswordHash.Should().Be("password_hash_6"); // Newest should be #6
    }

    [Fact]
    public async Task IsPasswordInHistoryAsync_Should_Detect_Previous_Password()
    {
        // Arrange
        var plainPassword = "OldPassword123!";
        var hashedPassword = "hashed_old_password";

        _passwordHasher.Setup(h => h.VerifyHashedPassword(_testUser, hashedPassword, plainPassword))
            .Returns(PasswordVerificationResult.Success);

        await _service.AddPasswordToHistoryAsync(_testUser.Id, hashedPassword);

        // Act
        var result = await _service.IsPasswordInHistoryAsync(_testUser.Id, plainPassword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsPasswordInHistoryAsync_Should_Return_False_For_New_Password()
    {
        // Arrange
        var plainPassword = "NewPassword123!";

        _passwordHasher.Setup(h => h.VerifyHashedPassword(_testUser, It.IsAny<string>(), plainPassword))
            .Returns(PasswordVerificationResult.Failed);

        await _service.AddPasswordToHistoryAsync(_testUser.Id, "some_other_hash");

        // Act
        var result = await _service.IsPasswordInHistoryAsync(_testUser.Id, plainPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPasswordAgeAsync_Should_Return_Time_Since_Last_Change()
    {
        // Arrange
        var passwordHistory = new PasswordHistory
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            PasswordHash = "recent_hash",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        _context.PasswordHistories.Add(passwordHistory);
        await _context.SaveChangesAsync();

        // Act
        var age = await _service.GetPasswordAgeAsync(_testUser.Id);

        // Assert
        age.Should().NotBeNull();
        age!.Value.Days.Should().BeInRange(9, 11);
    }

    [Fact]
    public async Task IsPasswordExpiredAsync_Should_Return_True_When_Expired()
    {
        // Arrange
        var oldPasswordHistory = new PasswordHistory
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            PasswordHash = "old_hash",
            CreatedAt = DateTime.UtcNow.AddDays(-100) // Older than 90 days
        };
        _context.PasswordHistories.Add(oldPasswordHistory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsPasswordExpiredAsync(_testUser.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsPasswordExpiredAsync_Should_Return_False_When_Not_Expired()
    {
        // Arrange
        var recentPasswordHistory = new PasswordHistory
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            PasswordHash = "recent_hash",
            CreatedAt = DateTime.UtcNow.AddDays(-30) // Within 90 days
        };
        _context.PasswordHistories.Add(recentPasswordHistory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsPasswordExpiredAsync(_testUser.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanChangePasswordAsync_Should_Enforce_Minimum_Age()
    {
        // Arrange
        var recentPasswordHistory = new PasswordHistory
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            PasswordHash = "recent_hash",
            CreatedAt = DateTime.UtcNow.AddHours(-12) // Less than 1 day
        };
        _context.PasswordHistories.Add(recentPasswordHistory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanChangePasswordAsync(_testUser.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ClearPasswordHistoryAsync_Should_Remove_All_History()
    {
        // Arrange
        for (int i = 0; i < 3; i++)
        {
            await _service.AddPasswordToHistoryAsync(_testUser.Id, $"hash_{i}");
        }

        // Act
        await _service.ClearPasswordHistoryAsync(_testUser.Id);

        // Assert
        var count = await _context.PasswordHistories
            .Where(p => p.UserId == _testUser.Id)
            .CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetPasswordExpirationDateAsync_Should_Return_Expiration_Date()
    {
        // Arrange
        var passwordHistory = new PasswordHistory
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            PasswordHash = "current_hash",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        _context.PasswordHistories.Add(passwordHistory);
        await _context.SaveChangesAsync();

        // Act
        var expirationDate = await _service.GetPasswordExpirationDateAsync(_testUser.Id);

        // Assert
        expirationDate.Should().NotBeNull();
        expirationDate!.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(60), TimeSpan.FromHours(1));
    }
}