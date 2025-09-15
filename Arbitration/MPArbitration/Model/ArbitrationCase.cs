using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;
using NuGet.ProjectModel;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace MPArbitration.Model
{
    [Index(nameof(IsDeleted), Name = "IX_ArbitrationCases_IsDeleted")]
    [Index(nameof(Customer), Name = "IX_ArbitrationCases_Customer")]
    [Index(nameof(Authority), Name = "IX_ArbitrationCases_Authority")]
    [Index(nameof(NSAStatus), Name = "IX_ArbitrationCases_NSAStatus")]
    [Index(nameof(NSAWorkflowStatus), Name = "IX_ArbitrationCases_NSAWorkflowStatus")]
    [Index(nameof(NegotiationNoticeDeadline), Name = "IX_ArbitrationCases_NegotiationNoticeDeadline")]
    [Index(nameof(Payor), Name = "IX_ArbitrationCases_Payor")]
    [Index(nameof(AuthorityStatus), Name = "IX_ArbitrationCases_AuthorityStatus")]
    [Index(nameof(EOBDate), Name = "IX_ArbitrationCases_EOBDate")]
    [Index(nameof(AuthorityStatus), Name = "IX_ArbitrationCases_AuthorityStatus")]
    [Index(nameof(DOB), Name = "IX_ArbitrationCases_DOB")]
    [Index(nameof(NSACaseId), Name = "IX_ArbitrationCases_NSACaseId")]
    [Index(nameof(PatientName), Name = "IX_ArbitrationCases_PatientName")]
    [Index(nameof(PayorId), Name = "IX_ArbitrationCases_PayorId")]
    [Index(nameof(PayorNegotiatorId), Name = "IX_ArbitrationCases_PayorNegotiatorId")]

    public class ArbitrationCase : IAuthorityCase, IEHRRecord, IEHRKey
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;
        /*
         * VM flags
         */
        [NotMapped]
        [JsonPropertyName("keepAuthorityInfo")]
        public bool KeepAuthorityInfo { get; set; }

        /* 
         * Children and keys 
         */

        [ForeignKey("ArbitrationCaseId")]
        [JsonPropertyName("arbitrators")]
        public virtual List<CaseArbitrator> Arbitrators { get; set; } = new List<CaseArbitrator>();

        [ForeignKey("ArbitrationCaseId")]
        [JsonPropertyName("caseArchives")]
        public virtual List<CaseArchive> CaseArchives { get; set; } = new List<CaseArchive>();

        [JsonPropertyName("tracking")]
        public virtual CaseTracking? Tracking { get; set; } = null;

        [ForeignKey("ArbitrationCaseId")]
        [JsonPropertyName("cptCodes")]
        public virtual List<ClaimCPT> CPTCodes { get; set; } = new List<ClaimCPT>();

        [ForeignKey("ArbitrationCaseId")]
        [JsonPropertyName("benchmarks")]
        public virtual List<CaseBenchmark> Benchmarks { get; set; } = new List<CaseBenchmark>();

        [ForeignKey("ArbitrationCaseId")]
        [JsonPropertyName("caseSettlements")]
        public virtual List<CaseSettlement> CaseSettlements { get; set; } = new List<CaseSettlement>();

        [ForeignKey("ArbitrationCaseId")]
        [JsonPropertyName("attachments")]
        public virtual List<EMRClaimAttachment> Attachments { get; set; } = new List<EMRClaimAttachment>();

        [ForeignKey("ArbitrationCaseId")]
        [JsonPropertyName("log")]
        public virtual List<CaseLog> Log { get; set; } = new List<CaseLog>();

        [ForeignKey("ArbitrationCaseId")]
        [JsonPropertyName("notes")]
        public virtual List<Note> Notes { get; set; } = new List<Note>();

        [ForeignKey("ArbitrationCaseId")]
        [JsonPropertyName("notifications")]
        public virtual List<Notification> Notifications { get; set; } = new List<Notification>();

        [ForeignKey("ArbitrationCaseId")]
        [JsonPropertyName("offerHistory")]
        public virtual List<OfferHistory> OfferHistory { get; set; } = new List<OfferHistory>();

        [ForeignKey("ArbitrationCaseId")]
        [JsonPropertyName("settlementDetails")]
        public virtual List<CaseSettlementDetail> SettlementDetails { get; set; } = new List<CaseSettlementDetail>();

        [JsonPropertyName("additionalPaidAmount")]
        public double AdditionalPaidAmount { get; set; } = 0;

        [JsonPropertyName("arbitrationDeadlineDate")]
        public DateTime? ArbitrationDeadlineDate { get; set; } = null;

        [JsonPropertyName("arbitratorPaymentDeadlineDate")]
        public DateTime? ArbitratorPaymentDeadlineDate { get; set; } = null;

        [JsonPropertyName("arbitrationPaymentDeadlineDate")]
        public DateTime? ArbitrationPaymentDeadlineDate { get; set; } = null;

        [JsonPropertyName("arbitrationBriefDueDate")]
        public DateTime? ArbitrationBriefDueDate { get; set; } = null;

        [JsonPropertyName("arbitrationBriefPreparedDate")]
        public DateTime? ArbitrationBriefPreparedDate { get; set; } = null;

        [JsonPropertyName("arbitrationBriefSubmittedDate")]
        public DateTime? ArbitrationBriefSubmittedDate { get; set; } = null;

        [JsonPropertyName("arbitrationFeeAmount")]
        public double ArbitrationFeeAmount { get; set; } = 0;

        [JsonPropertyName("assignedUser")]
        [StringLength(100)]
        public string AssignedUser { get; set; } = "";

        [JsonPropertyName("assignmentDeadlineDate")]
        public DateTime? AssignmentDeadlineDate { get; set; } = null;

        [JsonPropertyName("authority")]
        [Required]
        [StringLength(8)]
        public string Authority { get; set; } = ""; /** e.g. TX. The width of this is set to 40 in the ThirteenthMigration */

        [JsonPropertyName("authorityCaseId")]
        [StringLength(30)]
        public string AuthorityCaseId { get; set; } = "";

        [JsonPropertyName("authorityProviderFinalOfferAmount")]
        public double AuthorityProviderFinalOfferAmount { get; set; } = 0;

        [JsonPropertyName("authorityStatus")]
        [StringLength(60)]
        public string AuthorityStatus { get; set; } = "";

        [JsonPropertyName("authorityUserId")]
        [StringLength(100)]
        public string AuthorityUserId { get; set; } = "";

        [JsonPropertyName("awardedTo")]
        [StringLength(100)]
        public string AwardedTo { get; set; } = "";

        [JsonPropertyName("benchmarkGeoZip")]
        [StringLength(10)]
        public string BenchmarkGeoZip { get; set; } = ""; /** Override the zip used to calculate a benchmark */

        [JsonPropertyName("calculatedPayorFinalOfferAmount")]
        public double CalculatedPayorFinalOfferAmount { get; set; } = 0; /** not editable in UI **/

        [JsonPropertyName("EHRNumber")]
        [StringLength(40)]
        public string EHRNumber { get; set; } = "";

        [JsonPropertyName("EHRSource")]
        [StringLength(40)]
        public string EHRSource { get; set; } = ""; // aka Customer such as MPowerHealth, etc

        [JsonPropertyName("createdBy")]
        [StringLength(100)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("customer")]
        [StringLength(100)]
        public string Customer { get; set; } = "";  /** MPower field e.g. MPowerHealth, etc */

        [JsonPropertyName("DOB")]
        public DateTime? DOB { get; set; } = null;

        [JsonPropertyName("daysOpen")]
        public int DaysOpen { get; set; } = 0; /** TDI DaysOpen */

        [JsonPropertyName("disputedAmount")]
        public double DisputedAmount { get; set; } = 0; /** TDI DisputedAmount */

        [JsonPropertyName("entity")]
        [StringLength(100)]
        public string Entity { get; set; } = ""; /** MPower field - provider entity name*/

        [JsonPropertyName("entityNPI")]
        [StringLength(40)]
        public string EntityNPI { get; set; } = "";

        [JsonPropertyName("encounterServiceNo")]
        [StringLength(40)]
        public string EncounterServiceNo { get; set; } = "";  // MPower - internal number that encompasses multiple cmdCaseId values

        [JsonPropertyName("EOBDate")]
        public DateTime? EOBDate { get; set; } = null;  // The date on the EOB / when the EOB arrives. Not the first payment date, which could be much later. Sometimes known as FirstResponseDate_Current or _Cur

        [JsonPropertyName("estimatedDisputedAmount")]
        public double EstimatedDisputedAmount { get; set; } = 0; /** TDI EstimatedDisputedAmount */

        [JsonPropertyName("expectedArbFee")]
        public double ExpectedArbFee { get; set; } = 0;

        [JsonPropertyName("fh50thPercentileExtendedCharges")]
        public double FH50thPercentileExtendedCharges { get; set; } = 0;

        [JsonPropertyName("fh80thPercentileExtendedCharges")]
        public double FH80thPercentileExtendedCharges { get; set; } = 0;

        [JsonPropertyName("firstAppealDate")]
        public DateTime? FirstAppealDate { get; set; } = null;

        [JsonPropertyName("firstResponseDate")]
        public DateTime? FirstResponseDate { get; set; } = null; /** MPower field - sometimes known as FirstResponseDate_Post */

        [JsonPropertyName("firstResponsePayment")]
        public double FirstResponsePayment { get; set; } = 0;

        [JsonPropertyName("hasArbitratorWarning")]
        public bool HasArbitratorWarning { get; set; }

        [JsonPropertyName("history")]
        public string History { get; set; } = ""; /** TDI History */

        [JsonPropertyName("ineligibilityAction")]
        [StringLength(50)]
        public string IneligibilityAction { get; set; } = "";

        [JsonPropertyName("ineligibilityReasons")]
        [StringLength(1024)]
        public string IneligibilityReasons { get; set; } = ""; /** TDI IneligibleReasons */

        [JsonPropertyName("informalTeleconferenceDate")]
        public DateTime? InformalTeleconferenceDate { get; set; } = null; /** TDI InformalTeleconferenceDate */

        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; } = false; /** MPower field - allows soft delete and history tracking to continue */

        [JsonPropertyName("isUnread")]
        public bool IsUnread { get; set; } = false; /** MPower field - back-end process updates applied flag */

        [JsonPropertyName("locationGeoZip")]
        [StringLength(10)]
        public string LocationGeoZip { get; set; } = ""; /** MPower field - full zip code of service location */

        [JsonPropertyName("methodOfPayment")]
        [StringLength(255)]
        public string MethodOfPayment { get; set; } = ""; /** TDI MethodOfPayment */

        [NotMapped]
        public Authority? NSAAuthority { get; set; } = null;

        [NotMapped]
        public Authority? StateAuthority { get; set; } = null;  // NOTE: There are no rules that force the StateAuthority to actually be the one referenced in the Authority field since this is a ViewModel property that can be filled in with anything

        [JsonPropertyName("NSACaseId")]
        [StringLength(255)]
        public string NSACaseId { get; set; } = ""; /** NSA Portal submission ID(s) aka IDR Number(s) */

        [JsonPropertyName("NSAIneligibilityAction")]
        [StringLength(50)]
        public string NSAIneligibilityAction { get; set; } = "";

        [JsonPropertyName("NSAIneligibilityReasons")]
        [StringLength(2048)]
        public string NSAIneligibilityReasons { get; set; } = ""; /** TDI IneligibleReasons */

        [JsonPropertyName("NSARequestDiscount")]
        public double NSARequestDiscount { get; set; } = 0;

        //[JsonPropertyName("NSAOpenRequestOffer")]
        //public double NSAOpenRequestOffer { get; set; } = 0;

        [JsonPropertyName("NSAStatus")]
        [StringLength(60)]
        public string NSAStatus { get; set; } = ""; /** NSA Portal submission status - 'Not Submitted' until IDR number is received */

        private string _NSATracking = "";
        [JsonPropertyName("NSATracking")]
        [StringLength(1024)]
        public string NSATracking
        {
            get { return _NSATracking; }
            set
            {
                _NSATracking = value;
                if (!string.IsNullOrWhiteSpace(_NSATracking))
                {
                    try
                    {
                        var NSATrackingData = JObject.Parse(json: _NSATracking);
                        const string propName = "NegotiationNoticeDeadline";
                        if (NSATrackingData.ContainsKey(propName))
                        {
                            var val = NSATrackingData.GetValue<string>(propName);
                            try
                            {
                                if (!string.IsNullOrEmpty(val))
                                {
                                    NegotiationNoticeDeadline = DateOnly.Parse(val.Substring(0, 10)).ToDateTime(TimeOnly.MinValue);
                                }
                            }
                            catch (FormatException ex)
                            {
                                throw new FormatException(ex.Message);
                            }
                        }
                    }
                    catch (FormatException)
                    {
                        throw;
                    }
                    catch
                    {
                        //TODO need to log it 
                    }
                }
                // we should come to this where we have to use EOBDate
                if (string.IsNullOrWhiteSpace(_NSATracking) || NegotiationNoticeDeadline == null)
                {
                    if (this.EOBDate != null)
                    {
                        NegotiationNoticeDeadline = this.EOBDate.Value.Date.AddDays(29);
                    }
                    else
                    {
                        NegotiationNoticeDeadline = null;
                    }

                }
            }
        } /** The last saved version of any NSA date tracking info */

        [JsonPropertyName("NSAWorkflowStatus")]
        [Column(TypeName = "NvarChar(60)")]
        public ArbitrationStatus NSAWorkflowStatus { get; set; } /** Internal NSA process tracking  */

        [JsonPropertyName("originalBilledAmount")]
        public double OriginalBilledAmount { get; set; } = 0; /** TDI OriginalBilledAmount */

        [JsonPropertyName("patientName")]
        [StringLength(50)]
        public string PatientName { get; set; } = ""; /** TDI PatientName */

        [JsonPropertyName("patientShareAmount")]
        public double PatientShareAmount { get; set; } = 0; /** TDI PatientShareAmount */

        [JsonPropertyName("paymentMadeDate")]
        public DateTime? PaymentMadeDate { get; set; } = null; /** TDI PaymentMadeDate */

        [JsonPropertyName("paymentReferenceNumber")]
        [StringLength(255)]
        public string PaymentReferenceNumber { get; set; } = ""; /** TDI PaymentReferenceNumber */

        [JsonPropertyName("payor")]
        [StringLength(60)]
        public string Payor { get; set; } = "";

        [JsonPropertyName("payorId")]
        public int? PayorId { get; set; } = null;

        /*
        [NotMapped]
        Payor? _payorRef = null;
        public Payor? PayorRef { 
            get
            {
                return _payorRef;
            }
            
            set
            {
                if (value == null)
                    this._payorRef = null;
                else if (this.PayorId.HasValue && value.Id != this.PayorId.Value)
                    throw new Exception("PayorRef.Id must match this object's PayorId value");
                else if (!string.IsNullOrEmpty(this.Payor) && value.Name != this.Payor)
                    throw new Exception("PayorRef.Name must match this object's Payor value");
                else
                {
                    this._payorRef = value;
                    this.PayorId = value.Id;
                    this.Payor = value.Name;
                }
            } 
        }
        */

        [JsonPropertyName("payorEntity")]
        [JsonIgnore]
        public virtual Payor? PayorEntity { get; set; } = null;

        [JsonPropertyName("payorClaimNumber")]
        [StringLength(50)]
        public string PayorClaimNumber { get; set; } = "";

        [JsonPropertyName("payorNegotiatorId")]
        public Nullable<int> PayorNegotiatorId { get; set; } = null; /** MPower field - see foreign keys up above */

        [JsonPropertyName("payorNegotiator")]
        [JsonIgnore]
        public virtual Negotiator? PayorNegotiator { get; set; } = null;

        [JsonPropertyName("payorFinalOfferAmount")]
        public double PayorFinalOfferAmount { get; set; } = 0; /** TDI PayorFinalOfferAmount **/

        [JsonPropertyName("payorGroupName")]
        [StringLength(60)]
        public string PayorGroupName { get; set; } = "";

        [JsonPropertyName("payorGroupNo")]
        [StringLength(40)]
        public string PayorGroupNo { get; set; } = "";

        [JsonPropertyName("payorNSAIneligibilityAction")]
        [StringLength(50)]
        public string PayorNSAIneligibilityAction { get; set; } = "";

        [JsonPropertyName("payorNSAIneligibilityReasons")]
        [StringLength(2048)]
        public string PayorNSAIneligibilityReasons { get; set; } = "";

        [JsonPropertyName("payorResolutionRequestReceivedDate")]
        public DateTime? PayorResolutionRequestReceivedDate { get; set; } = null;

        [JsonPropertyName("planPaidAmount")]
        public double PlanPaidAmount { get; set; } = 0; /** TDI PlanPaidAmount */

        [JsonPropertyName("planType")]
        [StringLength(40)]
        public string PlanType { get; set; } = ""; /** MPower PlanType */

        [JsonPropertyName("policyNumber")]
        [StringLength(40)]
        public string PolicyNumber { get; set; } = ""; /** MPower PolicyNumber */

        [JsonPropertyName("policyType")]
        [StringLength(40)]
        public string PolicyType { get; set; } = ""; /** TDI PolicyType */

        [JsonPropertyName("projectedProfitFromFormalArb")]
        public double ProjectedProfitFromFormalArb { get; set; } = 0; /** MPower field - calculated */

        [JsonPropertyName("providerName")]
        [StringLength(60)]
        public string ProviderName { get; set; } = "";  /** Doctor's name */

        [JsonPropertyName("providerNegotiator")]
        [StringLength(50)]
        public string ProviderNegotiator { get; set; } = "";

        [JsonPropertyName("providerNPI")]
        [StringLength(20)]
        public string ProviderNPI { get; set; } = ""; /** Doctor's NPI license number */

        [JsonPropertyName("providerFinalOfferAmount")]
        public double ProviderFinalOfferAmount { get; set; } = 0; /** TDI ProviderFinalOfferAmount */

        [JsonPropertyName("providerFinalOfferAdjustedAmount")]
        public double ProviderFinalOfferAdjustedAmount { get; set; } = 0; /** MPower field - manual override value */

        [JsonPropertyName("providerFinalOfferCalculatedAmount")]
        public double ProviderFinalOfferCalculatedAmount { get; set; } = 0; /** MPower field - calculated offer amount */

        [JsonPropertyName("providerFinalOfferNotToExceed")]
        public double ProviderFinalOfferNotToExceed { get; set; } = 0; /** MPower field - calculated */

        [JsonPropertyName("providerPaidDate")]
        public DateTime? ProviderPaidDate { get; set; } = null; /** TDI ProviderPaidDate */

        [JsonPropertyName("providerType")]
        [StringLength(40)]
        public string ProviderType { get; set; } = ""; /** TDI ProviderType */

        [JsonPropertyName("reasonableAmount")]
        public double ReasonableAmount { get; set; } = 0; /** MPower field - calculated */

        [JsonPropertyName("receivedFromCustomer")]
        public DateTime? ReceivedFromCustomer { get; set; } = null;

        //[JsonPropertyName("renderingProvider")]
        //[StringLength(40)]
        //public string RenderingProvider { get; set; } = "";  /** MPower field - billing entity for the Dr. ? */

        [JsonPropertyName("requestDate")]
        public DateTime? RequestDate { get; set; } = null; /** TDI RequestDate */

        [JsonPropertyName("requestType")]
        [StringLength(30)]
        public string RequestType { get; set; } = ""; /** TDI RequestType */

        [JsonPropertyName("resolutionDeadlineDate")]
        public DateTime? ResolutionDeadlineDate { get; set; } = null; /** TDI ResolutionDeadlineDate */

        [JsonPropertyName("service")]
        [StringLength(20)]
        public string Service { get; set; } = "";  // e.g. IOM Pro

        [JsonPropertyName("serviceDate")]
        public DateTime? ServiceDate { get; set; } = null;/** TDI ServiceDate */

        [JsonPropertyName("serviceLine")]
        [StringLength(20)]
        public string ServiceLine { get; set; } = ""; /** MPower field - short form of Service e.g. IOM */

        [JsonPropertyName("serviceLocationCode")]
        [StringLength(20)]
        public string ServiceLocationCode { get; set; } = ""; /** aka PlaceOfServiceCode - Found in table PlaceOfServiceCodes */

        /*
        [JsonPropertyName("serviceLocationName")]
        [StringLength(50)]
        public string ServiceLocationName { get; set; } = "";
        */

        //[JsonPropertyName("serviceZip")]
        //public string ServiceZip { get; set; } = "";  /** MPower field - full zip code of service location */

        [JsonPropertyName("status")]
        [Column(TypeName = "NvarChar(60)")]
        public ArbitrationStatus Status { get; set; } /** aka AuthorityWorkflowStatus - Local (State) Work flow control */

        [JsonPropertyName("submittedBy")]
        [StringLength(60)]
        public string SubmittedBy { get; set; } = ""; /** TDI Submitted By */

        [JsonPropertyName("totalPaidAmount")]
        public double TotalPaidAmount { get; set; } = 0; /** MPower field - calculated */

        [JsonPropertyName("totalChargedAmount")]
        public double TotalChargedAmount { get; set; } = 0; /** MPower field - calculated */

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("wasIneligibilityDenied")]
        public bool WasIneligibilityDenied { get; set; } = false; /** TDI post-case question*/

        [JsonPropertyName("wasDisputeSettledOutsideOfArbitration")]
        public bool WasDisputeSettledOutsideOfArbitration { get; set; } = false; /** TDI post-case question*/

        [JsonPropertyName("wasDisputeSettledWithTeleconference")]
        public bool WasDisputeSettledWithTeleconference { get; set; } = false; /** TDI post-case question*/


        [JsonPropertyName("NegotiationNoticeDeadline")]
        public DateTime? NegotiationNoticeDeadline { get; set; }

        #region Audit Columns
        [StringLength(255)]
        public string? zEditor { get; private set; } = null;

        public DateTime? zEditedOn { get; private set; } = null;
        #endregion
    }
}
