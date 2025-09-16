using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace MPArbitration.Model
{
    [Index(propertyNames: nameof(Name), IsUnique = true)]
    public class Customer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        public virtual List<Entity> Entities { get; set; } = new List<Entity>();

        [NotMapped]
        [JsonPropertyName("stats")]
        public JsonNode? Stats { get; set; } = null;

        [JsonPropertyName("createdBy")]
        [StringLength(50)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("defaultAuthority")]
        [StringLength(8)]
        public string DefaultAuthority { get; set; } = "";

        [JsonPropertyName("EHRSystem")]
        [StringLength(50)]
        public string EHRSystem { get; set; } = "";

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;  // Inactive customers cannot receive new import data

        [JsonPropertyName("JSON")]
        public string JSON { get; set; } = "{}"; // additional settings including personalized templates with embedded PNGs (eventually :)

        [StringLength(50)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [NotMapped]
        [JsonPropertyName("arbitCasesCount")]
        public int ArbitCasesCount { get; set; } = 0;
    }

}
