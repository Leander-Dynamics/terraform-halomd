export class BenchmarkDataItemVM {
    // Authority Benchmark Details
    public id = 0;

    public authorityId = 0;
    public additionalAllowedFields = '';
    public additionalChargesFields = '';
    public benchmarkDatasetId = 0;
    public isDefault = false;
    public payorAllowedField = '';
    public providerChargesField = '';
    public service = '';

    // Benchmark Dataset
    public availableForService = '';
    public dataYear = 0;
    public key = '';
    public name = '';
    public valueFields = '';
    public vendor = '';

    // Benchmark DataItem
    public geoZip = '';
    public modifiers = '';
    public procedureCode = '';

    constructor(obj?:any) {
        if(!obj)
            return;

        this.key = this.key.toLowerCase();
        
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
    }
}
