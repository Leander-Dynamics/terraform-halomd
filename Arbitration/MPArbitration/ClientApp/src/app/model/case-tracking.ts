export class CaseTracking {
    public id = 0;
    public arbitrationCaseId = 0;
    public trackingValues = ''; // JSON
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
