using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ToDoList.Application.Common.Interfaces;
using ToDoList.Application.Features.Auth.DTOs;
using ToDoList.Domain.Entities;
using ToDoList.Domain.Repositories;

namespace ToDoList.Application.Features.Auth.Commands.GoogleLogin
{
    public class GoogleLoginCommand : IRequest<ErrorOr<TokenResponseDTO>>
    {
        public string? IpAddress { get; set; }
    }

    public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, ErrorOr<TokenResponseDTO>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly ILogger<GoogleLoginCommandHandler> _logger;

        public GoogleLoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUnitOfWork unitOfWork,
            IJwtService jwtService,
            ILogger<GoogleLoginCommandHandler> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<ErrorOr<TokenResponseDTO>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return Error.Failure("auth.google.info.not.found", "معلومات تسجيل الدخول عبر Google غير متوفرة.");
                }

                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

                if (user == null)
                {
                    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                    if (string.IsNullOrEmpty(email))
                    {
                        return Error.Failure("auth.google.email.not.provided", "لم يتم الحصول على البريد الإلكتروني من Google.");
                    }

                    user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0];
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = name,
                            EmailConfirmed = true,
                            IsActive = true
                        };

                        var createResult = await _userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            return Error.Failure("auth.google.user.creation.failed", "فشل إنشاء حساب المستخدم.");
                        }
                    }
                    else if (!user.EmailConfirmed)
                    {
                        user.EmailConfirmed = true;
                        await _userManager.UpdateAsync(user);
                    }

                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (!addLoginResult.Succeeded)
                    {
                        return Error.Failure("auth.google.login.link.failed", "فشل ربط الحساب بـ Google.");
                    }
                }

                if (!user.IsActive)
                {
                    return Error.Unauthorized("auth.user.blocked", "حسابك موقوف. يرجى التواصل مع الدعم.");
                }

                var (accessToken, accessExp) = await _jwtService.GenerateAccessTokenAsync(user, _userManager);
                var (refreshToken, refreshExp) = _jwtService.GenerateRefreshToken();

                var refreshTokenRepo = _unitOfWork.Repository<RefreshTokenTable, int>();
                await refreshTokenRepo.AddAsync(new RefreshTokenTable
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = refreshExp,
                    CreatedByIp = request.IpAddress
                }, cancellationToken);

                await _unitOfWork.CompleteAsync(cancellationToken);

                return new TokenResponseDTO
                {
                    AccessToken = accessToken,
                    AccessTokenExpiresAt = accessExp,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiresAt = refreshExp,
                    Email = user.Email,
                    FullName = user.FullName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authenticate Google user");
                return Error.Unexpected("auth.google.unexpected_error", "حدث خطأ غير متوقع أثناء تسجيل الدخول بـ Google.");
            }
        }
    }
}
