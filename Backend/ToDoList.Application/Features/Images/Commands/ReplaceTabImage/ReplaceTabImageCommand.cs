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

namespace ToDoList.Application.Features.Images.Commands.ReplaceTabImage
{
    public class ReplaceTabImageCommand : IRequest<ErrorOr<TabImageDto>>
    {
        public Guid ImageId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public IFormFile File { get; set; } = null!;
    }

    public class ReplaceTabImageCommandHandler : IRequestHandler<ReplaceTabImageCommand, ErrorOr<TabImageDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<ReplaceTabImageCommandHandler> _logger;

        public ReplaceTabImageCommandHandler(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            ILogger<ReplaceTabImageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<ErrorOr<TabImageDto>> Handle(ReplaceTabImageCommand request, CancellationToken cancellationToken)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return Error.Validation("images.empty_file", "الملف فارغ أو لم يتم إرسال صورة.");
            }

            var imageRepo = _unitOfWork.Repository<TabImage, Guid>();
            var images = await imageRepo.FindAsync(i => i.Id == request.ImageId && !i.IsDeleted, cancellationToken);
            var image = images.FirstOrDefault();

            if (image == null)
            {
                return Error.NotFound("images.not_found", "الصورة غير موجودة.");
            }

            if (image.UserId != request.UserId)
            {
                return Error.Forbidden("images.forbidden", "لا تملك صلاحية استبدال هذه الصورة.");
            }

            var oldFileName = image.FileName;

            try
            {
                // 1. Upload new optimized image
                var newStoredFileName = await _fileStorageService.UploadImageAsync(request.File, "tabs", cancellationToken);

                // 2. Delete old physical file to prevent orphaned storage
                try
                {
                    _logger.LogInformation("Deleting old physical file {OldFileName} replacing with {NewFileName}", oldFileName, newStoredFileName);
                    _fileStorageService.Delete(oldFileName, "tabs");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete previous physical file {OldFileName}", oldFileName);
                }

                // 3. Update database record
                image.FileName = newStoredFileName;
                image.OriginalFileName = request.File.FileName;
                image.FileSize = request.File.Length;
                image.ContentType = "image/webp";
                image.RelativePath = $"tabs/{newStoredFileName}";
                image.UpdatedAt = DateTime.UtcNow;

                imageRepo.Update(image);
                await _unitOfWork.CompleteAsync(cancellationToken);

                return new TabImageDto
                {
                    Id = image.Id,
                    TabId = image.TabId,
                    FileName = image.FileName,
                    OriginalFileName = image.OriginalFileName,
                    FileSize = image.FileSize,
                    ContentType = image.ContentType,
                    Url = _fileStorageService.GetFileUrl(image.FileName, "tabs"),
                    CreatedAt = image.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to replace image {ImageId}", image.Id);
                return Error.Failure("images.replace_failed", $"فشل استبدال الصورة: {ex.Message}");
            }
        }
    }
}
