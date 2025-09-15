using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace MPArbitration.Model
{
    [Index(nameof(AuthorityId), nameof(PayorId), nameof(GroupNumber), IsUnique = true)]
    public class AuthorityPayorGroupExclusion : GroupExclusionBase
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("authorityId")]
        [Required]
        public int AuthorityId { get; set; } = 0;

        [JsonPropertyName("payorId")]
        [Required]
        public int PayorId { get; set; } = 0;

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;
    }
}
