using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity to handle certified entity master data
    /// </summary>
    public class DisputeMasterCertifiedEntity
    {
        /// <summary>
        /// Used to hold certified entity
        /// </summary>
        public string? CertifiedEntity { get; set; }
    }
}
