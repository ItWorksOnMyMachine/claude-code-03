using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Security;

public class AuditLogService : IAuditLogService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AuthDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAuthenticationAsync(
        string userId,
        AuthenticationEventType eventType,
        string? ipAddress,
        string? userAgent,
        object? additionalData = null)
    {
        var log = new AuthenticationAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = IsSuccessEvent(eventType),
            AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
        };

        _context.AuthenticationAuditLogs.Add(log);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Authentication event logged: {EventType} for user {UserId}", eventType, userId);
    }

    public async Task<List<AuthenticationAuditLog>> GetUserAuthenticationHistoryAsync(
        string userId,
        int limit = 100)
    {
        return await _context.AuthenticationAuditLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetRecentFailedAttemptsAsync(
        string userId,
        string ipAddress,
        TimeSpan window)
    {
        var cutoff = DateTime.UtcNow.Subtract(window);
        
        return await _context.AuthenticationAuditLogs
            .CountAsync(log => 
                log.UserId == userId &&
                log.IpAddress == ipAddress &&
                log.EventType == AuthenticationEventType.LoginFailed &&
                log.Timestamp >= cutoff);
    }

    public async Task<List<SuspiciousActivity>> GetSuspiciousActivityAsync(
        TimeSpan window)
    {
        var cutoff = DateTime.UtcNow.Subtract(window);
        
        var suspiciousIps = await _context.AuthenticationAuditLogs
            .Where(log => 
                log.EventType == AuthenticationEventType.LoginFailed &&
                log.Timestamp >= cutoff)
            .GroupBy(log => log.IpAddress)
            .Where(g => g.Count() >= 5) // 5 or more failed attempts
            .Select(g => new SuspiciousActivity
            {
                IpAddress = g.Key ?? "Unknown",
                FailedAttempts = g.Count(),
                FirstAttempt = g.Min(log => log.Timestamp),
                LastAttempt = g.Max(log => log.Timestamp),
                UniqueUsers = g.Select(log => log.UserId).Distinct().Count()
            })
            .ToListAsync();

        return suspiciousIps;
    }

    public async Task LogPasswordChangeAsync(
        string userId,
        string? ipAddress,
        string? userAgent)
    {
        await LogAuthenticationAsync(
            userId,
            AuthenticationEventType.PasswordChanged,
            ipAddress,
            userAgent);
    }

    public async Task LogAccountLockoutAsync(
        string userId,
        TimeSpan lockoutDuration,
        string? ipAddress,
        string? userAgent)
    {
        await LogAuthenticationAsync(
            userId,
            AuthenticationEventType.AccountLocked,
            ipAddress,
            userAgent,
            new { LockoutMinutes = lockoutDuration.TotalMinutes });
    }

    public async Task<int> CleanupOldLogsAsync(int retentionDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        
        var oldLogs = await _context.AuthenticationAuditLogs
            .Where(log => log.Timestamp < cutoff)
            .ToListAsync();

        if (oldLogs.Any())
        {
            _context.AuthenticationAuditLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} audit logs older than {Days} days", 
                oldLogs.Count, retentionDays);
        }

        return oldLogs.Count;
    }

    public async Task<Dictionary<AuthenticationEventType, int>> GetEventStatisticsAsync(
        DateTime startDate,
        DateTime endDate)
    {
        return await _context.AuthenticationAuditLogs
            .Where(log => 
                log.Timestamp >= startDate &&
                log.Timestamp <= endDate)
            .GroupBy(log => log.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventType, x => x.Count);
    }

    public async Task<List<AuthenticationAuditLog>> GetAuditLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        AuthenticationEventType? eventType = null,
        string? userId = null,
        int limit = 1000)
    {
        var query = _context.AuthenticationAuditLogs.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(log => log.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(log => log.Timestamp <= endDate.Value);

        if (eventType.HasValue)
            query = query.Where(log => log.EventType == eventType.Value);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(log => log.UserId == userId);

        return await query
            .OrderByDescending(log => log.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    private bool IsSuccessEvent(AuthenticationEventType eventType)
    {
        return eventType switch
        {
            AuthenticationEventType.LoginSuccess => true,
            AuthenticationEventType.Logout => true,
            AuthenticationEventType.PasswordChanged => true,
            AuthenticationEventType.PasswordReset => true,
            AuthenticationEventType.AccountUnlocked => true,
            AuthenticationEventType.TokenRefreshed => true,
            AuthenticationEventType.MfaEnabled => true,
            AuthenticationEventType.MfaDisabled => true,
            AuthenticationEventType.MfaChallengeSuccess => true,
            _ => false
        };
    }
}

public interface IAuditLogService
{
    Task LogAuthenticationAsync(string userId, AuthenticationEventType eventType, 
        string? ipAddress, string? userAgent, object? additionalData = null);
    Task<List<AuthenticationAuditLog>> GetUserAuthenticationHistoryAsync(string userId, int limit = 100);
    Task<int> GetRecentFailedAttemptsAsync(string userId, string ipAddress, TimeSpan window);
    Task<List<SuspiciousActivity>> GetSuspiciousActivityAsync(TimeSpan window);
    Task LogPasswordChangeAsync(string userId, string? ipAddress, string? userAgent);
    Task LogAccountLockoutAsync(string userId, TimeSpan lockoutDuration, string? ipAddress, string? userAgent);
    Task<int> CleanupOldLogsAsync(int retentionDays);
    Task<Dictionary<AuthenticationEventType, int>> GetEventStatisticsAsync(DateTime startDate, DateTime endDate);
    Task<List<AuthenticationAuditLog>> GetAuditLogsAsync(DateTime? startDate = null, 
        DateTime? endDate = null, AuthenticationEventType? eventType = null, string? userId = null, int limit = 1000);
}

public class SuspiciousActivity
{
    public string IpAddress { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTime FirstAttempt { get; set; }
    public DateTime LastAttempt { get; set; }
    public int UniqueUsers { get; set; }
}