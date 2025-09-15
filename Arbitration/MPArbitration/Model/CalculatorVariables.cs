using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    [Index(nameof(ServiceLine))]
    public class CalculatorVariable
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("arbitrationFee")]
        public double ArbitrationFee { get; set; } = 0;

        [JsonPropertyName("chargesCapDiscount")]
        public double ChargesCapDiscount { get; set; } = 0;  // percentage

        [JsonPropertyName("createdBy")]
        [StringLength(60)]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("nsaOfferDiscount")]
        public double NSAOfferDiscount { get; set; } = 0;

        [JsonPropertyName("nsaOfferBaseValueFieldname")]
        [StringLength(255)]
        public string NSAOfferBaseValueFieldname { get; set; } = "";

        [JsonPropertyName("offerCap")]
        public double OfferCap { get; set; } = 0;  // e.g. 35,000.00

        [JsonPropertyName("offerSpread")]
        public double OfferSpread { get; set; } = 0;   // percentage

        [JsonPropertyName("serviceLine")]
        [StringLength(20)]
        public string ServiceLine { get; set; } = "";  // e.g. IOM
    }
}
