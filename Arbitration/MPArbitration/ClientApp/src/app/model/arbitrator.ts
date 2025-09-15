import { LogLevel } from "@azure/msal-browser";
import { loggerCallback } from "../app.module";
import { IModifier } from "./imodifier";
import { ArbitratorType } from "./arbitrator-type-enum";
import { ArbitratorFee } from "./arbitrator-fee";
import { AuthorityDispute } from "./authority-dispute";

export interface IArbitrator {
    allStats:IArbStats[];
    cases:number;
    email:string;
    losses:number;
    name:string;
    notes:string;
    phone:string;
    statistics:string;
    wins:number;
}

export interface IArbStats {
    cases:number;
    lost:number;
    service:string;
    won:number;
}

export class Arbitrator implements IArbitrator, IModifier {
    public allStats: IArbStats[] = [];
    
    public id = 0;
    public arbitratorType = ArbitratorType.Arbitrator;
    public disputes:AuthorityDispute[] = [];

    /** A comma-separated list of services e.g. IOM,PA */
    public eliminateForServices = '';
    public email = '';
    public fees:ArbitratorFee[] = [];
    //public fixedFee = 0;
    public isActive = false;
    public isLastResort = false;
    //public mediatorFixedFee = 0;
    public name = '';
    public notes = '';
    public phone = '';
    public statistics = '';
    public updatedBy = '';
    public updatedOn: Date|undefined;

    constructor(obj?:any) {
        if(!obj)
            return;

        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        
        if(typeof obj.arbitratorType === 'string') {
            const t = obj.arbitratorType as keyof typeof ArbitratorType;
            this.arbitratorType = ArbitratorType[t] ?? ArbitratorType.Arbitrator;
        }

        if(obj.arbitratorType === undefined)
            this.arbitratorType = ArbitratorType.Arbitrator;


        // instantiate AuthorityFees array
        if(this.fees.length) {
            this.fees = this.fees.map(v => new ArbitratorFee(v));
        }

        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    
        if(this.statistics) {
            // parse stats
            try {
                this.allStats = JSON.parse(this.statistics);
            } catch {
                loggerCallback(LogLevel.Warning,'Unable to parse Arbitrator Statistics');
            }
        }    
    }
    get cases():number {
        let sum=0;
        this.allStats.map(d => sum+=d.cases);
        return sum;
    }
    get losses():number {
        let sum=0;
        this.allStats.map(d => sum+=d.lost);
        return sum;
    }
    get wins():number {
        let sum=0;
        this.allStats.map(d => sum+=d.won);
        return sum;
    }
}