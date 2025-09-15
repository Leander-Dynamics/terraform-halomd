export class CaseSettlementCPT {
    public id = 0;
    public caseSettlementId = 0;
    public claimCPTId = 0;
    public isDeleted = false;
    public perUnitAwardAmount = 0;
    public units = 0;
    public updatedBy = '';
    public updatedOn: Date|undefined;

    constructor(obj?:any) {
        if(!obj)
            return;

        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    }
}

export class CaseSettlementCPTVM extends CaseSettlementCPT {
    public arbitrationCaseId = 0;
    public cptCode = '';
    public description = '';
    public isSettled = false;
    public payorClaimNumber = '';

    constructor(obj?:any) {
        super(obj);
        if(!obj)
            return;
        
        this.arbitrationCaseId = obj.arbitrationCaseId ?? 0;
        this.cptCode = obj.cptCode ?? '';
        this.description = obj.description ?? '';
        this.isSettled = !!obj.isSettled;
        this.payorClaimNumber = obj.payorClaimNumber ?? '';
    }
}