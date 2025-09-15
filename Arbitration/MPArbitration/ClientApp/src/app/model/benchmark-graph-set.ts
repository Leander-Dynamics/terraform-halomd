import { UtilService } from "../services/util.service";
import { AuthorityBenchmarkDetails } from "./authority-benchmark-details";
import { BenchmarkDataItem } from "./benchmark-data-item";
import { ClaimCPT } from "./claim-cpt";

export class BenchmarkGraphPoint {
    /** Procedure Code number or other key. Used when grouping */
    public cptCode = '';

    public isIncluded = true;

    // public geoZip = '';  since all data is for the same ArbitrationCase.Geozip, no need to worry about putting multiple zips in our collection

    public modifier = '';

    /** Typically mapped to the [50th Percentile Allowed] x [Units] */
    public payorAllowed = 0;

    /** Typically mapped to the [80th Percentile Charges] x [Units] */
    public providerCharges = 0;
}

export class BenchmarkGraphSet {
    public datasetId = 0;
    public dataYear = 0;
    public isVisible = true;
    public items = new Array<BenchmarkGraphPoint>();
    public name = '';
    public order = 0;

    constructor(datasetId:number, name:string, year:number) {
        this.datasetId = datasetId;
        this.name = name;
        this.dataYear = year;
    }

    public get payorAllowedTotal() {
        let b = 0;
        this.items.filter(d => d.isIncluded).forEach(v=>b+=v.payorAllowed);
        return b;
    }

    public get providerChargesTotal() {
        let b = 0;
        this.items.filter(d => d.isIncluded).forEach(v=>b+=v.providerCharges);
        return b;
    }

    public addInsertItem(abm:AuthorityBenchmarkDetails, data:BenchmarkDataItem, cpt:ClaimCPT) {
        let i = this.getItem(cpt);
        const found = !!i;
        if(!i) {
            i = new BenchmarkGraphPoint();
        }
        i.cptCode = cpt.cptCode;
        i.isIncluded = cpt.isIncluded;
        i.modifier = cpt.modifiers;
        i.payorAllowed = UtilService.RoundMoney(cpt.units * (data.values[abm.payorAllowedField] || 0));
        i.providerCharges = UtilService.RoundMoney(cpt.units * (data.values[abm.providerChargesField] || 0));
        if(!found) {
            //console.log(`addInsertItem: adding cpt ${cpt.cptCode} to benchmark ${abm.benchmarkDatasetId} : $${i.payorAllowed}`);
            this.items.push(i);
        } 
        /*
        else {
            console.log(`addInsertItem: updated cpt ${cpt.cptCode} on benchmark ${abm.benchmarkDatasetId} to $${i.payorAllowed}`);
        }
        */
    }

    public deleteItem(cpt:ClaimCPT) {
        const i = this.getItem(cpt);
        if(!i)
            return;
        const n = this.items.indexOf(i);
        this.items.splice(n,1);
    }

    public getItem(cpt:ClaimCPT): BenchmarkGraphPoint | undefined {
        return this.items.find(d => d.cptCode.toLowerCase() === cpt.cptCode.toLowerCase() && d.modifier === cpt.modifiers);
    }
    
}