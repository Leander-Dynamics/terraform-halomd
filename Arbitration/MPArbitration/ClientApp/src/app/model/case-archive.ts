import { CMSCaseStatus } from "./arbitration-status-enum";
import { IArbitrationCase } from "./iarbitration-case";

export class CaseArchive implements IArbitrationCase {
    id = 0;
    arbitrationCaseId = 0;

    authorityId:number|undefined;
    authorityCaseId = '';
    authorityStatus = '';
    authorityWorkflowStatus = CMSCaseStatus.New;
    createdBy = '';
    createdOn:Date|undefined;
    JSON = '{}';

    constructor(obj?:any) {
        if(!obj)
            return;
        
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.createdOn)
            this.createdOn = new Date(obj.createdOn);
    }    
}
