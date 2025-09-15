using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    [Index(propertyNames: nameof(Key), IsUnique = true)]
    public class BenchmarkDataset
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [ForeignKey("BenchmarkDatasetId")]
        [JsonIgnore]
        public virtual List<BenchmarkDataItem> BenchmarkItems { get; set; } = new List<BenchmarkDataItem>();

        [ForeignKey("BenchmarkDatasetId")]
        [JsonIgnore]
        public virtual List<CaseBenchmark> CaseBenchmarks { get; set; } = new List<CaseBenchmark>();

        [JsonPropertyName("dataYear")]
        public int DataYear { get; set; } = 0;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("key")]
        [StringLength(20)] 
        public string Key { get; set; } = "";  // e.g. FH-2022-Q4-AVG

        [JsonPropertyName("name")]
        [StringLength(50)]
        public string Name { get; set; } = ""; // e.g. Fair Health 2022 Fourth Quarter Average

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("valueFields")]
        [StringLength(512)]
        public string ValueFields { get; set; } = "";  // these are used as choices when the dataset is added to an Authority

        [JsonPropertyName("vendor")]
        [StringLength(50)]
        public string Vendor { get; set; } = ""; // e.g. Fair Health
    }
}
