import { LogLevel } from "@azure/msal-browser";
import { loggerCallback } from "../app.module";

export class Entity {
    public id = 0;
    //public customer:Customer|undefined;
    public customerId = 0;
    public address = '';
    public city='';
    public JSON = '{}';
    public name = '';
    public ownerName = '';
    public ownerTaxId = '';
    public NPINumber = '';
    public state = '';
    public updatedBy = '';
    public updatedOn:Date|undefined;
    public zipCode = '';

    get isValid() {
        return !!this.name && !!this.NPINumber;
    }

    constructor(obj?:any) {
        if(!obj)
            return;
        
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        try {
            if(this.updatedOn)
                this.updatedOn = new Date(obj.updatedOn);
        } catch(err) {
            loggerCallback(LogLevel.Error, 'Unable to instantiate Entity');
            console.error(err);
        }
    }
}