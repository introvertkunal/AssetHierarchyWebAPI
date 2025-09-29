namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendAsync(string message);

        Task SendToUserAsync(string UserName, string message);
    }
}