import { BenchmarkDataset } from "./benchmark-dataset";

export class AuthorityBenchmarkDetails {
    public id = 0;
    public additionalAllowedFields = '';
    public additionalChargesFields = '';
    public authorityId = 0;
    public benchmark: BenchmarkDataset | null = null;
    public benchmarkDatasetId = 0;
    public isDefault = false;
    public payorAllowedField = ''; // mapped to the fh50thPercentile field in ClaimCPT and rolled up to the same field in ArbitrationClaim
    public providerChargesField = ''; // mapped to the fh80thPercentile field in ClaimCPT and rolled up to the same field in ArbitrationClaim
    public service = ''; // Service Line e.g. IOM Pro
    public updatedBy = '';
    public updatedOn: Date|undefined;

    public getAllowedFieldsArray():string[] {
        return this.additionalAllowedFields.split(/[;,| ]/g);
    }

    public getChargesFieldsArray():string[] {
        return this.additionalChargesFields.split(/[;,| ]/g);
    }

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
        if(this.benchmark)
            this.benchmark = new BenchmarkDataset(this.benchmark);
    }
}