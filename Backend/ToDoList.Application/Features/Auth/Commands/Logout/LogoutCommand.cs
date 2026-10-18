using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using MediatR;
using ToDoList.Domain.Entities;
using ToDoList.Domain.Repositories;

namespace ToDoList.Application.Features.Auth.Commands.Logout
{
    public class LogoutCommand : IRequest<ErrorOr<Success>>
    {
        public string UserId { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, ErrorOr<Success>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public LogoutCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ErrorOr<Success>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var refreshTokenRepo = _unitOfWork.Repository<RefreshTokenTable, int>();

            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var tokens = await refreshTokenRepo.FindAsync(t => t.Token == request.RefreshToken, cancellationToken);
                var token = tokens.FirstOrDefault();
                if (token != null && token.RevokedAt == null)
                {
                    token.RevokedAt = DateTime.UtcNow;
                    refreshTokenRepo.Update(token);
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                var tokens = await refreshTokenRepo.FindAsync(t => t.UserId == request.UserId && t.RevokedAt == null, cancellationToken);
                foreach (var t in tokens)
                {
                    t.RevokedAt = DateTime.UtcNow;
                    refreshTokenRepo.Update(t);
                }
            }

            await _unitOfWork.CompleteAsync(cancellationToken);
            return Result.Success;
        }
    }
}
