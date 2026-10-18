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

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("Startup.DatabaseInitialization");
    var context = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        logger.LogInformation("Starting database migration...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "Failed to apply database migrations. Application startup will continue. " +
            "Ensure PostgreSQL connectivity and credentials in DATABASE_URL.");
    }
}

app.UseSwaggerDocumentation();
app.UseApiMiddlewares();
app.MapControllers();

app.Run();
