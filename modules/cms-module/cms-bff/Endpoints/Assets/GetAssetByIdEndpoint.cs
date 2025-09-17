using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Data.Entities;
using CmsBff.Services;
using PlatformShared.Authorization;

namespace CmsBff.Endpoints.Assets;

public class GetAssetByIdRequest
{
    public Guid Id { get; set; }
}

[HttpGet("/assets/{id}"), Authorize, RequireCmsAssets]
public class GetAssetByIdEndpoint : Endpoint<GetAssetByIdRequest, CmsAsset>
{
    private readonly CmsAssetService _assetService;

    public GetAssetByIdEndpoint(CmsAssetService assetService)
    {
        _assetService = assetService;
    }

    public override async Task HandleAsync(GetAssetByIdRequest req, CancellationToken ct)
    {
        var asset = await _assetService.GetByIdAsync(req.Id);

        if (asset == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(asset, ct);
    }
}