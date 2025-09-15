export class AppSettings {
    id = 0;
    JSON = '{}';
    updatedBy = '';
    updatedOn:Date|undefined;

    constructor(obj?:any) {
        if(!obj)
            return;

        Object.assign(this,JSON.parse(JSON.stringify(obj)));
        
        if(!!obj.updatedOn)
            this.updatedOn = new Date(obj.updatedOn!);
    }

    get stateActionList():Array<string> {
        //return ['Batching','Denial','Duplicate','High Reimbursement','Incorrect Claim Data','NSA','Other Payor is Primary','Out of State Policy','Patient Elected OON Services','Timing'];
        if(this.JSON==='')
            this.JSON = '{}';
        
        try {
            const j = JSON.parse(this.JSON);
            return j.stateActionList ?? [];
        } catch (err){
            console.error('Error parsing AppSettings JSON');
        }
        return [];
    }

    set stateActionList(a:Array<string>) {
        if(this.JSON==='')
            this.JSON = '{}';
        
        try {
            const j = JSON.parse(this.JSON);
            j.stateActionList = a;
            this.JSON = JSON.stringify(j);
        } catch (err){
            console.error('Error parsing AppSettings JSON');
        }
    }
}
