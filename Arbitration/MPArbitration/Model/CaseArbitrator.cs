using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public enum ArbitratorDisqualification
    {
        Arbitrator,  // arbitrator disqualifies themselves (sickness, etc - no idea if this happens but seems possible)
        None, // not disqualified
        Payor,  // payor disqualified the arbitrator
        Provider,  // provider disqualified the arbitrator
        TDI  // TDI disqualified the arbitrator for some reason (no idea if this happens but seems possible)
    }

    public class CaseArbitrator
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("assignedOn")]
        public DateTime? AssignedOn { get; set; } = null;

        [JsonPropertyName("disqualifiedBy")]
        [Column(TypeName = "nvarchar(60)")]
        public ArbitratorDisqualification DisqualifiedBy { get; set; }

        [NotMapped]
        [JsonPropertyName("eliminateForServices")]
        public string EliminateForServices { get; set; } = "";

        [NotMapped]
        [JsonPropertyName("email")]
        [StringLength(60)]
        public string Email { get; set; } = "";

        [JsonPropertyName("fee")]
        public double Fee { get; set; } = 0;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [NotMapped]
        [JsonPropertyName("isLastResort")]
        public bool IsLastResort { get; set; }

        [JsonPropertyName("isDismissed")]
        public bool IsDismissed { get; set; }

        [NotMapped]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [NotMapped]
        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";

        [NotMapped]
        [JsonPropertyName("phone")]
        public string Phone { get; set; } = "";

        [NotMapped]
        [JsonPropertyName("statistics")]
        public string Statistics { get; set; } = "";

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("arbitratorId")]
        public int? ArbitratorId { get; set; } = null;

        [JsonPropertyName("arbitrationCaseId")]
        public int? ArbitrationCaseId { get; set; } = null;

        [NotMapped]
        [JsonPropertyName("arbitrator")]
        public virtual Arbitrator? Arbitrator { get; set; } = null;
    }
}
