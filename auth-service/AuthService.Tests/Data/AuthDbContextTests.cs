using System;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Data.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthService.Tests.Data;

public class AuthDbContextTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AuthDbContext _context;

    public AuthDbContextTests()
    {
        var services = new ServiceCollection();
        
        // Configure in-memory database for testing
        services.AddDbContext<AuthDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AuthDbContext>();
    }

    [Fact]
    public void DbContext_CanBeCreated()
    {
        // Assert
        _context.Should().NotBeNull();
        _context.Should().BeOfType<AuthDbContext>();
    }

    [Fact]
    public void DbContext_HasRequiredDbSets()
    {
        // Assert
        _context.Users.Should().NotBeNull();
        _context.Roles.Should().NotBeNull();
        _context.UserClaims.Should().NotBeNull();
        _context.UserRoles.Should().NotBeNull();
        _context.UserLogins.Should().NotBeNull();
        _context.RoleClaims.Should().NotBeNull();
        _context.UserTokens.Should().NotBeNull();
        _context.AuthenticationAuditLogs.Should().NotBeNull();
        _context.PasswordHistories.Should().NotBeNull();
    }

    [Fact]
    public async Task DbContext_CanAddAndRetrieveAppUser()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "test@identity.local",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var retrievedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.UserName.Should().Be("testuser");
        retrievedUser.Email.Should().Be("test@identity.local");
        retrievedUser.FirstName.Should().Be("Test");
        retrievedUser.LastName.Should().Be("User");
        retrievedUser.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DbContext_CanAddAndRetrieveAppRole()
    {
        // Arrange
        var role = new AppRole
        {
            Id = Guid.NewGuid().ToString(),
            Name = "TestRole",
            NormalizedName = "TESTROLE",
            Description = "Test role for identity provider",
            IsSystemRole = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var retrievedRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == role.Id);

        // Assert
        retrievedRole.Should().NotBeNull();
        retrievedRole!.Name.Should().Be("TestRole");
        retrievedRole.NormalizedName.Should().Be("TESTROLE");
        retrievedRole.Description.Should().Be("Test role for identity provider");
        retrievedRole.IsSystemRole.Should().BeFalse();
    }

    [Fact]
    public async Task DbContext_CanCreateMultipleUsers()
    {
        // Arrange
        var user1 = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "user1",
            Email = "user1@identity.local",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "user2",
            Email = "user2@identity.local",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var allUsers = await _context.Users.ToListAsync();

        // Assert
        allUsers.Should().HaveCount(2);
        allUsers.Should().Contain(u => u.UserName == "user1");
        allUsers.Should().Contain(u => u.UserName == "user2");
    }

    [Fact]
    public void DbContext_ConfiguresIdentityTablesCorrectly()
    {
        // Assert
        var model = _context.Model;
        
        // Check for Identity tables
        model.FindEntityType(typeof(AppUser)).Should().NotBeNull();
        model.FindEntityType(typeof(AppRole)).Should().NotBeNull();
        model.FindEntityType(typeof(IdentityUserClaim<string>)).Should().NotBeNull();
        model.FindEntityType(typeof(IdentityUserRole<string>)).Should().NotBeNull();
        model.FindEntityType(typeof(IdentityUserLogin<string>)).Should().NotBeNull();
        model.FindEntityType(typeof(IdentityRoleClaim<string>)).Should().NotBeNull();
        model.FindEntityType(typeof(IdentityUserToken<string>)).Should().NotBeNull();
        
        // Check for custom audit tables
        model.FindEntityType(typeof(AuthenticationAuditLog)).Should().NotBeNull();
        model.FindEntityType(typeof(PasswordHistory)).Should().NotBeNull();
    }

    [Fact]
    public async Task DbContext_CanTrackAuthenticationAuditLog()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "audituser",
            Email = "audit@identity.local",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var auditLog = new AuthenticationAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            EventType = AuthenticationEventType.LoginSuccess,
            Success = true,
            IpAddress = "127.0.0.1",
            UserAgent = "Test Browser",
            Timestamp = DateTime.UtcNow
        };

        // Act
        _context.AuthenticationAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        var retrievedLog = await _context.AuthenticationAuditLogs
            .FirstOrDefaultAsync(l => l.Id == auditLog.Id);

        // Assert
        retrievedLog.Should().NotBeNull();
        retrievedLog!.UserId.Should().Be(user.Id);
        retrievedLog.EventType.Should().Be(AuthenticationEventType.LoginSuccess);
        retrievedLog.Success.Should().BeTrue();
        retrievedLog.IpAddress.Should().Be("127.0.0.1");
    }

    [Fact]
    public async Task DbContext_TracksAuditFieldsCorrectly()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "audituser",
            Email = "audit@identity.local",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        // Modify the user
        user.FirstName = "Modified";
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var retrievedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        retrievedUser.UpdatedAt.Should().NotBeNull();
        retrievedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}