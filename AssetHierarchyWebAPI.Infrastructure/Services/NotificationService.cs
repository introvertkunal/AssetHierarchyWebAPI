using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AssetHierarchyWebAPI.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly INotificationStore _notificationStore;

        public NotificationService(IHubContext<NotificationHub> hubContext, INotificationStore notificationStore)
        {
            _hubContext = hubContext;
            _notificationStore = notificationStore;
        }

        public async Task SendAsync(string message)
        {
            var id = _notificationStore.AddGlobalNotification(message);
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", id, message);
        }

        public async Task SendToUserAsync(string UserName, string message)
        {
            var id = _notificationStore.AddUserNotification(UserName, message);
            await _hubContext.Clients.User(UserName).SendAsync("ReceiveNotification", id, message);
        }
    }
}