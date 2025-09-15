namespace AssetHierarchyWebAPI.Application.DTOs
{
    public class AssetNodeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public List<AssetNodeDto> Children { get; set; } = new();
        public List<AssetSignalDto> Signals { get; set; } = new();
    }
}
