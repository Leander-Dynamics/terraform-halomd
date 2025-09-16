using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public enum AuthorityTrackingDetailScope
    {
        All = 0,
        ArbitrationCase = 1,
        AuthorityDispute = 2
    }

    public class AuthorityTrackingDetail
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("authorityId")]
        public int AuthorityId { get; set; }

        [StringLength(30)]
        [JsonPropertyName("displayColumn")]
        public string DisplayColumn { get; set; } = "";

        [StringLength(255)]
        [JsonPropertyName("helpText")]
        public string HelpText { get; set; } = "";
        
        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; }

        [StringLength(50)]
        [JsonPropertyName("mapToCaseField")]
        public string? MapToCaseField { get; set; } = ""; // only used by the client app to update the ArbitrationCase property during save

        [JsonPropertyName("order")]
        public int Order { get; set; } = 0;

        [StringLength(50)]
        [JsonPropertyName("referenceFieldName")]
        public string ReferenceFieldName { get; set; } = "";

        [JsonPropertyName("scope")]
        [Column(TypeName = "nvarchar(40)")]
        public AuthorityTrackingDetailScope Scope { get; set; } = AuthorityTrackingDetailScope.All;

        [StringLength(50)]
        [JsonPropertyName("trackingLabel")]
        [Required]
        public string TrackingLabel { get; set; } = "";

        [StringLength(50)]
        [JsonPropertyName("trackingFieldName")]
        [Required]
        public string TrackingFieldName { get; set; } = "";

        [StringLength(20)]
        [JsonPropertyName("trackingFieldType")]
        [Required]
        public string TrackingFieldType { get; set; } = "";

        [JsonPropertyName("unitsFromReference")]
        public double UnitsFromReference { get; set; } = 0;

        [StringLength(20)]
        [JsonPropertyName("unitsType")]
        public string UnitsType { get; set; } = "";

        [StringLength(200)]
        [JsonPropertyName("unlockForStatuses")]
        public string UnlockForStatuses { get; set; } = "";

        [StringLength(60)]
        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;
    }
}
