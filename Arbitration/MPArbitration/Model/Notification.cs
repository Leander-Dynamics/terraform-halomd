using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    [Index(nameof(IsDeleted),nameof(SentOn),nameof(Status))]
    public class Notification 
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [NotMapped]
        public ArbitrationCase? ArbitrationCase { get; set; } = null;

        [NotMapped]
        public AuthorityDispute? AuthorityDispute { get; set;} = null;

        [JsonPropertyName("arbitrationCaseId")]
        public int ArbitrationCaseId { get; set; } = 0;

        [JsonPropertyName("approvedBy")]
        [StringLength(60)]
        public string ApprovedBy { get; set; } = string.Empty;

        [JsonPropertyName("approvedOn")]
        public DateTime? ApprovedOn { get; set; } = null;

        [JsonPropertyName("authorityKey")]
        [StringLength(8)]
        public string AuthorityKey { get; set; }= string.Empty;

        [JsonPropertyName("bcc")]
        [StringLength(512)]
        public string BCC { get; set; } = String.Empty;

        [JsonPropertyName("cc")]
        [StringLength(512)]
        public string CC { get; set; } = String.Empty;

        [JsonPropertyName("customer")]
        [StringLength(100)]
        public string Customer { get; set; } = "";

        [JsonPropertyName("html")]
        [StringLength(16535)]
        public string HTML { get; set; } = String.Empty;

        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; }

        // JSON values e.g. some used to populate the HTML template, delivery method and results, etc
        // {values: {}, documents:[], delivery:{ deliveredOn: date, method: SendGrid or WestFax, deliveryId: GUID, processedOn: date, status: rejected or success } }
        // {values: {}, {delivery:{{deliveredOn: date,deliveryMethod:,SendGrid or WestFax, deliveryId: GUID, processedOn: date, status: failed or queued:}} }}";
        [JsonPropertyName("JSON")]
        public string JSON { get; set; } = "{}";

        [JsonPropertyName("notificationType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public NotificationType NotificationType { get; set; } // NOTE! You cannot shorten the length of strings for ENUMs

        [JsonPropertyName("payorClaimNumber")]
        [StringLength(50)]
        public string PayorClaimNumber { get; set; } = "";

        [JsonPropertyName("replyTo")]
        [StringLength(50)]
        public string ReplyTo { get; set; } = "";

        [JsonPropertyName("sentOn")]
        public DateTime? SentOn { get; set; } = null;

        [JsonPropertyName("status")]
        [StringLength(20)]
        public string? Status { get; set; } = null;  // pending, queued, delivered, failed where queued means the delivery service, e.g. SendGrid, accepted it.
                                                     // TODO: add a code path in MPNotify to go update the status of "method" and "deliveredOn", similar to the OPs Report project's monitoring mechanism

        [JsonPropertyName("submittedBy")]
        [StringLength(60)]
        public string SubmittedBy { get; set; } = string.Empty;

        [JsonPropertyName("submittedOn")]
        public DateTime? SubmittedOn { get; set; } = null;

        [JsonPropertyName("to")]
        [StringLength(512)]
        public string To { get; set; } = String.Empty;

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [StringLength(255)]
        public string? zEditor { get; private set; } = null;

        public DateTime? zEditedOn { get; private set; } = null;
    }
}
