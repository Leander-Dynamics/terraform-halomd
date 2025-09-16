export class JobQueueItem {
    public id = 0;
    public JSON = '{}';
    updatedBy = '';
    updatedOn:Date|undefined;

    constructor(obj?:any){
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    }
    
    public get jobStatus():JobStatus {
        if(!this.JSON)
            this.JSON='{}';
        const j = JSON.parse(this.JSON);
        return new JobStatus(j);
    }
}

export class JobStatus {
    public jobType='';
    public message = '';
    public lastUpdated:Date|undefined;
    public recordsAdded = 0;
    public recordsError = 0;
    public recordsProcessed = 0;
    public recordsSkipped = 0;
    public recordsUpdated = 0;
    public status = '';
    public startTime:Date|undefined;
    public totalRecords = 0;

    constructor(obj?:any){
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        // GetUTCDate2 is intended for properly-formatted JSON dates incl timezone
        if(this.lastUpdated)
            this.lastUpdated = new Date(obj.lastUpdated);
        if(this.startTime)
            this.startTime = new Date(obj.startTime);
    }
}
