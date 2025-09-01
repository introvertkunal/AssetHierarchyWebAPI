using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetHierarchyWebAPI.Models
{
    public class AssetSignals
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonProperty(Required = Required.Always)]
        public int SignalId { get; set; }

        [Required]
        [MaxLength(100)]
        [JsonProperty(Required = Required.Always)]
        public string SignalName { get; set; }

        [Required]
        [MaxLength(20)]
        [JsonProperty(Required = Required.Always)]
        public string SignalType { get; set; }

        [MaxLength(500)]
        [JsonProperty(Required = Required.Always)]
        public string Description { get; set; }

        public int AssetNodeId { get; set; }

        [ForeignKey("AssetNodeId")]
        public AssetNode AssetNode { get; set; }
    }
}
