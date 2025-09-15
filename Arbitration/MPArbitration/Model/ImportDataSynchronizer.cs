using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Configuration;
using ObjectsComparator.Comparator.Helpers;
using SendGrid;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using System.Configuration;

namespace MPArbitration.Model
{
    #region Enums, Structures Interfaces and Utility classes
    public enum DisputeRecordType
    {
        Detail,
        Fee,
        Header
    }

    public enum EHRRecordType
    {
        Detail,
        Header
    }

    public interface IEHRKey
    {
        string Customer { get; set; }
        DateTime? DOB { get; set; }
        string ProviderNPI { get; set; }
        string PatientName { get; set; }
        string PayorClaimNumber { get; set; }
        DateTime? ServiceDate { get; set; }
//        DateTime? EOBDate { get; set; }

    }

    public class ClaimLocator : IEHRKey
    {
        public string Customer { get; set; } = "";
        public DateTime? DOB { get; set; } = null;
        public string ProviderNPI { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string PayorClaimNumber { get; set; } = "";
        public DateTime? ServiceDate { get; set; } = null;
//        public DateTime? EOBDate { get; set; } = null;

    }

    public interface IEHRRecord
    {
        string Authority { get; set; }
        string AuthorityCaseId { get; set; }
        string Customer { get; set; }
        DateTime? DOB { get; set; }
        string EHRNumber { get; set; }
        //string EntityNPI { get; set; }
        string ProviderNPI { get; set; }
        string LocationGeoZip { get; set; }
        string NSACaseId { get; set; }
        string PatientName { get; set; }
        string Payor { get; set; }
        string PayorClaimNumber { get; set; }
        string Service { get; set; }
        DateTime? ServiceDate { get; set; }
        string ServiceLine { get; set; }
    }

    public class CaseUtility : IEHRKey
    {
        public string Customer { get; set; } = "";
        public DateTime? DOB { get; set; } = null;
        public string ProviderNPI { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string PayorClaimNumber { get; set; } = "";
        public string Payor { get; set; } = "";
        public int? PayorId { get; set; } = null;
        public DateTime? ServiceDate { get; set; } = null;
        //public DateTime? EOBDate { get; set; } = null;
    }

    public class ImportDetail : IEHRKey, IEHRRecord
    {
        public string Authority { get; set; } = "";
        public string AuthorityCaseId { get; set; } = "";
        public string CPTCode { get; set; } = "";
        public string Customer { get; set; } = "";
        public DateTime? DOB { get; set; } = null;
        public string EHRNumber { get; set; } = "";
        //public string EntityNPI { get; set; } = "";
        public string ProviderNPI { get; set; } = "";
        public bool IsEligible { get; set; } = false;  // this comes in from the EHR system and is copied to the IsIncluded value by the ImportDataSynchronizer - not ideal and probably needs re-thinking
        public bool IsIncluded { get; set; } = false;  // the UI will only ever alter this value and it won't get overridden by the EHR import process
        public string LocationGeoZip { get; set; } = "";
        public string Modifiers { get; set; } = "";
        //public bool Modifier26_YN { get; set; } = false;
        public string NSACaseId { get; set; } = "";
        public double PaidAmount { get; set; } = 0;
        public string PatientName { get; set; } = "";
        public double PatientRespAmount { get; set; } = 0;
        public string Payor { get; set; } = "";
        public string PayorClaimNumber { get; set; } = "";
        public double ProviderChargeAmount { get; set; } = 0;
        public string Service { get; set; } = "";
        public DateTime? ServiceDate { get; set; } = null;
//        public DateTime? EOBDate { get; set; } = null;
        public string ServiceLine { get; set; } = "";
        public double Units { get; set; } = 0;
        public bool Modifier26_YN()
        {
            if (string.IsNullOrEmpty(Modifiers))
                return false;

            return Modifiers.Split(new char[] { ',', ';' }).Contains("26");
        }
    }

    public struct ObjectChangeResult
    {
        public ObjectChangeResult()
        {
            WasChanged = false;
            ErrorMessage = "";
        }
        public bool WasChanged;
        public string ErrorMessage;
    }

    public struct RecordImportResults
    {
        public RecordImportResults(int caseId, RecordImportActionResult result)
        {
            this.ArbitrationCaseId = caseId;
            this.RecordImportResult = result;
        }
        public int ArbitrationCaseId;
        public RecordImportActionResult RecordImportResult;
    }

    public enum RecordImportActionResult
    {
        Added,
        Error,
        Skipped,
        Updated
    }

    public struct SyncObjectDataResult
    {
        public SyncObjectDataResult()
        {
            WereChangesMade = false;
            ChangesJSON = "{}";
        }
        public bool WereChangesMade;
        public string ChangesJSON;
    }

    public enum ValueCopyResult
    {
        Error,
        Success,
        UnknownType
    }

    // Interface allows the dependency injector (service locater) to provide controllers an instance 

    #endregion
    /// <summary>
    /// 
    /// </summary>
    public class ImportDataSynchronizer : IImportDataSynchronizer
    {
        //private readonly string CACHE_KEY = "uploadInProgress";
        //private readonly string BENCHMARK_CACHE_KEY = "benchmarkUploadInProgress";
        private readonly ILogger<ImportDataSynchronizer> _logger;
        private readonly ArbitrationDbContext _context;
        private readonly ArbitrationDbContext _errorContext;  // separate context for logging errors - allows saving independent of the default context
        private readonly DisputeIdrDbContext _idr_context;
        IConfiguration _configuration;
        IPrincipal _principal;
        private SendGridClient? SgClient = null;


        /// <summary>
        /// 
        /// </summary>
        public List<Authority> Authorities { get; set; } = new List<Authority>();

        /// <summary>
        /// 
        /// </summary>
        public List<CalculatorVariable> CalculatorVariables { get; set; } = new List<CalculatorVariable>();

        /// <summary>
        /// 
        /// </summary>
        public List<Customer> Customers { get; set; } = new List<Customer>();
        /// <summary>
        /// 
        /// </summary>
        public List<Payor> Payors { get; set; } = new List<Payor>();

        /// <summary>
        /// 
        /// </summary>
        public List<ProcedureCode> ProcedureCodes { get; set; } = new List<ProcedureCode>();

        private static bool AUTO_ADD_ENTITIES = true;
        private static bool AUTO_ADD_PAYORS = true;
        private string DEFAULT_PAYOR_JSON = System.Configuration.ConfigurationManager.AppSettings["DEFAULT_PAYOR_JSON"] ?? "";
        private string DEFAULT_ENTITY_ADDRESS = System.Configuration.ConfigurationManager.AppSettings["DEFAULT_ENTITY_ADDRESS"] ?? "";
        private string DEFAULT_ENTITY_CITY = System.Configuration.ConfigurationManager.AppSettings["DEFAULT_ENTITY_CITY"] ?? "";
        private string DEFAULT_ENTITY_STATE = System.Configuration.ConfigurationManager.AppSettings["DEFAULT_ENTITY_STATE"] ?? "";
        private string DEFAULT_ENTITY_ZIP = System.Configuration.ConfigurationManager.AppSettings["DEFAULT_ENTITY_ZIP"] ?? "";
        private static string SendGridApiKey = System.Configuration.ConfigurationManager.AppSettings["SendGridApiKey"] ?? "";
        private static string FromAddress = System.Configuration.ConfigurationManager.AppSettings["FromAddress"] ?? "";
        private static string ReplyToAddress = System.Configuration.ConfigurationManager.AppSettings["ReplyToAddress"] ?? "";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="principal"></param>
        public ImportDataSynchronizer(ILogger<ImportDataSynchronizer> logger, IConfiguration configuration, IPrincipal principal)
        {
            //_memoryCache = cache;
            _principal = principal;
            _configuration = configuration;
            var contextOptions = new DbContextOptionsBuilder<ArbitrationDbContext>()
                .UseSqlServer(configuration.GetSection("ConnectionStrings").GetSection("ConnStr").Value)
                .Options;

            _context = new ArbitrationDbContext(contextOptions);
            _context.Database.SetCommandTimeout(TimeSpan.FromSeconds(120));
            _errorContext = new ArbitrationDbContext(contextOptions);
            _errorContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(60));

            _logger = logger; // this doesn't have the same scoped limitation so we let DI give it to us

            var idr_contextOptions = new DbContextOptionsBuilder<DisputeIdrDbContext>()
             .UseSqlServer(configuration.GetSection("ConnectionStrings").GetSection("IDRConnStr").Value)
             .Options;

            _idr_context = new DisputeIdrDbContext(idr_contextOptions);
            _idr_context.Database.SetCommandTimeout(TimeSpan.FromSeconds(120));

            // NOTE: If we use DI to inject a DbContext, .net will inject the same instance it gave to the controller (scoped instance).
            // This will destroy the DbContext before this task can complete. Since I don't want the calling client
            // to wait around for the method (below) to complete, I am not using await in the caller.
            // So, for now, manually setup new instance is an easy way to go. The "proper" way is to implement our own
            // async service with its own scope. Crazy amount of code for something so simple.
            // See: https://docs.microsoft.com/en-us/dotnet/core/extensions/scoped-service

            /*
             * _context = context;
             */
        }

        /// <summary>
        /// 
        /// </summary>
        ~ImportDataSynchronizer()
        {
            if (_context != null)
                _context.Dispose();

            if (_errorContext != null)
                _errorContext.Dispose();

            if (_idr_context != null)
                _idr_context.Dispose();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="records"></param>
        /// <param name="currentUser"></param>
        /// <param name="currentJob"></param>
        /// <exception cref="Exception"></exception>
        public async void ImportIDRDisputeDetailsAsync(IEnumerable<DisputeCPT> records, AppUser currentUser, JobQueueItem? currentJob)
        {
            if (currentJob != null)
            {
                _errorContext.Entry(currentJob).State = EntityState.Unchanged;
            }

            //            var NewRecords = records.Where(d => !string.IsNullOrEmpty(d.AuthorityKey) && !string.IsNullOrEmpty(d.AuthorityCaseId) && !string.IsNullOrEmpty(d.ClaimCPTCode)).ToImmutableArray();
            int RecordCount = 1; // NewRecords.Count();

            if (RecordCount == 0)
                throw new Exception("No valid Dispute Detail records found to process!");

            int RecordsProcessed = 0;
            int RecordsSkipped = 0;
            int RecordsError = 0;
            var BatchUploadDate = Utilities.GetCurrentUtcDate();

            var log = new StringBuilder();

            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Begin loading Dispute Details...");
            log.AppendLine();

            try
            {
                //await EnsureAuthorities();
                //var NSAAuthority = Authorities.FirstOrDefault(d => d.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase))!;
                //if (NewRecords.Count(d => d.AuthorityKey.Equals("nsa", StringComparison.CurrentCultureIgnoreCase)) > 0)
                //{
                //    log.AppendLine("WARNING: Non-NSA data detected. If you are intentionally uploading non-NSA records you can ignore this warning. Otherwise, verify the Authority Id in your upload file!!!");
                //}

                //var CalcVars = await Utilities.GetCalculatorVariablesAsync(_context);

                //// Make a cache for speed
                //var DisputeNumbers = await (from d in _context.AuthorityDisputes
                //                            join a in _context.Authorities on d.AuthorityId equals a.Id
                //                            select new { d.Id, d.Authority, AuthorityId = a.Id, d.AuthorityCaseId }).ToArrayAsync();

                //// Group the CPTs by Authority case identifier
                //var ItemGroups = NewRecords.GroupBy(c => new { c.AuthorityKey, c.AuthorityCaseId }).Select(g => new { g.Key.AuthorityKey, g.Key.AuthorityCaseId }).ToArray();

                //foreach (var Disputes in ItemGroups)
                //{
                //    var AuthorityKey = Disputes.AuthorityKey;
                //    var AuthorityObj = Authorities.FirstOrDefault(d => d.Key.Equals(AuthorityKey, StringComparison.CurrentCultureIgnoreCase));
                //    var AuthorityCaseId = Disputes.AuthorityCaseId;
                //    var CPTs = NewRecords.Where(d => d.AuthorityKey.Equals(AuthorityKey, StringComparison.CurrentCultureIgnoreCase) && d.AuthorityCaseId == AuthorityCaseId);
                //    RecordsProcessed += CPTs.Count();
                //    bool HasChanges = false;

                //    // Ensure accurate Authority import
                //    if (AuthorityObj == null)
                //    {
                //        RecordsSkipped += CPTs.Count();
                //        log.AppendLine($@"{BatchUploadDate.ToString("R")}: ERROR: Unknown AuthorityKey {AuthorityKey}. Skipping {CPTs.Count()} CPTs.");
                //        continue;
                //    }

                //    var AuthorityId = AuthorityObj.Id;

                //    // Ensure Dispute Header already exists
                //    var Check = DisputeNumbers.FirstOrDefault(v => v.AuthorityId == AuthorityId && v.AuthorityCaseId.Equals(AuthorityCaseId, StringComparison.CurrentCultureIgnoreCase));
                //    if (Check == null)
                //    {
                //        RecordsSkipped += CPTs.Count();
                //        log.AppendLine($@"{BatchUploadDate.ToString("R")}: Dispute Header {AuthorityCaseId} could not be found for Authority {AuthorityId}. Skipping!");
                //        continue;
                //    }
                //}
            }
            catch (Exception ex)
            {
                string Message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;

                await ImportUtils.UpdateJob(_errorContext, currentJob, "Job complete.", "error", RecordsProcessed, RecordCount, Message);
                log.AppendLine($@"{BatchUploadDate.ToString("R")}: Unexpected error while processing.");
                log.AppendLine(Message);
            }


            //
            finally
            {
                // update real-time status record
                if (currentJob != null)
                {
                    await ImportUtils.UpdateJob(_errorContext, currentJob, $@"Importing Dispute Details records", "importing", RecordsProcessed, RecordsError, RecordsProcessed, RecordsSkipped, RecordCount, 0);
                }
            }

            await ImportUtils.UpdateJob(_errorContext, currentJob, "Job complete.", "finished");
            //
        }



        /// <summary>
        /// Creates a new ArchiveCases record and resets the AuthorityCaseId and AuthorityStatus and AuthorityWorkflowStatus fields.
        /// </summary>
        /// <param name="originalCase"></param>
        /// <param name="user"></param>
        /// <param name="au"></param>
        /// <param name="resetOriginal"></param>
        /// <param name="saveInstantly"></param>
        /// <returns></returns>
        public async Task<string> ArchiveCaseAsync(ArbitrationCase originalCase, AppUser user, Authority? au = null, bool resetOriginal = true, bool saveInstantly = false)
        {
            string msg = "";
            try
            {
                if (au == null)
                {
                    if (string.IsNullOrEmpty(originalCase.Authority))
                    {
                        msg = $@"Authority not specified on ArbitrationCase.Id {originalCase.Id}. Contact technical support.";
                        return msg;
                    }
                    await EnsureAuthorities();
                    au = Authorities.FirstOrDefault(d => d.Key.Equals(originalCase.Authority, StringComparison.CurrentCultureIgnoreCase));
                    if (au == null)
                    {
                        msg = $@"Unable to find Authority record for {originalCase.Authority}. Contact technical support.";
                        await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.MDMissingAuthority, originalCase.Authority, msg, _errorContext);
                        return msg;
                    }
                }

                if (au.Key.Equals("nsa", StringComparison.CurrentCulture))
                {
                    msg = $@"NSA is not a valid Authority for the Case Archive process. Contact technical support regarding ArbitrationCase record Id {originalCase.Id}.";
                    return msg;
                }

                var archive = new CaseArchive
                {
                    ArbitrationCaseId = originalCase.Id,
                    AuthorityCaseId = originalCase.AuthorityCaseId,
                    AuthorityId = au.Id,
                    AuthorityStatus = originalCase.AuthorityStatus,
                    AuthorityWorkflowStatus = originalCase.Status,
                    CreatedBy = user.Email,
                    CreatedOn = Utilities.GetCurrentUtcDate(),
                    Id = 0
                };

                string json = $@"{{""IneligibilityAction"":""{originalCase.IneligibilityAction}"",""IneligibilityReasons"":""{originalCase.IneligibilityReasons}""}}";
                archive.JSON = json;

                // capture any Notes or Rejection / Ineligible info before clearing
                _context.CaseArchives.Add(archive);
                if (saveInstantly)
                    await _context.SaveChangesAsync();

                if (resetOriginal)
                {
                    originalCase.AuthorityCaseId = String.Empty;
                    originalCase.AuthorityStatus = "Not Submitted";
                    originalCase.Status = ArbitrationStatus.New;
                    originalCase.IneligibilityAction = "";
                    originalCase.IneligibilityReasons = "";
                }

            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return msg;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newArbCase"></param>
        /// <param name="ArbitCaseInDB"></param>
        /// <param name="runAs"></param>
        /// <returns></returns>
        public async Task<ArchiveCaseResult> ArchiveIfNecessaryAsync(IAuthorityCase newArbCase, ArbitrationCase ArbitCaseInDB, AppUser runAs)
        {
            var result = new ArchiveCaseResult();

            if (string.IsNullOrEmpty(newArbCase.AuthorityCaseId) || string.IsNullOrEmpty(newArbCase.Authority)
                || string.IsNullOrEmpty(ArbitCaseInDB.AuthorityCaseId) || string.IsNullOrEmpty(ArbitCaseInDB.Authority))
                return result;

            if (ArbitCaseInDB.Authority.Equals(newArbCase.Authority, StringComparison.CurrentCultureIgnoreCase) &&
                ArbitCaseInDB.AuthorityCaseId.Equals(newArbCase.AuthorityCaseId, StringComparison.CurrentCultureIgnoreCase))
                return result;

            await EnsureAuthorities();

            if (!string.IsNullOrEmpty(ArbitCaseInDB.AuthorityCaseId) && Utilities.IsActiveWorkflow(ArbitCaseInDB.Status))
            {
                if (Utilities.IsActiveStateCase(newArbCase))
                {
                    result.ArchiveError = true;
                    result.Message = $@"ArbitrationCases Id {ArbitCaseInDB.Id}: Cannot replace active case {ArbitCaseInDB.Authority}-{ArbitCaseInDB.AuthorityCaseId} with another active case {newArbCase.Authority}-{newArbCase.AuthorityCaseId}. One of these case numbers may be linked to the wrong PayorClaimNumber.";
                    await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.AuthorityCaseArchiveBlocked, ArbitCaseInDB.Id.ToString(), result.Message, _errorContext);
                    return result;
                }
                else
                {
                    // since the "new" case was already Closed by the authority, push it straight to the archive if not there already
                    var archiveAuthority = Authorities.FirstOrDefault(d => d.Key.Equals(newArbCase.Authority, StringComparison.CurrentCultureIgnoreCase));
                    if (archiveAuthority == null)
                    {
                        result.ArchiveError = true;
                        result.Message = $@"No Authority record found for Key '{newArbCase.Authority}' on AuthorityCaseId {newArbCase.AuthorityCaseId} (import record).";
                        await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.MDMissingAuthority, newArbCase.Authority, result.Message, _errorContext);
                        result.Message += " (Exception logged.)";
                        return result;
                    }

                    result.IsAlreadyArchived = true;
                    result.ArchiveNeeded = false;
                    result.Message = $@"Authority Case {archiveAuthority.Key}-{newArbCase.AuthorityCaseId} archived.";

                    var caseArchived = await _context.CaseArchives.FirstOrDefaultAsync(d => d.AuthorityId == archiveAuthority.Id && d.AuthorityCaseId == newArbCase.AuthorityCaseId);
                    if (caseArchived != null)
                        return result; // nothing to do

                    var mappings = archiveAuthority.AuthorityJson?.StatusMappings;

                    var caseArchive = new CaseArchive
                    {
                        ArbitrationCaseId = ArbitCaseInDB.Id,
                        AuthorityCaseId = newArbCase.AuthorityCaseId,
                        AuthorityId = archiveAuthority.Id,
                        AuthorityStatus = newArbCase.AuthorityStatus,
                        AuthorityWorkflowStatus = GetWorkflowStatusFromAuthorityStatus(mappings, newArbCase.AuthorityStatus),
                        CreatedBy = runAs.Email,
                        CreatedOn = Utilities.GetCurrentUtcDate(),
                        Id = 0
                    };

                    string json = $@"{{""IneligibilityAction"":""{newArbCase.IneligibilityAction}"",""IneligibilityReasons"":""{newArbCase.IneligibilityReasons}""}}";
                    caseArchive.JSON = json;

                    // capture any Notes or Rejection / Ineligible info before clearing
                    _context.CaseArchives.Add(caseArchive);
                    await _context.SaveChangesAsync();
                    return result;
                }
            }

            var authority = Authorities.FirstOrDefault(d => d.Key.Equals(ArbitCaseInDB.Authority, StringComparison.CurrentCultureIgnoreCase));
            if (authority == null)
            {
                result.ArchiveError = true;
                result.Message = $@"No Authority record found for Key '{ArbitCaseInDB.Authority}' on ArbitrationCases.Id {ArbitCaseInDB.Id}.";
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.MDMissingAuthority, ArbitCaseInDB.Authority, result.Message, _errorContext);
                result.Message += " (Exception logged.)";
                return result;
            }

            // Move the current authority case info to the archive since it is Closed and the incoming record might be more recent (no real way to tell = last in wins)
            var archiveRecord = await _context.CaseArchives.FirstOrDefaultAsync(d => d.AuthorityId == authority.Id && d.AuthorityCaseId == ArbitCaseInDB.AuthorityCaseId);
            if (archiveRecord != null)
            {
                /* this was causing too many issues so we will try another approach...overwriting the existing 
                 * archive record with whatever is on the Original record and making a not of it
                 * 
                result.ArchiveError = true;
                result.Message = $@"Case {Original.Authority}-{Original.AuthorityCaseId} already archived. Cannot update ArbitrationCases.Id {Original.Id}";
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.PreviouslyArchived, archiveRecord.Id.ToString(), result.Message, runAs);
                result.Message += " (Exception logged.)";
                return result;
                */
                archiveRecord.ArbitrationCaseId = ArbitCaseInDB.Id;
                archiveRecord.AuthorityId = authority.Id;
                archiveRecord.AuthorityStatus = ArbitCaseInDB.AuthorityStatus;
                archiveRecord.AuthorityWorkflowStatus = ArbitCaseInDB.Status;
                archiveRecord.CreatedOn = Utilities.GetCurrentUtcDate();
                archiveRecord.JSON = $@"{{""IneligibilityAction"":""{ArbitCaseInDB.IneligibilityAction}"",""IneligibilityReasons"":""{ArbitCaseInDB.IneligibilityReasons}""}}";
                result.Message = $@"Existing archive for Authority Case {ArbitCaseInDB.Authority}-{ArbitCaseInDB.AuthorityCaseId} was updated.";
                result.ArchiveNeeded = true;
            }
            else
            {

                // Create an archive record and reset the Status, AuthorityCaseId, AuthorityStatus,
                // IneligibilityAction and IneligibilityReason fields on the currently active record.
                result.ArchiveNeeded = true;
                string archiveMsg = await ArchiveCaseAsync(ArbitCaseInDB, runAs, authority);
                if (!string.IsNullOrEmpty(archiveMsg))
                {
                    result.ArchiveError = true;
                    result.Message = $@"Error archiving case {ArbitCaseInDB.Authority.ToUpper()}-{ArbitCaseInDB.AuthorityCaseId} for ArbitrationCases.Id {ArbitCaseInDB.Id}. {archiveMsg}.";
                    await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.AuthorityCaseArchiveBlocked, ArbitCaseInDB.Id.ToString(), result.Message, _errorContext);
                    result.Message += " (Exception logged.)";
                    return result;
                }
            }

            // update the current claim header with the latest authority info
            ArbitCaseInDB.Authority = newArbCase.Authority;
            ArbitCaseInDB.AuthorityCaseId = newArbCase.AuthorityCaseId;
            ArbitCaseInDB.AuthorityStatus = newArbCase.AuthorityStatus;
            ArbitCaseInDB.IneligibilityAction = newArbCase.IneligibilityAction;
            ArbitCaseInDB.IneligibilityReasons = newArbCase.IneligibilityReasons;

            result.Message = $@"Authority Case {ArbitCaseInDB.Authority}-{ArbitCaseInDB.AuthorityCaseId} was archived.";
            return result;
        }
        /// <summary>
        /// Validates and save many Notifications into the queue all at once.
        /// </summary>
        /// <param name="Notifications"></param>
        /// <param name="RunAs"></param>
        /// <param name="FullUserName"></param>
        /// <returns></returns>
        public async Task BatchQueueNotificationsAsync(IEnumerable<Notification> Notifications, AppUser RunAs, string FullUserName)
        {
            var log = new StringBuilder("<html><head><style>HTML,body,table {font-family:Calibri, sans-serif;text-align:left}</style></head>");
            log.Append("<body>");
            log.AppendLine($@"<p>{Utilities.GetCurrentCSTDate2().ToString("R")} (CST): Begin queuing Notifications...</p>");
            log.AppendLine("<table><thead><tr><th>Arbit Id</th><th>Result</th></tr><tbody>");
            Notification[] CachedNotifications = new Notification[0];

            try
            {
                // Cache values that will be used multiple times
                await EnsureAuthorities();
                await EnsureCustomers();

                foreach (var Notification in Notifications)
                {
                    string Message = await RenderAndQueueNotificationAsync(Notification, RunAs, FullUserName);
                    log.AppendLine($@"<tr><td>{Notification.ArbitrationCaseId}</td><td>{Message}</td></tr>");
                }

                log.AppendLine("</tbody></table></body></html>");

                // Email the run log to the requester
                try
                {
                    string CcAddress = "";
                    string Subject = $@"Batch Notification Results for {Utilities.GetCurrentCSTDate2().ToString("R")} (CST)";
                    var MessageArgs = new Dictionary<string, string>();
                    var MessageCategories = new List<string>();
                    MessageCategories.Add("NotificationBatchLog");
#if DEBUG
                    CcAddress = "developer.email@HaloMD.com";
#endif

                    var response = await SendEmailAsync(RunAs.Email, CcAddress, Subject, log.ToString(), MessageArgs, MessageCategories);
                    if (response != null && !response.IsSuccessStatusCode)
                    {
                        var sendError = response.Body.ReadAsStringAsync();
                    }
                }
                catch (Exception ex)
                {
                    // fall back to uploading the log to the blob store
                    _logger.LogError(ex.Message);
                    if (ex.InnerException != null)
                        _logger.LogError(ex.InnerException.Message);
                }
            }
            catch (Exception ex)
            {
                log.AppendLine("Unrecoverable error when queuing Notifications");
                if (ex.InnerException != null)
                    log.AppendLine(ex.InnerException.Message);
                else
                    log.AppendLine(ex.Message);
            }
        }

