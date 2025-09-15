import { ArbitrationCase } from "./arbitration-case";

export class ArbitrationCaseVM extends ArbitrationCase {
    includeClosed = false;
    includeInactive = false;
    isDisputeSearch = false;

    constructor(obj?:any) {
        super(obj);
        if(!obj)
            return;
        this.includeClosed = !!obj.includeClosed;
        this.includeInactive = !!obj.includeIneligible;
        this.isDisputeSearch = !!obj.isDisputeSearch;
    }
}