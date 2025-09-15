export class OfferHistory {
    public arbitrationCaseId = 0;
    public caseSettlementId = 0;
    public id = 0;
    public notes = '';
    public offerAmount = 0;
    /** How the offer came in e.g. Email, Fax, Phone, Text, Other */
    public offerSource = ''; 
    /** Is this a Payor or Provider offer */
    public offerType = '';
    public updatedBy = '';
    public updatedOn: Date|undefined;
    public wasOfferAccepted = false;

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn); 
    }
}