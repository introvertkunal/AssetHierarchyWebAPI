namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface INotificationStore
    {
        int LastNotificationId { get; }

        int AddGlobalNotification(string message);
        int AddUserNotification(string userName, string message);

        List<(int Id, string Message)> GetMissedNotifications(string userName);

        void MarkAsSeen(string userName, int lastSeenId);
    }

}


