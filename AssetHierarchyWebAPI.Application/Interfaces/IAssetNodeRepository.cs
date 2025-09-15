using AssetHierarchyWebAPI.Domain.Entities;

namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface IAssetNodeRepository
    {
        Task<AssetNode> AddNodeAsync(AssetNode node);
        Task<AssetNode?> GetNodeByIdAsync(int id, bool includeChildren = false, bool includeSignals = false);
        Task<AssetNode?> GetNodeByNameAsync(string name);
        Task<List<AssetNode>> GetAllNodesAsync(bool includeChildren = false, bool includeSignals = false);
        Task<bool> NodeExistsAsync(string name);
        Task<bool> NodeExistsByIdAsync(int id);
        Task RemoveNodeAsync(AssetNode node);
        Task UpdateNodeAsync(AssetNode node);
        Task<bool> IsDescendantAsync(int nodeId, int potentialParentId);
    }
}