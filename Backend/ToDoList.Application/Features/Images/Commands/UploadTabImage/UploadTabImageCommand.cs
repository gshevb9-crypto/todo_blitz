using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ToDoList.Application.Common.Interfaces;
using ToDoList.Application.Features.Tabs.DTOs;
using ToDoList.Domain.Entities;
using ToDoList.Domain.Repositories;

namespace ToDoList.Application.Features.Images.Commands.UploadTabImage
{
    public class UploadTabImageCommand : IRequest<ErrorOr<TabImageDto>>
    {
        public Guid TabId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public IFormFile File { get; set; } = null!;
    }

    public class UploadTabImageCommandHandler : IRequestHandler<UploadTabImageCommand, ErrorOr<TabImageDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<UploadTabImageCommandHandler> _logger;

        public UploadTabImageCommandHandler(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            ILogger<UploadTabImageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<ErrorOr<TabImageDto>> Handle(UploadTabImageCommand request, CancellationToken cancellationToken)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return Error.Validation("images.empty_file", "الملف فارغ أو لم يتم إرسال صورة.");
            }

            var tabRepo = _unitOfWork.Repository<Tab, Guid>();
            var tabs = await tabRepo.FindAsync(t => t.Id == request.TabId && !t.IsDeleted, cancellationToken);
            var tab = tabs.FirstOrDefault();

            if (tab == null)
            {
                return Error.NotFound("tabs.not_found", "التاب غير موجود.");
            }

            if (tab.UserId != request.UserId)
            {
                return Error.Forbidden("tabs.forbidden", "لا تملك صلاحية إضافة صور لهذا التاب.");
            }

            try
            {
                var storedFileName = await _fileStorageService.UploadImageAsync(request.File, "tabs", cancellationToken);

                var tabImage = new TabImage
                {
                    Id = Guid.NewGuid(),
                    TabId = tab.Id,
                    UserId = request.UserId,
                    FileName = storedFileName,
                    OriginalFileName = request.File.FileName,
                    FileSize = request.File.Length,
                    ContentType = "image/webp",
                    RelativePath = $"tabs/{storedFileName}",
                    CreatedAt = DateTime.UtcNow
                };

                var imageRepo = _unitOfWork.Repository<TabImage, Guid>();
                await imageRepo.AddAsync(tabImage, cancellationToken);
                await _unitOfWork.CompleteAsync(cancellationToken);

                return new TabImageDto
                {
                    Id = tabImage.Id,
                    TabId = tabImage.TabId,
                    FileName = tabImage.FileName,
                    OriginalFileName = tabImage.OriginalFileName,
                    FileSize = tabImage.FileSize,
                    ContentType = tabImage.ContentType,
                    Url = _fileStorageService.GetFileUrl(tabImage.FileName, "tabs"),
                    CreatedAt = tabImage.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload and optimize image {FileName} for tab {TabId}", request.File.FileName, tab.Id);
                return Error.Failure("images.upload_failed", $"فشل رفع الصورة: {ex.Message}");
            }
        }
    }
}
