using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// A configured fee for an Authority. All configured fees are copied into each new dispute.
    /// </summary>
    [Index(nameof(AuthorityId), IsUnique = false)]
    public class AuthorityFee : BaseFee
    {
        [JsonPropertyName("authorityId")]
        [Required]
        public int AuthorityId { get; set; } = 0;

    }
}
