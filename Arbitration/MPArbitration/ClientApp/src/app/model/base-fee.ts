import { DeadlineType } from "./deadline-type-enum";
import { FeeType } from "./fee-type-enum";

export class BaseFee {
    public id = 0;
    public createdBy = '';
    public createdOn: Date|undefined;
    public dueDaysAfterColumnName = 0;
    public dueDayType:DeadlineType = DeadlineType.CalendarDays;
    public feeAmount = 0;
    public feeName = '';
    public feeType:FeeType = FeeType.Administrative;
    public isActive = true;
    public isRefundable = false;
    public isRequired = false;
    public referenceColumnName = '';
    public sizeMin = 0;
    public sizeMax = 0;
    public updatedBy = '';
    public updatedOn: Date|undefined;

    constructor(obj?:any){
        if(!obj)
            return;

        Object.assign(this, JSON.parse(JSON.stringify(obj)));

        if(typeof obj.dueDayType === 'string') {
            const t = obj.dueDayType as keyof typeof DeadlineType;
            this.dueDayType = DeadlineType[t] ?? DeadlineType.CalendarDays;
        }
        
        if(obj.dueDayType === undefined)
            this.dueDayType = DeadlineType.CalendarDays; // this is the safest default

        if(typeof obj.feeType === 'string') {
            const t = obj.feeType as keyof typeof FeeType;
            this.feeType = FeeType[t] ?? FeeType.Administrative;
        }
        
        if(obj.feeType === undefined)
            this.feeType = FeeType.Administrative; // Administrative won't trigger any calculations
    }
}
