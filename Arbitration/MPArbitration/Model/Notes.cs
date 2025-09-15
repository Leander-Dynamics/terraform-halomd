using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class BaseNote
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;
        
        [JsonPropertyName("details")]
        public string Details { get; set; } = "";

        [StringLength(60)]
        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = "";
        
        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;
    }

    [Index(nameof(ArbitrationCaseId), IsUnique = false)]
    public class Note : BaseNote
    {
        [JsonPropertyName("arbitrationCaseId")]
        public int ArbitrationCaseId { get; set; } = 0;
    }

    [Index(nameof(AuthorityDisputeId), IsUnique = false)]
    public class AuthorityDisputeNote : BaseNote
    {
        [JsonPropertyName("authorityDisputeId")]
        public int AuthorityDisputeId { get; set; } = 0;
    }

    public class AuthorityDisputeNoteCSV
    {
        /// <summary>
        /// Combine with AuthorityCaseId to reference the targeted AuthorityDispute without using a database Id.
        /// </summary>
        [JsonPropertyName("authorityKey")]
        [Required]
        [StringLength(3)]
        public string AuthorityKey { get; set; } = "";

        /// <summary>
        /// Combine with AuthorityId to reference the targeted AuthorityDispute without using a database Id.
        /// </summary>
        [Required]
        [JsonPropertyName("authorityCaseId")]
        [StringLength(30)]
        public string AuthorityCaseId { get; set; } = "";  // aka "Dispute Number"

        [Required]
        [JsonPropertyName("details")]
        public string Details { get; set; } = "";
    }
}
