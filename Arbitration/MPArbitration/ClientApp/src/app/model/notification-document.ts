import { NotificationType } from "./notification-type-enum";

export interface INotificationDocument {
    arbitrationCaseId:number;
    html:string;
    JSON:string;
    name:string;
    notificationType:NotificationType;
}

export class NotificationDocument implements INotificationDocument {
    arbitrationCaseId = 0;
    html = '';
    JSON = '{}';
    name = '';
    notificationType = NotificationType.Unknown;

    constructor(obj?:any){
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
    }
}