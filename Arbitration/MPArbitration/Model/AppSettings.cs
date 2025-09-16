using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class AppSettings
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("JSON")]
        public string JSON { get; set; } = "{}"; // dynamic lists of dropdown values, etc

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = ""; 

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;
    }
}
