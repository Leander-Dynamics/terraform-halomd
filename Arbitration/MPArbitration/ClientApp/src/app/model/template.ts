export class Template {
    public id = 0;
    public componentType = '';
    public createdOn: Date|undefined;
    public createdBy = '';
    public description = '';
    public html = '';
    public JSON = '{}';
    public name = '';
    public updatedOn: Date|undefined;
    public updatedBy = '';

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
    }

    // Viewmodel properties

}