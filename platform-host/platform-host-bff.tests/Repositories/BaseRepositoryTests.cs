using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlatformBff.Data;
using PlatformBff.Data.Entities;
using PlatformBff.Repositories;
using PlatformBff.Services;
using Moq;

namespace PlatformBff.Tests.Repositories;

public class BaseRepositoryTests : IDisposable
{
    private readonly PlatformDbContext _context;
    private readonly Guid _tenant1Id = Guid.NewGuid();
    private readonly Guid _tenant2Id = Guid.NewGuid();
    private readonly Guid _platformTenantId = new Guid("00000000-0000-0000-0000-000000000001");

    public BaseRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PlatformDbContext(options);
        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        // Create test tenants
        var tenant1 = new Tenant
        {
            Id = _tenant1Id,
            Name = "Tenant 1",
            Slug = "tenant-1",
            DisplayName = "Tenant One",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var tenant2 = new Tenant
        {
            Id = _tenant2Id,
            Name = "Tenant 2",
            Slug = "tenant-2",
            DisplayName = "Tenant Two",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var platformTenant = new Tenant
        {
            Id = _platformTenantId,
            Name = "Platform Admin",
            Slug = "platform-admin",
            DisplayName = "Platform Administration",
            IsActive = true,
            IsPlatformTenant = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.Tenants.AddRange(tenant1, tenant2, platformTenant);

        // Create roles for each tenant
        var tenant1AdminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant1Id,
            Name = "Admin",
            DisplayName = "Administrator",
            IsSystemRole = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var tenant2AdminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant2Id,
            Name = "Admin",
            DisplayName = "Administrator",
            IsSystemRole = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.Roles.AddRange(tenant1AdminRole, tenant2AdminRole);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Repository_Should_Filter_By_Current_Tenant()
    {
        // Arrange
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.GetCurrentTenantId()).Returns(_tenant1Id);
        
        // Create a new context with tenant filtering
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        using var tenantScopedContext = new PlatformDbContext(options, _tenant1Id);
        
        // Seed data in the new context
        await SeedTestDataInContext(tenantScopedContext);
        
        var repository = new BaseRepository<Role>(tenantScopedContext, tenantContext.Object);

        // Act
        var roles = await repository.GetAllAsync();

        // Assert
        roles.Should().HaveCount(1);
        roles.First().TenantId.Should().Be(_tenant1Id);
    }
    
    private async Task SeedTestDataInContext(PlatformDbContext context)
    {
        // Create test tenants
        var tenant1 = new Tenant
        {
            Id = _tenant1Id,
            Name = "Tenant 1",
            Slug = "tenant-1",
            DisplayName = "Tenant One",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var tenant2 = new Tenant
        {
            Id = _tenant2Id,
            Name = "Tenant 2",
            Slug = "tenant-2",
            DisplayName = "Tenant Two",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Tenants.AddRange(tenant1, tenant2);

        // Create roles for each tenant
        var tenant1AdminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant1Id,
            Name = "Admin",
            DisplayName = "Administrator",
            IsSystemRole = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var tenant2AdminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant2Id,
            Name = "Admin",
            DisplayName = "Administrator",
            IsSystemRole = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Roles.AddRange(tenant1AdminRole, tenant2AdminRole);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Repository_Should_Return_Different_Data_For_Different_Tenants()
    {
        // Arrange
        var tenantContext1 = new Mock<ITenantContext>();
        tenantContext1.Setup(x => x.GetCurrentTenantId()).Returns(_tenant1Id);
        
        var tenantContext2 = new Mock<ITenantContext>();
        tenantContext2.Setup(x => x.GetCurrentTenantId()).Returns(_tenant2Id);
        
        var repository1 = new BaseRepository<Role>(_context, tenantContext1.Object);
        var repository2 = new BaseRepository<Role>(_context, tenantContext2.Object);

        // Act
        var roles1 = await repository1.GetAllAsync();
        var roles2 = await repository2.GetAllAsync();

        // Assert
        roles1.Should().HaveCount(1);
        roles1.First().TenantId.Should().Be(_tenant1Id);
        
        roles2.Should().HaveCount(1);
        roles2.First().TenantId.Should().Be(_tenant2Id);
    }

    [Fact]
    public async Task Repository_Should_Allow_Admin_To_Ignore_Filters()
    {
        // Arrange
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.GetCurrentTenantId()).Returns(_platformTenantId);
        tenantContext.Setup(x => x.IsPlatformTenant()).Returns(true);
        
        var repository = new BaseRepository<Role>(_context, tenantContext.Object);

        // Act
        var allRoles = await repository.GetAllAsync(ignoreQueryFilters: true);

        // Assert
        allRoles.Should().HaveCount(2); // Should see roles from both tenants
    }

    [Fact]
    public async Task Repository_Should_Create_Entity_With_Current_Tenant()
    {
        // Arrange
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.GetCurrentTenantId()).Returns(_tenant1Id);
        
        var repository = new BaseRepository<Role>(_context, tenantContext.Object);
        
        var newRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant1Id, // Should be set automatically
            Name = "Member",
            DisplayName = "Member",
            IsSystemRole = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var createdRole = await repository.AddAsync(newRole);

        // Assert
        createdRole.TenantId.Should().Be(_tenant1Id);
        var savedRole = await repository.GetByIdAsync(createdRole.Id);
        savedRole.Should().NotBeNull();
        savedRole!.TenantId.Should().Be(_tenant1Id);
    }

    [Fact]
    public async Task Repository_Should_Soft_Delete_Entity()
    {
        // Arrange
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.GetCurrentTenantId()).Returns(_tenant1Id);
        
        var repository = new BaseRepository<Role>(_context, tenantContext.Object);
        
        var roleToDelete = await repository.GetAllAsync();
        var roleId = roleToDelete.First().Id;

        // Act
        await repository.SoftDeleteAsync(roleId);

        // Assert
        var deletedRole = await repository.GetByIdAsync(roleId);
        deletedRole.Should().BeNull(); // Should be filtered out by soft delete

        // Check it still exists in database but is marked as deleted
        var actualRole = await _context.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == roleId);
        
        actualRole.Should().NotBeNull();
        actualRole!.IsDeleted.Should().BeTrue();
        actualRole.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Repository_Should_Not_Return_Soft_Deleted_Entities()
    {
        // Arrange
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.GetCurrentTenantId()).Returns(_tenant1Id);
        
        var repository = new BaseRepository<Role>(_context, tenantContext.Object);
        
        // Soft delete a role
        var roles = await repository.GetAllAsync();
        var roleId = roles.First().Id;
        await repository.SoftDeleteAsync(roleId);

        // Act
        var remainingRoles = await repository.GetAllAsync();

        // Assert
        remainingRoles.Should().BeEmpty();
    }

    [Fact]
    public async Task Repository_Should_Update_Entity_Only_In_Current_Tenant()
    {
        // Arrange
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.GetCurrentTenantId()).Returns(_tenant1Id);
        
        var repository = new BaseRepository<Role>(_context, tenantContext.Object);
        
        var role = (await repository.GetAllAsync()).First();
        
        // Act
        role.DisplayName = "Updated Admin";
        var updated = await repository.UpdateAsync(role);

        // Assert
        updated.DisplayName.Should().Be("Updated Admin");
        updated.TenantId.Should().Be(_tenant1Id);
        updated.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Repository_Should_Prevent_Cross_Tenant_Access()
    {
        // Arrange
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.GetCurrentTenantId()).Returns(_tenant1Id);
        
        var repository = new BaseRepository<Role>(_context, tenantContext.Object);
        
        // Get a role ID from tenant 2
        var tenant2Role = await _context.Roles
            .IgnoreQueryFilters()
            .FirstAsync(r => r.TenantId == _tenant2Id);

        // Act
        var result = await repository.GetByIdAsync(tenant2Role.Id);

        // Assert
        result.Should().BeNull(); // Should not be able to access tenant 2's role
    }

    [Fact]
    public async Task Repository_Should_Support_Pagination()
    {
        // Arrange
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.GetCurrentTenantId()).Returns(_tenant1Id);
        
        var repository = new BaseRepository<Role>(_context, tenantContext.Object);
        
        // Add more roles for pagination test
        for (int i = 0; i < 10; i++)
        {
            await repository.AddAsync(new Role
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant1Id,
                Name = $"Role{i}",
                DisplayName = $"Role {i}",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        // Act
        var page1 = await repository.GetPagedAsync(1, 5);
        var page2 = await repository.GetPagedAsync(2, 5);

        // Assert
        page1.Items.Should().HaveCount(5);
        page1.TotalCount.Should().Be(11); // 1 original + 10 new
        page1.PageNumber.Should().Be(1);
        
        page2.Items.Should().HaveCount(5);
        page2.PageNumber.Should().Be(2);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}