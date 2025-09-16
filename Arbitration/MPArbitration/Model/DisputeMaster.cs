using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity to handle dispute master data's
    /// </summary>
    public class DisputeMaster
    {
        /// <summary>
        /// Used to hold DisbuteId
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Used to hold arbitID
        /// </summary>
        [NotMapped]
        public int ArbitId { get; set; }

        /// <summary>
        /// Used to hold dispute numer
        /// </summary>
        public string? DisputeNumber { get; set; }

        /// <summary>
        /// Used to hold initiation date input
        /// </summary>
        public string? DisputeStatus { get; set; }

        /// <summary>
        /// Used to hold dispute workflow
        /// </summary>
        public string? DisputeWorkFlowStatus { get; set; }
        

        /// <summary>
        /// Used to hold customer
        /// </summary>
        public string? Customer { get; set; }

        /// <summary>
        /// Used to hold entity
        /// </summary>
        public string? Entity { get; set; }

        /// <summary>
        /// Used to hold entity APi
        /// </summary>
        public string? EntityNPI { get; set; }

        /// <summary>
        /// Used to hold payor
        /// </summary>
        public string? Payor { get; set; }

        /// <summary>
        /// Used to hold brief due date
        /// </summary>
        public DateTime? BriefDueDate { get; set; }

        /// <summary>
        /// Used to hold BriefSubmissionLink
        /// </summary>
        public string? BriefSubmissionLink { get; set; }

        /// <summary>
        /// Used to hold brief approver
        /// </summary>
        public string? BriefApprover { get; set; }

        /// <summary>
        /// Used to hold brief assignedDate date
        /// </summary>
        [Column(TypeName="Date")]
        public DateTime? BriefAssignedDate { get; set; }

        /// <summary>
        /// Used to hold submission date
        /// </summary>
        [Column(TypeName="Date")]
        public DateTime? SubmissionDate { get; set; }

        /// <summary>
        /// Used to hold certified entity
        /// </summary>
        public string? CertifiedEntity { get; set; }

        /// <summary>
        /// Used to hold IDR selection date
        /// </summary>
        [Column(TypeName="Date")]
        public DateTime? IDRESelectionDate { get; set; }

        /// <summary>
        /// Used to hold FormalReceivedDate date
        /// </summary>
        [Column(TypeName="Date")]
        public DateTime? FormalReceivedDate { get; set; }
        
        /// <summary>
        /// Used to hold fee request date
        /// </summary>
        [Column(TypeName="Date")]
        public DateTime? FeeRequestDate { get; set; }

        /// <summary>
        /// Used to hold fee due date
        /// </summary>
        [Column(TypeName="Date")]
        public DateTime? FeeDueDate { get; set; }

        /// <summary>
        /// Used to hold fee amount
        /// </summary>
        public decimal? FeeAmountAdmin { get; set; }

        /// <summary>
        /// Used to hold fee amount entity
        /// </summary>
        public decimal? FeeAmountEntity { get; set; }

        /// <summary>
        /// Used to hold fee amount total
        /// </summary>
        public decimal? FeeAmountTotal { get; set; }

        /// <summary>
        /// Used to hold fee invoice link
        /// </summary>
        public string? FeeInvoiceLink { get; set; }

        /// <summary>
        /// Used to hold fee paid date
        /// </summary>
        public DateTime? FeePaidDate { get; set; }

        /// <summary>
        /// Used to hold award date
        /// </summary>
        public DateTime? AwardDate { get; set; }

        /// <summary>
        /// Used to hold fee paid amount
        /// </summary>
        public decimal? FeePaidAmount { get; set; }

        /// <summary>
        /// Used to hold CreatedBy
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Used to hold CreatedOn
        /// </summary>
        public DateTime? CreatedOn { get; set; }

        /// <summary>
        /// Used to hold UpdatedBy
        /// </summary>
        public string? UpdatedBy { get; set; } 

        /// <summary>
        /// Used to hold updated on 
        /// </summary>
        public DateTime? UpdatedOn { get; set; }

        /// <summary>
        /// Used to hold Comments
        /// </summary>
        public string? Comments { get; set; }

        /// <summary>
        /// Used to hold ServiceLine
        /// </summary>
        public string? ServiceLine { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public DisputeMaster()
        {
            this.ArbitId = 0;
            this.DisputeNumber = "";
        }

        /// <summary>
        /// Constructor with parameters
        /// </summary>
        /// <param name="arbitId"> Used to get Arbit Id</param>
        /// <param name="disputeNumber"> Used to get Dispute number</param>
        public DisputeMaster(int arbitId, string disputeNumber)
        {
            this.ArbitId = arbitId;
            this.DisputeNumber = disputeNumber;
        }


    }
}
