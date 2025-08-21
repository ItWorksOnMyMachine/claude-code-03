using System;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Data.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthService.Tests.Data;

public class EntityConfigurationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AuthDbContext _context;

    public EntityConfigurationTests()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<AuthDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AuthDbContext>();
    }

    [Fact]
    public void AppUser_HasCorrectConfiguration()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(AppUser));

        // Assert
        entityType.Should().NotBeNull();
        
        // Check primary key
        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().HaveCount(1);
        primaryKey.Properties.First().Name.Should().Be("Id");
        
        // Check required properties
        var userNameProperty = entityType.FindProperty("UserName");
        userNameProperty.Should().NotBeNull();
        
        var emailProperty = entityType.FindProperty("Email");
        emailProperty.Should().NotBeNull();
    }

    [Fact]
    public void AppRole_HasCorrectConfiguration()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(AppRole));

        // Assert
        entityType.Should().NotBeNull();
        
        // Check primary key
        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().HaveCount(1);
        primaryKey.Properties.First().Name.Should().Be("Id");
        
        // Check required properties
        var nameProperty = entityType.FindProperty("Name");
        nameProperty.Should().NotBeNull();
        
        var normalizedNameProperty = entityType.FindProperty("NormalizedName");
        normalizedNameProperty.Should().NotBeNull();
    }

    [Fact]
    public void AuthenticationAuditLog_HasCorrectConfiguration()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(AuthenticationAuditLog));

        // Assert
        entityType.Should().NotBeNull();
        
        // Check primary key
        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().HaveCount(1);
        primaryKey.Properties.First().Name.Should().Be("Id");
        
        // Check required properties
        var userIdProperty = entityType.FindProperty("UserId");
        userIdProperty.Should().NotBeNull();
        userIdProperty!.IsNullable.Should().BeFalse();
        
        var timestampProperty = entityType.FindProperty("Timestamp");
        timestampProperty.Should().NotBeNull();
        timestampProperty!.IsNullable.Should().BeFalse();
        
        var eventTypeProperty = entityType.FindProperty("EventType");
        eventTypeProperty.Should().NotBeNull();
        eventTypeProperty!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void PasswordHistory_HasCorrectConfiguration()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(PasswordHistory));

        // Assert
        entityType.Should().NotBeNull();
        
        // Check primary key
        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().HaveCount(1);
        primaryKey.Properties.First().Name.Should().Be("Id");
        
        // Check required properties
        var userIdProperty = entityType.FindProperty("UserId");
        userIdProperty.Should().NotBeNull();
        userIdProperty!.IsNullable.Should().BeFalse();
        
        var passwordHashProperty = entityType.FindProperty("PasswordHash");
        passwordHashProperty.Should().NotBeNull();
        passwordHashProperty!.IsNullable.Should().BeFalse();
        
        var createdAtProperty = entityType.FindProperty("CreatedAt");
        createdAtProperty.Should().NotBeNull();
        createdAtProperty!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task AppUser_CanBeCreatedWithRequiredProperties()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@identity.local",
            NormalizedEmail = "TEST@IDENTITY.LOCAL",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        var savedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.UserName.Should().Be("testuser");
        savedUser.Email.Should().Be("test@identity.local");
        savedUser.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AppRole_CanBeCreatedWithRequiredProperties()
    {
        // Arrange
        var role = new AppRole
        {
            Id = Guid.NewGuid().ToString(),
            Name = "TestRole",
            NormalizedName = "TESTROLE",
            Description = "Test role description",
            IsSystemRole = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        
        var savedRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == role.Id);

        // Assert
        savedRole.Should().NotBeNull();
        savedRole!.Name.Should().Be("TestRole");
        savedRole.Description.Should().Be("Test role description");
        savedRole.IsSystemRole.Should().BeFalse();
    }

    [Fact]
    public void AppUser_HasAuditProperties()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(AppUser));

        // Assert
        entityType.Should().NotBeNull();
        
        var createdAtProperty = entityType!.FindProperty("CreatedAt");
        createdAtProperty.Should().NotBeNull();
        createdAtProperty!.IsNullable.Should().BeFalse();
        
        var updatedAtProperty = entityType.FindProperty("UpdatedAt");
        updatedAtProperty.Should().NotBeNull();
        updatedAtProperty!.IsNullable.Should().BeTrue();
        
        var lastLoginAtProperty = entityType.FindProperty("LastLoginAt");
        lastLoginAtProperty.Should().NotBeNull();
        lastLoginAtProperty!.IsNullable.Should().BeTrue();
        
        var lastPasswordChangeAtProperty = entityType.FindProperty("LastPasswordChangeAt");
        lastPasswordChangeAtProperty.Should().NotBeNull();
        lastPasswordChangeAtProperty!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void AppUser_HasSecurityProperties()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(AppUser));

        // Assert
        entityType.Should().NotBeNull();
        
        var properties = new[]
        {
            "IsActive",
            "FailedPasswordAttempts",
            "ConsecutiveLockouts",
            "MustChangePassword",
            "PasswordExpiresAt",
            "DeactivatedAt",
            "DeactivationReason"
        };

        foreach (var propertyName in properties)
        {
            var property = entityType!.FindProperty(propertyName);
            property.Should().NotBeNull($"Property {propertyName} should exist");
        }
    }

    [Fact]
    public void AppRole_HasSystemRoleFlag()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(AppRole));

        // Assert
        entityType.Should().NotBeNull();
        
        var isSystemRoleProperty = entityType!.FindProperty("IsSystemRole");
        isSystemRoleProperty.Should().NotBeNull();
        isSystemRoleProperty!.IsNullable.Should().BeFalse();
        
        var descriptionProperty = entityType.FindProperty("Description");
        descriptionProperty.Should().NotBeNull();
        descriptionProperty!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticationAuditLog_CanTrackUserActivity()
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
            Timestamp = DateTime.UtcNow
        };

        // Act
        _context.AuthenticationAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        var savedLog = await _context.AuthenticationAuditLogs
            .FirstOrDefaultAsync(l => l.Id == auditLog.Id);

        // Assert
        savedLog.Should().NotBeNull();
        savedLog!.UserId.Should().Be(user.Id);
        savedLog.EventType.Should().Be(AuthenticationEventType.LoginSuccess);
        savedLog.Success.Should().BeTrue();
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}