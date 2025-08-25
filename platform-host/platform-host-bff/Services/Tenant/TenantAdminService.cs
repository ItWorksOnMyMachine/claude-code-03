using Microsoft.EntityFrameworkCore;
using PlatformBff.Data;
using PlatformBff.Data.Entities;
using PlatformBff.Models.Tenant;
using PlatformBff.Repositories;

namespace PlatformBff.Services.Tenant;

/// <summary>
/// Implementation of platform administration service for managing tenants
/// Requires platform admin privileges
/// </summary>
public class TenantAdminService : ITenantAdminService
{
    private readonly PlatformDbContext _context;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantAdminService> _logger;

    public TenantAdminService(
        PlatformDbContext context,
        ITenantRepository tenantRepository,
        ITenantUserRepository tenantUserRepository,
        ITenantService tenantService,
        ILogger<TenantAdminService> logger)
    {
        _context = context;
        _tenantRepository = tenantRepository;
        _tenantUserRepository = tenantUserRepository;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            // Platform admins can see all tenants
            var query = _context.Tenants
                .IgnoreQueryFilters()
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.Name);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var tenants = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TenantInfo
                {
                    Id = t.Id,
                    Name = t.DisplayName,
                    Description = t.Settings,
                    IsActive = t.IsActive,
                    IsPlatformTenant = t.IsPlatformTenant,
                    CreatedAt = t.CreatedAt.UtcDateTime
                })
                .ToListAsync();

            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tenants");
            throw;
        }
    }

    public async Task<TenantInfo> CreateTenantAsync(CreateTenantDto dto)
    {
        try
        {
            // Check for duplicate name/slug before proceeding
            var slug = dto.Name.ToLowerInvariant().Replace(" ", "-");
            
            var existingTenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == slug || t.DisplayName == dto.Name);
            
            if (existingTenant != null)
            {
                _logger.LogWarning("Attempt to create duplicate tenant with name {TenantName} / slug {Slug}", dto.Name, slug);
                throw new InvalidOperationException($"A tenant with the name '{dto.Name}' already exists");
            }
            
            // For compatibility with the admin controller, support CreateTenantRequest as well
            return await CreateTenantAsync(new CreateTenantRequest
            {
                Slug = slug,
                DisplayName = dto.Name,
                Settings = dto.Description
            });
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error creating tenant {TenantName}", dto.Name);
            throw;
        }
    }

    public async Task<TenantInfo> CreateTenantAsync(CreateTenantRequest request)
    {
        try
        {
            // Note: Duplicate check is already done in CreateTenantAsync(CreateTenantDto dto)
            // This method is internal and assumes validation has been done

            var tenant = new Data.Entities.Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Slug,
                Slug = request.Slug,
                DisplayName = request.DisplayName,
                Settings = request.Settings ?? "",
                IsActive = true,
                IsPlatformTenant = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.Tenants.Add(tenant);

            // Create default roles for the new tenant
            var adminRole = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Admin",
                DisplayName = "Administrator",
                Description = "Full access to tenant resources",
                IsSystemRole = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var userRole = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "User",
                DisplayName = "User",
                Description = "Standard user access",
                IsSystemRole = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.Roles.AddRange(adminRole, userRole);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new tenant {TenantId} ({TenantName})", tenant.Id, tenant.DisplayName);

            return new TenantInfo
            {
                Id = tenant.Id,
                Name = tenant.DisplayName,
                Description = tenant.Settings,
                IsActive = tenant.IsActive,
                IsPlatformTenant = tenant.IsPlatformTenant,
                CreatedAt = tenant.CreatedAt.UtcDateTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant {TenantSlug}", request.Slug);
            throw;
        }
    }

    public async Task<TenantUserInfo> AddUserToTenantAsync(Guid tenantId, AddUserToTenantRequest request)
    {
        try
        {
            var success = await AssignUserToTenantAsync(tenantId, request.UserId, request.Email, request.RoleName);
            if (success)
            {
                // Get the tenant name for the response
                var tenant = await _context.Tenants
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                return new TenantUserInfo
                {
                    AuthSubjectId = request.UserId,
                    Email = request.Email,
                    Roles = new List<string> { request.RoleName },
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            throw new InvalidOperationException("Failed to add user to tenant");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> AssignUserToTenantAsync(Guid tenantId, string userId, string email, string role)
    {
        try
        {
            // Verify tenant exists
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);

            if (tenant == null)
            {
                throw new ArgumentException($"Tenant {tenantId} not found");
            }

            // Check if user already in tenant
            var existingUser = await _context.TenantUsers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && tu.UserId == userId);

            if (existingUser != null)
            {
                if (existingUser.IsActive && !existingUser.IsDeleted)
                {
                    throw new InvalidOperationException($"User {userId} is already in tenant {tenantId}");
                }

                // Reactivate if previously removed
                existingUser.IsActive = true;
                existingUser.IsDeleted = false;
                existingUser.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                // Create new tenant user
                var tenantUser = new TenantUser
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TenantId = tenantId,
                    IsActive = true,
                    JoinedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _context.TenantUsers.Add(tenantUser);
                existingUser = tenantUser;
            }

            // Assign role if specified
            if (!string.IsNullOrEmpty(role))
            {
                var roleEntity = await _context.Roles
                    .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == role);

                if (roleEntity != null)
                {
                    var userRole = new UserRole
                    {
                        Id = Guid.NewGuid(),
                        TenantUserId = existingUser.Id,
                        RoleId = roleEntity.Id,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    _context.UserRoles.Add(userRole);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Added user {UserId} to tenant {TenantId}", userId, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to tenant {TenantId}", userId, tenantId);
            throw;
        }
    }

    public async Task<ImpersonationContext> ImpersonateTenantAsync(Guid tenantId, string adminUserId)
    {
        var context = await ImpersonateTenantAsync(adminUserId, tenantId);
        return new ImpersonationContext
        {
            TenantId = context.TenantId,
            TenantName = context.TenantName,
            ImpersonatingUserId = adminUserId,
            StartedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
    }

    public async Task<Models.Tenant.TenantContext> ImpersonateTenantAsync(string adminUserId, Guid tenantId)
    {
        try
        {
            // Verify tenant exists
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);

            if (tenant == null)
            {
                throw new ArgumentException($"Tenant {tenantId} not found");
            }

            // Create tenant context for impersonation
            var context = new Models.Tenant.TenantContext
            {
                TenantId = tenantId,
                TenantName = tenant.DisplayName,
                IsPlatformTenant = tenant.IsPlatformTenant
            };

            _logger.LogWarning("Platform admin {AdminUserId} started impersonating tenant {TenantId} ({TenantName})",
                adminUserId, tenantId, tenant.DisplayName);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error impersonating tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request)
    {
        try
        {
            var dto = new UpdateTenantDto
            {
                Name = request.DisplayName,
                Description = request.Settings,
                IsActive = request.IsActive
            };
            var result = await UpdateTenantAsync(tenantId, dto);
            return result != null;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<TenantInfo> UpdateTenantAsync(Guid tenantId, UpdateTenantDto dto)
    {
        try
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);

            if (tenant == null)
            {
                throw new ArgumentException($"Tenant {tenantId} not found");
            }

            if (!string.IsNullOrEmpty(dto.Name))
            {
                tenant.DisplayName = dto.Name;
            }

            if (dto.Description != null)
            {
                tenant.Settings = dto.Description;
            }

            if (dto.IsActive.HasValue)
            {
                tenant.IsActive = dto.IsActive.Value;
            }

            tenant.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated tenant {TenantId} ({TenantName})", tenantId, tenant.DisplayName);

            return new TenantInfo
            {
                Id = tenant.Id,
                Name = tenant.DisplayName,
                Description = tenant.Settings,
                IsActive = tenant.IsActive,
                IsPlatformTenant = tenant.IsPlatformTenant,
                CreatedAt = tenant.CreatedAt.UtcDateTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> DeactivateTenantAsync(Guid tenantId)
    {
        try
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);

            if (tenant == null)
            {
                return false;
            }

            if (tenant.IsPlatformTenant)
            {
                throw new InvalidOperationException("Cannot deactivate platform tenant");
            }

            tenant.IsActive = false;
            tenant.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning("Deactivated tenant {TenantId} ({TenantName})", tenantId, tenant.DisplayName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> RemoveUserFromTenantAsync(Guid tenantId, string userId)
    {
        try
        {
            var tenantUser = await _context.TenantUsers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && tu.UserId == userId && !tu.IsDeleted);

            if (tenantUser == null)
            {
                return false;
            }

            tenantUser.IsActive = false;
            tenantUser.IsDeleted = true;
            tenantUser.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed user {UserId} from tenant {TenantId}", userId, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from tenant {TenantId}", userId, tenantId);
            throw;
        }
    }

    public async Task<IEnumerable<TenantUserInfo>> GetTenantUsersAsync(Guid tenantId)
    {
        try
        {
            var users = await _context.TenantUsers
                .IgnoreQueryFilters()
                .Include(tu => tu.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(tu => tu.TenantId == tenantId && tu.IsActive && !tu.IsDeleted)
                .Select(tu => new TenantUserInfo
                {
                    Id = tu.Id,
                    AuthSubjectId = tu.UserId,
                    Email = tu.UserId, // This would normally come from identity provider
                    IsActive = tu.IsActive,
                    Roles = tu.UserRoles.Select(ur => ur.Role.Name).ToList(),
                    CreatedAt = tu.CreatedAt.UtcDateTime
                })
                .ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<TenantStatistics> GetTenantStatisticsAsync(Guid tenantId)
    {
        try
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .Include(t => t.TenantUsers)
                .ThenInclude(tu => tu.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);

            if (tenant == null)
            {
                throw new ArgumentException($"Tenant {tenantId} not found");
            }

            var activeUsers = tenant.TenantUsers.Where(tu => tu.IsActive && !tu.IsDeleted).ToList();
            var adminCount = activeUsers.Count(tu => 
                tu.UserRoles.Any(ur => ur.Role?.Name == "Admin"));

            return new TenantStatistics
            {
                TenantId = tenantId,
                TenantName = tenant.DisplayName,
                TotalUsers = activeUsers.Count,
                ActiveUsers = activeUsers.Count,
                AdminUsers = adminCount,
                CreatedAt = tenant.CreatedAt,
                LastActivity = tenant.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for tenant {TenantId}", tenantId);
            throw;
        }
    }
}