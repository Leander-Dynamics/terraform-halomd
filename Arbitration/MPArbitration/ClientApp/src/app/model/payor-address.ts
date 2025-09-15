export class PayorAddress {
    public id = 0;
    public addressLine1 = '';
    public addressLine2 = '';
    public addressType = '';
    public city = '';
    public email = '';
    public name = '';
    public payorId = 0;
    public phone = '';
    public stateCode = '';
    public zipCode = '';
    public updatedBy = '';
    public updatedOn:Date | undefined;

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));

        if(obj.updatedOn) 
            this.updatedOn = new Date(obj.updatedOn);
        
    }
}
