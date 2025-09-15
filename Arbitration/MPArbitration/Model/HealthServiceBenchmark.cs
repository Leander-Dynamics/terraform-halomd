using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class HealthServiceBenchmark
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("dataYear")]
        public int DataYear { get; set; } = 0;
        
        [JsonPropertyName("fh50thPercentileCharges")]
        [Column(TypeName = "decimal(22,9)")]
        public decimal FH50thPercentileCharges { get; set; } = 0;

        [JsonPropertyName("fh80thPercentileCharges")]
        [Column(TypeName = "decimal(22,9)")]
        public decimal FH80thPercentileCharges { get; set; } = 0;

        [JsonPropertyName("geozip")]
        [StringLength(3)]
        public string Geozip { get; set; } = "";

        [JsonPropertyName("modifier")]
        [StringLength(2)]
        public string? Modifier { get; set; }

        [JsonPropertyName("procedureCode")]
        [StringLength(10)]
        public string Procedure_Code { get; set; } = "";

        [JsonPropertyName("reportYear")]
        public int ReportYear { get; set; } = 0;

        [JsonPropertyName("source")]
        [StringLength(8)]
        public string Source { get; set; } = "";

        [JsonPropertyName("state")]
        [StringLength(8)]
        public string State { get; set; } = "";

    }
}