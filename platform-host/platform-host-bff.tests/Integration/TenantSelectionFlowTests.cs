using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PlatformBff.Models;
using PlatformBff.Services;
using PlatformBff.Services.Tenant;
using TenantInfo = PlatformBff.Models.Tenant.TenantInfo;
using TenantContext = PlatformBff.Models.Tenant.TenantContext;
using PlatformBff.Tests.Authentication;
using Xunit;

namespace PlatformBff.Tests.Integration;

/// <summary>
/// Tests the complete tenant selection flow from authentication to tenant selection
/// </summary>
public class TenantSelectionFlowTests
{
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IDataProtectionProvider> _dataProtectionProviderMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<DistributedSessionService>> _sessionLoggerMock;
    private readonly DistributedSessionService _sessionService;

    // Test data
    private readonly string _sessionId = Guid.NewGuid().ToString();
    private readonly string _userId = "auth-user-123";
    private readonly Guid _tenantId1 = Guid.NewGuid();
    private readonly Guid _tenantId2 = Guid.NewGuid();

    public TenantSelectionFlowTests()
    {
        _tenantServiceMock = new Mock<ITenantService>();
        _cacheMock = new Mock<IDistributedCache>();
        _configurationMock = new Mock<IConfiguration>();
        _dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _sessionLoggerMock = new Mock<ILogger<DistributedSessionService>>();

        // Setup data protection
        var dataProtector = new TestDataProtector();
        _dataProtectionProviderMock.Setup(x => x.CreateProtector(It.IsAny<string>()))
            .Returns(dataProtector);

        _sessionService = new DistributedSessionService(
            _cacheMock.Object,
            _dataProtectionProviderMock.Object,
            _sessionLoggerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object
        );

        SetupMocks();
    }

