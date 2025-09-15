using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Permissions;

namespace MPArbitration.Model
{
    public interface IDisputeFee
    {
        int BaseFeeId { get; set; }
        BaseFee? BaseFee { get; set; }  // aka the fee "definition" containing the rules for calculation, etc
        FeeRecipient FeeRecipient { get; set; }
    }

    public class FeeStorageBase
    {
        [JsonPropertyName("amountDue")]
        public double AmountDue { get; set; }

        [JsonPropertyName("dueOn")]
        public DateTime? DueOn { get; set; } = null;

        [JsonPropertyName("feeRecipient")]
        [Column(TypeName = "nvarchar(20)")]
        public FeeRecipient FeeRecipient { get; set; } = FeeRecipient.Arbitrator;

        [JsonPropertyName("invoiceLink")]
        [StringLength(512)]
        public string InvoiceLink { get; set; } = "";

        [JsonPropertyName("invoiceReceivedOn")]
        public DateTime? InvoiceReceivedOn { get; set; } = null;

        [JsonPropertyName("isRefundable")]
        public bool IsRefundable { get; set; }

        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; }

        [JsonPropertyName("paidBy")]
        [StringLength(60)]
        public string PaidBy { get; set; } = "";

        [JsonPropertyName("paidOn")]
        public DateTime? PaidOn { get; set; } = null;

        [JsonPropertyName("paymentMethod")]
        [StringLength(60)]
        public string PaymentMethod { get; set; } = "";

        [JsonPropertyName("paymentReferenceNumber")]
        [StringLength(255)]
        public string PaymentReferenceNumber { get; set; } = "";

        [JsonPropertyName("paymentRequestedOn")]
        public DateTime? PaymentRequestedOn { get; set; } = null;

        [JsonPropertyName("refundableAmount")]
        public double RefundableAmount { get; set; }

        [JsonPropertyName("refundAmount")]
        public double RefundAmount { get; set; }

        [JsonPropertyName("refundDueOn")]
        public DateTime? RefundDueOn { get; set; } = null;

        [JsonPropertyName("refundedOn")]
        public DateTime? RefundedOn { get; set; } = null;

        [JsonPropertyName("refundedTo")]
        [StringLength(60)]
        public string RefundedTo { get; set; } = "";

        [JsonPropertyName("refundMethod")]
        [StringLength(60)]
        public string RefundMethod { get; set; } = "";

        [JsonPropertyName("refundReferenceNumber")]
        [StringLength(255)]
        public string RefundReferenceNumber { get; set; } = "";

        [JsonPropertyName("refundRequestedBy")]
        [StringLength(60)]
        public string RefundRequestedBy { get; set; } = "";

        [JsonPropertyName("refundRequestedOn")]
        public DateTime? RefundRequestedOn { get; set; } = null;

        [JsonPropertyName("wasRefunded")]
        public bool WasRefunded { get; set; }

        [JsonPropertyName("wasRefundRequested")]
        public bool WasRefundRequested { get; set; }

    }

    /// <summary>
    /// Class used for importing CSV records using alternate foreign key values
    /// </summary>
    public class AuthorityDisputeFeeCSV : FeeStorageBase
    {
        /// <summary>
        /// If relevant, the text name of the Arbitration Entity that owns this Fee depending on the value of FeeRecipient.
        /// Without this property, fees imports would require a lot of duplicate logic.
        /// </summary>
        [JsonPropertyName("arbitratorEntityName")]
        public string ArbitratorEntityName { get; set; } = "";

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

        /// <summary>
        /// Alternate primary key for locating the base fee
        /// </summary>
        [Required]
        [JsonPropertyName("feeName")]
        [StringLength(60)]
        public string FeeName { get; set; } = "";
    }

    /// <summary>
    /// Storage for Arbitrator and Authority fee instances
    /// </summary>
    [Index(nameof(AuthorityDisputeId),nameof(FeeRecipient),nameof(BaseFeeId),IsUnique = true)]
    public class AuthorityDisputeFee : FeeStorageBase, IDisputeFee
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("baseFeeId")]
        public int BaseFeeId { get; set; } = 0;

        [NotMapped]
        [JsonPropertyName("baseFee")]
        public BaseFee? BaseFee { get; set; } = null;

        [JsonPropertyName("authorityDisputeId")]
        [Required]
        public int AuthorityDisputeId { get; set; } = 0;

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;
    }
}
