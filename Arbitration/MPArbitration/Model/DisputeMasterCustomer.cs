using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity to handle dispute customer master data's
    /// </summary>
    public class DisputeMasterCustomer
    {

        /// <summary>
        /// Used to hold customer
        /// </summary>
        public string? Customer { get; set; }
    }
}
