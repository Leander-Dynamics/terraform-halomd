import { BaseFee } from "./base-fee";

export class AuthorityFee extends BaseFee {
    public authorityId = 0;

    constructor(obj?:any){
        super(obj);
        if(!obj)
            return;

        if(!!obj.authorityId)
            this.authorityId = obj.authorityId+0;
    }
}