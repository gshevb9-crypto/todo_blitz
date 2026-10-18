using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDoList.Domain.Entities
{
    public class TabImage : BaseEntity<Guid>
    {
        public Guid TabId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string OriginalFileName { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [MaxLength(100)]
        public string ContentType { get; set; } = "image/webp";

        [Required]
        [MaxLength(500)]
        public string RelativePath { get; set; } = string.Empty;

        // Navigation Properties
        [ForeignKey("TabId")]
        public Tab Tab { get; set; } = null!;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
    }
}
