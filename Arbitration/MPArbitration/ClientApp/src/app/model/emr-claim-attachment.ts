export class BaseAttachment {
    public id = 0;
    public blobLink = '';
    public blobName = '';
    public createdBy = '';
    public createdOn:Date|undefined;
    public docType = '';
    public isDeleted = false;
    public updatedBy = '';
    public updatedOn:Date|undefined;

    constructor(obj?:any){
        if(!obj)
            return;

        Object.assign(this,JSON.parse(JSON.stringify(obj)));
        if(this.createdOn)
            this.createdOn = new Date(obj.createdOn);
    
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);

    }
}

export class AuthorityDisputeAttachment extends BaseAttachment{
    public authorityDisputeId = 0;
    constructor(obj?:any){
        super(obj);
        if(!obj)
            return;
        this.authorityDisputeId = obj.authorityDisputeId ?? 0;
    }
}

export class EMRClaimAttachment extends BaseAttachment{
    public arbitrationCaseId = 0;
    constructor(obj?:any){
        super(obj);
        if(!obj)
            return;
        this.arbitrationCaseId = obj.arbitrationCaseId ?? 0;
    }
}
