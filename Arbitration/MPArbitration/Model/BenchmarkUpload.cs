using System.Text.Json.Serialization;

namespace MPArbitration.Model
{
    public class BenchmarkUploadItem
    {
        [JsonPropertyName("benchmarks")]
        public System.Text.Json.JsonElement? Benchmarks { get; set; } = null;

        [JsonPropertyName("geoZip")]
        public string GeoZip { get; set; } = string.Empty;

        [JsonPropertyName("modifiers")]
        public string Modifiers { get; set; } = string.Empty;

        [JsonPropertyName("procedureCode")]
        public string ProcedureCode { get; set; } = string.Empty;
    }

    public class BenchmarkUpload
    {
        public IEnumerable<BenchmarkUploadItem>? BenchmarkDataItems { get; set; }
    }
}
