using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class Holiday
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [MaxLength(50)]
        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;
        
        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; } = null;

        [MaxLength(100)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        [JsonPropertyName("region")]
        public string Region { get; set; } = string.Empty;

        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; } = null;

    }
}
