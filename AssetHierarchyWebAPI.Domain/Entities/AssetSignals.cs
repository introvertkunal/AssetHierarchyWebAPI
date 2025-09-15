namespace AssetHierarchyWebAPI.Domain.Entities
{
    public class AssetSignals
    {
        public int SignalId { get; set; }
        public string SignalName { get; set; }
        public string SignalType { get; set; }
        public string Description { get; set; }

        public int AssetNodeId { get; set; }
        public AssetNode AssetNode { get; set; }
    }
}
