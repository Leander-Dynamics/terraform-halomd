using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace MPArbitration.Model
{
    [Index(nameof(ParentId),IsUnique =false)]
    [Index(nameof(Name),IsUnique=true)]
    public class Payor
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("JSON")]  
        public string JSON { get; set; } = "{}"; // Other Payor-specific settings such as notification templates

        [JsonPropertyName("name")]
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = "";

        [JsonPropertyName("NSARequestEmail")]
        [Required]
        [StringLength(255)]
        public string NSARequestEmail { get; set; } = "";

        [JsonPropertyName("parentId")]
        public int ParentId { get; set; } = 0;

        [JsonPropertyName("sendNSARequests")]
        public bool SendNSARequests { get; set; } = false;

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [ForeignKey("PayorId")]
        [JsonPropertyName("addresses")]
        public virtual List<PayorAddress> Addresses { get; set; } = new List<PayorAddress>();

        [ForeignKey("PayorId")]
        [JsonPropertyName("arbitrationCases")]
        public virtual List<ArbitrationCase> ArbitrationCases { get; set; } = new List<ArbitrationCase>();

        [ForeignKey("PayorId")]
        [JsonPropertyName("authorityGroupExceptions")]
        public virtual List<AuthorityPayorGroupExclusion> AuthorityGroupExceptions { get; set; } = new List<AuthorityPayorGroupExclusion>();

        [ForeignKey("PayorId")]
        [JsonPropertyName("caseSettlements")]
        public virtual List<CaseSettlement> CaseSettlements { get; set; } = new List<CaseSettlement>();

        [ForeignKey("PayorId")]
        [JsonPropertyName("negotiators")]
        public virtual List<Negotiator> Negotiators { get; set; } = new List<Negotiator>();

        [ForeignKey("PayorId")]
        [JsonPropertyName("payorGroups")]
        public virtual List<PayorGroup> PayorGroups { get; set; } = new List<PayorGroup>();

        // todo: Refactor this so it works like the AuthorityJson class
        public List<EntityVM> GetExcludedEntities()
        {
            var entities = new List<EntityVM>();
            if (string.IsNullOrEmpty(JSON) || !JSON.StartsWith("{") || !JSON.EndsWith("}"))
                return entities;

            try
            {
                var jsonNode = JsonNode.Parse(JSON);
                if (jsonNode == null)
                    return entities;
                
                List<string> keys = jsonNode.AsObject().Select(child => child.Key).ToList();
                if (!keys.Contains("exclusions") || jsonNode["exclusions"] == null || string.IsNullOrEmpty(jsonNode["exclusions"]?.ToJsonString()))
                    return entities;
                return JsonSerializer.Deserialize<List<EntityVM>>(jsonNode["exclusions"].ToJsonString())?? new List<EntityVM>();
            }
            catch(Exception ex) { 
                Console.WriteLine(ex.ToString());
            }
            return entities;
        }
    }
}
