using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace MPArbitration.Model
{
    [Index(nameof(PayorId),nameof(AddressType),IsUnique=true)]
    public class PayorAddress
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("addressLine1")]
        [MaxLength(100)]
        public string AddressLine1 { get; set; } = "";

        [JsonPropertyName("addressLine2")]
        [MaxLength(100)]
        public string AddressLine2 { get; set; } = "";

        [JsonPropertyName("addressType")]
        [Required]
        [StringLength(30)]
        public string AddressType { get; set; } = "";

        [JsonPropertyName("city")]
        [MaxLength(100)]
        public string City { get; set; } = "";

        [JsonPropertyName("email")]
        [MaxLength(60)]
        public string Email { get; set; } = "";

        [JsonPropertyName("name")]
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = "";

        [JsonPropertyName("phone")]
        [StringLength(20)]
        public string Phone { get; set; } = "";

        [JsonPropertyName("stateCode")]
        [MaxLength(2)]
        public string StateCode { get; set; } = "";

        [JsonPropertyName("zipCode")]
        [MaxLength(10)]
        public string ZipCode { get; set; } = "";

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("payorId")]
        public int PayorId { get; set; } = 0;
    }
}