        private async Task<string> RenderAndQueueNotificationAsync(Notification Notification, AppUser User, string FullUserName)
        {
            try
            {
                var NSAAuth = Notification.ArbitrationCase?.NSAAuthority;
                var StateAuth = Notification.ArbitrationCase?.StateAuthority;

                if (Notification.ArbitrationCase == null)
                    return "Server error. ArbitrationCase not attached to the Notification.";
                if (NSAAuth == null || NSAAuth.TrackingDetails.Count == 0)
                    return "Server error. NSA Authority configuration not found.";
                if (Notification.ArbitrationCase.StateAuthority == null)
                    return "Server error. State Authority configuration not found.";

                // Get CPT code descriptions
                if (Notification.ArbitrationCase.CPTCodes.Count() > 0)
                {
                    await EnsureProcedureCodes();
                    foreach (var _CPTCode in Notification.ArbitrationCase.CPTCodes)
                    {
                        var procCode = ProcedureCodes.FirstOrDefault(d => d.Code == _CPTCode.CPTCode);
                        if (procCode != null)
                        {
                            _CPTCode.Description = procCode.Description.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
                        }
                    }
                }

                // update a copy of the tracking data before referencing those values in the templates

                if (!string.IsNullOrEmpty(Notification.ArbitrationCase.NSATracking) && Notification.NotificationType == NotificationType.NSANegotiationRequest)
                {
                    await Extensions.EnsureHolidays(_context);
                    Notification.ArbitrationCase.NSATracking = Utilities.SetTrackingValue(Utilities.GetCurrentUtcDate(), NSAAuth.TrackingDetails, Notification.ArbitrationCase.NSATracking, "DateNegotiationSent", Notification.ArbitrationCase);
                }

                await EnsurePayors(false);

                var Payor = Payors.FirstOrDefault(v => v.Id == Notification.ArbitrationCase.PayorId);
                if (Payor == null)
                    return $@"Unable to locate Payor Id {Notification.ArbitrationCase.PayorId}";

                var template = Utilities.GetDocumentTemplate(Notification.NotificationType, Payor);
                if (string.IsNullOrEmpty(template))
                    return "No template found for Payor";

                await EnsureCalculatorVariables();

                // get latest CalculatorVariable settings for the case's service line
                var asOf = Utilities.GetCurrentUtcDate();

                var filter = from r in CalculatorVariables.Where(x => x.CreatedOn <= asOf && x.ServiceLine == Notification.ArbitrationCase.ServiceLine)
                             group r by r.ServiceLine into op
                             select op.OrderByDescending(x => x.CreatedOn).First();

                var CalcVariables = filter.FirstOrDefault();
                if (CalcVariables == null || string.IsNullOrEmpty(CalcVariables.NSAOfferBaseValueFieldname))
                    return "Invalid or missing global app settings. Try updating the Calculator Variables and re-selecting the NSA Offer Base Value Field.";

                await EnsureCustomers();
                var Customer = Customers.FirstOrDefault(d => d.Name.Equals(Notification.ArbitrationCase.Customer, StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(d.JSON));
                if (Customer == null)
                    return "Invalid Customer value";



                JsonNode? node = JsonNode.Parse(Customer.JSON);
                string NSAReplyTo = node == null || node["NSAReplyTo"] == null ? "" : node["NSAReplyTo"]!.ToString();
                if (string.IsNullOrEmpty(NSAReplyTo))
                    return "Customer is not configured properly. NSAReplyTo is missing!";

                Notification.HTML = Utilities.MergeTemplateData(template, Notification.NotificationType, Notification.ArbitrationCase, CalcVariables, StateAuth!, NSAReplyTo, _logger); // the main notification email

                // TODO: Insert some REGEX validation here such as detecting the BLANK constant $___ or unresolved tokens. Those are bad!


                var docTemplates = Utilities.GetDocumentTemplates(NotificationType.NSANegotiationRequestAttachment, Payor); // supplemental content

                var supplements = new List<NotificationDocument>();

                // move this config option to the MPNotify logic once we figure out how/where to set up the preference grid 
                // -> bool template.includeInline = false; // default is to make it an attachment

                bool makePDFs = true; // TODO: Future use - at one point they wanted the other attachments rendered in line with the rest of the message body so they could decide to do it again or make a per-payor preference


                foreach (var docTemplate in docTemplates)
                {
                    var html = Utilities.MergeTemplateData(docTemplate.HTML, docTemplate.NotificationType, Notification.ArbitrationCase, CalcVariables, StateAuth!, NSAReplyTo, _logger);
                    if (makePDFs)
                    {
                        var pdf = NRecoPdfWrapper.GeneratePDF(_logger, html, new Dictionary<string, string>(), new Dictionary<string, string>(), out string PDFRenderProblems);

                        if (pdf?.Length > 0) // NOTE! If the returned stream is null then there's something seriously wrong b/c the this has good fail over
                        {
                            using (var stream = new MemoryStream(pdf))
                            {

                                var message = await SaveClaimBLOB(stream, Notification.ArbitrationCase, CaseDocumentType.NSARequestAttachment, docTemplate.Name + ".pdf", FullUserName);
                                if (!string.IsNullOrEmpty(message))
                                    return message;
                            }
                        }
                        else if (!string.IsNullOrEmpty(PDFRenderProblems))
                        {
                            return PDFRenderProblems;
                        }
                    }
                    else
                    {
                        supplements.Add(new NotificationDocument()
                        {
                            ArbitrationCaseId = Notification.ArbitrationCaseId,
                            HTML = html,
                            JSON = "{}",
                            Name = docTemplate.Name,
                            NotificationType = docTemplate.NotificationType
                        });

                    }
                }


                var a = new
                {
                    payorId = Payor.Id,
                    supplements = supplements
                };

                if (Notification.NotificationType == NotificationType.NSANegotiationRequest)
                    Notification.AuthorityKey = "nsa";
                else
                    Notification.AuthorityKey = Notification.ArbitrationCase.Authority;

                Notification.Customer = Notification.ArbitrationCase.Customer;
                Notification.JSON = JsonSerializer.Serialize(a);
                Notification.PayorClaimNumber = Notification.ArbitrationCase.PayorClaimNumber;
                Notification.Status = "pending";
                Notification.SubmittedBy = User.Email;
                Notification.SubmittedOn = Utilities.GetCurrentUtcDate();
                Notification.UpdatedBy = User.Email;
                Notification.UpdatedOn = Utilities.GetCurrentUtcDate();
                Notification.ReplyTo = NSAReplyTo;
                Notification.To = Payor.NSARequestEmail; // validate above

                if (Utilities.IsValidEmail(User.Email) && !Notification.To.Equals(User.Email, StringComparison.CurrentCultureIgnoreCase) && !NSAReplyTo.Equals(User.Email, StringComparison.CurrentCultureIgnoreCase))
                    Notification.CC = NSAReplyTo + ";" + User.Email;
                else
                    Notification.CC = NSAReplyTo;
#if DEBUG
                Notification.BCC = "developer.email@HaloMD.com";
#endif

                _context.Notifications.Add(Notification);
                await _context.SaveChangesAsync();

                return "Notification queued successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError($@"Error while queuing Notification. Item skipped. {ex.Message}");
                return "Error while queuing Notification. Item skipped. Try queuing the item manually. " + ex.Message;
            }
        }

        private async Task<bool> CreateEntityIfNeededAsync(ArbitrationDbContext Context, Customer Target, string EntityName, string EntityNPI)
        {
            try
            {
                await EnsureCustomers();
                var Customer = Customers.FirstOrDefault(d => d.Id == Target.Id);
                if (Customer == null)
                    throw new Exception("Unexpected: Cannot find Customer in CreateEntityIfNeededAsync!");

                var Entity = Customer.Entities.FirstOrDefault(d => d.NPINumber == EntityNPI.Trim() || d.Name.Equals(EntityName.Trim(), StringComparison.CurrentCultureIgnoreCase));
                if (Entity != null)
                    return false;

                Entity = new Entity
                {
                    Address = DEFAULT_ENTITY_ADDRESS,
                    City = DEFAULT_ENTITY_CITY,
                    CustomerId = Customer.Id,
                    JSON = "{}",
                    Name = EntityName.Trim(),
                    NPINumber = EntityNPI.Trim(),
                    State = DEFAULT_ENTITY_STATE,
                    UpdatedBy = "system",
                    UpdatedOn = Utilities.GetCurrentUtcDate(),
                    ZipCode = DEFAULT_ENTITY_ZIP
                };

                Customer.Entities.Add(Entity);
                await Context.SaveChangesAsync();

                // auto-add a DataException to indicate this happened
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.MDMissingCustomerEntity, EntityNPI, $@"Entity {EntityName.Trim()} automatically added to Customer {Customer.Name} during a system task", Context);

                // TODO: Is the new Entity available in the cached list?
                return true;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    _logger.LogCritical(ex.InnerException, ex.InnerException.Message);
                else
                    _logger.LogCritical(ex, ex.Message);

                Context.ChangeTracker.Clear();
            }
            return false;
        }

        private async Task<bool> CreatePayorIfNeededAsync(ArbitrationDbContext Context, string PayorName)
        {
            if (PayorName.Trim().ToLower() == "bad record")
                return false;

            try
            {
                await EnsurePayors();
                if (Payors.FirstOrDefault(d => d.Name.Equals(PayorName.Trim(), StringComparison.CurrentCultureIgnoreCase)) == null)
                {
                    // auto-add payor
                    var payor = new Payor()
                    {
                        IsActive = true,
                        JSON = "{}",
                        Name = PayorName.Trim(),
                        NSARequestEmail = "NoReply@mPowerHealth.com",
                        ParentId = 0,
                        SendNSARequests = true,
                        UpdatedBy = "system",
                        UpdatedOn = Utilities.GetCurrentUtcDate()
                    };

                    if (PayorName != "BCBSTX")
                    {
                        payor.JSON = DEFAULT_PAYOR_JSON;
                    }

                    Context.Payors.Add(payor);
                    await Context.SaveChangesAsync();

                    payor.ParentId = payor.Id;
                    await Context.SaveChangesAsync();

                    // auto-add a DataException to indicate this happened
                    await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.MDMissingPayor, PayorName, $@"Payor {PayorName.Trim()} automatically added during a system task", Context);

                    Payors.Add(payor); // add to local cache
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    _logger.LogCritical(ex.InnerException, ex.InnerException.Message);
                else
                    _logger.LogCritical(ex, ex.Message);

                Context.ChangeTracker.Clear();
            }
            return false;
        }

        /// <summary>
        /// Find an ArbitrationCase record using the encounter composite key of PatientName + ServiceDate + ProviderNPI.
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="skipDOBCheck">When false, the patient's DOB is required to be present in the criteria. Note that if a record is found and the DOB is in the criteria is will always be used as a sanity check.</param>
        /// <param name="skipPCNCheck">When false, a message is returned indicating that the PayorClaimNumber in the criteria conflicts with different set of "encounter" criteria</param>
        /// <param name="skipCustomerCheck">When false, Customer is excluded from the search criteria</param>
        /// <param name="returnValue">The ArbitrationCase record if found</param>
        /// <param name="message">Empty if the criteria points to one and only one record.</param>
        public async Task<FindCaseResult> FindArbitrationCase(IEHRKey criteria, bool skipDOBCheck, bool skipPCNCheck, bool skipCustomerCheck = false)
        {
            //tryMerge = false; // Disable for now
            var result = new FindCaseResult();

            const string EXCEPTION_LOGGED = ". (Exception logged.)";
            var user = new AppUser { Email = "system", Id = -1, IsActive = true };
            //            var key = string.Format("{0} | {1} | {2} | {3} ", criteria.PatientName, criteria.ServiceDate, criteria.ProviderNPI, criteria.EOBDate);
            var key = string.Format("{0} | {1} | {2}", criteria.PatientName, criteria.ServiceDate, criteria.ProviderNPI);

            if (!skipPCNCheck && string.IsNullOrEmpty(criteria.PayorClaimNumber))
            {
                result.Message += "Cannot search for a claim without a PayorClaimNumber when skipPCNCheck is false";
                return result;
            }

            if (!skipDOBCheck && !criteria.DOB.HasValue)
            {
                result.Message += "Cannot search for a claim without a valid DOB";
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.ClaimMissingDOB, key, result.Message, _errorContext);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                result.Message += EXCEPTION_LOGGED;
                return result;
            }

            if (!criteria.ServiceDate.HasValue)
            {
                result.Message += "Cannot search for a claim without a valid ServiceDate";
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.ClaimMissingServiceDate, key, result.Message, _errorContext);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                result.Message += EXCEPTION_LOGGED;
                return result;
            }

