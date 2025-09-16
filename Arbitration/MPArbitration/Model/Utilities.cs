using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Text.Json.Serialization;
using System.Threading;
using ObjectsComparator.Comparator.Helpers;
using System.Text;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System;
using ObjectsComparator.Comparator.RepresentationDistinction;
using CsvHelper;
using Newtonsoft.Json.Linq;
using NuGet.Packaging;
using Microsoft.AspNetCore.Mvc;
using CsvHelper.Configuration;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using MPArbitration.Controllers;


namespace MPArbitration.Model
{
    public struct CSVHeaderRowInfo
    {
        public string[]? cols;
        public bool FoundHeader;
        public int HeaderRowNumber;
        public List<string> matches;
        public string Message;
        public string HeaderValue;
        public string[] requiredFields;

        public CSVHeaderRowInfo()
        {
            cols = null;
            FoundHeader = false;
            HeaderRowNumber = 0;
            HeaderValue = "";
            matches = new List<string>();
            Message = "";
            requiredFields = new string[] { };
        }
    }

    public static class Utilities
    {
        static string _BLANK = "___";
        public static readonly ArbitrationStatus[] CLOSED_STATUSES = {  ArbitrationStatus.ClosedPaymentReceived,
                                                    ArbitrationStatus.ClosedPaymentWithdrawn,
                                                    ArbitrationStatus.SettledArbitrationHealthPlanWon,
                                                    ArbitrationStatus.SettledArbitrationNoDecision,
                                                    ArbitrationStatus.SettledArbitrationPendingPayment,
                                                    ArbitrationStatus.SettledInformalPendingPayment,
                                                    ArbitrationStatus.SettledArbitrationPendingPayment};

        public static readonly ArbitrationStatus[] OPEN_STATUSES = { ArbitrationStatus.ActiveArbitrationBriefCreated,
                                                   ArbitrationStatus.ActiveArbitrationBriefNeeded,
                                                   ArbitrationStatus.ActiveArbitrationBriefSubmitted,
                                                   ArbitrationStatus.DetermineAuthority,
                                                   ArbitrationStatus.InformalInProgress,
                                                   ArbitrationStatus.MissingInformation,
                                                   ArbitrationStatus.New,
                                                   ArbitrationStatus.Open,
                                                   ArbitrationStatus.PendingArbitration };
        public static string DetectDifferencesBetweenObjects(Object orig, Object updates)
        {
            // Note: Fields that only appear in a Tracking JSON string cannot be detected for changes. 
            // For instance, in order to detect a change in the NSA's DateOfInitialClaimPayment tracking field, the field that 
            // it is synced to in the ArbitrationCase record must have changed. This could be something like ProviderPaidDate or FirstResponseDate or EOBDate.
            DeepEqualityResult? deep = null;
            try
            {
                deep = orig.DeeplyEquals(updates);
            }
            catch
            {
                return ""; // this is extremely rare - need to debug in a test environment
            }
            var results = deep.Select(x => new KeyValuePair<string, object?>(x.Path, x.ActualValue)).Where(d => !d.Key.StartsWith("Updated", StringComparison.CurrentCultureIgnoreCase));
            if (results.Count() > 0)
            {
                StringBuilder t = new StringBuilder("{");
                foreach (var r in results)
                {
                    t.Append($@"""{r.Key}"":");
                    if (r.Value == null)
                    {
                        t.Append("null");
                    }
                    else
                    {
                        bool isNumeric = double.TryParse(r.Value.ToString(), out double result);
                        if (!isNumeric)
                        {
                            bool isBool = Boolean.TryParse(r.Value.ToString(), out bool bv);
                            try
                            {
                                string s = r.Value.ToString() ?? "";
                                if (isBool)
                                    t.Append(s.ToLower() + ",");
                                else
                                {
                                    s = s.Replace("\"", "");
                                    if (string.IsNullOrEmpty(s))
                                    {
                                        t.Append("\"\",");
                                    }
                                    else
                                    {
                                        t.Append($@"""{s}"",");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                        else
                        {
                            t.Append(r.Value + ",");
                        }
                    }
                }
                t.Length = t.Length - 1;
                t.Append("}");
                return t.ToString();
            }
            return "";
        }

