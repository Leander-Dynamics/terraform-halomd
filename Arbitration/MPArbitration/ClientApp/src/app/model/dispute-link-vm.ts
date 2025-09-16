import { CMSCaseStatus } from "./arbitration-status-enum";

/** Simple structure for displaying links between ClaimCPTs and Disputes that reference them */
export class DisputeLinkVM {
    public authorityDisputeId = 0; // link using ../batch/${authorityDisputeId}
    public authorityCaseId = ''; // aka Dispute Number or RequestID
    public authorityId = 0;
    public authorityKey = '';
    public authorityName = '';
    public claimCPTId = 0;
    public cptCode = '';
    public disputeStatus:CMSCaseStatus = CMSCaseStatus.New; // aka WorkflowStatus

    public get IsActive() {
        return this.disputeStatus === CMSCaseStatus.ActiveArbitrationBriefCreated ||
               this.disputeStatus === CMSCaseStatus.ActiveArbitrationBriefNeeded ||
               this.disputeStatus === CMSCaseStatus.ActiveArbitrationBriefSubmitted ||
               this.disputeStatus === CMSCaseStatus.InformalInProgress ||
               this.disputeStatus === CMSCaseStatus.MissingInformation ||
               this.disputeStatus === CMSCaseStatus.New ||
               this.disputeStatus === CMSCaseStatus.Open ||
               this.disputeStatus === CMSCaseStatus.PendingArbitration;
    }

    public get uri() {
        return `/batch/${this.authorityDisputeId}`;
    }

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
    }
}
