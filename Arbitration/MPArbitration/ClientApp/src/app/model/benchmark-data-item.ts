export class BenchmarkDataItem {
    public id = 0;
    public benchmarks:string = '';
    public benchmarkDatasetId = 0;
    public geoZip = '';
    public modifiers = '';
    public procedureCode = '';
    public updatedBy = '';
    public updatedOn: Date|undefined;

    public get values():any {
        if(!this.benchmarks)
            return {};
        try {
            return JSON.parse(this.benchmarks);
        } catch {
            return {};
        }
    }

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    }
}
