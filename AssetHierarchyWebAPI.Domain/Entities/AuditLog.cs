namespace AssetHierarchyWebAPI.Domain.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;   
        public int? EntityId { get; set; }                     
        public string? EntityName { get; set; }                 
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;


    }
}
