using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformBff.Authorization;
using PlatformBff.Models;
using PlatformBff.Models.Tenant;
using PlatformShared.Services;
using PlatformBff.Services.Tenant;

namespace PlatformBff.Controllers;

/// <summary>
/// Platform administration endpoints for managing tenants
/// Requires platform admin privileges
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
[PlatformAdmin] // Require platform admin for all endpoints
public class TenantAdminController : ControllerBase
{
    private readonly ITenantAdminService _tenantAdminService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<TenantAdminController> _logger;

    public TenantAdminController(
        ITenantAdminService tenantAdminService,
        ISessionService sessionService,
        ILogger<TenantAdminController> logger)
    {
        _tenantAdminService = tenantAdminService;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all tenants with pagination
    /// </summary>
    [HttpGet("tenants")]
    public async Task<IActionResult> GetAllTenants([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100; // Max page size

            var result = await _tenantAdminService.GetAllTenantsAsync(page, pageSize);

            _logger.LogInformation("Platform admin retrieved tenant list (page {Page})", page);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenants");
            return StatusCode(500, new ErrorResponse
            {
                Error = "Failed to retrieve tenants",
                StatusCode = 500
            });
        }
    }

    /// <summary>
    /// Create a new tenant
    /// </summary>
    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "Tenant name is required",
                    StatusCode = 400
                });
            }

            var tenant = await _tenantAdminService.CreateTenantAsync(dto);

            // Log admin action
            var sessionId = User.FindFirst("session_id")?.Value;
            var userIdResult = await _sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.UserId));
            _logger.LogWarning("Platform admin {AdminUserId} created tenant {TenantId} ({TenantName})",
                userIdResult.Value, tenant.Id, tenant.Name);

            return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse
            {
                Error = ex.Message,
                StatusCode = 409
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return StatusCode(500, new ErrorResponse
            {
                Error = "Failed to create tenant",
                StatusCode = 500
            });
        }
    }

    /// <summary>
    /// Get a specific tenant by ID
    /// </summary>
    [HttpGet("tenant/{id}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        try
        {
            var statistics = await _tenantAdminService.GetTenantStatisticsAsync(id);
            return Ok(statistics);
        }
        catch (ArgumentException)
        {
            return NotFound(new ErrorResponse
            {
                Error = $"Tenant {id} not found",
                StatusCode = 404
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant {TenantId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Failed to retrieve tenant",
                StatusCode = 500
            });
        }
    }

    /// <summary>
    /// Update a tenant
    /// </summary>
    [HttpPut("tenant/{id}")]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantDto dto)
    {
        try
        {
            var result = await _tenantAdminService.UpdateTenantAsync(id, dto);
            var success = result != null;

            if (!success)
            {
                return NotFound(new ErrorResponse
                {
                    Error = $"Tenant {id} not found",
                    StatusCode = 404
                });
            }

            // Log admin action
            var sessionId = User.FindFirst("session_id")?.Value;
            var userIdResult = await _sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.UserId));
            _logger.LogWarning("Platform admin {AdminUserId} updated tenant {TenantId}",
                userIdResult.Value, id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Failed to update tenant",
                StatusCode = 500
            });
        }
    }

    /// <summary>
    /// Deactivate a tenant
    /// </summary>
    [HttpPost("tenant/{id}/deactivate")]
    public async Task<IActionResult> DeactivateTenant(Guid id)
    {
        try
        {
            var success = await _tenantAdminService.DeactivateTenantAsync(id);

            if (!success)
            {
                return NotFound(new ErrorResponse
                {
                    Error = $"Tenant {id} not found",
                    StatusCode = 404
                });
            }

            // Log admin action
            var sessionId = User.FindFirst("session_id")?.Value;
            var userIdResult = await _sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.UserId));
            _logger.LogWarning("Platform admin {AdminUserId} deactivated tenant {TenantId}",
                userIdResult.Value, id);

            return Ok(new { Success = true, Message = "Tenant deactivated" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Error = ex.Message,
                StatusCode = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating tenant {TenantId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Failed to deactivate tenant",
                StatusCode = 500
            });
        }
    }

    /// <summary>
    /// Add a user to a tenant
    /// </summary>
    [HttpPost("tenant/{id}/users")]
    public async Task<IActionResult> AddUserToTenant(Guid id, [FromQuery] string userId, [FromQuery] string email, [FromQuery] string role = "User")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "User ID is required",
                    StatusCode = 400
                });
            }

            var success = await _tenantAdminService.AssignUserToTenantAsync(id, userId, email, role);

            // Log admin action
            var sessionId = User.FindFirst("session_id")?.Value;
            var userIdResult = await _sessionService.GetSessionDataAsync(sessionId, nameof(PlatformSessionKeys.UserId));
            _logger.LogWarning("Platform admin {AdminUserId} added user {UserId} to tenant {TenantId}",
                userIdResult.Value, userId, id);

            if (success)
            {
                return Ok(new { Success = true, Message = "User added to tenant" });
            }
            return BadRequest(new ErrorResponse { Error = "Failed to add user", StatusCode = 400 });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ErrorResponse
            {
                Error = ex.Message,
                StatusCode = 404
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse
            {
                Error = ex.Message,
                StatusCode = 409
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to tenant {TenantId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Failed to add user to tenant",
                StatusCode = 500
            });
        }
    }

}