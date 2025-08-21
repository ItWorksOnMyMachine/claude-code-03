using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthService.Data;
using AuthService.Models.Admin;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthService.Data.Entities;

namespace AuthService.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/sessions")]
    [Authorize(Roles = "Admin")]
    public class SessionManagementController : ControllerBase
    {
        private readonly IPersistedGrantStore _persistedGrantStore;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<SessionManagementController> _logger;

        public SessionManagementController(
            IPersistedGrantStore persistedGrantStore,
            UserManager<AppUser> userManager,
            ILogger<SessionManagementController> logger)
        {
            _persistedGrantStore = persistedGrantStore;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<SessionListResponse>> GetSessions(
            [FromQuery] string? userId = null,
            [FromQuery] bool activeOnly = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) return BadRequest("Page must be at least 1");
            if (pageSize < 1 || pageSize > 100) return BadRequest("PageSize must be between 1 and 100");

            // Get all refresh tokens (which represent active sessions)
            var filter = new PersistedGrantFilter 
            { 
                Type = "refresh_token"
            };
            
            if (!string.IsNullOrWhiteSpace(userId))
                filter.SubjectId = userId;

            var allGrants = await _persistedGrantStore.GetAllAsync(filter);
            
            // Filter by active status if requested
            if (activeOnly)
            {
                allGrants = allGrants.Where(g => g.Expiration == null || g.Expiration > DateTime.UtcNow);
            }

            var totalCount = allGrants.Count();

            var grants = allGrants
                .OrderByDescending(g => g.CreationTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var sessions = new List<SessionSummary>();
            foreach (var grant in grants)
            {
                var user = await _userManager.FindByIdAsync(grant.SubjectId ?? "");
                sessions.Add(new SessionSummary
                {
                    SessionId = grant.Key,
                    UserId = grant.SubjectId ?? string.Empty,
                    UserEmail = user?.Email ?? string.Empty,
                    ClientId = grant.ClientId,
                    ClientName = grant.ClientId, // In real app, would lookup client name
                    CreatedAt = grant.CreationTime,
                    LastActivity = grant.CreationTime, // Would track actual activity in production
                    ExpiresAt = grant.Expiration ?? DateTime.UtcNow.AddDays(30),
                    IsActive = grant.Expiration == null || grant.Expiration > DateTime.UtcNow
                });
            }

            return Ok(new SessionListResponse
            {
                Items = sessions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("{sessionId}")]
        public async Task<ActionResult<SessionDetailResponse>> GetSession(string sessionId)
        {
            var grant = await _persistedGrantStore.GetAsync(sessionId);

            if (grant == null)
                return NotFound($"Session with ID '{sessionId}' not found");

            var user = await _userManager.FindByIdAsync(grant.SubjectId ?? "");
            
            var session = new SessionDetailResponse
            {
                SessionId = grant.Key,
                UserId = grant.SubjectId ?? string.Empty,
                UserEmail = user?.Email ?? string.Empty,
                ClientId = grant.ClientId,
                ClientName = grant.ClientId,
                CreatedAt = grant.CreationTime,
                LastActivity = grant.CreationTime,
                ExpiresAt = grant.Expiration ?? DateTime.UtcNow.AddDays(30),
                IsActive = grant.Expiration == null || grant.Expiration > DateTime.UtcNow,
                Claims = new Dictionary<string, string>(), // Would parse from grant.Data in production
                Properties = new Dictionary<string, string>(),
                Scopes = new List<string>() // Would parse from grant.Data
            };

            return Ok(session);
        }

        [HttpDelete("{sessionId}")]
        public async Task<IActionResult> RevokeSession(string sessionId)
        {
            var grant = await _persistedGrantStore.GetAsync(sessionId);

            if (grant == null)
                return NotFound($"Session with ID '{sessionId}' not found");

            await _persistedGrantStore.RemoveAsync(sessionId);

            _logger.LogInformation("Admin revoked session {SessionId} for user {UserId}", 
                sessionId, grant.SubjectId);

            return NoContent();
        }

        [HttpDelete("user/{userId}")]
        public async Task<ActionResult<SessionRevocationResult>> RevokeAllUserSessions(string userId)
        {
            var filter = new PersistedGrantFilter { SubjectId = userId };
            var grants = (await _persistedGrantStore.GetAllAsync(filter)).ToList();

            if (!grants.Any())
                return NotFound($"No sessions found for user '{userId}'");

            var sessionIds = grants.Select(g => g.Key).ToList();
            await _persistedGrantStore.RemoveAllAsync(filter);

            _logger.LogInformation("Admin revoked {Count} sessions for user {UserId}", 
                grants.Count, userId);

            return Ok(new SessionRevocationResult
            {
                RevokedCount = grants.Count,
                RevokedSessionIds = sessionIds
            });
        }

        [HttpPost("{sessionId}/extend")]
        public async Task<ActionResult<SessionExtensionResult>> ExtendSession(
            string sessionId, 
            [FromBody] ExtendSessionRequest request)
        {
            if (request.ExtensionMinutes <= 0)
                return BadRequest("Extension minutes must be positive");

            var grant = await _persistedGrantStore.GetAsync(sessionId);

            if (grant == null)
                return NotFound($"Session with ID '{sessionId}' not found");

            var previousExpiry = grant.Expiration ?? DateTime.UtcNow.AddDays(30);
            var newExpiry = DateTime.UtcNow.AddMinutes(request.ExtensionMinutes);
            
            grant.Expiration = newExpiry;
            await _persistedGrantStore.StoreAsync(grant);

            _logger.LogInformation("Admin extended session {SessionId} by {Minutes} minutes", 
                sessionId, request.ExtensionMinutes);

            return Ok(new SessionExtensionResult
            {
                PreviousExpiryTime = previousExpiry,
                NewExpiryTime = newExpiry
            });
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<SessionStatistics>> GetSessionStatistics()
        {
            var filter = new PersistedGrantFilter { Type = "refresh_token" };
            var allGrants = await _persistedGrantStore.GetAllAsync(filter);
            var activeGrants = allGrants
                .Where(g => g.Expiration == null || g.Expiration > DateTime.UtcNow)
                .ToList();

            var stats = new SessionStatistics
            {
                TotalActiveSessions = activeGrants.Count,
                UniqueUsers = activeGrants.Select(g => g.SubjectId).Distinct().Count(),
                AverageSessionDurationMinutes = 30 * 24 * 60, // Default 30 days in minutes
                SessionsByClient = activeGrants
                    .GroupBy(g => g.ClientId)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Hourly activity (simplified - would track actual creation/expiry in production)
            var now = DateTime.UtcNow;
            stats.HourlyActivity = Enumerable.Range(0, 24)
                .Select(hour => new HourlySessionActivity
                {
                    Hour = (now.AddHours(hour - 23)).Hour,
                    NewSessions = 0, // Would track actual new sessions
                    ExpiredSessions = 0 // Would track actual expired sessions
                })
                .ToList();

            return Ok(stats);
        }

        [HttpPost("cleanup")]
        public async Task<ActionResult<SessionCleanupResult>> CleanupExpiredSessions()
        {
            // Get all refresh tokens to check for expired ones
            // We need to check multiple grant types that could expire
            var grantTypes = new[] { "refresh_token", "authorization_code", "device_code", "reference_token" };
            var expiredGrants = new List<PersistedGrant>();
            
            foreach (var grantType in grantTypes)
            {
                var filter = new PersistedGrantFilter { Type = grantType };
                var grants = await _persistedGrantStore.GetAllAsync(filter);
                expiredGrants.AddRange(grants.Where(g => g.Expiration != null && g.Expiration < DateTime.UtcNow));
            }

            if (expiredGrants.Any())
            {
                foreach (var grant in expiredGrants)
                {
                    await _persistedGrantStore.RemoveAsync(grant.Key);
                }
            }

            _logger.LogInformation("Admin cleaned up {Count} expired sessions", expiredGrants.Count);

            return Ok(new SessionCleanupResult
            {
                ExpiredSessionsRemoved = expiredGrants.Count,
                CleanupTimestamp = DateTime.UtcNow
            });
        }

        [HttpGet("concurrent")]
        public async Task<ActionResult<ConcurrentSessionsResponse>> GetConcurrentSessions()
        {
            var filter = new PersistedGrantFilter { Type = "refresh_token" };
            var allGrants = await _persistedGrantStore.GetAllAsync(filter);
            var activeGrants = allGrants
                .Where(g => g.Expiration == null || g.Expiration > DateTime.UtcNow)
                .ToList();

            var userSessions = activeGrants
                .GroupBy(g => g.SubjectId)
                .Where(g => g.Count() > 1)
                .Select(async g =>
                {
                    var user = await _userManager.FindByIdAsync(g.Key ?? "");
                    return new UserConcurrentSessions
                    {
                        UserId = g.Key ?? string.Empty,
                        UserEmail = user?.Email ?? string.Empty,
                        SessionCount = g.Count(),
                        Sessions = g.Select(grant => new SessionSummary
                        {
                            SessionId = grant.Key,
                            UserId = grant.SubjectId ?? string.Empty,
                            UserEmail = user?.Email ?? string.Empty,
                            ClientId = grant.ClientId,
                            ClientName = grant.ClientId,
                            CreatedAt = grant.CreationTime,
                            LastActivity = grant.CreationTime,
                            ExpiresAt = grant.Expiration ?? DateTime.UtcNow.AddDays(30),
                            IsActive = true
                        }).ToList()
                    };
                });

            var results = await Task.WhenAll(userSessions);

            return Ok(new ConcurrentSessionsResponse
            {
                UserSessions = results.ToList()
            });
        }

        [HttpGet("client/{clientId}")]
        public async Task<ActionResult<SessionListResponse>> GetSessionsByClient(
            string clientId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) return BadRequest("Page must be at least 1");
            if (pageSize < 1 || pageSize > 100) return BadRequest("PageSize must be between 1 and 100");

            var filter = new PersistedGrantFilter { Type = "refresh_token", ClientId = clientId };
            var allGrants = await _persistedGrantStore.GetAllAsync(filter);

            var totalCount = allGrants.Count();

            var grants = allGrants
                .OrderByDescending(g => g.CreationTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var sessions = new List<SessionSummary>();
            foreach (var grant in grants)
            {
                var user = await _userManager.FindByIdAsync(grant.SubjectId ?? "");
                sessions.Add(new SessionSummary
                {
                    SessionId = grant.Key,
                    UserId = grant.SubjectId ?? string.Empty,
                    UserEmail = user?.Email ?? string.Empty,
                    ClientId = grant.ClientId,
                    ClientName = grant.ClientId,
                    CreatedAt = grant.CreationTime,
                    LastActivity = grant.CreationTime,
                    ExpiresAt = grant.Expiration ?? DateTime.UtcNow.AddDays(30),
                    IsActive = grant.Expiration == null || grant.Expiration > DateTime.UtcNow
                });
            }

            return Ok(new SessionListResponse
            {
                Items = sessions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
    }
}