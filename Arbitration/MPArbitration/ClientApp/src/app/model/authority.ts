import { ArbitrationResult, CMSCaseStatus } from "./arbitration-status-enum";
import { AuthorityBenchmarkDetails } from "./authority-benchmark-details";
import { AuthorityCalculatorOption } from "./authority-calculator-enum";
import { AuthorityTrackingDetail } from "./authority-tracking-detail";
import { AuthorityUser } from "./authority-user";
import { AuthorityPayorGroupExclusion } from "./authority-payor-group-exclusion";
import { AuthorityFee } from "./authority-fee";
import { PayorAuthorityMap } from "./payor-authority-map";
import { UtilService } from "../services/util.service";

//import { BenchmarkSource } from "./benchmark-source";

export class Authority 
{
    private _statusList = new Array<string>();
    public id = 0;
    public authorityGroupExclusions = new Array<AuthorityPayorGroupExclusion>();
    //public benchmark = BenchmarkSource.FairHealth;
    public activeAsOf:Date|undefined;
    public benchmarks: AuthorityBenchmarkDetails[] = [];    
    public calculatorOption:AuthorityCalculatorOption = AuthorityCalculatorOption.Default;
    public chargePct = 0;
    public fees:AuthorityFee[] = [];
    public isActive = true;
    public JSON = '{}';
    public key = '';
    public name = '';
    public payorAuthorityMaps:PayorAuthorityMap[] = [];
    public stats = ''; // JSON element
    public statusValues = '';
    public trackingDetails:AuthorityTrackingDetail[] = [];
    public updatedBy = '';
    public updatedOn:Date|undefined;
    public website = '';
    // some viewmodel fields taken out of the JSON
    public defaultSubmittedStatus = '';
    public defaultUnsubmittedStatus = '';

    public addCustomerMapping(v:AuthorityUser){
        if(!v.customerName||!v.userId)
            return false;
        const x = this.customerMappings;
        if(!x.find(d=>d.userId===v.userId)){
            x.push(v);
            const j = JSON.parse(this.JSON);
            j.customerMappings = x;
            this.JSON = JSON.stringify(j);
            return true;
        }
        return false;
    }

    public removeCustomerMapping(v:AuthorityUser){
        if(!v.userId)
            return false;
        const x = this.customerMappings;
        const i = x.findIndex(d => d.userId === v.userId);
        if(i>-1){
            x.splice(i,1);
            const j = JSON.parse(this.JSON);
            j.customerMappings = x;
            this.JSON=JSON.stringify(j);
            return true;
        }
        return false;
    }

    public updateCustomerMapping(v:AuthorityUser){
        if(!v.userId)
            return false;
        const x = this.customerMappings;
        const i = x.findIndex(d=>d.userId===v.userId);
        if(i>-1){
            x[i] = v;
            const j = JSON.parse(this.JSON);
            j.customerMappings = x;
            this.JSON=JSON.stringify(j);
            return true;
        }
        return false;
    }

    public get customerMappings():AuthorityUser[] {
        if(!this.JSON)
            this.JSON = '{}'; // correct undefined and NULL to prevent error
        const j = JSON.parse(this.JSON);
        let m = j.customerMappings;
        if(!m || !Array.isArray(m))
            return new Array<AuthorityUser>();
        return m; //.map(d => new AuthorityUser(d));
    }

    public get ineligibilityReasons():{search:string, tag:string}[] {
        const j = JSON.parse(this.JSON);
        const ir:{search:string, tag:string}[] = j.ineligibilityReasons ?? [];
        
        return ir.filter(v=>!!v.search);
    }

    public set ineligibilityReasons(value:{search:string, tag:string}[]) {
        if(!this.JSON)
            this.JSON='{}'; // correct undefined and NULL to prevent error
        const j = JSON.parse(this.JSON);
        j.ineligibilityReasons = value;
        this.JSON = JSON.stringify(j);
    }
    
    public get statusMappings():AuthorityStatusMappings[] {
        if(!this.JSON)
            this.JSON='{}';
        const j = JSON.parse(this.JSON);
        
        if(!!j.statusMappings && Array.isArray(j.statusMappings)) {
            const ir = new Array<AuthorityStatusMappings>();
            return (j.statusMappings as Array<AuthorityStatusMappings>).map(v => new AuthorityStatusMappings(v));
        } else {
            return [];
        }
    }

    public set statusMappings(value:AuthorityStatusMappings[]) {
        if(!this.JSON)
            this.JSON='{}';
        const j = JSON.parse(this.JSON);
        j.statusMappings = value;
        this.JSON = JSON.stringify(j);
    }

    public get statusList() {
        if(!!this.statusValues && !this._statusList.length) {
            const r = /[;,]/;
            this._statusList = this.statusValues.split(r).map(v=>v.trim()).sort(UtilService.SortSimple);
        }
        return this._statusList;
    }

    constructor(obj?:any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        
        this.key = this.key.toLowerCase();

        if(this.activeAsOf)
            this.activeAsOf = new Date(obj.activeAsOf);
        if(this.updatedOn)
            this.updatedOn = new Date(obj.updatedOn);

        // instantiate TrackingDetails array
        if(this.trackingDetails.length) {
            const cc:AuthorityTrackingDetail[] = [];
            this.trackingDetails.forEach(d => cc.push(new AuthorityTrackingDetail(d)));
            this.trackingDetails = cc;
        }

        // instantiate BenchmarkDetails array
        if(this.benchmarks.length) {
            const bm:AuthorityBenchmarkDetails[] = [];
            this.benchmarks.forEach(d => bm.push(new AuthorityBenchmarkDetails(d)));
            this.benchmarks = bm;
        }

        // instantiate AuthorityGroupExclusions array
        if(this.authorityGroupExclusions.length) {
            const ge:AuthorityPayorGroupExclusion[] = [];
            this.authorityGroupExclusions.forEach(d => ge.push(new AuthorityPayorGroupExclusion(d)));
            this.authorityGroupExclusions = ge;
        }

        // instantiate AuthorityFees array
        if(this.fees.length) {
            this.fees = this.fees.map(v => new AuthorityFee(v));
        }

        // recover the vm fields from JSON
        if(obj.JSON) {
            try {
                const j = JSON.parse(obj.JSON);
                this.defaultSubmittedStatus = j.defaultSubmittedStatus;
                this.defaultUnsubmittedStatus = j.defaultUnsubmittedStatus;
            } catch (err){
                console.error('Error parsing Authority JSON');
            }
        } else {
            this.JSON = '{}';
        }
    }
}

export class AuthorityStatusMappings {
    arbitrationResult:ArbitrationResult = ArbitrationResult.None;
    authorityStatus='';
    workflowStatus:CMSCaseStatus|null = null;

    constructor(obj?:any){
        if(!obj)
            return;

        Object.assign(this,JSON.parse(JSON.stringify(obj)));
    }
}