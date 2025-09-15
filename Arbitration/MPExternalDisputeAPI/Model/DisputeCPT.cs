using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MPExternalDisputeAPI.Model
{
    public class DisputeCPT
    {
        /// <summary>
        /// ID
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// ArbitID
        /// </summary>
        public int? ArbitID { get; set; }
        /// <summary>
        /// Dispute Number
        /// </summary>
        public string? DisputeNumber { get; set; }
        /// <summary>
        /// CPT Code
        /// </summary>
        public string? CPTCode { get; set; }
        /// <summary>
        /// Benchmark Amount
        /// </summary>
        public decimal? BenchmarkAmount { get; set; }
        /// <summary>
        /// Provider Offer Amount
        /// </summary>
        public decimal? ProviderOfferAmount { get; set; }
        /// <summary>
        /// Payor Offer Amount
        /// </summary>
        public decimal? PayorOfferAmount { get; set; }
        /// <summary>
        /// Prevailing Party
        /// </summary>
        public string? PrevailingParty { get; set; }
        /// <summary>
        /// Award Amount
        /// </summary>
        public decimal? AwardAmount { get; set; }
    }


    public class DisputeDetail
    {
        /// <summary>
        /// Determination
        /// </summary>
        public string? Determination { get; set; }

        /// <summary>
        /// Arbit Id
        /// </summary>
        public int? ArbitId { get; set; }
        /// <summary>
        /// CPT Code
        /// </summary>
        public string? CPTCode { get; set; }
        /// <summary>
        /// Provider Offer Amount
        /// </summary>
        public decimal? ProviderOfferAmount { get; set; }
        /// <summary>
        /// Payor Offer Amount
        /// </summary>
        public decimal? PayorOfferAmount { get; set; }
        /// <summary>
        /// Award Amount
        /// </summary>
        public decimal? AwardAmount { get; set; }
        /// <summary>
        /// Prevailing Party
        /// </summary>
        public string? PrevailingParty { get; set; }

        /// <summary>
        /// Prevailing Amount
        /// </summary>
        public decimal? PrevailingAmount { get; set; }

        /// <summary>
        /// Non Prevailing Party
        /// </summary>
        public string? NonPrevailingParty { get; set; }

        ///// <summary>
        ///// Benchmark Amount
        ///// </summary>
        //public decimal? BenchmarkAmount { get; set; }
        /// <summary>
        /// EHR Number
        /// </summary>
        public string? EHRNumber { get; set; }
        /// <summary>
        /// PayorClaimNumber
        /// </summary>
        public string? PayorClaimNumber { get; set; }

        private string? _providerType;
        /// <summary>
        /// Provider Type
        /// </summary>
        public string ProviderType
        {
            get
            {
                string strReturn = "Provider";
                _providerType = !string.IsNullOrEmpty(_providerType) ? _providerType.Trim().ToUpper() : "PROVIDER";
                if (!string.IsNullOrEmpty(_providerType))
                {
                    if (_providerType.Contains("NEUROLOGIST") || _providerType.Contains("EMERGENCY") || _providerType.Contains("OTRHOPEDIC"))
                        strReturn = _providerType;
                }
                return strReturn;
            }
            set
            {
                _providerType = value;
            }
        }
        /// <summary>
        /// Provider Name
        /// </summary>
        public string? ProviderName { get; set; }

        /// <summary>
        /// ClaimCPT List
        /// </summary>
        /// <summary>
        /// Patient Name
        /// </summary>
        public string? PatientName { get; set; }

        /// <summary>
        /// Service Date
        /// </summary>
        public DateTime? ServiceDate { get; set; }
        /// <summary>
        /// Operating States
        /// </summary>
        public string? OperatingState { get; set; }
    }
}
