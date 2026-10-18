using System;
using System.Collections.Generic;

namespace ToDoList.Application.Features.Tabs.DTOs
{
    public class TabDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ImagesCount { get; set; }
    }

    public class TabDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string TextContent { get; set; } = string.Empty;
        public IReadOnlyList<TabImageDto> Images { get; set; } = new List<TabImageDto>();
    }

    public class TabContentDto
    {
        public Guid TabId { get; set; }
        public string TextContent { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }

    public class TabImageDto
    {
        public Guid Id { get; set; }
        public Guid TabId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
