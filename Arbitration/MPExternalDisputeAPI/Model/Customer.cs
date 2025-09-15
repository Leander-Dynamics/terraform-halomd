using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace MPExternalDisputeAPI.Model
{
    [Index(propertyNames: nameof(Name), IsUnique = true)]
    public class Customer
    {
        /// <summary>
        /// id
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Entities
        /// </summary>
        public virtual List<Entity> Entities { get; set; } = new List<Entity>();

        /// <summary>
        /// Stats
        /// </summary>
        [NotMapped]
        [JsonPropertyName("stats")]
        public JsonNode? Stats { get; set; } = null;

        /// <summary>
        /// CreatedBy
        /// </summary>
        [JsonPropertyName("createdBy")]
        [StringLength(50)]
        public string CreatedBy { get; set; } = "";

        /// <summary>
        /// CreatedOn
        /// </summary>
        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        /// <summary>
        /// DefaultAuthority
        /// </summary>
        [JsonPropertyName("defaultAuthority")]
        [StringLength(8)]
        public string DefaultAuthority { get; set; } = "";

        /// <summary>
        /// EHRSystem
        /// </summary>
        [JsonPropertyName("EHRSystem")]
        [StringLength(50)]
        public string EHRSystem { get; set; } = "";

        /// <summary>
        /// IsActive
        /// </summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;  // Inactive customers cannot receive new import data

        /// <summary>
        /// JSON
        /// </summary>
        [JsonPropertyName("JSON")]
        public string JSON { get; set; } = "{}"; // additional settings including personalized templates with embedded PNGs (eventually :)

        /// <summary>
        /// Name
        /// </summary>
        [StringLength(50)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// UpdatedBy
        /// </summary>
        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        /// <summary>
        /// UpdatedOn
        /// </summary>
        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        /// <summary>
        /// ArbitCasesCount
        /// </summary>
        [NotMapped]
        [JsonPropertyName("arbitCasesCount")]
        public Int16 ArbitCasesCount { get; set; } = 0;

        /// <summary>
        /// AgreementStartDate
        /// </summary>
        public DateTime? AgreementStartDate { get; set; }

        /// <summary>
        /// AgreementEndDate
        /// </summary>
        public DateTime? AgreementEndDate { get; set; }

        /// <summary>
        /// ExternalPartnerName
        /// </summary>
        public string? ExternalPartnerName { get; set; }
    }

}
