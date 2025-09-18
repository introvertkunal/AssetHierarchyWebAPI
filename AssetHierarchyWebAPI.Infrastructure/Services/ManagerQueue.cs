using AssetHierarchyWebAPI.Application.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetHierarchyWebAPI.Infrastructure.Services
{
    public class ManagerQueue : IManagerQueue
    {
        private readonly ConcurrentQueue<int> _queue = new();

        public void Enqueue(int Id)
        {
            _queue.Enqueue(Id);
        }

        public bool TryDequeue(out int Id)
        {
            return _queue.TryDequeue(out Id);
        }

    }
}
