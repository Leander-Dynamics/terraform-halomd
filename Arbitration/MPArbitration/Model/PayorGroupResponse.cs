using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    [NotMapped]
    public class PayorGroupResponse
    {
        [JsonPropertyName("itemsAdded")]
        public int ItemsAdded { get; set; } = 0;
        
        [JsonPropertyName("itemsSkipped")]
        public int ItemsSkipped { get; set; } = 0;

        [JsonPropertyName("itemsUpdated")]
        public int ItemsUpdated { get; set; } = 0;

        [JsonPropertyName("payorsSkipped")]
        public string[] PayorsSkipped { get; set; } = { };

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
