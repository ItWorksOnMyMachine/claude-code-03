using FluentAssertions;
using PlatformShared.Services;
using Xunit;

namespace PlatformShared.Tests.Services;

/// <summary>
/// Tests for the IEntitlementService interface contract
/// </summary>
public class IEntitlementServiceTests
{
    [Fact]
    public void IEntitlementService_Should_HaveCorrectMethods()
    {
        // Assert - Verify interface contract
        var interfaceType = typeof(IEntitlementService);

        interfaceType.Should().HaveMethod("GetUserEntitlementsAsync", new Type[0]);
        interfaceType.Should().HaveMethod("GetUserEntitlementsAsync", new[] { typeof(string), typeof(Guid) });
        interfaceType.Should().HaveMethod("HasEntitlementAsync", new[] { typeof(string) });
        interfaceType.Should().HaveMethod("HasAnyEntitlementAsync", new[] { typeof(string[]) });
        interfaceType.Should().HaveMethod("HasAllEntitlementsAsync", new[] { typeof(string[]) });
        interfaceType.Should().HaveMethod("GetMissingEntitlementsAsync", new[] { typeof(string[]) });
        interfaceType.Should().HaveMethod("CanAccessModuleAsync", new[] { typeof(string) });
        interfaceType.Should().HaveMethod("GetAccessibleModulesAsync", new Type[0]);
        interfaceType.Should().HaveMethod("RefreshEntitlementsAsync", new Type[0]);
    }

    [Fact]
    public void IEntitlementService_Should_BeAnInterface()
    {
        // Assert
        typeof(IEntitlementService).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void IEntitlementService_Should_BeInCorrectNamespace()
    {
        // Assert
        typeof(IEntitlementService).Namespace.Should().Be("PlatformShared.Services");
    }
}

/// <summary>
/// Tests for PlatformEntitlements constants
/// </summary>
public class PlatformEntitlementsTests
{
    [Fact]
    public void PlatformEntitlements_Should_HaveCorrectConstants()
    {
        // Platform-wide entitlements
        PlatformEntitlements.PLATFORM_ACCESS.Should().Be("PLATFORM_ACCESS");
        PlatformEntitlements.PLATFORM_ADMIN.Should().Be("PLATFORM_ADMIN");
        PlatformEntitlements.TENANT_ADMIN.Should().Be("TENANT_ADMIN");

        // CMS entitlements
        PlatformEntitlements.CMS_ACCESS.Should().Be("CMS_ACCESS");
        PlatformEntitlements.CMS_MANAGE.Should().Be("CMS_MANAGE");
        PlatformEntitlements.CMS_ASSETS.Should().Be("CMS_ASSETS");
        PlatformEntitlements.CMS_TEMPLATES.Should().Be("CMS_TEMPLATES");
        PlatformEntitlements.CMS_PUBLISH.Should().Be("CMS_PUBLISH");

        // User Management entitlements
        PlatformEntitlements.USER_READ.Should().Be("USER_READ");
        PlatformEntitlements.USER_WRITE.Should().Be("USER_WRITE");
        PlatformEntitlements.USER_DELETE.Should().Be("USER_DELETE");

        // Tenant Management entitlements
        PlatformEntitlements.TENANT_READ.Should().Be("TENANT_READ");
        PlatformEntitlements.TENANT_WRITE.Should().Be("TENANT_WRITE");
        PlatformEntitlements.TENANT_DELETE.Should().Be("TENANT_DELETE");
    }

    [Fact]
    public void PlatformEntitlements_Should_BeStringConstants()
    {
        // Act & Assert - Verify they're all strings
        typeof(PlatformEntitlements).GetFields()
            .Where(f => f.IsStatic && f.IsLiteral)
            .All(f => f.FieldType == typeof(string))
            .Should().BeTrue();
    }

    [Fact]
    public void PlatformEntitlements_Should_HaveCMSEntitlements()
    {
        // Assert - Verify CMS-specific entitlements exist
        var cmsEntitlements = new[]
        {
            PlatformEntitlements.CMS_ACCESS,
            PlatformEntitlements.CMS_MANAGE,
            PlatformEntitlements.CMS_ASSETS,
            PlatformEntitlements.CMS_TEMPLATES,
            PlatformEntitlements.CMS_PUBLISH
        };

        cmsEntitlements.Should().AllSatisfy(e => e.Should().StartWith("CMS_"));
        cmsEntitlements.Should().HaveCount(5);
    }

    [Fact]
    public void PlatformEntitlements_Should_HavePlatformEntitlements()
    {
        // Assert - Verify platform-wide entitlements exist
        var platformEntitlements = new[]
        {
            PlatformEntitlements.PLATFORM_ACCESS,
            PlatformEntitlements.PLATFORM_ADMIN,
            PlatformEntitlements.TENANT_ADMIN
        };

        platformEntitlements.Should().AllSatisfy(e =>
            (e.StartsWith("PLATFORM_") || e.StartsWith("TENANT_"))
            .Should().BeTrue());
        platformEntitlements.Should().HaveCount(3);
    }
}