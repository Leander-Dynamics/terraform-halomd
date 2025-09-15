using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public class ProviderVM
    {
        [JsonPropertyName("entityNPI")]
        public string EntityNPI { get; set; } = "";

        [JsonPropertyName("providerName")]
        public string ProviderName { get; set; } = "";

        [JsonPropertyName("providerNPI")]
        public string ProviderNPI { get; set; } = "";
    }
}
