using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PlatformBff.Controllers;
using PlatformBff.Models;
using PlatformShared.Services;
using SharedModels = PlatformShared.Models;
using PlatformBff.Services.Tenant;
using TenantInfo = PlatformBff.Models.Tenant.TenantInfo;
using TenantContext = PlatformBff.Models.Tenant.TenantContext;
using SelectTenantRequest = PlatformBff.Models.Tenant.SelectTenantRequest;
using CurrentTenantResponse = PlatformBff.Models.Tenant.CurrentTenantResponse;
using AvailableTenantsResponse = PlatformBff.Models.Tenant.AvailableTenantsResponse;
using TenantSelectionResponse = PlatformBff.Models.Tenant.TenantSelectionResponse;
using ClearTenantResponse = PlatformBff.Models.Tenant.ClearTenantResponse;
using PlatformBff.Tests.Helpers;
using Xunit;

namespace PlatformBff.Tests.Controllers;

public class TenantControllerTests
{
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<ILogger<TenantController>> _loggerMock;
    private readonly TenantController _controller;
    private readonly HttpContext _httpContext;
    
    // Test data
    private readonly string _sessionId = "test-session-123";
    private readonly string _userId = "test-user-456";
    private readonly Guid _testTenantId = Guid.NewGuid();
    private readonly Guid _platformTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    
    public TenantControllerTests()
    {
        _tenantServiceMock = new Mock<ITenantService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _loggerMock = new Mock<ILogger<TenantController>>();
        
        _controller = new TenantController(
            _tenantServiceMock.Object,
            _sessionServiceMock.Object,
            _loggerMock.Object
        );
        
        // Setup HTTP context with user claims
        _httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim("session_id", _sessionId),
            new Claim(ClaimTypes.NameIdentifier, _userId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _httpContext.User = principal;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };

        // Setup default session service mocks for UserId
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(_sessionId, nameof(PlatformSessionKeys.UserId)))
            .ReturnsAsync(SharedModels.HasValueOrMissingResult<string>.SetValue(_userId));
    }
    
    [Fact]
    public async Task GetCurrentTenant_Should_Return_Unauthorized_When_No_Session()
    {
        // Arrange - clear user claims to simulate no session
        var identity = new ClaimsIdentity(); // No claims
        var principal = new ClaimsPrincipal(identity);
        _httpContext.User = principal;
        
        // Act
        var result = await _controller.GetCurrentTenant();
        
        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
        Assert.Equal("No active session", error.Error);
    }
    
    [Fact]
    public async Task GetCurrentTenant_Should_Return_No_Tenant_Selected_When_Session_Has_No_Tenant()
    {
        // Arrange - mock session service to return empty value instead of missing
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(_sessionId, nameof(PlatformSessionKeys.SelectedTenantId)))
            .ReturnsAsync(SharedModels.HasValueOrMissingResult<string>.SetValue("")); // Empty tenant selection
        
        // Act
        var result = await _controller.GetCurrentTenant();
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CurrentTenantResponse>(okResult.Value);
        Assert.False(response.HasSelectedTenant);
        Assert.Equal("No tenant selected", response.Message);
    }
    
    [Fact]
    public async Task GetCurrentTenant_Should_Return_Tenant_Context_When_Tenant_Selected()
    {
        // Arrange - mock session service to return selected tenant
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(_sessionId, nameof(PlatformSessionKeys.SelectedTenantId)))
            .ReturnsAsync(SharedModels.HasValueOrMissingResult<string>.SetValue(_testTenantId.ToString()));
        
        var tenantInfo = new TenantInfo
        {
            Id = _testTenantId,
            Name = "Test Tenant",
            IsPlatformTenant = false
        };
        _tenantServiceMock.Setup(x => x.GetTenantAsync(_userId, _testTenantId))
            .ReturnsAsync(tenantInfo);
        
        // Act
        var result = await _controller.GetCurrentTenant();
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CurrentTenantResponse>(okResult.Value);
        Assert.True(response.HasSelectedTenant);
        Assert.NotNull(response.Tenant);
        Assert.Equal(_testTenantId, response.Tenant.TenantId);
        Assert.Equal("Test Tenant", response.Tenant.TenantName);
    }
    
    [Fact]
    public async Task GetAvailableTenants_Should_Return_User_Tenants()
    {
        // Arrange
        var sessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId,
            SelectedTenantId = _testTenantId
        };
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(_sessionId, nameof(PlatformSessionKeys.SelectedTenantId)))
            .ReturnsAsync(SharedModels.HasValueOrMissingResult<string>.SetValue(_testTenantId.ToString()));
        
        var tenants = new List<TenantInfo>
        {
            new TenantInfo { Id = _testTenantId, Name = "Test Tenant", IsPlatformTenant = false },
            new TenantInfo { Id = Guid.NewGuid(), Name = "Another Tenant", IsPlatformTenant = false }
        };
        _tenantServiceMock.Setup(x => x.GetAvailableTenantsAsync(_userId))
            .ReturnsAsync(tenants);
        
        // Act
        var result = await _controller.GetAvailableTenants();
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AvailableTenantsResponse>(okResult.Value);
        Assert.NotNull(response.Tenants);
        Assert.Equal(_testTenantId, response.CurrentTenantId);
        Assert.Equal(2, response.Count);
    }
    
    [Fact]
    public async Task SelectTenant_Should_Update_Session_And_Return_Success()
    {
        // Arrange
        var sessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId
        };
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(_sessionId, nameof(PlatformSessionKeys.SelectedTenantId)))
            .ReturnsAsync(SharedModels.HasValueOrMissingResult<string>.SetValue(_testTenantId.ToString()));
        
        var tenantContext = new TenantContext
        {
            TenantId = _testTenantId,
            TenantName = "Test Tenant",
            IsPlatformTenant = false,
            UserRoles = new List<string> { "User" },
            SelectedAt = DateTime.UtcNow
        };
        _tenantServiceMock.Setup(x => x.SelectTenantAsync(_userId, _testTenantId))
            .ReturnsAsync(tenantContext);
        
        var request = new SelectTenantRequest { TenantId = _testTenantId };
        
        // Act
        var result = await _controller.SelectTenant(request);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TenantSelectionResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Tenant);
        Assert.Contains("Successfully selected tenant", response.Message);
        
        // Verify session was updated (only SelectedTenantId is stored now)
        _sessionServiceMock.Verify(x => x.StoreSessionDataAsync(
            _sessionId, 
            nameof(PlatformSessionKeys.SelectedTenantId),
            _testTenantId.ToString(),
            It.IsAny<DateTimeOffset?>()), Times.Once);
    }
    
    [Fact]
    public async Task SelectTenant_Should_Return_Forbidden_When_User_Has_No_Access()
    {
        // Arrange
        var sessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId
        };
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(_sessionId, nameof(PlatformSessionKeys.SelectedTenantId)))
            .ReturnsAsync(SharedModels.HasValueOrMissingResult<string>.SetValue(_testTenantId.ToString()));
        
        _tenantServiceMock.Setup(x => x.SelectTenantAsync(_userId, _testTenantId))
            .ThrowsAsync(new UnauthorizedAccessException());
        
        var request = new SelectTenantRequest { TenantId = _testTenantId };
        
        // Act
        var result = await _controller.SelectTenant(request);
        
        // Assert
        Assert.IsType<ForbidResult>(result);
    }
    
    [Fact]
    public async Task SwitchTenant_Should_Call_SelectTenant()
    {
        // Arrange
        var sessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId
        };
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(_sessionId, nameof(PlatformSessionKeys.SelectedTenantId)))
            .ReturnsAsync(SharedModels.HasValueOrMissingResult<string>.SetValue(_testTenantId.ToString()));
        
        var tenantContext = new TenantContext
        {
            TenantId = _testTenantId,
            TenantName = "Test Tenant",
            IsPlatformTenant = false,
            UserRoles = new List<string> { "User" },
            SelectedAt = DateTime.UtcNow
        };
        _tenantServiceMock.Setup(x => x.SelectTenantAsync(_userId, _testTenantId))
            .ReturnsAsync(tenantContext);
        
        var request = new SelectTenantRequest { TenantId = _testTenantId };
        
        // Act
        var result = await _controller.SwitchTenant(request);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TenantSelectionResponse>(okResult.Value);
        Assert.True(response.Success);
    }
    
    [Fact]
    public async Task ClearTenantSelection_Should_Clear_Session_Tenant_Data()
    {
        // Arrange
        var sessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId,
            SelectedTenantId = _testTenantId,
            SelectedTenantName = "Test Tenant",
            TenantRoles = new List<string> { "User" },
            IsPlatformAdmin = false
        };
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(_sessionId, nameof(PlatformSessionKeys.SelectedTenantId)))
            .ReturnsAsync(SharedModels.HasValueOrMissingResult<string>.SetValue(_testTenantId.ToString()));
        
        // Act
        var result = await _controller.ClearTenantSelection();
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ClearTenantResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Tenant selection cleared", response.Message);
        
        // Verify session tenant was cleared (SelectedTenantId removed)
        _sessionServiceMock.Verify(x => x.RemoveSessionDataAsync(
            _sessionId,
            nameof(PlatformSessionKeys.SelectedTenantId)), Times.Once);
    }
    
    [Fact]
    public async Task GetCurrentTenant_Should_Clear_Invalid_Tenant_From_Session()
    {
        // Arrange
        var sessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId,
            SelectedTenantId = _testTenantId,
            SelectedTenantName = "Test Tenant",
            TenantRoles = new List<string> { "User" }
        };
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(_sessionId, nameof(PlatformSessionKeys.SelectedTenantId)))
            .ReturnsAsync(SharedModels.HasValueOrMissingResult<string>.SetValue(_testTenantId.ToString()));
        
        // Tenant no longer exists or user lost access
        _tenantServiceMock.Setup(x => x.GetTenantAsync(_userId, _testTenantId))
            .ReturnsAsync((TenantInfo?)null);
        
        // Act
        var result = await _controller.GetCurrentTenant();
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CurrentTenantResponse>(okResult.Value);
        Assert.False(response.HasSelectedTenant);
        Assert.Equal("Previously selected tenant is no longer available", response.Message);
        
        // Verify session user was cleared (UserId removed)
        _sessionServiceMock.Verify(x => x.RemoveSessionDataAsync(
            _sessionId,
            nameof(PlatformSessionKeys.UserId)), Times.Once);
    }
    
    [Fact]
    public async Task SelectTenant_Should_Set_PlatformAdmin_Flag_For_Platform_Tenant()
    {
        // Arrange
        var sessionData = new SessionData
        {
            SessionId = _sessionId,
            UserId = _userId
        };
        _sessionServiceMock.Setup(x => x.GetSessionDataAsync(_sessionId, nameof(PlatformSessionKeys.SelectedTenantId)))
            .ReturnsAsync(SharedModels.HasValueOrMissingResult<string>.SetValue(_testTenantId.ToString()));
        
        var tenantContext = new TenantContext
        {
            TenantId = _platformTenantId,
            TenantName = "Platform Administration",
            IsPlatformTenant = true,
            UserRoles = new List<string> { "Admin" },
            SelectedAt = DateTime.UtcNow
        };
        _tenantServiceMock.Setup(x => x.SelectTenantAsync(_userId, _platformTenantId))
            .ReturnsAsync(tenantContext);
        
        var request = new SelectTenantRequest { TenantId = _platformTenantId };
        
        // Act
        var result = await _controller.SelectTenant(request);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TenantSelectionResponse>(okResult.Value);
        Assert.True(response.Success);
        
        // Note: IsPlatformAdmin is now determined live from tenant service, not stored in session
        // Verify tenant selection was stored
        _sessionServiceMock.Verify(x => x.StoreSessionDataAsync(
            _sessionId,
            nameof(PlatformSessionKeys.SelectedTenantId),
            It.IsAny<string>(),
            It.IsAny<DateTimeOffset?>()), Times.Once);
    }
}