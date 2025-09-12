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

            var missed = _notificationStore.GetMissedNotifications(userName);
            foreach (var (id, msg) in missed)
            {
                await Clients.Caller.SendAsync("ReceiveNotification", id, msg);  
            }

            if (missed.Any())
            {
                _notificationStore.MarkAsSeen(userName, missed.Last().Id);  
            }
            else
            {
             
                _notificationStore.MarkAsSeen(userName, _notificationStore.LastNotificationId);
            }

            await base.OnConnectedAsync();
        }

     
        public Task AcknowledgeNotification(int lastSeenId)
        {
            var userName = Context.User?.Identity?.Name ?? Context.ConnectionId;
            _notificationStore.MarkAsSeen(userName, lastSeenId);
            return Task.CompletedTask;
        }
    }

}
