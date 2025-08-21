using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Models.Admin
{
    public class UserListResponse
    {
        public List<UserSummary> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class UserSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool LockedOut { get; set; }
    }

    public class UserDetailResponse : UserSummary
    {
        public List<string> Roles { get; set; } = new();
        public Dictionary<string, string> Claims { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        public string? Name { get; set; }
        
        [Required]
        [MinLength(8)]
        public string TemporaryPassword { get; set; } = string.Empty;
        
        public bool MustChangePassword { get; set; } = true;
        
        public string[]? Roles { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
        public string[]? Roles { get; set; }
    }

    public class PasswordResetResponse
    {
        public string TemporaryPassword { get; set; } = string.Empty;
        public bool MustChangePassword { get; set; } = true;
        public DateTime ExpiresAt { get; set; }
    }
}