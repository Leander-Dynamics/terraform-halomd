using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MPArbitration.Model
{
    public enum ImportFieldAction
    {
        Always,
        Ignore,
        OnlyWhenEmpty,
        /// <summary>
        /// never overwrite the target field with an empty value - prevent bulk erasure using a bad import file
        /// </summary>
        NeverWithEmpty
    }
    /// <summary>
    /// defaiult model validation is disable for this. because of exception. But model validation is done in the action method
    /// </summary>
    [ValidateNever] 
    public class ImportFieldConfig
        {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("action")]
        [StringLength(40)]
        public ImportFieldAction Action { get; set; }
        /// <summary>
        /// when evaluating an imported row, this field (even if required to be present) can have an empty value
        /// </summary>
        [JsonPropertyName("canBeEmpty")]
        public bool CanBeEmpty { get; set; }
        /// <summary>
        /// use this to describe or link to any business rules affected by altering the settings
        /// </summary>
        [JsonPropertyName("description")]
        [StringLength(2048)]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("isBoolean")]
        public bool IsBoolean { get; set; }

        [JsonPropertyName("isDate")]
        public bool IsDate { get; set; }

        [JsonPropertyName("isNumeric")]
        public bool IsNumeric { get; set; }

        /// <summary>
        /// a column of this name must be in the import
        /// </summary>
        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; }

        /// <summary>
        /// this column's targetFieldname is found in the Tracking record
        /// </summary>
        [JsonPropertyName("isTracking")]
        public bool IsTracking { get; set; } 

        [JsonPropertyName("source")]
        [StringLength(40)]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("sourceFieldname")]
        [StringLength(100)]
        public string SourceFieldname { get; set; } = string.Empty;

        [JsonPropertyName("targetFieldname")]
        [StringLength(100)]
        public string TargetFieldname { get; set; } = string.Empty;

        /// <summary>
        /// for future use - will allow for granular targeting of authority-only custom fields
        /// </summary>
        [JsonPropertyName("targetAuthorityKey")]
        [StringLength(8)]
        public string TargetAuthorityKey { get; set; } = string.Empty;

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; }
    }
}
