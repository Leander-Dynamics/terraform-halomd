using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity to handle DisputeDetail
    /// </summary>
    public class DisputeDetail : DisputeMaster
    {
        /// <summary>
        /// To hold initiation date
        /// </summary>
        public List<DisputeCPT>? DisputeCPTs { get; set; }

    }
}
