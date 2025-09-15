namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface IFileService
    {
        Task UpdateJsonFileAsync();
        Task<T> DeserializeJsonAsync<T>(Stream fileStream);
        Task BackupJsonFileAsync(string filePath, int keepLast);
    }
}