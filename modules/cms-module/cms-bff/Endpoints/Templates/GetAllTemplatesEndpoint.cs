using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Data.Entities;
using CmsBff.Services;

namespace CmsBff.Endpoints.Templates;

[HttpGet("/templates"), Authorize]
public class GetAllTemplatesEndpoint : EndpointWithoutRequest<IEnumerable<CmsTemplate>>
{
    private readonly CmsTemplateService _templateService;

    public GetAllTemplatesEndpoint(CmsTemplateService templateService)
    {
        _templateService = templateService;
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var templates = await _templateService.GetAllAsync();
        await SendOkAsync(templates, ct);
    }
}