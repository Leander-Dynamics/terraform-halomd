using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MPArbitration.Model
{
    public class DocumentTemplate
    {
        [JsonPropertyName("html")]
        [StringLength(16535)]
        public string HTML { get; set; } = String.Empty;

        [JsonPropertyName("name")]
        [StringLength(100)]
        public string Name { get; set; } = String.Empty;

        [JsonPropertyName("notificationType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public NotificationType NotificationType { get; set; }

        [JsonPropertyName("tags")]
        [StringLength(255)]
        public string Tags { get; set; } = String.Empty;
    }

    public class DocumentTemplateCollection
    {
        [JsonPropertyName("templates")]
        public List<DocumentTemplate>? Templates { get; set; }
    }
}
