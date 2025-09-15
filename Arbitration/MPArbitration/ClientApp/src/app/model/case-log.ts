export class CaseLog {
    public arbitrationCaseId = 0;
    public id = 0;
    public action = '';
    public details = '';
    public createdBy = '';
    public createdOn: Date|undefined;

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.createdOn)
            this.createdOn = new Date(obj.createdOn);
    }
}