    private void SetupMocks()
    {
        // Setup configuration
        _configurationMock.Setup(x => x["SessionExpiration"])
            .Returns("120"); // 2 hours

        // Setup cache to store and retrieve data
        var cacheData = new Dictionary<string, byte[]>();

        _cacheMock.Setup(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, options, token) => cacheData[key] = value)
            .Returns(Task.CompletedTask);

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken token) =>
                cacheData.ContainsKey(key) ? cacheData[key] : null);

        _cacheMock.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, token) => cacheData.Remove(key))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Complete_Tenant_Selection_Flow_Should_Work()
    {
        // Step 1: User authenticates and session is created (only store UserId as per new session pattern)
        await _sessionService.StoreSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.UserId), _userId, DateTime.UtcNow.AddHours(2));

        // Verify session was stored
        var userIdResult = await _sessionService.GetSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.UserId));
        Assert.True(userIdResult.HasValue);
        Assert.Equal(_userId, userIdResult.Value);

        // Verify no tenant is selected initially
        var tenantIdResult = await _sessionService.GetSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId));
        Assert.True(tenantIdResult.IsMissing);

        // Step 2: Get available tenants for user
        var availableTenants = new List<TenantInfo>
        {
            new TenantInfo
            {
                Id = _tenantId1,
                Name = "Tenant One",
                IsPlatformTenant = false,
                UserRoles = new List<string> { "Admin" }
            },
            new TenantInfo
            {
                Id = _tenantId2,
                Name = "Tenant Two",
                IsPlatformTenant = false,
                UserRoles = new List<string> { "User" }
            }
        };

        _tenantServiceMock.Setup(x => x.GetAvailableTenantsAsync(_userId))
            .ReturnsAsync(availableTenants);

        var tenants = await _tenantServiceMock.Object.GetAvailableTenantsAsync(_userId);
        Assert.Equal(2, tenants.Count());

        // Step 3: User selects a tenant
        var selectedTenantContext = new TenantContext
        {
            TenantId = _tenantId1,
            TenantName = "Tenant One",
            IsPlatformTenant = false,
            UserRoles = new List<string> { "Admin" },
            SelectedAt = DateTime.UtcNow
        };

        _tenantServiceMock.Setup(x => x.SelectTenantAsync(_userId, _tenantId1))
            .ReturnsAsync(selectedTenantContext);

        var context = await _tenantServiceMock.Object.SelectTenantAsync(_userId, _tenantId1);

        // Step 4: Update session with selected tenant (only store SelectedTenantId as per new session pattern)
        await _sessionService.StoreSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId), context.TenantId.ToString());

        // Step 5: Verify session state (only verify the stored session variables)
        var finalUserIdResult = await _sessionService.GetSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.UserId));
        Assert.True(finalUserIdResult.HasValue);
        Assert.Equal(_userId, finalUserIdResult.Value);

        var finalTenantIdResult = await _sessionService.GetSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId));
        Assert.True(finalTenantIdResult.HasValue);
        Assert.Equal(_tenantId1.ToString(), finalTenantIdResult.Value);

        // Note: Other data like TenantName, UserRoles, etc. are now pulled live from the tenant service,
        // not stored in session to avoid race conditions

        // Step 6: Validate user can access the selected tenant
        _tenantServiceMock.Setup(x => x.ValidateAccessAsync(_userId, _tenantId1))
            .ReturnsAsync(true);

        var hasAccess = await _tenantServiceMock.Object.ValidateAccessAsync(_userId, _tenantId1);
        Assert.True(hasAccess);
    }

    [Fact]
    public async Task Tenant_Switch_Flow_Should_Update_Session()
    {
        // Setup: User already has a tenant selected (only store essential session variables)
        await _sessionService.StoreSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.UserId), _userId, DateTime.UtcNow.AddHours(2));
        await _sessionService.StoreSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId), _tenantId1.ToString());

        // User switches to a different tenant
        var newTenantContext = new TenantContext
        {
            TenantId = _tenantId2,
            TenantName = "Tenant Two",
            IsPlatformTenant = false,
            UserRoles = new List<string> { "User" },
            SelectedAt = DateTime.UtcNow
        };

        _tenantServiceMock.Setup(x => x.SelectTenantAsync(_userId, _tenantId2))
            .ReturnsAsync(newTenantContext);

        var context = await _tenantServiceMock.Object.SelectTenantAsync(_userId, _tenantId2);

        // Update session with new tenant (only store SelectedTenantId)
        await _sessionService.StoreSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId), context.TenantId.ToString());

        // Verify tenant was switched (only verify stored session variables)
        var tenantIdResult = await _sessionService.GetSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId));
        Assert.True(tenantIdResult.HasValue);
        Assert.Equal(_tenantId2.ToString(), tenantIdResult.Value);

        // Note: TenantName, UserRoles etc. are now pulled live from tenant service to avoid race conditions
    }

    [Fact]
    public async Task Platform_Admin_Selection_Flow_Should_Set_Admin_Flag()
    {
        // Setup platform admin tenant
        var platformTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Setup session for platform admin (only store UserId)
        await _sessionService.StoreSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.UserId), _userId, DateTime.UtcNow.AddHours(2));

        // Admin selects platform tenant
        var platformContext = new TenantContext
        {
            TenantId = platformTenantId,
            TenantName = "Platform Administration",
            IsPlatformTenant = true,
            UserRoles = new List<string> { "Admin" },
            SelectedAt = DateTime.UtcNow
        };

        _tenantServiceMock.Setup(x => x.SelectTenantAsync(_userId, platformTenantId))
            .ReturnsAsync(platformContext);

        _tenantServiceMock.Setup(x => x.IsPlatformAdminAsync(_userId))
            .ReturnsAsync(true);

        var context = await _tenantServiceMock.Object.SelectTenantAsync(_userId, platformTenantId);

        // Update session (only store SelectedTenantId)
        await _sessionService.StoreSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId), context.TenantId.ToString());

        // Verify platform admin status (only verify stored session variables)
        var tenantIdResult = await _sessionService.GetSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId));
        Assert.True(tenantIdResult.HasValue);
        Assert.Equal(platformTenantId.ToString(), tenantIdResult.Value);

        // Note: IsPlatformAdmin and TenantRoles are now checked live from tenant service

        // Verify IsPlatformAdmin check
        var isPlatformAdmin = await _tenantServiceMock.Object.IsPlatformAdminAsync(_userId);
        Assert.True(isPlatformAdmin);
    }

    [Fact]
    public async Task Clear_Tenant_Selection_Should_Remove_Tenant_From_Session()
    {
        // Setup: User has a tenant selected
        await _sessionService.StoreSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.UserId), _userId, DateTime.UtcNow.AddHours(2));
        await _sessionService.StoreSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId), _tenantId1.ToString());

        // Clear tenant selection
        await _sessionService.RemoveSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId));

        // Verify tenant was cleared
        var userIdResult = await _sessionService.GetSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.UserId));
        Assert.True(userIdResult.HasValue);
        Assert.Equal(_userId, userIdResult.Value); // User still authenticated

        var tenantIdResult = await _sessionService.GetSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId));
        Assert.True(tenantIdResult.IsMissing); // Tenant cleared
    }

    [Fact]
    public async Task Invalid_Tenant_Selection_Should_Fail()
    {
        // Setup
        await _sessionService.StoreSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.UserId), _userId, DateTime.UtcNow.AddHours(2));

        // Try to select a tenant user doesn't have access to
        var invalidTenantId = Guid.NewGuid();

        _tenantServiceMock.Setup(x => x.SelectTenantAsync(_userId, invalidTenantId))
            .ThrowsAsync(new UnauthorizedAccessException($"User {_userId} does not have access to tenant {invalidTenantId}"));

        _tenantServiceMock.Setup(x => x.ValidateAccessAsync(_userId, invalidTenantId))
            .ReturnsAsync(false);

        // Verify selection fails
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _tenantServiceMock.Object.SelectTenantAsync(_userId, invalidTenantId)
        );

        // Verify access check returns false
        var hasAccess = await _tenantServiceMock.Object.ValidateAccessAsync(_userId, invalidTenantId);
        Assert.False(hasAccess);

        // Verify session remains unchanged - no tenant should be selected
        var tenantIdResult = await _sessionService.GetSessionDataAsync(_sessionId, nameof(PlatformBffSessionKeys.SelectedTenantId));
        Assert.True(tenantIdResult.IsMissing); // No tenant selected
    }
}