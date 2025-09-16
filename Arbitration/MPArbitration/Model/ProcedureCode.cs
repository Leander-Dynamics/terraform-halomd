using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    [Index(nameof(CodeType), nameof(Group), IsUnique = false)]
    [Index(propertyNames: nameof(Code), IsUnique = true)]
    public class ProcedureCode
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("code")]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("codeType")]
        [StringLength(20)]
        public string CodeType { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("effectiveDate")]
        public DateTime? EffectiveDate { get; set; } = null;

        [JsonPropertyName("group")]
        [StringLength(100)]
        public string Group { get; set; } = string.Empty;

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

    }
}
