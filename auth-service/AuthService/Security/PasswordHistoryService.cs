using System;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Security;

public class PasswordHistoryService : IPasswordHistoryService
{
    private readonly AuthDbContext _context;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ILogger<PasswordHistoryService> _logger;
    private readonly PasswordHistoryOptions _options;

    public PasswordHistoryService(
        AuthDbContext context,
        IPasswordHasher<AppUser> passwordHasher,
        ILogger<PasswordHistoryService> logger,
        IOptions<PasswordHistoryOptions> options)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _options = options.Value;
    }

    public async Task AddPasswordToHistoryAsync(string userId, string passwordHash)
    {
        var history = new PasswordHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordHistories.Add(history);
        await _context.SaveChangesAsync();

        // Clean up old passwords beyond the history limit
        await TrimPasswordHistoryAsync(userId);
        
        _logger.LogInformation("Added password to history for user {UserId}", userId);
    }

    public async Task<bool> IsPasswordInHistoryAsync(string userId, string plainPassword)
    {
        if (_options.HistoryCount <= 0)
        {
            return false; // Password history is disabled
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        // Get recent password hashes
        var recentPasswords = await _context.PasswordHistories
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(_options.HistoryCount)
            .Select(p => p.PasswordHash)
            .ToListAsync();

        // Check current password
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            var currentResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, plainPassword);
            if (currentResult == PasswordVerificationResult.Success)
            {
                return true;
            }
        }

        // Check historical passwords
        foreach (var historicalHash in recentPasswords)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, historicalHash, plainPassword);
            if (result == PasswordVerificationResult.Success)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<TimeSpan?> GetPasswordAgeAsync(string userId)
    {
        var latestPassword = await _context.PasswordHistories
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestPassword == null)
        {
            // Check if user has a password set
            var user = await _context.Users.FindAsync(userId);
            if (user?.LastPasswordChangeAt != null)
            {
                return DateTime.UtcNow - user.LastPasswordChangeAt.Value;
            }
            return null;
        }

        return DateTime.UtcNow - latestPassword.CreatedAt;
    }

    public async Task<bool> IsPasswordExpiredAsync(string userId)
    {
        if (_options.MaximumPasswordAge == TimeSpan.Zero)
        {
            return false; // Password expiration is disabled
        }

        var age = await GetPasswordAgeAsync(userId);
        if (!age.HasValue)
        {
            return false; // No password set
        }

        return age.Value > _options.MaximumPasswordAge;
    }

    public async Task<bool> CanChangePasswordAsync(string userId)
    {
        if (_options.MinimumPasswordAge == TimeSpan.Zero)
        {
            return true; // No minimum age requirement
        }

        var age = await GetPasswordAgeAsync(userId);
        if (!age.HasValue)
        {
            return true; // No password set yet
        }

        return age.Value >= _options.MinimumPasswordAge;
    }

    public async Task ClearPasswordHistoryAsync(string userId)
    {
        var histories = await _context.PasswordHistories
            .Where(p => p.UserId == userId)
            .ToListAsync();

        if (histories.Any())
        {
            _context.PasswordHistories.RemoveRange(histories);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleared password history for user {UserId}", userId);
        }
    }

    public async Task<DateTime?> GetPasswordExpirationDateAsync(string userId)
    {
        if (_options.MaximumPasswordAge == TimeSpan.Zero)
        {
            return null; // Password expiration is disabled
        }

        var latestPassword = await _context.PasswordHistories
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestPassword == null)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user?.LastPasswordChangeAt != null)
            {
                return user.LastPasswordChangeAt.Value.Add(_options.MaximumPasswordAge);
            }
            return null;
        }

        return latestPassword.CreatedAt.Add(_options.MaximumPasswordAge);
    }

    public async Task<int> GetPasswordHistoryCountAsync(string userId)
    {
        return await _context.PasswordHistories
            .CountAsync(p => p.UserId == userId);
    }

    private async Task TrimPasswordHistoryAsync(string userId)
    {
        if (_options.HistoryCount <= 0)
        {
            return;
        }

        var histories = await _context.PasswordHistories
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        if (histories.Count > _options.HistoryCount)
        {
            var toRemove = histories.Skip(_options.HistoryCount);
            _context.PasswordHistories.RemoveRange(toRemove);
            await _context.SaveChangesAsync();
        }
    }
}

public interface IPasswordHistoryService
{
    Task AddPasswordToHistoryAsync(string userId, string passwordHash);
    Task<bool> IsPasswordInHistoryAsync(string userId, string plainPassword);
    Task<TimeSpan?> GetPasswordAgeAsync(string userId);
    Task<bool> IsPasswordExpiredAsync(string userId);
    Task<bool> CanChangePasswordAsync(string userId);
    Task ClearPasswordHistoryAsync(string userId);
    Task<DateTime?> GetPasswordExpirationDateAsync(string userId);
    Task<int> GetPasswordHistoryCountAsync(string userId);
}

public class PasswordHistoryOptions
{
    public int HistoryCount { get; set; } = 5;
    public TimeSpan MinimumPasswordAge { get; set; } = TimeSpan.FromDays(1);
    public TimeSpan MaximumPasswordAge { get; set; } = TimeSpan.FromDays(90);
}