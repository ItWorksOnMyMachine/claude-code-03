using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using PlatformShared.Services;

namespace PlatformBff.Endpoints;

/// <summary>
/// Response containing user entitlements
/// </summary>
public class EntitlementResponse
{
    public required string[] Entitlements { get; set; }
    public required string[] AccessibleModules { get; set; }
    public required string UserId { get; set; }
    public required string TenantId { get; set; }
}

/// <summary>
/// Endpoint for retrieving current user's entitlements
/// Used by frontend to determine available features and modules
/// </summary>
[HttpGet("/api/entitlements"), Authorize]
public class GetEntitlementsEndpoint : EndpointWithoutRequest<EntitlementResponse>
{
    private readonly IEntitlementService _entitlementService;
    private readonly ITenantContext _tenantContext;

    public GetEntitlementsEndpoint(IEntitlementService entitlementService, ITenantContext tenantContext)
    {
        _entitlementService = entitlementService;
        _tenantContext = tenantContext;
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = await _tenantContext.GetCurrentUserId();
        var tenantId = await _tenantContext.GetCurrentTenantIdAsync();

        if (string.IsNullOrEmpty(userId) || !tenantId.HasValue)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        try
        {
            var entitlements = await _entitlementService.GetUserEntitlementsAsync();
            var accessibleModules = await _entitlementService.GetAccessibleModulesAsync();

            var response = new EntitlementResponse
            {
                Entitlements = entitlements.ToArray(),
                AccessibleModules = accessibleModules.ToArray(),
                UserId = userId,
                TenantId = tenantId.Value.ToString()
            };

            await SendOkAsync(response, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get entitlements for user {UserId} in tenant {TenantId}", userId, tenantId);
            await SendErrorsAsync(cancellation: ct);
        }
    }
}

/// <summary>
/// Request for checking specific entitlement
/// </summary>
public class CheckEntitlementRequest
{
    public required string Entitlement { get; set; }
}

/// <summary>
/// Response for entitlement check
/// </summary>
public class CheckEntitlementResponse
{
    public required bool HasEntitlement { get; set; }
    public required string Entitlement { get; set; }
}

/// <summary>
/// Endpoint for checking if user has a specific entitlement
/// </summary>
[HttpPost("/api/entitlements/check"), Authorize]
public class CheckEntitlementEndpoint : Endpoint<CheckEntitlementRequest, CheckEntitlementResponse>
{
    private readonly IEntitlementService _entitlementService;

    public CheckEntitlementEndpoint(IEntitlementService entitlementService)
    {
        _entitlementService = entitlementService;
    }

    public override async Task HandleAsync(CheckEntitlementRequest req, CancellationToken ct)
    {
        try
        {
            var hasEntitlement = await _entitlementService.HasEntitlementAsync(req.Entitlement);

            var response = new CheckEntitlementResponse
            {
                HasEntitlement = hasEntitlement,
                Entitlement = req.Entitlement
            };

            await SendOkAsync(response, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to check entitlement {Entitlement}", req.Entitlement);
            await SendErrorsAsync(cancellation: ct);
        }
    }
}