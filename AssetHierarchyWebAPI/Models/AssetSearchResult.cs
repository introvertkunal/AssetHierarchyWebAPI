namespace AssetHierarchyWebAPI.Models
{
    public class AssetSearchResult
    {
        public int Id { get; set; }

        public string NodeName { get; set; }

        public string ParentName { get; set; }

        public List<string> Children { get; set; } = new();
    }
}
