using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Services;

namespace CmsBff.Endpoints.Content;

public class DeleteContentRequest
{
    public Guid Id { get; set; }
}

[HttpDelete("/content/{id}"), Authorize]
public class DeleteContentEndpoint : Endpoint<DeleteContentRequest>
{
    private readonly CmsContentService _contentService;
    private readonly ITenantContext _tenantContext;

    public DeleteContentEndpoint(CmsContentService contentService, ITenantContext tenantContext)
    {
        _contentService = contentService;
        _tenantContext = tenantContext;
    }

    public override async Task HandleAsync(DeleteContentRequest req, CancellationToken ct)
    {
        var tenantId = await _tenantContext.GetCurrentTenantIdAsync();
        if (tenantId == null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var success = await _contentService.DeleteAsync(req.Id);
        
        if (!success)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendNoContentAsync(ct);
    }
}