using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using MediatR;
using ToDoList.Application.Common.Interfaces;
using ToDoList.Application.Features.Tabs.DTOs;
using ToDoList.Domain.Entities;
using ToDoList.Domain.Repositories;

namespace ToDoList.Application.Features.Tabs.Queries.GetTabById
{
    public class GetTabByIdQuery : IRequest<ErrorOr<TabDetailDto>>
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
    }

    public class GetTabByIdQueryHandler : IRequestHandler<GetTabByIdQuery, ErrorOr<TabDetailDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public GetTabByIdQueryHandler(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<ErrorOr<TabDetailDto>> Handle(GetTabByIdQuery request, CancellationToken cancellationToken)
        {
            var tabRepo = _unitOfWork.Repository<Tab, Guid>();
            var tabs = await tabRepo.FindAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);
            var tab = tabs.FirstOrDefault();

            if (tab == null)
            {
                return Error.NotFound("tabs.not_found", "التاب غير موجود.");
            }

            if (tab.UserId != request.UserId)
            {
                return Error.Forbidden("tabs.forbidden", "لا تملك صلاحية الوصول إلى هذا التاب.");
            }

            var contentRepo = _unitOfWork.Repository<TabContent, Guid>();
            var contents = await contentRepo.FindAsync(c => c.TabId == tab.Id, cancellationToken);
            var content = contents.FirstOrDefault();

            var imageRepo = _unitOfWork.Repository<TabImage, Guid>();
            var images = await imageRepo.FindAsync(i => i.TabId == tab.Id && !i.IsDeleted, cancellationToken);

            var imageDtos = images
                .OrderBy(i => i.CreatedAt)
                .Select(i => new TabImageDto
                {
                    Id = i.Id,
                    TabId = i.TabId,
                    FileName = i.FileName,
                    OriginalFileName = i.OriginalFileName,
                    FileSize = i.FileSize,
                    ContentType = i.ContentType,
                    Url = _fileStorageService.GetFileUrl(i.FileName, "tabs"),
                    CreatedAt = i.CreatedAt
                })
                .ToList();

            return new TabDetailDto
            {
                Id = tab.Id,
                Title = tab.Title,
                Icon = tab.Icon,
                OrderIndex = tab.OrderIndex,
                CreatedAt = tab.CreatedAt,
                UpdatedAt = tab.UpdatedAt,
                TextContent = content?.TextContent ?? string.Empty,
                Images = imageDtos
            };
        }
    }
}
