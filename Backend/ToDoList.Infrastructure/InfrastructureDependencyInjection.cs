using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ToDoList.Application.Common.Interfaces;
using ToDoList.Domain.Repositories;
using ToDoList.Infrastructure.Authentication;
using ToDoList.Infrastructure.Email;
using ToDoList.Infrastructure.Persistence.Context;
using ToDoList.Infrastructure.Persistence.Repositories;
using ToDoList.Infrastructure.Storage.Options;
using ToDoList.Infrastructure.Storage.Services;

namespace ToDoList.Infrastructure
{
    public static class InfrastructureDependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            AddDatabaseContext(services, configuration);
            AddUnitOfWork(services);
            AddAuthenticationServices(services, configuration);
            AddEmailServices(services, configuration);
            AddFileStorageServices(services, configuration);

            return services;
        }

        private static void AddDatabaseContext(
            IServiceCollection services,
            IConfiguration configuration)
        {
            var isProduction = string.Equals(
                configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Production",
                StringComparison.OrdinalIgnoreCase);

            var databaseUrl = configuration["DATABASE_URL"]
                ?? configuration.GetConnectionString("DATABASE_URL")
                ?? configuration["POSTGRES_URL"];

            string rawConnectionString;

            if (isProduction)
            {
                if (string.IsNullOrWhiteSpace(databaseUrl))
                {
                    throw new InvalidOperationException(
                        "Production database configuration failed: DATABASE_URL is not set. " +
                        "In Blitz.cloud, connect your Managed PostgreSQL database or configure DATABASE_URL in Environment Variables.");
                }

                rawConnectionString = databaseUrl;
            }
            else
            {
                var defaultConn = configuration.GetConnectionString("DefaultConnection");

                if (!string.IsNullOrWhiteSpace(databaseUrl))
                {
                    rawConnectionString = databaseUrl;
                }
                else if (!string.IsNullOrWhiteSpace(defaultConn))
                {
                    rawConnectionString = defaultConn;
                }
                else
                {
                    // Fallback local connection string for development
                    rawConnectionString = "Host=localhost;Port=5432;Database=todolist_db;Username=postgres;Password=postgres";
                }
            }

            var connectionString = FormatPostgresConnectionString(rawConnectionString, isProduction);

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });
            });
        }

        private static string FormatPostgresConnectionString(string connectionString, bool isProduction)
        {
            if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(connectionString);
                var userInfo = uri.UserInfo?.Split(':', 2);
                var username = userInfo != null && userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
                var password = userInfo != null && userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var database = uri.AbsolutePath.Trim('/');

                var builder = new Npgsql.NpgsqlConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.Port > 0 ? uri.Port : 5432,
                    Username = username,
                    Password = password,
                    Database = database,
                    SslMode = Npgsql.SslMode.Prefer,
                    Timeout = 15,
                    CommandTimeout = 30
                };

                if (!string.IsNullOrWhiteSpace(uri.Query))
                {
                    var query = uri.Query.TrimStart('?').Split('&');
                    foreach (var param in query)
                    {
                        var kv = param.Split('=', 2);
                        if (kv.Length == 2)
                        {
                            var key = kv[0].Trim();
                            var val = Uri.UnescapeDataString(kv[1].Trim());
                            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                            {
                                if (Enum.TryParse<Npgsql.SslMode>(val, true, out var sslMode))
                                {
                                    builder.SslMode = sslMode;
                                }
                            }
                        }
                    }
                }

                return builder.ToString();
            }

            try
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                return builder.ToString();
            }
            catch
            {
                return connectionString;
            }
        }

        private static void AddUnitOfWork(IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void AddAuthenticationServices(
            IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<JwtOptions>(configuration.GetSection("JWT"));

            var jwtOptions = configuration.GetSection("JWT").Get<JwtOptions>();
            if (jwtOptions != null)
            {
                jwtOptions.Validate();
            }

            services.AddScoped<IJwtService, JwtService>();
        }

        private static void AddEmailServices(
            IServiceCollection services,
            IConfiguration configuration)
        {
            var emailSection = configuration.GetSection("Email").Exists()
                ? configuration.GetSection("Email")
                : configuration.GetSection("EmailSettings");

            services.Configure<EmailOptions>(options =>
            {
                if (emailSection.Exists())
                {
                    emailSection.Bind(options);
                }

                var host = configuration["Email__Host"] ?? configuration["SMTP_HOST"] ?? configuration["Email:Host"];
                if (!string.IsNullOrWhiteSpace(host)) options.Host = host.Trim();

                var portStr = configuration["Email__Port"] ?? configuration["SMTP_PORT"] ?? configuration["Email:Port"];
                if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr.Trim(), out var port)) options.Port = port;

                var user = configuration["Email__SmtpUsername"] ?? configuration["SMTP_USERNAME"] ?? configuration["Email:SmtpUsername"];
                if (!string.IsNullOrWhiteSpace(user)) options.SmtpUsername = user.Trim();

                var pass = configuration["Email__SmtpPassword"] ?? configuration["SMTP_PASSWORD"] ?? configuration["Email:SmtpPassword"];
                if (!string.IsNullOrWhiteSpace(pass)) options.SmtpPassword = pass.Trim();

                var sender = configuration["Email__SenderEmail"] ?? configuration["SMTP_FROM"] ?? configuration["Email:SenderEmail"];
                if (!string.IsNullOrWhiteSpace(sender)) options.SenderEmail = sender.Trim();

                var displayName = configuration["Email__SenderDisplayName"] ?? configuration["SMTP_DISPLAY_NAME"] ?? configuration["Email:SenderDisplayName"];
                if (!string.IsNullOrWhiteSpace(displayName)) options.SenderDisplayName = displayName.Trim();

                var sslStr = configuration["Email__EnableSsl"] ?? configuration["SMTP_SSL"] ?? configuration["Email:EnableSsl"];
                if (!string.IsNullOrWhiteSpace(sslStr) && bool.TryParse(sslStr.Trim(), out var ssl)) options.EnableSsl = ssl;

                var expStr = configuration["Email__ExpirationMinutes"] ?? configuration["Email:ExpirationMinutes"];
                if (!string.IsNullOrWhiteSpace(expStr) && int.TryParse(expStr.Trim(), out var exp)) options.ExpirationMinutes = exp;
            });

            services.AddScoped<IEmailConfiguration>(provider =>
                provider.GetRequiredService<IOptions<EmailOptions>>().Value);

            services.AddScoped<IEmailSender, EmailService>();
        }

        private static void AddFileStorageServices(
            IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<LocalStorageOptions>(configuration.GetSection("LocalStorage"));
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
        }
    }
}
