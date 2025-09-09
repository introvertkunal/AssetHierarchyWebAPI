using Microsoft.AspNetCore.SignalR;

namespace AssetHierarchyWebAPI.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly INotificationStore _notificationStore;

        public NotificationHub(INotificationStore notificationStore)
        {
            _notificationStore = notificationStore;
        }

        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name ?? Context.ConnectionId;

            // Send missed notifications
            var missed = _notificationStore.GetMissedNotifications(userName);
            foreach (var (id, msg) in missed)
            {
                await Clients.Caller.SendAsync("ReceiveNotification", msg);
            }

            // Mark as seen
            if (missed.Any())
            {
                _notificationStore.MarkAsSeen(userName, missed.Last().Id);
            }

            await base.OnConnectedAsync();
        }

        // Optional: client can explicitly acknowledge
        public Task AcknowledgeNotification(int lastSeenId)
        {
            var userName = Context.User?.Identity?.Name ?? Context.ConnectionId;
            _notificationStore.MarkAsSeen(userName, lastSeenId);
            return Task.CompletedTask;
        }
    }

}
