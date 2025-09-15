namespace MPArbitration.Model
{
    using System.Text.Json.Serialization;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;
    using System.ComponentModel.DataAnnotations;

    public class OfferHistory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("authority")]
        [StringLength(8)]
        public string? Authority { get; set; } = null;

        [JsonPropertyName("arbitrationCaseId")]
        public int ArbitrationCaseId { get; set; } = 0;

        // Note! There is a one-to-one relationship between OfferHistory and CaseSettlement. This
        // results in the OfferHistory ID being stored in the CaseSettlement table and the Case Settlement Id
        // being stored in the Offer History table. It can be confusing to look at. Either side can be null, i.e.
        // an Offer may or may not have reached a settlement and a CaseSettlement may or may not have come from
        // an offer (a settlement may come directly from arbitration/mediation).
        [JsonPropertyName("caseSettlementId")]
        public int? CaseSettlementId { get; set; } = null;

        [JsonPropertyName("offerAmount")]
        public double OfferAmount { get; set; } = 0;

        [JsonPropertyName("offerSource")]
        [StringLength(40)]
        public string OfferSource { get; set; } = ""; /** Email, Fax, Phone, Text, Other */

        [StringLength(20)]
        [JsonPropertyName("offerType")]
        public string OfferType { get; set; } = "";  /** The type of offer such as Payor, Provider or even something like PayorInformal */

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";

        [StringLength(60)]
        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        [JsonPropertyName("wasOfferAccepted")]
        public bool WasOfferAccepted { get; set; }

    }
}
