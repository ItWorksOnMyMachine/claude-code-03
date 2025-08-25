using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformBff.Models;
using PlatformBff.Models.Tenant;
using PlatformBff.Services;
using PlatformBff.Services.Tenant;

namespace PlatformBff.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<TenantController> _logger;

    public TenantController(
        ITenantService tenantService,
        ISessionService sessionService,
        ILogger<TenantController> logger)
    {
        _tenantService = tenantService;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current tenant context from session
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentTenant()
    {
        var sessionId = Request.Cookies["platform.session"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized(new ErrorResponse 
            { 
                Error = "No active session",
                StatusCode = 401 
            });
        }

        var sessionData = await _sessionService.GetSessionDataAsync(sessionId);
        if (sessionData == null)
        {
            return Unauthorized(new ErrorResponse 
            { 
                Error = "Invalid session",
                StatusCode = 401 
            });
        }

        // Check if tenant is selected in session
        if (!sessionData.SelectedTenantId.HasValue)
        {
            return Ok(new CurrentTenantResponse
            {
                HasSelectedTenant = false,
                Message = "No tenant selected"
            });
        }

        // Get tenant details
        var tenant = await _tenantService.GetTenantAsync(sessionData.UserId, sessionData.SelectedTenantId.Value);
        if (tenant == null)
        {
            // Clear invalid tenant from session
            sessionData.SelectedTenantId = null;
            sessionData.SelectedTenantName = null;
            sessionData.TenantRoles = new List<string>();
            await _sessionService.UpdateSessionDataAsync(sessionId, sessionData);

            return Ok(new CurrentTenantResponse
            {
                HasSelectedTenant = false,
                Message = "Previously selected tenant is no longer available"
            });
        }

        return Ok(new CurrentTenantResponse
        {
            HasSelectedTenant = true,
            Tenant = new Models.Tenant.TenantContext
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                IsPlatformTenant = tenant.IsPlatformTenant,
                UserRoles = sessionData.TenantRoles ?? new List<string>(),
                SelectedAt = sessionData.TenantSelectedAt ?? DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// Get all tenants available to the current user
    /// </summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableTenants()
    {
        var sessionId = Request.Cookies["platform.session"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized(new ErrorResponse 
            { 
                Error = "No active session",
                StatusCode = 401 
            });
        }

        var sessionData = await _sessionService.GetSessionDataAsync(sessionId);
        if (sessionData == null)
        {
            return Unauthorized(new ErrorResponse 
            { 
                Error = "Invalid session",
                StatusCode = 401 
            });
        }

        var tenants = await _tenantService.GetAvailableTenantsAsync(sessionData.UserId);
        
        return Ok(new AvailableTenantsResponse
        {
            Tenants = tenants,
            CurrentTenantId = sessionData.SelectedTenantId,
            Count = tenants.Count()
        });
    }

    /// <summary>
    /// Select a tenant for the current session
    /// </summary>
    [HttpPost("select")]
    public async Task<IActionResult> SelectTenant([FromBody] SelectTenantRequest request)
    {
        var sessionId = Request.Cookies["platform.session"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized(new ErrorResponse 
            { 
                Error = "No active session",
                StatusCode = 401 
            });
        }

        var sessionData = await _sessionService.GetSessionDataAsync(sessionId);
        if (sessionData == null)
        {
            return Unauthorized(new ErrorResponse 
            { 
                Error = "Invalid session",
                StatusCode = 401 
            });
        }

        try
        {
            // Select the tenant and get context
            var context = await _tenantService.SelectTenantAsync(sessionData.UserId, request.TenantId);

            // Update session with selected tenant
            sessionData.SelectedTenantId = context.TenantId;
            sessionData.SelectedTenantName = context.TenantName;
            sessionData.TenantRoles = context.UserRoles;
            sessionData.TenantSelectedAt = context.SelectedAt;
            sessionData.IsPlatformAdmin = context.IsPlatformAdmin;

            await _sessionService.UpdateSessionDataAsync(sessionId, sessionData);

            _logger.LogInformation("User {UserId} selected tenant {TenantId} ({TenantName})", 
                sessionData.UserId, context.TenantId, context.TenantName);

            return Ok(new TenantSelectionResponse
            {
                Success = true,
                Tenant = context,
                Message = $"Successfully selected tenant: {context.TenantName}"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("User {UserId} attempted to select unauthorized tenant {TenantId}", 
                sessionData.UserId, request.TenantId);
            
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting tenant {TenantId} for user {UserId}", 
                request.TenantId, sessionData.UserId);
            
            return StatusCode(500, new ErrorResponse 
            { 
                Error = "Failed to select tenant",
                Details = ex.Message,
                StatusCode = 500 
            });
        }
    }

    /// <summary>
    /// Switch to a different tenant (convenience endpoint)
    /// </summary>
    [HttpPost("switch")]
    public async Task<IActionResult> SwitchTenant([FromBody] SelectTenantRequest request)
    {
        // This is the same as select but with a different name for clarity
        return await SelectTenant(request);
    }

    /// <summary>
    /// Clear tenant selection (return to tenant selection screen)
    /// </summary>
    [HttpPost("clear")]
    public async Task<IActionResult> ClearTenantSelection()
    {
        var sessionId = Request.Cookies["platform.session"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized(new ErrorResponse 
            { 
                Error = "No active session",
                StatusCode = 401 
            });
        }

        var sessionData = await _sessionService.GetSessionDataAsync(sessionId);
        if (sessionData == null)
        {
            return Unauthorized(new ErrorResponse 
            { 
                Error = "Invalid session",
                StatusCode = 401 
            });
        }

        // Clear tenant selection from session
        sessionData.SelectedTenantId = null;
        sessionData.SelectedTenantName = null;
        sessionData.TenantRoles = new List<string>();
        sessionData.TenantSelectedAt = null;
        sessionData.IsPlatformAdmin = false;

        await _sessionService.UpdateSessionDataAsync(sessionId, sessionData);

        _logger.LogInformation("User {UserId} cleared tenant selection", sessionData.UserId);

        return Ok(new ClearTenantResponse
        {
            Success = true,
            Message = "Tenant selection cleared"
        });
    }
}