using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class AuthorityImportDetails
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("batchUploadDate")]
        public DateTime BatchUploadDate { get; set; } = new DateTime();

        // All of the defined ImportConfiguration fields (even those that might have a dedicated field in this class) are stored in this column
        [JsonPropertyName("JSON")]
        [StringLength(4096)]
        public string JSON { get; set; } = "{}";

        [JsonPropertyName("uploadedBy")]
        [StringLength(50)]
        public string UploadedBy { get; set; } = "";

        // relationships
        [JsonPropertyName("authorityId")]
        public int AuthorityId { get; set; }
    }
}
