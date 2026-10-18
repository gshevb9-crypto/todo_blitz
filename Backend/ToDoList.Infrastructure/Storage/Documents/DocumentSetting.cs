using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ToDoList.Infrastructure.Storage.Documents
{
    public static class DocumentSetting
    {
        private const int WebpQuality = 75;
        private const long MaxImageSizeInBytes = 20 * 1024 * 1024; // 20 MB

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".webp"
        };

        public static async Task<string> UploadImageAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file was uploaded or file is empty.", nameof(file));
            }

            if (file.Length > MaxImageSizeInBytes)
            {
                throw new InvalidOperationException($"File size exceeds maximum allowed size of {MaxImageSizeInBytes / (1024 * 1024)}MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImageExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Only image files are allowed (JPG, JPEG, PNG, BMP, TIFF, WebP).");
            }

            using var stream = file.OpenReadStream();
            return await UploadImageAsync(stream, file.FileName, folderName, cancellationToken);
        }

        public static async Task<string> UploadImageAsync(Stream inputStream, string originalFileName, string folderName, CancellationToken cancellationToken = default)
        {
            if (inputStream == null || inputStream.Length == 0)
            {
                throw new ArgumentException("Stream is empty.", nameof(inputStream));
            }

            // Create target folder path
            var cleanFolder = SanitizeFolderName(folderName);
            var targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files", cleanFolder);

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // Generate safe unique filename
            var uniqueFileName = $"{Guid.NewGuid():N}.webp";
            var destinationFilePath = Path.Combine(targetDirectory, uniqueFileName);

            // Load and re-encode image to WebP with orientation fix
            inputStream.Position = 0;
            using var image = await Image.LoadAsync(inputStream, cancellationToken);
            image.Mutate(x => x.AutoOrient());

            var encoder = new WebpEncoder
            {
                Quality = WebpQuality
            };

            await image.SaveAsWebpAsync(destinationFilePath, encoder, cancellationToken);

            return uniqueFileName;
        }

        public static void Delete(string fileName, string folderName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var cleanFileName = Path.GetFileName(fileName);
            var cleanFolder = SanitizeFolderName(folderName);

            var rootPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files"));
            var targetPath = Path.GetFullPath(Path.Combine(rootPath, cleanFolder, cleanFileName));

            // Prevent path traversal
            if (!targetPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid file path detected.");
            }

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }

        public static string GetFileUrl(string fileName, string folderName, string? baseUrl = null)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;

            if (fileName.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }

            var cleanFolder = SanitizeFolderName(folderName);
            var cleanFileName = Path.GetFileName(fileName);
            var relativePath = $"/files/{cleanFolder}/{cleanFileName}";

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return relativePath;
            }

            var baseUri = baseUrl.TrimEnd('/');
            // If baseUrl already ends with /api, remove /api for file URLs
            if (baseUri.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                baseUri = baseUri[..^4];
            }

            return $"{baseUri}{relativePath}";
        }

        private static string SanitizeFolderName(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName)) return "general";
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Concat(folderName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Replace("..", "").Trim('/');
        }
    }
}
