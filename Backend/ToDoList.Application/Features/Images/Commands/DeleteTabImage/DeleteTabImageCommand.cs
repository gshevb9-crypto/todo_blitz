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

namespace ToDoList.Application.Features.Images.Commands.DeleteTabImage
{
    public class DeleteTabImageCommand : IRequest<ErrorOr<Success>>
    {
        public Guid ImageId { get; set; }
        public string UserId { get; set; } = string.Empty;
    }

    public class DeleteTabImageCommandHandler : IRequestHandler<DeleteTabImageCommand, ErrorOr<Success>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<DeleteTabImageCommandHandler> _logger;

        public DeleteTabImageCommandHandler(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            ILogger<DeleteTabImageCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<ErrorOr<Success>> Handle(DeleteTabImageCommand request, CancellationToken cancellationToken)
        {
            var imageRepo = _unitOfWork.Repository<TabImage, Guid>();
            var images = await imageRepo.FindAsync(i => i.Id == request.ImageId && !i.IsDeleted, cancellationToken);
            var image = images.FirstOrDefault();

            if (image == null)
            {
                return Error.NotFound("images.not_found", "الصورة غير موجودة.");
            }

            if (image.UserId != request.UserId)
            {
                return Error.Forbidden("images.forbidden", "لا تملك صلاحية حذف هذه الصورة.");
            }

            try
            {
                _logger.LogInformation("Deleting physical image file {FileName} for image {ImageId}", image.FileName, image.Id);
                _fileStorageService.Delete(image.FileName, "tabs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete physical file {FileName}", image.FileName);
            }

            imageRepo.Delete(image);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return Result.Success;
        }
    }
}
