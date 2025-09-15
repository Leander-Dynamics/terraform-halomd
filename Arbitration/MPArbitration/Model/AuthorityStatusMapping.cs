using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MPArbitration.Model
{
    [NotMapped]
    public class AuthorityStatusMapping
    {
        [JsonPropertyName("arbitrationResult")]
        public ArbitrationResult ArbitrationResult { get; set; } = ArbitrationResult.None;

        [JsonPropertyName("authorityStatus")]
        public string AuthorityStatus { get; set; } = "";

        [JsonPropertyName("workflowStatus")]
        public ArbitrationStatus? WorkflowStatus { get; set; }
    }

    /// <summary>
    /// Convenience class for using JsonSerializer.Deserialize.
    /// Does NOT persist any values back into the parent's JSON field.
    /// </summary>
    [NotMapped]
    public class AuthorityJson
    {
        [JsonPropertyName("customerMappings")]
        public List<AuthorityUserVM>? CustomerMappings { get; set; }  

        [JsonPropertyName("statusMappings")]
        public List<AuthorityStatusMapping>? StatusMappings { get; set; }
    }
}
