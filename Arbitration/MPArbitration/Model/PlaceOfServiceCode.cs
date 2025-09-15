using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    [Index(nameof(CodeNumber), IsUnique = true)]
    public class PlaceOfServiceCode
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [Required]
        [StringLength(100)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(4)]  // allow for future expansion
        [JsonPropertyName("codeNumber")]
        public string CodeNumber { get; set; } = string.Empty;

        [JsonPropertyName("effectiveDate")]
        public DateTime? EffectiveDate { get; set; } = null;
    }
}
