using Microsoft.EntityFrameworkCore;
using CmsBff.Data;
using CmsBff.Data.Entities;
using PlatformShared.Services;

namespace CmsBff.Services;

public class CmsAssetService
{
    private readonly CmsDbContext _context;

    public CmsAssetService(CmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CmsAsset>> GetAllAsync()
    {
        return await _context.Assets
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<CmsAsset?> GetByIdAsync(Guid id)
    {
        return await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<CmsAsset> CreateAsync(CmsAsset asset)
    {
        asset.Id = Guid.NewGuid();
        asset.CreatedAt = DateTimeOffset.UtcNow;
        asset.UpdatedAt = DateTimeOffset.UtcNow;
        
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
        
        return asset;
    }

    public async Task<CmsAsset?> UpdateAsync(Guid id, CmsAsset asset)
    {
        var existingAsset = await _context.Assets.FindAsync(id);
        if (existingAsset == null)
            return null;

        existingAsset.AltText = asset.AltText;
        existingAsset.Description = asset.Description;
        existingAsset.Tags = asset.Tags;
        existingAsset.UpdatedAt = DateTimeOffset.UtcNow;
        existingAsset.UpdatedBy = asset.UpdatedBy;

        await _context.SaveChangesAsync();
        return existingAsset;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
            return false;

        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CmsAsset>> GetByTypeAsync(string assetType)
    {
        return await _context.Assets
            .Where(a => a.AssetType == assetType)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<CmsAsset>> SearchByTagsAsync(string tags)
    {
        return await _context.Assets
            .Where(a => a.Tags != null && a.Tags.Contains(tags))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<long> GetTotalFileSizeAsync()
    {
        return await _context.Assets.SumAsync(a => a.FileSize);
    }
}