using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ToDoList.API.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddAuthenticationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtKey = configuration["JWT:Key"]
                ?? "DefaultSuperSecretDevKeyForToDoListApplication123456!";
            var jwtIssuer = configuration["JWT:Issuer"] ?? "ToDoListApp";
            var jwtAudience = configuration["JWT:Audience"] ?? "ToDoListClient";

            var googleClientId = configuration["Authentication:Google:ClientId"]
                ?? configuration["Authentication__Google__ClientId"];
            var googleClientSecret = configuration["Authentication:Google:ClientSecret"]
                ?? configuration["Authentication__Google__ClientSecret"];

            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            // External cookie for Google OAuth flow
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.ExternalScheme, options =>
            {
                options.Cookie.Name = "Identity.External";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            });

            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    options.SignInScheme = IdentityConstants.ExternalScheme;
                });
            }

            return services;
        }
    }
}
