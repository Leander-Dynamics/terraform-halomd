import { UtilService } from "../services/util.service";
import { CaseSettlementCPT } from "./case-settlement-cpt";
import { CaseSettlementDetails } from "./case-settlement-details";
import { OfferHistory } from "./offer-history";

export class CaseSettlement {
    public id = 0;
    public caseSettlementDetails:Array<CaseSettlementDetails> = [];
    public caseSettlementCPTs:Array<CaseSettlementCPT> = [];

    public arbitrationCaseId = 0;
    public authorityId:number | null = null;
    public authorityCaseId = ''; // i.e. TDI Number

    public arbitrationDecisionDate: Date|undefined;
    public arbitratorReportSubmissionDate: Date|undefined;
    
    public authorityKey = ''; // VM
    public createdBy = '';
    public createdOn: Date|undefined;
    public grossSettlementAmount = 0;

    public isDeleted = false;
    public JSON = '{}'; // Room for other details

    public netSettlementAmount = 0;
    public notes = '';
    public offer:OfferHistory | null = null;

    public partiesAwardNotificationDate: Date|undefined;
    public payorClaimNumber = '';
    public payorId = 0;
    public prevailingParty = ''; // Payor, Provider, Informal (mutual)

    public reasonableAmount = 0;
    public totalSettlementAmount = 0; // per Authority
    public updatedBy = '';
    public updatedOn: Date|undefined;

    // TODO: These really should be in JSON since they are Authority-specific
    public wasSettledAtArbitration = false;
    public wasPayorPaymentReceived = false;
    public wasPayorPaymentTimely = false;
    public wasProviderPaymentReceived = false;
    public wasProviderPaymentTimely = false;

    
    public get arbitrationDecisionDateForPicker() {
        return !!this.arbitrationDecisionDate ? this.arbitrationDecisionDate.toLocaleDateString() : undefined;
    }
    public set dueOnForPicker(value:any){
        if(!value)
            this.arbitrationDecisionDate = undefined;
        if(UtilService.IsValidUSDate(value))
            this.arbitrationDecisionDate = new Date(value);
    }

    public get arbitratorReportSubmissionDateForPicker() {
        return !!this.arbitratorReportSubmissionDate ? this.arbitratorReportSubmissionDate.toLocaleDateString() : undefined;
    }
    public set arbitratorReportSubmissionDateForPicker(value:any){
        if(!value)
            this.arbitratorReportSubmissionDate = undefined;
        if(UtilService.IsValidUSDate(value))
            this.arbitratorReportSubmissionDate = new Date(value);
    }

    public get partiesAwardNotificationDateForPicker() {
        return !!this.partiesAwardNotificationDate ? this.partiesAwardNotificationDate.toLocaleDateString() : undefined;
    }
    public set partiesAwardNotificationDateForPicker(value:any){
        if(!value)
            this.partiesAwardNotificationDate = undefined;
        if(UtilService.IsValidUSDate(value))
            this.partiesAwardNotificationDate = new Date(value);
    }

    constructor(obj?:any) {
        if(!obj)
            return;
        
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        try {
            if(this.arbitrationDecisionDate)
                this.arbitrationDecisionDate = new Date(obj.arbitrationDecisionDate);
            if(this.arbitratorReportSubmissionDate)
                this.arbitratorReportSubmissionDate = new Date(obj.arbitratorReportSubmissionDate);
            if(this.createdOn)
                this.createdOn = new Date(obj.createdOn);
            if(this.partiesAwardNotificationDate)
                this.partiesAwardNotificationDate = new Date(obj.partiesAwardNotificationDate);
            if(this.updatedOn)
                this.updatedOn = new Date(obj.updatedOn);
            if(!!this.offer)
                this.offer = new OfferHistory(this.offer);
        }
        catch(err){
            console.error(err);
        }
    }
}