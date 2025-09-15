using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using CmsBff.Data.Entities;
using CmsBff.Services;
using PlatformShared.Authorization;

namespace CmsBff.Endpoints.Assets;

[HttpGet("/assets"), Authorize, RequireCmsAssets]
public class GetAllAssetsEndpoint : EndpointWithoutRequest<IEnumerable<CmsAsset>>
{
    private readonly CmsAssetService _assetService;

    public GetAllAssetsEndpoint(CmsAssetService assetService)
    {
        _assetService = assetService;
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var assets = await _assetService.GetAllAsync();
        await SendOkAsync(assets, ct);
    }
}