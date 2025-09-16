using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class CaseLog
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;
        [StringLength(20)]
        [JsonPropertyName("action")]
        public string Action { get; set; } = "";
        [JsonPropertyName("details")]
        public string Details { get; set; } = "";
        [JsonPropertyName("createdBy")]
        [StringLength(50)]
        public string CreatedBy { get; set; } = "";
        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("arbitrationCaseId")]
        public int ArbitrationCaseId { get; set; } = 0;
    }
}
