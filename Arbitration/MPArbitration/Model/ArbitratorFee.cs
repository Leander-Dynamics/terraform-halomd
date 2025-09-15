using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    [Index(nameof(ArbitratorId), IsUnique = false)]
    public class ArbitratorFee : BaseFee
    {

        [JsonPropertyName("arbitratorId")]
        [Required]
        public int ArbitratorId { get; set; } = 0;
    }
}
