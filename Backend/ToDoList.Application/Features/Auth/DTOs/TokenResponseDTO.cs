using System;

namespace ToDoList.Application.Features.Auth.DTOs
{
    public class TokenResponseDTO
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
    }

    public class SendOtpRequestDTO
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyOtpRequestDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class RefreshTokenRequestDTO
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RevokeTokenRequestDTO
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
