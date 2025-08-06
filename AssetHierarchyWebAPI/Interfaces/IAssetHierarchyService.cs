using AssetHierarchyWebAPI.Models;
using System.Net;

namespace AssetHierarchyWebAPI.Interfaces
{
    public interface IAssetHierarchyService
    {
        string addNode(string name, string parentName);

        string removeNode(string name);

        List<AssetNode> GetHierarchy();

        void ReplaceJsonFile(IFormFile file);
    }
}
