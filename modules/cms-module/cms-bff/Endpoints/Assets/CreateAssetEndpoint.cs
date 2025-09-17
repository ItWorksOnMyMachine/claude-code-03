using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Data.Entities;
using CmsBff.Services;
using FluentValidation;
using PlatformShared.Services;
using PlatformShared.Authorization;

namespace CmsBff.Endpoints.Assets;

public class CreateAssetRequest
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? PublicUrl { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string AssetType { get; set; } = "file";
    public string? AltText { get; set; }
    public string? Description { get; set; }
    public string Tags { get; set; } = "[]";
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? Duration { get; set; }
    public string Metadata { get; set; } = "{}";
    public string? ContentHash { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class CreateAssetValidator : Validator<CreateAssetRequest>
{
    public CreateAssetValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.OriginalFileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.StoragePath)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.MimeType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.AssetType)
            .NotEmpty()
            .MaximumLength(50);
    }
}

[HttpPost("/api/cms/assets"), Authorize, RequireCmsAssets]
public class CreateAssetEndpoint : Endpoint<CreateAssetRequest, CmsAsset>
{
    private readonly CmsAssetService _assetService;
    private readonly ITenantContext _tenantContext;

    public CreateAssetEndpoint(CmsAssetService assetService, ITenantContext tenantContext)
    {
        _assetService = assetService;
        _tenantContext = tenantContext;
    }

    public override async Task HandleAsync(CreateAssetRequest req, CancellationToken ct)
    {
        var tenantId = await _tenantContext.GetCurrentTenantIdAsync();
        if (tenantId == null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var userId = await _tenantContext.GetCurrentUserId();

        var asset = new CmsAsset
        {
            TenantId = tenantId.Value,
            FileName = req.FileName,
            OriginalFileName = req.OriginalFileName,
            StoragePath = req.StoragePath,
            PublicUrl = req.PublicUrl,
            MimeType = req.MimeType,
            FileSize = req.FileSize,
            AssetType = req.AssetType,
            AltText = req.AltText,
            Description = req.Description,
            Tags = req.Tags,
            Width = req.Width,
            Height = req.Height,
            Duration = req.Duration,
            Metadata = req.Metadata,
            ContentHash = req.ContentHash,
            IsPublic = req.IsPublic,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        var createdAsset = await _assetService.CreateAsync(asset);
        await SendCreatedAtAsync<GetAssetByIdEndpoint>(new { id = createdAsset.Id }, createdAsset, cancellation: ct);
    }
}