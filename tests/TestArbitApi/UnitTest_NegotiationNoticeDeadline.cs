using System;
using System.Collections.Generic;
using MPArbitration.Model;

namespace TestArbitApi
{
    public class UnitTest_NegotiationNoticeDeadline
    {
        private const string ValidNegotiationNoticeDeadlineJson = "{\"NegotiationNoticeDeadline\":\"2022-06-07T06:00:00+00:00\",\"DateNegotiationSent\":\"2022-04-12T06:00:00+00:00\",\"NegotiationDeadline\":\"2022-05-23T06:00:00+00:00\",\"ArbitrationFilingStartDate\":\"2022-05-24T06:00:00+00:00\",\"ArbitrationFilingDeadline\":\"2022-05-27T06:00:00+00:00\",\"SubmittedToAuthority\":null,\"PayorAcceptanceDeadline\":null,\"ArbitratorSelectionDeadline\":null,\"ArbitratorAssignedOn\":null,\"NoConflictConfirmationDeadline\":null,\"ArbitrationBriefDueOn\":null,\"ArbitrationFeeDeadline\":null,\"AuthorityResolutionDeadline\":null,\"DateDecisionWasMade\":null,\"PaymentDeadlineIfWon\":null,\"ArbitrationFeeRefundDeadline\":null}";

        public static IEnumerable<object?[]> NegotiationNoticeDeadlineData => new[]
        {
            new object?[] { ValidNegotiationNoticeDeadlineJson, null, "2022-06-07", null },
            new object?[] { string.Empty, "2024-07-01", "2024-07-30", null },
            new object?[] { "{\"NegotiationNoticeDeadline\":null,}", null, null, null },
            new object?[] { "{\"NegotiationNoticeDeadline\":\"\",}", null, null, null },
            new object?[] { "{}", null, null, null },
            new object?[] { null, null, null, null },
            new object?[] { string.Empty, null, null, null },
            new object?[] { "{\"NegotiationNoticeDeadline\":\"202-06-07T06:00:00+00:00\",}", null, null, typeof(FormatException) },
        };

        [Theory]
        [MemberData(nameof(NegotiationNoticeDeadlineData))]
        public void TestNegotiationNoticeDeadline(string? jsonData, string? eobDate, string? expectedDeadline, Type? expectedException)
        {
            var arb = new ArbitrationCase();

            if (!string.IsNullOrEmpty(eobDate))
            {
                arb.EOBDate = DateTime.Parse(eobDate);
            }

            if (expectedException != null)
            {
                Assert.Throws(expectedException, () => arb.NSATracking = jsonData);
                return;
            }

            arb.NSATracking = jsonData;

            if (expectedDeadline == null)
            {
                Assert.Null(arb.NegotiationNoticeDeadline);
            }
            else
            {
                Assert.Equal(expectedDeadline, arb.NegotiationNoticeDeadline!.Value.ToString("yyyy-MM-dd"));
            }
        }
    }
}
