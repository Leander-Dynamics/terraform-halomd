using MPArbitration.Model;

namespace TestArbitApi
{
    public class UnitTest_NegotiationNoticeDeadline
    {
        private const string ValidTrackingJson =
            "{\"NegotiationNoticeDeadline\":\"2022-06-07T06:00:00+00:00\"," +
            "\"DateNegotiationSent\":\"2022-04-12T06:00:00+00:00\"," +
            "\"NegotiationDeadline\":\"2022-05-23T06:00:00+00:00\"," +
            "\"ArbitrationFilingStartDate\":\"2022-05-24T06:00:00+00:00\"," +
            "\"ArbitrationFilingDeadline\":\"2022-05-27T06:00:00+00:00\"," +
            "\"SubmittedToAuthority\":null,\"PayorAcceptanceDeadline\":null," +
            "\"ArbitratorSelectionDeadline\":null,\"ArbitratorAssignedOn\":null," +
            "\"NoConflictConfirmationDeadline\":null,\"ArbitrationBriefDueOn\":null," +
            "\"ArbitrationFeeDeadline\":null,\"AuthorityResolutionDeadline\":null," +
            "\"DateDecisionWasMade\":null,\"PaymentDeadlineIfWon\":null," +
            "\"ArbitrationFeeRefundDeadline\":null}";

        private const string InvalidDateTrackingJson =
            "{\"NegotiationNoticeDeadline\":\"202-06-07T06:00:00+00:00\"}";

        [Theory]
        [InlineData(ValidTrackingJson, "2022-06-07")]
        public void NegotiationNoticeDeadline_WithDateInJson_AssignsParsedDate(string json, string expectedDate)
        {
            var arb = new ArbitrationCase();

            arb.NSATracking = json;

            Assert.Equal(expectedDate, arb.NegotiationNoticeDeadline!.Value.ToString("yyyy-MM-dd"));
        }

        [Theory]
        [InlineData("", "2024-07-01")]
        [InlineData(null, "2024-07-01")]
        public void NegotiationNoticeDeadline_WhenJsonMissing_UsesEobDate(string? json, string eobDate)
        {
            var eobDateValue = DateTime.Parse(eobDate);
            var arb = new ArbitrationCase { EOBDate = eobDateValue };

            arb.NSATracking = json;

            Assert.Equal(eobDateValue.AddDays(29).ToString("yyyy-MM-dd"), arb.NegotiationNoticeDeadline!.Value.ToString("yyyy-MM-dd"));
        }

        [Theory]
        [InlineData(InvalidDateTrackingJson)]
        public void NegotiationNoticeDeadline_WithInvalidDate_ThrowsFormatException(string json)
        {
            var arb = new ArbitrationCase();

            Assert.Throws<FormatException>(() => arb.NSATracking = json);
        }

        [Theory]
        [InlineData("{\"NegotiationNoticeDeadline\":null}")]
        [InlineData("{\"NegotiationNoticeDeadline\":\"\"}")]
        [InlineData("{}")]
        [InlineData(null)]
        [InlineData("")]
        public void NegotiationNoticeDeadline_WhenValueMissing_ReturnsNull(string? json)
        {
            var arb = new ArbitrationCase();

            arb.NSATracking = json;

            Assert.Null(arb.NegotiationNoticeDeadline);
        }
    }
}
