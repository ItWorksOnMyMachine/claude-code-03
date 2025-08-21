using System;
using System.Collections.Generic;

namespace AuthService.Models.Admin
{
    public class AuditLogListResponse
    {
        public List<AuditLogEntry> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class AuditLogEntry
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
        public string? AdditionalData { get; set; }
    }

    public class AuditLogStatistics
    {
        public int TotalLogins { get; set; }
        public int FailedLoginAttempts { get; set; }
        public int UniqueUsers { get; set; }
        public int AccountLockouts { get; set; }
        public Dictionary<string, int> EventTypeCounts { get; set; } = new();
        public List<HourlyActivity> HourlyActivity { get; set; } = new();
    }

    public class HourlyActivity
    {
        public int Hour { get; set; }
        public int LoginCount { get; set; }
        public int FailureCount { get; set; }
    }

    public class SuspiciousActivity
    {
        public string IpAddress { get; set; } = string.Empty;
        public int FailedAttempts { get; set; }
        public DateTime FirstAttempt { get; set; }
        public DateTime LastAttempt { get; set; }
        public List<string> TargetedUsers { get; set; } = new();
    }
}