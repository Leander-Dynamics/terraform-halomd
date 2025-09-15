using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPExternalDisputeAPI.Model
{
    [Index(nameof(ArbitrationCaseId),nameof(CPTCode))]
    public class ClaimCPT
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        // virtual properties
        [ForeignKey("ClaimCPTId")]
        [JsonPropertyName("caseSettlementCPTs")]
        public virtual List<CaseSettlementCPT> CaseSettlementCPTs { get; set; } = new List<CaseSettlementCPT>();
        
        //[ForeignKey("ClaimCPTId")]
        //[JsonPropertyName("disputeCPTs")]
        //public virtual List<AuthorityDisputeCPT> DisputeCPTs { get; set; } = new List<AuthorityDisputeCPT>();

        // properties
        [JsonPropertyName("cptCode")]
        [StringLength(20)]
        public string CPTCode { get; set; } = "";

        [JsonPropertyName("createdBy")]
        [StringLength(100)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        // Fair Health CPT Data
        [JsonPropertyName("fh50thPercentileCharges")]
        public double FH50thPercentileCharges { get; set; } = 0;  // should be called Payor Allowed
        [JsonPropertyName("fh50thPercentileExtendedCharges")]
        public double FH50thPercentileExtendedCharges { get; set; } = 0;

        [JsonPropertyName("fh80thPercentileCharges")]
        public double FH80thPercentileCharges { get; set; } = 0; // should be called Provider Charges
        [JsonPropertyName("fh80thPercentileExtendedCharges")]
        public double FH80thPercentileExtendedCharges { get; set; } = 0; 

        [NotMapped]
        [JsonPropertyName("description")]
        [StringLength(300)]
        public string Description { get; set; } = "";  // ViewModel property used by the document templating utilities

        /** MPower field - allows soft delete and history tracking to continue */
        [JsonPropertyName("isDeleted")]
        public bool isDeleted { get; set; } = false; 
        [JsonPropertyName("isEligible")]
        public bool IsEligible { get; set; } = false;
        [JsonPropertyName("isIncluded")]
        public bool IsIncluded { get; set; } = false;
        [JsonPropertyName("isLocked")]
        public bool IsLocked { get; set; } = false;
        [JsonPropertyName("modifiers")]
        public string Modifiers { get; set; } = "";  // EHR flags
        [JsonPropertyName("modifier26_YN")]
        public bool Modifier26_YN { get; set; } = false;
        [JsonPropertyName("paidAmount")]
        public double PaidAmount { get; set; } = 0; // how much the payor has already paid on this CPT
        [JsonPropertyName("patientRespAmount")]
        public double PatientRespAmount { get; set; } = 0; // how much the patient is responsible form
        [JsonPropertyName("providerChargeAmount")]
        public double ProviderChargeAmount { get; set; } = 0;  // how much was originally charged for this CPT
        [JsonPropertyName("units")]
        public double Units { get; set; } = 0;
        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";
        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("arbitrationCaseId")]
        public int ArbitrationCaseId { get; set; } = 0;
    }
}
