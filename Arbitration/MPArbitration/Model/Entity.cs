using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// Per design by the business (MPower), an Entity can only ever belong to a single Customer.
    /// </summary>
    [Index(propertyNames: nameof(Name), IsUnique = false)]
    [Index(propertyNames: nameof(NPINumber), IsUnique = true)]
    public class Entity
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("customerId")]
        public int? CustomerId { get; set; } = null;

        [StringLength(60)]
        [JsonPropertyName("address")]
        public string Address { get; set; } = "";

        [StringLength(40)]
        [JsonPropertyName("city")]
        public string City { get; set; } = "";

        [JsonPropertyName("JSON")]
        public string JSON { get; set; } = "{}"; // additional settings including various email addresses and contact values

        [StringLength(80)]
        [JsonPropertyName("name")]
        [Required]
        public string Name { get; set; } = "";

        [StringLength(60)]
        [JsonPropertyName("ownerName")]
        public string OwnerName { get; set; } = "";

        [StringLength(30)]
        [JsonPropertyName("ownerTaxId")]
        public string OwnerTaxId { get; set; } = "";

        [StringLength(40)]
        [JsonPropertyName("NPINumber")]
        [Required]
        public string NPINumber { get; set; } = "";

        [StringLength(2)]
        [JsonPropertyName("state")]
        public string State { get; set; } = "";

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [StringLength(10)]
        [JsonPropertyName("zipCode")]
        public string ZipCode { get; set; } = "";
    }
}
