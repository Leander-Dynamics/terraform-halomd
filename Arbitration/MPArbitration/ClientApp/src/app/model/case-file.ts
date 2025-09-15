// TODO: Refactor CaseFile to AppBLOB and/or maybe replace with BaseAttachment / AuthorityDisputeAttachment / EHRClaimAttachment 
export class CaseFile
{
    public blobName = '';
    public createdOn:Date|undefined;
    public tags: {[key:string]: string} | undefined;

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(obj.createdOn)
            this.createdOn = new Date(obj.createdOn);
    }
}

export class CaseFileVM {
  public blobName = '';
  public createdOn:Date|undefined;
  public arbitrationCaseId = 0;
  public AuthorityDisputeId = 0;
  public AuthorityCaseId = '';
  public DocumentType = '';
  public EHRNumber = '';
  public Id = '';
  public UpdatedBy = '';

    // aliases to support EMRClaimAttachment use case in the widgets
    public get docType() {
        return this.DocumentType;
    }
    public set docType(value:string) {
        this.DocumentType = value;
    }
    public get updatedBy() {
        return this.UpdatedBy;
    }
    public set updatedBy(value:string) {
        this.UpdatedBy = value;
    }

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(obj.createdOn)
            this.createdOn = new Date(obj.createdOn);
    }
}
