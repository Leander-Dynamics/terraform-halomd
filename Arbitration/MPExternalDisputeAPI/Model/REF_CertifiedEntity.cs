using System.ComponentModel.DataAnnotations;

namespace MPExternalDisputeAPI.Model
{
    public class REF_CertifiedEntity
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Certified Entity Name
        /// </summary>
        public string? CertifiedEntityName { get; set; }
    }
}
