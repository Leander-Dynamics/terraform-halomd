export enum MasterDataExceptionType
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

export class MasterDataException {
    public id = 0;
    public createdBy = "";
    public createdOn: Date|undefined;
    public data = "";
    public exceptionType: MasterDataExceptionType | undefined;
    public isResolved = false;
    public message = "";
    public updatedBy = "";
    public updatedOn: Date|undefined;

    constructor(obj?:any) {
        if(!obj)
            return;
    
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        
        if(this.createdOn)
            this.createdOn = new Date(obj.createdOn);
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    }

}