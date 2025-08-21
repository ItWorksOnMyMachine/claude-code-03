using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Models.Admin;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/audit-logs")]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(AuthDbContext context, ILogger<AuditLogController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<AuditLogListResponse>> GetAuditLogs(
            [FromQuery] string? userId = null,
            [FromQuery] string? eventType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) return BadRequest("Page must be at least 1");
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = _context.AuthenticationAuditLogs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(e => e.UserId == userId);

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                if (Enum.TryParse<AuthenticationEventType>(eventType, out var eventTypeEnum))
                    query = query.Where(e => e.EventType == eventTypeEnum);
            }

            if (startDate.HasValue)
                query = query.Where(e => e.Timestamp >= startDate.Value);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.AddDays(1);
                query = query.Where(e => e.Timestamp < endOfDay);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(e => e.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new AuditLogEntry
                {
                    Id = e.Id,
                    UserId = e.UserId ?? string.Empty,
                    UserEmail = e.UserEmail ?? string.Empty,
                    EventType = e.EventType.ToString(),
                    Timestamp = e.Timestamp,
                    IpAddress = e.IpAddress,
                    UserAgent = e.UserAgent,
                    Success = e.Success,
                    FailureReason = e.FailureReason,
                    AdditionalData = e.AdditionalData
                })
                .ToListAsync();

            return Ok(new AuditLogListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<AuditLogStatistics>> GetAuditLogStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.AuthenticationAuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.Timestamp < endDate.Value.AddDays(1));

            var events = await query.ToListAsync();

            var stats = new AuditLogStatistics
            {
                TotalLogins = events.Count(e => e.EventType == AuthenticationEventType.LoginSuccess),
                FailedLoginAttempts = events.Count(e => e.EventType == AuthenticationEventType.LoginFailed),
                UniqueUsers = events.Where(e => !string.IsNullOrEmpty(e.UserId))
                    .Select(e => e.UserId)
                    .Distinct()
                    .Count(),
                AccountLockouts = events.Count(e => e.EventType == AuthenticationEventType.AccountLocked)
            };

            // Count by event type
            stats.EventTypeCounts = events
                .GroupBy(e => e.EventType.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Hourly activity (last 24 hours)
            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);
            var hourlyEvents = events
                .Where(e => e.Timestamp >= last24Hours)
                .ToList();

            stats.HourlyActivity = Enumerable.Range(0, 24)
                .Select(hour =>
                {
                    var hourStart = last24Hours.AddHours(hour);
                    var hourEnd = hourStart.AddHours(1);
                    var hourEvents = hourlyEvents
                        .Where(e => e.Timestamp >= hourStart && e.Timestamp < hourEnd)
                        .ToList();

                    return new HourlyActivity
                    {
                        Hour = hourStart.Hour,
                        LoginCount = hourEvents.Count(e => e.EventType == AuthenticationEventType.LoginSuccess),
                        FailureCount = hourEvents.Count(e => e.EventType == AuthenticationEventType.LoginFailed)
                    };
                })
                .ToList();

            return Ok(stats);
        }

        [HttpGet("suspicious")]
        public async Task<ActionResult<List<SuspiciousActivity>>> GetSuspiciousActivity(
            [FromQuery] int threshold = 5,
            [FromQuery] int hours = 24)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);

            var failedAttempts = await _context.AuthenticationAuditLogs
                .Where(e => e.EventType == AuthenticationEventType.LoginFailed &&
                           e.Timestamp >= cutoffTime &&
                           !string.IsNullOrEmpty(e.IpAddress))
                .ToListAsync();

            var suspiciousActivities = failedAttempts
                .GroupBy(e => e.IpAddress)
                .Where(g => g.Count() >= threshold)
                .Select(g => new SuspiciousActivity
                {
                    IpAddress = g.Key!,
                    FailedAttempts = g.Count(),
                    FirstAttempt = g.Min(e => e.Timestamp),
                    LastAttempt = g.Max(e => e.Timestamp),
                    TargetedUsers = g.Where(e => !string.IsNullOrEmpty(e.UserEmail))
                        .Select(e => e.UserEmail!)
                        .Distinct()
                        .ToList()
                })
                .OrderByDescending(s => s.FailedAttempts)
                .ToList();

            return Ok(suspiciousActivities);
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportAuditLogs(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string format = "csv")
        {
            if (format.ToLower() != "csv")
                return BadRequest("Only CSV format is currently supported");

            var query = _context.AuthenticationAuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.Timestamp < endDate.Value.AddDays(1));

            var events = await query
                .OrderByDescending(e => e.Timestamp)
                .Take(10000) // Limit export size
                .Select(e => new
                {
                    e.Id,
                    e.UserId,
                    e.UserEmail,
                    EventType = e.EventType.ToString(),
                    e.Timestamp,
                    e.IpAddress,
                    e.UserAgent,
                    e.Success,
                    e.FailureReason,
                    e.ClientId,
                    e.SessionId
                })
                .ToListAsync();

            var csv = new StringBuilder();
            using (var writer = new StringWriter(csv))
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                await csvWriter.WriteRecordsAsync(events);
            }

            var fileName = $"audit-logs-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.csv";
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            return File(bytes, "text/csv", fileName);
        }
    }
}