using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlatformBff.Data;
using PlatformBff.Data.Entities;

namespace PlatformBff.Tests.Repositories;

public class SimpleTenantFilterTests
{
    [Fact]
    public async Task DbContext_Should_Filter_Roles_By_Tenant()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Seed data
        using (var seedContext = new PlatformDbContext(options))
        {
            seedContext.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenant1Id,
                Name = "Admin1",
                DisplayName = "Admin1",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            
            seedContext.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenant2Id,
                Name = "Admin2",
                DisplayName = "Admin2",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            
            await seedContext.SaveChangesAsync();
        }

        // Act - Query with tenant1 context
        using (var tenant1Context = new PlatformDbContext(options, tenant1Id))
        {
            var tenant1Roles = await tenant1Context.Roles.ToListAsync();
            
            // Assert
            tenant1Roles.Should().HaveCount(1);
            tenant1Roles.First().Name.Should().Be("Admin1");
            tenant1Roles.First().TenantId.Should().Be(tenant1Id);
        }

        // Act - Query with tenant2 context
        using (var tenant2Context = new PlatformDbContext(options, tenant2Id))
        {
            var tenant2Roles = await tenant2Context.Roles.ToListAsync();
            
            // Assert
            tenant2Roles.Should().HaveCount(1);
            tenant2Roles.First().Name.Should().Be("Admin2");
            tenant2Roles.First().TenantId.Should().Be(tenant2Id);
        }

        // Act - Query without tenant context (should see all non-deleted)
        using (var noTenantContext = new PlatformDbContext(options))
        {
            var allRoles = await noTenantContext.Roles.ToListAsync();
            
            // Assert - without tenant context, should see all
            allRoles.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task Repository_Should_Respect_Soft_Delete()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Seed data
        using (var seedContext = new PlatformDbContext(options))
        {
            seedContext.Roles.Add(new Role
            {
                Id = roleId,
                TenantId = tenantId,
                Name = "ToDelete",
                DisplayName = "ToDelete",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false
            });
            
            await seedContext.SaveChangesAsync();
        }

        // Act - Soft delete the role
        using (var deleteContext = new PlatformDbContext(options, tenantId))
        {
            var role = await deleteContext.Roles.FindAsync(roleId);
            role.Should().NotBeNull();
            
            role!.IsDeleted = true;
            role.DeletedAt = DateTimeOffset.UtcNow;
            await deleteContext.SaveChangesAsync();
        }

        // Assert - Role should not be visible in normal queries
        using (var queryContext = new PlatformDbContext(options, tenantId))
        {
            var visibleRoles = await queryContext.Roles.ToListAsync();
            visibleRoles.Should().BeEmpty();
            
            // But should be visible when ignoring filters
            var allRoles = await queryContext.Roles.IgnoreQueryFilters().ToListAsync();
            allRoles.Should().HaveCount(1);
            allRoles.First().IsDeleted.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Tenants_Should_Not_Be_Filtered_By_TenantId()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Seed data
        using (var seedContext = new PlatformDbContext(options))
        {
            seedContext.Tenants.Add(new Tenant
            {
                Id = tenant1Id,
                Name = "Tenant1",
                Slug = "tenant1",
                DisplayName = "Tenant 1",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            
            seedContext.Tenants.Add(new Tenant
            {
                Id = tenant2Id,
                Name = "Tenant2",
                Slug = "tenant2",
                DisplayName = "Tenant 2",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            
            await seedContext.SaveChangesAsync();
        }

        // Act - Tenants table should not be filtered by tenant
        using (var context = new PlatformDbContext(options, tenant1Id))
        {
            var tenants = await context.Tenants.ToListAsync();
            
            // Assert - Should see all tenants regardless of context
            tenants.Should().HaveCount(2);
        }
    }
}