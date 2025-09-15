using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MPArbitration.Model
{
    // This class is not a dataset in Entity Framework. 
    // It represents a JSON data structure only.
    public class NotificationDocument : INotificationDocument
    {
        [JsonPropertyName("arbitrationCaseId")]
        public int ArbitrationCaseId { get; set; } = 0;

        [JsonPropertyName("html")]
        public string HTML { get; set; } = "";

        [JsonPropertyName("JSON")]
        public string JSON { get; set; } = "{}";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("notificationType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public NotificationType NotificationType { get; set; }
    }
}
