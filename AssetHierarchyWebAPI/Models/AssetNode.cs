using System.ComponentModel.DataAnnotations;

namespace AssetHierarchyWebAPI.Models
{
    public class AssetNode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public List<AssetNode> Children { get; set; } = new(); 
    }

}
