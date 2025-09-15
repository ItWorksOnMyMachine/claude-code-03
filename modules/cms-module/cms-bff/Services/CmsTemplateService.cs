using Microsoft.EntityFrameworkCore;
using CmsBff.Data;
using CmsBff.Data.Entities;

namespace CmsBff.Services;

public class CmsTemplateService
{
    private readonly CmsDbContext _context;

    public CmsTemplateService(CmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CmsTemplate>> GetAllAsync()
    {
        return await _context.Templates
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<CmsTemplate?> GetByIdAsync(Guid id)
    {
        return await _context.Templates
            .Include(t => t.Contents)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<CmsTemplate> CreateAsync(CmsTemplate template)
    {
        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTimeOffset.UtcNow;
        template.UpdatedAt = DateTimeOffset.UtcNow;
        
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();
        
        return template;
    }

    public async Task<CmsTemplate?> UpdateAsync(Guid id, CmsTemplate template)
    {
        var existingTemplate = await _context.Templates.FindAsync(id);
        if (existingTemplate == null)
            return null;

        existingTemplate.Name = template.Name;
        existingTemplate.Description = template.Description;
        existingTemplate.TemplateContent = template.TemplateContent;
        existingTemplate.TemplateType = template.TemplateType;
        existingTemplate.IsActive = template.IsActive;
        existingTemplate.UpdatedAt = DateTimeOffset.UtcNow;
        existingTemplate.UpdatedBy = template.UpdatedBy;

        await _context.SaveChangesAsync();
        return existingTemplate;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var template = await _context.Templates.FindAsync(id);
        if (template == null)
            return false;

        // Check if template is being used by any content
        var hasContent = await _context.Contents.AnyAsync(c => c.TemplateId == id);
        if (hasContent)
            return false; // Cannot delete template that's in use

        _context.Templates.Remove(template);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CmsTemplate>> GetActiveTemplatesAsync()
    {
        return await _context.Templates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<CmsTemplate>> GetByTypeAsync(string templateType)
    {
        return await _context.Templates
            .Where(t => t.TemplateType == templateType && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
}