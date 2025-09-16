using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class BaseAttachment
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("blobLink")]
        [StringLength(512)]
        public string BLOBLink { get; set; } = "";

        [Required]
        [JsonPropertyName("blobName")]
        [StringLength(255)]
        public string BLOBName { get; set; } = "";

        [JsonPropertyName("createdBy")]
        [StringLength(60)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [Required]
        [JsonPropertyName("docType")]
        public string DocType { get; set; } = string.Empty;

        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;
    }

    [Index(nameof(ArbitrationCaseId), nameof(DocType))]
    public class EMRClaimAttachment : BaseAttachment
    {
        [Required]
        [JsonPropertyName("arbitrationCaseId")]
        public int ArbitrationCaseId { get; set; } = 0;

    }

    [Index(nameof(AuthorityDisputeId), nameof(DocType), IsUnique = false)]
    public class AuthorityDisputeAttachment : BaseAttachment
    {
        [Required]
        [JsonPropertyName("authorityDisputeId")]
        public int AuthorityDisputeId { get; set; } = 0;

    }
}
