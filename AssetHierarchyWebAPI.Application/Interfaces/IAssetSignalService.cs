using AssetHierarchyWebAPI.Application.DTOs;

namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface IAssetSignalService
    {
        Task<ServiceResult> AddSignalAsync(int assetId, AssetSignalDto signal);
        Task<ServiceResult> RemoveSignalAsync(int signalId);
        Task<ServiceResult> UpdateSignalAsync(int signalId, AssetSignalDto updatedSignal);
        Task<List<AssetSignalDto>> GetSignalsByNodeIdAsync(int nodeId);
    }
}