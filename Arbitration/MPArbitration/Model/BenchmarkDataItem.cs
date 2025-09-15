using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    [Index("BenchmarkDatasetId","GeoZip","Modifiers","ProcedureCode",IsUnique =true)]
    public class BenchmarkDataItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("benchmarks")]
        public string Benchmarks { get; set; } = string.Empty;  // JSON

        [JsonPropertyName("dataset")]
        public virtual BenchmarkDataset? Dataset { get; set; } = null;

        [JsonPropertyName("geoZip")]
        [StringLength(10)]
        public string GeoZip { get; set; } = string.Empty;

        [JsonPropertyName("modifiers")]
        [StringLength(255)]
        public string Modifiers { get; set; } = string.Empty;

        [JsonPropertyName("procedureCode")]
        [StringLength(20)]
        public string ProcedureCode { get; set; } = string.Empty;

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("benchmarkDatasetId")]
        public int BenchmarkDatasetId { get; set; }
    }
}
