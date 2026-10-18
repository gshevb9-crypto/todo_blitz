using System;
using System.ComponentModel.DataAnnotations;

namespace ToDoList.Domain.Entities
{
    public class LoginAttempt : BaseEntity<int>
    {
        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

        public bool IsSuccessful { get; set; }

        [MaxLength(50)]
        public string IpAddress { get; set; } = string.Empty;
    }
}
