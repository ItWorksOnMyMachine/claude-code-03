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
    private readonly Mock<ILogger<RedisSessionService>> _sessionLoggerMock;
    private readonly RedisSessionService _sessionService;
    
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
        _sessionLoggerMock = new Mock<ILogger<RedisSessionService>>();
        
        // Setup data protection
        var dataProtector = new TestDataProtector();
        _dataProtectionProviderMock.Setup(x => x.CreateProtector(It.IsAny<string>()))
            .Returns(dataProtector);
        
        _sessionService = new RedisSessionService(
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
    }
    
    [Fact]
    public async Task Complete_Tenant_Selection_Flow_Should_Work()
    {
        // Step 1: User authenticates and session is created
        var initialSessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId,
            Username = "testuser",
            Email = "test@example.com",
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };
        
        await _sessionService.StoreSessionDataAsync(_sessionId, initialSessionData);
        
        // Verify session was stored
        var storedSession = await _sessionService.GetSessionDataAsync(_sessionId);
        Assert.NotNull(storedSession);
        Assert.Equal(_userId, storedSession.UserId);
        Assert.Null(storedSession.SelectedTenantId);
        
        // Step 2: Get available tenants for user
        var availableTenants = new List<TenantInfo>
        {
            new TenantInfo 
            { 
                Id = _tenantId1, 
                Name = "Tenant One", 
                IsPlatformTenant = false,
                UserRole = "Admin"
            },
            new TenantInfo 
            { 
                Id = _tenantId2, 
                Name = "Tenant Two", 
                IsPlatformTenant = false,
                UserRole = "User"
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
        
        // Step 4: Update session with selected tenant
        storedSession!.SelectedTenantId = context.TenantId;
        storedSession.SelectedTenantName = context.TenantName;
        storedSession.TenantRoles = context.UserRoles;
        storedSession.TenantSelectedAt = context.SelectedAt;
        storedSession.IsPlatformAdmin = context.IsPlatformAdmin;
        
        await _sessionService.StoreSessionDataAsync(_sessionId, storedSession);
        
        // Step 5: Verify complete session state
        var finalSession = await _sessionService.GetSessionDataAsync(_sessionId);
        Assert.NotNull(finalSession);
        Assert.Equal(_userId, finalSession.UserId);
        Assert.Equal(_tenantId1, finalSession.SelectedTenantId);
        Assert.Equal("Tenant One", finalSession.SelectedTenantName);
        Assert.Contains("Admin", finalSession.TenantRoles);
        Assert.False(finalSession.IsPlatformAdmin);
        
        // Step 6: Validate user can access the selected tenant
        _tenantServiceMock.Setup(x => x.ValidateAccessAsync(_userId, _tenantId1))
            .ReturnsAsync(true);
        
        var hasAccess = await _tenantServiceMock.Object.ValidateAccessAsync(_userId, _tenantId1);
        Assert.True(hasAccess);
    }
    
    [Fact]
    public async Task Tenant_Switch_Flow_Should_Update_Session()
    {
        // Setup: User already has a tenant selected
        var sessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId,
            Username = "testuser",
            Email = "test@example.com",
            SelectedTenantId = _tenantId1,
            SelectedTenantName = "Tenant One",
            TenantRoles = new List<string> { "Admin" },
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };
        
        await _sessionService.StoreSessionDataAsync(_sessionId, sessionData);
        
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
        
        // Update session with new tenant
        var currentSession = await _sessionService.GetSessionDataAsync(_sessionId);
        currentSession!.SelectedTenantId = context.TenantId;
        currentSession.SelectedTenantName = context.TenantName;
        currentSession.TenantRoles = context.UserRoles;
        currentSession.TenantSelectedAt = context.SelectedAt;
        
        await _sessionService.StoreSessionDataAsync(_sessionId, currentSession);
        
        // Verify tenant was switched
        var updatedSession = await _sessionService.GetSessionDataAsync(_sessionId);
        Assert.NotNull(updatedSession);
        Assert.Equal(_tenantId2, updatedSession.SelectedTenantId);
        Assert.Equal("Tenant Two", updatedSession.SelectedTenantName);
        Assert.Contains("User", updatedSession.TenantRoles);
        Assert.DoesNotContain("Admin", updatedSession.TenantRoles);
    }
    
    [Fact]
    public async Task Platform_Admin_Selection_Flow_Should_Set_Admin_Flag()
    {
        // Setup platform admin tenant
        var platformTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var sessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId,
            Username = "admin",
            Email = "admin@platform.com",
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };
        
        await _sessionService.StoreSessionDataAsync(_sessionId, sessionData);
        
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
        
        // Update session
        var currentSession = await _sessionService.GetSessionDataAsync(_sessionId);
        currentSession!.SelectedTenantId = context.TenantId;
        currentSession.SelectedTenantName = context.TenantName;
        currentSession.TenantRoles = context.UserRoles;
        currentSession.IsPlatformAdmin = context.IsPlatformAdmin;
        
        await _sessionService.StoreSessionDataAsync(_sessionId, currentSession);
        
        // Verify platform admin status
        var finalSession = await _sessionService.GetSessionDataAsync(_sessionId);
        Assert.NotNull(finalSession);
        Assert.Equal(platformTenantId, finalSession.SelectedTenantId);
        Assert.True(finalSession.IsPlatformAdmin);
        Assert.Contains("Admin", finalSession.TenantRoles);
        
        // Verify IsPlatformAdmin check
        var isPlatformAdmin = await _tenantServiceMock.Object.IsPlatformAdminAsync(_userId);
        Assert.True(isPlatformAdmin);
    }
    
    [Fact]
    public async Task Clear_Tenant_Selection_Should_Remove_Tenant_From_Session()
    {
        // Setup: User has a tenant selected
        var sessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId,
            Username = "testuser",
            Email = "test@example.com",
            SelectedTenantId = _tenantId1,
            SelectedTenantName = "Tenant One",
            TenantRoles = new List<string> { "Admin" },
            IsPlatformAdmin = false,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };
        
        await _sessionService.StoreSessionDataAsync(_sessionId, sessionData);
        
        // Clear tenant selection
        var currentSession = await _sessionService.GetSessionDataAsync(_sessionId);
        currentSession!.SelectedTenantId = null;
        currentSession.SelectedTenantName = null;
        currentSession.TenantRoles = new List<string>();
        currentSession.TenantSelectedAt = null;
        currentSession.IsPlatformAdmin = false;
        
        await _sessionService.StoreSessionDataAsync(_sessionId, currentSession);
        
        // Verify tenant was cleared
        var clearedSession = await _sessionService.GetSessionDataAsync(_sessionId);
        Assert.NotNull(clearedSession);
        Assert.Equal(_userId, clearedSession.UserId); // User still authenticated
        Assert.Null(clearedSession.SelectedTenantId);
        Assert.Null(clearedSession.SelectedTenantName);
        Assert.Empty(clearedSession.TenantRoles);
        Assert.False(clearedSession.IsPlatformAdmin);
    }
    
    [Fact]
    public async Task Invalid_Tenant_Selection_Should_Fail()
    {
        // Setup
        var sessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId,
            Username = "testuser",
            Email = "test@example.com",
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };
        
        await _sessionService.StoreSessionDataAsync(_sessionId, sessionData);
        
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
        
        // Verify session remains unchanged
        var unchangedSession = await _sessionService.GetSessionDataAsync(_sessionId);
        Assert.NotNull(unchangedSession);
        Assert.Null(unchangedSession.SelectedTenantId);
    }
}