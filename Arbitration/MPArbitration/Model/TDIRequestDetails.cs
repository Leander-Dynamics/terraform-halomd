using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{

    [Index(nameof(RequestId), nameof(BatchUploadDate))]
    public class TDIRequestDetails
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("action")]
        [StringLength(20)]
        public string Action { get; set; } = "";

        [JsonPropertyName("additionalPaidAmount")]
        public double AdditionalPaidAmount { get; set; } = 0;

        [JsonPropertyName("arbitrationDate")]
        public DateTime? ArbitrationDate { get; set; } = null;

        [JsonPropertyName("arbitrationDecisionDate")]
        public DateTime? ArbitrationDecisionDate { get; set; } = null;

        [JsonPropertyName("arbitratorReportSubmissionDate")]
        public DateTime? ArbitratorReportSubmissionDate { get; set; } = null;

        [JsonPropertyName("arbitrator1")]
        [StringLength(100)]
        public string Arbitrator1 { get; set; } = "";

        [JsonPropertyName("arbitrator2")]
        [StringLength(100)]
        public string Arbitrator2 { get; set; } = "";

        [JsonPropertyName("arbitrator3")]
        [StringLength(100)]
        public string Arbitrator3 { get; set; } = "";

        [JsonPropertyName("arbitrator4")]
        [StringLength(100)]
        public string Arbitrator4 { get; set; } = "";

        [JsonPropertyName("arbitrator5")]
        [StringLength(100)]
        public string Arbitrator5 { get; set; } = "";

        [JsonPropertyName("assignmentDeadlineDate")]
        public DateTime? AssignmentDeadlineDate { get; set; } = null;

        [JsonPropertyName("batchUploadDate")]
        public DateTime BatchUploadDate { get; set; } = new DateTime();

        [JsonPropertyName("payorClaimNumber")]
        [StringLength(50)]
        public string PayorClaimNumber { get; set; } = "";

        [JsonPropertyName("createdBy")]
        [StringLength(60)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("daysOpenCount")]
        public int DaysOpenCount { get; set; } = 0;

        [JsonPropertyName("disputedAmount")]
        public double DisputedAmount { get; set; } = 0;

        [NotMapped]
        [JsonPropertyName("DOB")]
        public DateTime? DOB { get; set; } = null;

        [JsonPropertyName("entity")]
        [StringLength(60)]
        public string Entity { get; set; } = ""; /* provider entity */

        [JsonPropertyName("entityNPI")]
        [StringLength(10)]
        public string EntityNPI { get; set; } = "";

        [JsonPropertyName("estimatedDisputedAmount")]
        public double EstimatedDisputedAmount { get; set; } = 0;

        [JsonPropertyName("healthPlanName")]
        [StringLength(100)]
        public string HealthPlanName { get; set; } = ""; /* Payor */

        [JsonPropertyName("history")]
        [StringLength(255)]
        public string History { get; set; } = "";

        [JsonPropertyName("ineligibilityReason")]
        [StringLength(2048)]
        public string IneligibilityReason { get; set; } = "";

        [JsonPropertyName("informalTeleconferenceDate")]
        public DateTime? InformalTeleconferenceDate { get; set; } = null;

        [JsonPropertyName("methodOfPayment")]
        [StringLength(50)]
        public string MethodOfPayment { get; set; } = "";

        [JsonPropertyName("originalBilledAmount")]
        public double OriginalBilledAmount { get; set; } = 0;

        [JsonPropertyName("partiesAwardNotificationDate")]
        public DateTime? PartiesAwardNotificationDate { get; set; } = null;

        [JsonPropertyName("patientName")]
        [StringLength(60)]
        public string PatientName { get; set; } = "";

        [JsonPropertyName("patientShareAmount")]
        public double PatientShareAmount { get; set; } = 0;

        [JsonPropertyName("paymentMadeDate")]
        public DateTime? PaymentMadeDate { get; set; } = null;

        [JsonPropertyName("paymentReferenceNumber")]
        [StringLength(80)]
        public string PaymentReferenceNumber { get; set; } = "";

        [JsonPropertyName("payorFinalOfferAmount")]
        public double PayorFinalOfferAmount { get; set; } = 0;

        [JsonPropertyName("payorResolutionRequestReceivedDate")]
        public DateTime? PayorResolutionRequestReceivedDate { get; set; } = null;

        [JsonPropertyName("planPaidAmount")]
        public double PlanPaidAmount { get; set; } = 0;

        [JsonPropertyName("policyType")]
        [StringLength(40)]
        public string PolicyType { get; set; } = "";

        [JsonPropertyName("providerFinalOfferAmount")]
        public double ProviderFinalOfferAmount { get; set; } = 0;

        [JsonPropertyName("providerName")]
        [StringLength(60)]
        public string ProviderName { get; set; } = "";

        [JsonPropertyName("providerNPI")]
        [StringLength(10)]
        public string ProviderNPI { get; set; } = "";

        [JsonPropertyName("providerPaidDate")]
        public DateTime? ProviderPaidDate { get; set; } = null;

        [JsonPropertyName("providerType")]
        [StringLength(50)]
        public string ProviderType { get; set; } = "";

        [JsonPropertyName("reasonableAmount")]
        public double ReasonableAmount { get; set; } = 0;

        [JsonPropertyName("requestDate")]
        public DateTime? RequestDate { get; set; } = null;

        [JsonPropertyName("requestId")]
        [StringLength(20)]
        public string RequestId { get; set; } = "";

        [JsonPropertyName("requestType")]
        [StringLength(20)]
        public string RequestType { get; set; } = "";

        [JsonPropertyName("resolutionDeadlineDate")]
        public DateTime? ResolutionDeadlineDate { get; set; } = null;

        [JsonPropertyName("serviceDate")]
        public DateTime? ServiceDate { get; set; } = null;

        [JsonPropertyName("status")]
        [StringLength(50)]
        public string Status { get; set; } = "";

        [JsonPropertyName("submittedBy")]
        [StringLength(60)]
        public string SubmittedBy { get; set; } = "";

        [JsonPropertyName("totalSettlementAmount")]
        public double TotalSettlementAmount { get; set; } = 0;

        [JsonPropertyName("userId")]
        [StringLength(40)]
        public string UserId { get; set; } = "";

        [JsonPropertyName("wasIneligibilityDenied")]
        public bool WasIneligibilityDenied { get; set; } = false;

        [JsonPropertyName("wasDisputeSettledOutsideOfArbitration")]
        public bool WasDisputeSettledOutsideOfArbitration { get; set; } = false;

        [JsonPropertyName("wasDisputeSettledWithTeleconference")]
        public bool WasDisputeSettledWithTeleconference { get; set; } = false;

        [JsonPropertyName("wasSettledAtArbitration")]
        public bool WasSettledAtArbitration { get; set; } = false;

        [JsonPropertyName("wasPayorPaymentReceived")]
        public bool WasPayorPaymentReceived { get; set; } = false;

        [JsonPropertyName("wasPayorPaymentTimely")]
        public bool WasPayorPaymentTimely { get; set; } = false;

        [JsonPropertyName("wasProviderPaymentReceived")]
        public bool WasProviderPaymentReceived { get; set; } = false;

        [JsonPropertyName("wasProviderPaymentTimely")]
        public bool WasProviderPaymentTimely { get; set; } = false;

        [JsonPropertyName("winner")]
        [StringLength(50)]
        public string Winner { get; set; } = "";
    }
}
