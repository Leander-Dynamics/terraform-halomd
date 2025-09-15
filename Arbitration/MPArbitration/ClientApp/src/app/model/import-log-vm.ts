import { CaseFile } from "./case-file";

export class ImportLogVM {
    public blobName = '';
    public createdOn:Date|undefined;
    public authority = '';
    public documentType = '';
    public batchUploadDate: string | undefined = '';
    public uploadedBy = '';

    constructor(obj:CaseFile) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(obj.tags && obj.tags['Authority'])
            this.authority = obj.tags['Authority'];
        if(obj.tags && obj.tags['BatchUploadDate']) {
            this.batchUploadDate = new Date(obj.tags['BatchUploadDate']).toLocaleString();
        }
        if(obj.tags && obj.tags['UploadedBy'])
            this.uploadedBy = obj.tags['UploadedBy'];
        if(obj.createdOn)
            this.createdOn = new Date(obj.createdOn);
    }
}