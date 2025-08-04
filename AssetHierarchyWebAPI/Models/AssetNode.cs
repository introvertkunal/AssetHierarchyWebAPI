namespace AssetHierarchyWebAPI.Models
{
    public class AssetNode
    {
        public string Name { get; set; }
        public List<AssetNode> Children { get; set; } = new();
    }
}
