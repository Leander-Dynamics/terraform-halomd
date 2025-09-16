export class TDIRequestDetails
{
    public id = 0;
    public action = '';
    public additionalPaidAmount = 0;
    public arbitrationDate: Date|undefined;
    public arbitrationDecisionDate: Date|undefined;
    public arbitratorReportSubmissionDate: Date|undefined;
    public arbitrator1 = '';
    public arbitrator2 = '';
    public arbitrator3 = '';
    public arbitrator4 = '';
    public arbitrator5 = '';
    public assignmentDeadlineDate: Date|undefined;
    public batchUploadDate:Date|undefined;
    public payorClaimNumber = '';
    public createdBy = '';
    public daysOpen = 0;
    public disputedAmount = 0;
    public entityNPI = '';
    public entity = '';
    public estimatedDisputedAmount = 0;
    public healthPlanName = '';
    public history = '';
    public ineligibilityReason = '';
    public informalTeleconferenceDate:Date|undefined;
    public methodOfPayment = '';
    public originalBilledAmount = 0;
    public partiesAwardNotificationDate: Date|undefined;
    public patientName = '';
    public patientShareAmount = 0;
    public paymentMadeDate:Date|undefined;
    public paymentReferenceNumber = '';
    public payorFinalOfferAmount = 0;
    public payorResolutionRequestReceivedDate:Date|undefined;
    public planPaidAmount = 0;
    public policyType = '';
    public providerName = '';
    public providerFinalOfferAmount = 0;
    public providerNPI = '';
    public providerPaidDate: Date|undefined;
    public providerType = '';
    public reasonableAmount = 0;
    public requestDate:Date|undefined;
    public requestId = 0;
    public requestType = '';
    public resolutionDeadlineDate:Date|undefined;
    public serviceDate:Date|undefined;
    public status = '';
    public submittedBy = '';
    public totalSettlementAmount = 0;
    public userId = '';
    public wasIneligibilityDenied:Date|undefined;
    public wasDisputeSettledOutsideOfArbitration = false;        
    public wasDisputeSettledWithTeleconference = false;

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        try {
            if(this.arbitrationDate)
                this.arbitrationDate = new Date(obj.arbitrationDate); 

            if(this.arbitrationDecisionDate)
                this.arbitrationDecisionDate = new Date(obj.arbitrationDecisionDate); 

            if(this.assignmentDeadlineDate)
                this.assignmentDeadlineDate = new Date(obj.assignmentDeadlineDate);

            if(this.batchUploadDate)
                this.batchUploadDate = new Date(obj.batchUploadDate); 
            if(this.informalTeleconferenceDate)
                this.informalTeleconferenceDate = new Date(obj.informalTeleconferenceDate); 
            if(this.partiesAwardNotificationDate)
                this.partiesAwardNotificationDate = new Date(obj.partiesAwardNotificationDate); 
            if(this.paymentMadeDate)
                this.paymentMadeDate = new Date(obj.paymentMadeDate); 
            if(this.payorResolutionRequestReceivedDate)
                this.payorResolutionRequestReceivedDate = new Date(obj.payorResolutionRequestReceivedDate);
            if(this.providerPaidDate)
                this.providerPaidDate = new Date(obj.providerPaidDate); 
            if(this.resolutionDeadlineDate)
                this.resolutionDeadlineDate = new Date(obj.resolutionDeadlineDate); 
            if(this.serviceDate)
                this.serviceDate = new Date(obj.serviceDate);
            if(this.wasIneligibilityDenied)
                this.wasIneligibilityDenied = new Date(obj.wasIneligibilityDenied); 
        }
        catch(err) {
            console.error('Error constructing TDIRequestDetails:');
            console.error(err);
        }
    }
}
