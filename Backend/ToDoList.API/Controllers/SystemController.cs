using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToDoList.Application.Common.Models;
using ToDoList.Application.Features.System.DTOs;
using ToDoList.Infrastructure.Persistence.Context;

namespace ToDoList.API.Controllers
{
    [Route("api/[controller]")]
    public class SystemController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemController> _logger;

        public SystemController(
            ApplicationDbContext context,
            ILogger<SystemController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Real startup readiness check that executes ASP.NET Core logic and verifies
        /// active PostgreSQL connectivity via a lightweight read-only query.
        /// Accessible before authentication to allow the frontend to wake sleeping backend containers.
        /// </summary>
        [HttpGet("startup")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStartupStatus(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation("Incoming startup readiness check request from IP: {ClientIp}", clientIp);

            try
            {
                // 1. Verify basic database connectivity
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                if (!canConnect)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "Startup readiness check: database connection check failed after {ElapsedMilliseconds} ms (IP: {ClientIp})",
                        stopwatch.ElapsedMilliseconds, clientIp);

                    return StatusCode(
                        StatusCodes.Status503ServiceUnavailable,
                        ApiResponse<StartupStatusDto>.FailureResponse("Database dependency is not available yet."));
                }

                // 2. Perform a lightweight read-only query against PostgreSQL
                await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "Startup readiness check completed successfully. Database verified in {ElapsedMilliseconds} ms (IP: {ClientIp})",
                    stopwatch.ElapsedMilliseconds, clientIp);

                var statusDto = new StartupStatusDto
                {
                    Status = "Ready",
                    Service = "ToDo & Notes API",
                    Database = "Ready",
                    Timestamp = DateTime.UtcNow
                };

                return Ok(ApiResponse<StartupStatusDto>.SuccessResponse(
                    statusDto,
                    "Backend application and database dependency are ready."));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "Startup readiness check failed due to dependency exception after {ElapsedMilliseconds} ms (IP: {ClientIp}): {ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, clientIp, ex.Message);

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<StartupStatusDto>.FailureResponse("Backend service or database dependency is not ready."));
            }
        }
    }
}
