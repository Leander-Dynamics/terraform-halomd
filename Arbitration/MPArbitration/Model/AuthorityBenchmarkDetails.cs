using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class AuthorityBenchmarkDetails
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; } = true; // when true, this benchmark's data will automaticaly be added to an ArbitrationCase

        [JsonPropertyName("additionalAllowedFields")]
        [MaxLength(255)]
        public string AdditionalAllowedFields { get; set; } = string.Empty;  // delimited. can be substituted in place of the Allowed field in formulas

        [JsonPropertyName("additionalChargesFields")]
        [MaxLength(255)]
        public string AdditionalChargesFields { get; set; } = string.Empty;  // delimited. can be substituted in place of the Charges field in formulas

        [JsonPropertyName("payorAllowedField")]
        [MaxLength(50)]
        public string PayorAllowedField { get; set; } = string.Empty; // mapped to the fh50thPercentile field in ClaimCPT and rolled up to the same field in ArbitrationClaim
        
        [JsonPropertyName("providerChargesField")]
        [MaxLength(50)]
        public string ProviderChargesField { get; set; } = string.Empty;  // mapped to the fh80thPercentile field in ClaimCPT and rolled up to the same field in ArbitrationClaim

        [JsonPropertyName("service")]
        [MaxLength(30)]
        public string Service { get; set; } = string.Empty;  // e.g. IOM Pro

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        // relationships
        [JsonPropertyName("authorityId")]
        public int AuthorityId { get; set; }

        [JsonPropertyName("benchmarkDatasetId")]
        public int BenchmarkDatasetId { get; set; }

        [JsonPropertyName("benchmarkDataset")]
        public BenchmarkDataset? BenchmarkDataset { get; set; } = null;
    }
}
