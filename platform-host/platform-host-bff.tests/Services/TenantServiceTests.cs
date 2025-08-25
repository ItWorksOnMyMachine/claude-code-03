using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PlatformBff.Data;
using PlatformBff.Data.Entities;
using PlatformBff.Repositories;
using TenantInfo = PlatformBff.Models.Tenant.TenantInfo;
using TenantContext = PlatformBff.Models.Tenant.TenantContext;
using PlatformBff.Services;
using PlatformBff.Services.Tenant;
using Xunit;

namespace PlatformBff.Tests.Services;

public class TenantServiceTests : IDisposable
{
    private readonly PlatformDbContext _context;
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<ITenantUserRepository> _tenantUserRepositoryMock;
    private readonly Mock<ILogger<TenantService>> _loggerMock;
    private readonly TenantService _tenantService;
    
    // Test data
    private readonly Guid _testTenantId = Guid.NewGuid();
    private readonly Guid _platformTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly string _testUserId = "test-user-123";
    private readonly string _adminUserId = "admin-user-456";

    public TenantServiceTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PlatformDbContext(options);
        
        // Create mocks
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _tenantUserRepositoryMock = new Mock<ITenantUserRepository>();
        _loggerMock = new Mock<ILogger<TenantService>>();
        
        // Initialize service
        _tenantService = new TenantService(
            _context,
            _tenantRepositoryMock.Object,
            _tenantUserRepositoryMock.Object,
            _loggerMock.Object
        );
        
