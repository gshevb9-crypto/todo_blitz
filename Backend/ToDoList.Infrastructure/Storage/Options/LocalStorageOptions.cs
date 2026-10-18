namespace ToDoList.Infrastructure.Storage.Options
{
    public class LocalStorageOptions
    {
        public string RootPath { get; set; } = "wwwroot";
        public string FilesDirectory { get; set; } = "files";
        public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024; // 20 MB
    }
}
