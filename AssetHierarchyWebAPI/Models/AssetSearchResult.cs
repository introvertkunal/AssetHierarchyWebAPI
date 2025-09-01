namespace AssetHierarchyWebAPI.Models
{
    public class AssetSearchResult
    {
        public int Id { get; set; }

        public string NodeName { get; set; }

        public string ParentName { get; set; }

        public List<string> Children { get; set; } = new();

        public List<SignalResult> Signals { get; set; } = new();
    }

    public class SignalResult
    {
        public int SignalId { get; set; }
        public string SignalName { get; set; }
        public string SignalType { get; set; }
        public string Description { get; set; }
    }

}
