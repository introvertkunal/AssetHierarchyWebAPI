using AssetHierarchyWebAPI.Application.Interfaces;
using System.Collections.Concurrent;

namespace AssetHierarchyWebAPI.Infrastructure.Stores
{
    public class InMemoryNotificationStore : INotificationStore
    {
        private readonly List<(int Id, string Message)> _globalNotifications = new();
        private readonly ConcurrentDictionary<string, List<(int Id, string Message)>> _userNotifications = new();
        private readonly ConcurrentDictionary<string, int> _userLastSeen = new();
        private int _counter = 0;

        public int LastNotificationId => _counter;

        public int AddGlobalNotification(string message)
        {
            var id = Interlocked.Increment(ref _counter);
            lock (_globalNotifications)
            {
                _globalNotifications.Add((id, message));
                if (_globalNotifications.Count > 100)
                    _globalNotifications.RemoveAt(0);
            }
            return id;
        }

        public int AddUserNotification(string userName, string message)
        {
            var id = Interlocked.Increment(ref _counter);
            var list = _userNotifications.GetOrAdd(userName, _ => new List<(int Id, string Message)>());
            lock (list)
            {
                list.Add((id, message));
                if (list.Count > 50) // limit per user
                    list.RemoveAt(0);
            }
            return id;
        }

        public List<(int Id, string Message)> GetMissedNotifications(string userName)
        {
            int lastSeenId = _userLastSeen.GetOrAdd(userName, 0);
            var result = new List<(int Id, string Message)>();

            lock (_globalNotifications)
            {
                result.AddRange(_globalNotifications.Where(n => n.Id > lastSeenId));
            }

            if (_userNotifications.TryGetValue(userName, out var personalList))
            {
                lock (personalList)
                {
                    result.AddRange(personalList.Where(n => n.Id > lastSeenId));
                }
            }

            return result.OrderBy(n => n.Id).ToList();
        }

        public void MarkAsSeen(string userName, int lastSeenId)
        {
            _userLastSeen.AddOrUpdate(userName, lastSeenId,
                (key, oldValue) => Math.Max(oldValue, lastSeenId));
        }
    }
}
