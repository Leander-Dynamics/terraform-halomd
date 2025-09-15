using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class CaseTracking
    {
        [ForeignKey("ArbitrationCase")]
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("trackingValues")]
        public string TrackingValues { get; set; } = "";  // custom JSON document containing all Authority-specific tracking data, e.g. dates

        [StringLength(60)]
        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;
    }
}
