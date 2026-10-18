using System;
using System.Collections.Generic;
using System.Linq;

namespace ToDoList.Infrastructure.Authentication
{
    public class JwtOptions
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 60;
        public int RefreshTokenExpirationDays { get; set; } = 7;

        public void Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Key))
            {
                errors.Add("JWT:Key is required and cannot be empty. Minimum 32 characters required.");
            }
            else if (Key.Length < 32)
            {
                errors.Add("JWT:Key is too short. Minimum 32 characters required.");
            }

            if (string.IsNullOrWhiteSpace(Issuer))
            {
                errors.Add("JWT:Issuer is required and cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(Audience))
            {
                errors.Add("JWT:Audience is required and cannot be empty.");
            }

            if (AccessTokenExpirationMinutes <= 0)
            {
                errors.Add("JWT:AccessTokenExpirationMinutes must be greater than 0.");
            }

            if (RefreshTokenExpirationDays <= 0)
            {
                errors.Add("JWT:RefreshTokenExpirationDays must be greater than 0.");
            }

            if (errors.Any())
            {
                throw new InvalidOperationException($"JWT configuration is invalid:\n{string.Join("\n", errors)}");
            }
        }
    }
}
