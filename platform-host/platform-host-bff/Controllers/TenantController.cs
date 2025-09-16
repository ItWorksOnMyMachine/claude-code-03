using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformBff.Models;
using PlatformBff.Models.Tenant;
using PlatformShared.Services;
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
        var sessionId = User.FindFirst("session_id")?.Value;
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "No active session",
                StatusCode = 401
            });
        }

        var selectedTenantIdResult = await _sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.SelectedTenantId));
        if (selectedTenantIdResult.IsMissing)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Invalid session",
                StatusCode = 401
            });
        }

        // Check if tenant is selected in session
        if (selectedTenantIdResult.Value == null || !Guid.TryParse(selectedTenantIdResult.Value, out var selectedTenantId))
        {
            return Ok(new CurrentTenantResponse
            {
                HasSelectedTenant = false,
                Message = "No tenant selected"
            });
        }

        // Get tenant details
        var userIdResult = await _sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.UserId));

        var tenant = await _tenantService.GetTenantAsync(userIdResult.Value, selectedTenantId);
        if (tenant == null)
        {
            await _sessionService.RemoveSessionDataAsync(sessionId, nameof(PlatformSessionKeys.UserId));

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
                UserRoles = tenant.UserRoles.ToList(),
                SelectedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// Get all tenants available to the current user
    /// </summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableTenants()
    {
        var sessionId = User.FindFirst("session_id")?.Value;
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "No active session",
                StatusCode = 401
            });
        }

        var UserIdResult = await _sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.UserId));
        if (UserIdResult.IsMissing)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Invalid session",
                StatusCode = 401
            });
        }

        var tenants = await _tenantService.GetAvailableTenantsAsync(UserIdResult.Value);
        var selectedTenantIdResult = await _sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.SelectedTenantId));

        return Ok(new AvailableTenantsResponse
        {
            Tenants = tenants,
            CurrentTenantId = selectedTenantIdResult.IsMissing || selectedTenantIdResult.Value == null ? null :
                Guid.TryParse(selectedTenantIdResult.Value, out var tenantId) ? tenantId : null,
            Count = tenants.Count()
        });
    }

    /// <summary>
    /// Select a tenant for the current session
    /// </summary>
    [HttpPost("select")]
    public async Task<IActionResult> SelectTenant([FromBody] SelectTenantRequest request)
    {
        var sessionId = User.FindFirst("session_id")?.Value;
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "No active session",
                StatusCode = 401
            });
        }

        var UserIdResult = await _sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.UserId));
        if (UserIdResult.IsMissing)
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
            var context = await _tenantService.SelectTenantAsync(UserIdResult.Value, request.TenantId);
            await _sessionService.StoreSessionDataAsync(sessionId, nameof(PlatformSessionKeys.SelectedTenantId), context.TenantId.ToString());

            _logger.LogInformation("User {UserId} selected tenant {TenantId} ({TenantName})",
                UserIdResult.Value, context.TenantId, context.TenantName);

            return Ok(new TenantSelectionResponse
            {
                Success = true,
                Tenant = context,
                Message = $"Successfully selected tenant: {context.TenantName}"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "User {UserId} attempted to select unauthorized tenant {TenantId}",
                UserIdResult.Value, request.TenantId);

            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting tenant {TenantId} for user {UserId}",
                request.TenantId, UserIdResult.Value);

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
        var sessionId = User.FindFirst("session_id")?.Value;
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "No active session",
                StatusCode = 401
            });
        }

        var UserIdResult = await _sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.UserId));
        if (UserIdResult.IsMissing)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Invalid session",
                StatusCode = 401
            });
        }

        // Clear tenant selection from session
        await _sessionService.RemoveSessionDataAsync(sessionId, nameof(PlatformSessionKeys.SelectedTenantId));

        _logger.LogInformation("User {UserId} cleared tenant selection", UserIdResult.Value);

        return Ok(new ClearTenantResponse
        {
            Success = true,
            Message = "Tenant selection cleared"
        });
    }
}