using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MPArbitration.Model;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;


// NOTE: This project uses a special <ValidateExecutableReferencesMatchSelfContained>false</ValidateExecutableReferencesMatchSelfContained>
// build flag that allows a self-contained project (MPNotify) to reference a self-contained project (MPArbitration). This, however,
// means the non-stand-alone project will not run from the same folder as the stand-alone deployment.

// Build instructions:
// 1. Temporarily add <RuntimeIdentifier>win10-x64</RuntimeIdentifier> into the MPArbitration project
// 2. In the Package Manager Console window of Visual Studio, use the "ls" and "cd" commands to go into the MPNotify project directory
// 3. use this command to try and build: dotnet build MPNotify.csproj --self-contained true
// 4. Remove the <RuntimeIdentifier> flag from the MPArbitration project file before checking into Azure or else continuous integration (CI) will fail. 
// (Azure CI is not set up to build or deploy onto a specific platform such as win10-x64)
namespace MPNotify
{
    enum AppFileType
    {
        ArbitrationCase,
        Authority,
        Payor
    }

    enum ArbitDaemonProcess
    {
        GenerateBriefAssets,
        RebuildNSANotifications,
        SendNSANotifications
    }

    struct MPNotifyStats
    {
        public int Failed;
        public int Proccessed;
        public int TotalRecords;
    }

    internal interface ISendGridApiResponse
    {
        public HttpHeaders? Headers { get; set; }
    }

    internal class SendGridMessages : ISendGridApiResponse
    {
        public HttpHeaders? Headers { get; set; }
#pragma warning disable IDE1006 // Naming Styles
        public List<SendGridActivity>? messages { get; set; }  // lower-case to facilitate deserialization from HttpResponse 
#pragma warning restore IDE1006 // Naming Styles
    }

    internal class SendGridActivity
    {
#pragma warning disable IDE1006 // Naming Styles
        public string from_email { get; set; } = "";
        public string msg_id { get; set; } = "";
        public string subject { get; set; } = "";
        public string to_email { get; set; } = "";
        public string status { get; set; } = "";
        public int opens_count { get; set; } = 0;

        public int clicks_count { get; set; } = 0;

        public DateTime? last_event_time { get; set; } = null;
#pragma warning restore IDE1006 // Naming Styles
        /*
        public SendGridActivity Map(dynamic a)
        {
            var r = new SendGridActivity();
            r.from_email = a.from_email ?? "";
            r.msg_id = a.msg_id ?? "";
            r.last_event_time = a.last_event_time ?? null;
            r.subject = a.subject ?? string.Empty;
            r.to_email = a.to_email ?? string.Empty;
            r.opens_count = (int)a.opens_count;
            r.clicks_count = (int)a.clicks_count;
            return r;
        }
        */
    }

    internal class AppMain
    {
        // Get Tokens (MPArbitration and SendGrid)
        private static readonly bool IsDryRun = false;
        private static string BaseAppAddress = ConfigurationManager.AppSettings["baseAppAddress"] ?? ""; // base address for the Arbit API
        private static readonly string ClientId = ConfigurationManager.AppSettings["client_id"] ?? ""; // Arbit client id
        private static readonly string ClientSecret = ConfigurationManager.AppSettings["clientSecret"] ?? ""; // Arbit app token
        private static readonly int DaysToKeepLogFiles = Math.Abs(Convert.ToInt32(ConfigurationManager.AppSettings["daysToKeepLogFiles"] ?? "30")); // Arbit app token
        private static readonly string EmailFromAddress = ConfigurationManager.AppSettings["EmailFromAddress"] ?? "";
        private static readonly string EmailFromName = ConfigurationManager.AppSettings["EmailFromName"] ?? "";
        private static readonly string SendGridApiKey = ConfigurationManager.AppSettings["SendGridApiKey"] ?? "";
        private static readonly string Resource = ConfigurationManager.AppSettings["resource"] ?? ""; // more Arbit Azure stuff
        private static readonly string TenantId = ConfigurationManager.AppSettings["tenant_id"] ?? "";  // more Arbit Azure stuff

        private static string GetArbitrationDbConnectionString()
        {
            var configured = ConfigurationManager.AppSettings["ArbitrationDbConnectionString"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            var connectionString = ConfigurationManager.ConnectionStrings["ArbitrationDb"]?.ConnectionString;
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }

            var envValue = Environment.GetEnvironmentVariable("ARBITRATION_DB_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                return envValue;
            }

            throw new InvalidOperationException(
                "The arbitration database connection string is not configured. " +
                "Set the 'ArbitrationDbConnectionString' appSetting, the 'ArbitrationDb' connection string, " +
                "or the 'ARBITRATION_DB_CONNECTION_STRING' environment variable.");
        }

        string _assetEmailTemplate = "";
        private async Task<string> GetAssetEmailTemplate()
        {
            
            if(!string.IsNullOrEmpty(_assetEmailTemplate))
            {
                return _assetEmailTemplate;
            }
            string filename = Path.Combine(AppContext.BaseDirectory, "asset-email-template.html");
            if (!File.Exists(filename))
                return string.Empty;

            _assetEmailTemplate = await File.ReadAllTextAsync(filename);
            return _assetEmailTemplate;
            
        }
        private string _appToken = "";
        private HttpClient _client;
        private int _exitCode = 0;
        //private StringBuilder _log = new StringBuilder();
        private readonly string _logPath = "";
        private static readonly string LOG_TEMPLATE = "{0:MM/dd/yyyy hh:mm:ss}: {1}";
        private List<Notification>? _notifications = null;
        private MPNotifyStats _stats;

        private HttpClient GetHttpClient()
        {
            //if (IsTokenExpired(_appToken))
            //{
            //    _ = GetArbAppTokenAsync();
            //    ConfigureArbitClient(_client);
            //}
            return _client;
        }

        public AppMain()
        {
            _logPath = Path.Combine(Path.GetTempPath(), $@"MPNotify_RunLog_{DateTime.Now:MMddyyyy_hhmmss}.log");
            

            _client =  new HttpClient();
            ConfigureArbitClient(_client);

            //GetHttpClient().DefaultRequestHeaders.Accept.Clear();
            //GetHttpClient().DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            //GetHttpClient().DefaultRequestHeaders.Add("User-Agent", ".NET Core 6 Console");

            if (!string.IsNullOrEmpty(BaseAppAddress) && !BaseAppAddress.EndsWith("/"))
                BaseAppAddress+="/";
            
//#if DEBUG
//            BaseAppAddress = "https://localhost:44473/";
//            ClientId = "e6ddd06c-eb88-47fb-8579-185b2436a2cb";
//            ClientSecret = "Sen8Q~ZHDZ0Br-~2l-hdEfWD7uhPl0OvHz7TYbOG";
//            Resource = "api://e6ddd06c-eb88-47fb-8579-185b2436a2cb";
//            TenantId = "2e09f3a3-0520-461f-8474-052a8ed7814a";
//#endif
            
            LogMessage("Starting...", true);
        }

        private void ConfigureArbitClient(HttpClient client, TimeSpan? timeout = null)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Core 6 Console");
            if (!string.IsNullOrEmpty(_appToken))
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _appToken);

