namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface INotificationStore
    {
        int AddNotification(string message);
        List<(int Id, string Message)> GetMissedNotifications(string userName);
        void MarkAsSeen(string userName, int lastSeenId);
        int LastNotificationId { get; }
    }
}