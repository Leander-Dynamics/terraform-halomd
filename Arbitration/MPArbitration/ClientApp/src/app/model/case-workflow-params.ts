export enum CaseWorkflowAction {
    AssignCustomer = 3,
    AssignUser = 2,
    MarkRead = 0,
    MarkUnread = 1,
    NSARequestSentToPayor = 4
}

export class CaseWorkflowParams {
    public caseId: number = 0;
    public customerId:number = 0;
    public action:CaseWorkflowAction | undefined;
    public assignToId: number = 0;
}