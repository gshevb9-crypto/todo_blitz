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

namespace ToDoList.Application.Features.Tabs.Commands.CreateTab
{
    public class CreateTabCommand : IRequest<ErrorOr<TabDto>>
    {
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int OrderIndex { get; set; }
    }

    public class CreateTabCommandValidator : AbstractValidator<CreateTabCommand>
    {
        public CreateTabCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("المستخدم مطلوب.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("عنوان التاب مطلوب.")
                .MaximumLength(200).WithMessage("عنوان التاب لا يتجاوز 200 حرف.");
        }
    }

    public class CreateTabCommandHandler : IRequestHandler<CreateTabCommand, ErrorOr<TabDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateTabCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ErrorOr<TabDto>> Handle(CreateTabCommand request, CancellationToken cancellationToken)
        {
            var tabRepo = _unitOfWork.Repository<Tab, Guid>();
            var contentRepo = _unitOfWork.Repository<TabContent, Guid>();

            // Determine order index if 0
            var existingTabs = await tabRepo.FindAsync(t => t.UserId == request.UserId && !t.IsDeleted, cancellationToken);
            var nextOrder = request.OrderIndex > 0 ? request.OrderIndex : existingTabs.Count;

            var tab = new Tab
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = string.IsNullOrWhiteSpace(request.Title) ? "Untitled" : request.Title.Trim(),
                Icon = string.IsNullOrWhiteSpace(request.Icon) ? "📝" : request.Icon.Trim(),
                OrderIndex = nextOrder,
                CreatedAt = DateTime.UtcNow
            };

            await tabRepo.AddAsync(tab, cancellationToken);

            // Also initialize an empty TabContent entity
            var content = new TabContent
            {
                Id = Guid.NewGuid(),
                TabId = tab.Id,
                TextContent = string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await contentRepo.AddAsync(content, cancellationToken);

            await _unitOfWork.CompleteAsync(cancellationToken);

            return new TabDto
            {
                Id = tab.Id,
                Title = tab.Title,
                Icon = tab.Icon,
                OrderIndex = tab.OrderIndex,
                CreatedAt = tab.CreatedAt,
                UpdatedAt = tab.UpdatedAt,
                ImagesCount = 0
            };
        }
    }
}
