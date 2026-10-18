using System;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ToDoList.Application.Common.Interfaces;
using ToDoList.Domain.Entities;

namespace ToDoList.Application.Features.Auth.Commands.SendOTP
{
    public class SendOTPCommand : IRequest<ErrorOr<Success>>
    {
        public string Email { get; set; } = string.Empty;
    }

    public class SendOTPCommandValidator : AbstractValidator<SendOTPCommand>
    {
        public SendOTPCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("البريد الإلكتروني مطلوب.")
                .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة.");
        }
    }

    public class SendOTPCommandHandler : IRequestHandler<SendOTPCommand, ErrorOr<Success>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IEmailConfiguration _emailConfiguration;
        private readonly ILogger<SendOTPCommandHandler> _logger;

        public SendOTPCommandHandler(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IEmailConfiguration emailConfiguration,
            ILogger<SendOTPCommandHandler> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _emailConfiguration = emailConfiguration;
            _logger = logger;
        }

        public async Task<ErrorOr<Success>> Handle(SendOTPCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Email = request.Email,
                        UserName = request.Email,
                        EmailConfirmed = false
                    };
                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning("Failed to create user account for email {Email}", request.Email);
                        return Error.Failure("auth.user.creation.failed", "فشل إنشاء حساب المستخدم.");
                    }
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Blocked user attempted OTP generation: {Email}", request.Email);
                    return Error.Failure("auth.user.blocked", "حسابك موقوف. يرجى التواصل مع الدعم.");
                }

                // Check for existing active code
                if (user.CodeExpiresAt != null && user.CodeExpiresAt > DateTime.UtcNow && !string.IsNullOrWhiteSpace(user.VerificationCode))
                {
                    var remaining = user.CodeExpiresAt.Value - DateTime.UtcNow;
                    var remainingSeconds = (int)remaining.TotalSeconds;
                    return Error.Conflict(
                        "auth.otp.active_code_exists",
                        $"لديك بالفعل كود تحقق نشط تم إرساله إلى بريدك. يرجى مراجعة رسائلك واستخدامه، أو الانتظار {remainingSeconds} ثانية قبل طلب كود جديد.");
                }

                // Generate 6-digit OTP
                var otp = new Random().Next(100000, 999999).ToString();
                var validMinutes = _emailConfiguration.ExpirationMinutes > 0
                    ? _emailConfiguration.ExpirationMinutes
                    : 15;
                user.SetVerificationCode(otp, DateTime.UtcNow.AddMinutes(validMinutes));

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError("Failed to update user with verification code for email {Email}", request.Email);
                    return Error.Failure("auth.otp.generation.failed", "فشل إنشاء كود التحقق.");
                }

                var userName = !string.IsNullOrWhiteSpace(user.FullName)
                    ? user.FullName
                    : request.Email.Split('@')[0];

                try
                {
                    await _emailSender.SendOtpEmailAsync(request.Email, userName, otp);
                }
                catch (Exception mailEx)
                {
                    _logger.LogError(mailEx, "Failed to deliver OTP verification email to recipient {Email}", request.Email);
                    // Don't fail if SMTP is not configured in local dev, but log error
                }

                return Result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SendOTPCommandHandler for email {Email}", request.Email);
                return Error.Unexpected("auth.otp.unexpected_error", "حدث خطأ غير متوقع أثناء إرسال كود التحقق.");
            }
        }
    }
}
