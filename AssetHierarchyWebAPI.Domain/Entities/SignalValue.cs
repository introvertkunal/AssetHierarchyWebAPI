using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetHierarchyWebAPI.Domain.Entities
{
    public class SignalValue
    {
        public int ValueId { get; set; }

        public double SignalValueData { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public int SignalId { get; set; }

        public AssetSignals AssetSignal { get; set; }
    }
}
