using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Data.Entities;
using CmsBff.Services;
using FluentValidation;
using PlatformShared.Services;
using PlatformShared.Authorization;

namespace CmsBff.Endpoints.Content;

public class CreateContentRequest
{
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

public class CreateContentValidator : Validator<CreateContentRequest>
{
    public CreateContentValidator()
    {
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

[HttpPost("/content"), Authorize, RequireCmsManage]
public class CreateContentEndpoint : Endpoint<CreateContentRequest, CmsContent>
{
    private readonly CmsContentService _contentService;
    private readonly ITenantContext _tenantContext;

    public CreateContentEndpoint(CmsContentService contentService, ITenantContext tenantContext)
    {
        _contentService = contentService;
        _tenantContext = tenantContext;
    }

    public override async Task HandleAsync(CreateContentRequest req, CancellationToken ct)
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
            CreatedBy = userId,
            UpdatedBy = userId
        };

        var createdContent = await _contentService.CreateAsync(content);
        await SendCreatedAtAsync<GetContentByIdEndpoint>(new { id = createdContent.Id }, createdContent, cancellation: ct);
    }
}