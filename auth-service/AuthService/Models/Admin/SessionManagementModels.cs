using System;
using System.Collections.Generic;

namespace AuthService.Models.Admin
{
    public class SessionListResponse
    {
        public List<SessionSummary> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class SessionSummary
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? ClientId { get; set; }
        public string? ClientName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivity { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsActive { get; set; }
    }

    public class SessionDetailResponse : SessionSummary
    {
        public Dictionary<string, string> Claims { get; set; } = new();
        public Dictionary<string, string> Properties { get; set; } = new();
        public List<string> Scopes { get; set; } = new();
    }

    public class SessionRevocationResult
    {
        public int RevokedCount { get; set; }
        public List<string> RevokedSessionIds { get; set; } = new();
    }

    public class ExtendSessionRequest
    {
        public int ExtensionMinutes { get; set; }
    }

    public class SessionExtensionResult
    {
        public DateTime NewExpiryTime { get; set; }
        public DateTime PreviousExpiryTime { get; set; }
    }

    public class SessionStatistics
    {
        public int TotalActiveSessions { get; set; }
        public int UniqueUsers { get; set; }
        public double AverageSessionDurationMinutes { get; set; }
        public Dictionary<string, int> SessionsByClient { get; set; } = new();
        public List<HourlySessionActivity> HourlyActivity { get; set; } = new();
    }

    public class HourlySessionActivity
    {
        public int Hour { get; set; }
        public int NewSessions { get; set; }
        public int ExpiredSessions { get; set; }
    }

    public class SessionCleanupResult
    {
        public int ExpiredSessionsRemoved { get; set; }
        public DateTime CleanupTimestamp { get; set; }
    }

    public class ConcurrentSessionsResponse
    {
        public List<UserConcurrentSessions> UserSessions { get; set; } = new();
    }

    public class UserConcurrentSessions
    {
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int SessionCount { get; set; }
        public List<SessionSummary> Sessions { get; set; } = new();
    }
}