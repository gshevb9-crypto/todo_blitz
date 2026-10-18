using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ToDoList.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public bool IsActive { get; set; } = true;

        [MaxLength(10)]
        public string? VerificationCode { get; set; }

        public DateTime? CodeExpiresAt { get; set; }

        // Navigation Properties
        public ICollection<RefreshTokenTable> RefreshTokens { get; set; } = new List<RefreshTokenTable>();

        public ICollection<Tab> Tabs { get; set; } = new List<Tab>();

        public ICollection<TabImage> Images { get; set; } = new List<TabImage>();

        public void SetVerificationCode(string code, DateTime expiresAt)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Verification code cannot be empty.", nameof(code));

            VerificationCode = code;
            CodeExpiresAt = expiresAt;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsVerificationCodeValid(string code, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(VerificationCode) || CodeExpiresAt == null)
                return false;

            if (utcNow > CodeExpiresAt.Value)
                return false;

            return string.Equals(VerificationCode, code, StringComparison.Ordinal);
        }

        public void ClearVerificationCode()
        {
            VerificationCode = null;
            CodeExpiresAt = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
