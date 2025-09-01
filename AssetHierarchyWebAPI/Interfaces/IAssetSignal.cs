using AssetHierarchyWebAPI.Models;

namespace AssetHierarchyWebAPI.Interfaces
{
    public interface IAssetSignal
    {
        Task<string> AddSignalAsync(int AssetId, AssetSignals signal);
        Task<string> RemoveSignalAsync(int signalId);
        Task<string> UpdateSignalAsync(int signalId, AssetSignals updatedSignal);
        Task<List<AssetSignals>> GetSignalsByNodeIdAsync(int nodeId);
    }
}
