namespace AssetHierarchyWebAPI.Domain.Entities
{
    public class AssetNode
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int? ParentId { get; set; }
        public AssetNode Parent { get; set; }

        public ICollection<AssetNode> Children { get; set; } = new List<AssetNode>();
        public ICollection<AssetSignals> Signals { get; set; } = new List<AssetSignals>();
    }
}
