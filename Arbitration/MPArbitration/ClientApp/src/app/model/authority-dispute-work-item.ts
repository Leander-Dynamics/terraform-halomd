import { LogLevel } from "@azure/msal-browser";
import { loggerCallback } from "../app.module";
import { AuthorityDisputeNote } from "./note";
import { WorkQueueName } from "./work-queue-name-enum";

export class AuthorityDisputeWorkItem
{
    assignedUser = '';
    disputeId = 0;
    note:AuthorityDisputeNote|undefined;
    workQueue:WorkQueueName = WorkQueueName.None;

    constructor(obj?:any) {
        if(!obj)
            return;

        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        try {
            if(!!this.note) {
                this.note = new AuthorityDisputeNote(this.note);
            }
        } catch(err) {
            loggerCallback(LogLevel.Error, 'Unable to instantiate Entity');
            console.error(err);
        }
    }
}