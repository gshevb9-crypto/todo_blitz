using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ToDoList.API.Extensions
{
    public static class CorsExtensions
    {
        public const string PolicyName = "CorsPolicy";

        public static IServiceCollection AddCorsPolicy(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var allowedUrls = configuration
                .GetSection("Frontend:AllowedBaseUrls")
                .Get<string[]>() ?? Array.Empty<string>();

            var defaultOrigins = new[]
            {
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:3000"
            };

            var origins = allowedUrls
                .Concat(defaultOrigins)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            services.AddCors(options =>
            {
                options.AddPolicy(PolicyName, builder =>
                {
                    builder.SetIsOriginAllowed(origin =>
                    {
                        if (origins.Any(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase)))
                        {
                            return true;
                        }

                        // Also permit Vercel preview and production subdomains
                        if (origin.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        return false;
                    })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                });
            });

            return services;
        }
    }
}
