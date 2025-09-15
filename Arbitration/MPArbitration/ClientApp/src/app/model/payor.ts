import { DocumentTemplate } from "./document-template";
import { EntityVM } from "./entity-vm";
import { Negotiator } from "./negotiator";
import { NotificationType } from "./notification-type-enum";
import { PayorAddress } from "./payor-address";
import { PayorGroup } from "./payor-group";

export class Payor {
    public id = 0;
    public addresses:PayorAddress[] = [];
    public isActive = true;
    public JSON = '{}';
    public name = '';
    public negotiators:Negotiator[] = [];
    public NSARequestEmail = '';
    public parentId:number | null = null;
    public payorGroups:PayorGroup[] = [];
    
    public sendNSARequests = false;
    
    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.addresses) {
            const n:PayorAddress[] = [];
            this.addresses.forEach(d => n.push(new PayorAddress(d)));
            this.addresses = n;
        }
        if(this.negotiators) {
            const n:Negotiator[] = [];
            this.negotiators.forEach(d => n.push(new Negotiator(d)));
            this.negotiators = n;
        }
    }

    public get excludedEntities():EntityVM[] {
        if(!this.JSON)
            this.JSON='{}'; // prevent error
        const j = JSON.parse(this.JSON);
        let x = j.exclusions;
        if(!x||!Array.isArray(x))
            return new Array<EntityVM>();
        return x; //.map(d => new EntityVM(d));
    }

    public get templates():DocumentTemplate[] {
        const j = JSON.parse(this.JSON ?? '{}');
        const t:DocumentTemplate[] = j.templates ?? [];
        const retval = new Array<DocumentTemplate>();
        for(const v of t)
            retval.push(new DocumentTemplate(v));
        return retval;
    }

    public addExclusion(v:EntityVM){
        if(!v.name||!v.NPINumber)
            return false;
        const x=this.excludedEntities;
        if(!x.find(d=>d.NPINumber===v.NPINumber)){
            x.push(v);
            const j=JSON.parse(this.JSON);
            j.exclusions=x;
            this.JSON=JSON.stringify(j);
            return true;
        }
        return false;
    }

    public removeExclusion(v:EntityVM){
        if(!v.name||!v.NPINumber)
            return false;
        const x=this.excludedEntities;
        const i=x.findIndex(d=>d.NPINumber===v.NPINumber);
        if(i>-1){
            x.splice(i,1);
            const j=JSON.parse(this.JSON);
            j.exclusions=x;
            this.JSON=JSON.stringify(j);
            return true;
        }
        return false;
    }

    public updateTemplate(value:DocumentTemplate) {
        if(value.notificationType === NotificationType.Unknown)
            throw 'Cannot save a DocumentTemplate of type Unknown';
        if(!this.JSON)
            this.JSON='{}'; // prevent error
        const j = JSON.parse(this.JSON);
        const v = this.templates; // new-up the typed objects
        let t = v.find(d=>d.name.toLowerCase()===value.name.toLowerCase()&&d.notificationType===value.notificationType);
        
        if(t)
            v[v.indexOf(t)] = value;
        else
            v.push(value);

        j.templates = v;
        this.JSON = JSON.stringify(j);
    }
}
