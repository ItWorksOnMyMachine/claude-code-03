using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Data.Entities;
using CmsBff.Services;
using PlatformShared.Authorization;

namespace CmsBff.Endpoints.Content;

public class GetContentByIdRequest
{
    public Guid Id { get; set; }
}

[HttpGet("/api/cms/content/{id}"), Authorize, RequireCmsAccess]
public class GetContentByIdEndpoint : Endpoint<GetContentByIdRequest, CmsContent>
{
    private readonly CmsContentService _contentService;

    public GetContentByIdEndpoint(CmsContentService contentService)
    {
        _contentService = contentService;
    }

    public override async Task HandleAsync(GetContentByIdRequest req, CancellationToken ct)
    {
        var content = await _contentService.GetByIdAsync(req.Id);
        
        if (content == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(content, ct);
    }
}