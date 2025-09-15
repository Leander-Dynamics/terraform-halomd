using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class CaseBenchmark
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;
        
        [JsonPropertyName("extendedValue")]
        public double ExtendedValue { get; set; } = 0;

        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; } = false;

        [JsonPropertyName("procedureCode")]
        [StringLength(20)]
        public string ProcedureCode { get; set; } = "";

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; } = 0;

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("value")]
        public double Value { get; set; } = 0;

        [JsonPropertyName("valueField")]
        [StringLength(60)]
        public string ValueField { get; set; } = "";

    }
}
