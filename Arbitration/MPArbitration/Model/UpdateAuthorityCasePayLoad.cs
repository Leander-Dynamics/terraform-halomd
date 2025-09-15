using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity used to set the payload information for AuthorityCase update from workflow
    /// </summary>
    public class UpdateAuthorityCasePayLoad
    {
        /// <summary>
        /// Used to set AuthorityCaseId from payload
        /// </summary>
        [Required]
        public string? AuthorityCaseId { get; set; }


        /// <summary>
        /// Used to set InformalTeleconferenceDate from payload
        /// </summary>
        public DateTime? InformalTeleconferenceDate { get; set; }

        /// <summary>
        /// Used to set ArbitrationBriefDueDate from payload
        /// </summary>
        public DateTime? ArbitrationBriefDueDate { get; set; }

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
