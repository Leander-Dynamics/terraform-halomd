import { LogLevel } from "@azure/msal-browser";
import { loggerCallback } from "../app.module";
import { Arbitrator, IArbitrator, IArbStats } from "./arbitrator";
import { IModifier } from "./imodifier";

export enum ArbitratorDisqualification {
    Arbitrator,
    None,
    Payor,
    Provider,
    TDI
}

export class CaseArbitrator implements IArbitrator, IModifier {
    public id = 0;
    public allStats: IArbStats[] = [];
    public arbitrationCaseId: number | undefined;
    public arbitratorId: number | undefined;
    public arbitrator: Arbitrator | undefined;
    public assignedOn: Date|undefined;
    public disqualifiedBy = ArbitratorDisqualification.None;
    private eliminateForServices = '';
    public email = ''; // viewmodel property
    public fee = 0;
    public isActive = true;
    public isLastResort = false; // viewmodel property
    public isDismissed = false;
    public name = ''; // viewmodel property
    public notes = ''; // viewmodel property
    public phone = ''; // viewmodel property
    public statistics = ''; // viewmodel property
    public updatedBy = '';
    public updatedOn: Date|undefined;

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn); 
        if(this.arbitrator)
            this.arbitrator = new Arbitrator(this.arbitrator);
            
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
    get unfavorableServices():string[] {
        return this.eliminateForServices.split(';');
    }
}
