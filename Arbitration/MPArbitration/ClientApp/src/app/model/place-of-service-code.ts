export class PlaceOfServiceCode {
    public id = 0;
    public codeNumber = '';
    public description = '';
    public effectiveDate:Date|undefined;
    public name = '';

    constructor(obj?:any){
        if(!obj)
            return;

        Object.assign(this,JSON.parse(JSON.stringify(obj)));
        if(this.effectiveDate)
            this.effectiveDate = new Date(obj.effectiveDate);
    }
}