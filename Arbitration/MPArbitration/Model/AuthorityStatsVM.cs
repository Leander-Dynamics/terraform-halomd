using System.Text.Json.Serialization;
//using System.ComponentModel.DataAnnotations.Schema;
//using Microsoft.EntityFrameworkCore;
//using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MPArbitration.Model
{
    public class AuthorityStatsVM
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = "";

        [JsonPropertyName("active")]
        public int Active { get; set; } = 0;

        [JsonPropertyName("ineligible")]
        public int Ineligible { get; set; } = 0;

        [JsonPropertyName("paid")]
        public int Paid { get; set; } = 0;

        [JsonPropertyName("settledFormal")]
        public int SettledFormal { get; set; } = 0;

        [JsonPropertyName("settledInformal")]
        public int SettledInformal { get; set; } = 0;

        [JsonPropertyName("unpaid")]
        public int Unpaid { get; set; } = 0;
    }
}
