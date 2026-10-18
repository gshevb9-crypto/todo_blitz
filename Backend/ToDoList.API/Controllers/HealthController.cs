using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            int? pendingMigrationsCount = null;

            try
            {
                isDbConnected = await _context.Database.CanConnectAsync(cancellationToken);
                if (isDbConnected)
                {
                    var pending = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
                    pendingMigrationsCount = System.Linq.Enumerable.Count(pending);
                }
            }
            catch (Exception ex)
            {
                dbError = ex.Message;
            }

            var status = isDbConnected && (pendingMigrationsCount ?? 0) == 0 ? "Healthy" : "Degraded";

            return Ok(new
            {
                status,
                service = "ToDo & Notes API",
                timestamp = DateTime.UtcNow.ToString("O"),
                database = isDbConnected ? "Connected" : "Disconnected",
                pendingMigrations = pendingMigrationsCount,
                details = dbError
            });
        }

        [HttpPost("api/health/migrate")]
        [HttpGet("api/health/migrate")]
        public async Task<IActionResult> RunMigration()
        {
            try
            {
                var pending = (await _context.Database.GetPendingMigrationsAsync()).ToList();
                await _context.Database.MigrateAsync();
                return Ok(new
                {
                    status = "Success",
                    message = "Database migrations applied successfully.",
                    appliedMigrations = pending
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Failed",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }
}