//            if (!criteria.EOBDate.HasValue)
//            {
//                result.Message += "Cannot search for a claim without a valid EOBDate";
//#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//                ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.ClaimMissingEOBDate, key, result.Message, _errorContext);
//#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//                result.Message += EXCEPTION_LOGGED;
//                return result;
//            }



            if (string.IsNullOrEmpty(criteria.ProviderNPI))
            {
                result.Message += "Cannot search for a claim without an ProviderNPI value";
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.ClaimMissingNPI, key, result.Message, _errorContext);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                result.Message += EXCEPTION_LOGGED;
                return result;
            }

            if (string.IsNullOrEmpty(criteria.PatientName))
            {
                result.Message += "Cannot search for a claim without a PatientName value";
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.ClaimMissingPatientName, key, result.Message, _errorContext);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                result.Message += EXCEPTION_LOGGED;
                return result;
            }

            await EnsureCustomers();

            if (!skipCustomerCheck && (string.IsNullOrEmpty(criteria.Customer) || Customers.FirstOrDefault(d => d.Name.Equals(criteria.Customer, StringComparison.CurrentCultureIgnoreCase)) == null))
            {
                result.Message += "Cannot search for a claim without a valid Customer value";
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.ClaimMissingPatientName, key, result.Message, _errorContext);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                result.Message += EXCEPTION_LOGGED;
                return result;
            }

            var NameVariations = Utilities.PersonNameVariations(criteria.PatientName);

            // The goal of the following rules is to validate that the criteria passed in refers to one and only one ArbitrationCase
            var arbitCases = _context.ArbitrationCases.Include(d => d.Arbitrators)
                                                         .Include(d => d.SettlementDetails.Where(g => !g.IsDeleted))
                                                         .Where(d => !d.IsDeleted
                                                                     && (skipCustomerCheck || d.Customer == criteria.Customer)
                                                                     && NameVariations.Contains(d.PatientName)
                                                                     && d.ServiceDate.HasValue
                                                                     && d.ServiceDate.Value.Date == criteria.ServiceDate.Value.Date
//                                                                     && d.EOBDate.Value.Date == criteria.EOBDate.Value.Date
                                                                     && d.ProviderNPI == criteria.ProviderNPI).ToArray();
            if (arbitCases.Count() > 1)
            {
                // try to merge the multiple matching records
                //var temp = tryMerge ? await Utilities.MergeCaseDataAsync(_context, this, user, authCases.First(), nsa) : new MergeClaimsResult();

                //if (tmp.MergedRecord == null)
                //{
                result.Message = $@"Multiple records matched Patient | ServiceDate | ProviderNPI: {criteria.PatientName} | {criteria.ServiceDate.Value.ToShortDateString()} | {criteria.ProviderNPI}.";
                //if (tryMerge && !string.IsNullOrEmpty(tmp.Message))
                //    result.Message += $@" {tmp.Message}";
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.DuplicateKeyValues, key, result.Message, _errorContext);
                result.Message += EXCEPTION_LOGGED;
                return result;
                //}
                //result.Record = tmp.MergedRecord;
            }
            else
            {
                result.Record = arbitCases.FirstOrDefault();
            }

            // Even if skipDOB check is on, if the criteria has it we will still use it to be safe
            if (result.Record != null && criteria.DOB.HasValue && result.Record.DOB.HasValue && result.Record.DOB.Value.Date != criteria.DOB.Value.Date)
            {
                result.Record = null;
                result.Message = $@"DOB mismatch on record Patient | ServiceDate | ProviderNPI: {criteria.PatientName} | {criteria.ServiceDate.Value.ToShortDateString()} | {criteria.ProviderNPI}";
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.ClaimMismatchDOB, $@"{criteria.PatientName} | {criteria.ServiceDate.Value.ToShortDateString()} | {criteria.ProviderNPI}", result.Message, _errorContext); // DOB left out of message log for privacy reasons
                result.Message += EXCEPTION_LOGGED;
                return result;
            }

            // Claim wasn't found with the composite key but is there another record
            // with the same PayorClaimNumber out there? If so let's tell the users.
            // This may end up being useless but, for now, let's keep a watch on this safety signal.
            if (result.Record == null && !skipPCNCheck)
            {
                var testCase = _context.ArbitrationCases.FirstOrDefault
                    (d => !d.IsDeleted && d.PayorClaimNumber.Length > 5
                    && (d.PayorClaimNumber.Contains(criteria.PayorClaimNumber)
                    || criteria.PayorClaimNumber.Contains(d.PayorClaimNumber)));
                if (testCase != null)
                {
                    if (testCase.PatientName.Equals(criteria.PatientName, StringComparison.CurrentCultureIgnoreCase)
                        | testCase.ProviderNPI.Equals(criteria.ProviderNPI, StringComparison.CurrentCultureIgnoreCase)
                       && (testCase.ServiceDate.HasValue && testCase.ServiceDate.Value.Date == criteria.ServiceDate.Value.Date))
                    {
                        var log = $@"FindArbitrationCase WARNING: PayorClaimNumber {criteria.PayorClaimNumber}, PatientName {criteria.PatientName}, ProviderNPI {criteria.ProviderNPI}, ServiceDate {criteria.ServiceDate}  appears to conflict with record ID {testCase.Id}.! Adding record";
                        await this.SaveUploadLog("FindArbitrationCase", "system", Utilities.GetCurrentUtcDate(), log);
                        _logger.LogWarning(log);
                    }
                    else if (testCase.PatientName.Equals(criteria.PatientName, StringComparison.CurrentCultureIgnoreCase)
                       || testCase.ProviderNPI.Equals(criteria.ProviderNPI, StringComparison.CurrentCultureIgnoreCase)
                       || (testCase.ServiceDate.HasValue && testCase.ServiceDate.Value.Date == criteria.ServiceDate.Value.Date))
                    {
                        var log = $@"FindArbitrationCase error: PayorClaimNumber {criteria.PayorClaimNumber}, PatientName {testCase.PatientName}={criteria.PatientName}, ProviderNPI {testCase.ProviderNPI}={criteria.ProviderNPI}, ServiceDate {testCase.ServiceDate}={criteria.ServiceDate}  appears to conflict with record ID {testCase.Id}, still continuing";
                        await this.SaveUploadLog("FindArbitrationCase", "system", Utilities.GetCurrentUtcDate(), log);
                        _logger.LogWarning(log);
                    }
                }
            }
            return result;
        }

        public static ArbitrationResult GetArbitrationResultFromAuthorityStatus(IEnumerable<AuthorityStatusMapping>? mappings, string authorityStatus)
        {
            if (mappings != null && mappings.Count() > 0)
            {
                var from = mappings.FirstOrDefault(d => d.AuthorityStatus.Equals(authorityStatus, StringComparison.CurrentCultureIgnoreCase));
                if (from != null)
                {
                    return from.ArbitrationResult;
                }
            }
            return ArbitrationResult.None;
        }

        public static ArbitrationStatus GetWorkflowStatusFromAuthorityStatus(IEnumerable<AuthorityStatusMapping>? mappings, string authorityStatus)
        {
            if (mappings != null && mappings.Count() > 0)
            {
                var from = mappings.FirstOrDefault(d => d.AuthorityStatus.Equals(authorityStatus, StringComparison.CurrentCultureIgnoreCase));
                if (from != null && from.WorkflowStatus.HasValue)
                {
                    return from.WorkflowStatus.Value;
                }
            }
            return ArbitrationStatus.Unknown;
        }

        public async void ImportBenchmarks(int benchmarkId, string userName, string upload, JobQueueItem? job)
        {


            var log = new StringBuilder();
            try
            {
                log.AppendLine($@"{Utilities.GetCurrentCSTDate2().ToString("G")} CST - Starting benchmark import...");
                BenchmarkUpload? parsed = null;
                var benchmark = await _context.BenchmarkDatasets.FindAsync(benchmarkId);

                if (benchmark == null)
                    throw new ArgumentOutOfRangeException("Invalid Benchmark Dataset id");

                parsed = JsonSerializer.Deserialize<BenchmarkUpload>(upload);
                if (parsed == null || parsed.BenchmarkDataItems == null)
                    throw new ArgumentOutOfRangeException("No valid benchmarks found in upload. Verify property names match the specification, including case.");

                var filtered = parsed.BenchmarkDataItems.Where(j => j.Benchmarks != null && !string.IsNullOrEmpty(j.GeoZip) && !string.IsNullOrEmpty(j.ProcedureCode));
                if (filtered.Count() == 0)
                    throw new ArgumentOutOfRangeException("No valid benchmarks found in upload. Verify property names match the specification, including case.");

                if (benchmark.Id < 1 || string.IsNullOrEmpty(benchmark.Key))
                    throw new ArgumentNullException("Benchmark Dataset object has missing or invalid values");
                if (filtered.Count() == 0)
                    throw new ArgumentException("No benchmark items available for processing.");

                // silly sanity check for now
                var dupes = parsed.BenchmarkDataItems.Where(d => d.GeoZip == "750" && d.ProcedureCode == "99281").ToArray();
                if (dupes.Length > 1)
                    throw new ArgumentException("There appear to be duplicate benchmarks.");

                //_memoryCache.Set(BENCHMARK_CACHE_KEY, true, DateTime.Now.AddMinutes(15)); // automatically unlock the upload process in case it isn't unlocked at the end of processing

                // purge the existing benchmark items
                var count = await _context.BenchmarkDataItems.CountAsync(d => d.BenchmarkDatasetId == benchmarkId);
                if (count > 0)
                {
                    log.AppendLine($@"Attempting to remove {count} previous benchmarks for {benchmark.Key}");
                    benchmark = await _context.BenchmarkDatasets.Include(d => d.BenchmarkItems).FirstAsync(d => d.Id == benchmarkId);
                    _context.BenchmarkDataItems.RemoveRange(benchmark.BenchmarkItems);
                    await _context.SaveChangesAsync();
                    log.AppendLine($@"Removed any previous benchmarks for {benchmark.Key}");
                }

                var batchUploadDate = Utilities.GetCurrentUtcDate();

                foreach (var item in filtered)
                {
                    benchmark.BenchmarkItems.Add(new BenchmarkDataItem
                    {
                        Id = 0,
                        Benchmarks = JsonSerializer.Serialize(item.Benchmarks),
                        GeoZip = item.GeoZip,
                        Modifiers = item.Modifiers,
                        ProcedureCode = item.ProcedureCode,
                        UpdatedBy = "system",
                        UpdatedOn = batchUploadDate
                    });
                }

                log.AppendLine($@"{Utilities.GetCurrentCSTDate2().ToString("G")} CST - Calling SaveChanges for all new benchmark items...");
                await _context.SaveChangesAsync();
                log.AppendLine($@"{Utilities.GetCurrentCSTDate2().ToString("G")} CST - Successfully added {filtered.Count()} benchmarks to {benchmark.Key}");

            }
            catch (Exception ex)
            {
                log.AppendLine(ex.Message);
                var innerException = ex.InnerException;
                while (innerException != null)
                {
                    log.AppendLine(innerException.Message);
                    innerException = ex.InnerException;
                }
            }
            finally
            {
                //_memoryCache.Remove(BENCHMARK_CACHE_KEY);
                log.AppendLine($@"{Utilities.GetCurrentCSTDate2().ToString("G")} CST - Uploading log to document store");
                await this.SaveUploadLog("BenchmarkDataset", "system", Utilities.GetCurrentUtcDate(), log.ToString());
            }
        }

        public async void ImportDisputeDetailsAsync(IEnumerable<AuthorityDisputeDetailsCSV> Records, AppUser CurrentUser, JobQueueItem? CurrentJob)
        {
            if (CurrentJob != null)
            {
                _errorContext.Entry(CurrentJob).State = EntityState.Unchanged;
            }

            var NewRecords = Records.Where(d => !string.IsNullOrEmpty(d.AuthorityKey) && !string.IsNullOrEmpty(d.AuthorityCaseId) && !string.IsNullOrEmpty(d.ClaimCPTCode)).ToImmutableArray();
            int RecordCount = NewRecords.Count();

            if (RecordCount == 0)
                throw new Exception("No valid Dispute Detail records found to process!");

            int RecordsProcessed = 0;
            int RecordsSkipped = 0;
            int RecordsError = 0;
            var BatchUploadDate = Utilities.GetCurrentUtcDate();

            var log = new StringBuilder();

            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Begin loading Dispute Details...");
            log.AppendLine();

            try
            {
                await EnsureAuthorities();
                var NSAAuthority = Authorities.FirstOrDefault(d => d.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase))!;
                if (NewRecords.Count(d => d.AuthorityKey.Equals("nsa", StringComparison.CurrentCultureIgnoreCase)) > 0)
                {
                    log.AppendLine("WARNING: Non-NSA data detected. If you are intentionally uploading non-NSA records you can ignore this warning. Otherwise, verify the Authority Id in your upload file!!!");
                }

                var CalcVars = await Utilities.GetCalculatorVariablesAsync(_context);

                // Make a cache for speed
                var DisputeNumbers = await (from d in _context.AuthorityDisputes
                                            join a in _context.Authorities on d.AuthorityId equals a.Id
                                            select new { d.Id, d.Authority, AuthorityId = a.Id, d.AuthorityCaseId }).ToArrayAsync();

                // Group the CPTs by Authority case identifier
                var ItemGroups = NewRecords.GroupBy(c => new { c.AuthorityKey, c.AuthorityCaseId }).Select(g => new { g.Key.AuthorityKey, g.Key.AuthorityCaseId }).ToArray();

                foreach (var Disputes in ItemGroups)
                {
                    var AuthorityKey = Disputes.AuthorityKey;
                    var AuthorityObj = Authorities.FirstOrDefault(d => d.Key.Equals(AuthorityKey, StringComparison.CurrentCultureIgnoreCase));
                    var AuthorityCaseId = Disputes.AuthorityCaseId;
                    var CPTs = NewRecords.Where(d => d.AuthorityKey.Equals(AuthorityKey, StringComparison.CurrentCultureIgnoreCase) && d.AuthorityCaseId == AuthorityCaseId);
                    RecordsProcessed += CPTs.Count();
                    bool HasChanges = false;

                    // Ensure accurate Authority import
                    if (AuthorityObj == null)
                    {
                        RecordsSkipped += CPTs.Count();
                        log.AppendLine($@"{BatchUploadDate.ToString("R")}: ERROR: Unknown AuthorityKey {AuthorityKey}. Skipping {CPTs.Count()} CPTs.");
                        continue;
                    }

                    var AuthorityId = AuthorityObj.Id;

                    // Ensure Dispute Header already exists
                    var Check = DisputeNumbers.FirstOrDefault(v => v.AuthorityId == AuthorityId && v.AuthorityCaseId.Equals(AuthorityCaseId, StringComparison.CurrentCultureIgnoreCase));
                    if (Check == null)
                    {
                        RecordsSkipped += CPTs.Count();
                        log.AppendLine($@"{BatchUploadDate.ToString("R")}: Dispute Header {AuthorityCaseId} could not be found for Authority {AuthorityId}. Skipping!");
                        continue;
                    }

                    AuthorityDispute? DisputeHeader = await _context.AuthorityDisputes.Include(x => x.DisputeCPTs).ThenInclude(d => d.ClaimCPT).AsNoTracking().FirstOrDefaultAsync(d => d.AuthorityId == AuthorityId && d.AuthorityCaseId == AuthorityCaseId);
                    int LastArbitId = 0;
                    ArbitrationCase? ArbitCase = null;
                    foreach (var CPT in CPTs.OrderBy(d => d.ArbitrationCaseId))
                    {
                        if (ArbitCase == null || CPT.ArbitrationCaseId != LastArbitId)
                        {
                            LastArbitId = CPT.ArbitrationCaseId;
                            ArbitCase = await _context.ArbitrationCases.Include(d => d.CPTCodes).AsNoTracking().FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == LastArbitId);
                        }

                        if (ArbitCase == null)
                        {
                            RecordsSkipped++;
                            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Error updating AuthorityCaseId {AuthorityCaseId} for AuthorityId {AuthorityId}. Unable to find ArbitrationCaseId {LastArbitId}. Skipping!");
                            continue;
                        }

                        var ClaimCPT = ArbitCase.CPTCodes.FirstOrDefault(d => d.CPTCode.Equals(CPT.ClaimCPTCode, StringComparison.CurrentCultureIgnoreCase));
                        if (ArbitCase.CPTCodes.Count == 0 || ClaimCPT == null)
                        {
                            RecordsSkipped++;
                            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Error updating AuthorityCaseId {AuthorityCaseId} for AuthorityId {AuthorityId}. Unable to find CPT Code {CPT.ClaimCPTCode} attached to ArbitrationCaseId {LastArbitId}. Skipping!");
                            continue;
                        }

                        var DisputeCPT = DisputeHeader!.DisputeCPTs.FirstOrDefault(d => d.ClaimCPT.ArbitrationCaseId == LastArbitId && d.ClaimCPT.CPTCode.Equals(CPT.ClaimCPTCode, StringComparison.CurrentCultureIgnoreCase));
                        RecordsProcessed++;

                        if (DisputeCPT == null)
                        {
                            var ServiceLineVars = CalcVars.FirstOrDefault(d => d.ServiceLine.Equals(ArbitCase.ServiceLine, StringComparison.CurrentCultureIgnoreCase));
                            // Add new CPT to Header
                            DisputeCPT = new AuthorityDisputeCPT
                            {
                                AddedBy = CurrentUser.Email,
                                AddedOn = Utilities.GetCurrentUtcDate(),
                                AuthorityDisputeId = DisputeHeader!.Id,
                                CalculatedOfferAmount = 0, // requires a benchmark value 
                                ClaimCPTId = ClaimCPT.Id,
                                FinalOfferAmount = CPT.FinalUnitOfferAmount,
                                ServiceLineDiscount = ServiceLineVars == null ? 0 : Utilities.GetDefaultServiceLineDiscount(AuthorityObj, ServiceLineVars),
                                UpdatedBy = CurrentUser.Email,
                                UpdatedOn = Utilities.GetCurrentUtcDate()
                            };

                            // TODO: calculate other stuff
                            HasChanges = true;
                            _context.AuthorityDisputeCPTs.Add(DisputeCPT);
                            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Successfully added CPT Code {CPT.ClaimCPTCode} to AuthorityCaseId {AuthorityCaseId} for Authority {AuthorityId}.");

                        }
                        else
                        {
                            // Update CPT with new data if it exists
                            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Updating existing Dispute CPTs is not supported.");
                        }
                    }

                    if (HasChanges)
                    {
                        try
                        {
                            await _context.SaveChangesAsync();

                        }
                        catch (Exception ex)
                        {
                            RecordsError++;
                            var Message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                            log.AppendLine(Message);
                            _context.ChangeTracker.Clear();
                        }
                        finally
                        {
                            // update real-time status record
                            if (CurrentJob != null)
                            {
                                await ImportUtils.UpdateJob(_errorContext, CurrentJob, $@"Importing Dispute Details records", "importing", RecordsProcessed, RecordsError, RecordsProcessed, RecordsSkipped, RecordCount, 0);
                            }
                        }
                    }
                }

                await ImportUtils.UpdateJob(_errorContext, CurrentJob, "Job complete.", "finished");
            }
            catch (Exception ex)
            {
                string Message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;

                await ImportUtils.UpdateJob(_errorContext, CurrentJob, "Job complete.", "error", RecordsProcessed, RecordCount, Message);
                log.AppendLine($@"{BatchUploadDate.ToString("R")}: Unexpected error while processing.");
                log.AppendLine(Message);
            }

            try
            {
                log.AppendLine($@"{BatchUploadDate.ToString("R")}: Finish Dispute Details load");
                string url = await SaveUploadLog("DisputeDetails", "system", BatchUploadDate, log.ToString());
                if (url.StartsWith("HTTP", StringComparison.InvariantCultureIgnoreCase))
                    await ImportUtils.UpdateJob(_errorContext, CurrentJob, "Job log: " + url, "finished");
                if (_context.ChangeTracker.HasChanges())
                    await _context.SaveChangesAsync();
                if (_errorContext.ChangeTracker.HasChanges())
                    await _errorContext.SaveChangesAsync();
            }
            catch { }
        }

        public async void ImportDisputeFeesAsync(IEnumerable<AuthorityDisputeFeeCSV> Records, AppUser CurrentUser, JobQueueItem? CurrentJob)
        {
            if (CurrentJob != null)
            {
                _errorContext.Entry(CurrentJob).State = EntityState.Unchanged;
            }

            var NewFeeItems = Records.Where(d => !string.IsNullOrEmpty(d.AuthorityKey) && !string.IsNullOrEmpty(d.AuthorityCaseId) && !string.IsNullOrEmpty(d.FeeName)).ToImmutableArray();
            int RecordCount = NewFeeItems.Count();

            if (RecordCount == 0)
                throw new Exception("No valid Dispute Fee records found to process!");

            int RecordsProcessed = 0;
            int RecordsSkipped = 0;
            int RecordsError = 0;
            var BatchUploadDate = Utilities.GetCurrentUtcDate();

            var log = new StringBuilder();

            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Begin loading Dispute Fees...");
            log.AppendLine();

            try
            {
                var AllArbitratorFees = await _context.ArbitratorFees.AsNoTracking().ToListAsync();
                var AllAuthorityFees = await _context.AuthorityFees.AsNoTracking().ToListAsync();

                await EnsureAuthorities(); ;
                var NSAAuthority = Authorities.FirstOrDefault(d => d.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase))!;
                if (NewFeeItems.Count(d => d.AuthorityKey.Equals("nsa", StringComparison.CurrentCultureIgnoreCase)) > 0)
                {
                    log.AppendLine("WARNING: Non-NSA data detected. If you are intentionally uploading non-NSA records you can ignore this warning. Otherwise, verify the Authority Id in your upload file!!!");
                }

                var CalcVars = await Utilities.GetCalculatorVariablesAsync(_context);

                // Make a cache for speed
                var DisputeNumbers = await (from d in _context.AuthorityDisputes
                                            join a in _context.Authorities on d.AuthorityId equals a.Id
                                            select new { d.Id, d.Authority, AuthorityId = a.Id, d.AuthorityCaseId }).ToArrayAsync();

                // Group the Fees so we can limit db calls
                var DisputeGroups = NewFeeItems.GroupBy(c => new { c.AuthorityKey, c.AuthorityCaseId }).Select(g => new { g.Key.AuthorityKey, g.Key.AuthorityCaseId }).ToArray();

                foreach (var Dispute in DisputeGroups)
                {
                    var AuthorityKey = Dispute.AuthorityKey;
                    var AuthorityObj = Authorities.FirstOrDefault(d => d.Key.Equals(AuthorityKey, StringComparison.CurrentCultureIgnoreCase));
                    var AuthorityCaseId = Dispute.AuthorityCaseId;

                    var NewFees = NewFeeItems.Where(d => d.AuthorityKey.Equals(AuthorityKey, StringComparison.CurrentCultureIgnoreCase) && d.AuthorityCaseId == AuthorityCaseId);
                    RecordsProcessed += NewFees.Count();
                    bool HasChanges = false;

                    // Ensure accurate Authority foreign key
                    if (AuthorityObj == null)
                    {
                        RecordsSkipped += NewFees.Count();
                        log.AppendLine($@"{BatchUploadDate.ToString("R")}: ERROR: Unknown AuthorityKey {AuthorityKey}. Skipping {NewFees.Count()} Fees.");
                        continue;
                    }

                    var AuthorityId = AuthorityObj.Id;

                    // Ensure Dispute Header already exists
                    var Check = DisputeNumbers.FirstOrDefault(v => v.AuthorityId == AuthorityId && v.AuthorityCaseId.Equals(AuthorityCaseId, StringComparison.CurrentCultureIgnoreCase));
                    if (Check == null)
                    {
                        RecordsSkipped += NewFees.Count();
                        log.AppendLine($@"{BatchUploadDate.ToString("R")}: Dispute Header {AuthorityCaseId} could not be found for Authority {AuthorityId}. Skipping!");
                        continue;
                    }

                    // following row should never* return null unless a record gets physically deleted while the import is running and the app doesn't allow this so...
                    AuthorityDispute? DisputeHeader = await _context.AuthorityDisputes.Include(x => x.DisputeCPTs).ThenInclude(d => d.ClaimCPT).AsNoTracking().FirstOrDefaultAsync(d => d.AuthorityId == AuthorityId && d.AuthorityCaseId == AuthorityCaseId);

                    foreach (var Fee in NewFees)
                    {
                        // find the BaseFee by Name and Type
                        BaseFee? FeeTemplate = null;

                        if (Fee.FeeRecipient == FeeRecipient.Arbitrator)
                            FeeTemplate = AllArbitratorFees.FirstOrDefault(d => d.FeeName!.Equals(Fee.FeeName, StringComparison.CurrentCultureIgnoreCase));
                        if (Fee.FeeRecipient == FeeRecipient.Authority)
                            FeeTemplate = AllAuthorityFees.FirstOrDefault(d => d.FeeName!.Equals(Fee.FeeName, StringComparison.CurrentCultureIgnoreCase));

                        if (FeeTemplate == null)
                        {
                            RecordsSkipped++;
                            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Could not locate a(n) {Enum.GetName<FeeRecipient>(Fee.FeeRecipient)} fee named {Fee.FeeName}. Skipping Fee");
                            continue;
                        }

                        AuthorityDisputeFee? CurrentFee = null;
                        CurrentFee = DisputeHeader!.Fees.FirstOrDefault(d => d.BaseFeeId == FeeTemplate.Id && d.FeeRecipient == Fee.FeeRecipient);

                        if (CurrentFee == null)
                        {
                            // Add new CPT to Header
                            CurrentFee = new AuthorityDisputeFee
                            {
                                AmountDue = Fee.AmountDue,
                                AuthorityDisputeId = DisputeHeader!.Id,
                                BaseFeeId = FeeTemplate.Id,
                                DueOn = Fee.DueOn,
                                FeeRecipient = Fee.FeeRecipient,
                                InvoiceLink = Fee.InvoiceLink,
                                InvoiceReceivedOn = Fee.InvoiceReceivedOn,
                                IsRefundable = Fee.IsRefundable,
                                IsRequired = Fee.IsRequired,
                                PaidBy = Fee.PaidBy,
                                PaidOn = Fee.PaidOn,
                                PaymentMethod = Fee.PaymentMethod,
                                PaymentReferenceNumber = Fee.PaymentReferenceNumber,
                                PaymentRequestedOn = Fee.PaymentRequestedOn,
                                RefundableAmount = Fee.RefundableAmount,
                                RefundAmount = Fee.RefundAmount,
                                RefundDueOn = Fee.RefundDueOn,
                                RefundedOn = Fee.RefundedOn,
                                RefundedTo = Fee.RefundedTo,
                                RefundMethod = Fee.RefundMethod,
                                RefundReferenceNumber = Fee.RefundReferenceNumber,
                                RefundRequestedBy = Fee.RefundRequestedBy,
                                RefundRequestedOn = Fee.RefundRequestedOn,
                                WasRefunded = Fee.WasRefunded,
                                WasRefundRequested = Fee.WasRefundRequested,
                                UpdatedBy = CurrentUser.Email,
                                UpdatedOn = Utilities.GetCurrentUtcDate()
                            };

                            // TODO: calculate other stuff
                            HasChanges = true;
                            _context.AuthorityDisputeFees.Add(CurrentFee);
                            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Successfully added {Enum.GetName<FeeRecipient>(Fee.FeeRecipient)} Fee {Fee.FeeName} to AuthorityCaseId {AuthorityObj.Key}-{AuthorityCaseId}.");

                        }
                        else
                        {
                            // Update CPT with new data if it exists
                            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Updating existing Dispute Fees is not supported.");
                        }
                    }

                    if (HasChanges)
                    {
                        try
                        {
                            await _context.SaveChangesAsync();

                        }
                        catch (Exception ex)
                        {
                            RecordsError++;
                            var Message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                            log.AppendLine(Message);
                            _context.ChangeTracker.Clear();
                        }
                        finally
                        {
                            // update real-time status record
                            if (CurrentJob != null)
                            {
                                await ImportUtils.UpdateJob(_errorContext, CurrentJob, $@"Importing Dispute Fee records", "importing", RecordsProcessed, RecordsError, RecordsProcessed, RecordsSkipped, RecordCount, 0);
                            }
                        }
                    }
                }

                await ImportUtils.UpdateJob(_errorContext, CurrentJob, "Job complete.", "finished");
            }
            catch (Exception ex)
            {
                string Message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;

                await ImportUtils.UpdateJob(_errorContext, CurrentJob, "Job complete.", "error", RecordsProcessed, RecordCount, Message);
                log.AppendLine($@"{BatchUploadDate.ToString("R")}: Unexpected error while processing.");
                log.AppendLine(Message);
            }

            try
            {
                log.AppendLine($@"{BatchUploadDate.ToString("R")}: Finish Dispute Fees load");
                string url = await SaveUploadLog("DisputeFees", "system", BatchUploadDate, log.ToString());
                if (url.StartsWith("HTTP", StringComparison.InvariantCultureIgnoreCase))
                    await ImportUtils.UpdateJob(_errorContext, CurrentJob, "Job log: " + url, "finished");
                if (_context.ChangeTracker.HasChanges())
                    await _context.SaveChangesAsync();
                if (_errorContext.ChangeTracker.HasChanges())
                    await _errorContext.SaveChangesAsync();
            }
            catch { }
        }

        public async void ImportDisputeHeadersAsync(IEnumerable<AuthorityDisputeCSV> HeaderRecords, AppUser CurrentUser, JobQueueItem? CurrentJob)
        {
            if (CurrentJob != null)
            {
                _errorContext.Entry(CurrentJob).State = EntityState.Unchanged;
            }

            var NewHeaderRecords = HeaderRecords.Where(d => !string.IsNullOrEmpty(d.AuthorityCaseId)).ToImmutableArray();
            int RecordCount = NewHeaderRecords.Count();

            if (RecordCount == 0)
                throw new Exception("No Dispute Header records found to process!");

            int RecordsProcessed = 0;
            int RecordsSkipped = 0;
            int RecordsError = 0;
            var BatchUploadDate = Utilities.GetCurrentUtcDate();
            List<AuthorityStatusMapping>? mappings = null;

            var log = new StringBuilder();

            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Begin loading Dispute Headers...");
            log.AppendLine();

            try
            {
                await Extensions.EnsureHolidays(_context);
                await EnsureAuthorities(); ;
                var ArbitratorIDs = await _context.Arbitrators.Select(d => d.Id).ToArrayAsync();

                var disputeNumbers = await (from d in _context.AuthorityDisputes
                                            select new { d.AuthorityId, d.AuthorityCaseId }).ToArrayAsync();

                string[]? AuthorityStatuses = null;
                Authority? AuthorityObj = null;

                foreach (var rec in NewHeaderRecords)
                {
                    RecordsProcessed++;

                    if (AuthorityObj == null || rec.AuthorityKey != AuthorityObj.Key)
                    {
                        AuthorityObj = Authorities.FirstOrDefault(d => d.Key == rec.AuthorityKey);
                        if (AuthorityObj == null)
                        {
                            RecordsSkipped++;
                            log.AppendLine($@"{BatchUploadDate.ToString("R")}: {rec.AuthorityCaseId} has an invalid Authority Id. Skipping!");
                            continue;
                        }
                        AuthorityStatuses = AuthorityObj.StatusValues.Split(new char[] { ';', ',' }).Select(s => s.Trim().ToLower()).ToArray();
                        mappings = AuthorityObj.AuthorityJson?.StatusMappings;
                    }

                    var TrackingConfigs = AuthorityObj.TrackingDetails.Where(d => d.Scope == AuthorityTrackingDetailScope.AuthorityDispute).ToList();

                    if (disputeNumbers.FirstOrDefault(v => v.AuthorityId == AuthorityObj.Id && v.AuthorityCaseId.Equals(rec.AuthorityCaseId, StringComparison.CurrentCultureIgnoreCase)) != null)
                    {
                        RecordsSkipped++;
                        log.AppendLine($@"{BatchUploadDate.ToString("R")}: {rec.AuthorityCaseId} already exists. Skipping!");
                        continue;
                    }

                    if (rec.ArbitratorId != null && (ArbitratorIDs == null || !ArbitratorIDs.Contains(rec.ArbitratorId.Value)))
                    {
                        RecordsSkipped++;
                        log.AppendLine($@"{BatchUploadDate.ToString("R")}: {rec.AuthorityCaseId} refers to invalid Arbitrator Id {rec.ArbitratorId.Value}. Skipping!");
                        continue;
                    }

                    if (!rec.AuthorityStatus.Equals("not submitted", StringComparison.CurrentCultureIgnoreCase) && !AuthorityStatuses!.Contains(rec.AuthorityStatus, StringComparer.CurrentCultureIgnoreCase))
                    {
                        RecordsSkipped++;
                        log.AppendLine($@"{BatchUploadDate.ToString("R")}: {rec.AuthorityCaseId} has an invalid Authority Status specified. Skipping!");
                        continue;
                    }


                    var NewDispute = new AuthorityDispute
                    {
                        ArbitratorId = rec.ArbitratorId,
                        ArbitrationResult = GetArbitrationResultFromAuthorityStatus(mappings, rec.AuthorityStatus),
                        ArbitratorSelectedOn = rec.ArbitratorSelectedOn,
                        AuthorityCaseId = rec.AuthorityCaseId,
                        AuthorityId = AuthorityObj.Id,
                        AuthorityStatus = rec.AuthorityStatus,
                        BriefApprovedBy = rec.BriefApprovedBy,
                        BriefApprovedOn = rec.BriefApprovedOn,
                        BriefPreparationCompletedOn = rec.BriefPreparationCompletedOn,
                        BriefPreparer = rec.BriefPreparer,
                        BriefWriter = rec.BriefWriter,
                        BriefWriterCompletedOn = rec.BriefWriterCompletedOn,
                        CreatedBy = rec.CreatedBy,
                        CreatedOn = rec.CreatedOn,
                        IneligibilityAction = rec.IneligibilityAction,
                        IneligibilityReasons = rec.IneligibilityReasons,
                        SubmissionDate = rec.SubmissionDate,
                        TrackingValues = rec.TrackingValues,
                        UpdatedBy = rec.UpdatedBy,
                        WorkflowStatus = GetWorkflowStatusFromAuthorityStatus(mappings, rec.AuthorityStatus),
                        UpdatedOn = rec.UpdatedOn
                    };

                    if (TrackingConfigs.Count > 0)
                    {
                        var ValueNode = JsonNode.Parse(string.IsNullOrWhiteSpace(rec.TrackingValues) ? "{}" : rec.TrackingValues);
                        Utilities.UpdateTrackingCalculations(ValueNode!, TrackingConfigs, NewDispute);
                        NewDispute.TrackingValues = ValueNode!.ToJsonString();
                    }
                    _context.AuthorityDisputes.Add(NewDispute);

                    bool WasAdded = false;
                    try
                    {
                        await _context.SaveChangesAsync();
                        WasAdded = true;
                        log.AppendLine($@"{BatchUploadDate.ToString("R")}: Added {rec.AuthorityCaseId}");
                    }
                    catch (Exception ex)
                    {
                        RecordsError++;
                        var Message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                        log.AppendLine(Message);
                        _context.ChangeTracker.Clear();
                    }

                    if (WasAdded)
                    {
                        try
                        {
                            var entry = new AuthorityDisputeLog
                            {
                                Action = "DisputeHeaderImport", // TODO: Make this an enum throughout the app
                                AuthorityDisputeId = NewDispute.Id,
                                CreatedBy = CurrentUser.Email,
                                CreatedOn = BatchUploadDate,
                                Details = "New Dispute created",
                                Id = 0
                            };
                            NewDispute.ChangeLog.Add(entry);
                            await _context.SaveChangesAsync();
                        }
                        catch
                        {
                            _context.ChangeTracker.Clear();
                        }
                    }

                    // update real-time status record
                    if (CurrentJob != null && (RecordsProcessed % 25 == 0 || RecordsProcessed == RecordCount))
                    {
                        await ImportUtils.UpdateJob(_errorContext, CurrentJob, $@"Importing Dispute Header records", "importing", RecordsProcessed, RecordsError, RecordsProcessed, RecordsSkipped, RecordCount, 0);
                    }
                }

                await ImportUtils.UpdateJob(_errorContext, CurrentJob, "Job complete.", "finished");
            }
            catch (Exception ex)
            {
                string Message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;

                await ImportUtils.UpdateJob(_errorContext, CurrentJob, "Job complete.", "error", RecordsProcessed, RecordCount, Message);
                log.AppendLine($@"{BatchUploadDate.ToString("R")}: Error retrieving static list of all Dispute numbers. ");
                log.AppendLine(Message);
            }

            try
            {
                log.AppendLine($@"{BatchUploadDate.ToString("R")}: Finish Dispute Header load");
                string url = await SaveUploadLog("DisputeHeaders", "system", BatchUploadDate, log.ToString());
                if (url.StartsWith("HTTP", StringComparison.InvariantCultureIgnoreCase))
                    await ImportUtils.UpdateJob(_errorContext, CurrentJob, "Job log: " + url, "finished");
                if (_context.ChangeTracker.HasChanges())
                    await _context.SaveChangesAsync();
                if (_errorContext.ChangeTracker.HasChanges())
                    await _errorContext.SaveChangesAsync();
            }
            catch { }
        }

        public async void ImportDisputeNotesAsync(IEnumerable<AuthorityDisputeNoteCSV> Records, AppUser CurrentUser, JobQueueItem? CurrentJob)
        {
            if (CurrentJob != null)
            {
                _errorContext.Entry(CurrentJob).State = EntityState.Unchanged;
            }

            var NewNotes = Records.Where(d => !string.IsNullOrEmpty(d.AuthorityKey) && !string.IsNullOrEmpty(d.AuthorityCaseId) && !string.IsNullOrEmpty(d.Details)).ToImmutableArray();
            int RecordCount = NewNotes.Count();

            if (RecordCount == 0)
                throw new Exception("No Dispute Notes records found to process!");

            int RecordsProcessed = 0;
            int RecordsSkipped = 0;
            int RecordsError = 0;
            var BatchUploadDate = Utilities.GetCurrentUtcDate();

            var log = new StringBuilder();

            log.AppendLine($@"{BatchUploadDate.ToString("R")}: Begin loading Dispute Notes...");
            log.AppendLine();

            try
            {
                await EnsureAuthorities();
                var NSAAuthority = Authorities.FirstOrDefault(d => d.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase))!;
                if (NewNotes.Count(d => d.AuthorityKey.Equals("nsa", StringComparison.CurrentCultureIgnoreCase)) > 0)
                {
                    log.AppendLine("WARNING: Non-NSA data detected. If you are intentionally uploading non-NSA records you can ignore this warning. Otherwise, verify the Authority Id in your upload file!!!");
                }

                var disputeNumbers = await (from d in _context.AuthorityDisputes
                                            select new { d.AuthorityId, d.AuthorityCaseId }).ToArrayAsync();

                Authority? AuthorityObj = null;
                AuthorityDispute? DisputeHeader = null;

                foreach (var NewNote in NewNotes)
                {
                    RecordsProcessed++;

                    if (AuthorityObj == null || NewNote.AuthorityKey != AuthorityObj.Key)
                    {
                        AuthorityObj = Authorities.FirstOrDefault(d => d.Key.Equals(NewNote.AuthorityKey, StringComparison.CurrentCultureIgnoreCase));
                        if (AuthorityObj == null)
                        {
                            RecordsSkipped++;
                            log.AppendLine($@"{BatchUploadDate.ToString("R")}: {NewNote.AuthorityCaseId} has an invalid Authority Key. Skipping!");
                            continue;
                        }
                    }

                    if (disputeNumbers.FirstOrDefault(v => v.AuthorityId == AuthorityObj.Id && v.AuthorityCaseId.Equals(NewNote.AuthorityCaseId, StringComparison.CurrentCultureIgnoreCase)) == null)
                    {
                        RecordsSkipped++;
                        log.AppendLine($@"{BatchUploadDate.ToString("R")}: Dispute {NewNote.AuthorityKey}-{NewNote.AuthorityCaseId} does not exist. You must create the Dispute before you can attach Notes to it. Skipping!");
                        continue;
                    }

                    if (DisputeHeader == null || DisputeHeader.AuthorityId != AuthorityObj.Id || DisputeHeader.AuthorityCaseId != NewNote.AuthorityCaseId)
                    {
                        DisputeHeader = await _context.AuthorityDisputes.Include(d => d.Notes).FirstOrDefaultAsync(d => d.AuthorityId == AuthorityObj.Id && NewNote.AuthorityCaseId == NewNote.AuthorityCaseId);
                    }

                    var Note = new AuthorityDisputeNote
                    {
                        AuthorityDisputeId = DisputeHeader!.Id,
                        Details = NewNote.Details,
                        UpdatedBy = CurrentUser.Email,
                        UpdatedOn = Utilities.GetCurrentUtcDate()
                    };

                    _context.AuthorityDisputeNotes.Add(Note);
                    log.AppendLine($@"{BatchUploadDate.ToString("R")}: Successfully added DisputeNote to AuthorityCaseId {AuthorityObj.Key}-{NewNote.AuthorityCaseId}.");

                    if (RecordsProcessed % 25 == 0 || RecordsProcessed == RecordCount)
                    {
                        try
                        {
                            await _context.SaveChangesAsync();

                        }
                        catch (Exception ex)
                        {
                            RecordsError++;
                            var Message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                            log.AppendLine(Message);
                            _context.ChangeTracker.Clear();
                        }
                        finally
                        {
                            // update real-time status record
                            if (CurrentJob != null)
                            {
                                await ImportUtils.UpdateJob(_errorContext, CurrentJob, $@"Importing Dispute Notes records", "importing", RecordsProcessed, RecordsError, RecordsProcessed, RecordsSkipped, RecordCount, 0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string Message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;

                await ImportUtils.UpdateJob(_errorContext, CurrentJob, "Job complete.", "error", RecordsProcessed, RecordCount, Message);
                log.AppendLine($@"{BatchUploadDate.ToString("R")}: Unexpected error while processing.");
                log.AppendLine(Message);
            }

            try
            {
                log.AppendLine($@"{BatchUploadDate.ToString("R")}: Finish Dispute Notes load");
                string url = await SaveUploadLog("DisputeNotes", "system", BatchUploadDate, log.ToString());
                if (url.StartsWith("HTTP", StringComparison.InvariantCultureIgnoreCase))
                    await ImportUtils.UpdateJob(_errorContext, CurrentJob, "Job log: " + url, "finished");
                if (_context.ChangeTracker.HasChanges())
                    await _context.SaveChangesAsync();
                if (_errorContext.ChangeTracker.HasChanges())
                    await _errorContext.SaveChangesAsync();
            }
            catch { }
        }

        public async void ImportEHR(IEnumerable<string> csvLines, EHRRecordType recordType, AppUser runAs, JobQueueItem? job = null)
        {
            var log = new StringBuilder();
            var batchUploadDate = Utilities.GetCurrentUtcDate();
            const bool IS_DRY_RUN = false;
            const bool CREATE_CASE_IF_MISSING = true;

            try
            {
                if (job != null)
                {
                    _errorContext.Entry(job).State = EntityState.Unchanged;
                }

                if (csvLines.Count() == 0)
                    throw new Exception("No records found to process!");
                var source = recordType == EHRRecordType.Header ? "EHRHeader" : "EHRDetail";
                var columnList = await _context.ImportFieldConfigs.Where(d => d.Source.Equals(source)).ToListAsync();
                if (columnList.Count() == 0)
                    throw new Exception($@"There are no import configurations available for the {source} source.");
                ///-------------------------
                int recordsCreated = 0;
                int recordsError = 0;
                int recordsFound = 0;
                int recordsSkipped = 0;
                int rowCount = 0;
                var nsa = Authorities.FirstOrDefault(d => d.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase));
                List<int> affectedHeaders = new List<int>();
                var fieldList = new Dictionary<int, string>();
                Regex CSVParser = new Regex("[,|](?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                ///-------------------------
                await EnsureAuthorities();
                await EnsureCustomers();
                await EnsurePayors();
                await Extensions.EnsureHolidays(_context);
                ///-------------------------
                var requiredCols = columnList.Where(d => d.IsRequired && d.IsActive).Select(d => d.SourceFieldname).Distinct().ToList();
                var importConfigs = columnList.Where(d => d.IsActive && d.Action != ImportFieldAction.Ignore).ToList();

                string[] allowEmptyCols = columnList.Where(d => d.IsRequired && d.CanBeEmpty).Select(d => d.SourceFieldname).ToArray();
                int checksumSequence = 0; // 1 + 2 + 3 ... but exclude any optional columns
                for (int i = 0; i < requiredCols.Count(); i++)
                {
                    if (Array.IndexOf(allowEmptyCols, requiredCols[i]) == -1)
                        checksumSequence += i + 1;
                }

                log.AppendLine($@"Import EHR {source} Data on {Utilities.GetCurrentCSTDate2().ToString("MM/dd/yyyy hh:mm tt")} CST");
                log.AppendLine($@"Initializing...");
                foreach (string cvsRow in csvLines)
                {
                    rowCount++;
                    // validate the header columns and make sure the required ones are present
                    if (rowCount == 1)
                    {
                        if (string.IsNullOrEmpty(cvsRow))
                            throw new Exception("Missing row headers on row 1");

                        var headerCols = CSVParser.Split(cvsRow);
                        headerCols.ApplyToEach(x => fieldList[fieldList.Count] = x.Replace(@"""", ""));

                        var requiredColsNotFound = requiredCols.Except(fieldList.Values.ToList<string>()).ToList();

                        if (requiredColsNotFound.Count() > 0)
                            throw new Exception("Header row is missing required column(s): " + requiredColsNotFound.StringJoin(", "));


                        log.AppendLine("All required header columns detected. Starting data import...");
                        continue;
                    }

                    // BEGIN: Process data row
                    if (string.IsNullOrEmpty(cvsRow))
                    {
                        log.AppendLine($@"Line {rowCount}: Skipping empty line.");
                        continue;
                    }


                    var csvColumns = CSVParser.Split(cvsRow);
                    if (csvColumns.Length != fieldList.Count())

                    {
                        log.AppendLine($@"Line {rowCount}: Value count mismatch. Skipping line.");
                        continue;
                    }

                    // use the requiredFields of the each CSV line to create criteria for finding an existing ArbitrationCase record 

                    //var configs = fieldList.Select(d => new ImportFieldConfig() { Action = ImportFieldAction.Always, IsActive = true, Source = source, SourceFieldname = d.Value, TargetFieldname = d.Value }).ToList();
                    var caseRecord = new ArbitrationCase();
                    var detailRecord = new ImportDetail();

                    // use Reflection to fill out a new object
                    IEnumerable<PropertyInfo> arbProps = recordType == EHRRecordType.Header ? caseRecord.GetType().GetProperties().Where(d => d.GetIndexParameters().Length == 0) : detailRecord.GetType().GetProperties().Where(d => d.GetIndexParameters().Length == 0);
                    int foundChecksum = 0;
                    var used = new List<string>();
                    var requiredColsAry = requiredCols.ToArray();

                    foreach (var field in fieldList)
                    {
                        // Per DevOps #1597 - Trimming all values
                        string columnValue = csvColumns[field.Key].Trim();  // key is the integer index of the column
                        bool isUnique = !used.Contains(field.Value);
                        int reqNdx = !isUnique || Array.IndexOf(allowEmptyCols, field.Value) >= 0 ? -1 : Array.IndexOf(requiredColsAry, field.Value); // is this field one of the required ones (not optional)? if so, grab its index
                        if (isUnique)
                            used.Add(field.Value);
                        int checksum = !string.IsNullOrEmpty(columnValue) && reqNdx >= 0 ? reqNdx + 1 : 0;  // the +1 accounts for the possibility that the first column (zero index) is required
                        foundChecksum += checksum; // if the current column is required and has a value, add the column index to a checksum that we'll use at the end
                        PropertyInfo? arbProp = arbProps.Where(d => d.Name.Equals(field.Value, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                        if (arbProp != null)
                        {
                            ValueCopyResult result = recordType == EHRRecordType.Header ? CopySourceValueToTargetValue(caseRecord, arbProp, columnValue, log) : CopySourceValueToTargetValue(detailRecord, arbProp, columnValue, log);
                            if (result == ValueCopyResult.Error)
                                log.AppendLine($@"Line {rowCount}, Column {field.Value}: Error converting value to target data-type");
                            else if (result == ValueCopyResult.UnknownType)
                                log.AppendLine($@"Line {rowCount}, Column {field.Value}: Unhandled data-type for property ({arbProp.PropertyType.ToString()})");
                        }
                        else
                        {
                            log.AppendLine($@"Line {rowCount}: Skipping unknown column {field.Value}");
                        }
                    }

                    if (foundChecksum != checksumSequence)
                    {
                        log.AppendLine($@"Line {rowCount}: One or more required value(s) is missing. Skipping line.");
                        recordsSkipped++;
                        continue;
                    }

                    IEHRRecord temp = recordType == EHRRecordType.Header ? caseRecord : detailRecord;
                    detailRecord.LocationGeoZip = "750";
                    caseRecord.LocationGeoZip = "750";
                    RecordImportResults recordImportResults;
                    if (recordType == EHRRecordType.Header)
                    {
                        int ndx = importConfigs.FindIndex(d => d.TargetFieldname.Equals("AuthorityCaseId"));
                        if (ndx != -1)
                        {
                            importConfigs.RemoveAt(ndx);
                            log.AppendLine("**** Attempt to update AuthorityCaseId using EHR Header was disallowed!");
                        }
                        recordImportResults = await FindAndUpdateEHRHeader(caseRecord, importConfigs, rowCount, batchUploadDate, log, IS_DRY_RUN, CREATE_CASE_IF_MISSING, runAs, nsa);
                    }
                    else
                    {
                        if (detailRecord.CPTCode.Length < 5)
                        {
                            log.AppendLine($@"Line {rowCount}: Invalid CPTCode. Length must be 5 or more.");
                            recordsSkipped++;
                            continue;
                        }
#pragma warning disable CS8604 // Possible null reference argument.
                        recordImportResults = await FindAndUpdateEHRDetail(detailRecord, importConfigs, rowCount, batchUploadDate, log, IS_DRY_RUN, runAs, nsa);
#pragma warning restore CS8604 // Possible null reference argument.
                    }

                    if (recordImportResults.RecordImportResult == RecordImportActionResult.Updated)
                        recordsFound++;
                    else if (recordImportResults.RecordImportResult == RecordImportActionResult.Added)
                        recordsCreated++;
                    else if (recordImportResults.RecordImportResult == RecordImportActionResult.Skipped)
                        recordsSkipped++;
                    else if (recordImportResults.RecordImportResult == RecordImportActionResult.Error)
                        recordsError++;

                    if (recordType == EHRRecordType.Detail && (recordImportResults.RecordImportResult == RecordImportActionResult.Added || recordImportResults.RecordImportResult == RecordImportActionResult.Updated))
                        affectedHeaders.Add(recordImportResults.ArbitrationCaseId);

                    // update real-time status record
                    if (job != null && rowCount % 25 == 0 || rowCount == csvLines.Count())
                    {
                        await ImportUtils.UpdateJob(_errorContext, job, $@"Importing EHR {recordType.ToString()} records", "importing", recordsCreated, recordsError, rowCount, recordsSkipped, csvLines.Count(), recordsFound);
                    }
                }

                if (recordType == EHRRecordType.Detail)
                {
                    await ImportUtils.UpdateJob(_errorContext, job, "Recalculating headers with modified detail records", "calculating");

                    log.AppendLine("**** Begin recalculating ArbitrationCase (header) values...");

                    // use the distinct list of updated and added ArbitrationCase IDs to go back and update the extended header values to support denormalized data queries and stats
                    foreach (int id in affectedHeaders.Distinct())
                    {
                        try
                        {
                            await RecalculateCaseHeader(id);
                            log.AppendLine($@"Case header for id:{id} recalculated successfully.");
                        }
                        catch (Exception ex)
                        {
                            log.AppendLine($@"ERROR recalculateCaseHeader({id}): {ex.Message}");
                        }
                    }

                    await ImportUtils.UpdateJob(_errorContext, job, "Uploading job log", "finalizing");
                }

                // log what happened during the run
                log.AppendLine($@"Read {rowCount} total lines from upload file");
                log.AppendLine($@"Added {recordsCreated} new records");  // TODO: Modify the return type of findAndUpdateEHRDetail to detect if record was updated or added
                log.AppendLine($@"Updated {recordsFound} existing records");
                log.AppendLine($@"Skipped {recordsSkipped} rows");
                log.AppendLine($@"Errors process {recordsError} rows");

                if (!IS_DRY_RUN && (recordsCreated > 0 || recordsFound > 0))
                {
                    _context.SaveChangesFailed += (object? sender, SaveChangesFailedEventArgs e) =>
                    {
                        log.AppendLine(e.Exception.Message);
                    };

                    _logger.LogInformation("ImportEHR: Calling SaveChangesAsync...");
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("ImportEHR: SaveChangesAsync complete. App Log to follow.");
                    _logger.LogWarning(log.ToString());
                }
                else
                {
                    _logger.LogWarning(log.ToString());
                }

                await ImportUtils.UpdateJob(_errorContext, job, "Job complete.", "finished");
            }
            catch (Exception ex)
            {
                log.AppendLine(ex.Message);
                await ImportUtils.UpdateJob(_errorContext, job, ex.Message, "error"); // if the client happens to catch this update it can do what it wants with it
            }
            finally
            {
                //await ImportUtils.UpdateJob(_errorContext, job, "Job complete.", "finished");

                log.AppendLine($@"Finished processing at {Utilities.GetCurrentCSTDate2().ToString("MM/dd/yyyy hh:mm tt")} CST");

                //_memoryCache.Remove(CACHE_KEY);
                var v = Enum.GetName(typeof(EHRRecordType), recordType) ?? "EHRImport";
                try
                {
                    await SaveUploadLog("EHR" + v, "system", batchUploadDate, log.ToString());
                    if (_errorContext.ChangeTracker.HasChanges())
                        await _errorContext.SaveChangesAsync();
                }
                catch { }
            }
        }

        public async void ImportAuthorityCases(Authority authority, IEnumerable<string> upload, AppUser initiator, JobQueueItem? job = null)
        {
            var batchUploadDate = Utilities.GetCurrentUtcDate();
            int reqFieldsFound = 0;
            var log = new StringBuilder();

            int headerRow = 0;
            int recordsFound = 0;
            int recordsSkipped = 0;
            int rowCount = 0;
            bool headerFound = false;
            List<string> matches = new List<string>();

            log = new StringBuilder();

            var CSVParser = new Regex("[,|](?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

            try
            {
                string authorityKey = authority.Key.ToLower();
                string authorityConfigKey = authorityKey + "-RequestDetails";
                log.AppendLine($@"Import Data from {authorityKey} on {Utilities.GetCurrentCSTDate2().ToString("MM/dd/yyyy hh:mm tt")} CST");

                var fieldList = await _context.ImportFieldConfigs.Where(d => d.Source.Equals(authorityConfigKey)).ToListAsync();
                if (fieldList.Count() == 0)
                    throw new Exception($@"There are no import configurations available for Authority '{authorityKey}'.");
                var requiredFields = fieldList.Where(d => d.IsRequired && d.IsActive).Select(d => d.SourceFieldname.ToLower()).ToArray();
                if (requiredFields.Count() == 0)
                    throw new Exception($@"The import configurations for Authority '{authorityKey}' must specify at least one required column.");

                foreach (string row in upload)
                {
                    rowCount++;

                    // try split
                    if (!string.IsNullOrEmpty(row))
                    {
                        var cols = CSVParser.Split(row);

                        if (cols.Length > 2)
                        {
                            // scan down the file until a header row is found
                            if (!headerFound)
                            {
                                // is current line the header row?
                                foreach (var col in cols)
                                {
                                    var low = col.Trim().ToLower();
                                    if (fieldList.FirstOrDefault(d => d.SourceFieldname == low) != null)
                                    {
                                        matches.Add(low);
                                        if (requiredFields != null)
                                        {
                                            reqFieldsFound += requiredFields.FirstOrDefault(d => d == low) == null ? 0 : 1;
                                        }
                                    }
                                    else
                                        matches.Add("");
                                }
                                // basic validation - if we find 3+ matching column names on this row then assume it is the header
                                // this will tolerate the government mucking about with the export format
                                // and not totally breaking our import immediately although some columns may not come in
                                headerFound = matches.Count(d => !string.IsNullOrEmpty(d)) > 2;
                                if (!headerFound)
                                {
                                    matches.Clear();
                                    reqFieldsFound = 0;
                                }
                                else if (requiredFields != null && reqFieldsFound != requiredFields.Count())
                                {
                                    throw new Exception("One or more of these required columns is missing:" + String.Join(',', requiredFields));
                                }
                                else
                                {
                                    headerRow = rowCount;
                                }
                            }
                            // process the row if the column count matches
                            else if (cols.Length == matches.Count())
                            {
                                // start processing detail records after we've found the header
                                // this is because the scraped upload file can often include some junk
                                // headers / titles at the top
                                int ndx = 0;
                                var json = new StringBuilder("{"); // build a json object we can deserialize into an EF object for insertion

                                foreach (var col in cols)
                                {
                                    if (!string.IsNullOrEmpty(matches[ndx]))
                                    {
                                        // use this column since we matched a field name in the header detection phase
                                        var m = matches[ndx];
                                        var field = fieldList.First(d => d.SourceFieldname == m);
                                        json.Append($@"""{field.TargetFieldname}"":");
                                        if (field.IsDate)
                                        {
                                            if (col == "")
                                            {
                                                json.Append("null,");
                                            }
                                            else if (DateTime.TryParse(col.Replace("\"", "").Trim(), out DateTime dt))
                                            {
                                                json.Append($@"""{dt.ToString("yyyy-MM-ddT00:00:00")}"",");
                                            }
                                            else
                                            {
                                                json.Append("null,");
                                            }
                                        }
                                        else if (field.IsNumeric)
                                        {
                                            Double.TryParse(col.Replace("\"", "").Trim().Replace(",", "").Replace("$", ""), out double count);
                                            json.Append($@"{count},");  // TODO: Make this nullable instead of defaulting to zero?
                                        }
                                        else if (field.IsBoolean)
                                        {
                                            bool.TryParse(col.Replace("\"", "").Trim(), out bool b);
                                            json.Append($@"{b},");
                                        }
                                        else
                                        {
                                            json.Append($@"""{col.Replace("\"", "").Trim()}"",");
                                        }
                                    }
                                    ndx++;
                                }

                                json.Length = json.Length - 1;
                                json.Append("}");
                                recordsFound++;

                                // save a JSON version of the imported text row into the database
                                // this will later be used with the CaseSync import configurations
                                // to update the ArbitrationCase record
                                if (json.Length > 3)
                                {
                                    var rec = new AuthorityImportDetails();
                                    rec.BatchUploadDate = batchUploadDate.ToUniversalTime();
                                    rec.UploadedBy = initiator.Email;
                                    rec.AuthorityId = authority.Id;
                                    rec.JSON = json.ToString();
                                    _context.AuthorityImportDetails.Add(rec);
                                }

                                // reset
                                json.Clear();
                            }
                            // skip goof ball rows
                            else
                            {
                                recordsSkipped++;
                                log.AppendLine($@"Column count mismatch! Skipped content to follow:.");
                                log.AppendLine(row);
                            }
                        }
                    }
                }

                // log what happened
                log.AppendLine($@"Read {rowCount} total lines from upload file");
                if (!headerFound)
                {
                    log.AppendLine("Header row not found! Nothing to do.");
                }
                else
                {
                    log.AppendLine($@"Header row found on row {headerRow}");
                    log.AppendLine($@"Imported {recordsFound} records");
                    log.AppendLine($@"Skipped {recordsSkipped} rows / records");
                }


                if (recordsFound > 0)
                {
                    _context.SaveChangesFailed += (object? sender, SaveChangesFailedEventArgs e) =>
                    {
                        log.AppendLine(e.Exception.Message);
                    };

                    await _context.SaveChangesAsync();
                    _logger.LogWarning(log.ToString());
                }
                else
                {
                    log.AppendLine("No valid data found in upload");
                    _logger.LogWarning(log.ToString());
                }
            }
            catch (Exception ex)
            {
                log.AppendLine(ex.Message);
            }
            finally
            {
                if (log != null)
                    await SaveUploadLog("AuthorityUploadLog", authority.Key, batchUploadDate, log.ToString());
                // the following is happening in the calling function so not gonna capture the data here
                //if (upload != null)
                //    await SaveUploadLog("AuthorityUpload", authority.Key, Utilities.GetCurrentCSTDate(), String.Join('\n', upload));

                //_memoryCache.Remove(CACHE_KEY);  // set by the caller
                if (_errorContext.ChangeTracker.HasChanges())
                    await _errorContext.SaveChangesAsync();
            }
        }

        public async void RecalculateAuthorityDates(DbContextOptions<ArbitrationDbContext> contextOptions, int jobId, AppUser user, Authority authority, bool activeOnly = true)
        {
            //if (authority.TrackingDetails.Count == 0)
            //    throw new Exception("Cannot recalculate dates for a non-bifurcated Authority or one without Authority Tracking configurations.");

            string message = $@"Recalculating {authority.Name} dates and deadlines...";
            var job = await _errorContext.JobQueueItems.FindAsync(jobId);

            int lastId = 1;
            bool isNSA = false;
            int totalRecs = 0;
            Authority? nsa = null;

            IQueryable<ArbitrationCase>? ArbitrationCaseQuery = null;

            if (authority.Key.ToLower() == "nsa")
            {
                isNSA = true;
                nsa = authority;
                ArbitrationCaseQuery = _context.ArbitrationCases.Where(d => !d.IsDeleted &&
                                                         d.EOBDate != null &&
                                                         (d.NSAStatus == "Pending NSA Negotiation Request" || d.NSAStatus == "Submitted NSA Negotiation Request" || !activeOnly));
                totalRecs = await ArbitrationCaseQuery.CountAsync();
            }
            else
            {
                ArbitrationCaseQuery = _context.ArbitrationCases.Include(j => j.Tracking).Where(d => !d.IsDeleted &&
                                                        d.Authority == authority.Key &&
                                                        (!activeOnly ||
                                                         d.Status == ArbitrationStatus.ActiveArbitrationBriefCreated ||
                                                         d.Status == ArbitrationStatus.ActiveArbitrationBriefNeeded ||
                                                         d.Status == ArbitrationStatus.ActiveArbitrationBriefSubmitted ||
                                                         d.Status == ArbitrationStatus.DetermineAuthority ||
                                                         d.Status == ArbitrationStatus.InformalInProgress ||
                                                         d.Status == ArbitrationStatus.MissingInformation ||
                                                         d.Status == ArbitrationStatus.New ||
                                                         d.Status == ArbitrationStatus.Open));

                totalRecs = await _context.ArbitrationCases.Where(d => !d.IsDeleted &&
                                                        d.Authority == authority.Key &&
                                                        (!activeOnly ||
                                                         d.Status == ArbitrationStatus.ActiveArbitrationBriefCreated ||
                                                         d.Status == ArbitrationStatus.ActiveArbitrationBriefNeeded ||
                                                         d.Status == ArbitrationStatus.ActiveArbitrationBriefSubmitted ||
                                                         d.Status == ArbitrationStatus.DetermineAuthority ||
                                                         d.Status == ArbitrationStatus.InformalInProgress ||
                                                         d.Status == ArbitrationStatus.MissingInformation ||
                                                         d.Status == ArbitrationStatus.New ||
                                                         d.Status == ArbitrationStatus.Open)).CountAsync();
            }
            _context.Database.SetCommandTimeout(TimeSpan.FromSeconds(180));
            var recs = await ArbitrationCaseQuery.OrderBy(d => d.Id).Take(500).ToArrayAsync();
            bool noRecords = recs.Count() == 0;

            int recsProcessed = 0;
            string lastError = "";

            if (noRecords)
                lastError = "No Claims matched the criteria for recalculating";

            await ImportUtils.UpdateJob(_errorContext, job, message, "loading", 0, totalRecs);
            await Extensions.EnsureHolidays(_context);

            try
            {
                while (!noRecords)
                {
                    foreach (var target in recs)
                    {
                        recsProcessed++;

                        bool isChanged = Utilities.FixStateArbitrationCaseDates(target);
                        JsonNode? trackingNode = null;
                        // NOTE: We cannot process both NSA and Local authority tracking objects at the same time b/c we would need the NSA authority obj passed in as a resource
                        // If this function somehow needs to be used regularly we might enhance this function to do both at the same time.
                        if (isNSA)
                            trackingNode = JsonNode.Parse(target.NSATracking);
                        else if (target.Tracking != null)
                            trackingNode = JsonNode.Parse(target.Tracking.TrackingValues);

                        if (trackingNode != null && authority.TrackingDetails.Count > 0)
                            isChanged = isChanged || Utilities.ValidateTracking(target, nsa, authority); // true if recalculation resulted in changes 

                        if (isChanged)
                        {
                            target.UpdatedBy = user.Email;
                            target.UpdatedOn = Utilities.GetCurrentUtcDate();
                            await _context.SaveChangesAsync();
                        }

                        // update real-time status record
                        if (job != null && recsProcessed == 1 || recsProcessed % 100 == 0 || recsProcessed == totalRecs)
                        {
                            await ImportUtils.UpdateJob(_errorContext, job, message, "calculating", recsProcessed, totalRecs, lastError);
                        }
                    }

                    lastId = recs.Max(d => d.Id);
                    recs = ArbitrationCaseQuery.Where(d => d.Id > lastId).OrderBy(d => d.Id).Take(500).ToArray();
                    noRecords = recs.Count() == 0;
                }
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                lastError = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
            }

            try
            {
                await ImportUtils.UpdateJob(_errorContext, job, $@"Recalculation of ArbitrationCase records for {authority.Key} complete", "finished", recsProcessed, totalRecs, lastError);
            }
            catch (Exception ex2)
            {
                Console.WriteLine(ex2.Message);
            }
        }

        private async Task<string> SaveClaimBLOB(MemoryStream stream, ArbitrationCase arbCase, CaseDocumentType cdt, string filename, string uploadedBy)
        {

            string blobName = $@"{arbCase.Id}-{cdt.ToString().ToLower()}-{filename.ToLower()}";
            string conn = _configuration.GetSection("Storage").GetSection("Connection").Value;
            string name = _configuration.GetSection("Storage").GetSection("Container").Value;
            var _containerClient = new BlobContainerClient(conn, name);

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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="docType"></param>
        /// <param name="authority"></param>
        /// <param name="uploadDate"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public async Task<string> SaveUploadLog(string docType, string authority, DateTime uploadDate, string log)
        {
            string conn = _configuration.GetSection("Storage").GetSection("Connection").Value;
            string name = _configuration.GetSection("Storage").GetSection("Container").Value;
            var _containerClient = new BlobContainerClient(conn, name);

            var uploadedOn = uploadDate;
            var uploadedBy = _principal.Identity?.Name ?? "anonymous";

            try
            {
                string blobName = $@"ImportLog-{docType}-{uploadDate.ToString("O")}.log";

                using (var reader = new MemoryStream(Encoding.UTF8.GetBytes(log)))
                {
                    try
                    {
                        BlobClient blob = _containerClient.GetBlobClient(blobName);

                        _logger.LogInformation($@"Attempting to upload file {blobName} to BLOB store...");
                        var response = await blob.UploadAsync(reader, true);
                        if (response.GetRawResponse().ReasonPhrase != "Created")
                            throw new Exception("Unexpected result from BLOB upload");

                        // add tags to new BLOB
                        var tags = new Dictionary<string, string>();
                        tags.Add("Authority", authority);
                        tags.Add("UploadedBy", uploadedBy.Split('@')[0].Replace("'", "-")); // some user's name may have an apostrophe due to bad IT policy (gMail does not allow you to create a new account with an apostrophe)
                        tags.Add("BatchUploadDate", string.Format("{0:u}", uploadDate));
                        tags.Add("DocumentType", docType);
                        await blob.SetTagsAsync(tags);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Unable to write to BLOB storage, " + ex.Message);
                        _logger.LogError(ex.Message);
                        return ex.Message;
                    }
                }
                return _containerClient.Uri.ToString() + "/" + blobName;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in SaveUploadLog");
#if DEBUG
                _logger.LogError(ex.Message);
#endif
                return ex.Message;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TDIRequestDetail"></param>
        /// <param name="arb"></param>
        /// <returns></returns>
        public async Task<string> SyncArbitrators(TDIRequestDetails TDIRequestDetail, ArbitrationCase arb)
        {
            // parse the arbitrators on the TDI case
            var tdiArbsList = new List<Arbitrator>();
            var allArbs = $@"{TDIRequestDetail.Arbitrator1}|{TDIRequestDetail.Arbitrator2}|{TDIRequestDetail.Arbitrator3}|{TDIRequestDetail.Arbitrator4}|{TDIRequestDetail.Arbitrator5}";
            int assignedCaseArbId = 0;
            string message = "";
            List<Arbitrator> assignedArbs = new List<Arbitrator>();

            // sync the list of arbitrators provided by the TDI record with what's assigned to this case
            foreach (var arbitrator in allArbs.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    DateTime AssignedDate = DateTime.MinValue;
                    var m = Arbitrator.Map(arbitrator, out AssignedDate);
                    if (m == null)
                        continue;
                    // search for existing arbitrator with matching email address
                    var a = await _context.Arbitrators.FirstOrDefaultAsync(d => d.Email == m.Email);

                    if (a == null)
                    {
                        _logger.LogInformation("Creating new Arbitrator " + m.Email);
                        // create a new Arbitrator record
                        a = new Arbitrator
                        {
                            Email = m.Email,
                            Name = m.Name,
                            Phone = m.Phone,
                            FixedFee = 0,
                            IsActive = true,
                            MediatorFixedFee = 0,
                            Notes = "Added automatically by TDI Synchronization process",
                            UpdatedBy = "system",
                            UpdatedOn = Utilities.GetCurrentUtcDate()
                        };
                        _context.Arbitrators.Add(a);
                        await _context.SaveChangesAsync();
                    }

                    assignedArbs.Add(a);

                    // We now have primary Arbitrator record. Did name or phone change? If so, update...
                    if (!a.Name.Equals(m.Name))
                    {
                        _logger.LogInformation($@"Updating name of Arbitrator {a.Id}");
                        a.Name = m.Name;
                    }
                    if (!a.Phone.Equals(m.Phone))
                    {
                        _logger.LogInformation($@"Updating phone of Arbitrator {a.Id}");
                        a.Phone = m.Phone;
                    }

                    // Is the Arbitrator assigned to the case yet?
                    var ca = arb.Arbitrators.Where(d => d.ArbitratorId == a.Id).FirstOrDefault();
                    if (ca == null)
                    {
                        _logger.LogInformation($@"Adding Arbitrator {a.Id} to ArbitrationCase {arb.Id}");
                        // No, so add the arbitrator to the case
                        ca = new CaseArbitrator
                        {
                            ArbitrationCaseId = arb.Id,
                            ArbitratorId = a.Id,
                            Fee = a.FixedFee, // TODO: Is this the right number?
                            IsActive = true,  // TODO: if one of the other tdiArbitrators has an assigned date, this will have to get set to false (down below)
                            IsDismissed = false, // TODO: same as IsActive but will mostly be used by our UI
                            UpdatedBy = "system",
                            UpdatedOn = Utilities.GetCurrentUtcDate()
                        };
                        arb.Arbitrators.Add(ca);
                    }

                    if (_context.ChangeTracker.HasChanges())
                        await _context.SaveChangesAsync();

                    if (AssignedDate != DateTime.MinValue)
                    {
                        assignedCaseArbId = ca.Id;
                        if (ca.AssignedOn != AssignedDate)
                        {
                            _logger.LogInformation($@"Arbitrator assignment detected. Arbitrator {a.Id} assigned to ArbitrationCase {arb.Id} on {AssignedDate.ToShortDateString()}");
                            ca.AssignedOn = AssignedDate;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }

            // detect and remove any arbitrators no long associated with this case
            List<int> removals = new List<int>();
            foreach (var a in arb.Arbitrators)
            {
                var f = assignedArbs.FirstOrDefault(d => d.Id == a.ArbitratorId);
                if (f == null)
                {
                    removals.Add(a.Id);
                    _logger.LogInformation($@"Arbitrator {a.ArbitratorId} no longer on Case {arb.Id}. Removed.");
                }
            }

            if (removals.Count > 0)
            {
                arb.Arbitrators.RemoveAll(j => removals.Contains(j.Id));
                await _context.SaveChangesAsync();
            }

            if (assignedCaseArbId > 0)
            {
                // mark all CaseArbitrators other than this one as Inactive
                var inactive = arb.Arbitrators.Where(d => d.Id != assignedCaseArbId && d.IsActive == true).ToList();
                if (inactive.Count() > 0)
                {
                    foreach (var c in inactive)
                    {
                        c.IsActive = false;
                        c.UpdatedBy = "system";
                        c.UpdatedOn = Utilities.GetCurrentUtcDate();
                    }

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        message += ex.Message;
                    }
                }
            }

            /* This shouldn't be necessary since the list of active arbitrators can easily reveal, in real time, any active, bad arbitrators
            // see if any of the active arbitrators are on the naughty list
            var n1 = from q in _context.Set<CaseArbitrator>()
                     from r in _context.Set<Arbitrator>()
                     where q.ArbitrationCaseId == arb.Id &&
                     q.ArbitratorId == r.Id &&
                     q.IsActive == true && r.IsLastResort == true
                     select q;

            var naughty = await n1.CountAsync();
            var hasNaughty = (naughty > 0);
            if (hasNaughty != arb.HasArbitratorWarning)
            {
                arb.HasArbitratorWarning = hasNaughty;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    message += ex.Message;
                }
            }
            */
            return message;
        }
        /// <summary>
        /// This method should eventually be replaced by SyncAuthorityImportToCases which will be flexible enough to handle all authority configurations.
        /// </summary>
        /// <param name="authorityId"></param>
        /// <param name="TDIRecords"></param>
        /// <param name="job"></param>
        public async void SyncTDIsToCases(int authorityId, List<TDIRequestDetails> TDIRecords, JobQueueItem? job = null)

        {
            // fetch the TDI info, if available, and initialize an ArbitrationCase object the user can start with
            _logger.LogInformation($@"Beginning SyncTDIsToCases");

            if (job != null)
                _errorContext.Entry(job).State = EntityState.Unchanged;

            string msg = "";
            var log = new StringBuilder();

            // Set up event handler to catch any failures during db processing
            _context.SaveChangesFailed += (object? sender, SaveChangesFailedEventArgs e) =>
            {
                var m = e.Exception.InnerException != null ? e.Exception.InnerException.Message : e.Exception.Message;
                _logger.Log(LogLevel.Error, m);
                log.AppendLine(m);
            };

            // Local vars
            Authority authority;
            Authority nsa;
            var runAs = new AppUser { Email = "system", Id = -1, IsActive = true, JSON = "{}" };

            try
            {
                if (TDIRecords.Count == 0)
                {
                    await ImportUtils.UpdateJob(_errorContext, job, "No TDIRequest objects found in the upload collection!", "error");
                    _logger.LogWarning("SyncTDIsToCases: No TDI Request data available!");
                    return;
                }

                await ImportUtils.UpdateJob(_errorContext, job, "Saving TDIRequest objects to staging table...", "processing");

                // Copy all the records to the DB
                // TODO: This could be eliminated if we have no need of keeping the raw import data around...
                _context.TDIRequests.AddRange(TDIRecords);

                // Save the staging recs in one batch - could take a while!
                await _context.SaveChangesAsync();

                await EnsureAuthorities();
                await EnsureCustomers();
                await EnsurePayors();

                if (AUTO_ADD_PAYORS)
                {
                    var ImportPayorNames = TDIRecords.Select(d => d.HealthPlanName).Distinct().ToArray();
                    foreach (var payerName in ImportPayorNames)
                    {
                        await CreatePayorIfNeededAsync(_errorContext, payerName);
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                await ImportUtils.UpdateJob(_errorContext, job, errorMessage, "error");
                _logger.LogWarning("SyncTDIsToCases: " + errorMessage);
                return;
            }

            // Now Sync the records to ArbitrationCases
            var batchUploadDate = TDIRecords.First().BatchUploadDate;


            authority = _context.Authorities.FirstOrDefault(d => d.Id == authorityId)!;
            if (authority == null)
            {
                msg = "NSA Authority configuration is unavailable. Unable to process the TDI data.";
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.MDMissingAuthority, "NSA", msg, _errorContext);
                await ImportUtils.UpdateJob(_errorContext, job, "Application configuration failure (NSA)", "finished");
                _logger.LogError(msg + " (Exception logged)");
                return;
            }

            nsa = _context.Authorities.FirstOrDefault(d => d.Key.Equals("nsa"))!;
            if (nsa == null)
            {
                msg = "NSA Authority configuration is unavailable. Unable to process the TDI data.";
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.MDMissingAuthority, "NSA", msg, _errorContext);
                await ImportUtils.UpdateJob(_errorContext, job, "Application configuration failure (NSA)", "finished");
                _logger.LogError(msg + " (Exception logged)");
                return;
            }

            log.AppendLine($@"Beginning SyncTDIsToCases for Authority {authority.Name} at {Utilities.GetCurrentCSTDate2().ToString("MM/dd/yyyy hh:mm tt")} CST"); // redundant since this is only for TDI but may be useful later

            var accountMaps = authority.AuthorityJson?.CustomerMappings;
            if (accountMaps == null || accountMaps.Count == 0)
            {
                msg = "TX Authority does not have the TDI User Ids mapped to the Customers!";
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.SyncTargetPropertyNotFound, "TX", msg, _errorContext);
                await ImportUtils.UpdateJob(_errorContext, job, "Application configuration failure (TX)", "finished");
                _logger.LogError(msg + " (Exception logged)");
                return;
            }

            await Extensions.EnsureHolidays(_context);

            string authorityKey = authority.Key;
            int recordsAdded = 0;
            int recordsError = 0;
            int recordsProcessed = 0;
            int recordsUpdated = 0;
            int recordsSkipped = 0;
            //var Q = _context.TDIRequests.Where(x => x.BatchUploadDate == batchUploadDate && x.RequestId != null);
            //var TDIs = await Q.ToListAsync();

            var ImportFieldConfigList = await _context.ImportFieldConfigs.Where(d => d.Source == "TDICaseSync").ToListAsync();

            bool changesMade = false;

            foreach (TDIRequestDetails? TDIRequestDetail in TDIRecords)
            {
                recordsProcessed++;

                bool canSync = false;
                changesMade = false;

                _logger.LogInformation($@"Processing case {TDIRequestDetail.RequestId}");
                log.AppendLine($@"Processing case {TDIRequestDetail.RequestId}");

                string findMsg = $@"Request ID {TDIRequestDetail.RequestId}";
                var arbitCaseQuery = _context.ArbitrationCases.Include(d => d.CPTCodes)
                            .Include(d => d.Arbitrators)
                            .Include(d => d.CaseSettlements.Where(g => !g.IsDeleted))
                            .ThenInclude(h => h.CaseSettlementDetails.Where(j => !j.IsDeleted)).DefaultIfEmpty()
                            .Include(k => k.CaseSettlements.Where(g => !g.IsDeleted))
                            .ThenInclude(h => h.CaseSettlementCPTs.Where(m => !m.IsDeleted)).DefaultIfEmpty()
                            .Where(d => !d.IsDeleted &&
                            (d.Authority == "tx" || d.Authority == "") &&
                            d.AuthorityCaseId == TDIRequestDetail.RequestId);
                // load the case and related arbitrators
                var arbitCaseList = await arbitCaseQuery.ToArrayAsync();

                if (arbitCaseList.Count() > 1)
                {
                    var errMsg = $@"Too many records found ({arbitCaseList.Count()}) for AuthorityCaseId {TDIRequestDetail.RequestId}.";
                    await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.DuplicateAuthorityCaseId, TDIRequestDetail.RequestId, errMsg, _errorContext);
                    log.AppendLine(errMsg + " Skipping update. (Exception logged)");
                    recordsSkipped++;
                    goto EndOfLoop;
                }

                var arbitCase = arbitCaseList.FirstOrDefault();


                // This next section tries an alternate method of finding the claim we want to update.
                // The code will fall into this block if the ArbitrationCase record does not have the targeted TDI case number on it.
                if (arbitCase == null)
                {
                    // make sure this TDI record is complete - TODO: Is the import validation making sure of this already???
                    if (!TDIRequestDetail.ServiceDate.HasValue || string.IsNullOrEmpty(TDIRequestDetail.PatientName) || string.IsNullOrEmpty(TDIRequestDetail.ProviderNPI))
                    {
                        log.AppendLine("The TDI Import record was missing a key lookup value. Skipping update.");
                        recordsSkipped++;
                        goto EndOfLoop;
                    }

                    var customer = accountMaps.FirstOrDefault(d => d.UserId == TDIRequestDetail.UserId)?.CustomerName;
                    if (customer == null)
                        customer = "";

                    var ClaimLocator = new ClaimLocator { Customer = customer, ProviderNPI = TDIRequestDetail.ProviderNPI, PatientName = TDIRequestDetail.PatientName, PayorClaimNumber = TDIRequestDetail.PayorClaimNumber, ServiceDate = TDIRequestDetail.ServiceDate };
                    string message = "";
                    FindCaseResult? findResult = null;

                    try
                    {
                        findResult = await FindArbitrationCase(ClaimLocator, true, false, true);
                        message = findResult.Message;
                        arbitCase = findResult.Record;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }


                    if (!string.IsNullOrEmpty(message))
                    {
                        log.AppendLine($@"{message}. Update skipped.");
                        recordsSkipped++;
                        goto EndOfLoop;
                    }

                    if (arbitCase != null)
                    {
                        findMsg = $@"{TDIRequestDetail.PatientName} | {TDIRequestDetail.ServiceDate.Value.ToShortDateString()} | {TDIRequestDetail.ProviderNPI} "; // $@"Payor {TDIRequestDetail.HealthPlanName} and PayorClaimNumber {TDIRequestDetail.PayorClaimNumber}";

                        // Check need for a case archive before continuing
                        // NOTE: Finding a claim without using the Authority ID (TDI number) means it could have a different TDI number than
                        // what we are trying to update. Therefore, we check the found record for archive eligibility.

                        // Create a temp object used for testing whether to archive what is currently on the ArbitrationCase record
                        var arbitCaseTemp = new AuthorityCase
                        {
                            Authority = "TX",
                            AuthorityCaseId = TDIRequestDetail.RequestId,
                            AuthorityStatus = TDIRequestDetail.Status,
                            IneligibilityReasons = TDIRequestDetail.IneligibilityReason
                        };

                        var archiveResult = await ArchiveIfNecessaryAsync(arbitCaseTemp, arbitCase, runAs); // , authority

                        if (archiveResult.ArchiveError || archiveResult.IsAlreadyArchived)
                        {
                            log.AppendLine(archiveResult.Message);  // TODO: Should previously-archived cases still be processed for Settlement info? In theory, only cases that never settle / never could settle should be in the archive.
                            recordsSkipped++;
                            goto EndOfLoop;
                        }
                        else if (archiveResult.ArchiveNeeded)
                        {
                            // TODO: Create change log showing archival
                            changesMade = true;
                            log.AppendLine(archiveResult.Message);
                        }
                        else if (!string.IsNullOrEmpty(archiveResult.Message))
                        {
                            log.AppendLine(archiveResult.Message);
                            if (archiveResult.WasNewRecordModified)
                            {
                                TDIRequestDetail.RequestId = arbitCaseTemp.AuthorityCaseId;
                                TDIRequestDetail.Status = arbitCaseTemp.AuthorityStatus;
                                //TDIRequestDetail.Authority = tdiCase.Authority; // not available in this method since it is always TX...but will need this when implementing the generic authoritySync method
                            }
                        }
                    }
                }

                /*** TODO: Make this more flexible: Convert the Unspecified Dates to the Authority's TimeZone ***/
                Utilities.FixRawTDIDates(TDIRequestDetail);

                // Create a new claim (ArbitrationCase) if neither search method up above found a matching claim
                if (arbitCase == null)
                {
                    msg = $@"ArbitrationCase record not found. Adding new case for TDI Request Id {TDIRequestDetail.RequestId}.";
                    _logger.LogInformation(msg);
                    log.AppendLine(msg);

                    arbitCase = new ArbitrationCase();
                    arbitCase.Authority = authorityKey;
                    arbitCase.IsUnread = true;

                    var syncResult = await SyncObjectData(TDIRequestDetail, arbitCase, ImportFieldConfigList);

                    try
                    {
                        var changeLog = new CaseLog()
                        {
                            Action = "AuthorityUpdate",
                            CreatedBy = runAs.Email,
                            CreatedOn = batchUploadDate,
                            Details = "Created during TDI Import",
                            Id = 0
                        };

                        // createArbitrationCase will add the ArbitrationCase AND the CaseLog items to the EF sets if there are no weird errors
                        msg = await CreateArbitrationCase(arbitCase, true, runAs, batchUploadDate, nsa, changeLog, authority); // set some tracking defaults

                        if (!string.IsNullOrEmpty(msg))
                        {
                            arbitCase = null;
                            _logger.LogInformation(msg);
                            log.AppendLine(msg + " (Update skipped)");
                            recordsSkipped++;
                            goto EndOfLoop;
                        }

                        // TX does not use custom tracking because all Texas dates are considered "baseline" dates (for our internal processes) and
                        // the dedicated fields on the ArbitrationCase class are there to normalize reporting. "Tracking" dates for other Authorities are
                        // actually mapped back to these normalized date fields to create a simple way of standardizing searches
                        // across Authorities. (The only "Authority" where this will not work is NSA since its work flow
                        // runs parallel to the State authority's demands (Brief Due Date, filing deadlines, etc).)
                        // If more of the process were understood prior to developing the application, a more robust
                        // multi-authority system would have been implemented that separated the concerns of deadline tracking
                        // from the Claim information itself. 

                        ProcessAuthorityChanges(arbitCase, string.Empty, false, _logger, authority.AuthorityJson?.StatusMappings, log);
                        await ProcessCaseSettlementAsync(arbitCase, TDIRequestDetail, ImportFieldConfigList, authority, TDIRequestDetail.RequestId, runAs, batchUploadDate, true); // could throw an Exception but will log it first
                        await _context.SaveChangesAsync();

                        canSync = true;
                        recordsAdded++;
                    }
                    catch (Exception ex)
                    {
                        recordsError++;
#pragma warning disable CS8604 // Possible null reference argument. NOTE: What about new subordinate objects? Are they removed, too?
                        _context.ArbitrationCases.Remove(arbitCase); // remove this new object so it doesn't keep causing save problems
                        _context.ChangeTracker.Clear(); // undo uncommitted changes so we can keep going
#pragma warning restore CS8604 // Possible null reference argument.
                        msg = $@"Unexpected create error during SyncTDIsToCases: " + ex.Message;

                        _logger.LogInformation(msg);
                        log.AppendLine(msg);
                    }
                }
                else
                {
                    // Existing claim record found so we sync that with the inbound TDI info
                    msg = $@"ArbitrationCase record found using {findMsg}. Synchronizing records.";

                    arbitCase.Authority = authorityKey;  // ensure records has a value for this field due to older data inconsistencies
                    var previousStatus = arbitCase.AuthorityStatus;
                    bool isNSA = authorityKey.Equals("nsa", StringComparison.CurrentCultureIgnoreCase);
                    _logger.LogInformation($@"Processing ArbitrationCase {arbitCase.Id}, RequestId {TDIRequestDetail.RequestId}");
                    log.AppendLine($@"Processing ArbitrationCase {arbitCase.Id}, RequestId {TDIRequestDetail.RequestId}");
                    try
                    {
                        SyncObjectDataResult syncResult1 = await SyncObjectData(TDIRequestDetail, arbitCase, ImportFieldConfigList);
                        changesMade = changesMade | syncResult1.WereChangesMade;

                        var updatedCustomer = await InitCaseOwnerAsync(authority, arbitCase);
                        changesMade = changesMade | updatedCustomer;

                        var syncResult = await ProcessCaseSettlementAsync(arbitCase, TDIRequestDetail, ImportFieldConfigList, authority, TDIRequestDetail.RequestId, runAs, batchUploadDate, true); // could throw an Exception but will log it first
                        changesMade = changesMade | syncResult.WereChangesMade;

                        if (changesMade) // used to preserve existing IsUnread value
                        {
                            arbitCase.UpdatedBy = runAs.Email;
                            arbitCase.UpdatedOn = batchUploadDate;
                            var JSONNow = JsonSerializer.Serialize(batchUploadDate);
                            var updatedJSON = $@"{{""updatedBy"":""{runAs.Email}"",""updatedOn"":{JSONNow}}}";
                            syncResult1.ChangesJSON = CombineJSON(syncResult1.ChangesJSON, updatedJSON);

                            arbitCase.IsUnread = true;
                            ProcessAuthorityChanges(arbitCase, previousStatus, isNSA, _logger, authority.AuthorityJson?.StatusMappings, log);
                            _context.CaseLog.Add(new CaseLog { Action = "AuthorityUpdate", ArbitrationCaseId = arbitCase.Id, CreatedBy = runAs.Email, CreatedOn = Utilities.GetCurrentUtcDate(), Details = syncResult1.ChangesJSON, Id = 0 });
                            // add separate log entry if Customer was auto-assigned
                            if (updatedCustomer)
                            {
                                _context.CaseLog.Add(new CaseLog()
                                {
                                    Action = "EHRImport",
                                    ArbitrationCaseId = arbitCase.Id,
                                    CreatedBy = runAs.Email,
                                    CreatedOn = batchUploadDate,
                                    Details = $@"{{""customer"":""{arbitCase.Customer}""}}",
                                    Id = 0
                                });
                            }

                            var settlement = arbitCase.CaseSettlements.FirstOrDefault(d => !d.IsDeleted && d.Id == 0);
                            /*
                            if (settlement!.Id == 0)
                                arbitCase.CaseSettlements.Add(settlement);
                            */

                            await _context.SaveChangesAsync();

                            canSync = true;
                            recordsUpdated++;
                            msg += " (Changes found.)";
                        }
                        else
                        {
                            msg += " (No changes detected.)";
                        }

                        _logger.LogInformation(msg);
                        log.AppendLine(msg);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            // undo changes
                            /* granular but prob overkill
                            await _context.Entry<ArbitrationCase>(arbitCase).ReloadAsync(); 
                            int i = arb.SettlementDetails.Count() - 1;
                            while (i >= 0)
                            {
                                var settlementDetails = arb.SettlementDetails[i];
                                if (_context.Entry(settlementDetails).State == EntityState.Added)
                                    arb.SettlementDetails.Remove(settlementDetails);
                                else
                                    await _context.Entry(settlementDetails).ReloadAsync();
                                i--;
                            }
                            */
                            _context.ChangeTracker.Clear(); // undo uncommitted changes so we can keep going
                        }
                        catch
                        {
                            Console.WriteLine(ex.Message);
                        }
                        recordsError++;
                        msg = $@"Unexpected update error during SaveChangesAsync: " + ex.Message;
                        _logger.LogInformation(msg);
                        log.AppendLine(msg);
                    }
                }

                if (canSync)
                {
                    var result = await SyncArbitrators(TDIRequestDetail, arbitCase);

                    if (!string.IsNullOrEmpty(result))
                    {
                        var syncError = $@"ERROR SyncArbitrators(TDI:{TDIRequestDetail.Id}, case:{arbitCase.Id}): {result}";
#if DEBUG
                        _logger.LogError(syncError);
#endif
                        log.AppendLine(syncError);
                    }
                }

            EndOfLoop:  // instead of using Continue, which bypasses the real-time status update, use a simple GoTo label instead

                if (job != null && recordsProcessed % 25 == 0 || recordsProcessed == TDIRecords.Count())
                {
                    string dots = new string('.', Convert.ToInt32(recordsProcessed / 200));
                    await ImportUtils.UpdateJob(_errorContext, job, "Synchronizing TDI records" + dots, "synchronizing", recordsAdded, recordsError, recordsProcessed, recordsSkipped, TDIRecords.Count(), recordsUpdated);
                }
            }

            msg = $@"Synchronization complete. Added {recordsAdded} new cases. Updated {recordsUpdated} cases. Skipped {recordsSkipped} cases.";
            _logger.LogInformation(msg);
            log.AppendLine(msg);
            msg = $@"Finished  {Utilities.GetCurrentCSTDate2().ToString("MM/dd/yyyy hh:mm tt")} CST";
            _logger.LogInformation(msg);
            log.AppendLine(msg);

            await ImportUtils.UpdateJob(_errorContext, job, "Uploading job log", "finalizing");

            await SaveUploadLog("SyncTDIsToCases", "system", batchUploadDate, log.ToString());
            if (_errorContext.ChangeTracker.HasChanges())
                await _errorContext.SaveChangesAsync();

            await ImportUtils.UpdateJob(_errorContext, job, "Job complete.", "finished");
        }

        private async Task<SyncObjectDataResult> ProcessCaseSettlementAsync(ArbitrationCase ArbitCase, object SourceData, IEnumerable<ImportFieldConfig> ImportConfigs, Authority AuthorityObj, string AuthorityCaseId, AppUser RunningAs, DateTime BatchUploadDate, bool AddAllCPTs)
        {
            var syncResult = new SyncObjectDataResult();

            var caseSettlement = GetOrCreateCaseSettlement(ArbitCase, AuthorityObj, AuthorityCaseId, RunningAs, AddAllCPTs);
            if (caseSettlement != null)
            {
                var settlementDetails = caseSettlement.CaseSettlementDetails.FirstOrDefault();
                if (settlementDetails == null)
                {
                    settlementDetails = new CaseSettlementDetail { ArbitrationCaseId = ArbitCase.Id, AuthorityId = AuthorityObj.Id, CreatedBy = RunningAs.Email, CreatedOn = BatchUploadDate };

                }
                syncResult = await SyncObjectData(SourceData, settlementDetails, ImportConfigs); // sync the matching settlement fields in the authority (TDI) record with the settlement object

                if (syncResult.WereChangesMade)
                {
                    if (settlementDetails.Id == 0)
                    {
                        settlementDetails.ArbitrationCaseId = caseSettlement.ArbitrationCaseId.GetValueOrDefault();
                        settlementDetails.CaseSettlementId = caseSettlement.Id;
                        caseSettlement.CaseSettlementDetails.Add(settlementDetails);
                    }
                    caseSettlement.ArbitrationCaseId = ArbitCase.Id;
                    await SyncObjectData(SourceData, caseSettlement, ImportConfigs);
                    var cMsg = Utilities.CalculateAuthoritySettlement(AuthorityObj, caseSettlement, ArbitCase);
                    if (caseSettlement.Id != 0)
                    {
                        _context.CaseSettlements.Update(caseSettlement);

                    }

                    if (!string.IsNullOrEmpty(cMsg))
                    {
                        await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.CaseSettlementError, AuthorityCaseId, cMsg, _errorContext);
                        throw new Exception($@"AuthorityCaseId {AuthorityCaseId}: " + cMsg + " Update skipped.");
                    }
                }
            }
            return syncResult;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="caseRecord"></param>
        /// <param name="skipDOBCheck"></param>
        /// <param name="nsaAuthority"></param>
        /// <param name="stateAuthority"></param>
        /// <param name="isUpdate"></param>
        /// <param name="caller"></param>
        /// <param name="calledByImport"></param>
        /// <returns></returns>
        public async Task<string> ValidateArbitrationCase(ArbitrationCase caseRecord, bool skipDOBCheck, Authority nsaAuthority, Authority? stateAuthority, bool isUpdate, AppUser? caller = null, bool calledByImport = true)
        {
            if (!isUpdate && caseRecord.Id != 0)
                return "createArbitrationCase(): Unexpected! CaseRecord ID is not zero!";

            if (!isUpdate && caseRecord.IsDeleted)
                return "Cannot create a deleted case!";

            if (string.IsNullOrEmpty(caseRecord.Customer))
                return "Customer is a required value!";

            if (!skipDOBCheck && !caseRecord.DOB.HasValue)
                return "DOB is a required value!";

            if (string.IsNullOrEmpty(caseRecord.ProviderNPI))
                return "ProviderNPI is a required value!";

            var customer = Customers.Count() > 0 ? Customers.FirstOrDefault(d => d.Name.Equals(caseRecord.Customer, StringComparison.CurrentCultureIgnoreCase)) : await _context.Customers.Include(d => d.Entities).FirstOrDefaultAsync(d => d.Name == caseRecord.Customer);

            if (customer == null)
                return "Invalid Customer value";

            if (!string.IsNullOrEmpty(caseRecord.Authority) && stateAuthority == null)
                return $@"Invalid Authority code '{caseRecord.Authority}'";

            if (caseRecord.NSARequestDiscount > .99 || caseRecord.NSARequestDiscount < 0)
                return $@"Invalid NSARequest Discount: {caseRecord.NSARequestDiscount}";

            // Authority "nsa" is handled via dedicated columns
            if (caseRecord.Authority.Equals("nsa", StringComparison.CurrentCultureIgnoreCase))
                return "Invalid Authority code 'NSA'. Use the NSA dedicated columns instead.";

            // check that Authority status exists only if an Authority was designated on the record
            if (stateAuthority != null)
            {
                if (!stateAuthority.Key.Equals(caseRecord.Authority, StringComparison.CurrentCultureIgnoreCase))
                    return "Authority object mismatch with provided Authority value!"; // this shouldn't be possible but just in case

                // if providing an Authority, a valid status value is required
                var statuses = stateAuthority.StatusValues.Split(new char[] { ';', ',' }).Select(s => s.Trim().ToLower()).ToArray();
                if (!string.IsNullOrEmpty(caseRecord.AuthorityStatus) && !statuses.Contains(caseRecord.AuthorityStatus.ToLower()))
                    return $@"Invalid AuthorityStatus ('{caseRecord.AuthorityStatus}')";

                // "Not Submitted" is a magic string - it should be present as a valid choice on all Authority records
                //if ((caller == null || caller.IsState) && !string.IsNullOrEmpty(au.Website) && string.IsNullOrEmpty(caseRecord.AuthorityCaseId) && caseRecord.AuthorityStatus != "Not Submitted")
                //    return $@"AuthorityStatus must be 'Not Submitted' when there is no AuthorityCaseId value and the Authority provides a web portal.";

                if (!string.IsNullOrEmpty(caseRecord.AuthorityCaseId) && caseRecord.AuthorityStatus == "Not Submitted")
                    return $@"AuthorityStatus cannot be 'Not Submitted' when the AuthorityCaseId value is provided.";
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
                    await _context.SaveChangesAsync();
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
                    return $@"Invalid NSAStatus ('{caseRecord.NSAStatus}')";


                //------------------- Payor determination starts here -----------------------------------------------//
                // verify child record values - using the Payor name to supersede the PayorId is consistent with other areas of the application (this is a convention)
                // don't care about log here to just dummy StringBuilder supplied
                var payor = this.FindPayerAsync(caseRecord, new StringBuilder()).Result;

                if (payor == null) // see we could not add new payor
                {
                    return "Unable to determine Payor from supplied information."; // again, garbage data. not going to log this as some sort of master data issue
                                                                                   // verify that the EntityNPI is not on an Exclusion list for this Payor
                }

                caseRecord.PayorId = payor!.Id; // link to payor record
                caseRecord.Payor = payor.Name; // standardize the spelling
                //------------------- Payor determination ends here -----------------------------------------------//

                // check for payor+entity exclusion (can happen if Provider/Entity signs away their right to arbitrate, for instance)
                if (!string.IsNullOrEmpty(caseRecord.EntityNPI))
                {
                    if (calledByImport && payor.GetExcludedEntities().FirstOrDefault(d => d.NPINumber.Equals(caseRecord.EntityNPI, StringComparison.CurrentCultureIgnoreCase)) != null)
                    {
                        var msg = $@"Entity '{caseRecord.Entity}' ({caseRecord.EntityNPI}) is on an exclusion list for Payor ({payor.Name}). Claim not added.";
                        await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.ExcludedEntity, caseRecord.EntityNPI, msg, _errorContext);
                        return msg + " (Exception logged.)";
                    }

                    // verify EntityNPI against Customer Entity list
                    if (customer.Entities.FirstOrDefault(d => d.NPINumber == caseRecord.EntityNPI) == null)
                    {
                        if (AUTO_ADD_ENTITIES)
                        {
                            await CreateEntityIfNeededAsync(_errorContext, customer, caseRecord.Entity, caseRecord.EntityNPI);
                        }
                        else
                        {
                            var msg = $@"EntityNPI {caseRecord.EntityNPI} does not match an Entity for Customer {customer.Name}. Claim not added.";
                            await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.UnknownEntity, caseRecord.EntityNPI, msg, _errorContext);
                            return msg + " (Exception logged.)";
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(caseRecord.Entity))
                {
                    var nt = customer.Entities.FirstOrDefault(d => d.Name.Equals(caseRecord.Entity, StringComparison.CurrentCultureIgnoreCase));
                    if (nt != null)
                    {
                        caseRecord.EntityNPI = nt.NPINumber;
                    }
                    else
                    {
                        var msg = $@"Entity {caseRecord.Entity} does not match an Entity for Customer {customer.Name}. Claim not added.";
                        await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.UnknownEntity, caseRecord.Entity, msg, _errorContext);
                        return msg + " (Exception logged.)";
                    }
                }

                // check for duplicate
                if (!isUpdate)
                {
                    var findResult = await FindArbitrationCase(caseRecord, skipDOBCheck, false);
                    if (findResult.Record != null)
                        return "Cannot create the new ArbitrationCase. One already exists with duplicate key values.";
                    else if (!string.IsNullOrEmpty(findResult.Message))
                        return findResult.Message;
                }
            }
            catch (Exception ex)
            {
                return ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }

            return string.Empty;
        }

        #region Private methods

        private bool AreValuesEqual(PropertyInfo p1, Object a, PropertyInfo p2, Object b)
        {
            if (p1.PropertyType.Name != p2.PropertyType.Name)
            {
                _logger.LogCritical($@"Import properties have mismatched data types: {p1.Name} and {p2.Name}");
                throw new Exception("Field type mismatch!"); // this shouldn't happen if we are properly filtering what is sent to this function
            }
            object? valueA;
            object? valueB;
            bool result;

            IComparable? selfValueComparer;

            valueA = p1.GetValue(a, null);
            valueB = p2.GetValue(b, null);
            selfValueComparer = valueA == null ? null : valueA as IComparable;

            if (valueA == null && valueB != null || valueA != null && valueB == null)
                result = false; // one of the values is null
            else if (selfValueComparer != null && selfValueComparer.CompareTo(valueB) != 0)
                result = false; // the comparison using IComparable failed
            else if (!object.Equals(valueA, valueB))
                result = false; // the comparison using Equals failed
            else
                result = true; // match

            return result;
        }

        private string CombineJSON(string json1, string json2)
        {
            // public static JsonObject Squish(string prefix, JsonNode parent, JsonObject child)
            var obj1 = JsonNode.Parse(json1);
            var obj2 = JsonNode.Parse(json2)?.AsObject();
            if (obj1 == null || obj2 == null)
                return json1;

            return Utilities.Squish("", obj1, obj2).ToJsonString();
        }

        private string DetectDifferencesBetweenObjects(Object OriginalObj, Object importRecord, IEnumerable<string> fieldList)
        {
            // Note: Fields that only appear in a Tracking JSON string cannot be detected for changes. 
            // For instance, in order to detect a change in the NSA's DateOfInitialClaimPayment tracking field, the field that 
            // it is synced to in the ArbitrationCase record must have changed. This could be something like ProviderPaidDate or FirstResponseDate or EOBDate.
            var deep = OriginalObj.DeeplyEquals(importRecord);
            var results = deep.Select(x => new KeyValuePair<string, object?>(x.Path.Split('.').Last(), x.ActualValue)).Where(d => fieldList.Contains(d.Key));  // Id wouldn't match if the importRecord is a new Object 
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


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task EnsureCalculatorVariables()
        {
            if (CalculatorVariables.Count() == 0)
                this.CalculatorVariables = await _context.CalculatorVariables.ToListAsync();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task EnsureCustomers()
        {
            if (Customers.Count() == 0)
                Customers = await _context.Customers.Include(d => d.Entities).Where(x => x.IsActive).ToListAsync();
        }

        /// <summary>
        /// Grabs Payors (without JSON!)
        /// </summary>
        /// <param name="ExcludeJSON"></param>
        /// <returns></returns>
        public async Task EnsurePayors(bool ExcludeJSON = true)
        {
            if (Payors.Count() == 0)
            {
                // Omit templates
                IQueryable<Payor> Q = null!;
                if (ExcludeJSON)
                {
                    Q = _context.Payors.Include(a => a.AuthorityGroupExceptions).Include(b => b.PayorGroups).Include(c => c.Negotiators).AsSplitQuery().AsNoTracking().Select(d => new Payor() { PayorGroups = d.PayorGroups, AuthorityGroupExceptions = d.AuthorityGroupExceptions, Id = d.Id, IsActive = d.IsActive, Name = d.Name, NSARequestEmail = d.NSARequestEmail, ParentId = d.ParentId, SendNSARequests = d.SendNSARequests, UpdatedBy = d.UpdatedBy, UpdatedOn = d.UpdatedOn });
                }
                else
                {
                    Q = _context.Payors.AsSplitQuery().Include(d => d.AuthorityGroupExceptions).Include(d => d.PayorGroups).Include(d => d.Negotiators).AsSplitQuery().AsNoTracking();
                }

                Payors = await Q.ToListAsync();

                _logger.Log(LogLevel.Debug, "Payors including AuthorityGroupExceptions, PayorGroups and Negotiators loaded");

                // Capture one template for use as a default (see CreatePayorIfNecessaryAsync)

                // TODO: Take advantage of ExcludeJSON = false
                var BCBSTXPayor = await _context.Payors.AsNoTracking().FirstOrDefaultAsync(d => d.Name.Equals("BCBSTX"));
                if (BCBSTXPayor != null)
                    DEFAULT_PAYOR_JSON = BCBSTXPayor.JSON;
                else
                    DEFAULT_PAYOR_JSON = "{}";
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task EnsureProcedureCodes()
        {
            if (ProcedureCodes.Count() == 0)
                this.ProcedureCodes = await _context.ProcedureCodes.AsNoTracking().ToListAsync();
        }

        private async Task<string> GetAuthorityTrackingJSON(string key)
        {
            await EnsureAuthorities(); ;
            var a = Authorities.FirstOrDefault(d => d.Key.Equals(key, StringComparison.CurrentCultureIgnoreCase));
            if (a == null)
                return "{}";

            return await GetAuthorityTrackingJSON(a);
        }

        private CaseSettlement? GetOrCreateCaseSettlement(ArbitrationCase ArbitCaseObj, Authority authority, string authorityCaseId, AppUser user, bool addAllCPTs = true)
        {
            if (string.IsNullOrEmpty(ArbitCaseObj.AuthorityCaseId))
                return null; // TODO: Why does it matter if the claim has no current AuthorityCaseId? Are we only accepting settlements on the "current" case id as a business rule of some sort?

            var deletedCaseSettlement = ArbitCaseObj.CaseSettlements.FirstOrDefault(d => d.IsDeleted && d.ArbitrationCaseId == ArbitCaseObj.Id && d.AuthorityId == authority.Id && d.AuthorityCaseId == authorityCaseId);
            if (deletedCaseSettlement != null)
            {
                _context.CaseSettlements.Remove(deletedCaseSettlement);
                _context.SaveChanges();
            }

            var caseSettlement = ArbitCaseObj.CaseSettlements.FirstOrDefault(d => !d.IsDeleted && d.ArbitrationCaseId == ArbitCaseObj.Id && d.AuthorityId == authority.Id && d.AuthorityCaseId == authorityCaseId);
            if (caseSettlement == null)
            {
                if (!ArbitCaseObj.PayorId.HasValue || ArbitCaseObj.PayorId.Value < 1)
                    throw new Exception("Cannot create a CaseSettlement for an ArbitrationCase without a PayorId");
                caseSettlement = new CaseSettlement { Id = 0, AuthorityCaseId = ArbitCaseObj.AuthorityCaseId, AuthorityId = authority.Id, CreatedBy = user.Email, CreatedOn = Utilities.GetCurrentUtcDate(), PayorId = ArbitCaseObj.PayorId.Value };
                ArbitCaseObj.CaseSettlements.Add(caseSettlement);
                _context.SaveChanges();
                if (addAllCPTs)
                {
                    foreach (var cpt in ArbitCaseObj.CPTCodes.Where(d => d.Id > 0 && !d.isDeleted && d.IsIncluded))
                    {
                        caseSettlement.CaseSettlementCPTs.Add(new CaseSettlementCPT { ClaimCPTId = cpt.Id, Id = 0, UpdatedBy = user.Email, UpdatedOn = caseSettlement.CreatedOn });
                    }
                }
                //_context.CaseSettlements.Add(caseSettlement);
                //_context.SaveChanges();
            }

            return caseSettlement;
        }
        /*
        private CaseSettlementDetail? getCaseSettlementDetail(ArbitrationCase original, Authority authority, AppUser user)
        {
            if (string.IsNullOrEmpty(original.AuthorityCaseId))
                return null;

            var s = original.SettlementDetails.FindAll(d => !d.IsDeleted && d.AuthorityCaseId == original.AuthorityCaseId && d.AuthorityId == authority.Id).OrderBy(d => d.Id).LastOrDefault();
            if (s == null)
                s = new CaseSettlementDetail { Id = 0, AuthorityCaseId = original.AuthorityCaseId, AuthorityId = authority.Id, CreatedBy = user.Email, CreatedOn = Utilities.GetCurrentUtcDate() };
                
            return s;
        }
        */

        private async Task<string> GetAuthorityTrackingJSON(Authority authority)
        {
            if (authority == null || authority.Id < 1)
                return "{}";

            if (authority.TrackingDetails == null)
                authority.TrackingDetails = await _context.AuthorityTrackingDetails.Where(d => !d.IsDeleted && d.AuthorityId == authority.Id).ToListAsync();

            if (authority == null || authority.TrackingDetails.Count() == 0)
                return "{}";

            // use the AuthorityTrackingDetails records to make a JSON tracking object the client / SQL reporting can interact with
            var j = String.Join(',', authority.TrackingDetails.Where(d => d.TrackingFieldType == "Date").Select(d => $@"""{d.TrackingFieldName}"":null"));
            var t = new StringBuilder("{");
            if (j.Length > 0)
            {
                t.Append(j);
                //t.Length--;
            }
            t.Append("}");
            return t.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetObject"></param>
        /// <param name="targetProp"></param>
        /// <param name="value"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        private ValueCopyResult CopySourceValueToTargetValue(Object targetObject, PropertyInfo targetProp, string value, StringBuilder log)
        {
            try
            {
                if (targetProp.PropertyType == typeof(string))
                {
                    targetProp.SetValue(targetObject, value.Trim('"'), null);
                }
                else if (targetProp.PropertyType == typeof(bool))
                {
                    value = value.Trim('"');
                    if (value == "-1" || value == "1" || value.Equals("y", StringComparison.CurrentCultureIgnoreCase) || value.Equals("yes", StringComparison.CurrentCultureIgnoreCase) || value.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                        targetProp.SetValue(targetObject, true, null);
                    else
                        targetProp.SetValue(targetObject, false, null);
                }
                else if (targetProp.PropertyType == typeof(DateTime))
                {
                    if (DateTime.TryParse(value.Trim('"'), out var dt))
                        targetProp.SetValue(targetObject, dt, null);
                }
                else if (targetProp.PropertyType == typeof(DateTime?))
                {
                    if (value != null && DateTime.TryParse(value.Trim('"'), out var dt))
                        targetProp.SetValue(targetObject, dt, null);
                    else
                        targetProp.SetValue(targetObject, null, null);
                }
                else if (targetProp.PropertyType == typeof(double) || targetProp.PropertyType == typeof(double?))
                {
                    value = Regex.Replace(value, "[^0-9.-]", "");
                    value = (String.IsNullOrEmpty(value) ? "0.0" : value);
                    targetProp.SetValue(targetObject, Convert.ToDouble(value), null);
                }
                else if (targetProp.PropertyType == typeof(int) || targetProp.PropertyType == typeof(int?))
                {
                    value = Regex.Replace(value, "[^0-9.-]", "");
                    value = (String.IsNullOrEmpty(value) ? "0" : value);
                    targetProp.SetValue(targetObject, Convert.ToInt32(value), null);
                }
                else
                {
                    return ValueCopyResult.UnknownType;
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message + ", Probable Data Format Issue, Value: " + value + "; Type: " + targetProp.PropertyType;
                log.AppendLine(msg);
                Console.WriteLine(msg);
                return ValueCopyResult.Error;
            }
            return ValueCopyResult.Success;
        }

        private void CopySourceValueToTargetValue(PropertyInfo srcProp, Object srcObj, PropertyInfo targetProp, Object targetObj)
        {
            targetProp.SetValue(targetObj, srcProp.GetValue(srcObj));  // will throw an exception if types are not the same - that's up to the class definitions to match!
        }

        /// <summary>
        /// Creates a new ArbitrationCase record only (does not create any child records such as ClaimCPT codes).
        /// Used by the bulk import processes.
        /// </summary>
        /// <param name="caseRecord"></param>
        /// <param name="runAs"></param>
        /// <param name="batchUploadDate"></param>
        /// <param name="nsa"></param>
        /// <param name="changeLog"></param>
        /// <param name="au">Local authority object with Tracking schema loaded</param>
        /// <returns></returns>
        private async Task<string> CreateArbitrationCase(ArbitrationCase caseRecord, bool skipDOBCheck, AppUser runAs, DateTime batchUploadDate, Authority nsa, CaseLog changeLog, Authority? au = null, bool force = false)
        {
            try
            {
                // check that Authority exists if none is provided (loops should provide it for performance enhancement)
                if (au == null && !string.IsNullOrEmpty(caseRecord.Authority))
                    au = string.IsNullOrEmpty(caseRecord.Authority) ? null : Authorities.FirstOrDefault(d => d.Key.Equals(caseRecord.Authority, StringComparison.CurrentCultureIgnoreCase));

                await InitCaseOwnerAsync(au, caseRecord);

                // check the general validation rules
                if (!force)
                {
                    var check = await ValidateArbitrationCase(caseRecord, skipDOBCheck, nsa, au, false);
                    if (!string.IsNullOrEmpty(check))
                        return check;
                }

                // set up Tracking
                if (au != null && !au.Key.Equals("TX", StringComparison.CurrentCultureIgnoreCase))
                    caseRecord.Tracking = new CaseTracking() { Id = 0, TrackingValues = await GetAuthorityTrackingJSON(au.Key), UpdatedBy = "system", UpdatedOn = batchUploadDate };


                // initialize tracking info for NSA 
                caseRecord.NSATracking = await GetAuthorityTrackingJSON("nsa");

                if (caseRecord.EOBDate.HasValue)
                {
                    var nsaNode = JsonNode.Parse(caseRecord.NSATracking);
                    if (nsaNode != null)
                    {
                        await Extensions.EnsureHolidays(_context);
                        Utilities.UpdateTrackingCalculations(nsaNode, nsa.TrackingDetails, caseRecord);
                        caseRecord.NSATracking = nsaNode.ToJsonString();
                    }
                }

                // init TX dates if avail (need a better way of initializing local tracking as we get more authorities coming on board - prob just a couple of choices on the Manage Authorities screen)
                if (caseRecord.Authority.Equals("TX", StringComparison.CurrentCultureIgnoreCase) && !caseRecord.ArbitrationDeadlineDate.HasValue && caseRecord.FirstResponseDate.HasValue)
                {
                    // TDI rule: Deadline is 90 days after date of first response from Payor
                    caseRecord.ArbitrationDeadlineDate = caseRecord.FirstResponseDate.Value.AddDays(90);
                }


                if ((caseRecord.PayorId > 0 || !string.IsNullOrEmpty(caseRecord.Payor)) && !string.IsNullOrEmpty(caseRecord.PayorGroupNo))
                    await InitPayorGroupInfo(caseRecord, null);

                caseRecord.Status = ArbitrationStatus.New;

                if (string.IsNullOrEmpty(caseRecord.AuthorityStatus))
                    caseRecord.AuthorityStatus = "Not Submitted";

                if (string.IsNullOrEmpty(caseRecord.NSAStatus))
                    caseRecord.NSAStatus = "Pending NSA Negotiation Request";

                caseRecord.IsUnread = true;
                caseRecord.CreatedOn = batchUploadDate;
                caseRecord.CreatedBy = runAs.Email;
                caseRecord.UpdatedBy = runAs.Email;
                caseRecord.UpdatedOn = batchUploadDate;
                caseRecord.Log.Add(changeLog);
                _context.ArbitrationCases.Add(caseRecord);
                var customer = Customers.FirstOrDefault(d => d.Name.Equals(caseRecord.Customer.Trim(), StringComparison.CurrentCultureIgnoreCase));
                if (customer != null)
                {
                    caseRecord.Customer = customer.Name;
                }
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                return ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }

            return string.Empty;
        }

        private async Task<Boolean> InitPayorGroupInfo(ArbitrationCase caseRecord, Dictionary<string, Object>? changes)
        {
            bool wasChanged = false;
            bool logChanges = changes != null;

            if (string.IsNullOrEmpty(caseRecord.PayorGroupNo))
                return wasChanged;

            // TODO: Use Select LINQ method to omit JSON due to size
            var payor = await _context.Payors.Include(d => d.PayorGroups.Where(b => b.GroupNumber == caseRecord.PayorGroupNo)).FirstOrDefaultAsync(d => d.Id == caseRecord.PayorId || d.Name == caseRecord.Payor);
            if (payor == null)
                return wasChanged;
            if (payor.Id != payor.ParentId)
                await _context.Payors.Include(d => d.PayorGroups.Where(b => b.GroupNumber == caseRecord.PayorGroupNo)).FirstOrDefaultAsync(d => d.Id == payor.ParentId);
            if (payor == null || payor.PayorGroups.Count() != 1)
                return wasChanged;

            var g = payor.PayorGroups.First();
            if (caseRecord.PayorGroupName != g.GroupName)
            {
                caseRecord.PayorGroupName = g.GroupName;
                if (logChanges && !changes!.ContainsKey("PayorGroupName"))
                    changes.Add("PayorGroupName", caseRecord.PayorGroupName);
                wasChanged = true;
            }

            string planType = "";
            if (g.PlanType == PlanType.FullyInsured)
                planType = "Fully Insured";
            else if (g.PlanType == PlanType.SelfFunded)
                planType = "Self-Funded";
            else if (g.PlanType == PlanType.SelfFundedOptIn)
                planType = "Self-Funded (Opt-In)";

            if (planType != caseRecord.PlanType)
            {
                caseRecord.PlanType = planType;
                if (logChanges && !changes!.ContainsKey("PlanType"))
                    changes.Add("PlanType", caseRecord.PayorGroupNo);
                wasChanged = true;
            }

            // When group number changes, may need to update the appropriate status fields
            if (logChanges && changes!.ContainsKey("PayorGroupNo"))
            {
                if (g.IsNSAIneligible && caseRecord.NSAStatus == "Pending NSA Negotiation Request")
                {
                    caseRecord.NSAStatus = "Ineligible";
                    caseRecord.NSAWorkflowStatus = ArbitrationStatus.Ineligible;
                    if (logChanges)
                    {
                        if (!changes!.ContainsKey("NSAStatus"))
                            changes.Add("NSAStatus", "Ineligible");
                        if (!changes!.ContainsKey("NSAWorkflowStatus"))
                            changes.Add("NSAWorkflowStatus", ArbitrationStatus.Ineligible);
                    }
                }
            }

            return wasChanged;
        }

        private async Task<RecordImportResults> FindAndUpdateEHRDetail(ImportDetail detailRecord, List<ImportFieldConfig> configs, int rowCount, DateTime batchUploadDate, StringBuilder log, bool isDryRun, AppUser runAs, Authority nsa)
        {
            if (configs.Count() == 0)
                throw new ArgumentException("Unexpected: Missing column configuration!");

            RecordImportResults result = new RecordImportResults(0, RecordImportActionResult.Skipped);
            if (detailRecord.CPTCode.Length < 5)
            {
                log.AppendLine($@"Line {rowCount}: CPTCode must be 5 or more characters. Skipping line.");
                return result;
            }
            var findResult = await FindArbitrationCase(detailRecord, true, true);
            ArbitrationCase? header = findResult.Record;
            string message = findResult.Message;

            if (!string.IsNullOrEmpty(message))
            {
                log.AppendLine($@"Line {rowCount}: {message}. Skipping line.");
                return result;
            }

            if (header == null)
            {
                var msg = $@"Line {rowCount}: Unable to locate existing case using composite encounter key. Skipping line.";
                log.AppendLine(msg);
                return result;
            }

            result.ArbitrationCaseId = header.Id;

            var originalClaimCPT = await _context.ClaimCPT.Where(d => !d.isDeleted && d.ArbitrationCaseId == header.Id && d.CPTCode == detailRecord.CPTCode).FirstOrDefaultAsync();

            // populate a new ClaimCPT record we can use to detect different from original
            var imported = new ClaimCPT();
            await SyncObjectData(detailRecord, imported, configs);

            IEnumerable<string> fieldList = configs.Select(d => d.TargetFieldname);

            if (originalClaimCPT != null)
            {
                string diffs = DetectDifferencesBetweenObjects(originalClaimCPT, imported, fieldList);
                if (!string.IsNullOrEmpty(diffs))  // this only detects that are potential differences - all depends on which fields really matter when it comes time to sync the changes (below)
                {

                    // TODO: field-by-field config necessary or always overwrite with incoming data? Perhaps a NeverWithEmpty option is needed to prevent bulk erasure!

                    try
                    {
                        if (!isDryRun)
                        {
                            var syncResult = await SyncObjectData(imported, originalClaimCPT, configs);
                            if (syncResult.WereChangesMade)
                            {
                                var msg = await UpdateCPTBenchmarks(header, originalClaimCPT);
                                string changeLogDetails = "{\"cptCodes\": [" + diffs + "]";

                                if (!string.IsNullOrEmpty(msg))
                                {
                                    log.AppendLine($@"Line {rowCount}: {msg}");
                                    if (msg.StartsWith("GeoZip defaulted"))
                                    {
                                        // capture the change to the header
                                        changeLogDetails += $@",""LocationGeoZip"":""{header.BenchmarkGeoZip}""";
                                    }
                                }
                                changeLogDetails += "}";
                                var changeLog = new CaseLog()
                                {
                                    Action = "EHRImport",
                                    CreatedBy = _principal.Identity?.Name ?? "anonymous",
                                    CreatedOn = batchUploadDate,
                                    Details = changeLogDetails,
                                    Id = 0
                                };
                                header.Log.Add(changeLog);
                                header.IsUnread = true;
                                await _context.SaveChangesAsync();

                                result.RecordImportResult = RecordImportActionResult.Updated;
                            }
                            log.AppendLine($@"Line {rowCount}: Updated ClaimCPT Id {originalClaimCPT.Id} and logged the change.");
                        }
                        else
                        {
                            log.AppendLine($@"Line {rowCount}: DRY_RUN - Updates to ClaimCPT Id {originalClaimCPT.Id} detected.");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.AppendLine($@"Line {rowCount}: Error during updates: " + ex.Message);
                        if (ex.InnerException != null)
                            log.AppendLine($@"Line {rowCount}: Inner Exception: " + ex.InnerException.Message);
                        result.RecordImportResult = RecordImportActionResult.Error;
                    }
                }
                else
                {
                    log.AppendLine($@"Line {rowCount}: No changes detected to ClaimCPT record. Record skipped.");
                }
            }
            else
            {
                try
                {
                    result.RecordImportResult = RecordImportActionResult.Added;
                    if (!isDryRun)
                    {
                        // create new CPT
                        var cpt = new ClaimCPT()
                        {
                            Id = 0,
                            CPTCode = detailRecord.CPTCode,
                            IsEligible = detailRecord.IsEligible,
                            IsIncluded = detailRecord.IsEligible,
                            Modifier26_YN = detailRecord.Modifier26_YN(),
                            Modifiers = detailRecord.Modifiers,
                            PaidAmount = detailRecord.PaidAmount,
                            PatientRespAmount = detailRecord.PatientRespAmount,
                            ProviderChargeAmount = detailRecord.ProviderChargeAmount,
                            Units = detailRecord.Units,
                            UpdatedBy = "system",
                            UpdatedOn = Utilities.GetCurrentUtcDate()
                        };

                        var msg = await UpdateCPTBenchmarks(header, cpt);
                        if (!string.IsNullOrEmpty(msg))
                        {
                            log.AppendLine($@"Line {rowCount}: {msg}");
                            if (msg.StartsWith("GeoZip defaulted"))
                            {
                                string changeDetails = $@"{{""LocationGeoZip"":""{header.BenchmarkGeoZip}""}}";
                                var changeLog = new CaseLog()
                                {
                                    Action = "EHRImport",
                                    CreatedBy = _principal.Identity?.Name ?? "anonymous",
                                    CreatedOn = batchUploadDate,
                                    Details = changeDetails,
                                    Id = 0
                                };
                                header.Log.Add(changeLog);
                                header.IsUnread = true;
                            }
                        }
                        header.CPTCodes.Add(cpt);
                        header.IsUnread = true;
                        // if the header id is same as last header id, 
                        await _context.SaveChangesAsync();
                        log.AppendLine($@"Line {rowCount}: Created new CPT {cpt.CPTCode} on ArbitrationCaseId {header.Id}");
                    }
                    else
                    {
                        log.AppendLine($@"Line {rowCount}: DRY_RUN - Would Create new CPT {detailRecord.CPTCode} on ArbitrationCaseId {header.Id}");
                    }
                }
                catch (Exception ex)
                {
                    log.AppendLine($@"Line {rowCount}: Error adding CPT: " + ex.Message);
                    if (ex.InnerException != null)
                        log.AppendLine($@"Line {rowCount}: Inner Exception: " + ex.InnerException.Message);
                    result.RecordImportResult = RecordImportActionResult.Error;
                }
            }
            return result;
        }

        private async Task<bool> InitCaseOwnerAsync(Authority? au, ArbitrationCase caseRecord)
        {
            // init Customer if a Customer mapping exists for this Authority
            if (string.IsNullOrEmpty(caseRecord.Customer) && au != null && au.AuthorityJson != null)
            {
                var maps = au.AuthorityJson.CustomerMappings;
                if (maps != null)
                {
                    var map = maps.FirstOrDefault(d => d.UserId.Equals(caseRecord.AuthorityUserId, StringComparison.CurrentCultureIgnoreCase));
                    if (map != null && await _context.Customers.FirstOrDefaultAsync(d => d.Name == map.CustomerName) != null)
                    {
                        caseRecord.Customer = map.CustomerName;
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task InitCaseTrackingValues(ArbitrationCase caseRecord, List<ImportFieldConfig> configs, Authority nsa)
        {
            if (nsa.TrackingDetails.Count == 0)
                throw new Exception("Critical: NSA Authority tracking details are missing!");

            await Extensions.EnsureHolidays(_context);
            var srcProps = caseRecord.GetType().GetProperties().Where(d => d.GetIndexParameters().Length == 0);
            var nsaNode = JsonNode.Parse(caseRecord.NSATracking);

            var authNode = caseRecord.Tracking != null && !string.IsNullOrEmpty(caseRecord.Tracking.TrackingValues) ? JsonNode.Parse(caseRecord.Tracking.TrackingValues) : JsonNode.Parse("{}");
            await EnsureAuthorities(); ;

            // NOTE: If an Authority is not marked as Active it means they do not have a bifurcated arbitration process. Therefore, no custom date scheme modifications will be initialized or modified.
            // NOTE: EnsureAuthorities caches all Authorities along with their tracking configurations
            var auth = string.IsNullOrEmpty(caseRecord.Authority) ? null : Authorities.FirstOrDefault(d => d.IsActive && d.Key.Equals(caseRecord.Authority));

            bool updateNational = nsaNode != null && nsaNode.AsObject().ToArray().Length > 0;
            bool updateLocal = caseRecord.Tracking != null && auth != null && authNode != null && authNode.AsObject().ToArray().Length > 0;

            foreach (var tc in configs.Where(d => d.IsTracking && d.IsActive))
            {
                PropertyInfo? srcProp = srcProps.Where(d => d.Name.Equals(tc.SourceFieldname, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                /*
                 * If we can't find a field name on the source object that matches SourceFieldName we try the TargetField Name.
                 * This is a bad (workaround) for an earlier design decision that allowed SourceFieldName in the ImportConfig
                 * to be something other than legitimate ArbitrationCase property names. The entire way the Import process works
                 * in the app needs to be streamlined such that there's no such thing as allowing a column on a CSV (source) file
                 * to be anything other than a property name for ArbitrationCase, ClaimCPT or one of the Tracking field names. 
                 * */
                if (srcProp == null)
                    srcProp = srcProps.Where(d => d.Name.Equals(tc.TargetFieldname, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

                if (srcProp == null)
                    continue;

                var value = srcProp.GetValue(caseRecord) as DateTime?;
                if (!value.HasValue)
                    continue;

                if (updateNational)
                    UpdateTrackingNode(nsa, value, nsaNode!, tc.TargetFieldname, caseRecord); // nsaChanged = nsaChanged | 

                if (updateLocal)
                    UpdateTrackingNode(auth!, value, authNode!, tc.TargetFieldname, caseRecord); // authChanged = authChanged | 
            }

            if (updateNational)
            {
                Utilities.UpdateTrackingCalculations(nsaNode!, nsa.TrackingDetails, caseRecord);
                caseRecord.NSATracking = nsaNode!.ToJsonString();
            }
            else
            {
                caseRecord.NSATracking = "{}";
            }

            if (updateLocal)
            {
                Utilities.UpdateTrackingCalculations(authNode!, auth!.TrackingDetails, caseRecord);
                caseRecord.Tracking!.TrackingValues = authNode!.ToJsonString();
            }
            /* NOTE: Electing not to clear out any "local" authority tracking for now. Why? Because someone could switch the IsActive
             * flag on an Authority which would mean they no longer are "bifurcated" with their own arbitration process. If there
             * are existing dates, these would be wiped out perhaps without the user's knowledge or intention. This doesn't really 
             * apply to the NSA scenario because it is handled differently.
            else if(caseRecord.Tracking != null)
            {
                caseRecord.Tracking.TrackingValues = "{}";
            }
            */
        }

        /// <summary>
        /// Uses the configs list to decide what data to copy from caseRecord (which usually contains the values copied from the 
        /// CSV import file) to either a new ArbitrationCase record or the original record located in the database.
        /// </summary>
        /// <param name="caseRecord"></param>
        /// <param name="configs"></param>
        /// <param name="rowCount"></param>
        /// <param name="batchUploadDate"></param>
        /// <param name="log"></param>
        /// <param name="isDryRun"></param>
        /// <param name="createCaseIfMissing"></param>
        /// <param name="runAs"></param>
        /// <param name="nsa"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private async Task<RecordImportResults> FindAndUpdateEHRHeader(ArbitrationCase caseRecord, List<ImportFieldConfig> configs, int rowCount, DateTime batchUploadDate, StringBuilder log, bool isDryRun, bool createCaseIfMissing, AppUser runAs, Authority? nsa = null)
        {
            caseRecord.AuthorityCaseId = "";

            // TODO: Could limit the functionality of this method based on the roles of the AppUser

            if (configs.Count() == 0)
                throw new ArgumentException("Unexpected: Missing column configuration!");

            if (nsa == null || !nsa.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase))
                nsa = Authorities.FirstOrDefault(d => d.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase));

            if (nsa == null)
                throw new ArgumentException("The NSA authority is missing from the database!");

            RecordImportResults result = new RecordImportResults(caseRecord.Id, RecordImportActionResult.Skipped);

            Authority? au = null;
            if (!string.IsNullOrEmpty(caseRecord.Authority))
            {
                au = Authorities.FirstOrDefault(d => d.Key.Equals(caseRecord.Authority, StringComparison.CurrentCultureIgnoreCase));
                if (au == null)
                {
                    log.AppendLine($@"Unable to locate an authority for the key '{caseRecord.Authority}'");
                    return result;
                }
            }

            bool skipDOBCheck = false;

            var findResult = await FindArbitrationCase(caseRecord, skipDOBCheck, false);
            ArbitrationCase? originalCase = findResult.Record;
            string message = findResult.Message;

            if (!string.IsNullOrEmpty(message))
            {
                log.AppendLine($@"Line {rowCount}: {message}! Skipping line.");
                return result;
            }

            bool hasUnread = false;
            bool trackingUpdated = false;

            // Record not found - maybe make a new one
            if (originalCase == null)
            {
                if (createCaseIfMissing)
                {
                    if (AUTO_ADD_PAYORS)
                    {
                        await CreatePayorIfNeededAsync(_errorContext, caseRecord.Payor);
                    }

                    var changeLog = new CaseLog()
                    {
                        Action = "EHRImport",
                        CreatedBy = runAs.Email,
                        CreatedOn = batchUploadDate,
                        Details = "Created during EHR Header Import",
                        Id = 0
                    };

                    caseRecord.CreatedBy = runAs.Email;
                    caseRecord.CreatedOn = batchUploadDate;
                    caseRecord.LocationGeoZip = "750";
                    message = await CreateArbitrationCase(caseRecord, skipDOBCheck, runAs, batchUploadDate, nsa, changeLog, au);

                    if (!string.IsNullOrEmpty(message))
                    {
                        log.AppendLine($@"Line {rowCount}: {message} (Skipping line)");
                        return result;
                    }

                    try
                    {
                        await InitCaseTrackingValues(caseRecord, configs, nsa);
                        await _context.SaveChangesAsync();
                        log.AppendLine($@"Line {rowCount}: Added ArbitrationCaseId {caseRecord.Id} and logged the change.");
                        result.RecordImportResult = RecordImportActionResult.Added;
                    }
                    catch (Exception ex)
                    {
                        log.AppendLine($@"Line {rowCount}: Error while creating new ArbitrationCase using EHR Header data: " + ex.Message);
                        // prevent the next save from bombing
                        _context.ArbitrationCases.Remove(caseRecord);
                        _context.ChangeTracker.Clear();
                    }

                    return result;
                }
                else
                {
                    log.AppendLine($@"Line {rowCount}: Unable to locate existing, active case using Customer+Payor+PayorClaimNumber, AuthorityCaseId or NSACaseId. Skipping line.");
                    return result;
                }
            }
            else
            {
                // record found 

                /* validations of inbound data */

                // Customer and Entity
                var customer = Customers.FirstOrDefault(d => d.Name.Equals(caseRecord.Customer.Trim(), StringComparison.CurrentCultureIgnoreCase));
                if (customer == null)
                {
                    log.AppendLine($@"Line {rowCount}: Missing or invalid Customer. (Skipping line)");
                    return result;
                }
                caseRecord.Customer = customer.Name;
                if (!string.IsNullOrEmpty(caseRecord.EntityNPI))
                {
                    var nt = customer.Entities.FirstOrDefault(d => d.NPINumber == caseRecord.EntityNPI);
                    if (nt == null)
                    {
                        log.AppendLine($@"Line {rowCount}: EntityNPI {caseRecord.EntityNPI} does not match a Customer Entity for {customer.Name}. (Skipping line)");
                        return result;
                    }
                    else
                    {
                        caseRecord.Entity = nt.Name;
                    }
                }
                else if (!string.IsNullOrEmpty(caseRecord.Entity))
                {
                    var customerEntity = customer.Entities.FirstOrDefault(d => d.Name.Equals(caseRecord.Entity.Trim(), StringComparison.CurrentCultureIgnoreCase));
                    if (customerEntity == null)
                    {
                        log.AppendLine($@"Line {rowCount}: Entity {caseRecord.Entity} does not match a Customer Entity for {customer.Name}. (Skipping line)");
                        return result;
                    }
                    else
                    {
                        caseRecord.Entity = customerEntity.Name;
                        caseRecord.EntityNPI = customerEntity.NPINumber;
                    }
                }

                // Because the authority date schemes can change, validate and fix the schema one time before calling any data updates.
                // We could argue that it would make more sense to only do this during Utilities.SetTrackingValue method but this would
                // result in the method being called multiple times per record (an unnecessary performance hit).
                //log.AppendLine($@"EOBDate={original.EOBDate}; NSATracking(Before)={original.NSATracking}");
                trackingUpdated = Utilities.ValidateTracking(originalCase, nsa, au);
                //log.AppendLine($@"EOBDate={original.EOBDate}; NSATracking(After) ={original.NSATracking}");
            }

            // Copy values from new record to original if any new values are detected
            IEnumerable<string> fieldList = configs.Select(d => d.TargetFieldname);
            bool wasUnreadAlready = originalCase.IsUnread;
            string diffs = DetectDifferencesBetweenObjects(originalCase, caseRecord, fieldList);

            if (trackingUpdated || !string.IsNullOrEmpty(diffs)) // different just creates a list of differences, not what will actually change. That's up to the ImportFieldConfig records.
            {
                try
                {
                    if (!isDryRun)
                    {
                        // It is important to understand that this method supports the EHR import process.
                        // If the ImportFieldConfig.Action for NSAStatus is set to something other than OnlyWhenEmpty,
                        // this import process method could overwrite the current value (set by a user or NSA import when available) 
                        // with an empty / invalid value.
                        // Until the NSA authority gets an ArchiveIfNeeded code path just like the local Authority currently has, this is a risk.

                        // make sure the caseRecord object has the correct PayorId before synchronizing
                        if (!string.IsNullOrEmpty(caseRecord.Payor))
                        {
                            var payor = FindPayerAsync(caseRecord, log, true).Result;
                            if (payor == null)
                            {
                                log.AppendLine($@"Line {rowCount}: Payor {caseRecord.Payor} does not match a known Payor and could not add new Payor. (Skipping line)");
                                return result;
                            }
                            caseRecord.PayorId = payor.Id;
                            originalCase.PayorId = payor.Id;
                            // no need to set hasUnread because a change in the Payor string will be picked up later
                        }

                        // NOTE 2: syncObjectData is not called a second time on the CaseSettlementDetails child record (if present) because 
                        // the EHR import is not meant to provide Authority-related settlement information.
                        var syncResult = await SyncObjectData(caseRecord, originalCase, configs);
                        hasUnread = hasUnread | syncResult.WereChangesMade;
                        var updatedCustomer = await InitCaseOwnerAsync(au, originalCase);
                        hasUnread = hasUnread | updatedCustomer;

                        if (trackingUpdated || hasUnread)
                        {
                            // Init TX deadline if not already - needs to be DRYed out with duplicate code in the createArbitrationCase method.
                            // Probably need a factory pattern to pass all of these claims through the same init procedures
                            if (caseRecord.Authority.Equals("TX", StringComparison.CurrentCultureIgnoreCase) && !originalCase.ArbitrationDeadlineDate.HasValue && originalCase.FirstResponseDate.HasValue)
                            {
                                // TDI rule: Deadline is 90 days after date of first response from Payor
                                originalCase.ArbitrationDeadlineDate = originalCase.FirstResponseDate.Value.AddDays(90);
                            }
                            originalCase.IsUnread = true;

                            if (trackingUpdated)
                            {
                                try
                                {
                                    var jobJSON = JsonNode.Parse(syncResult.ChangesJSON);
                                    if (jobJSON != null)
                                    {
                                        jobJSON["NSATracking"] = originalCase.NSATracking;
                                        if (originalCase.Tracking != null)
                                        {
                                            var tr = new JsonObject();
                                            tr["trackingValues"] = originalCase.Tracking.TrackingValues;
                                            jobJSON["Tracking"] = tr;
                                        }
                                        syncResult.ChangesJSON = jobJSON.ToJsonString();
                                    }
                                }
                                catch { } // don't let this bomb the routine in the unlikely event we missed something here
                            }

                            var changeLog = new CaseLog()
                            {
                                Action = "EHRImport",
                                CreatedBy = runAs.Email,
                                CreatedOn = batchUploadDate,
                                Details = syncResult.ChangesJSON,
                                Id = 0
                            };
                            originalCase.Log.Add(changeLog);

                            // add separate log entry if Customer was auto-assigned
                            if (updatedCustomer)
                            {
                                changeLog = new CaseLog()
                                {
                                    Action = "EHRImport",
                                    CreatedBy = runAs.Email,
                                    CreatedOn = batchUploadDate,
                                    Details = $@"{{""customer"":""{originalCase.Customer}""}}",
                                    Id = 0
                                };
                                originalCase.Log.Add(changeLog);
                            }

                            await _context.SaveChangesAsync();
                            result.RecordImportResult = RecordImportActionResult.Updated;
                            log.AppendLine($@"Line {rowCount}: Updated ArbitrationCase.Id {originalCase.Id} and logged the change");
                        }
                        else
                        {
                            log.AppendLine($@"Line {rowCount}: No changes detected for ArbitrationCase.Id {originalCase.Id}");
                        }
                    }
                    else
                    {
                        log.AppendLine($@"Line {rowCount}: DRY_RUN - Updates to ArbitrationCaseId {originalCase.Id} and the CaseLog would have been made.");
                    }
                }
                catch (Exception ex)
                {
                    log.AppendLine($@"Line {rowCount}: Error during updates: " + ex.Message);
                    if (ex.InnerException != null)
                        log.AppendLine($@"Line {rowCount}: Inner Exception: " + ex.InnerException.Message);

                    result.RecordImportResult = RecordImportActionResult.Error;
                    _context.ChangeTracker.Clear();
                }
            }
            else
            {
                log.AppendLine($@"Line {rowCount}: No differences detected in existing Case.");
            }

            return result;
        }

        private bool IsPropertyEmptyOrZero(PropertyInfo prop, Object obj)
        {
            if (prop.GetValue(obj) == null)
                return true;
            if (Type.GetTypeCode(prop.PropertyType) == TypeCode.String && string.IsNullOrEmpty((string?)prop.GetValue(obj)))
                return true;
            if (prop.PropertyType.IsNumeric() && Convert.ToDouble(prop.GetValue(obj)) == 0)
                return true;

            return false;
        }

        private async Task RecalculateCaseHeader(int id)
        {
            var claim = await _context.ArbitrationCases.FindAsync(id);  // choosing to include IsDeleted Cases just to avoid some later scavenger hunt for a "bug"
            var CPTs = await _context.ClaimCPT.Where(claimCPT => !claimCPT.isDeleted && claimCPT.ArbitrationCaseId == id).ToListAsync();
            if (claim != null && CPTs.Count() > 0)
            {
                claim.FH50thPercentileExtendedCharges = CPTs.Sum(claimCPT => claimCPT.FH50thPercentileExtendedCharges);
                claim.FH80thPercentileExtendedCharges = CPTs.Sum(claimCPT => claimCPT.FH80thPercentileExtendedCharges);
                claim.TotalChargedAmount = CPTs.Where(d => d.IsIncluded).Sum(claimCPT => claimCPT.ProviderChargeAmount);
                claim.TotalPaidAmount = CPTs.Where(d => d.IsIncluded).Sum(claimCPT => claimCPT.PaidAmount);
                claim.PatientShareAmount = CPTs.Where(d => d.IsIncluded).Sum(claimCPT => claimCPT.PatientRespAmount);
            }
        }

        /*
        private void processAuthorityChanges(ArbitrationCase target, string previousStatus, ILogger<ImportDataSynchronizer> _logger)
        {
            // TODO: Unfinished! Needs to read the new authority details record format and use the import configurations to handles the data
            if (!target.IsUnread)
                return; // case values haven't changed so nothing to process

            target.UpdatedBy = "Authority";
            target.UpdatedOn = Utilities.GetCurrentUtcDate();
        }
        */

        /// <summary>
        /// Check for any status changes and apply business rules
        /// </summary>
        /// <param name="target"></param>
        /// <param name="previousStatus"></param>
        /// <param name="_logger"></param>
        private void ProcessAuthorityChanges(ArbitrationCase target, string previousStatus, bool isNSA, ILogger<ImportDataSynchronizer> _logger, IEnumerable<AuthorityStatusMapping>? mappings, StringBuilder? log = null)
        {
            if (string.IsNullOrEmpty(target.UpdatedBy))
                target.UpdatedBy = "Authority";
            if (!target.UpdatedOn.HasValue)
                target.UpdatedOn = Utilities.GetCurrentUtcDate();

            if (isNSA)
            {
                if (!previousStatus.Equals(target.NSAStatus, StringComparison.CurrentCultureIgnoreCase))
                {
                    var wfStatus = GetWorkflowStatusFromAuthorityStatus(mappings, target.NSAStatus);
                    if (wfStatus != ArbitrationStatus.Unknown) // wait for nsa/local split branch to be merged in to use this: && target.NSAWorkflowStatus < from.WorkflowStatus.Value)
                    {
                        target.NSAWorkflowStatus = wfStatus;
                        var msg = $@"NSA status change detected ({target.NSAStatus}). Updating NSAWorkflowStatus to {wfStatus}.";
                        if (log != null)
                            log.AppendLine(msg);

                        _logger.LogInformation(msg);
                        return;
                    }

                }
            }
            else
            {
                if (!previousStatus.Equals(target.AuthorityStatus, StringComparison.CurrentCultureIgnoreCase))
                {
                    var wfStatus = GetWorkflowStatusFromAuthorityStatus(mappings, target.AuthorityStatus);
                    if (wfStatus != ArbitrationStatus.Unknown && target.Status < wfStatus)
                    {
                        target.Status = wfStatus;
                        var msg = $@"Authority status change detected ({target.AuthorityStatus}). Updating WorkflowStatus to {wfStatus}.";
                        if (log != null)
                            log.AppendLine(msg);

                        _logger.LogInformation(msg);
                        return;
                    }
                }
            }
        }

        private bool UpdateTrackingNode(Authority authority, DateTime? value, JsonNode benchmarkValues, string trackingFieldName, ArbitrationCase arbitCase)
        {
            if (benchmarkValues != null && benchmarkValues.AsObject().ContainsKey(trackingFieldName))
            {
                var t = benchmarkValues[trackingFieldName]; // pause and see what happens when miscast / can we detect if value is different?
                if (t == null)
                {
                    if (value.HasValue)
                    {
                        benchmarkValues[trackingFieldName] = value;
                        if (authority != null)
                            Utilities.UpdateTrackingCalculations(benchmarkValues, authority.TrackingDetails, arbitCase);
                        return true;
                    }
                }
                else if (!value.HasValue || !t.ToString().Equals(value.Value.ToString("s"), StringComparison.CurrentCultureIgnoreCase))
                {
                    benchmarkValues![trackingFieldName] = value;
                    if (authority != null)
                        Utilities.UpdateTrackingCalculations(benchmarkValues, authority.TrackingDetails, arbitCase);
                    return true;
                }
            }
            return false;
        }

        private Task<SendGrid.Response> SendEmailAsync(string ToAddress, string CcAddress, string Subject, string HTMLMessage, Dictionary<string, string> MessageArgs, List<string> MessageCategories, string FromAddress = "", string ReplyToAddress = "")
        {
            if (string.IsNullOrEmpty(ToAddress))
                throw new Exception("Cannot send an email without a ToAddress");

            if (SgClient == null)
            {
                // Do one-time setup of SendGrid stuff
                SgClient = new SendGridClient(SendGridApiKey);
            }
            if (FromAddress == string.Empty)
                FromAddress = ImportDataSynchronizer.FromAddress;
            if (ReplyToAddress == string.Empty)
                ReplyToAddress = ImportDataSynchronizer.ReplyToAddress;

            // create message and send
            if (!MessageCategories.Contains("Arbit System Notification"))
                MessageCategories.Add("Arbit System Notification");

            var msg = new SendGridMessage()
            {
                Categories = MessageCategories,
                From = new EmailAddress(FromAddress),
                ReplyTo = new EmailAddress(ReplyToAddress),
                Subject = Subject
            };

            if (MessageArgs.Count > 0)
                msg.CustomArgs = MessageArgs;

            // all of the TO, CC and BCC addresses
            int validRecipients = 0;
            var TOs = new List<string>();
            foreach (var g in ToAddress.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Utilities.IsValidEmail(g))
                {
                    msg.AddTo(g);
                    TOs.Add(g);
                    validRecipients++;
                }
            }

            if (validRecipients == 0)
                throw new Exception("No valid TO address detected.");

            if (!string.IsNullOrEmpty(CcAddress))
            {
                foreach (var g in CcAddress.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Utilities.IsValidEmail(g) && !TOs.Contains(g, StringComparer.CurrentCultureIgnoreCase))
                    {
                        msg.AddCc(g);
                        TOs.Add(g);
                    }
                }
            }

            msg.SetOpenTracking(true);
            msg.HtmlContent = HTMLMessage;

            return SgClient.SendEmailAsync(msg);
        }

        private async Task<ObjectChangeResult> SetTrackingValue(Object source, PropertyInfo srcProp, ArbitrationCase target, string trackingFieldName, ImportFieldAction action = ImportFieldAction.Always)
        {
            var msg = new ObjectChangeResult();
            msg.WasChanged = false;

            if (srcProp.PropertyType != typeof(DateTime) && srcProp.PropertyType != typeof(DateTime?))
            {
                msg.ErrorMessage = $@"Invalid type. ImportFieldConfig.IsTracking currently only works with DateTime values ({srcProp.Name}).";
                return msg;
            }

            bool wasFound = false;
            PropertyInfo? eobProp = null;
            DateTime? eobValue = null;

            // Total HACK due to NSA team not having a full understanding how the federal govt's process worked before we started using this product :(
            // This will be removed later by renaming DateOfInitialPayment in NSA tracking configuration to NSA_EOBDate (because DateOfInitialClaimPayment clashes with other authorities' schemes)
            // and adding an Import Configuration mapping that targets this new field name.
            // Meanwhile, this is what happens: EOBDate is picked up from the ArbitrationCase record and substituted down below when updating NSATracking.

            // This will only happen IF the targeted field name is DateOfInitialClaimPayment AND EOBDate has a value AND the import configuration allows updating the field (Always or OnlyWhenEmpty)
            if (trackingFieldName.Equals("DateOfInitialCClaimPayment", StringComparison.CurrentCultureIgnoreCase))

            {
                var tempObj = new ArbitrationCase();
                eobProp = tempObj.GetType().GetProperties().FirstOrDefault(d => d.GetIndexParameters().Length == 0 && d.Name == "EOBDate");
                if (eobProp != null)
                    eobValue = eobProp.GetValue(source) as DateTime?;
            }

            var value = srcProp.GetValue(source) as DateTime?;

            try
            {
                // verify that tracking JSON is available
                if (string.IsNullOrEmpty(target.NSATracking))
                    target.NSATracking = await GetAuthorityTrackingJSON("nsa"); // init NSATracking JSON string if necessary

                var benchmarkValues = JsonNode.Parse(target.NSATracking);

                if (benchmarkValues != null && benchmarkValues.AsObject().ContainsKey(trackingFieldName))
                {
                    await EnsureAuthorities(); ;
                    var nsa = Authorities.FirstOrDefault(d => d.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase));
                    var t = benchmarkValues[trackingFieldName]; // TODO: set breakpoint here and see what happens when miscast. Can we detect if value is different?

                    // Always use the tracking value if the existing tracking field is missing or empty
                    if (t == null || t.ToString().Length == 0)
                    {
                        bool recalculate = false;
                        // hack continues...
                        if (eobProp != null && eobValue.HasValue)
                        {
                            benchmarkValues[trackingFieldName] = eobValue;
                            recalculate = true;
                        }
                        // replace tracking value with mapped value - no hacks here - leave the below code when removing the previous hack (see Git change log if in doubt)
                        else if (value.HasValue)
                        {
                            benchmarkValues[trackingFieldName] = value;
                        }

                        if (recalculate)
                        {
                            if (nsa != null)
                                Utilities.UpdateTrackingCalculations(benchmarkValues, nsa.TrackingDetails, target);
                            target.NSATracking = benchmarkValues.ToJsonString();
                            msg.WasChanged = true;
                        }
                    }
                    // Given that there's already a value in the tracking field, only process the Always condition (since we cannot overwrite what is there)
                    else if (action == ImportFieldAction.Always)
                    {
                        bool recalculate = false;
                        // hack continues...
                        if (eobProp != null && (!eobValue.HasValue || !t.ToString().Equals(eobValue.Value.ToString("yyyy-MM-ddTHH:mm:ss"), StringComparison.CurrentCultureIgnoreCase)))
                        {
                            benchmarkValues![trackingFieldName] = eobValue;
                            recalculate = true;
                        }
                        //if the new value is NULL OR the new value doesn't equal the old value, use it
                        else if (!value.HasValue || !t.ToString().Equals(value.Value.ToString("yyyy-MM-ddTHH:mm:ss"), StringComparison.CurrentCultureIgnoreCase))
                        {
                            benchmarkValues![trackingFieldName] = value;
                            recalculate = true;
                        }

                        if (recalculate)
                        {
                            if (nsa != null)
                                Utilities.UpdateTrackingCalculations(benchmarkValues, nsa.TrackingDetails, target);
                            target.NSATracking = benchmarkValues.ToJsonString();
                            msg.WasChanged = true;
                        }
                    }
                    wasFound = true;
                }

                // try matching an Authority tracking field
                if (target.Tracking != null)
                {
                    if (string.IsNullOrEmpty(target.Tracking.TrackingValues))
                        target.Tracking.TrackingValues = await GetAuthorityTrackingJSON(target.Authority); // init Authority tracking JSON string if necessary
                    if (string.IsNullOrEmpty(target.Tracking.TrackingValues))
                        target.Tracking.TrackingValues = "{}";

                    benchmarkValues = JsonNode.Parse(target.Tracking.TrackingValues);

                    if (benchmarkValues != null && benchmarkValues.AsObject().ContainsKey(trackingFieldName))
                    {
                        await EnsureAuthorities(); ;
                        var authority = Authorities.FirstOrDefault(d => d.Key.Equals(target.Authority, StringComparison.CurrentCultureIgnoreCase));
                        var t = benchmarkValues[trackingFieldName];
                        if (t == null)
                        {
                            if (value.HasValue)
                            {
                                benchmarkValues![trackingFieldName] = value;
                                if (authority != null)
                                    Utilities.UpdateTrackingCalculations(benchmarkValues, authority.TrackingDetails, target);
                                target.Tracking.TrackingValues = benchmarkValues.ToJsonString();
                                msg.WasChanged = true;
                            }
                        }
                        else if (!value.HasValue || !t.ToString().Equals(value.Value.ToString("yyyy-MM-ddTHH:mm:ss"), StringComparison.CurrentCultureIgnoreCase))
                        {
                            benchmarkValues![trackingFieldName] = value;
                            if (authority != null)
                                Utilities.UpdateTrackingCalculations(benchmarkValues, authority.TrackingDetails, target);
                            target.Tracking.TrackingValues = benchmarkValues.ToJsonString();
                            msg.WasChanged = true;
                        }
                        wasFound = true;
                    }
                }

                if (!wasFound && target.Tracking != null)
                {
                    var runAs = new AppUser { Email = "system", IsActive = true, Id = -1 };
                    msg.ErrorMessage = $@"Unable to locate tracking field {trackingFieldName} in any Authority.";
                    await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.MDMissingTrackingField, trackingFieldName, msg.ErrorMessage, _errorContext);
                    msg.ErrorMessage += " (Exception logged.)";
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
            }
            return msg;
        }

        /// <summary>
        /// Uses a list of ImportFieldConfig objects as a rule set when copying property values (with matching names and data types) from source to target.
        /// </summary>
        private async Task<SyncObjectDataResult> SyncObjectData(Object source, Object target, IEnumerable<ImportFieldConfig> configs) // , out bool hasUnread
        {
            bool hasUnread = false;
            var targetProps = target.GetType().GetProperties().Where(d => d.GetIndexParameters().Length == 0); // hopefully none of our properties are indexed since I'm not sure what to do with them at this point
            var srcProps = source.GetType().GetProperties().Where(d => d.GetIndexParameters().Length == 0);
            dynamic changes = new Dictionary<string, Object>(); // System.Dynamic.ExpandObject();
            var t = target as ArbitrationCase;

            foreach (var config in configs.Where(d => d.IsActive && d.Action != ImportFieldAction.Ignore))
            {
                PropertyInfo? targetProp = targetProps.Where(d => d.Name.Equals(config.TargetFieldname, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                PropertyInfo? srcProp = srcProps.Where(d => d.Name.Equals(config.SourceFieldname, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

                if (srcProp == null)
                {
                    _logger.LogError($@"SyncObjectData: ImportFieldConfig.Source refers to unknown property: {config.SourceFieldname}");
                    continue;
                }

                if (targetProp == null && t != null)
                {
                    // some special handling for ArbitrationCase 
                    if (config.IsTracking && t != null)
                    {
                        var tr = await SetTrackingValue(source, srcProp, t, config.TargetFieldname, config.Action);
                        if (!string.IsNullOrEmpty(tr.ErrorMessage))
                            _logger.LogError($@"SyncDataToArbCase: {tr}");

                        hasUnread = hasUnread | tr.WasChanged;
                        if (tr.WasChanged)
                            changes.Add(config.TargetFieldname, srcProp.GetValue(source));
                        continue;
                    }
                    /* TODO: Prob need to create a PaymentInfo-TX set of ImportFieldConfig records and pass those in to avoid dealing with ArbitrationCase explicitly
                     * Otherwise, we get a lot of false positive errors in this section.
                    else
                    {
                        var user = new AppUser { Email = "system", Id = -1, IsActive = true, JSON = "{}" };
                        var message = $@"SyncObjectData: Target property not found on ArbitrationCase object: {cfg.TargetFieldname}";
                        _logger.LogError(message);
                        await AddUnresolvedMasterDataException(MasterDataExceptionType.SyncTargetPropertyNotFound, cfg.TargetFieldname, message, user);
                        continue;
                    }
                    */
                }

                if (targetProp == null)
                    continue;

                if (targetProp.PropertyType.FullName != srcProp.PropertyType.FullName)
                {
                    var msg = $@"SyncDataToArbCase: Source and Target field type mismatch ({config.SourceFieldname})!";
                    var user = new AppUser { Email = "system", Id = -1, IsActive = true, JSON = "{}" };
                    await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.SyncTargetPropertyNotFound, config.SourceFieldname, msg, _errorContext);
                    _logger.LogError(msg);
                    continue;
                }

                try
                {
                    var chg = SetValueUsingImportConfigRules(config, targetProp, target, srcProp, source);
                    if (chg)
                        changes.Add(targetProp.Name, srcProp.GetValue(source));
                    hasUnread = hasUnread | chg;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $@"SyncDataToArbCase: " + ex.Message);
                }
            }

            var result = new SyncObjectDataResult();
            if (changes.Count > 0)
            {
                // special cascading handlers
                if (t != null)
                {
                    var dict = ((Dictionary<string, Object>)changes);
                    if (dict.ContainsKey("PayorGroupNo"))
                    {
                        var pgn = await InitPayorGroupInfo(t, dict);
                    }
                }
                result.ChangesJSON = JsonSerializer.Serialize(changes);
            }
            result.WereChangesMade = hasUnread;

            return result;
        }

        private bool SetValueUsingImportConfigRules(ImportFieldConfig cfg, PropertyInfo? targetProp, Object target, PropertyInfo? srcProp, Object source)
        {
            if (targetProp == null || srcProp == null)
                return false;

            bool wasModified = false;

            if (!AreValuesEqual(targetProp, target, srcProp, source))
            {
                if (cfg.Action == ImportFieldAction.OnlyWhenEmpty && IsPropertyEmptyOrZero(targetProp, target))
                {
                    try
                    {
                        CopySourceValueToTargetValue(srcProp, source, targetProp, target);
                        wasModified = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $@"SyncDataToArbCase: " + ex.Message);
                    }

                }
                else if (cfg.Action == ImportFieldAction.NeverWithEmpty && IsPropertyEmptyOrZero(srcProp, source) == false)
                {
                    try
                    {
                        CopySourceValueToTargetValue(srcProp, source, targetProp, target);
                        wasModified = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $@"SyncDataToArbCase: " + ex.Message);
                    }
                }
                else if (cfg.Action == ImportFieldAction.Always)
                {
                    try
                    {
                        CopySourceValueToTargetValue(srcProp, source, targetProp, target);
                        wasModified = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $@"SyncDataToArbCase: " + ex.Message);
                    }
                }
            }

            // special cascading handlers
            //if (wasModified && source is ArbitrationCase && cfg.TargetFieldname.Equals("PayorGroupNo"))
            //    InitPayorGroupInfo(source as ArbitrationCase);

            return wasModified;
        }

        private string TransformCaseSyncNames(string json, IEnumerable<ImportFieldConfig> configs, string direction)
        {
            var newJson = json;
            foreach (var config in configs)
            {
                if (direction == "ToTargetFieldName")
                    newJson.Replace(config.SourceFieldname, config.TargetFieldname);
                else
                    newJson.Replace(config.TargetFieldname, config.SourceFieldname);
            }
            return newJson;
        }

        private async Task<string> UpdateCPTBenchmarks(ArbitrationCase arbCase, ClaimCPT detail)
        {
            var user = new AppUser { Email = "system", Id = -1, IsActive = true };

            if (string.IsNullOrEmpty(arbCase.Authority))
            {
                var msg = $@"AuthorityCaseId: {arbCase.Id}  Header record does not contain an Authority. Skipping benchmark update.";
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.ClaimMissingAuthority, arbCase.Id.ToString(), msg, _errorContext);
                return msg + " (Exception logged.)";
            }

            if (string.IsNullOrEmpty(arbCase.ServiceLine))
            {
                var msg = $@"AuthorityCaseId: {arbCase.Id}  Header record does not contain a ServiceLine. Skipping benchmark update.";
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.ClaimMissingServiceLine, arbCase.Id.ToString(), msg, _errorContext);
                return msg + " (Exception logged.)";
            }

            if (string.IsNullOrEmpty(detail.CPTCode))
                return $@"AuthorityCaseId: {arbCase.Id}  Detail ID: {detail.Id}  Update record did not contain a valid CPTCode. Skipping benchmark update.";

            if (!arbCase.LocationGeoZip.StartsWith("7"))
            {
                arbCase.LocationGeoZip = "750";
            }
            string geoZipUsed = string.IsNullOrEmpty(arbCase.BenchmarkGeoZip) ? arbCase.LocationGeoZip : arbCase.BenchmarkGeoZip;

            var benchmarkQ = from a in _context.Set<Authority>()
                             from abd in _context.Set<AuthorityBenchmarkDetails>()
                             from bds in _context.Set<BenchmarkDataItem>()
                             where a.Key == arbCase.Authority
                             && abd.AuthorityId == a.Id
                             && abd.Service == arbCase.Service // benchmarks are divided by the full Service, not the prefix aka ServiceLine
                             && abd.IsDefault
                             && bds.BenchmarkDatasetId == abd.BenchmarkDatasetId
                             && bds.ProcedureCode == detail.CPTCode
                             //&& bds.Modifiers == detail.Modifiers
                             && (bds.GeoZip == geoZipUsed || bds.GeoZip == "750")  //TODO dirty hack - technical debt - make this value configurable on the Authority record
                             select new
                             {
                                 BenchmarkDatasetId = bds.BenchmarkDatasetId,
                                 zBenchmarks = bds.Benchmarks,
                                 Modifier = bds.Modifiers,
                                 PayorAllowedField = abd.PayorAllowedField,
                                 ProcedureCode = bds.ProcedureCode,
                                 ProviderChargesField = abd.ProviderChargesField,
                                 Service = abd.Service,
                                 GeoZip = bds.GeoZip
                             };

            var lookup = await benchmarkQ.ToArrayAsync();

            var testForValues = lookup.FirstOrDefault(d => d.GeoZip == geoZipUsed);
            if (testForValues == null)
                geoZipUsed = "750"; // more dirty data

            var item = string.IsNullOrEmpty(detail.Modifiers) ? lookup.FirstOrDefault(d => d.Modifier == String.Empty && d.GeoZip == geoZipUsed) : benchmarkQ.FirstOrDefault(d => d.Modifier == detail.Modifiers && d.GeoZip == geoZipUsed);
            //if(item == null)
            //    item = benchmarkQ.FirstOrDefault();  
            string returnVal = "";
            if (item != null && !string.IsNullOrEmpty(item.zBenchmarks))
            {

                var benchmarkValues = JsonSerializer.Deserialize<JsonDocument>(item.zBenchmarks);
                if (benchmarkValues != null && item.PayorAllowedField != null)
                {
                    try
                    {

                        detail.FH50thPercentileCharges = benchmarkValues.RootElement.TryGetProperty(item.PayorAllowedField, out JsonElement payorAllowedProperty) ? Convert.ToDouble(payorAllowedProperty.ToString()) : 0;
                        detail.FH80thPercentileCharges = benchmarkValues.RootElement.TryGetProperty(item.ProviderChargesField, out JsonElement providerChargesProperty) ? Convert.ToDouble(providerChargesProperty.ToString()) : 0;
                        detail.FH50thPercentileExtendedCharges = detail.Units * detail.FH50thPercentileCharges;
                        detail.FH80thPercentileExtendedCharges = detail.Units * detail.FH80thPercentileCharges;

                        // TODO: breaking LISKOV's principle here - should probably move the verification of available benchmarks to an ArbitrationCase or ClaimCPT validation method
                        if (geoZipUsed != arbCase.LocationGeoZip && geoZipUsed != arbCase.BenchmarkGeoZip)
                        {
                            arbCase.BenchmarkGeoZip = geoZipUsed;
                            returnVal = $@"GeoZip defaulted to {geoZipUsed}";  // ugh
                        }
                    }
                    catch
                    {
                        // swallow error for now - could add it to the Exceptions report but this could create tens of thousands of records if a bad benchmark set were imported
                        returnVal = $@"Error parsing BenchmarkDataItem. Dataset:{item.BenchmarkDatasetId},ProcedureCode:{detail.CPTCode},GeoZip:{arbCase.LocationGeoZip}). Skipping benchmark update.";
                        await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.BenchmarkMissingCPTValue, item.zBenchmarks, returnVal, _errorContext);
                    }
                }
                else
                {
                    returnVal = $@"Benchmark field empty on BenchmarkDataItem. Dataset:{item.BenchmarkDatasetId},ProcedureCode:{detail.CPTCode},GeoZip:{arbCase.LocationGeoZip}). Skipping benchmark update.";
                    await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.BenchmarkMissingCPTValue, item.zBenchmarks, returnVal, _errorContext);
                }

            }
            else
            {
                detail.FH50thPercentileCharges = 0;
                detail.FH80thPercentileCharges = 0;
                detail.FH50thPercentileExtendedCharges = detail.Units * detail.FH50thPercentileCharges;
                detail.FH80thPercentileExtendedCharges = detail.Units * detail.FH80thPercentileCharges;

                returnVal = $@"Benchmark missing. ID:{arbCase.Id}  Authority:{arbCase.Authority}  ProcedureCode:{detail.CPTCode}  GeoZip Used:{geoZipUsed}  Service:{arbCase.ServiceLine}. Resetting values to zero.";
                var json = $@"{{""Authority"":""{arbCase.Authority}"",""GeoZip"":""{geoZipUsed}"",""ProcedureCode"":""{detail.CPTCode}"",""Service"":""{arbCase.ServiceLine}""}}";
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.BenchmarkMissingCPTValue, json, returnVal, _errorContext);
            }
            return returnVal;
        }
        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task EnsureAuthorities()
        {
            if (Authorities.Count() != 0)
                return;

            Authorities = await _context.Authorities.AsNoTracking().Include(d => d.TrackingDetails.Where(g => !g.IsDeleted)).ToListAsync();

            // Test for NSA Authority record. If NSA not in Authorities list, add it
            var nsa = Authorities.FirstOrDefault(d => d.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase));
            if (nsa == null)
            {
                nsa = new Authority
                {
                    IsActive = true,
                    Key = "nsa",
                    Name = "USA CMS",
                    CalculatorOption = AuthorityCalculatorOption.Default,
                    StatusValues = "",
                    UpdatedBy = "system",
                    UpdatedOn = Utilities.GetCurrentUtcDate()
                };
                _errorContext.Authorities.Add(nsa);
                await _errorContext.SaveChangesAsync();
                Authorities.Add(nsa);
            }
        }

        private async Task<Payor?> FindPayerAsync(ArbitrationCase caseRecord, StringBuilder log, bool CreateNewIfNotFound = false)
        {
            Payor? payor = null;
            if (caseRecord.PayorId > 0)
            {
                payor = Payors.Count() > 0 ? Payors.FirstOrDefault(d => d.Id == caseRecord.PayorId) : await _context.Payors.FirstOrDefaultAsync(d => d.Id == caseRecord.PayorId);
            }

            if (payor == null && !string.IsNullOrEmpty(caseRecord.Payor))
            {
                caseRecord.Payor = caseRecord.Payor.Trim();
                payor = Payors.Count() > 0 ? Payors.FirstOrDefault(d => d.Name.Equals(caseRecord.Payor, StringComparison.CurrentCultureIgnoreCase)) : await _context.Payors.FirstOrDefaultAsync(d => d.Name.Equals(caseRecord.Payor, StringComparison.CurrentCultureIgnoreCase));
            }
            if (payor == null)
            {
                var message = $@"Payor not found: Payor: '{caseRecord.Payor}' or PayorId: '{caseRecord.PayorId}' Customer: {caseRecord.Customer}. EHR Number: {caseRecord.EHRNumber}. PayorClaimNumber: {caseRecord.PayorClaimNumber}.";
                await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.MDMissingPayor, caseRecord.Payor, message, _errorContext);
                log.AppendLine(message);

                if (CreateNewIfNotFound && !string.IsNullOrEmpty(caseRecord.Payor))
                {
                    await ImportUtils.AddMasterDataException(_logger, MasterDataExceptionType.MDMissingPayor, caseRecord.Payor, $@"Trying to create a new Payor: '{caseRecord.Payor}'", _errorContext);
                    log.AppendLine($@"Trying to create a new Payor: '{caseRecord.Payor}'");
                    await this.CreatePayorIfNeededAsync(_context, caseRecord.Payor);
                    payor = Payors.Count() > 0 ? Payors.FirstOrDefault(d => d.Name.Equals(caseRecord.Payor, StringComparison.CurrentCultureIgnoreCase)) : await _context.Payors.FirstOrDefaultAsync(d => d.Name.Equals(caseRecord.Payor, StringComparison.CurrentCultureIgnoreCase));
                }
            }
            return payor;
        }
    }
}