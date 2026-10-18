using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ToDoList.Application.Common.Interfaces;
using ToDoList.Application.Features.Auth.DTOs;
using ToDoList.Domain.Entities;
using ToDoList.Domain.Repositories;

namespace ToDoList.Application.Features.Auth.Commands.VerifyOTP
{
    public class VerifyOTPCommand : IRequest<ErrorOr<TokenResponseDTO>>
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
    }

    public class VerifyOTPCommandValidator : AbstractValidator<VerifyOTPCommand>
    {
        public VerifyOTPCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("البريد الإلكتروني مطلوب.")
                .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("كود التحقق مطلوب.")
                .Length(6).WithMessage("كود التحقق يجب أن يتكون من 6 أرقام.");
        }
    }

    public class VerifyOTPCommandHandler : IRequestHandler<VerifyOTPCommand, ErrorOr<TokenResponseDTO>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;

        public VerifyOTPCommandHandler(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
        }

        public async Task<ErrorOr<TokenResponseDTO>> Handle(VerifyOTPCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Error.NotFound("auth.user.not.found", "المستخدم غير موجود.");
            }

            if (!user.IsActive)
            {
                return Error.Failure("auth.user.blocked", "حسابك موقوف. يرجى التواصل مع الدعم.");
            }

            if (!user.IsVerificationCodeValid(request.Code, DateTime.UtcNow))
            {
                if (user.CodeExpiresAt == null || user.CodeExpiresAt < DateTime.UtcNow)
                {
                    return Error.Failure("auth.otp.expired", "كود التحقق انتهت صلاحيته.");
                }

                return Error.Failure("auth.otp.invalid", "كود التحقق غير صحيح.");
            }

            user.EmailConfirmed = true;
            user.ClearVerificationCode();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return Error.Failure("auth.otp.verification.failed", "فشل التحقق من الكود.");
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
    }
}
