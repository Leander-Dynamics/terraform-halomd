import { BaseFee } from "./base-fee";

export class ArbitratorFee extends BaseFee {
    public arbitratorId = 0;

    constructor(obj?:any){
        super(obj);
        if(!obj)
            return;

        if(!!obj.arbitratorId)
            this.arbitratorId = obj.arbitratorId+0;
    }
}