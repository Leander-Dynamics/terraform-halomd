using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity for WorkflowBase
    /// </summary>
    public abstract class WorkflowBase
    {
        /// <summary>
        /// Used to hold ArbitId
        /// </summary>
        [Required]
        [JsonPropertyName("arbitId")]
        public int ArbitId { get; private set; }

        /// <summary>
        /// Used to hold ArbitrationCase
        /// </summary>
        [JsonPropertyName("arbitrationCase")]
        public ArbitrationCase? ArbitrationCase { get; private set; }

        /// <summary>
        /// Used to hold Payor
        /// </summary>
        [JsonPropertyName("payor")]
        public Payor? Payor { get; private set; }

        /// <summary>
        /// Used to hold Customer
        /// </summary>
        [JsonPropertyName("customer")]
        public Customer? Customer { get; private set; }

        /// <summary>
        /// Used to hold StateCode
        /// </summary>
        [JsonPropertyName("state")]
        public string StateCode { get; private set; }

        /// <summary>
        /// Used to hold DateNegotiationSent
        /// </summary>
        [JsonPropertyName("dateNegotiationSent")]
        public DateTime? DateNegotiationSent { get; private set; }

        /// <summary>
        /// Used to hold FirstResponseDate
        /// </summary>
        [JsonPropertyName("firstResponseDate")]
        public DateTime? FirstResponseDate { get; private set; }

        /// <summary>
        /// Used to hold the certified entity name
        /// </summary>
        [JsonPropertyName("certifiedEntityName")]
        public string? CertifiedEntityName { get; private set; }

        /// <summary>
        /// Used to hold the certified entity id
        /// </summary>
        [JsonPropertyName("certifiedEntityId")]
        public int? CertifiedEntityId { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="arbitId"></param>
        /// <param name="payor"></param>
        /// <param name="customer"></param>
        /// <param name="stateCode"></param>
        /// <param name="dateNegotiationSent"></param>
        /// <param name="firstResponseDate"></param>
        /// <param name="certifiedEntityName"></param>
        /// <param name="certifiedEntityId"></param>
        public WorkflowBase(int arbitId, Payor payor, Customer customer, string stateCode, DateTime? dateNegotiationSent, DateTime? firstResponseDate, string certifiedEntityName, int certifiedEntityId)
        {
            ArbitId = arbitId;
            if (payor != null) 
                Payor = payor;
            if (customer != null)
                Customer = customer;
            StateCode = stateCode;
            if (dateNegotiationSent.HasValue)
                DateNegotiationSent = Convert.ToDateTime(dateNegotiationSent.Value.ToString("MM-dd-yyyy"));

            if (firstResponseDate.HasValue)
                FirstResponseDate = Convert.ToDateTime(firstResponseDate.Value.ToString("MM-dd-yyyy"));

            CertifiedEntityName = certifiedEntityName;
            CertifiedEntityId = certifiedEntityId;
        }
    }

    /// <summary>
    /// Entity for WorkflowState
    /// </summary>
    public class WorkflowState : WorkflowBase
    {
        /// <summary>
        /// Used to hold AuthorityDisputeID
        /// </summary>
        [JsonPropertyName("authorityID")]
        public string AuthorityDisputeID { get; private set; }

        /// <summary>
        /// Used to hold IsDeleted
        /// </summary>
        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; private set; }

        /// <summary>
        /// Used to hold CaseSettlementId
        /// </summary>
        [JsonPropertyName("CaseSettlementId")]
        public int? CaseSettlementId{ get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="arbitId"></param>
        /// <param name="payor"></param>
        /// <param name="customer"></param>
        /// <param name="stateCode"></param>
        /// <param name="dateNegotiationSent"></param>
        /// <param name="firstResponseDate"></param>
        /// <param name="authorityDisputeID"></param>
        /// <param name="isDeleted"></param>
        /// <param name="caseSettlementId"></param>
        /// <param name="certifiedEntityName"></param>
        /// <param name="certifiedEntityId"></param>
        public WorkflowState(int arbitId, Payor? payor, Customer? customer, string stateCode, DateTime? dateNegotiationSent, DateTime? firstResponseDate,
                string authorityDisputeID, bool isDeleted, int? caseSettlementId, string certifiedEntityName, int certifiedEntityId) : base(arbitId, payor, customer, stateCode, dateNegotiationSent, firstResponseDate, certifiedEntityName, certifiedEntityId)
        {
            AuthorityDisputeID = authorityDisputeID;
            IsDeleted = isDeleted;
            CaseSettlementId = caseSettlementId;
            
        }
    }

    /// <summary>
    /// Entity for WorkflowNSA
    /// </summary>
    public class WorkflowNSA : WorkflowBase
    {
        /// <summary>
        /// Used to hold DisputeNumber
        /// </summary>
        [JsonPropertyName("disputeNumber")]
        public string DisputeNumber { get; private set; }

        /// <summary>
        /// Used to hold NegotiationSentDate
        /// </summary>
        [JsonPropertyName("negotiationSentDate")]
        public DateTime? NegotiationSentDate { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="arbitId"></param>
        /// <param name="payor"></param>
        /// <param name="customer"></param>
        /// <param name="stateCode"></param>
        /// <param name="dateNegotiationSent"></param>
        /// <param name="firstResponseDate"></param>
        /// <param name="disputeNumber"></param>
        /// <param name="certifiedEntityName"></param>
        /// <param name="certifiedEntityId"></param>
        public WorkflowNSA(int arbitId, Payor? payor, Customer? customer, string stateCode, DateTime? dateNegotiationSent, DateTime? firstResponseDate,
                           string disputeNumber, string? certifiedEntityName, int certifiedEntityId) : base(arbitId, payor, customer, stateCode, dateNegotiationSent, firstResponseDate, certifiedEntityName, certifiedEntityId)
        {
            DisputeNumber = disputeNumber;
            NegotiationSentDate = dateNegotiationSent;
        }
    }
    /// <summary>
    /// Entity for WorkflowPayorClaim
    /// </summary>
    public class WorkflowPayorClaim : WorkflowBase
    {
        /// <summary>
        /// Used to hold PayorClaimNumber
        /// </summary>
        [JsonPropertyName("payorClaimNumber")]
        public string PayorClaimNumber { get; private set; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="arbitId"></param>
        /// <param name="payor"></param>
        /// <param name="customer"></param>
        /// <param name="stateCode"></param>
        /// <param name="dateNegotiationSent"></param>
        /// <param name="firstResponseDate"></param>
        /// <param name="payorClaimNumber"></param>
        /// <param name="certifiedEntityName"></param>
        /// <param name="certifiedEntityId"></param>
        public WorkflowPayorClaim(int arbitId, Payor? payor, Customer? customer, string stateCode, DateTime? dateNegotiationSent, DateTime? firstResponseDate,
                           string payorClaimNumber, string? certifiedEntityName, int certifiedEntityId) : base(arbitId, payor, customer, stateCode, dateNegotiationSent, firstResponseDate, certifiedEntityName, certifiedEntityId)
        {
            PayorClaimNumber = payorClaimNumber;
        }
    }
}
