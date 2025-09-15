import { ClaimCPT } from "./claim-cpt";
import { IModifier } from "./imodifier";
// This is a viewmodel that flattens the properties for multiple classes
// into something easy to display in a datatable. 
export class VMArbitrationCPT extends ClaimCPT implements IModifier {
    public id = 0;
    public _80thPercentileCharges = 0;
    public allowableBasePerUnit = 0;
    public arbitrationDeadline: Date|undefined;
    public arbitrationFee = 0;
    public authorityCaseId = '';

    public caseSettlementIDs:Array<number> = [];
    public chargesCap = 0;
    public description = '';
    public extendedAllowed = 0;
    public extendedAdjustedAllowed = 0;
    
    public fhAllowedAmount = 0;
    public fhExtendedAmount = 0;
    public fhExtendedAdjustedAmount = 0;
    public fhProviderFinalOfferNotToExceed = 0;
    public fhProviderFinalOffer = 0;
    public fh80thPercentileAllowed = 0;
    public fh80thPercentileExtended = 0;

    public hardCodedCap = 0;
    public include = false;
    public isSettled = false;
    public locationGeoZip = '';
    public offerSpread = 0;
    public payorOffer = 0;
    public updatedBy = '';
    public updatedOn: Date|undefined;

    constructor(obj?:any) {
        super(obj);
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.arbitrationDeadline)
            this.arbitrationDeadline = new Date(obj.arbitrationDeadline); 
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn); 
    }
}
