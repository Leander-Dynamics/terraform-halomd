using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    [Index(nameof(AuthorityId), nameof(PayorId), IsUnique = true)]
    public class PayorAuthorityMap
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("authorityId")]
        public int AuthorityId { get; set; }

        [JsonPropertyName("payorId")]
        public int PayorId { get; set; }

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;
    }
}