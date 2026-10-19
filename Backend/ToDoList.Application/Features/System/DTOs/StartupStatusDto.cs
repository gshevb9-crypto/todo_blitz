using System;

namespace ToDoList.Application.Features.System.DTOs
{
    public class StartupStatusDto
    {
        public string Status { get; set; } = "Ready";
        public string Service { get; set; } = "ToDo & Notes API";
        public string Database { get; set; } = "Ready";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
