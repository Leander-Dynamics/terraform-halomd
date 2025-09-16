using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MPExternalDisputeAPI.Model
{
    public class RPT_EmailedFeePaymentRequests
    {
        /// <summary>
        /// Dispute Number
        /// </summary>
        public string? DisputeNumber { get; set; }

        /// <summary>
        /// Email date
        /// </summary>
        public DateTime? Emaildate { get; set; }

        /// <summary>
        /// From Email
        /// </summary>
        public string? FromEmail { get; set; }

        /// <summary>
        /// Email Link
        /// </summary>
        public string? EmailLink { get; set; }
    }
}
