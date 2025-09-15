using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class Negotiator
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("email")]
        [StringLength(60)]
        public string Email { get; set; } = "";

        [JsonPropertyName("organization")]
        [StringLength(60)]
        public string Organization { get; set; } = "";

        [JsonPropertyName("name")]
        [StringLength(60)]
        public string Name { get; set; } = "";

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";

        [JsonPropertyName("phone")]
        [StringLength(20)]
        public string Phone { get; set; } = "";

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("payorId")]
        public int PayorId { get; set; }

        [ForeignKey("PayorNegotiatorId")]
        [JsonPropertyName("arbitrationCases")]
        public virtual List<ArbitrationCase> ArbitrationCases { get; set; } = new List<ArbitrationCase>();
    }
}
