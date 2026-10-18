using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ToDoList.Application.Common.Exceptions;
using ToDoList.Application.Common.Models;

namespace ToDoList.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException valEx)
            {
                _logger.LogWarning("Validation failure: {Message}", valEx.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                var errorsDict = new System.Collections.Generic.Dictionary<string, string[]>(valEx.Errors);
                var response = ApiResponse<object>.FailureResponse("Validation failed", errorsDict);
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled server error occurred: {Message}", ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var response = ApiResponse<object>.FailureResponse("An unexpected server error occurred.");
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}
