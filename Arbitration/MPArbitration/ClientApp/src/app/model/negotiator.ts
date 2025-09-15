import { Payor } from "./payor";

export class Negotiator {
    public id = 0;
    public isActive = true;
    public email = '';
    public organization = '';
    public name = '';
    public notes = '';
    public payor:Payor | null = null;
    public payorId = 0;

    public phone = '';
    public updatedBy = '';
    public updatedOn: Date|undefined;

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn); 
        if(this.payor) {
            this.payor.negotiators = [];
            this.payor = new Payor(this.payor);
        }
    }
}