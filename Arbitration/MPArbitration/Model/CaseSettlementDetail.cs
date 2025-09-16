using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// Used to record received or disbursed payments against a CaseSettlement.
    /// NOTE: These should directly map to EOBs issued by a Payor, not "future expected payments".
    /// The CaseSettlement is used to track expected awards or liabilities as the result of 
    /// formal or informal negotiations. 
    /// TODO: Once existing data is refactord and split, the "move to CaseSettlement" properties should be deleted.
    /// </summary>
    [Index(nameof(AuthorityId), IsUnique = false)]
    public class CaseSettlementDetail
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("arbitrationCaseId")]
        public int ArbitrationCaseId { get; set; } = 0; // move to CaseSettlement

        [JsonPropertyName("authorityId")]
        public int? AuthorityId { get; set; } = null; 

        //todo: prob need a new settlement type record to capture Informal or Formal to denote this as "our" view of the situation

        [JsonPropertyName("authorityCaseId")]
        [StringLength(30)]
        public string AuthorityCaseId { get; set; } = ""; // move to CaseSettlement

        [JsonPropertyName("additionalPaidAmount")]
        public double AdditionalPaidAmount { get; set; } = 0;  // amount paid as an award to the winner of the decision

        [JsonPropertyName("arbitrationDecisionDate")]
        public DateTime? ArbitrationDecisionDate { get; set; } = null; // move to CaseSettlement

        [JsonPropertyName("arbitratorReportSubmissionDate")]
        public DateTime? ArbitratorReportSubmissionDate { get; set; } = null; // move to CaseSettlement

        // Foreign Key - nullable to support data migration for now
        [JsonPropertyName("caseSettlementId")]
        public int? CaseSettlementId { get; set; } = null;

        [JsonPropertyName("CreatedBy")]
        [StringLength(60)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        [JsonPropertyName("JSON")]
        [StringLength(1024)]
        public string JSON { get; set; } = "{}"; // Room for other details

        [JsonPropertyName("methodOfPayment")]
        [StringLength(255)]
        public string MethodOfPayment { get; set; } = "";

        [JsonPropertyName("partiesAwardNotificationDate")]
        public DateTime? PartiesAwardNotificationDate { get; set; } = null;  // move to CaseSettlement

        [JsonPropertyName("paymentMadeDate")]
        public DateTime? PaymentMadeDate { get; set; } = null;

        [JsonPropertyName("paymentReferenceNumber")]
        [StringLength(255)]
        public string PaymentReferenceNumber { get; set; } = "";

        /* New Fields for Capturing Authority Offers on a Per-Case Basis */
        [JsonPropertyName("payorFinalOfferAmount")]
        public double PayorFinalOfferAmount { get; set; } = 0;

        [JsonPropertyName("providerFinalOfferAmount")]
        public double ProviderFinalOfferAmount { get; set; } = 0;
        /*   */

        [JsonPropertyName("reasonableAmount")]
        public double ReasonableAmount { get; set; } = 0;  // move to CaseSettlement

        [JsonPropertyName("totalSettlementAmount")]
        public double TotalSettlementAmount { get; set; } = 0;  // move to CaseSettlement

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("wasSettledAtArbitration")]
        public bool WasSettledAtArbitration { get; set; }  // move to CaseSettlement

        [JsonPropertyName("wasPayorPaymentReceived")]
        public bool WasPayorPaymentReceived { get; set; }  // move to CaseSettlement

        [JsonPropertyName("wasPayorPaymentTimely")]
        public bool WasPayorPaymentTimely { get; set; }  // move to CaseSettlement

        [JsonPropertyName("wasProviderPaymentReceived")]
        public bool WasProviderPaymentReceived { get; set; }  // move to CaseSettlement

        [JsonPropertyName("wasProviderPaymentTimely")]
        public bool WasProviderPaymentTimely { get; set; }  // move to CaseSettlement

        [JsonPropertyName("prevailingParty")] // formerly Winner - copied to CaseSettlement
        [StringLength(50)]
        public string PrevailingParty { get; set; } = "";  // i.e. TDI's [Final Offer Closest To Reasonable]
    }

    [Index(nameof(AuthorityId), nameof(AuthorityCaseId), nameof(ArbitrationCaseId), IsUnique = true)]
    [Index(nameof(ArbitrationCaseId), IsUnique = false)]
    public class CaseSettlement
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        // virtual properties
        [ForeignKey("CaseSettlementId")]
        [JsonPropertyName("caseSettlementDetails")]
        public virtual List<CaseSettlementDetail> CaseSettlementDetails { get; set; } = new List<CaseSettlementDetail>();

        // virtual properties
        [ForeignKey("CaseSettlementId")]
        [JsonPropertyName("caseSettlementCPTs")]
        public virtual List<CaseSettlementCPT> CaseSettlementCPTs { get; set; } = new List<CaseSettlementCPT>();

        [ForeignKey("CaseSettlementId")]
        [JsonPropertyName("offer")]
        public virtual OfferHistory? Offer { get; set; } = null;

        // properties
        [JsonPropertyName("arbitrationCaseId")]
        public int? ArbitrationCaseId { get; set; } = null;
        
        [JsonPropertyName("authorityId")]
        public int? AuthorityId { get; set; } = null; // NULL means informal settlement

        [JsonPropertyName("authorityCaseId")]
        [StringLength(30)]
        public string? AuthorityCaseId { get; set; } = "";

        [JsonPropertyName("arbitrationDecisionDate")]
        public DateTime? ArbitrationDecisionDate { get; set; } = null;

        [JsonPropertyName("arbitratorReportSubmissionDate")]
        public DateTime? ArbitratorReportSubmissionDate { get; set; } = null;

        [JsonPropertyName("CreatedBy")]
        [StringLength(60)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("grossSettlementAmount")]
        public double GrossSettlementAmount { get; set; } = 0;

        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        [JsonPropertyName("JSON")]
        [StringLength(1024)]
        public string JSON { get; set; } = "{}"; // Room for other details

        // this would have to be continuously recalculated when new payments are received - not gonna attempt that yet - needs to be handled by on-the-fly calculation
        //[JsonPropertyName("netSettlementAmount")]
        //public double NetSettlementAmount { get; set; } = 0;  

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";

        [JsonPropertyName("partiesAwardNotificationDate")]
        public DateTime? PartiesAwardNotificationDate { get; set; } = null;

        [JsonPropertyName("payorClaimNumber")]
        [StringLength(50)]
        public string PayorClaimNumber { get; set; } = "";

        [JsonPropertyName("payorId")]
        public int PayorId { get; set; } = 0;

        [JsonPropertyName("reasonableAmount")]
        public double ReasonableAmount { get; set; } = 0;  // as per the Authority

        [JsonPropertyName("totalSettlementAmount")]
        public double TotalSettlementAmount { get; set; } = 0;

        [JsonPropertyName("wasSettledAtArbitration")]
        public bool WasSettledAtArbitration { get; set; }

        [JsonPropertyName("wasPayorPaymentReceived")]
        public bool WasPayorPaymentReceived { get; set; }

        [JsonPropertyName("wasPayorPaymentTimely")]
        public bool WasPayorPaymentTimely { get; set; }

        [JsonPropertyName("wasProviderPaymentReceived")]
        public bool WasProviderPaymentReceived { get; set; }

        [JsonPropertyName("wasProviderPaymentTimely")]
        public bool WasProviderPaymentTimely { get; set; }

        [JsonPropertyName("prevailingParty")]
        [StringLength(50)]
        public string PrevailingParty { get; set; } = "";  // i.e. TDI's Final Offer Closest To Reasonable

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

    }

    [Index(nameof(CaseSettlementId), nameof(ClaimCPTId), IsUnique = true)]
    public class CaseSettlementCPT
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("caseSettlementId")]
        public int CaseSettlementId { get; set; }

        [JsonPropertyName("claimCPTId")]
        public int ClaimCPTId { get; set; }

        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; } = false; /** MPower field - allows soft delete and history tracking to continue */

        [JsonPropertyName("perUnitAwardAmount")]
        public double PerUnitAwardAmount { get; set; } = 0;

        [JsonPropertyName("units")]
        public int Units { get; set; } = 0;

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

    }
}
