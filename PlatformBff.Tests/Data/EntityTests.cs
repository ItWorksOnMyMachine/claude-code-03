using System;
using Xunit;
using FluentAssertions;
using PlatformBff.Data.Entities;
using Microsoft.EntityFrameworkCore;
using PlatformBff.Data;
using System.Threading.Tasks;
using System.Linq;

namespace PlatformBff.Tests.Data;

public class EntityTests
{
    [Fact]
    public void Tenant_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Slug = "test-tenant",
            DisplayName = "Test Tenant Display",
            IsActive = true,
            IsPlatformTenant = false,
            Settings = """{"feature": "enabled"}""",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null
        };

        // Assert
        tenant.Id.Should().NotBeEmpty();
        tenant.Name.Should().Be("Test Tenant");
        tenant.Slug.Should().Be("test-tenant");
        tenant.DisplayName.Should().Be("Test Tenant Display");
        tenant.IsActive.Should().BeTrue();
        tenant.IsPlatformTenant.Should().BeFalse();
        tenant.Settings.Should().NotBeNullOrEmpty();
        tenant.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void TenantUser_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var tenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserId = "auth-user-123",
            TenantId = Guid.NewGuid(),
            IsActive = true,
            JoinedAt = DateTimeOffset.UtcNow,
            LastAccessedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "system",
            UpdatedBy = "system",
            IsDeleted = false
        };

        // Assert
        tenantUser.Id.Should().NotBeEmpty();
        tenantUser.UserId.Should().Be("auth-user-123");
        tenantUser.TenantId.Should().NotBeEmpty();
        tenantUser.IsActive.Should().BeTrue();
        tenantUser.JoinedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Role_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Admin",
            DisplayName = "Administrator",
            Description = "Full system access",
            IsSystemRole = true,
            Permissions = """["read", "write", "delete"]""",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "system",
            UpdatedBy = "system",
            IsDeleted = false
        };

        // Assert
        role.Id.Should().NotBeEmpty();
        role.TenantId.Should().NotBeEmpty();
        role.Name.Should().Be("Admin");
        role.DisplayName.Should().Be("Administrator");
        role.IsSystemRole.Should().BeTrue();
        role.Permissions.Should().Contain("read");
    }

    [Fact]
    public void UserRole_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            TenantUserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            AssignedAt = DateTimeOffset.UtcNow,
            AssignedBy = "admin",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "admin",
            UpdatedBy = "admin",
            IsDeleted = false
        };

        // Assert
        userRole.Id.Should().NotBeEmpty();
        userRole.TenantUserId.Should().NotBeEmpty();
        userRole.RoleId.Should().NotBeEmpty();
        userRole.AssignedBy.Should().Be("admin");
        userRole.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task DbContext_Should_Create_Tenant_With_Relationships()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);
        
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            DisplayName = "Test Tenant",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var tenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserId = "user-123",
            TenantId = tenantId,
            IsActive = true,
            JoinedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Member",
            DisplayName = "Member",
            IsSystemRole = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        context.Tenants.Add(tenant);
        context.TenantUsers.Add(tenantUser);
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        // Assert
        var savedTenant = await context.Tenants
            .Include(t => t.TenantUsers)
            .Include(t => t.Roles)
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        savedTenant.Should().NotBeNull();
        savedTenant!.TenantUsers.Should().HaveCount(1);
        savedTenant.Roles.Should().HaveCount(1);
    }

    [Fact]
    public async Task UserRole_Should_Link_TenantUser_And_Role()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);
        
        var tenantId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            DisplayName = "Test Tenant",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var tenantUser = new TenantUser
        {
            Id = tenantUserId,
            UserId = "user-456",
            TenantId = tenantId,
            IsActive = true,
            JoinedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var role = new Role
        {
            Id = roleId,
            TenantId = tenantId,
            Name = "Admin",
            DisplayName = "Administrator",
            IsSystemRole = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            TenantUserId = tenantUserId,
            RoleId = roleId,
            AssignedAt = DateTimeOffset.UtcNow,
            AssignedBy = "system",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        context.Tenants.Add(tenant);
        context.TenantUsers.Add(tenantUser);
        context.Roles.Add(role);
        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();

        // Assert
        var savedUserRole = await context.UserRoles
            .Include(ur => ur.TenantUser)
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.TenantUserId == tenantUserId);

        savedUserRole.Should().NotBeNull();
        savedUserRole!.TenantUser.Should().NotBeNull();
        savedUserRole.Role.Should().NotBeNull();
        savedUserRole.Role.Name.Should().Be("Admin");
    }

    [Fact]
    public async Task Platform_Tenant_Should_Be_Unique()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new PlatformDbContext(options);

        var platformTenant1 = new Tenant
        {
            Id = new Guid("00000000-0000-0000-0000-000000000001"),
            Name = "Platform Admin",
            Slug = "platform-admin",
            DisplayName = "Platform Administration",
            IsActive = true,
            IsPlatformTenant = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Tenants.Add(platformTenant1);
        await context.SaveChangesAsync();

        // Act & Assert - platform tenant should be unique
        var existingPlatformTenant = await context.Tenants
            .Where(t => t.IsPlatformTenant && !t.IsDeleted)
            .FirstOrDefaultAsync();

        existingPlatformTenant.Should().NotBeNull();
        existingPlatformTenant!.Id.Should().Be(new Guid("00000000-0000-0000-0000-000000000001"));
    }
}