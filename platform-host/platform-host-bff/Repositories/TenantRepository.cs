using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlatformBff.Data;
using PlatformBff.Data.Entities;
using PlatformBff.Services;

namespace PlatformBff.Repositories;

public class TenantRepository : NonTenantRepository<Tenant>, ITenantRepository
{
    public TenantRepository(PlatformDbContext context, ITenantContext tenantContext) 
        : base(context, tenantContext)
    {
    }

    public async Task<Tenant?> GetBySlugAsync(string slug)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Slug == slug && !t.IsDeleted);
    }

    public async Task<IEnumerable<Tenant>> GetActiveTenantsAsync()
    {
        return await _dbSet
            .Where(t => t.IsActive && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Tenant?> GetPlatformTenantAsync()
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.IsPlatformTenant && !t.IsDeleted);
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, Guid? excludeTenantId = null)
    {
        var query = _dbSet.Where(t => t.Slug == slug && !t.IsDeleted);
        
        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTenantId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<IEnumerable<Tenant>> GetUserTenantsAsync(string userId)
    {
        return await _context.TenantUsers
            .Where(tu => tu.UserId == userId && !tu.IsDeleted && tu.IsActive)
            .Include(tu => tu.Tenant)
            .Where(tu => tu.Tenant.IsActive && !tu.Tenant.IsDeleted)
            .Select(tu => tu.Tenant)
            .Distinct()
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
}