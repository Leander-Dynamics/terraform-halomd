using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class BenchmarkDataItemVM
    {
        // AuthorityBenchmarkDetails properties
        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; } = false;

        [JsonPropertyName("service")]
        public string Service { get; set; } = string.Empty;

        [JsonPropertyName("formLabels")]
        public string FromLabels { get; set; } = string.Empty;

        [JsonPropertyName("tableLabels")]
        public string TableLabels { get; set; } = string.Empty;

        [JsonPropertyName("valueFields")]
        public string ValueFields { get; set; } = string.Empty;

        // BenchmarkDataset properties
        [JsonPropertyName("dataYear")]
        public int DataYear { get; set; } = 0;

        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("vendor")]
        public string Vendor { get; set; } = string.Empty;

        // BenchmarkDataItem properties
        [JsonPropertyName("benchmarks")]
        public string Benchmarks { get; set; } = string.Empty;

        [JsonPropertyName("geoZip")]
        public string GeoZip { get; set; } = string.Empty;

        [JsonPropertyName("modifiers")]
        public string Modifiers { get; set; } = string.Empty;

        [JsonPropertyName("procedureCode")]
        public string ProcedureCode { get; set; } = string.Empty;
    }
}
