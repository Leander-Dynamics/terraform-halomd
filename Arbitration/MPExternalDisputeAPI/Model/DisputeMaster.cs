using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;

namespace MPExternalDisputeAPI.Model
{
    public class DisputeMaster
    {
        [Key]
        public int Id { get; set; }

        public DateTime? UpdatedOn { get; set; }
        
        public string? DisputeNumber { get; set; }

        public string? Customer { get; set; }

        public string? Entity { get; set; }

        public string? EntityNPI { get; set; }

        public string? Payor { get; set; }

        public DateTime? SubmissionDate { get; set; }

        public string? CertifiedEntity { get; set; }

        public DateTime? IDRESelectionDate { get; set; }

        public DateTime? FeeRequestDate { get; set; }

        public DateTime? FeeDueDate { get; set; }

        public decimal? FeeAmountAdmin { get; set; }

        public decimal? FeeAmountEntity { get; set; }

        public decimal? FeeAmountTotal { get; set; }

        public string? FeeInvoiceLink { get; set; }

        public DateTime? FeePaidDate { get; set; }

        public DateTime? AwardDate { get; set; }

        public decimal? FeePaidAmount { get; set; }

        /// <summary>
        /// Used to get initiation date input
        /// </summary>
        public string? DisputeStatus { get; set; }

    }
}
