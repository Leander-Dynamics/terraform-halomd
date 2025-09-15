import { NotificationType } from "./notification-type-enum";

export class DocumentTemplate {
    html = '';
    name = '';
    notificationType = NotificationType.Unknown;
    tags = '';
    
    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(typeof obj.notificationType === 'string') {
            const t = obj.notificationType as keyof typeof NotificationType;
            this.notificationType = NotificationType[t] ?? NotificationType.Unknown;
        }
    }
}