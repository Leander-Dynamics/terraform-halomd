using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MPArbitration.Controllers;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MPArbitration.Model
{
    public class ArbitrationDbContext : DbContext
    {
        protected readonly IConfiguration Configuration;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ArbitrationDbContext(DbContextOptions<ArbitrationDbContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(options)
        {

        }

        public DbSet<AppUser> AppUsers => Set<AppUser>();
        public DbSet<ArbitrationCase> ArbitrationCases { get; set; }
        public DbSet<Arbitrator> Arbitrators { get; set; }
        public DbSet<Authority> Authorities { get; set; }
        public DbSet<AuthorityImportDetails> AuthorityImportDetails { get; set; }
        public DbSet<AuthorityTrackingDetail> AuthorityTrackingDetails { get; set; }
        public DbSet<CalculatorVariable> CalculatorVariables { get; set; }
        public DbSet<CaseArbitrator> CaseArbitrators { get; set; }
        public DbSet<CaseLog> CaseLog { get; set; }
        public DbSet<CaseTracking> CaseTracking { get; set; }
        public DbSet<ClaimCPT> ClaimCPT { get; set; }
        public DbSet<HealthServiceBenchmark> HealthServiceBenchmarks { get; set; }
        public DbSet<Holiday> Holidays => Set<Holiday>();
        public DbSet<ImportFieldConfig> ImportFieldConfigs { get; set; }
        public DbSet<Negotiator> Negotiators { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OfferHistory> OfferHistory { get; set; }
        public DbSet<Payor> Payors { get; set; }
        public DbSet<TDIRequestDetails> TDIRequests { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<BenchmarkDataset> BenchmarkDatasets { get; set; }
        public DbSet<BenchmarkDataItem> BenchmarkDataItems { get; set; }
        public DbSet<CaseBenchmark> CaseBenchmarks { get; set; }
        public DbSet<AuthorityBenchmarkDetails> AuthorityBenchmarkDetails { get; set; }
        public DbSet<ProcedureCode> ProcedureCodes => Set<ProcedureCode>();
        public DbSet<Entity> Entities => Set<Entity>();
        public DbSet<Template> Templates => Set<Template>();
        public DbSet<CaseArchive> CaseArchives => Set<CaseArchive>();
        //public DbSet<PayorAlias> PayorAliases => Set<PayorAlias>();
        public DbSet<PayorAuthorityMap> PayorAuthorityMaps => Set<PayorAuthorityMap>();
        public DbSet<MasterDataException> MasterDataExceptions => Set<MasterDataException>();
        public DbSet<CaseSettlementDetail> CaseSettlementDetails => Set<CaseSettlementDetail>();
        public DbSet<PayorGroup> PayorGroups => Set<PayorGroup>();
        public DbSet<AuthorityPayorGroupExclusion> AuthorityPayorGroupExclusions => Set<AuthorityPayorGroupExclusion>();
        public DbSet<AppSettings> AppSettings => Set<AppSettings>();
        public DbSet<CaseSettlement> CaseSettlements => Set<CaseSettlement>();
        public DbSet<JobQueueItem> JobQueueItems => Set<JobQueueItem>();
        public DbSet<EMRClaimAttachment> EMRClaimAttachments => Set<EMRClaimAttachment>();
        public DbSet<AuthorityFee> AuthorityFees => Set<AuthorityFee>();
        //public DbSet<Fee> Fees => Set<Fee>();
        public DbSet<AuthorityDispute> AuthorityDisputes => Set<AuthorityDispute>();
        public DbSet<ArbitratorFee> ArbitratorFees => Set<ArbitratorFee>();
        public DbSet<AuthorityDisputeFee> AuthorityDisputeFees => Set<AuthorityDisputeFee>();
        public DbSet<AuthorityDisputeCPT> AuthorityDisputeCPTs => Set<AuthorityDisputeCPT>();
        public DbSet<PayorAddress> PayorAddresses => Set<PayorAddress>();
        public DbSet<AuthorityDisputeNote> AuthorityDisputeNotes => Set<AuthorityDisputeNote>();
        public DbSet<AuthorityDisputeAttachment> AuthorityDisputeAttachments => Set<AuthorityDisputeAttachment>();
        public DbSet<AuthorityDisputeLog> AuthorityDisputeLog => Set<AuthorityDisputeLog>();
        public DbSet<PlaceOfServiceCode> PlaceOfServiceCodes => Set<PlaceOfServiceCode>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // convert Status to string and back again instead of using integers as a backing store
            //builder.Entity<ArbitrationCase>()
            //    .Property(e => e.Status)
            //    .HasConversion(v => v.ToString(),
            //                    v => (ArbitrationStatus)Enum.Parse(typeof(ArbitrationStatus), v));

            builder.Entity<AuthorityDispute>()
                .Property(b => b.CreatedOn)
                .HasDefaultValueSql("getdate()"); 
            
            builder.Entity<AuthorityDispute>()
                .Property(b => b.UpdatedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<AuthorityDisputeCPT>()
                .Property(b => b.AddedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<AuthorityDisputeCPT>()
                .Property(b => b.UpdatedOn)
                .HasDefaultValueSql("getdate()");

            // convert DisqualifiedBy to string and back again instead of using integers as a backing store
            //builder.Entity<CaseArbitrator>()
            //    .Property(e => e.DisqualifiedBy)
            //    .HasConversion(v => v.ToString(),
            //                    v => (ArbitratorDisqualification)Enum.Parse(typeof(ArbitratorDisqualification), v));
            
            builder.Entity<ClaimCPT>()
                .Property(b => b.CreatedOn)
                .HasDefaultValueSql("getdate()");

            // convert Action value to string and back again instead of using integers as a backing store
            builder.Entity<ImportFieldConfig>()
                .Property(e => e.Action)
                .HasConversion(v => v.ToString(),
                                v => (ImportFieldAction)Enum.Parse(typeof(ImportFieldAction), v));

            // auditing columns
            builder.Entity<Notification>()
                .Property(b => b.zEditor)
                .HasDefaultValueSql("suser_name()");

            builder.Entity<Notification>()
                .Property(b => b.zEditedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<AuthorityBenchmarkDetails>()
                .Property(b => b.UpdatedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<ArbitrationCase>()
                .Property(b => b.CreatedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<ArbitrationCase>()
                .Property(b => b.zEditor)
                .HasDefaultValueSql("suser_name()");

            builder.Entity<ArbitrationCase>()
                .Property(b => b.zEditedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<AuthorityDisputeAttachment>()
                .Property(b => b.CreatedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<ArbitratorFee>()
                .Property(b => b.CreatedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<ArbitratorFee>()
                .Property(b => b.UpdatedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<AuthorityFee>()
                .Property(b => b.CreatedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<EMRClaimAttachment>()
                .Property(b => b.CreatedOn)
                .HasDefaultValueSql("getdate()");

            // convert NotificationType to string and back again
            builder.Entity<Notification>()
                .Property(e => e.NotificationType)
                .HasConversion(v => v.ToString(),
                                v => (NotificationType)Enum.Parse(typeof(NotificationType), v));

            builder.Entity<AuthorityFee>()
                .Property(b => b.UpdatedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<Template>()
                .Property(b => b.CreatedOn)
                .HasDefaultValueSql("getdate()");

            builder.Entity<CaseArchive>()
                .Property(e => e.AuthorityWorkflowStatus)
                .HasConversion(v => v.ToString(),
                                v => (ArbitrationStatus)Enum.Parse(typeof(ArbitrationStatus), v));
            
            builder.Entity<MasterDataException>()
                .Property(e => e.UpdatedOn)
                .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            builder.Entity<MasterDataException>()
                .Property(e => e.CreatedOn)
                .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            builder.Entity<EMRClaimAttachment>()
                .Property(e => e.CreatedOn)
                .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            builder.Entity<EMRClaimAttachment>()
                .Property(e => e.UpdatedOn)
                .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            builder.Entity<PayorGroup>()
                .Property(e => e.PlanType)
                .HasConversion(v => v.ToString(),
                                v => (PlanType)Enum.Parse(typeof(PlanType), v));

            #region UTC Date conversion in and out of the datastore
            var UtcDtConverter = new ValueConverter<DateTime?,  DateTime?>(
                from => !from.HasValue ? from : (from.Value.Kind != DateTimeKind.Unspecified ? from.Value.ToUniversalTime() : DateTime.SpecifyKind(from.Value, DateTimeKind.Utc)),
                to => to.HasValue ? DateTime.SpecifyKind(to.Value, DateTimeKind.Utc) : to
            );

            builder.Entity<AppSettings>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Arbitrator>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitratorFee>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitratorFee>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Authority>()
                .Property(e => e.ActiveAsOf)
                .HasConversion(UtcDtConverter);

            builder.Entity<Authority>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityBenchmarkDetails>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityFee>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityFee>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityImportDetails>()
                .Property(e => e.BatchUploadDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDispute>()
                .Property(e => e.ArbitratorSelectedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDispute>()
                .Property(e => e.BriefApprovedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDispute>()
                .Property(e => e.BriefPreparationCompletedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDispute>()
                .Property(e => e.BriefWriterCompletedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDispute>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDispute>()
                .Property(e => e.SubmissionDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDispute>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeCPT>()
                .Property(e => e.AddedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeCPT>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityPayorGroupExclusion>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            #region Arbitration Case Date Conversions

            builder.Entity<ArbitrationCase>()
                .Property(e => e.ArbitrationBriefDueDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.ArbitrationDeadlineDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.ArbitratorPaymentDeadlineDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.AssignmentDeadlineDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.DOB)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.EOBDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.FirstAppealDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.FirstResponseDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.InformalTeleconferenceDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.PaymentMadeDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.PayorResolutionRequestReceivedDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.ProviderPaidDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.ReceivedFromCustomer)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.RequestDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.ResolutionDeadlineDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.ServiceDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ArbitrationCase>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            #endregion

            builder.Entity<CaseArbitrator>()
                .Property(e => e.AssignedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseArbitrator>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlement>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlement>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlement>()
                .Property(e => e.ArbitrationDecisionDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlement>()
                .Property(e => e.ArbitratorReportSubmissionDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlement>()
                .Property(e => e.PartiesAwardNotificationDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlementDetail>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlementDetail>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlementDetail>()
                .Property(e => e.ArbitrationDecisionDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlementDetail>()
                .Property(e => e.ArbitratorReportSubmissionDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlementDetail>()
                .Property(e => e.PartiesAwardNotificationDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlementDetail>()
                .Property(e => e.PaymentMadeDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseSettlementCPT>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<ClaimCPT>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<ClaimCPT>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeFee>()
                .Property(e => e.DueOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeFee>()
                .Property(e => e.PaidOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeFee>()
                .Property(e => e.PaymentRequestedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeFee>()
                .Property(e => e.RefundedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeFee>()
                .Property(e => e.RefundRequestedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeFee>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityTrackingDetail>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AppUser>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<BenchmarkDataItem>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<BenchmarkDataset>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<CalculatorVariable>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseArchive>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseBenchmark>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseLog>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<CaseTracking>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Customer>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Customer>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Entity>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Holiday>()
                .Property(e => e.EndDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<Holiday>()
                .Property(e => e.StartDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ImportFieldConfig>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<JobQueueItem>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Negotiator>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Note>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Notification>()
                .Property(e => e.ApprovedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Notification>()
                .Property(e => e.SentOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Notification>()
                .Property(e => e.SubmittedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Notification>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Notification>()
                .Property(e => e.zEditedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<OfferHistory>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Payor>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<PayorAuthorityMap>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<PayorGroup>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<ProcedureCode>()
                .Property(e => e.EffectiveDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<ProcedureCode>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Template>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<Template>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<TDIRequestDetails>()
                .Property(e => e.BatchUploadDate)
                .HasConversion(UtcDtConverter);

            builder.Entity<PayorAddress>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeNote>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeAttachment>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeAttachment>()
                .Property(e => e.UpdatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<AuthorityDisputeLog>()
                .Property(e => e.CreatedOn)
                .HasConversion(UtcDtConverter);

            builder.Entity<PlaceOfServiceCode>()
                .Property(e => e.EffectiveDate)
                .HasConversion(UtcDtConverter);
            
            #endregion

            #region Set up some indexes EF can't seem to figure out
            #endregion

            #region Set up some relationships EF can't seem to figure out

            #endregion

            #region Create a special-purpose utility model
            builder.Entity<CaseUtility>(entity =>
            {
                entity.HasNoKey().ToView("nothing");
            });
            #endregion
        }

    }
}
