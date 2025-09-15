namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(string operation, int? entityId, string? entityName);
    }
}