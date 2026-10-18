using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToDoList.API.Helpers;
using ToDoList.Application.Common.Models;
using ToDoList.Application.Features.Auth.Commands.GoogleLogin;
using ToDoList.Application.Features.Auth.Commands.Logout;
using ToDoList.Application.Features.Auth.Commands.RefreshToken;
using ToDoList.Application.Features.Auth.Commands.SendOTP;
using ToDoList.Application.Features.Auth.Commands.VerifyOTP;
using ToDoList.Application.Features.Auth.DTOs;
using ToDoList.Domain.Entities;

namespace ToDoList.API.Controllers
{
    public class AuthController : BaseApiController
    {
        private readonly IMediator _mediator;
        private readonly CurrentUser _currentUser;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IMediator mediator,
            CurrentUser currentUser,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _mediator = mediator;
            _currentUser = currentUser;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("send-verification-code")]
        public async Task<IActionResult> SendVerificationCode([FromBody] SendOtpRequestDTO request)
        {
            var command = new SendOTPCommand { Email = request.Email };
            var result = await _mediator.Send(command);

            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<object>.SuccessResponse(new { message = "تم إرسال كود التحقق بنجاح." }));
        }

        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyOtpRequestDTO request)
        {
            var command = new VerifyOTPCommand
            {
                Email = request.Email,
                Code = request.Code,
                IpAddress = _currentUser.IpAddress
            };

            var result = await _mediator.Send(command);

            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<TokenResponseDTO>.SuccessResponse(result.Value, "تم التحقق وتسجيل الدخول بنجاح."));
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin([FromQuery] string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return BadRequest(ApiResponse<object>.FailureResponse("رابط العودة (returnUrl) مطلوب."));
            }

            var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl!);

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return BadRequest("رابط العودة مطلوب.");
            }

            var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (!authenticateResult.Succeeded)
            {
                return Redirect($"{returnUrl}?error={Uri.EscapeDataString("فشل تسجيل الدخول عبر Google.")}");
            }

            var command = new GoogleLoginCommand
            {
                IpAddress = _currentUser.IpAddress
            };

            var result = await _mediator.Send(command);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            if (result.IsError)
            {
                return Redirect($"{returnUrl}?error={Uri.EscapeDataString(result.FirstError.Description)}");
            }

            var token = result.Value;
            var callbackUrl = $"{returnUrl}" +
                $"?accessToken={Uri.EscapeDataString(token.AccessToken)}" +
                $"&refreshToken={Uri.EscapeDataString(token.RefreshToken)}" +
                $"&accessTokenExpiresAt={Uri.EscapeDataString(token.AccessTokenExpiresAt.ToString("O"))}" +
                $"&refreshTokenExpiresAt={Uri.EscapeDataString(token.RefreshTokenExpiresAt.ToString("O"))}";

            return Redirect(callbackUrl);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO request)
        {
            var command = new RefreshTokenCommand
            {
                RefreshToken = request.RefreshToken,
                IpAddress = _currentUser.IpAddress
            };

            var result = await _mediator.Send(command);

            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<TokenResponseDTO>.SuccessResponse(result.Value, "تم تجديد التوكن بنجاح."));
        }

        [HttpPost("revoke-refresh")]
        public async Task<IActionResult> RevokeRefresh([FromBody] RevokeTokenRequestDTO request)
        {
            var command = new LogoutCommand
            {
                UserId = _currentUser.UserId ?? string.Empty,
                RefreshToken = request.RefreshToken
            };

            var result = await _mediator.Send(command);
            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<object>.SuccessResponse(new { message = "تم إلغاء توكن التجديد بنجاح." }));
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var command = new LogoutCommand
            {
                UserId = _currentUser.UserId ?? string.Empty
            };

            var result = await _mediator.Send(command);
            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<object>.SuccessResponse(new { message = "تم تسجيل الخروج بنجاح." }));
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUserProfile()
        {
            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                userId = _currentUser.UserId,
                email = _currentUser.Email,
                name = _currentUser.Name,
                isAuthenticated = _currentUser.IsAuthenticated
            }));
        }
    }
}
