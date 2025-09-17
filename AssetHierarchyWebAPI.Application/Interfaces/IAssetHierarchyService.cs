using AssetHierarchyWebAPI.Application.DTOs;
using AssetHierarchyWebAPI.Domain.Entities;

public interface IAssetHierarchyService
{
    Task<ServiceResponse> AddNodeAsync(string name, int? parentId);
    Task<ServiceResponse> RemoveNodeAsync(int id);
    Task<ServiceResponse> UpdateNode(int id, string newName);
    Task<ServiceResponse> ReorderNode(int id, int? newParentId);
    Task<ServiceResponse> ReplaceJsonFileAsync(Stream fileStream);

    Task<List<AssetNodeDto>> GetHierarchyAsync();
    Task<AssetSearchResult?> SearchNode(string name);
}
