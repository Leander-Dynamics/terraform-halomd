using System.Text.Json.Serialization;

namespace MPArbitration.Model
{
    public class EntityVM
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        public string NPINumber { get; set; } = "";
    }
}
