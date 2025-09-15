using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using PlatformShared.Services;
using PlatformShared.Models;
using System.Text;
using Xunit;

namespace CmsBff.Tests.Integration;

public class EntitlementIntegrationTests
{
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<EntitlementService>> _loggerMock;
    private readonly EntitlementService _entitlementService;

    public EntitlementIntegrationTests()
    {
        _tenantContextMock = new Mock<ITenantContext>();
        _sessionServiceMock = new Mock<ISessionService>();
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<EntitlementService>>();
        
        _entitlementService = new EntitlementService(
            _tenantContextMock.Object,
            _sessionServiceMock.Object,
            _cacheMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetUserEntitlementsAsync_WithRegularTenant_ReturnsBasicCmsEntitlements()
    {
        // Arrange
        var userId = "user123";
        var tenantId = Guid.NewGuid(); // Regular tenant (not platform tenant)

        _tenantContextMock.Setup(x => x.GetCurrentUserId()).ReturnsAsync(userId);
        _tenantContextMock.Setup(x => x.GetCurrentTenantIdAsync()).ReturnsAsync(tenantId);

        // Act
        var result = await _entitlementService.GetUserEntitlementsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(PlatformEntitlements.PLATFORM_ACCESS);
        result.Should().Contain(PlatformEntitlements.CMS_ACCESS);
        result.Should().Contain(PlatformEntitlements.CMS_MANAGE);
        result.Should().Contain(PlatformEntitlements.CMS_ASSETS);
    }

    [Fact]
    public async Task HasEntitlementAsync_WithValidCmsEntitlement_ReturnsTrue()
    {
        // Arrange
        var userId = "user123";
        var tenantId = Guid.NewGuid();

        _tenantContextMock.Setup(x => x.GetCurrentUserId()).ReturnsAsync(userId);
        _tenantContextMock.Setup(x => x.GetCurrentTenantIdAsync()).ReturnsAsync(tenantId);

        // Act
        var result = await _entitlementService.HasEntitlementAsync(PlatformEntitlements.CMS_ACCESS);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasEntitlementAsync_WithPlatformAdminEntitlement_ReturnsFalseForRegularTenant()
    {
        // Arrange
        var userId = "user123";
        var tenantId = Guid.NewGuid(); // Regular tenant

        _tenantContextMock.Setup(x => x.GetCurrentUserId()).ReturnsAsync(userId);
        _tenantContextMock.Setup(x => x.GetCurrentTenantIdAsync()).ReturnsAsync(tenantId);

        // Act
        var result = await _entitlementService.HasEntitlementAsync(PlatformEntitlements.PLATFORM_ADMIN);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAccessibleModulesAsync_WithCmsEntitlements_ReturnsCmsModule()
    {
        // Arrange
        var userId = "user123";
        var tenantId = Guid.NewGuid();

        _tenantContextMock.Setup(x => x.GetCurrentUserId()).ReturnsAsync(userId);
        _tenantContextMock.Setup(x => x.GetCurrentTenantIdAsync()).ReturnsAsync(tenantId);

        // Act
        var result = await _entitlementService.GetAccessibleModulesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("cmsModule");
    }

    [Fact]
    public async Task GetAccessibleModulesAsync_WithNoUserContext_ReturnsEmpty()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.GetCurrentUserId()).ReturnsAsync((string?)null);
        _tenantContextMock.Setup(x => x.GetCurrentTenantIdAsync()).ReturnsAsync((Guid?)null);

        // Act
        var result = await _entitlementService.GetAccessibleModulesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HasAnyEntitlementAsync_WithMultipleEntitlements_ReturnsTrue()
    {
        // Arrange
        var userId = "user123";
        var tenantId = Guid.NewGuid();

        _tenantContextMock.Setup(x => x.GetCurrentUserId()).ReturnsAsync(userId);
        _tenantContextMock.Setup(x => x.GetCurrentTenantIdAsync()).ReturnsAsync(tenantId);

        // Act - Test with one valid and one invalid entitlement
        var result = await _entitlementService.HasAnyEntitlementAsync(
            PlatformEntitlements.CMS_ACCESS, 
            PlatformEntitlements.PLATFORM_ADMIN // This won't be present for regular tenant
        );

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessModuleAsync_WithCmsModule_ReturnsTrue()
    {
        // Arrange
        var userId = "user123";
        var tenantId = Guid.NewGuid();

        _tenantContextMock.Setup(x => x.GetCurrentUserId()).ReturnsAsync(userId);
        _tenantContextMock.Setup(x => x.GetCurrentTenantIdAsync()).ReturnsAsync(tenantId);

        // Act
        var result = await _entitlementService.CanAccessModuleAsync("cmsModule");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetMissingEntitlementsAsync_WithSomeRequiredEntitlements_ReturnsMissingOnes()
    {
        // Arrange
        var userId = "user123";
        var tenantId = Guid.NewGuid();

        _tenantContextMock.Setup(x => x.GetCurrentUserId()).ReturnsAsync(userId);
        _tenantContextMock.Setup(x => x.GetCurrentTenantIdAsync()).ReturnsAsync(tenantId);

        var requiredEntitlements = new[]
        {
            PlatformEntitlements.CMS_ACCESS, // User has this
            PlatformEntitlements.PLATFORM_ADMIN, // User doesn't have this
            PlatformEntitlements.CMS_MANAGE // User has this
        };

        // Act
        var result = await _entitlementService.GetMissingEntitlementsAsync(requiredEntitlements);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(PlatformEntitlements.PLATFORM_ADMIN);
        result.Should().NotContain(PlatformEntitlements.CMS_ACCESS);
        result.Should().NotContain(PlatformEntitlements.CMS_MANAGE);
    }
}
