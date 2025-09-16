import { Payor } from "./payor";

export class PayorGroupResponse {
    public message = '';
    public itemsAdded = 0;
    public itemsSkipped = 0;
    public itemsUpdated = 0;
    public payorsSkipped:Array<string> = [];

    constructor(obj?:any) {
        if(!obj)
            return;
        this.message = obj.message ?? '';
        this.message = this.message.replaceAll('\n','<br/>');
    }
}
