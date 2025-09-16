using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity to handle document details for file upload
    /// </summary>
    public class UploadDocumentInput
    {

        /// <summary>
        /// Used to hold Document Type
        /// </summary>
        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; } = null!;

        /// <summary>
        /// Used to hold Authority Case Id
        /// </summary>
        [JsonPropertyName("authorityCaseId")]
        public string? AuthorityCaseId { get; set; }

        /// <summary>
        /// Used to hold Payor Claim Number
        /// </summary>
        [JsonPropertyName("payorClaimNumber")]
        public string? PayorClaimNumber { get; set; }
    }
}
