using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    public enum MasterDataExceptionType
    {
        AuthorityCaseArchiveBlocked,
        BenchmarkMissingCPTValue,
        CaseSettlementError,
        ClaimMismatchDOB,
        ClaimMismatchServiceDate,
        ClaimMissingAuthority,
        ClaimMissingDOB,
        ClaimMissingNPI,
        ClaimMissingPatientName,
        ClaimMissingServiceDate,
//        ClaimMissingEOBDate,
        ClaimMissingServiceLine,
        DuplicateAuthorityCaseId,
        DuplicateKeyValues,
        DuplicateNSACaseId,
        DuplicatePayorClaimNumber,
        ExcludedEntity,
        LocalNSAClaimMismatch,
        MDMissingAuthority,
        MDMissingCustomer,
        MDMissingCustomerEntity,
        MDMissingPayor,
        MDMissingServiceLine,
        MDMissingTrackingField,
        PayorClaimNumberChangedOnCase,
        PreviouslyArchived,
        SyncTargetPropertyNotFound,
        SyncTypeMismatch,
        Unknown,
        UnknownEntity
    }

    [Index(nameof(ExceptionType),nameof(Data))]
    public class MasterDataException
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; } = null;

        [JsonPropertyName("data")]
        [StringLength(255)]
        public string Data { get; set; } = "";

        [JsonPropertyName("exceptionType")]
        [Column(TypeName = "nvarchar(60)")]
        public MasterDataExceptionType ExceptionType { get; set; }

        [JsonPropertyName("isResolved")]
        public bool IsResolved { get; set; }

        [JsonPropertyName("message")]
        [StringLength(2048)]
        public string Message { get; set; } = "";

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

    }
}
