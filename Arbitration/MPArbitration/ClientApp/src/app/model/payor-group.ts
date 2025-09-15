export enum PlanType {
    FullyInsured = 0,
    SelfFunded = 1,
    SelfFundedOptIn = 2
}

export class PayorGroup {
    public id = 0;
    public groupName = '';
    public groupNumber = '';
    public isNSAIneligible = false;
    public isStateIneligible = false;
    public payorId = 0;
    public planType:PlanType = PlanType.FullyInsured;
    updatedBy = '';
    updatedOn:Date|undefined;
    
    constructor(obj?:any) {
        if(!obj)
            return;
        
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(!this.planType)
            this.planType = PlanType.FullyInsured;
            
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    } 
}
