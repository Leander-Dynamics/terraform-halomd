export class AuthorityPayorGroupExclusion {
    public id = 0;
    public authorityId = 0;
    public groupNumber = '';
    public isNSAIneligible = false;
    public isStateIneligible = false;
    public payorId = 0;
    public updatedBy = '';
    public updatedOn: Date|undefined;

    constructor(obj?:any){
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    }
}

export class AuthorityPayorGroupExclusionVM extends AuthorityPayorGroupExclusion implements IPayorName {
    public payorName = ''; 

    constructor(obj?:any) {
        super(obj);
        
        this.payorName = obj.payorName ?? '';
    }
}

export interface IPayorName {
    payorName:string;
}