using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity to handle dispute status master data's
    /// </summary>
    public class DisputeMasterDisputeStatus
    {
        /// <summary>
        /// Used to hold dispute status
        /// </summary>
        public string? DisputeStatus { get; set; }
    }
}
