using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDoList.Domain.Entities
{
    public class TabContent : BaseEntity<Guid>
    {
        public Guid TabId { get; set; }

        public string TextContent { get; set; } = string.Empty;

        // Navigation Property
        [ForeignKey("TabId")]
        public Tab Tab { get; set; } = null!;
    }
}
