using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity to handle dispute CPT level activities
    /// </summary>
    public class DisputeCPT
    {
        /// <summary>
        /// Used to hold Id
        /// </summary>
        [Key]
        public int Id { get; private set; }

        /// <summary>
        /// Used to hold Arbit Id
        /// </summary>
        public int ArbitId { get; private set; }

        /// <summary>
        /// Used to hold Dispute number
        /// </summary>
        public string DisputeNumber { get; private set; }

        /// <summary>
        /// Used to hold CPT code
        /// </summary>
        public string? CPTCode { get; set; }

        /// <summary>
        /// Used to hold benchmark amount
        /// </summary>
        public decimal? BenchmarkAmount { get; set; }

        /// <summary>
        /// Used to hold provider offer amount
        /// </summary>
        public decimal? ProviderOfferAmount { get; set; }

        /// <summary>
        /// Used to hold payor offer amount
        /// </summary>
        public decimal? PayorOfferAmount { get; set; }

        /// <summary>
        /// Used to hold prevailing party
        /// </summary>
        public string? PrevailingParty { get; set; }

        /// <summary>
        /// Used to hold award amount
        /// </summary>
        public decimal? AwardAmount { get; set; }

        /// <summary>
        /// <summary>
        /// Used to set payar claim number from arbit cases table
        /// </summary>
        [NotMapped]
        public string?  PayorClaimNumber { get; set; }

        /// <summary>
        /// Constructor with params
        /// </summary>
        /// <param name="id">CPT Id</param>
        /// <param name="arbitId"> Arbit Id</param>
        /// <param name="disputeNumber"> Dispute number</param>
        public DisputeCPT(int id, int arbitId, string disputeNumber)
        {
            this.Id = id;
            this.ArbitId = arbitId;
            this.DisputeNumber = disputeNumber;
        }
    }
}
