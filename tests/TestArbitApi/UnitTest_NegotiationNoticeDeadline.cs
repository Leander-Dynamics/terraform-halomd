using MPArbitration.Model;

namespace TestArbitApi
{
    public class UnitTest_NegotiationNoticeDeadline
    {
        string JSONData = "{\"NegotiationNoticeDeadline\":\"2022-06-07T06:00:00+00:00\",\"DateNegotiationSent\":\"2022-04-12T06:00:00+00:00\",\"NegotiationDeadline\":\"2022-05-23T06:00:00+00:00\",\"ArbitrationFilingStartDate\":\"2022-05-24T06:00:00+00:00\",\"ArbitrationFilingDeadline\":\"2022-05-27T06:00:00+00:00\",\"SubmittedToAuthority\":null,\"PayorAcceptanceDeadline\":null,\"ArbitratorSelectionDeadline\":null,\"ArbitratorAssignedOn\":null,\"NoConflictConfirmationDeadline\":null,\"ArbitrationBriefDueOn\":null,\"ArbitrationFeeDeadline\":null,\"AuthorityResolutionDeadline\":null,\"DateDecisionWasMade\":null,\"PaymentDeadlineIfWon\":null,\"ArbitrationFeeRefundDeadline\":null}";
        [Fact]
        public void TestNegotiationNoticeDeadline_WithDateInCorrectFormatInJsonData()
        {
            var arb = new ArbitrationCase();
            arb.NSATracking = JSONData;
            Assert.Equal("2022-06-07", arb.NegotiationNoticeDeadline!.Value.ToString("yyyy-MM-dd"));
        }
        [Fact]
        public void TestNegotiationNoticeDeadline_EmptyJSONShouldPickEOBDate()
        {
            var arb = new ArbitrationCase() { EOBDate = DateTime.Parse("2024-07-01") };
            arb.NSATracking = "";
            Assert.Equal(arb.EOBDate.Value.AddDays(29).ToString("yyyy-MM-dd"), arb.NegotiationNoticeDeadline!.Value.ToString("yyyy-MM-dd"));
        }
        [Fact]
        public void TestNegotiationNoticeDeadline_BadFormatStringDateFormatException()
        {
            string JSONDataBad = "{\"NegotiationNoticeDeadline\":\"202-06-07T06:00:00+00:00\",}";
            var arb = new ArbitrationCase();
            Assert.Throws<FormatException>(() => arb.NSATracking = JSONDataBad);
        }
        [Fact]
        public void TestNegotiationNoticeDeadline_NullAsValue()
        {
            string JSONDataBad = "{\"NegotiationNoticeDeadline\":null,}";
            var arb = new ArbitrationCase();
            arb.NSATracking = JSONDataBad;
            Assert.Null(arb.NegotiationNoticeDeadline);
        }
        [Fact]
        public void TestNegotiationNoticeDeadline_Empty()
        {
            string JSONDataBad = "{\"NegotiationNoticeDeadline\":\"\",}";
            var arb = new ArbitrationCase();
            arb.NSATracking = JSONDataBad;
            Assert.Null(arb.NegotiationNoticeDeadline);
        }
        [Fact]
        public void TestNegotiationNoticeDeadline_NoValue()
        {
            string JSONDataBad = "{}";
            var arb = new ArbitrationCase();
            arb.NSATracking = JSONDataBad;
            Assert.Null(arb.NegotiationNoticeDeadline);
        }
        [Fact]
        public void TestNegotiationNoticeDeadline_NulData()
        {
            string JSONDataBad = "{}";
            var arb = new ArbitrationCase();
            arb.NSATracking = null;
            Assert.Null(arb.NegotiationNoticeDeadline);
        }
        [Fact]
        public void TestNegotiationNoticeDeadline_EmptyStringAsData()
        {
            string JSONDataBad = "{}";
            var arb = new ArbitrationCase();
            arb.NSATracking = string.Empty;
            Assert.Null(arb.NegotiationNoticeDeadline);
        }
    }
}