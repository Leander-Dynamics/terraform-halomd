import { INotificationDocument, NotificationDocument } from "./notification-document";
import { NotificationType } from "./notification-type-enum";

export class NotificationDeliveryInfo {
    public deliveredOn:Date|undefined;
    public deliverId = '';
    public deliveryMethod = '';
    public message = '';
    public messageId = '';
    public processedOn:Date|undefined;
    public sender = '';
    public status = '';
    public attachments = new Array<NotificationAttachment>();
    public recipients = new Array<NotificationRecipient>();

    constructor(obj?:any){
        if(!obj)
            return;

        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.attachments.length)
            this.attachments = this.attachments.map(v => new NotificationAttachment(v));
        if(this.recipients.length)
            this.recipients = this.recipients.map(v => new NotificationRecipient(v));
    }
}

export class NotificationAttachment {
    public fileName = '';
    public fileSize = 0;

    constructor(obj?:any){
        if(!obj)
            return;

        Object.assign(this, JSON.parse(JSON.stringify(obj)));
    }
}

export class NotificationRecipient {
    public to_email = '';
    public msg_id = '';
    public clicks_count = 0;
    public last_event_time: Date|undefined;
    public status = '';
    public opens_count = 0;

    constructor(obj?:any){
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
    }
}

export class Notification implements INotificationDocument {
    arbitrationCaseId = 0;
    id = 0;
    approvedBy = '';
    approvedOn:Date|undefined;
    bcc = '';
    cc = '';
    customer = '';
    // html for the email/fax that goes out
    readonly html = ''; // the template is combined with case and variable info on the server side to prevent pushing garbage through the API
    isDeleted = false;
    JSON = '{}';
    notificationType = NotificationType.Unknown;
    payorClaimNumber = '';
    replyTo = '';
    sentOn:Date|undefined;
    status = '';
    submittedBy = '';
    submittedOn:Date|undefined;
    to = '';
    updatedBy = '';
    updatedOn:Date|undefined;

    get name(): string {
        return NotificationType[this.notificationType];
    }

    constructor(obj?:any){
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));

        if(typeof obj.notificationType === 'string') {
            const t = obj.notificationType as keyof typeof NotificationType;
            this.notificationType = NotificationType[t] ?? NotificationType.Unknown;
        }
        if(this.approvedOn)
            this.approvedOn = new Date(obj.sentOn);
        if(this.sentOn)
            this.sentOn = new Date(obj.sentOn);
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    }

    
    public addVariable(name:string,value:string|number|Date) {
        if(!this.JSON)
            this.JSON='{}'; // correct undefined and NULL to prevent error
        const j = JSON.parse(this.JSON);
        j[name]=value;
        this.JSON = JSON.stringify(j);
    }
    
    public deleteVariable(name:string) {
        if(!this.JSON) {
            this.JSON='{}'; // correct undefined and NULL to prevent error
            return;
        }
        const j = JSON.parse(this.JSON);
        delete j[name];
        this.JSON = JSON.stringify(j);
    }

    public get Delivery():NotificationDeliveryInfo {
        if(!this.JSON) {
            this.JSON='{}'; // correct undefined and NULL to prevent error
        }
        const j = JSON.parse(this.JSON);
        return new NotificationDeliveryInfo(j.delivery);
    }

    public get supplements():INotificationDocument[] {
        const j = JSON.parse(this.JSON ?? '{}');
        const t:NotificationDocument[] = j.supplements ?? [];
        const retval = new Array<NotificationDocument>();
        for(const v of t)
            retval.push(new NotificationDocument(v));
        return retval;
    }

    // supplements is readonly for the moment since they're generated on the server
}
