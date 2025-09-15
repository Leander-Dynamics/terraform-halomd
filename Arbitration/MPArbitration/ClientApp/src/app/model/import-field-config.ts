export enum ImportFieldAction {
    Always,
    Ignore,
    OnlyWhenEmpty,
    NeverWithEmpty  // never overwrite a field with an empty value
}

export class ImportFieldConfig {
    public id = 0;
    public action: ImportFieldAction = ImportFieldAction.Always;
    public canBeEmpty = false;
    public isActive = true;
    public isBoolean = false;
    public isDate = false;
    public isNumeric = false;
    public isRequired = true;
    public isTracking = false;
    public source = '';
    public sourceFieldname = '';
    public targetFieldname = '';
    public updatedBy = '';
    public updatedOn: Date|undefined;

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        try {
            if(this.updatedOn)
                this.updatedOn = new Date(obj.updatedOn);
        } catch(err) {
            console.error('Unable to properly convert a date during ArbitrationCase construction');
            console.error(err);
        }
    }
}