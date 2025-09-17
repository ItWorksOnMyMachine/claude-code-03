using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Data.Entities;
using CmsBff.Services;
using FluentValidation;
using PlatformShared.Services;
using PlatformShared.Authorization;

namespace CmsBff.Endpoints.Templates;

public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string LayoutContent { get; set; } = "{}";
    public string TemplateType { get; set; } = "page";
    public bool IsActive { get; set; } = true;
    public string? PreviewImage { get; set; }
}

public class CreateTemplateValidator : Validator<CreateTemplateRequest>
{
    public CreateTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.TemplateType)
            .NotEmpty()
            .MaximumLength(50);
    }
}

[HttpPost("/api/cms/templates"), Authorize, RequireCmsTemplates]
public class CreateTemplateEndpoint : Endpoint<CreateTemplateRequest, CmsTemplate>
{
    private readonly CmsTemplateService _templateService;
    private readonly ITenantContext _tenantContext;

    public CreateTemplateEndpoint(CmsTemplateService templateService, ITenantContext tenantContext)
    {
        _templateService = templateService;
        _tenantContext = tenantContext;
    }

    public override async Task HandleAsync(CreateTemplateRequest req, CancellationToken ct)
    {
        var tenantId = await _tenantContext.GetCurrentTenantIdAsync();
        if (tenantId == null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var userId = await _tenantContext.GetCurrentUserId();

        var template = new CmsTemplate
        {
            TenantId = tenantId.Value,
            Name = req.Name,
            Description = req.Description,
            LayoutContent = req.LayoutContent,
            TemplateType = req.TemplateType,
            IsActive = req.IsActive,
            PreviewImage = req.PreviewImage,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        var createdTemplate = await _templateService.CreateAsync(template);
        await SendCreatedAtAsync<GetTemplateByIdEndpoint>(new { id = createdTemplate.Id }, createdTemplate, cancellation: ct);
    }
}