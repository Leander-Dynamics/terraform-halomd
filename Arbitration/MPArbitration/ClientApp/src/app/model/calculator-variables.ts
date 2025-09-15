export class CalculatorVariables {
    id = 0;
    arbitrationFee = 0;
    chargesCapDiscount = 0;  // percentage
    createdBy = '';
    createdOn:Date|undefined;
    nsaOfferDiscount = 0;
    nsaOfferBaseValueFieldname = ""; //fh80thPercentileExtendedCharges
    offerCap = 0;  // e.g. 35,000.00
    offerSpread = 0;   // percentage
    serviceLine = '';  // e.g. IOM

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(obj.createdOn)
            this.createdOn = new Date(obj.createdOn);
        if(!this.nsaOfferBaseValueFieldname)
            this.nsaOfferBaseValueFieldname = 'fh80thPercentileExtendedCharges';
    }
}