        // Seed test data
        SeedTestData();
    }
    
    private void SeedTestData()
    {
        // Create test tenant
        var testTenant = new Tenant
        {
            Id = _testTenantId,
            Name = "test-tenant",
            Slug = "test-tenant",
            DisplayName = "Test Tenant",
            IsActive = true,
            IsPlatformTenant = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        // Create platform tenant
        var platformTenant = new Tenant
        {
            Id = _platformTenantId,
            Name = "platform-admin",
            Slug = "platform-admin",
            DisplayName = "Platform Administration",
            IsActive = true,
            IsPlatformTenant = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        _context.Tenants.AddRange(testTenant, platformTenant);
        
        // Create roles
        var userRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _testTenantId,
            Name = "User",
            DisplayName = "User",
            IsSystemRole = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _platformTenantId,
            Name = "Admin",
            DisplayName = "Administrator",
            IsSystemRole = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        _context.Roles.AddRange(userRole, adminRole);
        
        // Create tenant users
        var testTenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TenantId = _testTenantId,
            IsActive = true,
            JoinedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        var adminTenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserId = _adminUserId,
            TenantId = _platformTenantId,
            IsActive = true,
            JoinedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        _context.TenantUsers.AddRange(testTenantUser, adminTenantUser);
        
        // Create user roles
        var testUserRole = new UserRole
        {
            Id = Guid.NewGuid(),
            TenantUserId = testTenantUser.Id,
            RoleId = userRole.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        var adminUserRole = new UserRole
        {
            Id = Guid.NewGuid(),
            TenantUserId = adminTenantUser.Id,
            RoleId = adminRole.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        
        _context.UserRoles.AddRange(testUserRole, adminUserRole);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAvailableTenantsAsync_Should_Return_User_Tenants()
    {
        // Act
        var result = await _tenantService.GetAvailableTenantsAsync(_testUserId);
        
        // Assert
        Assert.NotNull(result);
        var tenants = result.ToList();
        Assert.Single(tenants);
        Assert.Equal(_testTenantId, tenants[0].Id);
        Assert.Equal("Test Tenant", tenants[0].Name);
        Assert.False(tenants[0].IsPlatformTenant);
    }
    
    [Fact]
    public async Task GetAvailableTenantsAsync_Should_Return_Multiple_Tenants_For_User_In_Multiple_Tenants()
    {
        // Arrange - Add user to platform tenant as well
        var additionalTenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TenantId = _platformTenantId,
            IsActive = true,
            JoinedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _context.TenantUsers.Add(additionalTenantUser);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _tenantService.GetAvailableTenantsAsync(_testUserId);
        
        // Assert
        Assert.NotNull(result);
        var tenants = result.ToList();
        Assert.Equal(2, tenants.Count);
        Assert.Contains(tenants, t => t.Id == _testTenantId);
        Assert.Contains(tenants, t => t.Id == _platformTenantId);
    }
    
    [Fact]
    public async Task GetAvailableTenantsAsync_Should_Not_Return_Inactive_Tenants()
    {
        // Arrange - Deactivate the test tenant
        var tenant = await _context.Tenants.FindAsync(_testTenantId);
        tenant!.IsActive = false;
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _tenantService.GetAvailableTenantsAsync(_testUserId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task GetTenantAsync_Should_Return_Tenant_When_User_Has_Access()
    {
        // Act
        var result = await _tenantService.GetTenantAsync(_testUserId, _testTenantId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testTenantId, result.Id);
        Assert.Equal("Test Tenant", result.Name);
        Assert.Equal("User", result.UserRole);
    }
    
    [Fact]
    public async Task GetTenantAsync_Should_Return_Null_When_User_Has_No_Access()
    {
        // Act
        var result = await _tenantService.GetTenantAsync(_testUserId, _platformTenantId);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task SelectTenantAsync_Should_Return_TenantContext_When_User_Has_Access()
    {
        // Act
        var result = await _tenantService.SelectTenantAsync(_testUserId, _testTenantId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testTenantId, result.TenantId);
        Assert.Equal("Test Tenant", result.TenantName);
        Assert.False(result.IsPlatformTenant);
        Assert.Contains("User", result.UserRoles);
        Assert.False(result.IsPlatformAdmin);
    }
    
    [Fact]
    public async Task SelectTenantAsync_Should_Throw_When_User_Has_No_Access()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _tenantService.SelectTenantAsync(_testUserId, _platformTenantId)
        );
    }
    
    [Fact]
    public async Task ValidateAccessAsync_Should_Return_True_When_User_Has_Access()
    {
        // Act
        var result = await _tenantService.ValidateAccessAsync(_testUserId, _testTenantId);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public async Task ValidateAccessAsync_Should_Return_False_When_User_Has_No_Access()
    {
        // Act
        var result = await _tenantService.ValidateAccessAsync(_testUserId, _platformTenantId);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task ValidateAccessAsync_Should_Return_False_When_User_Not_Found()
    {
        // Act
        var result = await _tenantService.ValidateAccessAsync("non-existent-user", _testTenantId);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task IsPlatformAdminAsync_Should_Return_True_For_Platform_Admin()
    {
        // Act
        var result = await _tenantService.IsPlatformAdminAsync(_adminUserId);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public async Task IsPlatformAdminAsync_Should_Return_False_For_Regular_User()
    {
        // Act
        var result = await _tenantService.IsPlatformAdminAsync(_testUserId);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task IsPlatformAdminAsync_Should_Return_False_For_Non_Admin_In_Platform_Tenant()
    {
        // Arrange - Add test user to platform tenant but without admin role
        var platformTenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TenantId = _platformTenantId,
            IsActive = true,
            JoinedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _context.TenantUsers.Add(platformTenantUser);
        
        // Add a non-admin role
        var userRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _platformTenantId,
            Name = "Support",
            DisplayName = "Support",
            IsSystemRole = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _context.Roles.Add(userRole);
        
        var userRoleAssignment = new UserRole
        {
            Id = Guid.NewGuid(),
            TenantUserId = platformTenantUser.Id,
            RoleId = userRole.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _context.UserRoles.Add(userRoleAssignment);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _tenantService.IsPlatformAdminAsync(_testUserId);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void GetPlatformTenantId_Should_Return_Correct_Id()
    {
        // Act
        var result = _tenantService.GetPlatformTenantId();
        
        // Assert
        Assert.Equal(_platformTenantId, result);
    }
    
    public void Dispose()
    {
        _context?.Dispose();
    }
}