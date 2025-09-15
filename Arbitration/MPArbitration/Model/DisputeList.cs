using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// Model to hold dispute API response data's
    /// </summary>
    public class DisputeList
    {

        /// <summary>
        /// Used to hold DisbuteId
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// To hold dispute number
        /// </summary>
        public string? DisputeNumber { get; set; }


        /// <summary>
        /// To hold customer name
        /// </summary>
        public string? Customer { get; set; }

        /// <summary>
        /// Used to get initiation date input
        /// </summary>
        public string? DisputeStatus { get; set; }

        /// <summary>
        /// To hold Entity name
        /// </summary>
        public string? Entity { get; set; }

        /// <summary>
        /// Used to get certified entity input
        /// </summary>
        public string? CertifiedEntity { get; set; }

        /// <summary>
        /// Used to hold cpt's count
        /// </summary>
        public int? NumberOfCPTs { get; set; }

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
        /// Used to hold brief approver
        /// </summary>
        public string? BriefApprover { get; set; }
    }
}
