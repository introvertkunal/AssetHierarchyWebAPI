using AssetHierarchyWebAPI.Models;
using System.Net;

namespace AssetHierarchyWebAPI.Interfaces
{
    public interface IAssetHierarchyService
    {
        Task<string> AddNodeAsync(string name, int? parentId);

        Task<string> RemoveNodeAsync(int id);

        Task<List<AssetNode>> GetHierarchyAsync();

        Task<string> ReplaceJsonFileAsync(IFormFile file);

        Task<AssetSearchResult> SearchNode(string name);

        Task<string> UpdateNode(int id, string newName);

        Task<string> ReorderNode(int id, int? newParentId);
    }
}
