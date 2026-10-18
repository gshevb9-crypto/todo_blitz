using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using ToDoList.Application.Common.Interfaces;
using ToDoList.Domain.Entities;
using ToDoList.Domain.Repositories;

namespace ToDoList.Application.Features.Tabs.Commands.DeleteTab
{
    public class DeleteTabCommand : IRequest<ErrorOr<Success>>
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
    }

    public class DeleteTabCommandHandler : IRequestHandler<DeleteTabCommand, ErrorOr<Success>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<DeleteTabCommandHandler> _logger;

        public DeleteTabCommandHandler(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            ILogger<DeleteTabCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<ErrorOr<Success>> Handle(DeleteTabCommand request, CancellationToken cancellationToken)
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
                return Error.Forbidden("tabs.forbidden", "لا تملك صلاحية حذف هذا التاب.");
            }

            // 1. Explicitly clean up all physical image files associated with this tab
            var imageRepo = _unitOfWork.Repository<TabImage, Guid>();
            var images = await imageRepo.FindAsync(i => i.TabId == tab.Id, cancellationToken);

            foreach (var img in images)
            {
                try
                {
                    _logger.LogInformation("Cleaning up physical file for image {FileName} on tab {TabId} deletion", img.FileName, tab.Id);
                    _fileStorageService.Delete(img.FileName, "tabs");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete physical file {FileName} during tab deletion", img.FileName);
                }
                imageRepo.Delete(img);
            }

            // 2. Remove associated TabContent records
            var contentRepo = _unitOfWork.Repository<TabContent, Guid>();
            var contents = await contentRepo.FindAsync(c => c.TabId == tab.Id, cancellationToken);
            foreach (var c in contents)
            {
                contentRepo.Delete(c);
            }

            // 3. Remove tab entity
            tabRepo.Delete(tab);

            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Tab {TabId} and all associated images deleted successfully by user {UserId}", tab.Id, request.UserId);
            return Result.Success;
        }
    }
}
