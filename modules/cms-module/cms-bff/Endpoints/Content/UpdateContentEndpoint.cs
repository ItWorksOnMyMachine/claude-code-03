using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Data.Entities;
using CmsBff.Services;
using FluentValidation;
using PlatformShared.Services;
using PlatformShared.Authorization;

namespace CmsBff.Endpoints.Content;

public class UpdateContentRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = "page";
    public string Status { get; set; } = "draft";
    public Guid? TemplateId { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? Tags { get; set; }
    public string? FeaturedImage { get; set; }
}

public class UpdateContentValidator : Validator<UpdateContentRequest>
{
    public UpdateContentValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(255)
            .Matches(@"^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status => new[] { "draft", "published", "archived" }.Contains(status))
            .WithMessage("Status must be draft, published, or archived");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(60);

        RuleFor(x => x.MetaDescription)
            .MaximumLength(160);
    }
}

[HttpPut("/content/{id}"), Authorize, RequireCmsManage]
public class UpdateContentEndpoint : Endpoint<UpdateContentRequest, CmsContent>
{
    private readonly CmsContentService _contentService;
    private readonly ITenantContext _tenantContext;

    public UpdateContentEndpoint(CmsContentService contentService, ITenantContext tenantContext)
    {
        _contentService = contentService;
        _tenantContext = tenantContext;
    }

    public override async Task HandleAsync(UpdateContentRequest req, CancellationToken ct)
    {
        var tenantId = await _tenantContext.GetCurrentTenantIdAsync();
        if (tenantId == null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var userId = await _tenantContext.GetCurrentUserId();

        var content = new CmsContent
        {
            Id = req.Id,
            TenantId = tenantId.Value,
            Title = req.Title,
            Slug = req.Slug,
            Content = req.Content,
            ContentType = req.ContentType,
            Status = req.Status,
            TemplateId = req.TemplateId,
            MetaTitle = req.MetaTitle,
            MetaDescription = req.MetaDescription,
            Tags = req.Tags,
            FeaturedImage = req.FeaturedImage,
            UpdatedBy = userId
        };

        var updatedContent = await _contentService.UpdateAsync(req.Id, content);
        
        if (updatedContent == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(updatedContent, ct);
    }
}