export class CaseBenchmark {
    public id = 0;
    public extendedValue = 0;
    public formLabel = '';
    public isHidden = false;
    public procedureCode = '';
    public sortOrder = 0;
    public tableLabel = '';
    public value = 0;
    public valueField = '';
    public updatedOn: Date|undefined;
    public updatedBy = '';
    
    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    }
}
