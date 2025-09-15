using AssetHierarchyWebAPI.Domain.Entities;

namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface IAssetHierarchyService
    {
        Task<string> AddNodeAsync(string name, int? parentId);
        Task<string> RemoveNodeAsync(int id);
        Task<List<AssetNode>> GetHierarchyAsync();
        Task<string> ReplaceJsonFileAsync(Stream fileStream); // instead of IFormFile
        Task<AssetSearchResult> SearchNode(string name);
        Task<string> UpdateNode(int id, string newName);
        Task<string> ReorderNode(int id, int? newParentId);
    }
}
