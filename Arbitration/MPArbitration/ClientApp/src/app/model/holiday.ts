export class Holiday {
    public id = 0;

    public country = '';
    public endDate: Date|undefined;
    public name = '';
    public region ='';
    public startDate: Date|undefined;

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.endDate)
            this.endDate = new Date(obj.endDate); 
        if(this.startDate)
            this.startDate = new Date(obj.startDate);
    }
}