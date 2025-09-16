using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace MPArbitration.Model
{
    public class GroupExclusionBase
    {
        [JsonPropertyName("groupNumber")]
        [Required]
        [StringLength(20)]
        public string GroupNumber { get; set; } = "";

        [JsonPropertyName("isNSAIneligible")]
        public bool IsNSAIneligible { get; set; }

        [JsonPropertyName("isStateIneligible")]
        public bool IsStateIneligible { get; set; }
    }

    public class PayorGroupImport : GroupExclusionBase
    {
        [JsonPropertyName("groupName")]
        [Required]
        [StringLength(60)]
        public string GroupName { get; set; } = "";

        [JsonPropertyName("payorName")]
        [Required]
        [StringLength(60)]
        public string PayorName { get; set; } = "";

        [JsonPropertyName("planType")]
        [Required]
        [Column(TypeName = "nvarchar(20)")]
        public PlanType PlanType { get; set; }
    }

    public class PayorGroupExclusionImport : GroupExclusionBase
    {
        [JsonPropertyName("authority")]
        [Required]
        [StringLength(8)]
        public string Authority { get; set; } = "";

        [JsonPropertyName("payorName")]
        [Required]
        [StringLength(60)]
        public string PayorName { get; set; } = "";
    }

    [Index(nameof(PayorId), nameof(GroupName), IsUnique = true)]
    [Index(nameof(PayorId), nameof(GroupNumber), IsUnique = true)]
    public class PayorGroup : GroupExclusionBase
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0; 
        
        [JsonPropertyName("groupName")]
        [Required]
        [StringLength(60)]
        public string GroupName { get; set; } = "";

        [JsonPropertyName("payorId")]
        [Required]
        public int PayorId { get; set; } = 0;

        [JsonPropertyName("planType")]
        [Required]
        [Column(TypeName = "nvarchar(20)")]
        public PlanType PlanType { get; set; }

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;
    }
}
