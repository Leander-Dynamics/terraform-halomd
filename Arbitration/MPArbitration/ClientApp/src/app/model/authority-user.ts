export class AuthorityUser {
    public customerName = '';
    public displayName = '';
    public userId = '';

    
    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
    }
}
