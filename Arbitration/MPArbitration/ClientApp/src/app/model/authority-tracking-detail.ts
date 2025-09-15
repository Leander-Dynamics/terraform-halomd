export enum AuthorityTrackingDetailScope
{
    All = 0,
    ArbitrationCase = 1,
    AuthorityDispute = 2
}

export class AuthorityTrackingDetail {
    public id = 0;
    public authorityId = 0;
    public displayColumn = '';
    public helpText = '';
    public isDeleted = false;
    public isHidden = false;
    public mapToCaseField = '';
    public order = 0;
    public referenceFieldName = '';
    public scope = AuthorityTrackingDetailScope.All;
    public trackingLabel = '';
    public trackingFieldName = '';
    public trackingFieldType = '';
    public unitsFromReference = 0;
    public unitsType = '';
    public unlockForStatuses = '';
    public updatedBy = '';
    public updatedOn: Date|undefined;

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
        if(this.isDeleted)
            console.error('AuthorityTrackingDetail.IsDeleted should never be true!');
        if(!AuthorityTrackingDetailScope[this.scope])
            this.scope = AuthorityTrackingDetailScope.All;
        
    }
}