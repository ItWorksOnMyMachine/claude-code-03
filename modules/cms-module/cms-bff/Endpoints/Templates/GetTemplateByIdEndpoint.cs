using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Data.Entities;
using CmsBff.Services;
using PlatformShared.Authorization;

namespace CmsBff.Endpoints.Templates;

public class GetTemplateByIdRequest
{
    public Guid Id { get; set; }
}

[HttpGet("/templates/{id}"), Authorize, RequireCmsTemplates]
public class GetTemplateByIdEndpoint : Endpoint<GetTemplateByIdRequest, CmsTemplate>
{
    private readonly CmsTemplateService _templateService;

    public GetTemplateByIdEndpoint(CmsTemplateService templateService)
    {
        _templateService = templateService;
    }

    public override async Task HandleAsync(GetTemplateByIdRequest req, CancellationToken ct)
    {
        var template = await _templateService.GetByIdAsync(req.Id);

        if (template == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(template, ct);
    }
}