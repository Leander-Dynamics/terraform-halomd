import { Entity } from "./entity";
import { ICreatedOn } from "./icreated-on";

export class Customer implements ICreatedOn {
    public stats = '';
    public createdBy = '';
    public createdOn:Date|undefined;
    public defaultAuthority = '';
    public entities:Entity[] = [];
    public EHRSystem = '';
    public id = 0;
    public isActive = true;
    public JSON = '{}';
    public name = '';
    public updatedBy = '';
    public updatedOn:Date|undefined;
    public arbitCasesCount: number = 0;
    public get NSAReplyTo() {
        let obj:any|undefined;
        try {
            obj = JSON.parse(this.JSON);
        } catch {
            obj = {};
        }
        if(!obj.NSAReplyTo)
            obj.NSAReplyTo='';
        return obj.NSAReplyTo;
    }

    public set NSAReplyTo(v:string) {
        let obj:any|undefined;
        try {
            obj = JSON.parse(this.JSON);
        } catch {
            obj = {};
        }
        obj.NSAReplyTo = v;
        this.JSON=JSON.stringify(obj);
    }

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.createdOn) 
            this.createdOn = new Date(obj.createdOn);
        if(this.updatedOn) 
            this.updatedOn = new Date(obj.updatedOn);
        if(this.entities.length) {
            const cc:Entity[] = [];
            this.entities.forEach(d => cc.push(new Entity(d)));
            this.entities = cc;
        }
    }
}
