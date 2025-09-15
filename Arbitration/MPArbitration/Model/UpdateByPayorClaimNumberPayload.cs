using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity to get payload information for UpdateByPayorClaimNumber
    /// </summary>
    public class UpdateByPayorClaimNumberPayload
    {
        /// <summary>
        /// Used to set PayorClaimNumber from payload
        /// </summary>
        [Required]
        public string? PayorClaimNumber { get; set; }

        /// <summary>
        /// Used to set DisputeStatus from payload
        /// </summary>
        public string? DisputeStatus { get; set; }

        /// <summary>
        /// Used to set AuthorityStatus from payload
        /// </summary>
        public string? AuthorityStatus { get; set; }

        /// <summary>
        /// Used to set NSAWorkflowStatus from payload
        /// </summary>
        public string? NSAWorkflowStatus { get; set; }
    }
}
