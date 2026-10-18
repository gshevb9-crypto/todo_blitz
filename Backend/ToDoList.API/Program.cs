using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToDoList.API.Extensions;
using ToDoList.API.Helpers;
using ToDoList.Application;
using ToDoList.Infrastructure;
using ToDoList.Infrastructure.Persistence.Context;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// Add Services
// ==========================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy(builder.Configuration);

// Context & Identity Accessors
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();

// Clean Architecture Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddAuthorization();

// ==========================================
// Build and Configure Middleware
// ==========================================

var app = builder.Build();

// Apply database migrations on startup with resilient retry loop for container cold-starts
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("Startup.DatabaseInitialization");
    var context = services.GetRequiredService<ApplicationDbContext>();

    const int maxRetries = 10;
    var delay = TimeSpan.FromSeconds(3);

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            logger.LogInformation("Starting database migration (Attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migration completed successfully.");
            break;
        }
        catch (Exception ex)
        {
            if (attempt == maxRetries)
            {
                logger.LogError(
                    ex,
                    "Failed to apply database migrations after {MaxRetries} attempts. Application startup will continue. " +
                    "Ensure PostgreSQL connectivity and credentials in DATABASE_URL.", maxRetries);
            }
            else
            {
                logger.LogWarning(
                    "Database not ready yet (Attempt {Attempt}/{MaxRetries}): {Message}. Retrying in {Delay}s...",
                    attempt, maxRetries, ex.Message, delay.TotalSeconds);
                await System.Threading.Tasks.Task.Delay(delay);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 1.5, 15));
            }
        }
    }
}

app.UseSwaggerDocumentation();
app.UseApiMiddlewares();
app.MapControllers();

app.Run();
