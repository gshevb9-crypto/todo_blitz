using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using MediatR;
using ToDoList.Application.Features.Tabs.DTOs;
using ToDoList.Domain.Entities;
using ToDoList.Domain.Repositories;

namespace ToDoList.Application.Features.Tabs.Commands.UpdateTabContent
{
    public class UpdateTabContentCommand : IRequest<ErrorOr<TabContentDto>>
    {
        public Guid TabId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string TextContent { get; set; } = string.Empty;
    }

    public class UpdateTabContentCommandHandler : IRequestHandler<UpdateTabContentCommand, ErrorOr<TabContentDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTabContentCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ErrorOr<TabContentDto>> Handle(UpdateTabContentCommand request, CancellationToken cancellationToken)
        {
            var tabRepo = _unitOfWork.Repository<Tab, Guid>();
            var tabs = await tabRepo.FindAsync(t => t.Id == request.TabId && !t.IsDeleted, cancellationToken);
            var tab = tabs.FirstOrDefault();

            if (tab == null)
            {
                return Error.NotFound("tabs.not_found", "التاب غير موجود.");
            }

            if (tab.UserId != request.UserId)
            {
                return Error.Forbidden("tabs.forbidden", "لا تملك صلاحية تعديل محتوى هذا التاب.");
            }

            var contentRepo = _unitOfWork.Repository<TabContent, Guid>();
            var contents = await contentRepo.FindAsync(c => c.TabId == request.TabId, cancellationToken);
            var content = contents.FirstOrDefault();

            if (content == null)
            {
                content = new TabContent
                {
                    Id = Guid.NewGuid(),
                    TabId = tab.Id,
                    TextContent = request.TextContent,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await contentRepo.AddAsync(content, cancellationToken);
            }
            else
            {
                content.TextContent = request.TextContent;
                content.UpdatedAt = DateTime.UtcNow;
                contentRepo.Update(content);
            }

            tab.UpdatedAt = DateTime.UtcNow;
            tabRepo.Update(tab);

            await _unitOfWork.CompleteAsync(cancellationToken);

            return new TabContentDto
            {
                TabId = tab.Id,
                TextContent = content.TextContent,
                UpdatedAt = content.UpdatedAt
            };
        }
    }
}
