using AssetHierarchyWebAPI.Models;
using System.Net;

namespace AssetHierarchyWebAPI.Interfaces
{
    public interface IAssetHierarchyService
    {
        string AddNode(string name, int? parentId);

        string RemoveNode(int id);

        List<AssetNode> GetHierarchy();

        Task ReplaceJsonFileAsync(IFormFile file);

        AssetSearchResult SearchNode(string name);

        string UpdateNode(int id, string newName);

        string ReorderNode(int id, int? newParentId);
    }
}
