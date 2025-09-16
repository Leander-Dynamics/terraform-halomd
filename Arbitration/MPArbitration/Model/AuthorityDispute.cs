using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class AuthorityDisputeCSV
    {
        [JsonPropertyName("arbitratorId")]
        public int? ArbitratorId { get; set; } = null;

        [JsonPropertyName("arbitrationResult")]
        public ArbitrationResult ArbitrationResult { get; set; } = ArbitrationResult.None;

        [JsonPropertyName("arbitratorSelectedOn")]
        public DateTime? ArbitratorSelectedOn { get; set; } = null;

        [JsonPropertyName("authorityKey")]
        [Required]
        [StringLength(3)]
        public string AuthorityKey { get; set; } = "";

        [JsonPropertyName("authorityCaseId")]
        [StringLength(30)]
        public string AuthorityCaseId { get; set; } = "";  // aka "Dispute Number"

        [JsonPropertyName("authorityStatus")]
        [StringLength(60)]
        [Required]
        public string AuthorityStatus { get; set; } = "";

        [JsonPropertyName("briefApprovedOn")]
        public DateTime? BriefApprovedOn { get; set; } = null;

        [JsonPropertyName("briefApprovedBy")]
        [StringLength(60)]
        public string BriefApprovedBy { get; set; } = "";

        [JsonPropertyName("briefPreparer")]
        [StringLength(60)]
        public string BriefPreparer { get; set; } = "";

        [JsonPropertyName("briefPreparationCompletedOn")]
        public DateTime? BriefPreparationCompletedOn { get; set; } = null;

        [JsonPropertyName("briefWriter")]
        [StringLength(60)]
        public string BriefWriter { get; set; } = "";

        [JsonPropertyName("briefWriterCompletedOn")]
        public DateTime? BriefWriterCompletedOn { get; set; } = null;

        [JsonPropertyName("createdBy")]
        [StringLength(60)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("ineligibilityAction")]
        [StringLength(50)]
        public string IneligibilityAction { get; set; } = "";

        [JsonPropertyName("ineligibilityReasons")]
        [StringLength(1024)]
        public string IneligibilityReasons { get; set; } = "";

        [Required]
        [JsonPropertyName("submissionDate")]
        public DateTime SubmissionDate { get; set; }

        [JsonPropertyName("trackingValues")]
        [StringLength(1024)]
        public string TrackingValues { get; set; } = "{}"; // JSON 

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("workflowStatus")]
        [Column(TypeName = "nvarchar(60)")]
        [Required]
        public ArbitrationStatus WorkflowStatus { get; set; }
    }

    [Index(nameof(AuthorityId), nameof(AuthorityCaseId), IsUnique = true)]
    [Index(nameof(AuthorityId), nameof(AuthorityStatus), IsUnique = false)]
    [Index(nameof(WorkflowStatus), nameof(AuthorityId), nameof(AuthorityCaseId), IsUnique = false)]
    [Index(nameof(AuthorityId), nameof(AuthorityStatus), nameof(ArbitrationResult), nameof(AuthorityCaseId), IsUnique = false)]
    public class AuthorityDispute
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("arbitrator")]
        public virtual Arbitrator? Arbitrator { get; set; } = null!;  // aka "Certified Entity"

        [JsonPropertyName("arbitratorId")]
        public int? ArbitratorId { get; set; } = null;

        [JsonPropertyName("arbitrationResult")]
        [Column(TypeName = "nvarchar(60)")]
        public ArbitrationResult ArbitrationResult { get; set; } = ArbitrationResult.None;

        [JsonPropertyName("arbitratorSelectedOn")]
        public DateTime? ArbitratorSelectedOn { get; set; } = null; // aka "Certified Entity Selection Date"

        [ForeignKey("AuthorityDisputeId")]
        [JsonPropertyName("attachments")]
        public virtual List<AuthorityDisputeAttachment> Attachments { get; set; } = new List<AuthorityDisputeAttachment>();

        [JsonPropertyName("authority")]
        public virtual Authority? Authority { get; set; } = null;

        [JsonPropertyName("authorityId")]
        [Required]
        public int AuthorityId { get; set; } = 0;

        [JsonPropertyName("authorityCaseId")]
        [StringLength(30)]
        public string AuthorityCaseId { get; set; } = "";  // aka "Dispute Number"

        [JsonPropertyName("authorityStatus")]
        [StringLength(60)]
        public string AuthorityStatus { get; set; } = "";

        [JsonPropertyName("briefApprovedOn")]
        public DateTime? BriefApprovedOn { get; set; } = null;

        [JsonPropertyName("briefApprovedBy")]
        [StringLength(60)]
        public string BriefApprovedBy { get; set; } = "";

        [JsonPropertyName("briefPreparer")]
        [StringLength(60)]
        public string BriefPreparer { get; set; } = "";

        [JsonPropertyName("briefPreparationCompletedOn")]
        public DateTime? BriefPreparationCompletedOn { get; set; } = null;

        [JsonPropertyName("briefWriter")]
        [StringLength(60)]
        public string BriefWriter { get; set; } = "";

        [JsonPropertyName("briefWriterCompletedOn")]
        public DateTime? BriefWriterCompletedOn { get; set; } = null;

        [NotMapped]
        [JsonPropertyName("cptViewmodels")]
        public List<AuthorityDisputeCPTVM> CPTViewmodels { get; set; } = new List<AuthorityDisputeCPTVM>();

        [JsonPropertyName("createdBy")]
        [StringLength(60)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("ineligibilityAction")]
        [StringLength(50)]
        public string IneligibilityAction { get; set; } = "";

        [JsonPropertyName("ineligibilityReasons")]
        [StringLength(1024)]
        public string IneligibilityReasons { get; set; } = "";

        [ForeignKey("AuthorityDisputeId")]
        [JsonPropertyName("disputeCPTs")]
        public virtual List<AuthorityDisputeCPT> DisputeCPTs { get; set; } = new List<AuthorityDisputeCPT>();

        [ForeignKey("AuthorityDisputeId")]
        [JsonPropertyName("fees")]
        public virtual List<AuthorityDisputeFee> Fees { get; set; } = new List<AuthorityDisputeFee>();

        [ForeignKey("AuthorityDisputeId")]
        [JsonPropertyName("changeLog")]
        public virtual List<AuthorityDisputeLog> ChangeLog { get; set; } = new List<AuthorityDisputeLog>();

        [ForeignKey("AuthorityDisputeId")]
        [JsonPropertyName("notes")]
        public virtual List<AuthorityDisputeNote> Notes { get; set; } = new List<AuthorityDisputeNote>();

        [Required]
        [JsonPropertyName("submissionDate")]
        public DateTime SubmissionDate { get; set; }

        [JsonPropertyName("trackingValues")]
        [StringLength(1024)]
        public string TrackingValues { get; set; } = "{}"; // JSON 

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("workflowStatus")]
        [Column(TypeName = "nvarchar(60)")]
        public ArbitrationStatus WorkflowStatus { get; set; }

        // Viewmodel fields
        [NotMapped]
        [JsonPropertyName("customers")]
        public string Customers { get; set; } = ""; // this could contain delimited values for obvious reasons

        [NotMapped]
        [JsonPropertyName("linkedClaimIDs")]
        public string LinkedClaimIDs { get; set; } = ""; // this could contain delimited values for obvious reasons

        [NotMapped]
        [JsonPropertyName("patientNames")]
        public string PatientNames { get; set; } = ""; // this could contain delimited values for obvious reasons
    }

    [NotMapped]
    public class AuthorityDisputeInit
    {
        [JsonPropertyName("auth")]
        public string Auth { get; set; } = "";

        [JsonPropertyName("authorityCaseId")]
        public string AuthorityCaseId { get; set; } = "";

        // List of AritrationCase ID numbers (aka Arbit IDs)
        [JsonPropertyName("claims")]
        public string Claims { get; set; } = "";

        // Either a single CPT value or an asterisk
        [JsonPropertyName("cpt")]
        public string CPT { get; set; } = "";

    }
}
