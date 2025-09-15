using Microsoft.VisualBasic;

namespace MPExternalDisputeAPI.Model
{
    /// <summary>
    /// Model to hold dispute API response data's
    /// </summary>
    public class DisputeData
    {
        /// <summary>
        /// To hold dispute number
        /// </summary>
        public string? Number { get; set; }

        /// <summary>
        /// Dispute Status
        /// </summary>
        public string? Status { get; set; }        /// <summary>
                                                   /// To hold customer name
                                                   /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// To hold Entity name
        /// </summary>
        public string? Entity { get; set; }

        /// <summary>
        /// EntityNPI
        /// </summary>
        public string? EntityNPI { get; set; }


        /// <summary>
        /// To hold Payor name
        /// </summary>
        public string? Payor { get; set; }

        /// <summary>
        /// To hold initiation date
        /// </summary>
        public DateTime? InitiationDate { get; set; }

        /// <summary>
        /// IDRE
        /// </summary>
        public IDRE? IDRE { get; set; }

        /// <summary>
        /// Fee
        /// </summary>
        public Fee? Fee { get; set; }

        /// <summary>
        /// Award
        /// </summary>
        public Award? Award { get; set; }

        /// <summary>
        /// To hold initiation date
        /// </summary>
        public List<DisputeDetail>? DisputeDetailList { get; set; }

        public int GetDisputeDetailsCount()
        {
            int count = 0;
            if (this.DisputeDetailList != null)
            {
                count = this.DisputeDetailList.Count;
            }
            return count;
        }

        /// <summary>
        /// Brief Due Date
        /// </summary>
        public DateTime? BriefDueDate { get; set; }

        //public string? OriginalEmailBody { get; set; }

        public Refund? Refund { get; set; }
    }

    public class IDRE
    {
        /// <summary>
        /// IDRE Selection Date
        /// </summary>
        public DateTime? IDRESelectionDate { get; set; }

        /// <summary>
        /// To hold certified entity name
        /// </summary>
        public string? CertifiedEntity { get; set; }

        /// <summary>
        /// Certified Entity Id
        /// </summary>
        public int? CertifiedEntityId { get; set; }
    }

    public class Fee
    {
        /// <summary>
        /// To hold fee amount admin valaue
        /// </summary>
        public DateTime? FeeRequestDate { get; set; }

        /// <summary>
        /// To hold fee amount admin valaue
        /// </summary>
        public DateTime? FeeDueDate { get; set; }

        /// <summary>
        /// Fee Paid Date
        /// </summary>
        public DateTime? FeePaidDate { get; set; }

        /// <summary>
        /// To hold fee amount link
        /// </summary>
        public string? FeeInvoiceLink { get; set; }

        /// <summary>
        /// To hold original email body
        /// </summary>
        public string? FeeEmailBody { get; set; }

        /// <summary>
        /// To hold fee amount admin valaue
        /// </summary>
        public decimal? AdminFeeAmount { get; set; }

        /// <summary>
        /// To hold fee amount entity valaue
        /// </summary>
        public decimal? EntityFeeAmount { get; set; }

        /// <summary>
        /// To hold fee amount total valaue
        /// </summary>
        public decimal? TotalFeeAmount { get; set; }
    }

    /// <summary>
    /// Award class
    /// </summary>
    public class Award
    {
        /// <summary>
        /// Award Date
        /// </summary>
        public DateTime? AwardDate { get; set; }

        /// <summary>
        /// Prevailing Party
        /// </summary>
        public string? PrevailingParty { get; set; }

        /// <summary>
        /// Total Award Amount
        /// </summary>
        public decimal? TotalAwardAmount { get; set; }
    }

    public class Refund
    {
        /// <summary>
        /// Reward Amount
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary>
        /// Reward Date
        /// </summary>
        public DateTime? Date { get; set; }
    }

}



/*
 *         /// <summary>
        /// To hold certified entity id
        /// </summary>
        public int? CertifiedEntityId { get; set; }

        /// <summary>
        /// Refund Amount
        /// </summary>
        public decimal? RefundAmount { get; set; }

        /// <summary>
        /// Refund Date
        /// </summary>
        public DateTime? RefundDate { get; set; }

        /// <summary>
        /// ProviderNPI
        /// </summary>
        public string? ProviderNpi { get; set; }

 
        /// <summary>
        /// CPT Count
        /// </summary>
        public int CPTCount { get; set; }

        public DateTime? AwardDate { get; set; }




 */