            if (timeout.HasValue)
                client.Timeout = timeout.Value;
        }

        private static void RemoveOldLogFiles()
        {
            try
            {
                var startDate = DateTime.Now.AddDays(DaysToKeepLogFiles * -1);
                var dir = new DirectoryInfo(Path.GetTempPath());
                foreach (var f in dir.GetFiles($@"MPNotify_RunLog_*.*").Where(g => g.CreationTimeUtc < startDate).ToArray())
                {
                    f.Delete();
                }
            }
            catch(Exception ex) 
            { 
                Console.WriteLine("Error removing old temp files:" + ex.Message);
            }
        }

        private void LogMessage(string msg, bool writeToConsole = false)
        {
            File.AppendAllText(_logPath, string.Format(LOG_TEMPLATE, DateTime.Now, msg + "\r\n"));
            if(writeToConsole)
                Console.WriteLine(msg);
        }

        public async Task<int> Start(ArbitDaemonProcess processId)
        {
            try
            {
                //RemoveOldLogFiles();
                if (processId == ArbitDaemonProcess.RebuildNSANotifications)
                {
                    return await RebuildNSANotifications();
                }
                   
                /// to test locally processId = ArbitDaemonProcess.SendNSANotifications; 
                // validate processId 
                if (processId != ArbitDaemonProcess.GenerateBriefAssets && processId != ArbitDaemonProcess.SendNSANotifications)
                {
                    var msg = "Invalid process request";
                    LogMessage(msg);
                    throw new Exception(msg);
                }

                // validate some settings
                if (string.IsNullOrEmpty(EmailFromAddress) || string.IsNullOrEmpty(EmailFromName))
                {
                    var msg = "Bad configuration: No EmailFromAddress or EmailFromName!";
                    LogMessage(msg);
                    throw new Exception(msg);
                }

                if(string.IsNullOrEmpty(await GetAssetEmailTemplate()))
                {
                    var msg = "Bad configuration: Missing AssetEmailTemplate!";
                    LogMessage(msg);
                    throw new Exception(msg);
                }

                // get the API token
                bool success = await GetArbAppTokenAsync();
                if (!success)
                    return -1;


                if (processId == ArbitDaemonProcess.GenerateBriefAssets)
                {
/**** Generate Brief Assets ****/
                    await GenerateBriefAssetsAsync();
                    return _exitCode;
                }

/**** SendNSANotifications ****/

                // request pending Notifications from Arb app
                success = await GetArbAppPendingNotificationsAsync();
                if (!success)
                    return _exitCode;

                if (_notifications == null || _notifications.Count == 0)
                {
                    LogMessage("No Pending Notifications found to process. Exiting...", true);
                    return 0;
                }

                await ProcessNotificationsAsync(_notifications);
                LogMessage("Finished processing notifications.");

                _exitCode = 0;
            }
            catch (Exception ex)
            {
                WriteConsoleErrors(ex);
                _exitCode = -1;
            }
            finally {
                LogMessage("Writing stats...");
                WriteStats();
            }
            return _exitCode;
        }

        private async Task<ISendGridApiResponse> GetMessageDeliveryStatusAsync(SendGridClient sgClient, int arbitId)
        {
            var query = $@"{{
                            ""limit"": 10,
                            ""query"":""(Contains(categories,'NSAOpenRequest')) and (unique_args['aid']='{arbitId}')"",
                            }}"; 

            var activity = new SendGridMessages();
            Response? result = null;

            try
            {
                int tries = 0;
                while (tries < 3)
                {
                    try
                    {
                        result = await sgClient.RequestAsync(BaseClient.Method.GET, null, query);
                        tries = 4;
                    }
                    catch 
                    {
                        tries++;
                        LogMessage($@"SendGrid call failed. Retrying in 5 seconds ({tries})...", true);
                        await Task.Delay(5000);
                    }
                }

                if (result == null || !result.IsSuccessStatusCode)
                {
                    LogMessage("SendGrid API was unresponsive. Aborting.", true);
                    return activity;
                }

                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    LogMessage($@"SendGrid API returned {result.StatusCode} for ArbitrationCaseId {arbitId}", true);
                    return activity;
                }

                var stuff = new StreamReader(result.Body.ReadAsStream()).ReadToEnd();
                if (string.IsNullOrEmpty(stuff))
                {
                    LogMessage("SendGrid API call returned OK but results were empty!", true);
                    return activity;
                }
                
                
                // to deserialize -> https://stackoverflow.com/questions/59040873/sendgrid-deserialization-of-email-activity-api-json-response-not-populating-obj
                activity = JsonSerializer.Deserialize<SendGridMessages>(stuff);
                if (activity == null || activity.messages == null)
                    activity = new SendGridMessages();
                
                activity.Headers = result.Headers;
                return activity;
            }
            catch(Exception ex)
            {
                LogMessage($@"SendGrid Activity Search Error: {ex.Message}");
                if(ex.InnerException != null)
                    LogMessage($@"{ex.InnerException.Message}");
                return new SendGridMessages();
            }
            
        }

        private async Task<SendGridMessages> LoadNSAOpenRequestActivityReportsAsync(SendGridClient sgClient)
        {
            // Message Categories choices -> "Arbitration", "NSA", "NSAOpenRequest", "PayorNotification" 
            //,""categories"":""(Contains(categories, 'NSA Submission Request'))""
            // (unique_args['aid']='{arbitrationCaseId}') AND 
            // ??? (custom_args['aid']='{arbitrationCaseId}') AND 
            //  ""limit"": 1,

            // NOTE: SendGrid only keeps 30 days of activity so this should be sufficient for now. Yes, it is an external dependency but this
            // is all such a proof of concept that could change on a whim so it is good enough for now. Yes, it will need to get better as things solidify.
            var query = $@"{{
                            ""limit"": 2000,
                            ""query"":""(Contains(categories,'NSAOpenRequest'))"",
                            }}";
            Response? result;
            var activity = new SendGridMessages();

            result = await sgClient.RequestAsync(BaseClient.Method.GET, null, query);
            if (result.IsSuccessStatusCode)
            {
                if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    LogMessage($@"SendGrid Email Activity search returned NotFound", true);
                }
                else if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    //var stuff = await result.DeserializeResponseBodyAsync(result.Body);
                    var stuff = new StreamReader(result.Body.ReadAsStream()).ReadToEnd();
                    if (string.IsNullOrEmpty(stuff))
                    {
                        LogMessage($@"          SendGrid Email Activity search result was empty!", true);  // should never happen if we're sending out any email at all, of course
                    }
                    else
                    {
                        // to deserialize -> https://stackoverflow.com/questions/59040873/sendgrid-deserialization-of-email-activity-api-json-response-not-populating-obj
                        activity = JsonSerializer.Deserialize<SendGridMessages>(stuff);
                        if (activity == null || activity.messages == null)
                            activity = new SendGridMessages();
                    }
                }
            }

            return activity;
        }

        /// <summary>
        /// Searches SendGrid delivery info for the past 30 days for items matching the claim.
        /// </summary>
        /// <param name="arbitrationCaseId"></param>
        /// <returns></returns>
        private async Task CreateAndSaveProofOfNSAOpenNegotiationRequestAsync(AppHealthDetail claim, SendGridMessages NSAOpenRequestActivityReports)
        {
            /* Helpful link
             * https://www.csharpcodi.com/csharp-examples/SendGrid.SendGridClient.RequestAsync(SendGrid.SendGridClient.Method,%20string,%20string,%20string,%20System.Threading.CancellationToken)/
             */

            var activity = NSAOpenRequestActivityReports.messages!.FirstOrDefault(d => d.to_email.Equals(claim.NSARequestEmail, StringComparison.CurrentCultureIgnoreCase) && d.subject.EndsWith(claim.PayorClaimNumber, StringComparison.CurrentCultureIgnoreCase));
            if (activity == null)
            {
                LogMessage($@"No matching SendGrid activity found for Category 'NSAOpenRequest' and PayorClaimNumber {claim.PayorClaimNumber} going to email {claim.NSARequestEmail}", true);
                return;
            }


            if (activity.status != "delivered")
            {
                LogMessage($@"SendGrid status is not yet 'delivered' for PayorClaimNumber {claim.PayorClaimNumber} going to email {claim.NSARequestEmail}", true);
                return;
            }

            try
            {
                LogMessage($@"     '{activity.subject}' delivered. Open count: {activity.opens_count}. Building Proof PDF...", true); // GeneratePDFFromHTML() -> UploadFileToArbitClaim(recordId, ProofOfOpenNegotiation, pdf)
                
                var pdf = await CreateProofOfNSAOpenNotificationRequestAsync(claim.Id, activity.subject, activity.from_email, activity.to_email);

                if (pdf != null)
                {
                    LogMessage("     Proof created. Attaching to claim...", true);
                    var success = await AttachDocumentToArbitrationClaimAsync(claim.Id, "ProofOfOpenNegotiation", pdf);
                    if (success)
                    {
                        LogMessage("     Attaching to claim successfully!", true);
                    }
                }
                else
                {
                    LogMessage("     Unable to create Proof PDF!", true);
                }
            }
            catch (Exception ex)
            {
                LogMessage($@"Error in CreateAndSaveProofOfNSAOpenNegotiationRequest for PayorClaimNumber {claim.PayorClaimNumber} going to email {claim.NSARequestEmail}:", true);
                LogMessage(ex.Message, true);
            }
        }

        /// <summary>
        /// Sends the bytes of a PDF file to the Arbit CasesController.
        /// </summary>
        /// <param name="arbitrationCaseId"></param>
        /// <param name="docType"></param>
        /// <param name="pdf"></param>
        /// <returns></returns>
        private async Task<bool> AttachDocumentToArbitrationClaimAsync(int arbitrationCaseId, string docType, byte[] pdf)
        {
            bool retval = false;
            if (_client == null)
                return retval;
            var error = string.Empty;
            try
            {

                string address = $@"{BaseAppAddress}api/cases/blob?id={arbitrationCaseId}&cdt=";
                if (docType == "ProofOfOpenNegotiation")
                {
                    address += docType;
                    var uri = new Uri(address);
                    var b = new ByteArrayContent(pdf);
                    string filename = $@"{arbitrationCaseId}-{docType}.pdf";

                    var content = new MultipartFormDataContent {
                        { b, "file", filename}
                    };
                        
                    var result = await GetHttpClient().PostAsync(uri, content);

                    content.Dispose();
                    b.Dispose();
                    error = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    result.EnsureSuccessStatusCode();
                    retval = true;
                }
            }
            catch(Exception ex)
            {
                LogMessage($"Error in AttachDocumentToArbitrationClaim: {ex.Message} {error}");
                
            }

            return retval;
        }

        /// <summary>
        /// Creates a formatted outer email that shows recipients, attachment list and delivery info based on SendGrid's message activity.
        /// </summary>
        /// <param name="arbitrationCaseId"></param>
        /// <param name="sgActivity"></param>
        /// <returns></returns>
        private async Task<byte[]?> CreateProofOfNSAOpenNotificationRequestAsync(int arbitrationCaseId, string subject, string from_email, string to_email)
        {
            byte[]? pdf = null;
            ILogger? logger = null;

            try
            {
                // fetch the Notification for this case id 
                var notification = await GetNotificationAsync(arbitrationCaseId, NotificationType.NSANegotiationRequest);
                if (notification != null && !string.IsNullOrEmpty(notification.HTML))
                {
                    // add header and formatting
                    string html = await FormatAsEmailForPDFRenderingAsync(subject, from_email, to_email, notification);
                    
                    // render to pdf (FYI...we could just let the server do all of this at once but that would harm server performance and web users' experience)
                    pdf = NRecoPdfWrapper.GeneratePDF(logger, html, new Dictionary<string, string>(), new Dictionary<string, string>(), out string problems);
                }
            }
            catch (Exception ex) 
            {
                LogMessage($@"Unable to create ProofOfNSAOpenNotification for ID {arbitrationCaseId}: " + ex.Message);
            }

            return pdf;
        }

        private async Task GenerateBriefAssetsAsync()
        {
            try
            {
                // Do one-time setup of SendGrid stuff
                var sgClient = new SendGridClient(SendGridApiKey)
                {
                    UrlPath = "messages" // https://api.sendgrid.com/v3/messages
                };


                // Update sent notifications with delivery information - probably needs its own code path and
                // Enum parameter but that means running yet another instance of this daemon. Another option
                // is to make this same call at the end of the SendNSANotification path, maybe after a 2-3 minute delay
                // to give SendGrid a chance to deliver all of the messages. Or do both.
                await UpdateNotificationsDeliveryInfo(sgClient);

                // Update all Notifications with latest SendGrid info
                SendGridMessages NSAOpenRequestActivityReports = await LoadNSAOpenRequestActivityReportsAsync(sgClient);
                if(NSAOpenRequestActivityReports.messages == null || NSAOpenRequestActivityReports.messages.Count == 0)
                {
                    throw new Exception("No sent messages returned from the service provider. Aborting.");
                }
                // Get a list of 
                var worklist = await GetArbAppReadyForAssetGenerationAsync();

                foreach (var item in worklist)
                {
                    // get all 
                    var fileList = await GetNSARequestAttachmentListAsync(AppFileType.ArbitrationCase, item.Id.ToString(), "");

                    LogMessage($@"Building assets for ArbitrationCases.Id {item.Id}", true);

                    // Build ProofOfOpenNegotiation ?
                    string docType = Enum.GetName(CaseDocumentType.ProofOfOpenNegotiation) ?? "";
                    if (fileList.FirstOrDefault(d => d.Tags != null && d.Tags.ContainsKey("DocumentType") && d.Tags["DocumentType"] == docType) == null)
                    {
                        LogMessage("     Checking for NSA Open Negotiation email delivery...", true);
                        await CreateAndSaveProofOfNSAOpenNegotiationRequestAsync(item, NSAOpenRequestActivityReports);
                    } else
                    {
                        LogMessage($@"     {docType} already exists. Skipping creation.", true);
                    }

                    // Build NSAOfferTable ?
                }

                Console.WriteLine($@"Finished generating assets for {worklist.Count} claims");

                _exitCode = 0;
                return;
            }
            catch(Exception ex)
            {
                LogMessage(ex.Message);
                _exitCode = -1;
            }
        }

        /// <summary>
        /// Build the HTML needed to produce the SendGrid NSA Notification message
        /// </summary>
        /// <param name="n"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        private static string CombineHTMLContent(Notification n, JsonNode json)
        {
            var regs = new List<Regex>();
            var sb = new StringBuilder(@"<html><head><link href='https://fonts.googleapis.com/css?family=Sacramento' rel='stylesheet' /></head><body>");
            regs.Add(new Regex(@"(?s)<head>.*?<\/head>", RegexOptions.Multiline));
            regs.Add(new Regex(@"(?s)<body.*?>", RegexOptions.Multiline));
            regs.Add(new Regex(@"<\/body>", RegexOptions.Multiline));

            var html = n.HTML;

            // remove any head or body sections
            foreach (var exp in regs)
            {
                var matches = exp.Matches(html);
                if (matches.Count > 0)
                {
                    _ = html.Replace(matches[0].Value, "");
                }
            }
            
            sb.Append(html);

            // pull in supplements and append them to the first html body
            var supplements = json.AsObject()["supplements"];
            if (supplements != null && supplements.GetType() == typeof(JsonArray))
            {
                JsonArray arr = (JsonArray)supplements;
                foreach (var item in arr)
                {
                    if (item != null && item["html"] != null)
                    {
                        html = item["html"]?.ToString() ?? "";
                        foreach (var exp in regs)
                        {
                            var matches = exp.Matches(html);
                            if (matches.Count > 0)
                            {
                                _ = html.Replace(matches[0].Value, "");
                            }
                        }
                        sb.AppendLine("<br/><hr/><br/>");
                        sb.Append(html);
                    }
                }
            }
            
            sb.Append("</body></html>");
            return sb.ToString();
        }

        private async Task<byte[]?> GetAppFileAsync(AppFileType fileType, int recordId, string name, string key = "")
        {
            string controller;
            if (fileType == AppFileType.ArbitrationCase)
                controller = "cases";
            else if (fileType == AppFileType.Payor)
                controller = "payors";
            else
                controller = "authorities";

            var location = $@"{BaseAppAddress}api/{controller}/blob?id={recordId}&name={name}";
            if (!string.IsNullOrEmpty(key))
                location += $@"&key={key}";

            var address = new Uri(location);

            Console.WriteLine($@"Fetching NSA attachments...");
            byte[]? b = null;
            
            try
            {
                var response = await GetHttpClient().GetAsync(address);
                if (response.IsSuccessStatusCode)
                {
                    b = await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    Console.WriteLine($@"API error retrieving attachment {name}. Error to follow:");
                    Console.WriteLine(response.ReasonPhrase);
                    Console.WriteLine("Notification will still be attempted"); // why not make this a critical error? because the notifications are probably up against a deadline and a bad one is better than none at all
                }
            }
            catch (Exception ex)
            {
                LogMessage($@"Exception while retrieving list of {controller} attachments for Id {recordId}. Message(s) to follow:", true);
                WriteConsoleErrors(ex);
            }
            return b;
        }

        /// <summary>
        /// Pulls the HTML out of a Notification and surrounds it with the necessary boilerplate to recreate the effect of printing the message using a browser.
        /// NOTE: This does not currently incorporate any delivery info from the SendGridActivity but can easily be added when the business decides how they want to include/display it.
        /// </summary>
        /// <param name="sgActivity"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        private async Task<string> FormatAsEmailForPDFRenderingAsync(string subject, string from_email, string to_email, Notification notification)
        {
            string html = await GetAssetEmailTemplate();
            if (html.Length == 0 || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(from_email) || string.IsNullOrEmpty(to_email))
                return notification.HTML;

            var dom = new HtmlDocument();
            dom.LoadHtml(html);
            var bodyElement = dom.GetElementbyId("message-body");
            if(bodyElement == null)
                return notification.HTML;
            var subjectElement = dom.GetElementbyId("subject");
            if (subjectElement == null)
                return notification.HTML;
            var fromAddressElement = dom.GetElementbyId("from-address");
            if (fromAddressElement == null)
                return notification.HTML;
            var toAddressElement = dom.GetElementbyId("to-address");
            if (toAddressElement == null)
                return notification.HTML;
            var ccAddressElement = dom.GetElementbyId("cc-address");
            if (ccAddressElement == null)
                return notification.HTML;
            var sentOnElement = dom.GetElementbyId("sent-on");
            if (sentOnElement == null)
                return notification.HTML;
            var attBoxElement = dom.GetElementbyId("attachments-box");
            if (attBoxElement == null)
                return notification.HTML;
            var attNamesElement = dom.GetElementbyId("attachment-names");
            if (attNamesElement == null)
                return notification.HTML;

            subjectElement.InnerHtml = subject;
            fromAddressElement.InnerHtml = from_email;
            toAddressElement.InnerHtml = to_email;
            ccAddressElement.InnerHtml = notification.CC;
            sentOnElement.InnerHtml = "";

            if(notification.SentOn.HasValue)
                sentOnElement.InnerHtml = notification.SentOn.Value.ToString("ddd M/dd/yyyy h/mm");
            
            attBoxElement.InnerHtml = "";
            attNamesElement.InnerHtml = "";
            var jnode = JsonNode.Parse(notification.JSON);
            if (jnode != null && jnode["delivery"] is JsonObject && jnode["delivery"]!["attachments"] is JsonArray) {
                var attachments = jnode["delivery"]!["attachments"] as JsonArray;
                long totalContent = 0;
                string filenames = "";
                foreach (var att in attachments!)
                {
                    if (att != null)
                    {
                        if (att["fileSize"] is JsonValue)
                            totalContent += att["fileSize"]?.GetValue<int>() ?? 0;
                        var fn = att["fileName"]?.ToString();
                        if (!string.IsNullOrEmpty(fn))
                            filenames += fn + "; ";
                    }
                }
                 
                if(totalContent > 0)
                {
                    attBoxElement.InnerHtml = $@"{attachments.Count} attachment";
                    if (attachments.Count > 1)
                        attBoxElement.InnerHtml += "s";

                    attBoxElement.InnerHtml += $@" ({Utilities.FormatBytes(totalContent)})";
                    attNamesElement.InnerHtml = filenames;
                }
            }
            /*
            if (sgActivity.customArgs["attachments"])
                attBoxElement.InnerHtml = stuff
            */
            bodyElement.InnerHtml = notification.HTML;
            return dom.DocumentNode.OuterHtml;
        }

        /// <summary>
        /// Fetches a pending Notification record using the ArbitrationCases Id and NotificationType
        /// </summary>
        /// <param name="arbitrationCaseId"></param>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<Notification?> GetNotificationAsync(int arbitrationCaseId, NotificationType notificationType)
        {
            Notification? notification = null;

            try
            {
                var nt = Enum.GetName<NotificationType>(notificationType);
                var address = new Uri($@"{BaseAppAddress}api/notifications/queued?c={arbitrationCaseId}&t={nt}");

                LogMessage($@"Fetching Notification from {address}...", true);
                
                var result = await GetHttpClient().GetAsync(address);
                if (!result.IsSuccessStatusCode)
                {
                    LogMessage($@"Unable to retrieve Pending NSA Notifications. Reason to follow...");
                    LogMessage(result.ToString());
                }
                else if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    LogMessage($@"Notification not found. This could simply be because it has not been sent yet.");
                } 
                else 
                {
                    // parse notification
                    var content = await result.Content.ReadAsStringAsync();
                    notification = JsonSerializer.Deserialize<Notification>(content);
                }
            }
            catch (Exception ex)
            {
                LogMessage($@"Error fetching Notification: " + ex.Message);
                WriteConsoleErrors(ex);
            }
            return notification;
        }

        /// <summary>
        /// Fetches the list of Notifications in the Pending state and stores them in a global variable for use later.
        /// </summary>
        /// <returns>True for successfully fetching and parsing the records</returns>
        /// <exception cref="Exception"></exception>
        private async Task<bool> GetArbAppPendingNotificationsAsync()
        {
            var address = new Uri($@"{BaseAppAddress}api/notifications/unsent?t=NSANegotiationRequest");

            LogMessage($@"Fetching Pending NSA Notifications from {address}...", true);

            var result = await GetHttpClient().GetAsync(address);
            if (!result.IsSuccessStatusCode)
            {
               LogMessage($@"Unable to retrieve Pending NSA Notifications. Reason to follow...", true);
               LogMessage(result.ToString(), true);
                _exitCode = -1;
                return false;
            }

            // parse notifications 
            try
            {
                var content = await result.Content.ReadAsStringAsync();
                _notifications = JsonSerializer.Deserialize<List<Notification>>(content);
                if (_notifications != null)
                {
                    var logmsg = $@"Found {_notifications.Count} to process";
                    LogMessage(logmsg);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogMessage($@"Error Parsing notifications list: " + ex.Message);                
                WriteConsoleErrors(ex);
            }

            _exitCode = -1;
            return false;
        }

        /// <summary>
        /// Query the Arbit API for record IDs that are ready to have a brief built. 
        /// </summary>
        /// <returns></returns>
        private async Task<ArbitrationCase?> GetArbAppClaimByIdAsync(int id)
        {
            var address = new Uri($@"{BaseAppAddress}api/claims/{id}");

            LogMessage($@"Fetching ArbitrationCase by Id...", true);

            var result = await GetHttpClient().GetAsync(address);
            result.EnsureSuccessStatusCode();

            // parse notifications 
            var content = await result.Content.ReadAsStringAsync();
            var claim = JsonSerializer.Deserialize<ArbitrationCase>(content);
            
            return claim;
        }

        /// <summary>
        /// Query the Arbit API for record IDs that are ready to have a brief built. 
        /// </summary>
        /// <returns></returns>
        private async Task<List<AppHealthDetail>> GetArbAppReadyForAssetGenerationAsync()
        {
            var address = new Uri($@"{BaseAppAddress}api/briefs/assets/incomplete");

            LogMessage($@"Fetching list of ArbitrationCases Ids that need Brief assets generated. URL {address}...", true);

            var result = await GetHttpClient().GetAsync(address);
            result.EnsureSuccessStatusCode();

            // parse notifications 
            var content = await result.Content.ReadAsStringAsync();
            var arbitClaimList = JsonSerializer.Deserialize<List<AppHealthDetail>>(content);
            arbitClaimList ??= new List<AppHealthDetail>();

            LogMessage($@"Found {arbitClaimList.Count} claims to process");

            return arbitClaimList;
        }

        /// <summary>
        /// Log into Arbit
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<bool> GetArbAppTokenAsync()
        {

            var pairs = new List<KeyValuePair<string, string>>
            {
                new("client_id", ClientId),
                new("client_secret", ClientSecret),
                new("grant_type", "client_credentials"),
                new("resource", Resource),
                new("tenant_id", TenantId)
            };

            var address = new Uri($@"https://login.microsoftonline.com/{TenantId}/oauth2/token");
            var content = new FormUrlEncodedContent(pairs);
            var result = await GetHttpClient().PostAsync(address, content);

            if (!result.IsSuccessStatusCode)
            {
                LogMessage("Unable to retrieve access token. Reason to follow...", true);
                LogMessage(result.ToString(), true);
                _exitCode = -1;
                return false;
            }

            // parse and store MSFT token
            LogMessage($@"App token retrieved");

            string str = await result.Content.ReadAsStringAsync();
            dynamic msft = JsonSerializer.Deserialize<Dictionary<string, object>>(str);
            _appToken = msft!["access_token"].ToString();
            GetHttpClient().DefaultRequestHeaders.Add("Authorization", "Bearer " + _appToken);
            Console.WriteLine("Access token acquired.");
            _exitCode = 0;
            return true;
        }

        /// <summary>
        /// Fetch a list of attachments for a claim
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="idOrKey"></param>
        /// <returns></returns>
        private async Task<List<CaseFile>> GetNSARequestAttachmentListAsync(AppFileType fileType, string idOrKey, string docType)
        {

            string controller;
            if (fileType == AppFileType.ArbitrationCase)
                controller = $@"cases/files/{idOrKey}";
            else if (fileType == AppFileType.Payor)
                controller = $@"payors/files/{idOrKey}";
            else
                controller = $@"authorities/item/bykey/{idOrKey}/files";

            var address = new Uri($@"{BaseAppAddress}api/{controller}?docType={docType}");
            LogMessage($@"Fetching list of files from {controller} ...", true);

            try
            {
                var result = await GetHttpClient().GetAsync(address);
                if (result.IsSuccessStatusCode)
                {
                    var cf = JsonSerializer.Deserialize<List<CaseFile>>(await result.Content.ReadAsStringAsync());
                    return cf ?? new List<CaseFile>();
                }
            }
            catch (Exception ex)
            {
                LogMessage($@"Exception retrieving or decoding {controller} list. Message to follow:", true);
                LogMessage(ex.Message, true);
                WriteConsoleErrors(ex);
            }
            return new List<CaseFile>(); // just swallowing the errors for now
        }

        /// <summary>
        /// Sends an update to the API Notifications endpoint. (Note that at the time of this writing, the 
        /// PUT endpoint will only process updates to the Delivery JSON.)
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<bool> SaveChangesToNotificationDeliveryAsync(Notification notification)
        {
            var address = new Uri($@"{BaseAppAddress}api/notifications");

            using StringContent jsonContent = new(
               JsonSerializer.Serialize(notification),
               Encoding.UTF8,
               "application/json");

            bool success = false;
            try
            {
                LogMessage($@"Notification Id {notification.Id}: Updating Notification delivery status...", true);

                using HttpResponseMessage result = await GetHttpClient().PutAsync(
                    address,
                    jsonContent);

                success = result.IsSuccessStatusCode;

                if (!success)
                {
                    LogMessage(result.ReasonPhrase ?? "no reason given", true);
                }
                else
                {
                    LogMessage($@"Notification Id {notification.Id}: Delivery date updated successfully!", true);
                }
            }
            catch (Exception ex)
            {
                LogMessage($@"Notification Id {notification.Id}: Error in SaveChangesToNotificationDeliveryAsync. Message to follow:", true);
                WriteConsoleErrors(ex);
            }
            return success;
        }

        private async Task<bool> MarkNotificationFailedAsync(Notification notification, string message)
        {
            _stats.Failed++;
            LogMessage($@"Notification Id {notification.Id}: {message}", true);
            

            var address = new Uri($@"{BaseAppAddress}api/cases/wf");
            
            using StringContent jsonContent = new(
               JsonSerializer.Serialize(new
               {
                   action = 5, //"NotificationFailed"
                   assignToId = notification.Id,
                   customerId = 0,
                   caseId = notification.ArbitrationCaseId,
                   message = message
               }),
               Encoding.UTF8,
               "application/json");

            bool success = false;
            try
            {
                LogMessage($@"Notification Id {notification.Id}: Updating Notification status...", true);

                using HttpResponseMessage result = await GetHttpClient().PostAsync(
                    address,
                    jsonContent);
                
                success = result.IsSuccessStatusCode;

                if (!success)
                {
                    LogMessage(result.ReasonPhrase, true);
                }
                else
                {
                    LogMessage($@"Notification Id {notification.Id}: Status updated successfully!", true);
                }
            }
            catch(Exception ex)
            {
                LogMessage($@"Notification Id {notification.Id}: Error in MarkNotificationFailedAsync. Message to follow:", true);
                WriteConsoleErrors(ex);
            }
            return success;
        }

        private async Task<bool> MarkNotificationSuccessAsync(Notification notification)
        {
            _stats.Proccessed++;

            bool success = false;

            LogMessage($@"Notification Id {notification.Id}: Updating Notification status for ArbitrationCase {notification.ArbitrationCaseId} ...", true);

            try {
                var address = new Uri($@"{BaseAppAddress}api/cases/wf");

                using StringContent jsonContent = new(
                   JsonSerializer.Serialize(new
                   {
                       action = CaseWorkflowAction.NSARequestSentToPayor,
                       assignToId = notification.Id,
                       JSON = notification.JSON, // capture any additional delivery info and send it back for selective inclusion into the 
                       customerId = 0,
                       caseId = notification.ArbitrationCaseId,
                       message = ""
                   }),
                   Encoding.UTF8,
                   "application/json");

                using HttpResponseMessage result = await GetHttpClient().PostAsync(
                    address,
                    jsonContent);

                success = result.IsSuccessStatusCode;

                if (!success)
                {
                    LogMessage($@"Notification Id {notification.Id}: {result.ReasonPhrase}", true);
                }
                else
                {
                    LogMessage($@"Notification Id {notification.Id}: Status updated successfully!", true);
                }
            }
            catch (Exception ex)
            {
                LogMessage($@"Notification Id {notification.Id}: Error in MarkNotificationFailedAsync. Message to follow:", true);
                WriteConsoleErrors(ex);
            }
            return success;
        }

        private async Task ProcessNotificationsAsync(List<Notification> notifications)
        {
            _stats.TotalRecords = notifications.Count;

            // Do one-time setup of SendGrid stuff
            var sgClient = new SendGridClient(SendGridApiKey);
            var messageArgs = new Dictionary<string, string>
            {
                { "aid", "" },  // add the arbit record id to the message
            }; // {"attachments","" }  doesn't seem to be a need to keep adding this to SendGrid

            var messageCategories = new List<string> { "NSA", "Arbitration", "PayorNotification", "NSAOpenRequest" };

            foreach (var notification in notifications)
            {
                if (string.IsNullOrEmpty(notification.JSON))
                    notification.JSON = "{}"; // prevent meltdown

                Dictionary<string, object>? goodies = null;
                int payorId = 0;

                try
                {
                    // TODO: Redo this using JsonNode if that is simpler to use. See CombineHTMLContent method for example.
                    goodies = JsonSerializer.Deserialize<Dictionary<string, object>>(notification.JSON) ?? new Dictionary<string, object>();
                    payorId = goodies.ContainsKey("payorId") && goodies["payorId"] != null ? ((JsonElement)goodies["payorId"]).GetInt32() : 0;
                }
                catch (Exception ex)
                {
                    var m2 = $@"Invalid JSON object. payorId must be in the root and greater than zero.";
                    LogMessage(m2);
                    LogMessage(ex.Message);
                    await MarkNotificationFailedAsync(notification, m2);
                    continue;
                }
                

                if(payorId == 0)
                {
                    var m2 = "Missing payorId in Notification JSON object.";
                    LogMessage(m2);
                    await MarkNotificationFailedAsync(notification, m2);
                    continue;
                }

                messageArgs["aid"] = notification.ArbitrationCaseId.ToString();

                // create message and send
                var msg = new SendGridMessage()
                {
                    Categories = messageCategories,
                    From = new EmailAddress(notification.ReplyTo),    
                    ReplyTo = new EmailAddress(notification.ReplyTo),  // per Customer
                    Subject = $@"{notification.Customer} NSA Submission Request for {notification.PayorClaimNumber}",
                    CustomArgs = messageArgs // fetch the status of this notification using this URL: https://api.sendgrid.com/v3/messages?limit=1&query=(unique_args%5B%27aid%27%5D%3D%22999999%22)  <- replace 999999 with the Arbit Id
                };
                
                // all of the TO, CC and BCC addresses
                int validRecipients = 0;
                var TOs = new List<string>();
                foreach (var str in notification.To.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Utilities.IsValidEmail(str))
                    {
                        msg.AddTo(str);
                        TOs.Add(str);
                        validRecipients++;
                    }
                }

                if (validRecipients == 0)
                {
                    var m2 = "No valid TO address detected. Skipping.";
                    LogMessage(m2);

                    await MarkNotificationFailedAsync(notification, m2);
                    continue;
                }
                
                if (!string.IsNullOrEmpty(notification.CC))
                {
                    foreach (var str in notification.CC.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (Utilities.IsValidEmail(str) && !TOs.Contains(str, StringComparer.CurrentCultureIgnoreCase))
                        {
                            msg.AddCc(str);
                            TOs.Add(str);
                        }
                    }

                }

                if (!string.IsNullOrEmpty(notification.BCC))
                {
                    foreach (var str in notification.BCC.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (Utilities.IsValidEmail(str) && !TOs.Contains(str, StringComparer.CurrentCultureIgnoreCase))
                        {
                            msg.AddBcc(str);
                        }
                    }
                }

                // get attachments
                var payorFiles = await GetNSARequestAttachmentListAsync(AppFileType.Payor, payorId.ToString(), "NSARequestAttachment");
                var caseFiles = await GetNSARequestAttachmentListAsync(AppFileType.ArbitrationCase, notification.ArbitrationCaseId.ToString(), "NSARequestAttachment");
                var EOBs = await GetNSARequestAttachmentListAsync(AppFileType.ArbitrationCase, notification.ArbitrationCaseId.ToString(), "EOB");
                var authorityFiles = await GetNSARequestAttachmentListAsync(AppFileType.Authority, notification.AuthorityKey, "NSARequestAttachment");

                if (payorFiles.Count > 0 || caseFiles.Count > 0 || authorityFiles.Count > 0 || EOBs.Count > 0)
                {
                    msg.Attachments = new List<SendGrid.Helpers.Mail.Attachment>();

                    // fetch the blobs and add to the message
                    foreach (var f in payorFiles)
                    {
                        var BlobBytes = await GetAppFileAsync(AppFileType.Payor, payorId, f.BLOBName);
                        if (BlobBytes != null)
                        {
                            msg.Attachments.Add(new SendGrid.Helpers.Mail.Attachment() {
                                Content = Convert.ToBase64String(BlobBytes),
                                Filename = f.BLOBName,
                                Type = "application/pdf",
                                Disposition = "attachment"
                            });
                        }
                    }
                    // fetch the blobs and add to the message
                    foreach (var caseFile in caseFiles)
                    {
                        var b = await GetAppFileAsync(AppFileType.ArbitrationCase, notification.ArbitrationCaseId, caseFile.BLOBName);
                        if (b != null)
                        {
                            msg.Attachments.Add(new SendGrid.Helpers.Mail.Attachment()
                            {
                                Content = Convert.ToBase64String(b),
                                Filename = caseFile.BLOBName,
                                Type = "application/pdf",
                                Disposition = "attachment"
                            });
                        }
                    }
                    // fetch the blobs and add to the message
                    foreach (var caseFile in EOBs)
                    {
                        var b = await GetAppFileAsync(AppFileType.ArbitrationCase, notification.ArbitrationCaseId, caseFile.BLOBName);
                        if (b != null)
                        {
                            msg.Attachments.Add(new SendGrid.Helpers.Mail.Attachment()
                            {
                                Content = Convert.ToBase64String(b),
                                Filename = caseFile.BLOBName,
                                Type = "application/pdf",
                                Disposition = "attachment"
                            });
                        }
                    }
                    // fetch the blobs and add to the message
                    foreach (var f in authorityFiles)
                    {
                        var b = await GetAppFileAsync(AppFileType.Authority, 0, f.BLOBName, notification.AuthorityKey);
                        if (b != null)
                        {
                            msg.Attachments.Add(new SendGrid.Helpers.Mail.Attachment()
                            {
                                Content = Convert.ToBase64String(b),
                                Filename = f.BLOBName,
                                Type = "application/pdf",
                                Disposition = "attachment"
                            });
                        }
                    }
                }

                msg.SetOpenTracking(true);
                msg.HtmlContent = CombineHTMLContent(notification, JsonNode.Parse(notification.JSON));
                
                Response? result = null;
                bool success = IsDryRun;

                try
                {
                    if (!IsDryRun)
                    {
                        var logmsg = $@"Sending message to {notification.To}";
                        LogMessage(logmsg);

                        result = await sgClient.SendEmailAsync(msg);
                        success = result.IsSuccessStatusCode;
                        if (success)
                        {
                            LogMessage($@"Arbit Id {notification.ArbitrationCaseId}: Notification successfully queued for delivery.");
                        }
                    }
                    else
                    {
                       LogMessage("DryRun is true. Nothing sent.");
                    }

                    if (success)
                    {
                        var mid = result!.Headers.FirstOrDefault(d => d.Key.Equals("x-message-id", StringComparison.CurrentCultureIgnoreCase));
                        goodies.Add("messageId", (mid.Value as string[])[0]);
                        goodies.Add("sender", "sendgrid");
                        goodies.Add("subject", msg.Subject);
                        if (msg.Attachments != null && msg.Attachments.Count > 0)  // assuming this is always adding the attachments to JSON but need to validate during debug one more time
                            goodies.Add("attachments", msg.Attachments.ToArray().Select(x => new { fileName = x.Filename, fileSize = x.Content.Length }));
                        notification.JSON = JsonSerializer.Serialize(goodies);
                        var ok = await MarkNotificationSuccessAsync(notification);
                        if (!ok)
                        {
                            ShowCatastrophicMessage(notification.ArbitrationCaseId);
                            _exitCode = -1;
                            return;
                        }

                        // Build a PDF version of the "sent" email and attach it to the claim
                        try
                        {
                            var pdf = await CreateProofOfNSAOpenNotificationRequestAsync(notification.ArbitrationCaseId, msg.Subject, notification.ReplyTo, notification.To);

                            if (pdf != null)
                            {
                                LogMessage("     Proof of Notification created. Attaching to claim...", true);
                                var s2 = await AttachDocumentToArbitrationClaimAsync(notification.ArbitrationCaseId, "ProofOfOpenNegotiation", pdf);
                                if (s2)
                                    LogMessage("     Attached to Claim successfully!", true);
                            }
                            else
                            {
                                LogMessage("     Unable to create Proof PDF!", true);
                            }
                        }
                        catch (Exception ex)
                        {
                            string emsg = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                            LogMessage("     Unable to create Proof PDF and attach to claim:" + emsg);
                        }
                    }
                    else
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        var message = await result.Body.ReadAsStringAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        var props = await result.DeserializeResponseBodyAsync(); // breakpoint here for curiosity - see Ops fax project for what to expect here
                        var ok = await MarkNotificationFailedAsync(notification, message);
                        if (!ok)
                        {
                            ShowCatastrophicMessage(notification.ArbitrationCaseId);
                            _exitCode= -1;
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Catastrophic error sending Email. Message to follow.");
                    WriteConsoleErrors(ex);
                }
            }
        }

        private async Task UpdateNotificationsDeliveryInfo(SendGridClient sgClient)
        {
            // Request all Arbit Notifications containing a SendGrid messageId and no delivery info
            LogMessage($@"Fetching Arbit Notifications to update...", true);
            var address = new Uri($@"{BaseAppAddress}api/notifications/undelivered");
            HttpResponseMessage? result = null;
            using (var client = new HttpClient())
            {
                ConfigureArbitClient(client, TimeSpan.FromMinutes(10));
                result = await client.GetAsync(address);
            }

            result.EnsureSuccessStatusCode();

            // parse notifications 
            var content = await result.Content.ReadAsStringAsync();
            var notifications = JsonSerializer.Deserialize<List<Notification>>(content);
            SendGridMessages? activity = null;

            if (notifications == null || notifications.Count == 0)
                return;

            // loop over notifications and request status from the delivery service. These requests must be throttled
            // based on the rate limit returned by the service (SendGrid)
            foreach (var notification in notifications)
            {
                try
                {
                    JsonNode? node = JsonNode.Parse(notification.JSON);
                    if (node != null && node.AsObject().ContainsKey("delivery") && node["delivery"]!.AsObject().ContainsKey("deliveredOn") && node["delivery"]!["deliveredOn"] == null)
                    {
                        //var id = node["delivery"].AsObject()["messageId"].ToString();
                        //if (id != string.Empty)
                        //{
                        var response = await GetMessageDeliveryStatusAsync(sgClient, notification.ArbitrationCaseId);
                        activity = (SendGridMessages)response;
                        if (activity.messages == null || activity.messages.Count == 0)
                            goto DelayNextRequest;

                        // since we cannot search the SendGrid API by messageId (they mangle it post-delivery), this becomes a ...
                        // TODO: Once we are using the full CSV download of message activity from SendGrid, we can go back to searching
                        // via our own "StartsWith" clause.
                        //var a = activity.messages.Where(d => d.msg_id.StartsWith(id) && d.status == "delivered");
                        //if (a.Count() > 0)
                        //{
                        var delivery = node["delivery"]!.AsObject();
                        // find the first delivery outside of our domains - that should* be the targeted recipient
                        var first = activity.messages.FirstOrDefault(d => !d.to_email.Contains("halomd", StringComparison.CurrentCultureIgnoreCase) && !d.to_email.Contains("mpower", StringComparison.CurrentCultureIgnoreCase));
                        if (first != null && first.last_event_time.HasValue)
                        {
                            delivery["deliveredOn"] = first.last_event_time.Value.ToUniversalTime();
                            var recipients = JsonSerializer.Serialize(activity.messages.Select(b => new { b.to_email, b.msg_id, b.clicks_count, b.last_event_time, b.status, b.opens_count }));
                            JsonNode? n = JsonNode.Parse(recipients);
                            delivery["recipients"] = n;
                            delivery["status"] = "delivered";
                            node["delivery"] = delivery;
                            notification.JSON = node.ToJsonString();
                            await SaveChangesToNotificationDeliveryAsync(notification);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($@"Unexpected error for ArbitrationCaseId {notification.ArbitrationCaseId}: {ex.Message} \n(Cooling down for 30 seconds...)", true);
                    // who knows what happened - have a cooldown
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            //}
            DelayNextRequest:
                if (activity != null)
                {
                    var h = activity.Headers!.FirstOrDefault(a => a.Key.Equals("x-ratelimit-remaining", StringComparison.CurrentCultureIgnoreCase));
                    if (h.Value.Any())
                    {
                        var x = h.Value.First();
                        var delay = string.IsNullOrEmpty(x) ? 30 : int.Parse(x) + 1;
                        LogMessage($@"CaseId {notification.ArbitrationCaseId} processing complete. (Delaying next API call for {delay} seconds...)", true);
                        await Task.Delay(TimeSpan.FromSeconds(delay));
                    }
                    else
                    {
                        LogMessage($@"CaseId {notification.ArbitrationCaseId} processing complete. (No x-ratelimit-remaining header!!! Delaying for 30 seconds...)", true);
                        await Task.Delay(TimeSpan.FromSeconds(30));
                    }
                }
            }
        }

        private async Task<int> RebuildNSANotifications()
        {

            if (!await GetArbAppTokenAsync())
                return -1;

            // setup sendgrid
            //var sgClient = new SendGridClient(SendGridApiKey);
            //sgClient.UrlPath = "messages"; // https://api.sendgrid.com/v3/messages

            // connect
            var ConnStr = GetArbitrationDbConnectionString();
            var contextOptions = new DbContextOptionsBuilder<ArbitrationDbContext>()
                .UseSqlServer(ConnStr)
                .Options;

            var context = new ArbitrationDbContext(contextOptions);
            context.Database.SetCommandTimeout(120);

            // fetch sent notifications that need proof of notification attached
            /*
                * select c.id, c.Authority, c.AuthorityStatus, c.CreatedOn, c.Customer, c.DOB, c.EOBDate, c.Entity, c.EntityNPI, c. FirstResponseDate
                , c.NSAStatus, p.NSARequestEmail, c.PatientName, c.Payor, c.PayorClaimNumber, c.ProviderName, c.ProviderNPI, c.ReceivedFromCustomer
                , c.[Service], c.ServiceDate, c.ServiceLine
                from dbo.arbitrationcases c 
                    inner join dbo.Notifications n on c.id=n.ArbitrationCaseId
                    inner join dbo.Payors p on c.PayorId = p.Id
                    left join (select ArbitrationCaseId from dbo.EMRClaimAttachments where IsDeleted=0 and DocType='ProofOfOpenNegotiation') a
                    on c.id=a.ArbitrationCaseId 
                where c.nsastatus like 'Submitted NSA%' 
                and c.isdeleted=0 
                and a.ArbitrationCaseId is null
                and n.SentOn > dateadd(day,-30,getdate()) 
                AND n.[Status]='success' 
                AND ISNULL(JSON_VALUE(n.JSON,'$.delivery.deliveredOn'),'') = '' 
                AND ISNULL(JSON_VALUE(n.JSON,'$.delivery.messageId'),'') <> ''
                * */
            int DAYS_AGO = -250;
            var Q = (from c in context.ArbitrationCases.Where(d => !d.IsDeleted && d.NSAStatus == "Submitted NSA Negotiation Request")
                     join a in context.EMRClaimAttachments.Where(d => !d.IsDeleted && d.DocType == "ProofOfOpenNegotiation")
                     on c.Id equals a.ArbitrationCaseId into docs
                     from b in docs.DefaultIfEmpty()
                     where b == null
                     join n in context.Notifications.Where(d => !d.IsDeleted && d.SentOn > DateTime.Today.AddDays(DAYS_AGO) && d.Status == "success")
                     on c.Id equals n.ArbitrationCaseId
                     join p in context.Payors
                     on c.PayorId equals p.Id
                     select new AppHealthDetail
                     {
                         Id = c.Id,
                         Authority = c.Authority,
                         AuthorityStatus = c.AuthorityStatus,
                         CreatedOn = c.CreatedOn,
                         Customer = c.Customer,
                         DOB = c.DOB,
                         Entity = c.Entity,
                         EntityNPI = c.EntityNPI,
                         EOBDate = c.EOBDate,
                         FirstResponseDate = c.FirstResponseDate,
                         NotificationJSON = n.JSON,
                         NSARequestEmail = p.NSARequestEmail,
                         NotificationReplyTo = n.ReplyTo,
                         NSAStatus = c.NSAStatus,
                         PatientName = c.PatientName,
                         Payor = c.Payor,
                         PayorClaimNumber = c.PayorClaimNumber,
                         ProviderName = c.ProviderName,
                         ProviderNPI = c.ProviderNPI,
                         Service = c.Service,
                         ServiceDate = c.ServiceDate,
                         ServiceLine = c.ServiceLine
                     });

            var claims = await Q.ToArrayAsync();
            var worklist = new List<AppHealthDetail>();
            foreach (var claim in claims.Where(j => !string.IsNullOrEmpty(j.NotificationJSON)))
            {
                var delivery = JsonNode.Parse(claim.NotificationJSON)!.AsObject()?["delivery"]?.AsObject();

                if (delivery != null)
                {
                    var status = delivery["status"]?.GetValue<string>();
                    var don = delivery["deliveredOn"]?.GetValue<DateTime?>();
                    var messageId = delivery["messageId"]?.ToString();
                    var lastMonth = DateTime.Now.AddDays(DAYS_AGO);
                    if (status == "delivered" && don.HasValue && !string.IsNullOrEmpty(messageId) && don > lastMonth) //status == "queued" || <- equivalent to brute forcing the proofs to be built
                    {
                        worklist.Add(claim);
                    }
                }
            }
            worklist.Add(claims.FirstOrDefault());
            // loop and build
            foreach (var item in worklist)
            {
                LogMessage($@"Building assets for ArbitrationCases.Id {item.Id}", true);

                // Build ProofOfOpenNegotiation
                string docType = Enum.GetName(CaseDocumentType.ProofOfOpenNegotiation) ?? "";
                var delivery = JsonNode.Parse(item.NotificationJSON!)!.AsObject()?["delivery"]?.AsObject();

                if (delivery != null && item.NotificationReplyTo != null)
                {
                    var from_email = item.NotificationReplyTo;
                    var subject = $@"{item.Customer} NSA Submission Request for {item.PayorClaimNumber}";
                    var pdf = await CreateProofOfNSAOpenNotificationRequestAsync(item.Id, subject, from_email!, item.NSARequestEmail);

                    if (pdf != null)
                    {
                        LogMessage("     Proof created. Attaching to claim...", true);
                        var winner = await AttachDocumentToArbitrationClaimAsync(item.Id, "ProofOfOpenNegotiation", pdf);
                        if (winner)
                        {
                            LogMessage("     Attaching to claim successfully!", true);
                        }
                        else
                        {
                            LogMessage("     Unable to attach document to claim!", true);
                        }
                    }
                    else
                    {
                        LogMessage("     Unable to create Proof PDF!", true);
                    }
                }
            }
            return 0;
        }


        private void ShowCatastrophicMessage(int id)
        {
            LogMessage($@"Unknown catastrophic failure when updating Notification for ArbitrationCaseId {id} !!!", true);
            LogMessage("This is likely a network error, a foreign service API issue or local permission issue.", true);
            LogMessage("Stopping all further processing to prevent the creation of duplicate notifications being sent !!!", true);
        }

        private void WriteConsoleErrors(Exception ex)
        {
            LogMessage(ex.Message, true);

            if (ex.InnerException != null)
            {
                LogMessage(ex.InnerException.Message, true);
            }
        }

        /// <summary>
        /// Appends statistics to the log. TODO: Write the log to the BLOB store and/or post the stats to 
        /// the API for inclusion in a dashboard, trigger alerts, etc.
        /// </summary>
        private void WriteStats()
        {
            string msg = $@"----------------------------------------\n----------- Final Statistics -----------\n----- Notifications Found: {_stats.TotalRecords}\n----- Successes: {_stats.Proccessed}\n----- Errors: {_stats.Failed}\n----------------------------------------";
            LogMessage(msg, true);
        }

        private static bool IsTokenExpired(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return true;
            }
            /***
             * Make string valid for FromBase64String
             * FromBase64String cannot accept '.' characters and only accepts stringth whose length is a multitude of 4
             * If the string doesn't have the correct length trailing padding '=' characters should be added.
             */
            int indexOfFirstPoint = token.IndexOf('.') + 1;
            String toDecode = token[indexOfFirstPoint..token.LastIndexOf('.')];
            while (toDecode.Length % 4 != 0)
            {
                toDecode += '=';
            }
            //Decode the string
            string decodedString = Encoding.ASCII.GetString(Convert.FromBase64String(toDecode));
            //Get the "exp" part of the string
            var regex = new Regex("(\"exp\":)([0-9]{1,})");
            var match = regex.Match(decodedString);
            long timestamp = Convert.ToInt64(match.Groups[2].Value);
            var date = new DateTime(1970, 1, 1).AddSeconds(timestamp);
            var compareTo = DateTime.UtcNow;
            int result = DateTime.Compare(date, compareTo);
            return result < 0;
        }
    }
}
