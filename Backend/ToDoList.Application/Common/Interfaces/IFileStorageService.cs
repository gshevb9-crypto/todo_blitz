using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ToDoList.Application.Common.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadImageAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);

        Task<string> UploadImageAsync(Stream fileStream, string fileName, string folderName, CancellationToken cancellationToken = default);

        void Delete(string fileName, string folderName);

        string GetFileUrl(string fileName, string folderName, string? baseUrl = null);
    }
}
