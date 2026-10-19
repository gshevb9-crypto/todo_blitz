using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using ToDoList.API.Middleware;

namespace ToDoList.API.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseApiMiddlewares(this IApplicationBuilder app)
        {
            // 1. Forwarded Headers from PaaS edge router (Blitz.cloud / reverse proxy)
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                   ForwardedHeaders.XForwardedProto |
                                   ForwardedHeaders.XForwardedHost
            };
            forwardedHeadersOptions.KnownNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);

            // 2. Exception Middleware
            app.UseMiddleware<ExceptionMiddleware>();

            // 3. Static Files for uploaded user media (/files/...)
            var filesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files");
            if (!Directory.Exists(filesPath))
            {
                Directory.CreateDirectory(filesPath);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(filesPath),
                RequestPath = "/files"
            });

            // 4. Standard ASP.NET Core pipeline
            app.UseRouting();
            app.UseCors(CorsExtensions.PolicyName);
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }
}
