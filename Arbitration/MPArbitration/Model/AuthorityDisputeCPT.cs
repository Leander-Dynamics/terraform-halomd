using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{

    public class AuthorityDisputeDetailsCSV
    {
        [JsonPropertyName("arbitrationCaseId")]
        [Required]
        public int ArbitrationCaseId { get; set; } = 0; // aka "Arbit Id"

        [JsonPropertyName("authorityKey")]
        [Required]
        [StringLength(3)]
        public string AuthorityKey { get; set; } = "";

        [JsonPropertyName("authorityCaseId")]
        [StringLength(30)]
        public string AuthorityCaseId { get; set; } = "";  // aka "Dispute Number"

        [StringLength(60)]
        [JsonPropertyName("addedBy")]
        public string AddedBy { get; set; } = "";

        [JsonPropertyName("addedOn")]
        public DateTime? AddedOn { get; set; } = null;

        [JsonPropertyName("awardAmount")]
        public double AwardAmount { get; set; } = 0;

        [JsonPropertyName("claimCPTCode")]
        public string ClaimCPTCode { get; set; } = null!;  // This will act as a checksum against the ClaimCPTId value

        [JsonPropertyName("finalUnitOfferAmount")]
        public double FinalUnitOfferAmount { get; set; } = 0; // FinalOfferAmount will be calculated based on this unit amount

    }

    public class AuthorityDisputeCPT
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [StringLength(60)]
        [JsonPropertyName("addedBy")]
        public string AddedBy { get; set; } = "";

        [JsonPropertyName("addedOn")]
        public DateTime? AddedOn { get; set; } = null;

        //[JsonPropertyName("awardAmount")]
        //public double AwardAmount { get; set; } = 0;  // seems to be no use for this - awards should go into the CaseSettlementCPTs PerUnitAwardAmount field

        // Relates this record to the AuthorityDispute table
        [JsonPropertyName("authorityDisputeId")]
        public int AuthorityDisputeId { get; set; }  // Required foreign key property

        [JsonPropertyName("benchmarkDataItemId")]
        public int BenchmarkDataItemId { get; set; } = 0; // soft link to the benchmark used to calculate an offer

        [JsonPropertyName("benchmarkDatasetId")]
        public int BenchmarkDatasetId { get; set; } = 0; // soft link to the benchmark set used to calculate an offer - useful for fetching metadata

        [JsonPropertyName("benchmarkAmount")]
        public double BenchmarkAmount { get; set; } = 0; // Benchmark specified in Calculator Variables - copied from the Claim CPT itself

        [JsonPropertyName("benchmarkOverrideAmount")]
        public double BenchmarkOverrideAmount { get; set; } = 0;  // amount actually submitted to the Authority

        [JsonPropertyName("claimCPTId")]
        public int ClaimCPTId { get; set; }  // Required foreign key property

        [JsonPropertyName("claimCPT")]
        public ClaimCPT ClaimCPT { get; set; } = null!;  // Required reference navigation to principal

        //[JsonPropertyName("dispute")]
        //public AuthorityDispute Dispute { get; set; } = null!;  // Required reference navigation to principal

        [JsonPropertyName("calculatedOfferAmount")]
        public double CalculatedOfferAmount { get; set; } = 0; // uses the Authority configuration and the Calculator Variables

        [JsonPropertyName("finalOfferAmount")]
        public double FinalOfferAmount { get; set; } = 0; // uses the Authority configuration and the Calculator Variables

        [JsonPropertyName("serviceLineDiscount")]
        public double ServiceLineDiscount { get; set; } = 0;

        [StringLength(60)]
        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;
    }

    [NotMapped]
    public class AuthorityDisputeCPTVM : AuthorityDisputeCPT
    {
        //[JsonPropertyName("arbitrationCaseId")]
        //public int ArbitrationCaseId { get; set; }  // aka "Arbit Id" or "claim Id"


        [JsonPropertyName("customer")]
        public string Customer { get; set; } = "";

        [JsonPropertyName("entity")]
        public string Entity { get; set; } = "";

        [JsonPropertyName("entityNPI")]
        public string EntityNPI { get; set; } = "";

        [JsonPropertyName("geoRegion")]
        public string GeoRegion { get; set; } = "";

        [JsonPropertyName("geoZip")]
        public string GeoZip { get; set; } = "";

        [JsonPropertyName("isAwarded")]
        public bool IsAwarded { get; set; }

        //[JsonPropertyName("modifiers")]
        //public string Modifiers { get; set; } = "";

        [JsonPropertyName("notificationDate")]
        public DateTime? NotificationDate { get; set; } = null; // Is this the date a notification was sent out for the related ?

        [JsonPropertyName("payor")]
        public string Payor { get; set; } = "";

        [JsonPropertyName("payorClaimNumber")]
        public string PayorClaimNumber { get; set; } = "";

        [JsonPropertyName("payorId")]
        public int? PayorId { get; set; } = null;

        [JsonPropertyName("planType")]
        public string PlanType { get; set; } = "";

        //[JsonPropertyName("providerChargeAmount")]
        //public double ProviderChargeAmount { get; set; } // how much was originally charged for this CPT

        [JsonPropertyName("providerName")]
        public string ProviderName { get; set; } = "";

        [JsonPropertyName("providerNPI")]
        public string ProviderNPI { get; set; } = "";

        [JsonPropertyName("serviceDate")]
        public DateTime? ServiceDate { get; set; } = null;

        [JsonPropertyName("serviceLine")]
        public string ServiceLine { get; set; } = "";

        //[JsonPropertyName("units")]
        //public int Units { get; set; }


    }
}
