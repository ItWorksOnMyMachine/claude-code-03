using AuthService.Data;
using AuthService.Data.Entities;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthService.IdentityServer;

/// <summary>
/// Custom profile service that adds standard user claims to tokens
/// </summary>
public class AppProfileService : IProfileService
{
    private readonly AuthDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public AppProfileService(
        UserManager<AppUser> userManager,
        AuthDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        // Initialize claims list if needed
        context.IssuedClaims = context.IssuedClaims ?? new List<Claim>();
        
        // Get the user ID from the subject claim
        var sub = context.Subject?.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub))
        {
            return;
        }

        // Load the user
        // Try UserManager first, then fallback to direct DB query for testing
        var user = await _userManager.FindByIdAsync(sub);
        if (user == null)
        {
            // Fallback for testing scenarios where UserManager might not be configured properly
            user = await _context.Users.FirstOrDefaultAsync(u => u.Id == sub);
            if (user == null)
            {
                return;
            }
        }

        // Get requested claim types (default to all if not specified)
        var requestedClaims = context.RequestedClaimTypes?.ToList() ?? new List<string>();
        bool includeAllClaims = !requestedClaims.Any();

        // Add user-specific custom claims
        if ((includeAllClaims || requestedClaims.Contains("last_login")) && user.LastLoginAt.HasValue)
        {
            context.IssuedClaims.Add(new Claim("last_login", 
                user.LastLoginAt.Value.ToString("O"))); // ISO 8601 format
        }

        // Add user active status
        if (includeAllClaims || requestedClaims.Contains("is_active"))
        {
            context.IssuedClaims.Add(new Claim("is_active", user.IsActive.ToString().ToLower()));
        }

        // Get user roles for role claims
        if (includeAllClaims || requestedClaims.Contains("role"))
        {
            // Try to get roles via UserManager first
            var roles = await _userManager.GetRolesAsync(user);
            
            // If no roles found and we're in a testing scenario, try direct DB query
            if (!roles.Any())
            {
                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(_context.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (ur, r) => r.Name)
                    .ToListAsync();
                
                if (userRoles.Any())
                {
                    roles = userRoles.Where(r => r != null).ToList()!;
                }
            }
            
            foreach (var role in roles)
            {
                context.IssuedClaims.Add(new Claim("role", role));
            }
        }

        // Add email claim if requested and not already present
        if ((includeAllClaims || requestedClaims.Contains("email")) && 
            !context.IssuedClaims.Any(c => c.Type == "email"))
        {
            context.IssuedClaims.Add(new Claim("email", user.Email ?? string.Empty));
        }

        // Add name claim if requested and not already present
        if ((includeAllClaims || requestedClaims.Contains("name")) && 
            !context.IssuedClaims.Any(c => c.Type == "name"))
        {
            // Use FirstName + LastName if available, otherwise fall back to UserName
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = user.UserName ?? string.Empty;
            }
            context.IssuedClaims.Add(new Claim("name", fullName));
        }
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        // Default to inactive
        context.IsActive = false;

        // Get the user ID from the subject claim
        var sub = context.Subject?.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub))
        {
            context.IsActive = false;
            return;
        }

        // Load the user - try UserManager first, then fallback to direct DB query
        var user = await _userManager.FindByIdAsync(sub);
        if (user == null)
        {
            // Fallback for testing scenarios where UserManager might not be configured properly
            user = await _context.Users.FirstOrDefaultAsync(u => u.Id == sub);
            if (user == null)
            {
                context.IsActive = false;
                return;
            }
        }

        // Check if user is active
        if (!user.IsActive)
        {
            context.IsActive = false;
            return;
        }


        // Check if password has expired (if applicable)
        if (user.PasswordExpiresAt.HasValue && user.PasswordExpiresAt.Value < DateTime.UtcNow)
        {
            // Password expired - user needs to reset
            // Note: You might want to allow certain flows even with expired password
            // For now, we'll mark as inactive
            context.IsActive = false;
            return;
        }

        // User is active
        context.IsActive = true;
    }
}