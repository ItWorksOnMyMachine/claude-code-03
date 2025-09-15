using Microsoft.EntityFrameworkCore;
using CmsBff.Data;
using CmsBff.Data.Entities;

namespace CmsBff.Services;

public class CmsContentService
{
    private readonly CmsDbContext _context;

    public CmsContentService(CmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CmsContent>> GetAllAsync()
    {
        return await _context.Contents
            .Include(c => c.Template)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<CmsContent?> GetByIdAsync(Guid id)
    {
        return await _context.Contents
            .Include(c => c.Template)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CmsContent?> GetBySlugAsync(string slug)
    {
        return await _context.Contents
            .Include(c => c.Template)
            .FirstOrDefaultAsync(c => c.Slug == slug);
    }

    public async Task<CmsContent> CreateAsync(CmsContent content)
    {
        content.Id = Guid.NewGuid();
        content.CreatedAt = DateTimeOffset.UtcNow;
        content.UpdatedAt = DateTimeOffset.UtcNow;
        
        _context.Contents.Add(content);
        await _context.SaveChangesAsync();
        
        return content;
    }

    public async Task<CmsContent?> UpdateAsync(Guid id, CmsContent content)
    {
        var existingContent = await _context.Contents.FindAsync(id);
        if (existingContent == null)
            return null;

        existingContent.Title = content.Title;
        existingContent.Slug = content.Slug;
        existingContent.Content = content.Content;
        existingContent.ContentType = content.ContentType;
        existingContent.Status = content.Status;
        existingContent.TemplateId = content.TemplateId;
        existingContent.MetaTitle = content.MetaTitle;
        existingContent.MetaDescription = content.MetaDescription;
        existingContent.Tags = content.Tags;
        existingContent.UpdatedAt = DateTimeOffset.UtcNow;
        existingContent.UpdatedBy = content.UpdatedBy;

        await _context.SaveChangesAsync();
        return existingContent;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var content = await _context.Contents.FindAsync(id);
        if (content == null)
            return false;

        // Soft delete
        content.IsDeleted = true;
        content.DeletedAt = DateTimeOffset.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CmsContent>> GetByStatusAsync(string status)
    {
        return await _context.Contents
            .Include(c => c.Template)
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<CmsContent>> GetByContentTypeAsync(string contentType)
    {
        return await _context.Contents
            .Include(c => c.Template)
            .Where(c => c.ContentType == contentType)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }
}