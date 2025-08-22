using System;
using Xunit;
using FluentAssertions;
using PlatformBff.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace PlatformBff.Tests.Services;

public class TenantContextTests
{
    [Fact]
    public void TenantContext_Should_Store_And_Retrieve_TenantId()
    {
        // Arrange
        var tenantContext = new TenantContext();
        var expectedTenantId = Guid.NewGuid();

        // Act
        tenantContext.SetTenant(expectedTenantId);
        var actualTenantId = tenantContext.GetCurrentTenantId();

        // Assert
        actualTenantId.Should().Be(expectedTenantId);
    }

    [Fact]
    public void TenantContext_Should_Return_Null_When_No_Tenant_Set()
    {
        // Arrange
        var tenantContext = new TenantContext();

        // Act
        var tenantId = tenantContext.GetCurrentTenantId();

        // Assert
        tenantId.Should().BeNull();
    }

    [Fact]
    public void TenantContext_Should_Clear_Tenant()
    {
        // Arrange
        var tenantContext = new TenantContext();
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId);

        // Act
        tenantContext.ClearTenant();
        var actualTenantId = tenantContext.GetCurrentTenantId();

        // Assert
        actualTenantId.Should().BeNull();
    }

    [Fact]
    public async Task TenantContextMiddleware_Should_Set_Tenant_From_Session()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext();
        
        var context = new DefaultHttpContext();
        var session = new Mock<ISession>();
        var sessionData = System.Text.Encoding.UTF8.GetBytes(tenantId.ToString());
        
        session.Setup(s => s.TryGetValue("TenantId", out sessionData)).Returns(true);
        context.Session = session.Object;

        var middleware = new TenantContextMiddleware(
            next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, tenantContext);

        // Assert
        tenantContext.GetCurrentTenantId().Should().Be(tenantId);
    }

    [Fact]
    public async Task TenantContextMiddleware_Should_Add_TenantId_To_HttpContext_Items()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext();
        
        var context = new DefaultHttpContext();
        var session = new Mock<ISession>();
        var sessionData = System.Text.Encoding.UTF8.GetBytes(tenantId.ToString());
        
        session.Setup(s => s.TryGetValue("TenantId", out sessionData)).Returns(true);
        context.Session = session.Object;

        var middleware = new TenantContextMiddleware(
            next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, tenantContext);

        // Assert
        context.Items.Should().ContainKey("TenantId");
        context.Items["TenantId"].Should().Be(tenantId);
    }

    [Fact]
    public async Task TenantContextMiddleware_Should_Handle_Missing_Session()
    {
        // Arrange
        var tenantContext = new TenantContext();
        var context = new DefaultHttpContext();
        
        // No session set
        context.Session = null;

        var middleware = new TenantContextMiddleware(
            next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, tenantContext);

        // Assert
        tenantContext.GetCurrentTenantId().Should().BeNull();
        context.Items.Should().NotContainKey("TenantId");
    }

    [Fact]
    public async Task TenantContextMiddleware_Should_Set_Tenant_From_Claim_If_No_Session()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext();
        
        var context = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim("TenantId", tenantId.ToString())
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        
        // No session
        context.Session = null;

        var middleware = new TenantContextMiddleware(
            next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context, tenantContext);

        // Assert
        tenantContext.GetCurrentTenantId().Should().Be(tenantId);
    }

    [Fact]
    public void TenantContext_Should_Support_Platform_Tenant()
    {
        // Arrange
        var tenantContext = new TenantContext();
        var platformTenantId = new Guid("00000000-0000-0000-0000-000000000001");

        // Act
        tenantContext.SetTenant(platformTenantId);
        var isPlatformTenant = tenantContext.IsPlatformTenant();

        // Assert
        isPlatformTenant.Should().BeTrue();
        tenantContext.GetCurrentTenantId().Should().Be(platformTenantId);
    }

    [Fact]
    public void TenantContext_Should_Identify_Non_Platform_Tenant()
    {
        // Arrange
        var tenantContext = new TenantContext();
        var regularTenantId = Guid.NewGuid();

        // Act
        tenantContext.SetTenant(regularTenantId);
        var isPlatformTenant = tenantContext.IsPlatformTenant();

        // Assert
        isPlatformTenant.Should().BeFalse();
    }
}