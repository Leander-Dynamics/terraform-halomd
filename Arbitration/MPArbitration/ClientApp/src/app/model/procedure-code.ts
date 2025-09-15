export class ProcedureCode {
    id = 0;
    code = '';
    codeType = '';
    description = '';
    effectiveDate:Date|undefined;
    group = '';
    updatedBy = '';
    updatedOn:Date|undefined;

    constructor(obj?:any){
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));

        if(this.effectiveDate)
            this.effectiveDate = new Date(obj.effectiveDate);
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    }
}