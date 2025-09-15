using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public abstract class BaseFee
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("createdBy")]
        [StringLength(60)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("dueDaysAfterColumnName")]
        public int DueDaysAfterColumnName { get; set; } = 0;

        [JsonPropertyName("dueDayType")]
        [Column(TypeName = "nvarchar(20)")]
        public DeadlineType DueDayType { get; set; } = DeadlineType.CalendarDays;

        [JsonPropertyName("feeAmount")]
        public double FeeAmount { get; set; }

        [JsonPropertyName("feeName")]
        [StringLength(60)]
        public string FeeName { get; set; } = "";

        [JsonPropertyName("feeType")]
        [Column(TypeName = "nvarchar(60)")]
        public FeeType FeeType { get; set; } = FeeType.Administrative;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("isRefundable")]
        public bool IsRefundable { get; set; }

        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; }

        // Enter the name of a date column present on the Dispute.
        // If it exists and has a value when the fee is instantiated on the Dispute,
        // the fee deadline will be initialized based on the value.
        [JsonPropertyName("referenceColumnName")]
        [StringLength(40)]
        public string ReferenceColumnName { get; set; } = "";

        [JsonPropertyName("sizeMax")]
        public int SizeMax { get; set; } = 0;

        [JsonPropertyName("sizeMin")]
        public int SizeMin { get; set; } = 0;


        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;
    }
}
