using AssetHierarchyWebAPI.Application.Interfaces;
using System.Collections.Concurrent;

namespace AssetHierarchyWebAPI.Infrastructure.Stores
{
    public class InMemoryNotificationStore : INotificationStore
    {
        private readonly List<(int Id, string Message)> _notifications = new();
        private int _counter = 0;
        private readonly ConcurrentDictionary<string, int> _userLastSeen = new();

        public int LastNotificationId => _counter;

        public int AddNotification(string message)
        {
            var id = Interlocked.Increment(ref _counter);
            lock (_notifications)
            {
                _notifications.Add((id, message));
                if (_notifications.Count > 100)
                    _notifications.RemoveAt(0);
            }
            return id;
        }

        public List<(int Id, string Message)> GetMissedNotifications(string userName)
        {
            int lastSeenId = _userLastSeen.GetOrAdd(userName, 0);
            lock (_notifications)
            {
                return _notifications.Where(n => n.Id > lastSeenId).ToList();
            }
        }

        public void MarkAsSeen(string userName, int lastSeenId)
        {
            _userLastSeen.AddOrUpdate(userName, lastSeenId, (key, oldValue) => Math.Max(oldValue, lastSeenId));
        }
    }
}