using Microsoft.EntityFrameworkCore;
using PlatformBff.Data;
using PlatformBff.Models.Tenant;
using PlatformBff.Repositories;

namespace PlatformBff.Services.Tenant;

/// <summary>
/// Implementation of tenant service for managing tenant selection and context
/// </summary>
public class TenantService : ITenantService
{
    // Fixed GUID for platform administration tenant
    private static readonly Guid PLATFORM_TENANT_ID = Guid.Parse("00000000-0000-0000-0000-000000000001");
    
    private readonly PlatformDbContext _context;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        PlatformDbContext context,
        ITenantRepository tenantRepository,
        ITenantUserRepository tenantUserRepository,
        ILogger<TenantService> logger)
    {
        _context = context;
        _tenantRepository = tenantRepository;
        _tenantUserRepository = tenantUserRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<TenantInfo>> GetAvailableTenantsAsync(string userId)
    {
        try
        {
            // Get all tenant users for this auth subject ID
            var tenantUsers = await _context.TenantUsers
                .Include(tu => tu.Tenant)
                .Include(tu => tu.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(tu => tu.UserId == userId && tu.IsActive && tu.Tenant.IsActive)
                .ToListAsync();

            var tenants = tenantUsers.Select(tu => new TenantInfo
            {
                Id = tu.TenantId,
                Name = tu.Tenant.DisplayName,
                Description = tu.Tenant.Settings,
                IsActive = tu.Tenant.IsActive,
                IsPlatformTenant = tu.Tenant.IsPlatformTenant,
                CreatedAt = tu.Tenant.CreatedAt.DateTime,
                LogoUrl = null,
                UserRole = tu.UserRoles.FirstOrDefault()?.Role?.Name ?? "User"
            }).ToList();

            _logger.LogInformation("User {UserId} has access to {Count} tenants", userId, tenants.Count);
            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available tenants for user {UserId}", userId);
            throw;
        }
    }

    public async Task<TenantInfo?> GetTenantAsync(string userId, Guid tenantId)
    {
        try
        {
            var tenantUser = await _context.TenantUsers
                .Include(tu => tu.Tenant)
                .Include(tu => tu.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(tu => 
                    tu.UserId == userId && 
                    tu.TenantId == tenantId && 
                    tu.IsActive && 
                    tu.Tenant.IsActive);

            if (tenantUser == null)
            {
                _logger.LogWarning("User {UserId} does not have access to tenant {TenantId}", userId, tenantId);
                return null;
            }

            return new TenantInfo
            {
                Id = tenantUser.TenantId,
                Name = tenantUser.Tenant.DisplayName,
                Description = tenantUser.Tenant.Settings,
                IsActive = tenantUser.Tenant.IsActive,
                IsPlatformTenant = tenantUser.Tenant.IsPlatformTenant,
                CreatedAt = tenantUser.Tenant.CreatedAt.DateTime,
                LogoUrl = null,
                UserRole = tenantUser.UserRoles.FirstOrDefault()?.Role?.Name ?? "User"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant {TenantId} for user {UserId}", tenantId, userId);
            throw;
        }
    }

    public async Task<Models.Tenant.TenantContext> SelectTenantAsync(string userId, Guid tenantId)
    {
        try
        {
            // Validate user has access to this tenant
            var tenantUser = await _context.TenantUsers
                .Include(tu => tu.Tenant)
                .Include(tu => tu.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(tu => 
                    tu.UserId == userId && 
                    tu.TenantId == tenantId && 
                    tu.IsActive && 
                    tu.Tenant.IsActive);

            if (tenantUser == null)
            {
                throw new UnauthorizedAccessException($"User {userId} does not have access to tenant {tenantId}");
            }

            var context = new Models.Tenant.TenantContext
            {
                TenantId = tenantUser.TenantId,
                TenantName = tenantUser.Tenant.DisplayName,
                IsPlatformTenant = tenantUser.Tenant.IsPlatformTenant,
                UserRoles = tenantUser.UserRoles.Select(ur => ur.Role.Name).ToList(),
                SelectedAt = DateTime.UtcNow
            };

            _logger.LogInformation("User {UserId} selected tenant {TenantId} ({TenantName})", 
                userId, tenantId, context.TenantName);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting tenant {TenantId} for user {UserId}", tenantId, userId);
            throw;
        }
    }

    public async Task<bool> ValidateAccessAsync(string userId, Guid tenantId)
    {
        try
        {
            return await _context.TenantUsers
                .AnyAsync(tu => 
                    tu.UserId == userId && 
                    tu.TenantId == tenantId && 
                    tu.IsActive && 
                    tu.Tenant.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating access for user {UserId} to tenant {TenantId}", userId, tenantId);
            return false;
        }
    }

    public async Task<bool> IsPlatformAdminAsync(string userId)
    {
        try
        {
            var platformUser = await _context.TenantUsers
                .Include(tu => tu.Tenant)
                .Include(tu => tu.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(tu => 
                    tu.UserId == userId && 
                    tu.TenantId == PLATFORM_TENANT_ID && 
                    tu.IsActive);

            if (platformUser == null)
            {
                return false;
            }

            return platformUser.UserRoles.Any(ur => ur.Role.Name == "Admin");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking platform admin status for user {UserId}", userId);
            return false;
        }
    }

    public Guid GetPlatformTenantId()
    {
        return PLATFORM_TENANT_ID;
    }
}