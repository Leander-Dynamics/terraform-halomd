import { EntityCasesVM } from "./entity-vm";

export class PayorEntitiesVM {
    count = 0; 
    isExpanded = false;
    entities:Array<EntityCasesVM> = [];
    name = '';
    NSAEmail = '';

    get isInternalEmail(){
        const test=this.NSAEmail.toLowerCase();
        return test.indexOf('mpowerhealth.')>-1 || test.indexOf('halomd.')>-1 || test.indexOf('acquisitionbilling.') > -1;
    }
}