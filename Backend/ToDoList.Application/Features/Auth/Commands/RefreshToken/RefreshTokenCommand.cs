using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ToDoList.Application.Common.Interfaces;
using ToDoList.Application.Features.Auth.DTOs;
using ToDoList.Domain.Entities;
using ToDoList.Domain.Repositories;

namespace ToDoList.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<ErrorOr<TokenResponseDTO>>
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ErrorOr<TokenResponseDTO>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;

        public RefreshTokenCommandHandler(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
        }

        public async Task<ErrorOr<TokenResponseDTO>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Error.Validation("auth.token.empty", "توكن التجديد مطلوب.");
            }

            var refreshTokenRepo = _unitOfWork.Repository<RefreshTokenTable, int>();
            var tokens = await refreshTokenRepo.FindAsync(t => t.Token == request.RefreshToken, cancellationToken);
            var storedToken = tokens.FirstOrDefault();

            if (storedToken == null)
            {
                return Error.Unauthorized("auth.token.invalid", "توكن التجديد غير صالح.");
            }

            if (storedToken.RevokedAt != null)
            {
                return Error.Unauthorized("auth.token.revoked", "تم إلغاء توكن التجديد مسبقاً.");
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return Error.Unauthorized("auth.token.expired", "انتهت صلاحية توكن التجديد.");
            }

            var user = await _userManager.FindByIdAsync(storedToken.UserId);
            if (user == null || !user.IsActive)
            {
                return Error.Unauthorized("auth.user.inactive", "المستخدم غير موجود أو تم إيقاف حسابه.");
            }

            // Rotate refresh token: revoke current token
            storedToken.RevokedAt = DateTime.UtcNow;
            refreshTokenRepo.Update(storedToken);

            // Generate new pair
            var (accessToken, accessExp) = await _jwtService.GenerateAccessTokenAsync(user, _userManager);
            var (newRefreshToken, refreshExp) = _jwtService.GenerateRefreshToken();

            await refreshTokenRepo.AddAsync(new RefreshTokenTable
            {
                Token = newRefreshToken,
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
                RefreshToken = newRefreshToken,
                RefreshTokenExpiresAt = refreshExp,
                Email = user.Email,
                FullName = user.FullName
            };
        }
    }
}
