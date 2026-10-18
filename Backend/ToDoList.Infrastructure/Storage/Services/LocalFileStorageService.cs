using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToDoList.Application.Common.Interfaces;
using ToDoList.Infrastructure.Storage.Documents;

namespace ToDoList.Infrastructure.Storage.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(
            IConfiguration configuration,
            ILogger<LocalFileStorageService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Uploading and converting image {FileName} to WebP in folder {FolderName}", file?.FileName, folderName);
            return await DocumentSetting.UploadImageAsync(file!, folderName, cancellationToken);
        }

        public async Task<string> UploadImageAsync(Stream fileStream, string fileName, string folderName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Uploading and converting stream {FileName} to WebP in folder {FolderName}", fileName, folderName);
            return await DocumentSetting.UploadImageAsync(fileStream, fileName, folderName, cancellationToken);
        }

        public void Delete(string fileName, string folderName)
        {
            try
            {
                _logger.LogInformation("Deleting file {FileName} in folder {FolderName}", fileName, folderName);
                DocumentSetting.Delete(fileName, folderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FileName} in folder {FolderName}: {Message}", fileName, folderName, ex.Message);
            }
        }

        public string GetFileUrl(string fileName, string folderName, string? baseUrl = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            var effectiveBaseUrl = !string.IsNullOrWhiteSpace(baseUrl)
                ? baseUrl
                : _configuration["BaseURL"] ?? string.Empty;

            return DocumentSetting.GetFileUrl(fileName, folderName, effectiveBaseUrl);
        }
    }
}
