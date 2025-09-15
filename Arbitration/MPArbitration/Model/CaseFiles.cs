using System.Text.Json.Serialization;

namespace MPArbitration.Model
{
    /// <summary>
    /// ViewModel class for transmitting blob store information back to the client
    /// </summary>
    public class CaseFile
    {
        [JsonPropertyName("blobName")]
        public string BLOBName { get; set; } = "";
        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;
        [JsonPropertyName("tags")]
        public IDictionary<string, string>? Tags { get; set; } = null;
    }
}
