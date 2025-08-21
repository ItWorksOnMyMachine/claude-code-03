using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class UserManagementController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly AuthDbContext _context;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            AuthDbContext context,
            ILogger<UserManagementController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<UserListResponse>> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) return BadRequest("Page must be at least 1");
            if (pageSize < 1 || pageSize > 100) return BadRequest("PageSize must be between 1 and 100");

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => 
                    u.Email.Contains(search) || 
                    (u.FirstName != null && u.FirstName.Contains(search)) ||
                    (u.LastName != null && u.LastName.Contains(search)));
            }

            var totalCount = await query.CountAsync();
            
            var users = await query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserSummary
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty,
                    Name = u.FirstName != null || u.LastName != null 
                        ? $"{u.FirstName} {u.LastName}".Trim() 
                        : null,
                    IsActive = u.IsActive,
                    LastLoginDate = u.LastLoginDate,
                    LockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
                })
                .ToListAsync();

            return Ok(new UserListResponse
            {
                Items = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDetailResponse>> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID '{id}' not found");

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            return Ok(new UserDetailResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Name = user.FirstName != null || user.LastName != null 
                    ? $"{user.FirstName} {user.LastName}".Trim() 
                    : null,
                IsActive = user.IsActive,
                LastLoginDate = user.LastLoginDate,
                LockedOut = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow,
                Roles = roles.ToList(),
                Claims = claims.ToDictionary(c => c.Type, c => c.Value),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<ActionResult<UserDetailResponse>> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return Conflict($"User with email '{request.Email}' already exists");

            // Parse name if provided
            string? firstName = null;
            string? lastName = null;
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var nameParts = request.Name.Split(' ', 2);
                firstName = nameParts[0];
                lastName = nameParts.Length > 1 ? nameParts[1] : null;
            }

            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            // Set temporary password change requirement
            if (request.MustChangePassword)
            {
                user.MustChangePassword = true;
                user.PasswordChangeRequiredDate = DateTime.UtcNow;
            }

            var result = await _userManager.CreateAsync(user, request.TemporaryPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest($"Failed to create user: {errors}");
            }

            // Add roles if specified
            if (request.Roles != null && request.Roles.Any())
            {
                // Validate all roles exist
                foreach (var roleName in request.Roles)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        await _userManager.DeleteAsync(user);
                        return BadRequest($"Role '{roleName}' does not exist");
                    }
                }

                var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    return BadRequest($"Failed to add roles: {errors}");
                }
            }

            _logger.LogInformation("Admin {AdminId} created new user {UserId} with email {Email}", 
                User.FindFirstValue(ClaimTypes.NameIdentifier), user.Id, user.Email);

            var response = new UserDetailResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Name = request.Name,
                IsActive = user.IsActive,
                Roles = request.Roles?.ToList() ?? new List<string>(),
                CreatedAt = user.CreatedAt
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserDetailResponse>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID '{id}' not found");

            // Update name if provided
            if (request.Name != null)
            {
                var nameParts = request.Name.Split(' ', 2);
                user.FirstName = nameParts[0];
                user.LastName = nameParts.Length > 1 ? nameParts[1] : null;
            }

            // Update active status if provided
            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;

            // Update roles if provided
            if (request.Roles != null)
            {
                // Validate all roles exist
                foreach (var roleName in request.Roles)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        return BadRequest($"Role '{roleName}' does not exist");
                    }
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                var rolesToRemove = currentRoles.Except(request.Roles).ToList();
                var rolesToAdd = request.Roles.Except(currentRoles).ToList();

                if (rolesToRemove.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (!removeResult.Succeeded)
                    {
                        var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                        return BadRequest($"Failed to remove roles: {errors}");
                    }
                }

                if (rolesToAdd.Any())
                {
                    var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                    if (!addResult.Succeeded)
                    {
                        var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                        return BadRequest($"Failed to add roles: {errors}");
                    }
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return BadRequest($"Failed to update user: {errors}");
            }

            _logger.LogInformation("Admin {AdminId} updated user {UserId}", 
                User.FindFirstValue(ClaimTypes.NameIdentifier), user.Id);

            return await GetUser(id);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID '{id}' not found");

            // Soft delete - just deactivate the user
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest($"Failed to deactivate user: {errors}");
            }

            _logger.LogInformation("Admin {AdminId} deactivated user {UserId}", 
                User.FindFirstValue(ClaimTypes.NameIdentifier), user.Id);

            return NoContent();
        }

        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID '{id}' not found");

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest($"Failed to unlock user: {errors}");
            }

            // Reset failed access count
            await _userManager.ResetAccessFailedCountAsync(user);

            _logger.LogInformation("Admin {AdminId} unlocked user {UserId}", 
                User.FindFirstValue(ClaimTypes.NameIdentifier), user.Id);

            return Ok();
        }

        [HttpPost("{id}/reset-password")]
        public async Task<ActionResult<PasswordResetResponse>> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID '{id}' not found");

            // Generate a secure temporary password
            var temporaryPassword = GenerateSecurePassword();
            
            // Remove current password
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                return BadRequest($"Failed to remove current password: {errors}");
            }

            // Set new temporary password
            var addResult = await _userManager.AddPasswordAsync(user, temporaryPassword);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return BadRequest($"Failed to set temporary password: {errors}");
            }

            // Mark that user must change password
            user.MustChangePassword = true;
            user.PasswordChangeRequiredDate = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Admin {AdminId} reset password for user {UserId}", 
                User.FindFirstValue(ClaimTypes.NameIdentifier), user.Id);

            return Ok(new PasswordResetResponse
            {
                TemporaryPassword = temporaryPassword,
                MustChangePassword = true,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            });
        }

        private string GenerateSecurePassword()
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%^&*()";
            var random = new Random();
            var chars = new char[16];
            
            // Ensure at least one of each required character type
            chars[0] = validChars[random.Next(0, 26)]; // Uppercase
            chars[1] = validChars[random.Next(26, 52)]; // Lowercase
            chars[2] = validChars[random.Next(52, 62)]; // Number
            chars[3] = validChars[random.Next(62, validChars.Length)]; // Symbol
            
            // Fill the rest randomly
            for (int i = 4; i < chars.Length; i++)
            {
                chars[i] = validChars[random.Next(validChars.Length)];
            }
            
            // Shuffle the array
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            
            return new string(chars);
        }
    }
}