using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MPArbitration.Model;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Metadata;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Security.Principal;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Nodes;
using System.Runtime.Intrinsics.Arm;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace MPArbitration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class NotificationsController : MPBaseController
    {
        private readonly ILogger<NotificationsController> _logger;
        private readonly IImportDataSynchronizer _synchronizer;

        public NotificationsController(ILogger<NotificationsController> logger, ArbitrationDbContext context, IConfiguration configuration, IImportDataSynchronizer synchronizer) : base(context, configuration)
        {
            _logger = logger;
            _synchronizer = synchronizer;
        }

        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<Notification?>> GetByIdAsync(int id)
        {
            if (id < 1)
                return BadRequest();

            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                var n = await _context.Notifications.FindAsync(id);
                if (n == null || n.IsDeleted)
                    return NotFound();
                return n;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("queued")]
        [Produces("application/json")]
        public async Task<ActionResult<Notification?>> GetByClaimId([FromQuery]int c, [FromQuery]NotificationType t)
        {
            if (c < 1)
                return BadRequest("Bad Id");
            if (t == NotificationType.Unknown)
                return BadRequest("Invalid doc type");

            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                var notification = await _context.Notifications.FirstOrDefaultAsync(d => !d.IsDeleted && d.ArbitrationCaseId == c && d.NotificationType == t);
                if (notification == null)
                    return NotFound();
                return Ok(notification);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("sent")]
        [Produces("application/json")]
        public async Task<ActionResult<Notification?>> GetSentByClaimIdAsync([FromQuery] int c, [FromQuery] NotificationType t)
        {
            if (c < 1)
                return BadRequest("Bad Id");
            if (t == NotificationType.Unknown)
                return BadRequest("Invalid doc type");

            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                var n = await _context.Notifications.FirstOrDefaultAsync(d => !d.IsDeleted && d.ArbitrationCaseId == c && d.NotificationType == t && d.SentOn.HasValue && d.Status =="success");
                if (n == null)
                    return Ok(null);
                return Ok(n);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("undelivered")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetUnDeliveredAsync()
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsManager && !user.IsNegotiator && !user.IsSystem) 
                return Unauthorized("Access denied");

            try
            {

                var Q = _context.Set<Notification>().FromSqlRaw(@"SELECT Top 1000 [Id]
                                                                              ,[ArbitrationCaseId]
                                                                              ,[ApprovedBy]
                                                                              ,[ApprovedOn]
                                                                              ,[BCC]
                                                                              ,[CC]
                                                                              ,[HTML]
                                                                              ,[NotificationType]
                                                                              ,[To]
                                                                              ,[SentOn]
                                                                              ,[UpdatedBy]
                                                                              ,[UpdatedOn]
                                                                              ,[zEditor]
                                                                              ,[zEditedOn]
                                                                              ,[IsDeleted]
                                                                              ,[SubmittedBy]
                                                                              ,[SubmittedOn]
                                                                              ,[JSON]
                                                                              ,[Status]
                                                                              ,[Customer]
                                                                              ,[PayorClaimNumber]
                                                                              ,[ReplyTo]
                                                                              ,[AuthorityKey]
                                                                          FROM [dbo].[Notifications]
                                                                          WHERE IsDeleted=0 and ArbitrationCaseId > 0 
                                                                                AND [Status]='success' 
                                                                                AND ISNULL(JSON_VALUE(JSON,'$.delivery.deliveredOn'),'') = '' 
                                                                                AND ISNULL(JSON_VALUE(JSON,'$.delivery.messageId'),'') <> ''");
                var results = await Q.ToArrayAsync();
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("unsent")]
        [Produces("application/json")]
        public async Task<ActionResult<Notification?>> GetUnsentAsync([FromQuery] string? customer = null, [FromQuery] NotificationType t = NotificationType.Unknown, bool NoHTML = false)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var allowedCustomerIDs = new List<int>();
            List<string> allowedCustomerNames = new List<string>();
            bool allCustomers = user.HasGlobalCaseRole || user.IsSystem;

            if (!allCustomers)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer && (x.AccessLevel == UserAccessType.manager || x.AccessLevel == UserAccessType.negotiator)).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }

            try
            {
                var q = from c in _context.Set<ArbitrationCase>().Where(d => !d.IsDeleted && (customer == null || d.Customer == customer)
                                                                            && (allCustomers || allowedCustomerNames.Contains(d.Customer))).Include(n => n.Notifications)
                        from nn in _context.Set<Notification>().Where(d => !d.IsDeleted
                                                                            && c.Id == d.ArbitrationCaseId
                                                                            && d.SentOn == null
                                                                            && d.Status == "pending" // TODO: Do we need this? -> d.Status == "failed"
                                                                            && (t == NotificationType.Unknown || d.NotificationType == t))
                        select nn;

                Notification[] results;

                if (NoHTML)
                {
                    results = q.Select(x => new Notification()
                    {
                        Id = x.Id,
                        ArbitrationCaseId = x.ArbitrationCaseId,
                        ApprovedBy = x.ApprovedBy,
                        ApprovedOn = x.ApprovedOn,
                        BCC = x.BCC,
                        To = x.To,
                        CC = x.CC,
                        ReplyTo = x.ReplyTo,
                        JSON = x.JSON,
                        NotificationType = x.NotificationType,
                        PayorClaimNumber = x.PayorClaimNumber,
                        SentOn = x.SentOn,
                        Status = x.Status,
                        SubmittedBy = x.SubmittedBy,
                        SubmittedOn = x.SubmittedOn,
                        UpdatedOn = x.UpdatedOn,
                        UpdatedBy = x.UpdatedBy,
                    }).ToArrayAsync().Result;
                } else
                {
                    results = q.ToArrayAsync().Result;
                }
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Notification>> DeleteNotificationAsync(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (id < 1)
                return BadRequest("Bad id");

            if (user.HasGlobalCaseRole && !user.IsManager && !user.IsNegotiator && !user.IsSystem)
                return Unauthorized("Insufficient privileges to create a Notification");

            var notification = await _context.Notifications.FirstOrDefaultAsync(d => d.Id == id && d.IsDeleted == false && !d.SentOn.HasValue && d.Status == "pending");
            if (notification == null)
                return NotFound("No active Notification found for that Id.");
            try
            {
                notification.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                return ex.InnerException == null ? BadRequest(ex.Message) : BadRequest(ex.InnerException.Message);
            }

            return notification;
        }

        /// <summary>
        /// Only allows limited updating of the JSON data, not the other DB columns.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPut]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> UpdateNotificationAsync(Notification obj)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (obj.Id < 1)
                return NotFound();

            if (!user.IsManager && !user.IsNegotiator && !user.IsSystem)
                return Unauthorized("Insufficient privileges to update a Notification");

            try
            {
                var notification = await _context.Notifications.FindAsync(obj.Id);

                if (notification == null)
                    return NotFound();

                var node = JsonNode.Parse(obj.JSON);
                if (node == null || !node.AsObject().ContainsKey("delivery"))
                    return BadRequest();

                var delivery = node.AsObject()["delivery"]!.AsObject();

                var current = JsonNode.Parse(notification.JSON);
                if (current == null)
                    current = JsonNode.Parse("{}");

                current!["delivery"] = JsonNode.Parse(delivery.ToJsonString());

                notification.JSON = current.ToJsonString();
                notification.UpdatedBy = user.Email;
                notification.UpdatedOn = Utilities.GetCurrentUtcDate();
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("batch")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<String>> BatchQueueNotificationsAsync(Notification[] Notifications)
        {
            
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active user profile!");

            if (user.IsSystem && !user.Email.Contains("@"))
                return BadRequest("The system user cannot batch Notifications without a valid email address.");

            var log = new StringBuilder();
            log.AppendLine($@"{Utilities.GetCurrentCSTDate2().ToString("R")} (CST): Begin validating Notification requests...");
            int TotalErrors = 0;

            // Cache values that will be used multiple times
            await _synchronizer.EnsureAuthorities();
            await _synchronizer.EnsureCustomers();

            var NSAAuthority = _synchronizer.Authorities.FirstOrDefault(d => d.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase));
            
            var CachedNotifications = await _context.Notifications.Where(d => !d.IsDeleted && !d.SentOn.HasValue).Select(d => new Notification {ArbitrationCaseId = d.ArbitrationCaseId, NotificationType = d.NotificationType}).AsNoTracking().ToArrayAsync();
            var CachedPayors = new List<Payor>();
            var CachedAppVariables = await _context.CalculatorVariables.Where(x => x.CreatedOn <= Utilities.GetCurrentUtcDate()).ToArrayAsync();

            foreach (var notification in Notifications)
            {
                var Validation = "";

                // TODO: The following assumes we are dealing with an NSA notification that uses data from an ArbitrationCase aka a "claim"
                // To support, say, AuthorityDispute notifications, the following would need to be relocated into its own function.
                // Another approach would be to sub-class the Notifications into ClaimNotification and DisputeNotification...since Disputes can reference multiple claims.

                var ArbCase = await _context.ArbitrationCases.Include(d => d.CPTCodes).AsNoTracking().FirstOrDefaultAsync(d => d.Id == notification.ArbitrationCaseId);
                if (ArbCase == null)
                {
                    Validation = "Unable to locate ArbitrationCase";
                }
                else
                {
                    var Payor = CachedPayors.FirstOrDefault(d => d.Id == ArbCase.PayorId); // Payors are expensive to deal with so we build the cache as we go
                    if (Payor == null)
                    {
                        Payor = await _context.Payors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == ArbCase.PayorId);
                        if (Payor != null)
                        {
                            CachedPayors.Add(Payor);
                        }
                    }

                    Validation = Utilities.ValidateNotificationRequest(notification, ArbCase, CachedNotifications, _synchronizer.Customers, CachedPayors, CachedAppVariables, _synchronizer.Authorities);
                    
                }

                if (!string.IsNullOrEmpty(Validation))
                {
                    log.AppendLine($@"Validation failed for ArbitrationCase {notification.ArbitrationCaseId} : {Validation}.");
                    TotalErrors += 1;
                }
                else
                {
                    notification.ArbitrationCase = ArbCase;
                    notification.ArbitrationCase!.PayorEntity = CachedPayors.First(d => d.Id == ArbCase!.PayorId);
                    notification.ArbitrationCase.NSAAuthority = NSAAuthority;
                    notification.ArbitrationCase.StateAuthority = _synchronizer.Authorities.FirstOrDefault(d => d.Key.Equals(ArbCase!.Authority, StringComparison.CurrentCultureIgnoreCase));
                }
            }

            log.AppendLine($@"{Utilities.GetCurrentCSTDate2().ToString("R")} (CST): Validation analysis complete.");
            if (TotalErrors > 0)
            {
                log.AppendLine($@"{TotalErrors} errors detected.");
                log.AppendLine("NO NOTIFICATIONS WERE QUEUED FOR DELIVERY!!!");
                log.AppendLine("Recommendation: Fix the indicated errors or exclude the invalid claim(s) from the bulk request and try again.");
                return UnprocessableEntity(log.ToString());
            }

            string FullUserName = GetUsername();

            var task = Task.Factory.StartNew(() =>
            {
                _synchronizer.BatchQueueNotificationsAsync(Notifications, user, FullUserName);
            });
            
            return Ok($@"Your request to send {Notifications.Length} Notifications was successfully received by the Arbit API engine. All associated claims appear to be valid. After all records are added to the delivery queue, a status report will be sent to {user.Email}. Any Notifications that could not be queued will be listed in the report. You will need to manually fix them and re-queue each one.");
        }

        [HttpPost("")]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Notification>> CreateNotificationAsync(Notification obj, bool RebuildPDFsOnly = false)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            
            if (obj.Id != 0)
                return BadRequest("New Notifications cannot have a non-zero id value!");

            if (obj.ArbitrationCaseId < 1)
                return BadRequest("Invalid ArbitrationCaseId");

            // TODO: The following will need to expand later and all of the Notification logic will move to a business object
            if (obj.NotificationType != NotificationType.NSANegotiationRequest)
                return BadRequest("Unsupported NotificationType"); 

            var allCustomers = await _context.Customers.ToArrayAsync();

            var allowedCustomerIDs = new List<int>();
            List<string> allowedCustomerNames = new List<string>();
            if (!user.HasGlobalCaseRole && !user.IsSystem)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer && (x.AccessLevel == UserAccessType.manager || x.AccessLevel == UserAccessType.negotiator)).Select(x => x.EntityId));
                allowedCustomerNames = allCustomers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToList();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }

            if (user.HasGlobalCaseRole && !user.IsManager && !user.IsNegotiator && !user.IsSystem)
                return Unauthorized("Insufficient privileges to create Notifications");

            
            try
            {
                // Lots of validations. Notice we do not use the Utilities.ValidateNotification method because that one is geared around batching and requires a lot of pre-loaded data.
                // TODO: Implement the webserver cache to have access to the rarely-changing collections of customers, authorities, payors, etc.
                var arbCase = await _context.ArbitrationCases.Include(d => d.CPTCodes.Where(j => j.IsIncluded)).FirstOrDefaultAsync(d => d.Id == obj.ArbitrationCaseId);
                
                if (arbCase == null || arbCase.IsDeleted)
                    return NotFound("The Case record was not found.");
                
                if (!user.HasGlobalCaseRole && !allowedCustomerNames.Contains(arbCase.Customer))
                    return Unauthorized("Insufficient privileges to create a Notification for that Case");

                if (string.IsNullOrEmpty(arbCase.Customer))
                    return BadRequest("The Case record is not assigned to a Customer.");

                if (string.IsNullOrEmpty(arbCase.PayorClaimNumber))
                    return BadRequest("The Case record does not have a PayorClaimNumber");

                if (obj.IsDeleted)
                    return BadRequest("Cannot create a deleted Notification!");

                var check = await _context.Notifications.FirstOrDefaultAsync(d => d.ArbitrationCaseId == obj.ArbitrationCaseId && !d.IsDeleted && d.NotificationType == obj.NotificationType && !d.SentOn.HasValue);
                if (check != null)
                    return BadRequest("An unsent notification is already queued up for that ArbitrationCase record. Delete the existing one before adding a new one.");

                if (arbCase.PayorId < 1)
                    return NotFound("No Payor is associated with the ArbitrationCase. No one to notify!");

                var payor = await _context.Payors.FindAsync(arbCase.PayorId);
                if (payor == null)
                    return NotFound("Unable to locate associated Payor record!");

                if (string.IsNullOrEmpty(payor.NSARequestEmail))
                    return NotFound("Invalid or missing NSARequestEmail for Payor.");

                var customer = allCustomers.FirstOrDefault(d => d.Name.Equals(arbCase.Customer, StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(d.JSON));
                if (customer == null)
                    return BadRequest("Invalid Customer");

                JsonNode? node = JsonNode.Parse(customer.JSON);
                string nsaReplyTo = node == null || node["NSAReplyTo"] == null ? "" : node["NSAReplyTo"]!.ToString();
                if (string.IsNullOrEmpty(nsaReplyTo))
                    return BadRequest("Customer is not configured properly. NSAReplyTo is missing!");

                var template = Utilities.GetDocumentTemplate(obj.NotificationType, payor);
                if (string.IsNullOrEmpty(template))
                    return NotFound("No template found for Payor");

                // get latest CalculatorVariable settings for the case's service line
                var asOf = Utilities.GetCurrentUtcDate();

                var filter = from r in _context.CalculatorVariables.Where(x => x.CreatedOn <= asOf && x.ServiceLine == arbCase.ServiceLine)
                             group r by r.ServiceLine into op
                             select op.OrderByDescending(x => x.CreatedOn).First();

                var calcVars = await filter.FirstOrDefaultAsync();
                if (calcVars == null || string.IsNullOrEmpty(calcVars.NSAOfferBaseValueFieldname))
                    return NotFound("Invalid or missing global app settings. Try updating the Calculator Variables and re-selecting the NSA Offer Base Value Field.");

                var nsaAuth = await _context.Authorities.Include(d => d.TrackingDetails.Where(g => !g.IsDeleted)).FirstOrDefaultAsync(h => h.Key == "nsa");
                var auth = await _context.Authorities.Include(d => d.TrackingDetails.Where(g => !g.IsDeleted)).Include(d => d.Benchmarks).ThenInclude(g => g.BenchmarkDataset).FirstOrDefaultAsync(h => h.Key == arbCase.Authority);
                if (auth == null || nsaAuth == null) 
                    return NotFound("Invalid or missing Authority used for calculating benchmarks and deadlines"); // NOTE: The benchmarks used to calculate a "fair offer" vary by jurisdiction. Jurisdiction is tied to the local Authority.

                // Get CPT code descriptions
                if (arbCase.CPTCodes.Count() > 0)
                {
                    foreach (var t in arbCase.CPTCodes)
                    {
                        var procCode = await _context.ProcedureCodes.FirstOrDefaultAsync(d => d.Code == t.CPTCode);
                        if (procCode != null)
                        {
                            t.Description = procCode.Description.Replace("&", "&amp;").Replace("<","&lt;").Replace(">","&gt;");
                        }
                    }
                }

                // update a copy of the tracking data before referencing those values in the templates
                if (nsaAuth.TrackingDetails.Count > 0 && !string.IsNullOrEmpty(arbCase.NSATracking) && obj.NotificationType == NotificationType.NSANegotiationRequest)
                {
                    await Extensions.EnsureHolidays(_context);
                    arbCase.NSATracking = Utilities.SetTrackingValue(Utilities.GetCurrentUtcDate(), nsaAuth.TrackingDetails, arbCase.NSATracking, "DateNegotiationSent", arbCase);
                }
                
                obj.HTML = Utilities.MergeTemplateData(template, obj.NotificationType, arbCase, calcVars, auth, nsaReplyTo, _logger); // the main notification email

                // TODO: Insert some REGEX validation here such as detecting the BLANK constant $___ or unresolved tokens. Those are bad!

                var others = Utilities.GetDocumentTemplates(NotificationType.NSANegotiationRequestAttachment, payor); // supplemental content
                
                var supplements = new List<NotificationDocument>();

                // move this config option to the MPNotify logic once we figure out how/where to set up the preference grid 
                // -> bool template.includeInline = false; // default is to make it an attachment

                bool makePDFs = true; // TODO: Future use - at one point they wanted the other attachments rendered in-line with the rest of the message body so they could decide to do it again or make ia a per-payor preference

                foreach (var t in others)
                {
                    var html = Utilities.MergeTemplateData(t.HTML, t.NotificationType, arbCase, calcVars, auth, nsaReplyTo, _logger);
                    if (makePDFs)
                    {
                        var pdf = NRecoPdfWrapper.GeneratePDF(_logger, html, new Dictionary<string, string>(), new Dictionary<string, string>(), out string problems);

                        if (pdf?.Length > 0) // NOTE! If the returned stream is null then there's something seriously wrong b/c the object has good fail-over
                        {
                            using (var stream = new MemoryStream(pdf))
                            {
                                var message = await SaveClaimBLOB(stream, arbCase, CaseDocumentType.NSARequestAttachment, t.Name + ".pdf", GetUsername());
                                if (!string.IsNullOrEmpty(message))
                                    return BadRequest(message);
                            }
                        }
                        else if (!string.IsNullOrEmpty(problems))
                        {
                            return Problem(problems);
                        }
                    }
                    else
                    {
                        supplements.Add(new NotificationDocument() { ArbitrationCaseId = obj.ArbitrationCaseId, HTML = html, JSON = "{}", Name = t.Name, NotificationType = t.NotificationType });
                    }
                }

                if (!RebuildPDFsOnly)
                {
                    var a = new
                    {
                        payorId = payor.Id,
                        supplements = supplements
                    };

                    if (obj.NotificationType == NotificationType.NSANegotiationRequest)
                        obj.AuthorityKey = "nsa";
                    else
                        obj.AuthorityKey = arbCase.Authority;

                    obj.Customer = arbCase.Customer;
                    obj.JSON = JsonSerializer.Serialize(a);
                    obj.PayorClaimNumber = arbCase.PayorClaimNumber;
                    obj.Status = "pending";
                    obj.SubmittedBy = user.Email;
                    obj.SubmittedOn = Utilities.GetCurrentUtcDate();
                    obj.UpdatedBy = user.Email;
                    obj.UpdatedOn = Utilities.GetCurrentUtcDate();
                    obj.ReplyTo = nsaReplyTo;
                    obj.To = payor.NSARequestEmail; // validate above

                    if (Utilities.IsValidEmail(user.Email) && !obj.To.Equals(user.Email, StringComparison.CurrentCultureIgnoreCase))
                        obj.CC = nsaReplyTo + ";" + user.Email;
                    else
                        obj.CC = nsaReplyTo;
#if DEBUG
                    obj.BCC = "developer.email@HaloMD.com";
#endif

                    _context.Notifications.Add(obj);
                    await _context.SaveChangesAsync();
                }

                return Ok(obj);
            } 
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("ReGen")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MergeAllDuplicatesAsync()
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Problem("No active User context!");
            if (!user.Email.Equals("developer.email@HaloMD.com", StringComparison.CurrentCultureIgnoreCase))
                return Unauthorized("Method reserved for super-administrator");

            var log = new StringBuilder();
            log.AppendLine($@"Began run: {DateTime.Now.ToShortTimeString()}");

            try
            {
                var worklist = ""; // 82311,82547,82373,83172,82821,84017,82412,82916,82382,82409,83965,82405,82178,82392,85169,85170,82175,82698,82865,82539,85227,85227,84047,85210,88104,85315,85315,82401,82624,82360,84116,84019,85716,85174,82357,82356,82683,82685,82868,85547,84154,84140,82266,82376,82369,84016,82470,86214,85178,83994,83412,83811,83417,80917,83722,85186,82394,83774,84045,85176,83001,82475,84236,82155,82172,83532,85151,84058,85152,85153,82623,85753,83063,85270,86210,85272,86212,84921,85269,85183,85201,82337,83973,81810,81808,81814,81094,85195,84998,84962,84966,85324,85207,85207,83871,70006,85196,82399,83857,85192,82031,81954,81959,82048,81995,81979,81979,82004,82035,82035,82035,82008,82008,83754,81972,82052,81998,81962,82037,82049,81947,82006,81956,85410,84053,86986,82183,83203,80552,82247,82250,82158,82157,85216,82390,85220,82391,82370,83005,82989,82420,84277,82555,82530,84272,84326,85224,84314,83785,82506,82945,82498,85228,82182,83921,82473,82453,85290,82830,82483,85291,82490,85738,84628,82487,83706,84631,82516,85881,83728,82486,82485,83782,82489,82909,83064,82165,82488,82459,82171,82813,82169,82151,82823,82180,82159,83103,83061,83988,82477,82902,82507,85959,85967,83916,82847,82873,85969,85969,82794,82801,83530,83514,85204,82853,85331,83672,85254,83779,83007,85318,85307,85323,82819,83044,82617,84213,82633,82611,82604,82585,82616,82573,82608,82641,82613,82615,82643,82610,82609,82609,82642,82636,82577,82619,82644,82612,82634,82614,82614,82635,84988";
                var targets = worklist.Split(',');

                foreach( var target in targets)
                {
                    log.Append($@"Regenerating {target}...");
                    var notification = new Notification { ArbitrationCaseId = Int32.Parse(target), Id = 0, NotificationType = NotificationType.NSANegotiationRequest, To = "developer.email@mPowerHealth.com", ReplyTo = "NoReply@mPowerHealth.com" };
                    await this.CreateNotificationAsync(notification, true);
                    log.AppendLine("Done");
                }
                
            }
            catch (Exception ex)
            {
                log.AppendLine("***********************************************");
                log.AppendLine(ex.Message);
                log.AppendLine("***********************************************");
            }

            log.AppendLine($@"End run: {DateTime.Now.ToShortTimeString()}");
            return Ok(log.ToString());
        }

        private async Task<string> SaveClaimBLOB(MemoryStream stream, ArbitrationCase arbCase, CaseDocumentType cdt, string filename, string uploadedBy)
        {
            
            string blobName = $@"{arbCase.Id}-{cdt.ToString().ToLower()}-{filename.ToLower()}";

            try
            {
                BlobClient blob = _containerClient.GetBlobClient(blobName);
                var response = await blob.UploadAsync(stream, true); // true = overwrite
                var raw = response.GetRawResponse();
                if (raw.ReasonPhrase != "Created")
                {
#if DEBUG
                    return "Unexpected result from document store: " + raw.ReasonPhrase;
#else
                    return "Unexpected result from document store";
#endif
                }

                // add tags to new BLOB
                var tags = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(arbCase.AuthorityCaseId))
                    tags.Add("AuthorityCaseId", arbCase.AuthorityCaseId);
                tags.Add("Id", arbCase.Id.ToString());
                tags.Add("UpdatedBy", uploadedBy);
                tags.Add("DocumentType", cdt.ToString().ToLower());
                if (!string.IsNullOrEmpty(arbCase.EHRNumber))
                    tags.Add("EHRNumber", arbCase.EHRNumber);
                blob.SetTags(tags);
                var blobURL = $@"{_containerClient.Uri.ToString()}/{blobName}";
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
    }
}
