using AssetHierarchyWebAPI.Domain.Entities;

namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface IAssetSignalService
    {
        Task<string> AddSignalAsync(int assetId, AssetSignals signal);
        Task<string> RemoveSignalAsync(int signalId);
        Task<string> UpdateSignalAsync(int signalId, AssetSignals updatedSignal);
        Task<List<AssetSignals>> GetSignalsByNodeIdAsync(int nodeId);
    }
}
