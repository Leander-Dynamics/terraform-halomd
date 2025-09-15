using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    [Index(nameof(AuthorityId), nameof(AuthorityCaseId), IsUnique =true)]
    [Index(nameof(AuthorityId), nameof(AuthorityStatus), nameof(CreatedOn), nameof(CreatedBy))]
    public class CaseArchive
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("authorityCaseId")]
        [StringLength(30)]
        public string AuthorityCaseId { get; set; } = ""; /** e.g. TDI RequestID */

        [JsonPropertyName("authorityStatus")]
        [StringLength(60)]
        public string AuthorityStatus { get; set; } = ""; /** e.g. TDI Status */

        [JsonPropertyName("authorityWorkflowStatus")]
        [Column(TypeName = "nvarchar(60)")]
        public ArbitrationStatus AuthorityWorkflowStatus { get; set; } = ArbitrationStatus.New;

        [JsonPropertyName("JSON")]  // Other data we want to archive like all of the Tracking dates
        [StringLength(4096)]
        public string JSON { get; set; } = "{}";

        [JsonPropertyName("createdBy")]
        [StringLength(60)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        // relationships
        [JsonPropertyName("authorityId")]
        public int AuthorityId { get; set; }

        [JsonPropertyName("arbitrationCaseId")]
        public int ArbitrationCaseId { get; set; }
    }
}
