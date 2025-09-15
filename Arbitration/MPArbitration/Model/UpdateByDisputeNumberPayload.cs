using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MPArbitration.Model
{
    /// <summary>
    /// Diapute update payload
    /// </summary>
    public class UpdateByDisputeNumberPayload
    {

        /// <summary>
        /// Used to set disputeNumber from payload
        /// </summary>
        [Required]
        [JsonPropertyName("disputeNumber")]
        public string? DisputeNumber { get; set; }

        /// <summary>
        /// Used to set disputeStatus from payload
        /// </summary>
        [JsonPropertyName("disputeStatus")]
        public string? DisputeStatus { get; set; }

        /// <summary>
        /// Used to set feeAmountAdmin from payload
        /// </summary>
        [JsonPropertyName("feeAmountAdmin")]
        public decimal? FeeAmountAdmin { get; set; }

        /// <summary>
        /// Used to set feeAmountEntity from payload
        /// </summary>
        [JsonPropertyName("feeAmountEntity")]
        public decimal? FeeAmountEntity { get; set; }

        /// <summary>
        /// Used to set feeDueDate from payload
        /// </summary>
        [JsonPropertyName("feeDueDate")]
        public DateTime? FeeDueDate { get; set; }

        /// <summary>
        /// Used to set NSACaseId from payload
        /// </summary>
        [JsonPropertyName("nsaCaseId")]
        public string? NSACaseId { get; set; }

        /// <summary>
        /// Used to set NSAStatus from payload
        /// </summary>
        [JsonPropertyName("nsaStatus")]
        public string? NSAStatus { get; set; }

        /// <summary>
        /// Used to set NSAWorkflowStatus from payload
        /// </summary>
        [JsonPropertyName("nsaWorkflowStatus")]
        public string? NSAWorkflowStatus { get; set; }

        /// <summary>
        /// Used to set Notes from payload
        /// </summary>
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Used to set Formalreceiveddate from payload
        /// </summary>
        [JsonPropertyName("formalReceivedDate")]
        public DateTime? FormalReceivedDate { get; set; }

        /// <summary>
        /// Used to set BriefDueDate from payload
        /// </summary>
        [JsonPropertyName("briefDueDate")]
        public DateTime? BriefDueDate { get; set; }

        /// <summary>
        /// Used to set AssignmentDeadlineDate from payload
        /// </summary>
        [JsonPropertyName("assignmentDeadlineDate")]
        public DateTime? AssignmentDeadlineDate { get; set; }


        /// <summary>
        /// Used to set ArbitratorAssignmentDate from payload
        /// </summary>
        [JsonPropertyName("arbitratorAssignmentDate")]
        public DateTime? ArbitratorAssignmentDate { get; set; }

    }
}
