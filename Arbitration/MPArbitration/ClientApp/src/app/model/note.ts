import { IModifier } from "./imodifier";

export class BaseNote implements IModifier {
    public id = 0;
    public details = '';
    public updatedBy = '';
    public updatedOn:Date|undefined = undefined;

    constructor(obj?:any){
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    }
}

export class AuthorityDisputeNote extends BaseNote
{
    public authorityDisputeId = 0;
    
    constructor(obj?:any) {
        super(obj);
        if(!obj)
            return;
        this.authorityDisputeId = obj.authorityDisputeId ?? 0;
    }
}

export class Note extends BaseNote
{
    public arbitrationCaseId = 0;
    
    constructor(obj?:any) {
        super(obj);
        if(!obj)
            return;
        this.arbitrationCaseId = obj.arbitrationCaseId ?? 0;
    }
}