        public static CSVHeaderRowInfo FindCSVHeaderRow(List<string> upload, List<ImportFieldConfig> fieldList)
        {
            int reqFieldsCount = 0;
            int rowCount = 0;
            var result = new CSVHeaderRowInfo();
            result.requiredFields = fieldList.Where(d => d.IsRequired && d.IsActive).Select(d => d.SourceFieldname.ToLower()).ToArray();

            Regex CSVParser = new Regex("[,|](?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

            foreach (string row in upload)
            {
                rowCount++;

                // try split
                if (!string.IsNullOrEmpty(row))
                {
                    result.cols = CSVParser.Split(row);
                    if (result.cols.Length > 2)
                    {
                        // check for presence of header row
                        if (!result.FoundHeader)
                        {
                            // is current line the header row?
                            foreach (var col in result.cols)
                            {
                                var low = col.Trim().Replace(@"""", "").ToLower();
                                if (fieldList.FirstOrDefault(d => d.SourceFieldname == low) != null)
                                {
                                    result.matches.Add(low);
                                    if (result.requiredFields.Length > 0)
                                    {
                                        reqFieldsCount += result.requiredFields.FirstOrDefault(d => d == low) == null ? 0 : 1;
                                    }
                                }
                                else
                                {
                                    result.matches.Add("");
                                }
                            }
                            // basic validation - if we find 3+ matching column names on this row then assume it is the header
                            // this will tolerate the government mucking about with the export format
                            // and not totally breaking our import immediately although some columns may not come in
                            result.FoundHeader = result.matches.Count(d => !string.IsNullOrEmpty(d)) > 2;
                            if (!result.FoundHeader)
                            {
                                result.matches.Clear();
                                reqFieldsCount = 0;
                            }
                            else if (reqFieldsCount != result.requiredFields.Count())
                            {
                                var rf = String.Join(',', result.requiredFields);
                                result.Message = "One or more of these required columns is missing:" + rf;
                                return result;
                            }
                            else
                            {
                                result.HeaderRowNumber = rowCount;
                                result.FoundHeader = true;
                                result.HeaderValue = row;
                                return result;
                            }
                        }
                    }
                }
            }

            result.Message = "No valid Header Row found";
            return result;
        }

        /// <summary>
        /// Makes date corrections under the assumption that values earlier than Central Standard Time are likely skewed toward UTC time.
        /// This can result in date "drift" that shifts dates to the wrong day in the USA.
        /// </summary>
        /// <param name="claim"></param>
        /// <returns></returns>
        public static bool FixStateArbitrationCaseDates(ArbitrationCase claim)
        {
            bool isChanged = false;

            var nd = EnsureUtcMinimumHours(claim.ArbitrationBriefDueDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.ArbitrationBriefDueDate);
            claim.ArbitrationBriefDueDate = nd;

            nd = EnsureUtcMinimumHours(claim.ArbitrationDeadlineDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.ArbitrationDeadlineDate);
            claim.ArbitrationDeadlineDate = nd;

            nd = EnsureUtcMinimumHours(claim.ArbitratorPaymentDeadlineDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.ArbitratorPaymentDeadlineDate);
            claim.ArbitratorPaymentDeadlineDate = nd;

            nd = EnsureUtcMinimumHours(claim.AssignmentDeadlineDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.AssignmentDeadlineDate);
            claim.AssignmentDeadlineDate = nd;

            nd = EnsureUtcMinimumHours(claim.DOB);
            isChanged = isChanged || !nd.DeeplyEquals(claim.DOB);
            claim.DOB = nd;

            nd = EnsureUtcMinimumHours(claim.EOBDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.EOBDate);
            claim.EOBDate = nd;

            nd = EnsureUtcMinimumHours(claim.FirstAppealDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.FirstAppealDate);
            claim.FirstAppealDate = nd;

            nd = EnsureUtcMinimumHours(claim.FirstResponseDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.FirstResponseDate);
            claim.FirstResponseDate = nd;

            nd = EnsureUtcMinimumHours(claim.InformalTeleconferenceDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.InformalTeleconferenceDate);
            claim.InformalTeleconferenceDate = nd;

            nd = EnsureUtcMinimumHours(claim.PaymentMadeDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.PaymentMadeDate);
            claim.PaymentMadeDate = nd;

            nd = EnsureUtcMinimumHours(claim.PayorResolutionRequestReceivedDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.PayorResolutionRequestReceivedDate);
            claim.PayorResolutionRequestReceivedDate = nd;

            nd = EnsureUtcMinimumHours(claim.ProviderPaidDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.ProviderPaidDate);
            claim.ProviderPaidDate = nd;

            nd = EnsureUtcMinimumHours(claim.ReceivedFromCustomer);
            isChanged = isChanged || !nd.DeeplyEquals(claim.ReceivedFromCustomer);
            claim.ReceivedFromCustomer = nd;

            nd = EnsureUtcMinimumHours(claim.RequestDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.RequestDate);
            claim.RequestDate = nd;

            nd = EnsureUtcMinimumHours(claim.ResolutionDeadlineDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.ResolutionDeadlineDate);
            claim.ResolutionDeadlineDate = nd;

            nd = EnsureUtcMinimumHours(claim.ServiceDate);
            isChanged = isChanged || !nd.DeeplyEquals(claim.ServiceDate);
            claim.ServiceDate = nd;

            return isChanged;
        }

        /// <summary>
        /// Makes date corrections under the assumption that values earlier than Central Standard Time are likely skewed toward UTC time.
        /// This can result in date "drift" that shifts dates to the wrong day in the USA.
        /// </summary>
        /// <param name="Settlements"></param>
        /// <returns></returns>
        public static bool FixRawCaseSettlementDates(IEnumerable<CaseSettlement> Settlements)
        {
            bool isChanged = false;

            foreach (var s in Settlements)
            {
                var nd = EnsureUtcMinimumHours(s.ArbitrationDecisionDate);
                isChanged = isChanged || !nd.DeeplyEquals(s.ArbitrationDecisionDate);
                s.ArbitrationDecisionDate = nd;

                nd = EnsureUtcMinimumHours(s.ArbitratorReportSubmissionDate);
                isChanged = isChanged || !nd.DeeplyEquals(s.ArbitratorReportSubmissionDate);
                s.ArbitratorReportSubmissionDate = nd;

                nd = EnsureUtcMinimumHours(s.PartiesAwardNotificationDate);
                isChanged = isChanged || !nd.DeeplyEquals(s.PartiesAwardNotificationDate);
                s.PartiesAwardNotificationDate = nd;

                foreach (var d in s.CaseSettlementDetails)
                {
                    nd = EnsureUtcMinimumHours(d.ArbitrationDecisionDate);
                    isChanged = isChanged || !nd.DeeplyEquals(d.ArbitrationDecisionDate);
                    d.ArbitrationDecisionDate = nd;

                    nd = EnsureUtcMinimumHours(d.ArbitratorReportSubmissionDate);
                    isChanged = isChanged || !nd.DeeplyEquals(d.ArbitratorReportSubmissionDate);
                    d.ArbitratorReportSubmissionDate = nd;

                    nd = EnsureUtcMinimumHours(d.PartiesAwardNotificationDate);
                    isChanged = isChanged || !nd.DeeplyEquals(d.PartiesAwardNotificationDate);
                    d.PartiesAwardNotificationDate = nd;

                    nd = EnsureUtcMinimumHours(d.PaymentMadeDate);
                    isChanged = isChanged || !nd.DeeplyEquals(d.PaymentMadeDate);
                    d.PaymentMadeDate = nd;
                }
            }
            return isChanged;
        }

        /// <summary>
        /// Convert any Unspecified dates to Central Standard Time (Where Texas is located)
        /// TODO: Could use Reflection to apply a particular time zone to ALL date properties of ANY object.
        /// </summary>
        /// <param name="tdi"></param>
        public static void FixRawTDIDates(TDIRequestDetails tdi)
        {
            if (tdi.ArbitrationDate.HasValue && tdi.ArbitrationDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.ArbitrationDate.Value.Hour == 0)
                    tdi.ArbitrationDate = Utilities.ConvertUnspecifiedToCST(tdi.ArbitrationDate.Value);

                tdi.ArbitrationDate = DateTime.SpecifyKind(tdi.ArbitrationDate!.Value, DateTimeKind.Local);
            }

            if (tdi.ArbitrationDecisionDate.HasValue && tdi.ArbitrationDecisionDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.ArbitrationDecisionDate.Value.Hour == 0)
                    tdi.ArbitrationDecisionDate = Utilities.ConvertUnspecifiedToCST(tdi.ArbitrationDecisionDate.Value);

                tdi.ArbitrationDecisionDate = DateTime.SpecifyKind(tdi.ArbitrationDecisionDate!.Value, DateTimeKind.Local);
            }

            if (tdi.AssignmentDeadlineDate.HasValue && tdi.AssignmentDeadlineDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.AssignmentDeadlineDate.Value.Hour == 0)
                    tdi.AssignmentDeadlineDate = Utilities.ConvertUnspecifiedToCST(tdi.AssignmentDeadlineDate.Value);

                tdi.AssignmentDeadlineDate = DateTime.SpecifyKind(tdi.AssignmentDeadlineDate!.Value, DateTimeKind.Local);
            }

            if (tdi.ArbitratorReportSubmissionDate.HasValue && tdi.ArbitratorReportSubmissionDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.ArbitratorReportSubmissionDate.Value.Hour == 0)
                    tdi.ArbitratorReportSubmissionDate = Utilities.ConvertUnspecifiedToCST(tdi.ArbitratorReportSubmissionDate.Value);

                tdi.ArbitratorReportSubmissionDate = DateTime.SpecifyKind(tdi.ArbitratorReportSubmissionDate!.Value, DateTimeKind.Local);
            }

            if (tdi.InformalTeleconferenceDate.HasValue && tdi.InformalTeleconferenceDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.InformalTeleconferenceDate.Value.Hour == 0)
                    tdi.InformalTeleconferenceDate = Utilities.ConvertUnspecifiedToCST(tdi.InformalTeleconferenceDate.Value);

                tdi.InformalTeleconferenceDate = DateTime.SpecifyKind(tdi.InformalTeleconferenceDate!.Value, DateTimeKind.Local);
            }

            if (tdi.PartiesAwardNotificationDate.HasValue && tdi.PartiesAwardNotificationDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.PartiesAwardNotificationDate.Value.Hour == 0)
                    tdi.PartiesAwardNotificationDate = Utilities.ConvertUnspecifiedToCST(tdi.PartiesAwardNotificationDate.Value);

                tdi.PartiesAwardNotificationDate = DateTime.SpecifyKind(tdi.PartiesAwardNotificationDate!.Value, DateTimeKind.Local);
            }

            if (tdi.PaymentMadeDate.HasValue && tdi.PaymentMadeDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.PaymentMadeDate.Value.Hour == 0)
                    tdi.PaymentMadeDate = Utilities.ConvertUnspecifiedToCST(tdi.PaymentMadeDate.Value);

                tdi.PaymentMadeDate = DateTime.SpecifyKind(tdi.PaymentMadeDate!.Value, DateTimeKind.Local);
            }

            if (tdi.PayorResolutionRequestReceivedDate.HasValue && tdi.PayorResolutionRequestReceivedDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.PayorResolutionRequestReceivedDate.Value.Hour == 0)
                    tdi.PayorResolutionRequestReceivedDate = Utilities.ConvertUnspecifiedToCST(tdi.PayorResolutionRequestReceivedDate.Value);

                tdi.PayorResolutionRequestReceivedDate = DateTime.SpecifyKind(tdi.PayorResolutionRequestReceivedDate!.Value, DateTimeKind.Local);
            }

            if (tdi.ProviderPaidDate.HasValue && tdi.ProviderPaidDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.ProviderPaidDate.Value.Hour == 0)
                    tdi.ProviderPaidDate = Utilities.ConvertUnspecifiedToCST(tdi.ProviderPaidDate.Value);

                tdi.ProviderPaidDate = DateTime.SpecifyKind(tdi.ProviderPaidDate!.Value, DateTimeKind.Local);
            }

            if (tdi.RequestDate.HasValue && tdi.RequestDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.RequestDate.Value.Hour == 0)
                    tdi.RequestDate = Utilities.ConvertUnspecifiedToCST(tdi.RequestDate.Value);

                tdi.RequestDate = DateTime.SpecifyKind(tdi.RequestDate!.Value, DateTimeKind.Local);
            }

            if (tdi.ResolutionDeadlineDate.HasValue && tdi.ResolutionDeadlineDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.ResolutionDeadlineDate.Value.Hour == 0)
                    tdi.ResolutionDeadlineDate = Utilities.ConvertUnspecifiedToCST(tdi.ResolutionDeadlineDate.Value);

                tdi.ResolutionDeadlineDate = DateTime.SpecifyKind(tdi.ResolutionDeadlineDate!.Value, DateTimeKind.Local);
            }

            if (tdi.ServiceDate.HasValue && tdi.ServiceDate.Value.Kind == DateTimeKind.Unspecified)
            {
                if (tdi.ServiceDate.Value.Hour == 0)
                    tdi.ServiceDate = Utilities.ConvertUnspecifiedToCST(tdi.ServiceDate.Value);

                tdi.ServiceDate = DateTime.SpecifyKind(tdi.ServiceDate!.Value, DateTimeKind.Local);
            }

        }

        public static string FormatBytes(long bytes)
        {
            const int scale = 1024;
            string[] orders = new string[] { "GB", "MB", "KB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);

                max /= scale;
            }
            return "0 Bytes";
        }

        public static bool IsActiveStateCase(IAuthorityCase authCase)
        {
            // Just assuming TX for now
            var test = authCase.AuthorityStatus.ToLower();
            return test.Contains("assigned") || test.Contains("submitted") || test.Contains("not settled");
        }

        public static bool IsActiveWorkflow(ArbitrationStatus Status)
        {
            return Status == ArbitrationStatus.New ||
                Status == ArbitrationStatus.Open ||
                Status == ArbitrationStatus.DetermineAuthority ||
                Status == ArbitrationStatus.MissingInformation ||
                Status == ArbitrationStatus.ActiveArbitrationBriefCreated ||
                Status == ArbitrationStatus.ActiveArbitrationBriefNeeded ||
                Status == ArbitrationStatus.ActiveArbitrationBriefSubmitted ||
                Status == ArbitrationStatus.InformalInProgress ||
                Status == ArbitrationStatus.PendingArbitration;
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static string MergeTemplateData(string HTML, NotificationType templateType, Object src, CalculatorVariable calcVars, Authority stateAuth, string nsaReplyTo, ILogger _logger)
        {
            if (templateType != NotificationType.NSANegotiationRequest && templateType != NotificationType.NSANegotiationRequestAttachment || src == null)
                return HTML;

            var clone = HTML;

            var srcProps = src.GetType().GetProperties().Where(d => d.GetIndexParameters().Length == 0).ToArray();
            if (srcProps == null || srcProps.Length == 0)
                return HTML;

            // process any embedded tables first to strip out the compound tokens
            try
            {
                var doc = new HtmlDocument();

                var regex = new Regex(@"<tbody .*data-mp-tbody=""(\w+)""\W+.*<\/tbody>", RegexOptions.Multiline);  // should be name of a collection property on ArbitrationCase class e.g. cptCodes
                var obj = src;

                // TODO: Add Tracking and NSATracking props to obj for later use in calculations ?

                var matched = regex.Matches(HTML);
                var root = JsonNode.Parse(JsonSerializer.Serialize(obj));
                if (root == null)
                    return HTML;

                root["Customer_$_nsaReplyTo"] = nsaReplyTo;

                for (int count = 0; count < matched.Count; count++)
                {
                    var v = matched[count].Groups;
                    var v0 = v[0].Value;
                    var rowSrc = v[1].Value; // should match a collection property's name on the src Object so...

                    doc.LoadHtml("<html><body><p><table>" + v0 + "</table></p></body></html>");
                    var table = doc.DocumentNode.SelectSingleNode("//table");
                    var tBody = table.SelectSingleNode("//tbody");
                    if (tBody == null)
                        continue;
                    var tableRows = tBody.SelectNodes("tr");
                    if (tableRows == null || tableRows.Count == 0)
                        continue;

                    // ...verify that src has an IEnumerable property matching the data-mp attribute
                    PropertyInfo? srcProp = srcProps.FirstOrDefault(d => d.Name.Equals(rowSrc, StringComparison.CurrentCultureIgnoreCase));
                    if (srcProp == null || srcProp.PropertyType.GetInterfaces().FirstOrDefault(d => d.Name.Contains("IEnumerable")) == null)
                        continue;

                    Object? coll = srcProp.GetValue(obj); // get the collection values
                    if (coll == null)
                        continue;

                    // use JSON for dealing with the collection since we don't know what kind it is - easier than using reflection
                    var temp = JsonSerializer.Serialize(coll);
                    var items = JsonNode.Parse(temp);
                    if (items == null || items.AsArray().Count == 0)
                        continue;

                    // loop through each table row in the template and replace tokens
                    var newRows = new List<HtmlNode>();

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    foreach (JsonObject rec in items.AsArray())
                    {
#pragma warning disable CS8604 // Possible null reference argument.
                        var flat = Squish(rowSrc, root, rec);
#pragma warning restore CS8604 // Possible null reference argument.
                        for (var i = 0; i < tableRows.Count; i++)
                        {
                            var n = ReplaceHtmlTokens(tableRows[i].OuterHtml, templateType, flat, calcVars, stateAuth, _logger);
                            newRows.Add(HtmlNode.CreateNode(n));
                        }
                    }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                    tBody.ChildNodes.Clear();
                    // replace the tBody rows with our new ones
                    foreach (var n in newRows)
                        tBody.ChildNodes.Add(n);
                    clone = clone.Replace(v0, tBody.OuterHtml);

                }

                // process the rest of the document
                //HTML = ReplaceHtmlTokens(clone, templateType, src, calcVars, stateAuth);
                HTML = ReplaceHtmlTokens(clone, templateType, root.AsObject(), calcVars, stateAuth, _logger);
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
            }
            return HTML;
        }

        public static string[] PersonNameVariations(string Name)
        {
            string[] Values = new string[2] { Name, "" }; // returns Name + reversed Name w/without comma

            string temp = Name;
            var Position = Name.IndexOf(',');
            if (Position != -1)
            {
                Values[1] = Name.Substring(Position + 1, temp.Length - Position - 1).Trim() + " " + Name.Substring(0, Position).Trim();
            }
            else
            {
                Values[1] = Name; // failsafe
                try
                {
                    // where to put comma?
                    string resultName = "";
                    string resultLastName = "";
                    string resultSecondLastName = "";

                    var Rex = new Regex(@"[A-ZÁ-ÚÑÜa-zá-úñü]+|([aeodlsz]+\s+)+[A-ZÁ-ÚÑÜ][a-zá-úñü]+", RegexOptions.Singleline);
                    var nameTokens = Rex.Matches(Name);

                    if (nameTokens.Count > 3)
                    {
                        resultName = nameTokens[0].Value + " " + nameTokens[1].Value;
                    }
                    else
                    {
                        resultName = nameTokens[0].Value;
                    }

                    if (nameTokens.Count > 2)
                    {
                        if (nameTokens.Count == 3)
                        {
                            resultLastName = nameTokens[2].Value;
                            resultName = nameTokens[0].Value + " " + nameTokens[1].Value;
                        }
                        else
                        {
                            resultLastName = nameTokens.Reverse().ToArray()[1] + " " + nameTokens.Reverse().First().Value;
                            if (nameTokens.Count > 4)
                                resultSecondLastName += $@" {nameTokens[2].Value}";
                            if (nameTokens.Count > 5)
                                resultSecondLastName += $@" {nameTokens[3].Value}";
                        }
                        Values[1] = resultLastName + ", " + resultName + resultSecondLastName;
                    }
                    else
                    {
                        resultLastName = nameTokens.Reverse().First().Value;
                        Values[1] = resultLastName + ", " + resultName;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            return Values;
        }

        // Copies properties from the child object into the parent object, assigning each the prefix and the _$_ delimiter. 
        // thanks to https://deliverystack.net/2021/12/06/net-6-modify-json-in-memory-with-the-system-text-json-nodes-namespace/
        public static JsonObject Squish(string prefix, JsonNode parent, JsonObject child)
        {
            // get the names of the keys under the container key
            List<string> keys = child.Select(child => child.Key).ToList();
            var obj = parent.AsObject();

            // iterate and move keys from container to root
            foreach (string key in keys.Where(d => !d.Equals("id", StringComparison.CurrentCultureIgnoreCase)))
            {
                string newKey = string.IsNullOrEmpty(prefix) ? key : prefix + "_$_" + key;
                if (obj.ContainsKey(newKey))
                    obj.Remove(newKey);

                var dupe = child[key].Deserialize<JsonNode>();

                obj[newKey] = dupe;
            }

            return obj;
        }

        /// <summary>
        /// Replaces all found tokens with matching values from a JsonObject
        /// </summary>
        /// <param name="html"></param>
        /// <param name="docType"></param>
        /// <param name="src"></param>
        /// <param name="calcVars"></param>
        /// <param name="auth"></param>
        /// <returns></returns>
        private static string ReplaceHtmlTokens(string HTML, NotificationType docType, JsonObject src, CalculatorVariable calcVars, Authority auth, ILogger _logger)
        {
            var regex = new Regex(@"(\{\S+?\})", RegexOptions.Multiline);
            var matched = regex.Matches(HTML);

            for (int count = 0; count < matched.Count; count++)
            {

                var v = matched[count].Value;
                var field = v.Replace("{", "").Replace("}", "");
                JsonNode? value = null;
                if (src.ContainsKey(field))
                    value = src[field].Deserialize<JsonNode>();
                else
                    value = GetCalculatedValue(field, docType, src, calcVars, auth, _logger);

                if (value != null)
                {
                    // ugh...ugly hack for date formatting - JsonObject only returning strings at the moment prob because of loss of fidelity during Squish call
                    bool OK = DateTime.TryParse(value.ToString(), out DateTime test);
                    if (OK && test.Year > 2000 && test.Year < 2068)
                    {
                        // NOTE: This may be off by a day if not converted to CST!
                        HTML = HTML.Replace(v, test.ToString("MM/dd/yyyy"));
                        continue;
                    }
                    else
                    {
                        HTML = HTML.Replace(v, value.ToString());
                    }
                }
                else if (src.ContainsKey(field))
                {
                    HTML = HTML.Replace(v, "");
                }
            }
            return HTML;
        }

        public static async Task<List<CaseFile>> GetBlobLinksAsync(BlobContainerClient _containerClient, string idTag, int id, string fileType)
        {
            List<CaseFile> caseFile = new List<CaseFile>();
            string SQL = $"\"{idTag}\"='{id}'";
            if (!string.IsNullOrEmpty(fileType))
            {
                // TODO: Add file type tag to SQL
                SQL += $" AND \"DocumentType\"='{fileType}'";
            }

            await foreach (var page in _containerClient.FindBlobsByTagsAsync(SQL).AsPages())
            {
                foreach (TaggedBlobItem item in page.Values)
                {
                    var b = _containerClient.GetBlobClient(item.BlobName);
                    var c = await b.GetPropertiesAsync();
                    var tags = await b.GetTagsAsync();
                    var created = DateTime.SpecifyKind(c.Value.CreatedOn.DateTime, DateTimeKind.Utc);
                    var f = new CaseFile { BLOBName = item.BlobName, Tags = tags.Value.Tags, CreatedOn = created };
                    caseFile.Add(f);
                }
            }
            return caseFile;
        }

        public static DateTime? GetTrackingValue(string? tracking, string trackingFieldName)
        {
            if (tracking == null)
                return null;

            var goodies = JsonNode.Parse(tracking);
            if (goodies != null && goodies.AsObject().ContainsKey(trackingFieldName))
            {
                string? s = goodies[trackingFieldName]?.ToString();
                if (s == null)
                    return null;
                if (DateTime.TryParse(goodies[trackingFieldName]!.ToString(), out DateTime r))
                    return r;
            }

            return null;
        }

        // This makes sure we always get a UTC date (now) regardless of which server this runs on.
        // When calling DateTime.Now, local hosting will return something other than UTC
        // which can be problematic depending on circumstances.
        public static DateTime GetCurrentUtcDate()
        {
            /* handle possibility of server's time zone not being in our time zone (CST)
            
            var localZone = TimeZoneInfo.Local;
            var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var today = DateTime.Now;

            if (localZone.BaseUtcOffset == TimeSpan.Zero)
            {
                // needs conversion
                var UTC = DateTime.UtcNow;
                today = utc.AddHours(cstZone.BaseUtcOffset.Hours).AddMinutes(cstZone.BaseUtcOffset.Minutes);
            }
            return today;
            */
            return DateTime.UtcNow;
        }

        public static DateTime? ConvertUnspecifiedToCST(DateTime? src)
        {
            if (!src.HasValue)
                return src;

            if (src.Value.Kind != DateTimeKind.Unspecified)
                return src; // TODO: Investigate if this ever happens!

            var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var result = TimeZoneInfo.ConvertTimeToUtc(src.Value, cstZone);
            return result;
        }

        // For things like dates on outbound notifications or setting Tracking dates, 
        // we need to work in local time. The app is localized to Central Standard Time for now.
        public static DateTime GetCurrentCSTDate2()
        {
            // handle possibility of server's time zone not being in our time zone (CST)
            var localZone = TimeZoneInfo.Local;
            var cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var today = DateTime.Now;

            if (localZone.BaseUtcOffset == TimeSpan.Zero)
            {
                // needs conversion
                var utc = DateTime.UtcNow;
                today = utc.AddHours(cstZone.BaseUtcOffset.Hours).AddMinutes(cstZone.BaseUtcOffset.Minutes);
            }
            return today;
        }

        // Takes a DateTime, treats it as CST (regardless of the Kind property value)
        // and converts it to UTC (including the offset).
        public static DateTime? GetAsCSTDate(DateTime? value, bool keepHours = true)
        {
            if (value == null || !value.HasValue)
                return null;

            try
            {
                TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                value = DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);

                DateTime utcWithCSTOffset = keepHours ? TimeZoneInfo.ConvertTimeToUtc(value.Value, cstZone) : TimeZoneInfo.ConvertTimeToUtc(value.Value.Date, cstZone);

                //Console.WriteLine("The date and time are {0} {1}.",
                //                  cstTime,
                //                  cstZone.IsDaylightSavingTime(cstTime) ?
                //                          cstZone.DaylightName : cstZone.StandardName);
                return utcWithCSTOffset;
            }
            catch (TimeZoneNotFoundException)
            {
                //Console.WriteLine("The registry does not define the Central Standard Time zone.");
                return value;
            }
            catch (InvalidTimeZoneException)
            {
                //Console.WriteLine("Registry data on the Central Standard Time zone has been corrupted.");
                return value;
            }
        }

        /// <summary>
        /// Converts a UTC or Unknown DateTime to CST
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DateTime? ConvertFromUtcToCST(DateTime? value)
        {
            if (!value.HasValue)
                return value;

            if (value.Value.Kind == DateTimeKind.Local)
                return value; // is already localized to the machine settings so just use it

            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var t = value.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value.Value;
            t = TimeZoneInfo.ConvertTimeFromUtc(t, cstZone);
            return t;

        }

        /// <summary>
        /// Returns a property value for a given property name. 
        /// Also matches some pre-defined global and calculated symbols. 
        /// Used to fill out HTML templates with embedded tokens.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="templateType"></param>
        /// <param name="src">An object containing all the necessary properties to power this function.</param>
        /// <param name="calcVars"></param>
        /// <param name="auth"></param>
        /// <returns>Returns a string representation of the value matching propertyName. NOTE: A series of underscore characters is returned for 
        /// missing or otherwise incompatible values. This was done as a safeguard against sending out any false or misleading info
        /// such as an invalid dollar amount or date that might jeopardize ongoing negotiations. Is this following some explicit
        /// "coding standards"? Probably not. Is it a viable safeguard for the organization? Probably so.</returns>        
        public static string GetCalculatedValue(string propertyName, NotificationType templateType, JsonObject src, CalculatorVariable calcVars, Authority auth, ILogger _logger)
        {

            // look for global / statics first
            if (propertyName.Equals("global.today", StringComparison.CurrentCultureIgnoreCase))
            {
                var today = Utilities.GetCurrentCSTDate2();

                return today.ToString("MM/dd/yyyy");

            }

            Object? value = null;

            if (src.ContainsKey(propertyName))
            {
                value = src[propertyName].Deserialize<JsonNode>();
                if (value != null)
                {
                    // ugh...ugly hack for date formatting - JsonObject only returning strings at the moment prob because of loss of fidelity during Squish call
                    bool ok = DateTime.TryParse(value.ToString(), out DateTime test);
                    if (ok && test.Year > 2000 && test.Year < 2068)
                    {
                        return test.ToString("MM/dd/yyyy");
                    }
                }

                return value?.ToString() ?? _BLANK;
            }

            if (propertyName.StartsWith("tracking.", StringComparison.CurrentCultureIgnoreCase))
            {
                var splits = propertyName.Split('.');
                var tracking = src["tracking"].Deserialize<JsonNode>();
                if (tracking == null || string.IsNullOrEmpty(tracking.ToString()) || splits.Length < 2)
                    return _BLANK;

                value = GetTrackingValue(tracking.ToString(), splits[1]);
                if (value == null)
                    return _BLANK;
                if (value.GetType() == typeof(DateTime))
                    return String.Format("{0:MM/dd/yyyy}", value);
                else if (DateTime.TryParse(value.ToString(), out DateTime d))
                    return d.ToString("MM/dd/yyyy");
                return value.ToString() ?? "";  // dumb compiler
            }
            else if (propertyName.StartsWith("NsaTracking.", StringComparison.CurrentCultureIgnoreCase))
            {
                var splits = propertyName.Split('.');
                var tracking = (string?)src["NSATracking"];
                if (tracking == null || string.IsNullOrEmpty(tracking.ToString()) || splits.Length < 2)
                    return _BLANK;

                value = GetTrackingValue(tracking.ToString(), splits[1]);
                if (value == null)
                    return _BLANK;
                if (value.GetType() == typeof(DateTime))
                    return String.Format("{0:MM/dd/yyyy}", value);
                else if (DateTime.TryParse(value.ToString(), out DateTime d))
                    return d.ToString("MM/dd/yyyy");
                return value.ToString() ?? "";  // dumb compiler
            }

            // look for the symbol in other Typed objects

            // CalculatorVariables
            var objProps = calcVars.GetType().GetProperties().Where(d => d.GetIndexParameters().Length == 0).ToArray();
            var objProp = objProps?.FirstOrDefault(d => d.Name.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase));
            if (objProp != null)
            {
                value = objProp.GetValue(calcVars);
            }


            if (objProp != null)
            {
                if (value == null)
                    return _BLANK;

                switch (objProp.PropertyType.Name)
                {
                    case "DateTime":
                        return string.Format("{0:MM/dd/yyyy}", value);

                    default:
                        return value.ToString() ?? "";
                }
            }


            if (propertyName == "benchmarkTitle")
            {
                var b = auth.Benchmarks.FirstOrDefault(v => v.IsDefault);
                return b?.BenchmarkDataset?.Name != null ? b.BenchmarkDataset.Name : "_____";
            }

            if (propertyName == "settlementReduction")
            {
                var amt = (double?)src["NSARequestDiscount"];
                var nsaDiscount = (amt.HasValue && amt.Value > 0) ? amt.Value : calcVars.NSAOfferDiscount;
                return string.Format("{0:0}%", nsaDiscount * 100);
            }

            if (propertyName == "calculatedNSAOffer" || propertyName == "cptCodes_$_calculatedNSAOffer")  // NSA Open Negotiation Settlement Amount
            {
                var prefix = propertyName == "cptCodes_$_calculatedNSAOffer" ? "cptCodes_$_" : "";
                var baseChargeFieldname = prefix + calcVars.NSAOfferBaseValueFieldname;

                if (!src.ContainsKey("NSARequestDiscount") || !src.ContainsKey(baseChargeFieldname))
                    return _BLANK;

                var nsaDiscount = (double?)src["NSARequestDiscount"]; // can this work without the Deserialize?
                nsaDiscount = (nsaDiscount.HasValue && nsaDiscount.Value > 0) ? nsaDiscount.Value : calcVars.NSAOfferDiscount;

                var baseCharge = (double?)src[baseChargeFieldname];
                double? ChargedAmount = 0.0;
                if (propertyName == "cptCodes_$_calculatedNSAOffer")
                {
                    ChargedAmount = (double?)src["cptCodes_$_providerChargeAmount"];
                }
                else
                {
                    ChargedAmount = (double?)src["totalChargedAmount"];
                }

                if (baseCharge.HasValue && baseCharge > 0)
                {
                    if (baseCharge > ChargedAmount)
                    {
                        _logger.LogInformation($"{propertyName} ChargedAmount {baseCharge} is less then providerChargeAmount{ChargedAmount}, so using providerChargeAmount");
                        baseCharge = ChargedAmount;
                    }
                    var disc = 1 - nsaDiscount; // e.g. 1 - .3 = 70% of fh80th
                    var offerAmount = (baseCharge * disc);
                    return String.Format("{0:n}", offerAmount);
                }
            }

            return _BLANK;
        }

        public static async Task<IEnumerable<CalculatorVariable>> GetCalculatorVariablesAsync(ArbitrationDbContext Context, DateTime? AsOf = null)
        {

            if (AsOf == null)
                AsOf = GetCurrentUtcDate();

            var filter = from r in Context.CalculatorVariables.Where(x => x.CreatedOn <= AsOf)
                         group r by r.ServiceLine into op
                         select op.OrderByDescending(x => x.CreatedOn).First();

            var recs = await filter.ToListAsync();
            return recs;
        }

        public static string GetDocumentTemplate(NotificationType docType, Payor payor)
        {
            if (string.IsNullOrEmpty(payor.JSON))
                {
                return "";
            }

            // Use JsonNode parser to fetch template
            var coll = JsonSerializer.Deserialize<DocumentTemplateCollection>(payor.JSON);
            if (coll?.Templates?.Count() > 0)
            {
                var t = coll.Templates.FirstOrDefault(d => d.NotificationType == docType);
                return t == null ? "" : t.HTML;
            }
            return "";
        }

        public static List<DocumentTemplate> GetDocumentTemplates(NotificationType docType, Payor payor)
        {
            if (string.IsNullOrEmpty(payor.JSON))
                return new List<DocumentTemplate>();

            // Use JsonNode parser to fetch template
            var coll = JsonSerializer.Deserialize<DocumentTemplateCollection>(payor.JSON);
            return (coll?.Templates?.Count() > 0) ? coll.Templates.Where(d => d.NotificationType == docType).ToList() : new List<DocumentTemplate>();
        }

        /// <summary>
        /// Fetches the top-level Parent payor given the name of any of its aliases.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="name"></param>
        /// <param name="includeGroups"></param>
        /// <returns></returns>
        public static async Task<Payor?> GetPayorByAliasAsync(ArbitrationDbContext context, string name, bool includeGroups = true)
        {
            Payor? payor = await context.Payors.FirstOrDefaultAsync(d => d.Name == name);

            if (payor == null)
                return payor;

            if (payor.Id != payor.ParentId)
                payor = await context.Payors.FirstOrDefaultAsync(d => d.Id == payor.ParentId);

            if (payor == null)
                return payor;

            if (includeGroups)
                payor.PayorGroups = await context.PayorGroups.Where(d => d.PayorId == payor.Id).ToListAsync();

            return payor;
        }

        public static double GetDefaultServiceLineDiscount(Authority authority, CalculatorVariable calcs)
        {
            // Explicit NSA path
            if (authority.Key.ToLower() == "nsa")
            {
                return calcs.NSAOfferDiscount;
            }
            else
            {
                // TODO: Figure out how to create default 
                return 0; // calcs.ChargesCapDiscount ??
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IEnumerable<T> ReadGenericCSVRecord<T>(IFormFile file, out string message)
        {
            // Use CSV Library to handle EHR Imports
            message = "";
            T[] records = new T[] { };
            CsvReader? csvReader = null;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                //PrepareHeaderForMatch = args => args.Header.ToLower(),
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null,
                HasHeaderRecord = true,
                HeaderValidated = null, //args => Console.Write(string.Join("', '", args.InvalidHeaders?.FirstOrDefault()?.Names)),
                MissingFieldFound = null, //args => Console.WriteLine($"Field with names ['{string.Join("', '", args.HeaderNames)}'] at index '{args.Index}' was not found. ")
            };

            using (var reader = new StreamReader(file.OpenReadStream()))
            using (csvReader = new CsvReader(reader, config))
            {
                try
                {
                    records = csvReader.GetRecords<T>().ToArray();
                }
                catch (CsvHelperException ex)
                {
                    message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                }
                catch (Exception ex)
                {
                    message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                }
            }

            return records; //.Where(d => !string.IsNullOrEmpty(d.AuthorityCaseId) && d.AuthorityId > 0 && d.ArbitrationCaseId > 0 && !string.IsNullOrEmpty(d.ClaimCPTCode));
        }

        /// <summary>
        /// Sets the value of a tracking field stored in a custom tracking schema and then updates defined calculations.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trackingDetails"></param>
        /// <param name="trackingJson"></param>
        /// <param name="trackingFieldName"></param>
        /// <returns>JSON formatted string with the update value and calculated values</returns>
        public static string SetTrackingValue(DateTime? value, List<AuthorityTrackingDetail> trackingDetails, string trackingJson, string trackingFieldName, ArbitrationCase arb)
        {
            var msg = new ObjectChangeResult();
            msg.WasChanged = false;

            if (string.IsNullOrEmpty(trackingJson) || trackingDetails.Count == 0)
                return trackingJson;

            try
            {
                var goodies = JsonNode.Parse(trackingJson);

                if (goodies != null && goodies.AsObject().ContainsKey(trackingFieldName))
                {
                    var t = goodies[trackingFieldName];
                    if (t == null)
                    {
                        if (value.HasValue)
                        {
                            goodies[trackingFieldName] = value;
                            UpdateTrackingCalculations(goodies, trackingDetails, arb);
                            return goodies.ToJsonString();
                        }
                    }
                    else if (!value.HasValue || !t.ToString().Equals(value.Value.ToString("yyyy-MM-ddTHH:mm:ss"), StringComparison.CurrentCultureIgnoreCase))
                    {
                        goodies![trackingFieldName] = value;
                        UpdateTrackingCalculations(goodies, trackingDetails, arb);
                        return goodies.ToJsonString();
                    }
                }
            }
            catch
            {
                // nothing to do
            }
            return trackingJson;
        }

        public static JsonNode? ValidateAuthorityScheme(string json, List<AuthorityTrackingDetail> trackingDetails)
        {
            JsonNode? goodies = null;
            if (string.IsNullOrEmpty(json) || trackingDetails.Count == 0)
                return goodies;

            try
            {
                goodies = JsonNode.Parse(json);
                if (goodies == null)
                    return goodies;

                // update schema
                var obj = goodies.AsObject();
                var props = obj.Select(d => d.Key).ToArray();

                // remove any invalid properties
                foreach (var k in props)
                {
                    if (trackingDetails.FirstOrDefault(g => g.TrackingFieldName == k) == null)
                    {
                        obj.Remove(k);
                    }
                }

                // add any missing properties
                foreach (var t in trackingDetails.Where(d => !d.IsDeleted))
                {
                    if (props.FirstOrDefault(g => g.Equals(t.TrackingFieldName, StringComparison.CurrentCultureIgnoreCase)) == null)
                    {
                        if (t.TrackingFieldType.ToLower() == "date")
                        {
                            obj[t.TrackingFieldName] = null;
                        }
                        else if (t.TrackingFieldType.ToLower() == "number")
                        {
                            obj[t.TrackingFieldName] = 0;
                        }
                    }
                    else
                    {
                        // bug fix hack to prevent UTC midnight from rolling back to EST/CST previous day
                        var x = obj[t.TrackingFieldName];
                        if (x != null && DateTime.TryParse(x.AsValue().ToString(), out DateTime result))
                        {
                            obj[t.TrackingFieldName] = Utilities.EnsureUtcMinimumHours(result);
                        }
                    }

                }

                return goodies;
            }
            catch (Exception ex)
            {
                // nothing to do
                Console.WriteLine(ex.Message);
            }

            return null; // discard any partial modifications
        }

        public static DateTime? EnsureUtcMinimumHours(DateTime? result)
        {
            if (!result.HasValue)
                return result;

            if (result.Value.Kind == DateTimeKind.Utc && result.Value.Hour < 6)
                return result.Value.AddHours(6 - result.Value.Hour); // safety net to ensure the date lands firmly inside of the UTC "date string" and doesn't slide back by a day due to the hour being off for us USA blokes

            if (result.Value.Kind == DateTimeKind.Local && TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).Hours == 0 && result.Value.Hour < 6)
                return result.Value.AddHours(6 - result.Value.Hour); // safety net to ensure the date lands firmly inside of the UTC "date string" and doesn't slide back by a day due to the hour being off for us USA blokes

            if (result.Value.Kind == DateTimeKind.Unspecified)
                return Utilities.ConvertUnspecifiedToCST(result); // treating a UTC date (that isn't specified as UTC) as CST will actually add the 5 hours needed

            return result;

        }

        /// <summary>
        /// Recalculates the Tracking values according to the configured rule set. Also updates any suspicious UTC or Unknown dates to be at least 6 AM.
        /// This is to address any issues that may arise when an early-morning UTC date causes the calendar "day" value to be interpreted as the day prior in the USA.
        /// </summary>
        /// <param name="TrackingNode"></param>
        /// <param name="TrackingDetailConfigList"></param>
        /// <param name="TrackedRecord"></param>
        public static void UpdateTrackingCalculations(JsonNode TrackingNode, List<AuthorityTrackingDetail> TrackingDetailConfigList, Object TrackedRecord)
        {
            if (TrackingDetailConfigList.Count(d => !d.IsDeleted) == 0)
                return;

            bool changesFound = true; // loop flag
            int iterations = 0;  // failsafe counter
            var obj = TrackingNode.AsObject();

            while (changesFound && iterations < 10)
            {
                iterations++;
                changesFound = false;
                foreach (var d in TrackingDetailConfigList.Where(v => !v.IsDeleted && !string.IsNullOrEmpty(v.ReferenceFieldName) && !string.IsNullOrEmpty(v.TrackingFieldName)))
                {
                    // NOTE: Calculated fields are denoted by the presence of a value in ReferenceFieldName.
                    // This presents a challenge in terms of whether calculated values should be saved/memorialized.
                    // Currently, the system does persist new calculated values. However, there is obviously a strong
                    // case to be made that this is flawed logic and that calculated deadlines, for instance, should
                    // immediately change if an underlying value is modified. The counter-argument to this is that
                    // there could be an active (ongoing) case on file with an Authority that was begun
                    // using an older value for, say, EOBDate. If another EOB comes in and is newer, we may not want
                    // to modify an existing deadline for the active case but only use this value for calculating
                    // deadlines on new cases. This is because an individual CPT code may have a different expiration
                    // time line than a newly-approved one on a later EOB.
                    var referenceProperty = TrackedRecord.GetType().GetProperty(d.ReferenceFieldName);

                    switch (d.TrackingFieldType)
                    {
                        case "Date":
                            DateTime? trackingValue = null;
                            // Try to find the value in the existing tracking object (allows overriding a field of the same name in the ArbitrationCase record if necessary)
                            var isNull = obj![d.TrackingFieldName] == null;
                            if (!isNull && obj![d.TrackingFieldName]!.AsValue().TryGetValue<DateTime>(out var tmp))
                            {
                                trackingValue = tmp;
                            }
                            else if (!isNull && obj![d.TrackingFieldName]!.AsValue().TryGetValue<String>(out var s1) && DateTime.TryParse(s1, out var p))
                            {
                                trackingValue = p;
                            }

                            /* If the value stored in this field comes from a calculation, recalculate it */

                            // first check the tracking object itself for a reference field (i.e. let calculations refer to other properties in the tracking object)
                            if (obj.ContainsKey(d.ReferenceFieldName))
                            {
                                DateTime? oldValue = null;
                                isNull = obj![d.ReferenceFieldName] == null;
                                if (!isNull)
                                {
                                    if (obj![d.ReferenceFieldName]!.AsValue().TryGetValue<DateTime>(out var tmp2))
                                    {
                                        oldValue = tmp2;
                                    }
                                    else if (obj![d.ReferenceFieldName]!.AsValue().TryGetValue<String>(out var s2) && DateTime.TryParse(s2, out var p2))
                                    {
                                        oldValue = p2;
                                    }

                                    if (oldValue != null)
                                    {
                                        int units = Convert.ToInt32(d.UnitsFromReference);
                                        oldValue = Utilities.EnsureUtcMinimumHours(oldValue);
                                        var newValue = d.UnitsType.Equals("days", StringComparison.CurrentCultureIgnoreCase) ? oldValue!.Value.AddDays(units) : oldValue!.Value.AddWorkDays(units);

                                        if (!trackingValue.HasValue || !newValue.Equals(trackingValue.Value))
                                        {
                                            obj[d.TrackingFieldName] = newValue.ToUniversalTime();
                                            changesFound = true;
                                        }
                                    }
                                }
                                else if (trackingValue.HasValue)
                                {
                                    // the reference field is null so the calculation must be null as well
                                    obj[d.TrackingFieldName] = null;
                                    changesFound = true;
                                }
                            }

                            // check if the calculation requires a value from the "parent" object aka ArbitrationCase
                            else if (referenceProperty != null && referenceProperty.PropertyType.ToString().Contains("DateTime"))
                            {
                                var referenceValue = (DateTime?)referenceProperty.GetValue(TrackedRecord);
                                referenceValue = Utilities.EnsureUtcMinimumHours(referenceValue);

                                int units = Convert.ToInt32(d.UnitsFromReference);
                                if (referenceValue.HasValue)
                                {
                                    var newTrackingValue = d.UnitsType.Equals("days", StringComparison.CurrentCultureIgnoreCase) ? referenceValue.Value.AddDays(units) : referenceValue.Value.AddWorkDays(units);

                                    // Sets or adds the value on the tracking object
                                    if (!trackingValue.HasValue || !newTrackingValue.Equals(trackingValue))
                                    {
                                        obj[d.TrackingFieldName] = newTrackingValue.ToUniversalTime();
                                        changesFound = true;
                                    }
                                }
                                else if (trackingValue.HasValue)
                                {
                                    // the reference field is null so the calculation must be null as well
                                    obj[d.TrackingFieldName] = null;
                                    changesFound = true;
                                }
                            }
                            else
                            {
                                if (trackingValue.HasValue)
                                {
                                    obj[d.TrackingFieldName] = null;
                                    changesFound = true;
                                }
                            }
                            break;

                        case "Number":
                            double calcValue = 0.0;
                            int units2 = Convert.ToInt32(d.UnitsFromReference);
                            double? refValue = (double?)obj[d.ReferenceFieldName];

                            if (obj.ContainsKey(d.ReferenceFieldName) && refValue.HasValue && units2 != 0)
                            {
                                calcValue = refValue.Value + units2;
                            }
                            double? tvalue = (double?)obj[d.TrackingFieldName];
                            if (!tvalue.HasValue || tvalue.Value != calcValue)
                            {
                                obj[d.TrackingFieldName] = calcValue;
                                changesFound = true;
                            }
                            break;

                        default:
                            if (obj.ContainsKey(d.TrackingFieldName))
                            {
                                if (obj.ContainsKey(d.ReferenceFieldName))
                                {
                                    obj[d.TrackingFieldName] = obj[d.ReferenceFieldName];
                                }
                                else
                                {
                                    obj[d.TrackingFieldName] = string.Empty;
                                }
                            }
                            break;
                    }
                }
            }
            return;
        }

        /* public static void UpdateTrackingCalculations(JsonNode goodies, List<AuthorityTrackingDetail> t, ArbitrationCase arbitCase)
        public static void UpdateTrackingCalculationsDeprecated(JsonNode goodies, List<AuthorityTrackingDetail> t, ArbitrationCase arbitCase)
        {
            if (t.Count() == 0)
                return;

            var obj = goodies.AsObject();

            bool changesFound = true; // loop flag
            int iterations = 0;  // failsafe counter

            while (changesFound && iterations < 10)
            {
                iterations++;
                changesFound = false;
                foreach (var d in t.Where(v => !string.IsNullOrEmpty(v.ReferenceFieldName) && !string.IsNullOrEmpty(v.TrackingFieldName)))
                {
                    var property = arb.GetType().GetProperty(d.ReferenceFieldName);

                    switch (d.TrackingFieldType)
                    {
                        case "Date":
                            DateTime? trackingValue = null;
                            try
                            {
                                trackingValue = (DateTime?)obj![d.TrackingFieldName];
                            }
                            catch
                            {
                                var test = (string?)obj![d.TrackingFieldName];
                                if (!string.IsNullOrEmpty(test) && DateTime.TryParse(test, out DateTime converted)) //? (DateTime?)obj![d.TrackingFieldName];
                                    trackingValue = converted;
                            }

                            trackingValue = Utilities.GetAsCSTDate(trackingValue); // "before" value 

                            if (obj.ContainsKey(d.ReferenceFieldName))
                            {
                                DateTime? oldValue = null;
                                try
                                {
                                    oldValue = (DateTime?)obj![d.ReferenceFieldName];
                                }
                                catch
                                {
                                    var test = (string?)goodies![d.ReferenceFieldName];
                                    if (!string.IsNullOrEmpty(test) && DateTime.TryParse(test, out DateTime converted))
                                        oldValue = converted;
                                }

                                oldValue = Utilities.GetAsCSTDate(oldValue);

                                int units = Convert.ToInt32(d.UnitsFromReference);
                                if (oldValue.HasValue && units != 0)
                                {
                                    var newValue = d.UnitsType.Equals("days", StringComparison.CurrentCultureIgnoreCase) ? oldValue.Value.AddDays(units) : oldValue.Value.AddWorkDays(units);

                                    if (!trackingValue.HasValue || !newValue.Equals(trackingValue))
                                    {
                                        obj[d.TrackingFieldName] = newValue;
                                        changesFound = true;
                                    }
                                }
                            } else if(property != null && property.PropertyType.Name == "DateTime")
                            {
                                var oldValue = Utilities.GetAsCSTDate((DateTime?)property.GetValue(arbitCase));

                                int units = Convert.ToInt32(d.UnitsFromReference);
                                if (oldValue.HasValue && units != 0)
                                {
                                    var newValue = d.UnitsType.Equals("days", StringComparison.CurrentCultureIgnoreCase) ? oldValue.Value.AddDays(units) : oldValue.Value.AddWorkDays(units);

                                    if (!trackingValue.HasValue || !newValue.Equals(trackingValue))
                                    {
                                        obj[d.TrackingFieldName] = newValue;
                                        changesFound = true;
                                    }
                                }
                            }
                            else
                            {
                                if (trackingValue.HasValue)
                                {
                                    obj[d.TrackingFieldName] = null;
                                    changesFound = true;
                                }
                            }
                            break;

                        case "Number":
                            double calcValue = 0.0;
                            int units2 = Convert.ToInt32(d.UnitsFromReference);
                            double? refValue = (double?)obj[d.ReferenceFieldName];

                            if (obj.ContainsKey(d.ReferenceFieldName) && refValue.HasValue && units2 != 0)
                            {
                                calcValue = refValue.Value + units2;
                            }
                            double? tValue = (double?)obj[d.TrackingFieldName];
                            if (!tvalue.HasValue || tvalue.Value != calcValue)
                            {
                                obj[d.TrackingFieldName] = calcValue;
                                changesFound = true;
                            }
                            break;

                        default:
                            if (obj.ContainsKey(d.TrackingFieldName))
                            {
                                if (obj.ContainsKey(d.ReferenceFieldName))
                                {
                                    obj[d.TrackingFieldName] = obj[d.ReferenceFieldName];
                                }
                                else
                                {
                                    obj[d.TrackingFieldName] = string.Empty;
                                }
                            }
                            break;
                    }
                }
            }
            return;
        }
        */

        /// <summary>
        /// Uses Authority-specific rules to update the Gross and Net settlement values if they are new or match previous values. 
        /// Otherwise, a messages is returned.
        /// </summary>
        /// <param name="auth"></param>
        /// <param name="settlement"></param>
        /// <param name="arb"></param>
        /// <returns>An empty string if the settlement values are new or unchanged from existing values, or a message detailing the conflict.</returns>
        public static string CalculateAuthoritySettlement(Authority auth, CaseSettlement settlement, ArbitrationCase arb)
     {
            // CaseSettlement objects represent Arbit's view of a settlement
            // CaseSettlementDetails are supplied by the Authority and represent the settlement from their perspective which can
            // sometimes change / get tampered with by Payors even after a settlement is "decided"

            var caseSettlementDetailDetail = settlement.CaseSettlementDetails.FirstOrDefault();
            if (caseSettlementDetailDetail == null)
                {
                return "No CaseSettlementDetails object available";
            }  // TODO: this may be wonky - I think the calling function is responsible for making sure one exists...?

            string settlementWinner = settlement.PrevailingParty;
            string authorityWinner = string.IsNullOrEmpty(caseSettlementDetailDetail.PrevailingParty) ? "Informal" : caseSettlementDetailDetail.PrevailingParty;

            if (!string.IsNullOrEmpty(settlementWinner) && !settlementWinner.Equals(authorityWinner, StringComparison.CurrentCultureIgnoreCase))
                return $@"CaseSettlement already has a PrevailingParty value ({settlement.PrevailingParty})";

            if (caseSettlementDetailDetail.TotalSettlementAmount == 0 && authorityWinner == "Informal")
                {
                return "";
            } // not settled yet

            if (auth.Key.Equals("TX", StringComparison.CurrentCultureIgnoreCase))
            {
                if (caseSettlementDetailDetail.TotalSettlementAmount != 0)
                {
                    if (authorityWinner != "Informal")
                    {
                        return "TX settlement record rule violation: Cannot have TotalSettlementAmount with a PrevailingParty other than 'Informal'";
                    }
                    if (settlement.GrossSettlementAmount != 0 
                        && settlement.GrossSettlementAmount != caseSettlementDetailDetail.TotalSettlementAmount 
                        && Math.Abs(settlement.GrossSettlementAmount - caseSettlementDetailDetail.TotalSettlementAmount) > 1)
                    {
                        return $@"GrossSettlementAmount:{settlement.GrossSettlementAmount}; TotalSettlementAmount: {caseSettlementDetailDetail.TotalSettlementAmount} changed from previous update";
                    }
                    settlement.GrossSettlementAmount = caseSettlementDetailDetail.TotalSettlementAmount;
                    settlement.ArbitrationDecisionDate = arb.InformalTeleconferenceDate;
                    settlement.ArbitratorReportSubmissionDate = null;
                    settlement.PartiesAwardNotificationDate = null;
                }
                else if (caseSettlementDetailDetail.PrevailingParty.ToLower() == "health plan")
                {
                    if (settlement.GrossSettlementAmount != 0 && settlement.GrossSettlementAmount != caseSettlementDetailDetail.PayorFinalOfferAmount)
                        {
                        return $@"GrossSettlementAmount:{settlement.GrossSettlementAmount}; PayorFinalOfferAmount: {caseSettlementDetailDetail.PayorFinalOfferAmount} changed from previous update";
                    }

                    settlement.GrossSettlementAmount = caseSettlementDetailDetail.PayorFinalOfferAmount;
                    settlement.ArbitrationDecisionDate = caseSettlementDetailDetail.ArbitrationDecisionDate;
                    settlement.ArbitratorReportSubmissionDate = caseSettlementDetailDetail.ArbitratorReportSubmissionDate;
                    settlement.PartiesAwardNotificationDate = caseSettlementDetailDetail.PartiesAwardNotificationDate;
                }
                else if (caseSettlementDetailDetail.PrevailingParty.ToLower() == "provider")
                {
                    if (settlement.GrossSettlementAmount != 0 && settlement.GrossSettlementAmount != caseSettlementDetailDetail.ProviderFinalOfferAmount)
                        {
                        return $@"GrossSettlementAmount:{settlement.GrossSettlementAmount}; ProviderFinalOfferAmount: {caseSettlementDetailDetail.ProviderFinalOfferAmount} changed from previous update";
                    }

                    settlement.GrossSettlementAmount = caseSettlementDetailDetail.ProviderFinalOfferAmount;
                    settlement.ArbitrationDecisionDate = caseSettlementDetailDetail.ArbitrationDecisionDate;
                    settlement.ArbitratorReportSubmissionDate = caseSettlementDetailDetail.ArbitratorReportSubmissionDate;
                    settlement.PartiesAwardNotificationDate = caseSettlementDetailDetail.PartiesAwardNotificationDate;
                }
                else
                {
                    return $@"Unexpected condition; GrossSettlementAmount:{settlement.GrossSettlementAmount}; ProviderFinalOfferAmount: {caseSettlementDetailDetail.ProviderFinalOfferAmount}; PayorFinalOfferAmount: {caseSettlementDetailDetail.PayorFinalOfferAmount}; TotalSettlementAmount: {caseSettlementDetailDetail.TotalSettlementAmount} ";
                }
                settlement.PrevailingParty = authorityWinner;
                //settlement.NetSettlementAmount = settlement.GrossSettlementAmount - arb.TotalPaidAmount; // this cannot be kept up to day since payments come in all the time - removed NetSettlementAmount for now
            }

            return "";
        }

        /// <summary>
        /// Basic validation to ensure only one particular type of Notification can be queued up at a time. This should expand over time to be the single point of Notification validation, regardless of Notification type.
        /// </summary>
        /// <param name="Notification"></param>
        /// <param name="ArbCase"></param>
        /// <param name="PendingNotifications"></param>
        /// <param name="Customers"></param>
        /// <param name="Payors"></param>
        /// <param name="CalculatorVariables"></param>
        /// <param name="Authorities"></param>
        /// <returns></returns>
        public static string ValidateNotificationRequest(Notification Notification, ArbitrationCase? ArbCase, IEnumerable<Notification> PendingNotifications, IEnumerable<Customer> Customers, List<Payor> Payors, IEnumerable<CalculatorVariable> CalculatorVariables, IEnumerable<Authority> Authorities)
        {
            if (Notification.Id != 0)
                {
                return "New Notifications cannot have a non-zero id value!";
            }

            if (Notification.ArbitrationCaseId < 1)
                {
                return "Invalid ArbitrationCaseId";
            }

            if (ArbCase == null || ArbCase.IsDeleted)
                {
                return "Missing or deleted ArbitrationCase.";
            }

            if (string.IsNullOrEmpty(ArbCase.Customer))
                {
                return "ArbitrationCase contains an empty Customer value.";
            }

            if (string.IsNullOrEmpty(ArbCase.PayorClaimNumber))
                {
                return "ArbitrationCase contains an empty PayorClaimNumber value.";
            }

            if (Notification.IsDeleted)
                {
                return "Notification object is marked as deleted.";
            }

            var Pending = PendingNotifications.FirstOrDefault(d => d.ArbitrationCaseId == Notification.ArbitrationCaseId && d.NotificationType == Notification.NotificationType);
            if (Pending != null)
                return $@"An unsent {Enum.GetName<NotificationType>(Notification.NotificationType)} Notification is already queued up for the ArbitrationCase record.";

            if (ArbCase.PayorId < 1)
                {
                return "No Payor record is associated with the ArbitrationCase. No one to notify!";
            }

            var Payor = Payors.FirstOrDefault(d => d.Id == ArbCase.PayorId);
            if (Payor == null)
                {
                return "Unable to locate the related Payor record.";
            }

            if (Payor.Name != ArbCase.Payor)
                {
                return "The PayorId and Payor name do not appear to point to the same entity. Re-save the ArbitrationCase record to fix the issue.";
            }

            var template = Utilities.GetDocumentTemplate(Notification.NotificationType, Payor);
            if (string.IsNullOrEmpty(template))
                {
                return "No template found for Payor";
            }

            if (string.IsNullOrEmpty(Payor.NSARequestEmail))
                {
                return "Invalid or missing NSARequestEmail for Payor.";
            }

            var customer = Customers.FirstOrDefault(d => d.Name.Equals(ArbCase.Customer, StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(d.JSON));
            if (customer == null)
                {
                return "Invalid Customer value";
            }

            if (Notification.NotificationType == NotificationType.NSANegotiationRequest)
            {
                JsonNode? node = JsonNode.Parse(customer.JSON);
                string nsaReplyTo = node == null || node["NSAReplyTo"] == null ? "" : node["NSAReplyTo"]!.ToString();
                if (string.IsNullOrEmpty(nsaReplyTo))
                    {
                    return "Customer is not configured properly. NSAReplyTo is missing!";
                }
            }

            // get latest CalculatorVariable settings for the case's service line
            var filter = from r in CalculatorVariables.Where(x => x.ServiceLine == ArbCase.ServiceLine)
                         group r by r.ServiceLine into op
                         select op.OrderByDescending(x => x.CreatedOn).First();

            var calcVars = filter.FirstOrDefault();
            if (calcVars == null)
                return $@"Invalid or missing global app variables for ServiceLine {ArbCase.ServiceLine}.";
            if (Notification.NotificationType == NotificationType.NSANegotiationRequest && string.IsNullOrEmpty(calcVars.NSAOfferBaseValueFieldname))
                return $@"NSA Offer Base Value Field is not configured in the global app variables for ServiceLine {ArbCase.ServiceLine}.";

            var nsaAuth = Authorities.FirstOrDefault(h => h.Key == "nsa");
            var auth = Authorities.FirstOrDefault(h => h.Key == ArbCase.Authority);
            if (auth == null || nsaAuth == null)
                {
                return "Invalid or missing Authority used for calculating benchmarks and deadlines";
            }

            return "";
        }

        /// <summary>
        /// Validates the NSA and (State) Authority Tracking objects in one call, updating the NSATracking and TrackingDetails fields if necessary.
        /// </summary>
        /// <param name="arb">ArbitrationCase object</param>
        /// <param name="auth">Local Authority object</param>
        /// <param name="nsa">NSA Authority object</param>
        /// <returns></returns>
        public static bool ValidateTracking(ArbitrationCase arb, Authority? nsa = null, Authority? auth = null, bool activeOnly = true)
        {
            bool trackingUpdated = false;

            if (nsa != null)
            {
                // Q: Why don't we just look up the NSA Authority here?
                // A: Speed during import.

                // TODO: Should we superclass Authority to NSAAuthority so we can detect compile-time errors and ensure passing the correct class?
                // I'm sure I'm missing something obvious here.
                if (nsa.Key.ToLower() != "nsa")
                    throw new Exception("Wrong Authority passed to NSA parameter!");

                if (nsa.TrackingDetails.Count > 0)
                {
                    string before = arb.NSATracking;
                    var jnode = Utilities.ValidateAuthorityScheme(arb.NSATracking, nsa.TrackingDetails);
                    if (jnode != null)
                    {
                        Utilities.UpdateTrackingCalculations(jnode, nsa.TrackingDetails, arb);
                        trackingUpdated = (before != jnode.ToJsonString());
                        arb.NSATracking = jnode.ToJsonString();
                    }
                }
            }

            if (auth != null && arb.Tracking != null && auth.TrackingDetails.Count > 0)
            {
                if (auth.Key.ToLower() == "nsa")
                    throw new Exception("Wrong Authority passed to parameter!");

                string before = arb.Tracking.TrackingValues;
                var jnode = Utilities.ValidateAuthorityScheme(arb.Tracking.TrackingValues, auth.TrackingDetails);
                if (jnode != null)
                {
                    Utilities.UpdateTrackingCalculations(jnode, auth.TrackingDetails, arb);
                    trackingUpdated = trackingUpdated | (before != jnode.ToJsonString());
                    arb.Tracking.TrackingValues = jnode.ToJsonString();
                }
            }
            return trackingUpdated;
        }

        public static List<string> ValidateArbitrationCaseForUI(ArbitrationDbContext _context, ArbitrationCase caseRecord, bool skipDOBCheck, Authority nsaAuthority, Authority? stateAuthority, bool isUpdate, AppUser? caller = null)
        {
            var messages = new List<string>();

            if (!isUpdate && caseRecord.Id != 0)
                messages.Add("createArbitrationCase(): Unexpected! CaseRecord ID is not zero!");

            if (!isUpdate && caseRecord.IsDeleted)
                messages.Add("Cannot create a deleted case!");

            if (string.IsNullOrEmpty(caseRecord.Customer))
                messages.Add("Customer is a required value!");

            if (!skipDOBCheck && !caseRecord.DOB.HasValue)
                messages.Add("DOB is a required value!");

            if (string.IsNullOrEmpty(caseRecord.ProviderNPI))
                messages.Add("ProviderNPI is a required value!");

            var customer = _context.Customers.Include(d => d.Entities).FirstOrDefaultAsync(d => d.Name == caseRecord.Customer).Result;

            if (customer == null)
                messages.Add("Invalid Customer value");

            if (!string.IsNullOrEmpty(caseRecord.Authority) && stateAuthority == null)
                messages.Add($@"Invalid Authority code '{caseRecord.Authority}'");

            if (caseRecord.NSARequestDiscount > .99 || caseRecord.NSARequestDiscount < 0)
                messages.Add($@"Invalid NSARequest Discount: {caseRecord.NSARequestDiscount}");

            // Authority "nsa" is handled via dedicated columns
            if (caseRecord.Authority.Equals("nsa", StringComparison.CurrentCultureIgnoreCase))
                messages.Add("Invalid Authority code 'nsa'. Use the NSA dedicated columns instead.");

            // check that Authority status exists only if an Authority was designated on the record
            if (stateAuthority != null)
            {
                if (!stateAuthority.Key.Equals(caseRecord.Authority, StringComparison.CurrentCultureIgnoreCase))
                    messages.Add("Authority object mismatch with provided Authority value!"); // this shouldn't be possible but just in case

                // if providing an Authority, a valid status value is required
                var statuses = stateAuthority.StatusValues.Split(new char[] { ';', ',' }).Select(s => s.Trim().ToLower()).ToArray();
                if (!string.IsNullOrEmpty(caseRecord.AuthorityStatus) && !statuses.Contains(caseRecord.AuthorityStatus.ToLower()))
                    messages.Add($@"Invalid AuthorityStatus ('{caseRecord.AuthorityStatus}')");

                // "Not Submitted" is a magic string - it should be present as a valid choice on all Authority records
                //if ((caller == null || caller.IsState) && !string.IsNullOrEmpty(au.Website) && string.IsNullOrEmpty(caseRecord.AuthorityCaseId) && caseRecord.AuthorityStatus != "Not Submitted")
                //    return $@"AuthorityStatus must be 'Not Submitted' when there is no AuthorityCaseId value and the Authority provides a web portal.";

                if (!string.IsNullOrEmpty(caseRecord.AuthorityCaseId) && caseRecord.AuthorityStatus == "Not Submitted")
                    messages.Add($@"AuthorityStatus cannot be 'Not Submitted' when the AuthorityCaseId value is provided.");
            }
            if (messages.Count > 0)
            {
                return messages;
            }

            try
            {
                var user = caller == null ? new AppUser { Email = "system", Id = -1, IsActive = true } : caller;

                // handle NSA setup explicitly - there's dedicated app logic for it in various places that depend on the magic string in NSA_PENDING 
                const string NSA_PENDING = "Pending NSA Negotiation Request";
                var statuses = nsaAuthority.StatusValues.Split(new char[] { ';', ',' }).Select(s => s.Trim().ToLower()).ToArray();
                if (!statuses.Contains(NSA_PENDING.ToLower()))
                {
                    // correct this problem to head off other issues - this status value is a necessary convention
                    nsaAuthority.StatusValues = $@"{NSA_PENDING};" + nsaAuthority.StatusValues;
                    _context.SaveChanges();
                    statuses = nsaAuthority.StatusValues.Split(new char[] { ';', ',' }).Select(s => s.Trim().ToLower()).ToArray();
                }

                // NOTE this piece of logic that will overwrite the NSAStatus value if an NSACaseId is not provided for new records.
                // This logic may be faulty. Continue to observe.
                if (!isUpdate && string.IsNullOrEmpty(caseRecord.NSACaseId) || string.IsNullOrEmpty(caseRecord.NSAStatus))
                    caseRecord.NSAStatus = NSA_PENDING;

                // NOTE: If the business rules change such that authorityCaseId is no longer required, regardless of status,
                // a systematic search will need to be undertaken to find the various places this logic is coded in.
                // It could be that this will need to be configurable on an authority-by-authority basis but this
                // could get very, very messy as we start to look at things like Notifications, templates, external queries, etc.
                if (!statuses.Contains(caseRecord.NSAStatus.ToLower()))
                    messages.Add($@"Invalid NSAStatus ('{caseRecord.NSAStatus}')");


                //------------------- Payor determination starts here -----------------------------------------------//
                // verify child record values - using the Payor name to supersede the PayorId is consistent with other areas of the application (this is a convention)
                // don't care about log here to just dummy StringBuilder supplied
                var payor = _context.Payors.FirstOrDefault(payor => payor.Id == caseRecord.PayorId);

                if (payor == null) // see we could not add new payor
                {
                    messages.Add("Unable to determine Payor from supplied information."); // again, garbage data. not going to log this as some sort of master data issue
                                                                                          // verify that the EntityNPI is not on an Exclusion list for this Payor
                }

                caseRecord.PayorId = payor!.Id; // link to payor record
                caseRecord.Payor = payor.Name; // standardize the spelling
                //------------------- Payor determination ends here -----------------------------------------------//

                // check for payor+entity exclusion (can happen if Provider/Entity signs away their right to arbitrate, for instance)
                if (!string.IsNullOrEmpty(caseRecord.EntityNPI))
                {
                    if (payor.GetExcludedEntities().FirstOrDefault(d => d.NPINumber.Equals(caseRecord.EntityNPI, StringComparison.CurrentCultureIgnoreCase)) != null)
                    {
                        messages.Add($@"Entity '{caseRecord.Entity}' ({caseRecord.EntityNPI}) is on an exclusion list for Payor ({payor.Name}). Claim not added/updated.");
                    }

                    // verify EntityNPI against Customer Entity list
                    if (customer!.Entities.FirstOrDefault(d => d.NPINumber == caseRecord.EntityNPI) == null)
                    {
                        messages.Add($@"EntityNPI {caseRecord.EntityNPI} does not match an Entity for Customer {customer.Name}. Claim not added/updated..");

                    }
                }
                else if (!string.IsNullOrEmpty(caseRecord.Entity))
                {
                    var entity = customer!.Entities.FirstOrDefault(d => d.Name.Equals(caseRecord.Entity, StringComparison.CurrentCultureIgnoreCase));
                    if (entity != null)
                    {
                        caseRecord.EntityNPI = entity.NPINumber;
                    }
                    else
                    {
                        messages.Add($@"Entity {caseRecord.Entity} does not match an Entity for Customer {customer.Name}. Claim not added/updated.");
                    }
                }

                //// check for duplicate
                //if (!isUpdate)
                //{
                //    var findResult = await FindArbitrationCase(caseRecord, skipDOBCheck, false);
                //    if (findResult.Record != null)
                //        messages.Add("Cannot create the new ArbitrationCase. One already exists with duplicate key values.");
                //    else if (!string.IsNullOrEmpty(findResult.Message))
                //        return findResult.Message;
                //}
            }
            catch (Exception ex)
            {
                messages.Add(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }

            return messages;
        }
    }
}
