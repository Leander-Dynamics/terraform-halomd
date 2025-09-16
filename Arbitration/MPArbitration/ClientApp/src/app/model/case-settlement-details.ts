import { UtilService } from "../services/util.service";

export class CaseSettlementDetails {
    
        public id = 0;
        //public arbitrationCaseId = 0;
        public additionalPaidAmount = 0;  // amount paid as an award to the winner of the decision
        //public arbitrationDecisionDate: Date|undefined;
        //public arbitratorReportSubmissionDate: Date|undefined;
        //public authorityId = 0;
        public authorityCaseId = '';
        public authorityKey = ''; // VM
        public createdBy = '';
        public createdOn: Date|undefined;
        public isDeleted = false;
        public JSON = '{}'; // Room for other details
        public methodOfPayment = '';
        //public partiesAwardNotificationDate: Date|undefined;
        public paymentMadeDate: Date|undefined;
        public paymentReferenceNumber = '';
        public updatedBy = '';
        public updatedOn: Date|undefined;
        //public reasonableAmount = 0;
        //public totalSettlementAmount = 0;
        //public wasSettledAtArbitration = false;
        //public wasPayorPaymentReceived = false;
        //public wasPayorPaymentTimely = false;
        //public wasProviderPaymentReceived = false;
        //public wasProviderPaymentTimely = false;
        //public winner = ''; // i.e. TDI's Final Offer Closest To Reasonable


        public get paymentMadeDateForPicker() {
            return !!this.paymentMadeDate ? this.paymentMadeDate.toLocaleDateString() : undefined;
        }
        public set paymentMadeDateForPicker(value:any){
            if(!value)
                this.paymentMadeDate = undefined;
            if(UtilService.IsValidUSDate(value))
                this.paymentMadeDate = new Date(value);
        }

        constructor(obj?:any) {
            if(!obj)
                return;
            
            Object.assign(this, JSON.parse(JSON.stringify(obj)));

            try {
                if(this.createdOn)
                    this.createdOn = new Date(obj.createdOn);
                if(this.updatedOn)
                    this.updatedOn = new Date(obj.updatedOn);
                if(this.paymentMadeDate)
                    this.paymentMadeDate = new Date(obj.paymentMadeDate);
            }
            catch(err){
                console.error(err);
            }
        }

}
