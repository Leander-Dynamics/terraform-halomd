export class PayorAuthorityMap {
    public id = 0;
    public authorityId = 0;
    public payorAliasId = 0;
    public updatedBy = '';
    public updatedOn: Date|undefined;

    constructor(obj?: any) {
        if(!obj)
            return;
        Object.assign(this,JSON.parse(JSON.stringify(obj)));

        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);

    }
}