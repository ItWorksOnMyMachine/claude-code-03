using FluentAssertions;
using PlatformShared.Services;
using Xunit;

namespace PlatformShared.Tests.Services;

/// <summary>
/// Tests for the ITenantContext interface contract
/// These tests verify the expected behavior without implementation details
/// </summary>
public class ITenantContextTests
{
    [Fact]
    public void ITenantContext_Should_HaveCorrectMethods()
    {
        // Assert - Verify interface contract
        var interfaceType = typeof(ITenantContext);

        interfaceType.Should().HaveMethod("GetCurrentTenantIdAsync", new Type[0]);
        interfaceType.Should().HaveMethod("SetTenant", new[] { typeof(Guid) });
        interfaceType.Should().HaveMethod("ClearTenant", new Type[0]);
        interfaceType.Should().HaveMethod("IsPlatformTenant", new Type[0]);
        interfaceType.Should().HaveMethod("GetCurrentUserId", new Type[0]);
    }

    [Fact]
    public void ITenantContext_Should_BeAnInterface()
    {
        // Assert
        typeof(ITenantContext).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void ITenantContext_Should_BeInCorrectNamespace()
    {
        // Assert
        typeof(ITenantContext).Namespace.Should().Be("PlatformShared.Services");
    }
}

/// <summary>
/// Tests for TenantContext implementation
/// </summary>
public class TenantContextTests
{
    [Fact]
    public void TenantContext_Should_ImplementITenantContext()
    {
        // Assert
        typeof(TenantContext).Should().Implement<ITenantContext>();
    }

    [Fact]
    public void TenantContext_Should_HaveCorrectPlatformTenantId()
    {
        // Assert
        TenantContext.PlatformTenantId.Should().Be(new Guid("00000000-0000-0000-0000-000000000001"));
    }

    [Fact]
    public void TenantContext_Should_BeInCorrectNamespace()
    {
        // Assert
        typeof(TenantContext).Namespace.Should().Be("PlatformShared.Services");
    }
}