using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MPArbitration.Model
{
    public enum AuthorityCalculatorOption
    {
        Default,
        PercentOfCharges,
        PercentOfLookup
    }
    
    [Index(propertyNames: nameof(Key), IsUnique = true)]
    public class Authority
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [ForeignKey("AuthorityId")]
        [JsonPropertyName("benchmarks")]
        public virtual List<AuthorityBenchmarkDetails> Benchmarks { get; set; } = new List<AuthorityBenchmarkDetails>();

        [ForeignKey("AuthorityId")]
        [JsonPropertyName("payorAuthorityMaps")]
        public virtual List<PayorAuthorityMap> PayorAuthorityMaps { get; set; } = new List<PayorAuthorityMap>();

        [ForeignKey("AuthorityId")]
        [JsonPropertyName("archivedCases")]
        public virtual List<CaseArchive> ArchivedCases { get; set; } = new List<CaseArchive>();

        [ForeignKey("AuthorityId")]
        [JsonPropertyName("fees")]
        public virtual List<AuthorityFee> Fees { get; set; } = new List<AuthorityFee>();

        [ForeignKey("AuthorityId")]
        [JsonPropertyName("settlementDetails")]
        public virtual List<CaseSettlementDetail> SettlementDetails { get; set; } = new List<CaseSettlementDetail>();

        [ForeignKey("AuthorityId")]
        [JsonPropertyName("importHistory")]
        public virtual List<AuthorityImportDetails> ImportHistory { get; set; } = new List<AuthorityImportDetails>();

        [JsonPropertyName("activeAsOf")]
        public DateTime? ActiveAsOf { get; set; } = null;  // used in conjunction with isActive to determine the starting ServiceDate when cases may be arbitrated with this Authority

        [JsonPropertyName("calculatorOption")]
        [Column(TypeName = "nvarchar(20)")]
        public AuthorityCalculatorOption CalculatorOption { get; set; } = AuthorityCalculatorOption.Default;  // used by the UI to decide how to create offer estimates

        [JsonPropertyName("chargePct")]
        public double ChargePct { get; set; } = 0;

        // the Authority import routine is responsible for mapping
        // Ineligible Reasons to the standardized IneligibleActions

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true; // used to denote the presence or absence of a State-level arbitration program

        [JsonPropertyName("key")]
        [StringLength(8)]
        public string Key { get; set; } = "";  // e.g. tx, ca, fl, nsa, etc.

        [JsonPropertyName("name")]
        [StringLength(40)]
        public string Name { get; set; } = ""; // e.g. Texas Dept of Insurance

        [NotMapped]
        [JsonPropertyName("stats")]
        public JsonNode? Stats { get; set; } = null; // some basic stats about the entire Authority history that can be optionally requested

        // the import routine assigned to an Authority is responsible for mapping
        // Authority Status to our standardized Status entries
        [JsonPropertyName("statusValues")]
        [StringLength(1024)]
        public string StatusValues { get; set; } = ""; // Delimited list of statuses that this Authority uses. Use these values on the search screen and maybe on the Tracking screen.

        [JsonPropertyName("JSON")]  // Other Authority-specific settings such as AuthorityStatus to WorkflowStatus mappings, calculator variable overrides, etc
        [StringLength(4096)]
        public string JSON { get; set; } = "{}"; // authority status value|workflow status value;authority status value|workflow status value;...

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("useChargePct")]
        public bool UseChargePct { get; set; } = false; // use a percent of charges as the benchmark

        [JsonPropertyName("website")]
        [StringLength(512)]
        public string Website { get; set; } = "";  // website

        [ForeignKey("AuthorityId")]
        [JsonPropertyName("trackingDetails")]
        public virtual List<AuthorityTrackingDetail> TrackingDetails { get; set; } = new List<AuthorityTrackingDetail>();

        [ForeignKey("AuthorityId")]
        [JsonPropertyName("authorityGroupExclusions")]
        public virtual List<AuthorityPayorGroupExclusion> AuthorityGroupExclusions { get; set; } = new List<AuthorityPayorGroupExclusion>();

        public AuthorityJson? AuthorityJson
        {
            get
            {
                try
                {
                    var authorityJson = string.IsNullOrEmpty(this.JSON) ? new AuthorityJson() : JsonSerializer.Deserialize<AuthorityJson>(this.JSON);
                    if (authorityJson == null)
                        authorityJson = new AuthorityJson();
                    return authorityJson;
                }
                catch { 
                    return null; 
                }
            }
        }
        /*
         * 
        public List<AuthorityUserVM> GetCustomerMappings()
        {
            var mappings = new List<AuthorityUserVM>();
            if (string.IsNullOrEmpty(JSON) || !JSON.StartsWith("{") || !JSON.EndsWith("}"))
                return mappings;

            try
            {
                var obj1 = JsonNode.Parse(JSON);
                if (obj1 == null)
                    return mappings;

                List<string> keys = obj1.AsObject().Select(child => child.Key).ToList();
                var nodeName = "customerMappings";
                if (!keys.Contains(nodeName) || obj1[nodeName] == null || string.IsNullOrEmpty(obj1[nodeName]?.ToJsonString()))
                    return mappings;

                // todo: test if empty string yields an empty list
                return JsonSerializer.Deserialize<List<AuthorityUserVM>>(obj1[nodeName].ToJsonString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return mappings;
        }
         * */
    }
}
