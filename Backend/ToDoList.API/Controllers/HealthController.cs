using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ToDoList.Infrastructure.Persistence.Context;

namespace ToDoList.API.Controllers
{
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HealthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("/")]
        [HttpGet("health")]
        [HttpGet("api/health")]
        public async Task<IActionResult> GetHealth(System.Threading.CancellationToken cancellationToken = default)
        {
            var isDbConnected = false;
            string? dbError = null;

            try
            {
                isDbConnected = await _context.Database.CanConnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                dbError = ex.Message;
            }

            var status = isDbConnected ? "Healthy" : "Degraded";

            return Ok(new
            {
                status,
                service = "ToDo & Notes API",
                timestamp = DateTime.UtcNow.ToString("O"),
                database = isDbConnected ? "Connected" : "Disconnected",
                details = dbError
            });
        }
    }
}
