export class BenchmarkDataset {
    
    // Benchmark Dataset
    public id = 0;
    public dataYear = 0;
    public isActive = false;
    public key = '';
    public name = '';
    public updatedBy = '';
    public updatedOn: Date|undefined;
    public valueFields = '';
    public vendor = '';

    constructor(obj?:any) {
        if(!obj)
            return;
        
        Object.assign(this, JSON.parse(JSON.stringify(obj)));

        this.key = this.key.toLowerCase();

        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);
    }
}