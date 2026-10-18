using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using FluentValidation;
using MediatR;
using ToDoList.Application.Features.Tabs.DTOs;
using ToDoList.Domain.Entities;
using ToDoList.Domain.Repositories;

namespace ToDoList.Application.Features.Tabs.Commands.UpdateTab
{
    public class UpdateTabCommand : IRequest<ErrorOr<TabDto>>
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int? OrderIndex { get; set; }
    }

    public class UpdateTabCommandValidator : AbstractValidator<UpdateTabCommand>
    {
        public UpdateTabCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("معرف التاب مطلوب.");
            RuleFor(x => x.UserId).NotEmpty().WithMessage("المستخدم مطلوب.");
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان التاب مطلوب.").MaximumLength(200);
        }
    }

    public class UpdateTabCommandHandler : IRequestHandler<UpdateTabCommand, ErrorOr<TabDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTabCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ErrorOr<TabDto>> Handle(UpdateTabCommand request, CancellationToken cancellationToken)
        {
            var tabRepo = _unitOfWork.Repository<Tab, Guid>();
            var tabs = await tabRepo.FindAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);
            var tab = tabs.FirstOrDefault();

            if (tab == null)
            {
                return Error.NotFound("tabs.not_found", "التاب غير موجود.");
            }

            // Enforce ownership
            if (tab.UserId != request.UserId)
            {
                return Error.Forbidden("tabs.forbidden", "لا تملك صلاحية تعديل هذا التاب.");
            }

            tab.Title = request.Title.Trim();
            if (!string.IsNullOrWhiteSpace(request.Icon))
            {
                tab.Icon = request.Icon.Trim();
            }
            if (request.OrderIndex.HasValue)
            {
                tab.OrderIndex = request.OrderIndex.Value;
            }
            tab.UpdatedAt = DateTime.UtcNow;

            tabRepo.Update(tab);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var imageRepo = _unitOfWork.Repository<TabImage, Guid>();
            var imagesCount = await imageRepo.CountAsync(i => i.TabId == tab.Id && !i.IsDeleted, cancellationToken);

            return new TabDto
            {
                Id = tab.Id,
                Title = tab.Title,
                Icon = tab.Icon,
                OrderIndex = tab.OrderIndex,
                CreatedAt = tab.CreatedAt,
                UpdatedAt = tab.UpdatedAt,
                ImagesCount = imagesCount
            };
        }
    }
}
