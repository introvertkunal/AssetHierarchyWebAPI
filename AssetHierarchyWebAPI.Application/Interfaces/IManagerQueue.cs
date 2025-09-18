using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface IManagerQueue
    {
        void Enqueue(int Id);

        // out is keyword used with try to return multiple values
        // Here, we get true/false + Id + ColumnName as return
        bool TryDequeue(out int Id);
    }
}
