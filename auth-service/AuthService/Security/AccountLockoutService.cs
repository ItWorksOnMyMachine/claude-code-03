using System;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Security;

public class AccountLockoutService : IAccountLockoutService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<AccountLockoutService> _logger;
    private readonly LockoutOptions _options;

    public AccountLockoutService(
        AuthDbContext context,
        ILogger<AccountLockoutService> logger,
        IOptions<LockoutOptions> options)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<bool> RecordFailedAttemptAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Attempted to record failed attempt for non-existent user {UserId}", userId);
            return false;
        }

        user.AccessFailedCount++;
        
        // Check if account should be locked
        if (user.AccessFailedCount >= _options.MaxFailedAttempts)
        {
            var lockoutDuration = CalculateLockoutDuration(user.ConsecutiveLockouts);
            user.LockoutEnd = DateTimeOffset.UtcNow.Add(lockoutDuration);
            user.ConsecutiveLockouts++;
            
            _logger.LogWarning(
                "User {UserId} locked out for {Minutes} minutes after {Attempts} failed attempts. Consecutive lockouts: {ConsecutiveLockouts}",
                userId, lockoutDuration.TotalMinutes, user.AccessFailedCount, user.ConsecutiveLockouts);
        }

        await _context.SaveChangesAsync();
        return user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
    }

    public async Task ResetFailedAttemptsAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return;
        }

        user.AccessFailedCount = 0;
        
        // Reset consecutive lockouts if it's been more than 24 hours since last lockout
        if (user.LockoutEnd.HasValue && user.LockoutEnd < DateTimeOffset.UtcNow.AddHours(-24))
        {
            user.ConsecutiveLockouts = 0;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Reset failed attempts for user {UserId}", userId);
    }

    public async Task<bool> IsLockedOutAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        return user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
    }

    public async Task<TimeSpan?> GetTimeUntilUnlockAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.LockoutEnd.HasValue)
        {
            return null;
        }

        if (user.LockoutEnd <= DateTimeOffset.UtcNow)
        {
            return TimeSpan.Zero;
        }

        return user.LockoutEnd.Value - DateTimeOffset.UtcNow;
    }

    public async Task UnlockAccountAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return;
        }

        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Manually unlocked account for user {UserId}", userId);
    }

    public async Task<LockoutStatus> GetLockoutStatusAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return new LockoutStatus { IsLockedOut = false };
        }

        var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        
        return new LockoutStatus
        {
            IsLockedOut = isLockedOut,
            LockoutEnd = user.LockoutEnd,
            FailedAttempts = user.AccessFailedCount,
            ConsecutiveLockouts = user.ConsecutiveLockouts,
            TimeUntilUnlock = isLockedOut ? user.LockoutEnd!.Value - DateTimeOffset.UtcNow : null
        };
    }

    private TimeSpan CalculateLockoutDuration(int consecutiveLockouts)
    {
        // Progressive lockout: doubles with each consecutive lockout
        var minutes = _options.InitialLockoutMinutes * Math.Pow(_options.LockoutMultiplier, consecutiveLockouts);
        
        // Cap at maximum lockout duration
        minutes = Math.Min(minutes, _options.MaxLockoutMinutes);
        
        return TimeSpan.FromMinutes(minutes);
    }
}

public interface IAccountLockoutService
{
    Task<bool> RecordFailedAttemptAsync(string userId);
    Task ResetFailedAttemptsAsync(string userId);
    Task<bool> IsLockedOutAsync(string userId);
    Task<TimeSpan?> GetTimeUntilUnlockAsync(string userId);
    Task UnlockAccountAsync(string userId);
    Task<LockoutStatus> GetLockoutStatusAsync(string userId);
}

public class LockoutOptions
{
    public int MaxFailedAttempts { get; set; } = 5;
    public double InitialLockoutMinutes { get; set; } = 5;
    public double LockoutMultiplier { get; set; } = 2;
    public double MaxLockoutMinutes { get; set; } = 1440; // 24 hours
}

public class LockoutStatus
{
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int FailedAttempts { get; set; }
    public int ConsecutiveLockouts { get; set; }
    public TimeSpan? TimeUntilUnlock { get; set; }
}