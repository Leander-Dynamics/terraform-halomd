import { NSACaseVM } from "./nsa-case-vm";

export class EntityVM {
    public name = '';
    public NPINumber = '';

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
    }
}

export class EntityCasesVM extends EntityVM {
    public cases:Array<NSACaseVM> = [];
    public  isExpanded = false;
}
