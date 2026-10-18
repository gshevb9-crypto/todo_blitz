using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDoList.Domain.Entities
{
    public class Tab : BaseEntity<Guid>
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "Untitled Tab";

        [MaxLength(50)]
        public string? Icon { get; set; } = "📝";

        public int OrderIndex { get; set; } = 0;

        // Navigation Properties
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        public TabContent? Content { get; set; }

        public ICollection<TabImage> Images { get; set; } = new List<TabImage>();
    }
}
