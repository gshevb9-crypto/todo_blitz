using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDoList.Domain.Entities
{
    public class RefreshTokenTable : BaseEntity<int>
    {
        [Required]
        [MaxLength(256)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;

        [MaxLength(50)]
        public string? CreatedByIp { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
    }
}
