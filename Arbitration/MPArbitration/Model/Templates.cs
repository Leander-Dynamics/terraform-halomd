using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;


namespace MPArbitration.Model
{
    public class Template
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("componentType")]
        [StringLength(50)]
        public string ComponentType { get; set; } = "";

        [JsonPropertyName("createdBy")]
        [StringLength(100)]
        public string CreatedBy { get; set; } = "";
        
        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("description")]
        [StringLength(4096)]
        public string Description { get; set; } = "";

        [Required]
        [JsonPropertyName("name")]
        [MinLength(1, ErrorMessage ="Name cannot be an empty string")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("html")]
        public string HTML { get; set; } = string.Empty;

        [JsonPropertyName("JSON")]
        [MinLength(2, ErrorMessage = "JSON cannot be an empty string. Try {} instead.")]
        public string JSON { get; set; } = "{}";

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

    }
}
