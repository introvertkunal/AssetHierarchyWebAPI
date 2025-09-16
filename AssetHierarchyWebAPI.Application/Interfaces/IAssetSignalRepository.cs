using AssetHierarchyWebAPI.Domain.Entities;

namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface IAssetSignalRepository
    {
        Task<AssetSignals?> GetSignalByNameAndNodeIdAsync(string signalName, int nodeId);
        Task<AssetSignals> AddSignalAsync(AssetSignals signal);
        Task RemoveSignalAsync(int signalId);
        Task UpdateSignalAsync(AssetSignals signal);
        Task<List<AssetSignals>> GetSignalsByNodeIdAsync(int nodeId);

        Task<AssetSignals?> GetSignalByIdAsync(int signalId);
    }
}