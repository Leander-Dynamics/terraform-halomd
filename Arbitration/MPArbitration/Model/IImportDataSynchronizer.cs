using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace MPArbitration.Model
{
    // Interface allows the dependency injector (service locator) to provide controllers an instance 
    public interface IImportDataSynchronizer
    {
        void ImportIDRDisputeDetailsAsync(IEnumerable<DisputeCPT> disputeCPT, AppUser currentUser, JobQueueItem? CurrentJob);
        Task<string> ArchiveCaseAsync(ArbitrationCase orig, AppUser user, Authority? au = null, bool resetOrig = true, bool saveInstantly = false);
        Task<ArchiveCaseResult> ArchiveIfNecessaryAsync(IAuthorityCase newArbCase, ArbitrationCase orig, AppUser runAs);
        List<Authority> Authorities { get; set; }
        List<Customer> Customers { get; set; }
        List<Payor> Payors { get; set; }
        List<ProcedureCode> ProcedureCodes { get; set; }
        Task BatchQueueNotificationsAsync(IEnumerable<Notification> Notifications, AppUser User, string FullUserName);
        Task EnsureAuthorities();
        Task EnsureCalculatorVariables();
        Task EnsureCustomers();
        Task EnsurePayors(bool ExcludeJSON = true);
        Task EnsureProcedureCodes();
//        void SyncAuthorityImportToCasesNonTX(Authority authority, JobQueueItem? job);
        void SyncTDIsToCases(int authorityId, List<TDIRequestDetails> TDIRequests, JobQueueItem? job);
        void ImportDisputeDetailsAsync(IEnumerable<AuthorityDisputeDetailsCSV> Records, AppUser CurrentUser, JobQueueItem? CurrentJob);
        void ImportDisputeFeesAsync(IEnumerable<AuthorityDisputeFeeCSV> Records, AppUser CurrentUser, JobQueueItem? CurrentJob);
        void ImportDisputeHeadersAsync(IEnumerable<AuthorityDisputeCSV> records, AppUser runAs, JobQueueItem? job);
        void ImportDisputeNotesAsync(IEnumerable<AuthorityDisputeNoteCSV> HeaderRecords, AppUser CurrentUser, JobQueueItem? CurrentJob);
        void ImportEHR(IEnumerable<string> upload, EHRRecordType recordType, AppUser runAs, JobQueueItem? job);
        void ImportAuthorityCases(Authority authority, IEnumerable<string> upload, AppUser initiator, JobQueueItem? job);
        void ImportBenchmarks(int benchmarkId, string username, string upload, JobQueueItem? job);
        void RecalculateAuthorityDates(DbContextOptions<ArbitrationDbContext> contextOptions, int jobId, AppUser user, Authority nsa, bool activeOnly);
        Task<string> SaveUploadLog(string docType, string updatedBy, DateTime updatedOn, string log);
        Task<string> ValidateArbitrationCase(ArbitrationCase caseRecord, bool skipDOBCheck, Authority nsa, Authority? au, bool isUpdating, AppUser? caller, bool calledByImport = true);
    }
}
