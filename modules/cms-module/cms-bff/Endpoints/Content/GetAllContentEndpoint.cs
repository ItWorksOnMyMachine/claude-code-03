using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Data.Entities;
using CmsBff.Services;

namespace CmsBff.Endpoints.Content;

[HttpGet("/content"), Authorize]
public class GetAllContentEndpoint : EndpointWithoutRequest<IEnumerable<CmsContent>>
{
    private readonly CmsContentService _contentService;

    public GetAllContentEndpoint(CmsContentService contentService)
    {
        _contentService = contentService;
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var content = await _contentService.GetAllAsync();
        await SendOkAsync(content, ct);
    }
}