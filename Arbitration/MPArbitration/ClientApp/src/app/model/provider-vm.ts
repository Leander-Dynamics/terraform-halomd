export class ProviderVM {
    public entityNPI = '';
    public providerName = '';
    public providerNPI = '';

    constructor(obj?:any){
        if(!obj)
            return;
    
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
    }
}