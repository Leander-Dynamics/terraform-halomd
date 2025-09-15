export class AppHealth {
    missingCharges = 0;
    missingCustomer = 0;
    missingDOB = 0;
    missingEOBDate = 0;
    missingEntity = 0;
    missingEntityActive = 0;
    missingFirstResponseDate = 0;
    missingPatientName = 0;
    missingPayorClaimNumber = 0;
    missingPayorClaimNumberActive = 0;
    missingProvider = 0;
    missingProviderActive = 0;
    missingReceivedFromCustomer = 0;
    missingService = 0;
    missingServiceActive = 0;
}

export class AppHealthDetail
{
    public id = 0;
    public authority = '';
    public authorityStatus = '';
    public createdOn:Date|null|undefined;
    public customer = '';
    public DOB:Date | null | undefined;
    public EOBDate:Date | null | undefined;
    public entity = '';
    public entityNPI = '';
    public firstResponseDate:Date | null | undefined;
    public NSAStatus = '';
    public payor = '';
    public patientName = '';
    public payorClaimNumber = '';
    public providerName = '';
    public providerNPI = '';
    public receivedFromCustomer:Date | null | undefined;
    public service = '';
    public serviceDate:Date | null | undefined;
    public serviceLine = '';

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.createdOn) 
            this.createdOn = new Date(obj.createdOn);
        if(this.DOB) 
            this.DOB = new Date(obj.DOB);
        if(this.EOBDate) 
            this.EOBDate = new Date(obj.EOBDate);
        if(this.firstResponseDate)
            this.firstResponseDate = new Date(obj.firstResponseDate);
        if(this.receivedFromCustomer)
            this.receivedFromCustomer = new Date(obj.receivedFromCustomer);
        if(this.serviceDate)
            this.serviceDate = new Date(obj.serviceDate);
    }
}