using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /*
    [Index(nameof(Alias), IsUnique = true)]
    public class PayorAlias
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("alias")]
        [StringLength(255)]
        public string Alias { get; set; } = "";

        [JsonPropertyName("payorId")]
        public int PayorId { get; set; }

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [ForeignKey("PayorAliasId")]
        [JsonPropertyName("payorAuthorityMaps")]
        public virtual List<PayorAuthorityMap> PayorAuthorityMaps { get; set; } = new List<PayorAuthorityMap>();
    }
    */
}
