using System.Text.Json.Serialization;

namespace MPArbitration.Model
{
    public class AuthorityUserVM
    {
        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = "";
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = "";
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = "";
    }
}
