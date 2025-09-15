export class Benchmark {
    
    public id = 0;
    public dataYear = 0;
    public fh50thPercentileCharges = 0.0;
    public fh80thPercentileCharges = 0.0;
    public geozip = '';
    public modifier: string | undefined;
    public procedureCode = '';
    public reportYear = 0;
    public source = '';
    public state: string | undefined;
    
    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
    }
}