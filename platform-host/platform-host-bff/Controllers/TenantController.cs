using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformBff.Services;

namespace PlatformBff.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantController : ControllerBase
{
    private readonly ITenantContext _tenantContext;

    public TenantController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    [Authorize]
    [HttpGet("available")]
    public IActionResult GetAvailableTenants()
    {
        // This endpoint requires authentication
        // It will be implemented in Task 4 of the multi-tenant spec
        return Ok(new { tenants = new[] { new { id = _tenantContext.GetCurrentTenantId(), name = "Default Tenant" } } });
    }
}