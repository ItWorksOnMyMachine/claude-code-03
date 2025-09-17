using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Services;
using PlatformShared.Services;
using PlatformShared.Authorization;

namespace CmsBff.Endpoints.Templates;

public class DeleteTemplateRequest
{
    public Guid Id { get; set; }
}

[HttpDelete("/api/cms/templates/{id}"), Authorize, RequireCmsTemplates]
public class DeleteTemplateEndpoint : Endpoint<DeleteTemplateRequest>
{
    private readonly CmsTemplateService _templateService;
    private readonly ITenantContext _tenantContext;

    public DeleteTemplateEndpoint(CmsTemplateService templateService, ITenantContext tenantContext)
    {
        _templateService = templateService;
        _tenantContext = tenantContext;
    }

    public override async Task HandleAsync(DeleteTemplateRequest req, CancellationToken ct)
    {
        var tenantId = await _tenantContext.GetCurrentTenantIdAsync();
        if (tenantId == null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        // First check if template exists
        var template = await _templateService.GetByIdAsync(req.Id);
        if (template == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // Try to delete the template
        var deleted = await _templateService.DeleteAsync(req.Id);
        if (!deleted)
        {
            // Template exists but can't be deleted (likely in use)
            await SendAsync(new { error = "Template cannot be deleted because it is in use" }, 400, ct);
            return;
        }

        await SendNoContentAsync(ct);
    }
}
