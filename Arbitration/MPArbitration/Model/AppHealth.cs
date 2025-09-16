using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MPArbitration.Model
{
    /// <summary>
    /// A general "view model" class that is used to pass a narrow set of properties back to the Arbit UI client app 
    /// and the MPNotify daemon app (the code path that builds PDFs based on SendGrid activity).
    /// </summary>
    public class AppHealthDetail
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;
        
        [JsonPropertyName("authority")]
        [Required]
        [StringLength(8)]
        public string Authority { get; set; } = "";

        [JsonPropertyName("authorityStatus")]
        [StringLength(60)]
        public string AuthorityStatus { get; set; } = "";  /** MPower field e.g. MPowerHealth, etc */

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("customer")]
        [StringLength(100)]
        public string Customer { get; set; } = "";  /** MPower field e.g. MPowerHealth, etc */

        [JsonPropertyName("DOB")]
        public DateTime? DOB { get; set; } = null;

        [JsonPropertyName("EOBDate")]
        public DateTime? EOBDate { get; set; } = null;

        [JsonPropertyName("entity")]
        [StringLength(100)]
        public string Entity { get; set; } = "";

        [JsonPropertyName("entityNPI")]
        [StringLength(40)]
        public string EntityNPI { get; set; } = "";

        [JsonPropertyName("firstResponseDate")]
        public DateTime? FirstResponseDate { get; set; } = null;
        /** MPower field - sometimes known as FirstResponseDate_Post */

        [JsonPropertyName("notificationJSON")]
        public string? NotificationJSON { get; set; } = null;

        [JsonPropertyName("notificationReplyTo")]
        public string? NotificationReplyTo { get; set; } = null;

        [JsonPropertyName("NSAStatus")]
        [StringLength(60)]
        public string NSAStatus { get; set; } = "";

        [JsonPropertyName("NSARequestEmail")]
        [Required]
        [StringLength(255)]
        public string NSARequestEmail { get; set; } = "";

        [JsonPropertyName("patientName")]
        [StringLength(50)]
        public string PatientName { get; set; } = "";

        [JsonPropertyName("payor")]
        [StringLength(60)]
        public string Payor { get; set; } = "";

        [JsonPropertyName("payorClaimNumber")]
        [StringLength(50)]
        public string PayorClaimNumber { get; set; } = ""; 

        [JsonPropertyName("providerName")]
        [StringLength(60)]
        public string ProviderName { get; set; } = "";  /** Doctor's name */

        [JsonPropertyName("providerNPI")]
        [StringLength(20)]
        public string ProviderNPI { get; set; } = ""; /** Doctor's NPI license number */

        [JsonPropertyName("receivedFromCustomer")]
        public DateTime? ReceivedFromCustomer { get; set; } = null;

        [JsonPropertyName("service")]
        [StringLength(20)]
        public string Service { get; set; } = "";  // e.g. IOM Pro

        [JsonPropertyName("serviceDate")]
        public DateTime? ServiceDate { get; set; } = null;

        [JsonPropertyName("serviceLine")]
        [StringLength(20)]
        public string ServiceLine { get; set; } = ""; /** MPower field - short form of Service e.g. IOM */

        public string DeliveryStatus
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NotificationJSON)) return "";

                var delivery = JsonNode.Parse(NotificationJSON)!.AsObject()?["delivery"]?.AsObject();
                var status = "";
                if (delivery != null)
                {
                    status = delivery["status"]?.GetValue<string>();
                }
                return status;
            }
        }
        public DateOnly? DateNegotiationSent
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NSATracking)) return null;

                //var strDateNegotiationSent = JsonNode.Parse(claim.NSATracking)!.AsObject()["DateNegotiationSent"].ToString().Substring(0, 10);
                string strDateNegotiationSent = JsonNode.Parse(NSATracking)!.AsObject()["DateNegotiationSent"]?.ToString().Substring(0, 10);
                if (!string.IsNullOrEmpty(strDateNegotiationSent))
                {
                    return DateOnly.Parse(strDateNegotiationSent);
                }
                return null;
            }
        }
        public string NSATracking { get; set; } = "";
    }
}
