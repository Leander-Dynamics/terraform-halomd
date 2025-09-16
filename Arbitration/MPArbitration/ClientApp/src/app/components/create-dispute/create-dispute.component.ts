import { Component, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbDateAdapter, NgbDateParserFormatter, NgbInputDatepickerConfig, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { DataTableDirective } from 'angular-datatables';
import { Subject, combineLatest } from 'rxjs';
import { ArbitrationCase } from 'src/app/model/arbitration-case';
import { Authority } from 'src/app/model/authority';
import { AuthorityDisputeCPTVM } from 'src/app/model/authority-dispute-cpt';
import { AuthorityTrackingDetail } from 'src/app/model/authority-tracking-detail';
import { CalculatorVariables } from 'src/app/model/calculator-variables';
import { ClaimCPTBatchVM } from 'src/app/model/claim-cpt';
import { CustomDateParserFormatter, CustomNgbDateAdapter } from 'src/app/model/custom-date-handler';
import { Customer } from 'src/app/model/customer';
import { Entity } from 'src/app/model/entity';
import { Payor } from 'src/app/model/payor';
import { ProviderVM } from 'src/app/model/provider-vm';
import { ToastEnum } from 'src/app/model/toast-enum';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'create-dispute',
  templateUrl: './create-dispute.component.html',
  styleUrls: ['./create-dispute.component.css'],
  providers: [
    { provide: NgbDateAdapter, useClass: CustomNgbDateAdapter },
    { provide: NgbDateParserFormatter, useClass: CustomDateParserFormatter },
    NgbInputDatepickerConfig
  ]
})
export class CreateDisputeComponent implements OnInit {
  @ViewChild('batchForm', { static: false }) batchForm!: NgForm;
  @ViewChild(DataTableDirective, {static: false}) dtElement: DataTableDirective | undefined;
  allAuthorities: Authority[] = [];
  allCalcVariables: CalculatorVariables[] = [];
  allCustomers: Customer[] = [];
  allEntities: Entity[] = [];
  allGeoZips:string[]=[];
  allAuthorityStatuses:string[] = [];
  allPayors: Payor[] = [];
  allPlanTypes:string[] = [];
  allProviders:ProviderVM[] = [];
  
  claims: ArbitrationCase[] = [];
  claimCPTs:ClaimCPTBatchVM[] = [];
  currentCustomer = new Customer();
  dtOptions:DataTables.Settings = {};
  dtTrigger: Subject<any> = new Subject<any>();
  filters:{authorityKey:string,authorityStatus:string,customerId:number|null,daysTilDeadline:number,entityNPI:string,geoZip:string,payorId:number|null,planType:string,procedureCode:string,providerNPI:string} = {authorityKey:'',authorityStatus:'',customerId:null,daysTilDeadline:3,entityNPI:'',geoZip:'',payorId:null, planType:'', procedureCode:'',providerNPI:''};
  currentAuthority:Authority | undefined;
  currentEntity:Entity | null = null;
  currentPayor:Payor | null = null;
  destroyed$ = new Subject<void>();
  filteredCPTs:AuthorityDisputeCPTVM[] = [];
  showHelp = true;
  trackingFieldsForUI: AuthorityTrackingDetail[] = [];
  

  constructor(private svcData:CaseDataService, private router: Router, 
    private svcToast: ToastService, private svcUtil: UtilService, 
    private modalService: NgbModal, private route:ActivatedRoute){ }

  ngOnInit(): void {
  }

  addDays(d:Date,n:number,t:string){
    if(!d||!n||!t)
      return d;
    return UtilService.AddDays(d,n,t);
  }

  applySecondaryFilters() {
    const filteredData = [];
    
    for(let f of this.filteredCPTs) {
      // run the gauntlet of filters
      if(!!this.filters.procedureCode && f.claimCPT?.cptCode !== this.filters.procedureCode) 
        continue; // skip adding this to the filtered set
      if(!!this.filters.geoZip && f.geoZip !== this.filters.geoZip)
        continue;
      if(!!this.filters.planType && f.planType !== this.filters.planType)
        continue;

      // add it
      filteredData.push(Object.assign({},f));
    }
    
    this.dtOptions.data = filteredData;
    this.dtTrigger.next();
  }

  authorityChange() {
    this.currentAuthority = this.allAuthorities.find(v => v.key === this.filters.authorityKey)!;
    this.svcUtil.showLoading = true;
    this.svcData.getCustomersForBatching(this.currentAuthority).subscribe(data => {
      this.allCustomers = data;
      this.allCustomers.sort(UtilService.SortByName);
    }, err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast), () => this.svcUtil.showLoading = false);
  }
  /*
  caseSourceChange() {
    this.bulkFormat = 0;
  }
  */

  cptChange() {
    this.applySecondaryFilters();
  }

  customerChange() {
   this.resetVars('customer');
    this.currentCustomer = !!this.filters.customerId ? this.allCustomers.find(v=>v.id===this.filters.customerId)! : new Customer();
    if(!this.currentCustomer||!this.currentAuthority||this.currentCustomer.id < 1)
      return;
    
    this.svcUtil.showLoading = true;
    this.svcData.getEntitiesForBatching(this.currentAuthority,this.currentCustomer).subscribe(data => {
      this.allEntities = data;
      this.allEntities.sort(UtilService.SortByName);
    }, err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast), () => this.svcUtil.showLoading = false);
  }

  daysChange() {

  }

  entityChange() {
    this.resetVars('entity');
    this.currentEntity = this.allEntities.find(v=>v.NPINumber===this.filters.entityNPI)!;
    if(!this.currentAuthority||!this.currentCustomer||!this.currentEntity)
      return;

    this.svcUtil.showLoading = true;
    this.svcData.getProvidersForBatching(this.currentAuthority, this.currentCustomer, this.currentEntity).subscribe(data => {
      this.allProviders = data
    },
    err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
    () => this.svcUtil.showLoading = false
    );
  }

  geozipChange() {

  }
  
  loadPrerequisites() {
    const authorities$ = this.svcData.loadAuthorities();
    const calcvars$ = this.svcData.loadCalculatorVariables();
    
    combineLatest([authorities$, calcvars$]).subscribe(
      ([authorities, calcvars]) => {
        
        // authorities
        this.allAuthorities = authorities.filter(v=>v.key.toLowerCase()==='nsa' && v.isActive);
        const nsa = authorities.find(v=>v.key.toLowerCase()==='nsa');
        this.currentAuthority = nsa ? new Authority(nsa) : undefined;
        if(!this.currentAuthority) {
          this.svcToast.show(ToastEnum.danger,"Critical Error: NSA authority configuration not found. Contact IT Support immediately!");
          this.router.navigateByUrl('/');
          return;
        } else {
          this.allAuthorityStatuses = this.currentAuthority.statusList;
          this.trackingFieldsForUI = this.currentAuthority.trackingDetails.filter(d => !d.isHidden);
        }
        // calculator variables
        this.allCalcVariables = calcvars;

        this.resetVars('authority');
      },
      err => console.error('loadPrerequisites combineLatest failed', err),
      () => this.svcUtil.showLoading = false
      );
  }
  
  onSubmit() {
    if (!this.batchForm.valid)
      return false;
    return true;
  }
  
  payorChange() {
    this.resetVars('payor');

    if(!this.currentAuthority || !this.currentCustomer || !this.currentEntity || !this.filters.customerId || !this.filters.payorId || !this.filters.providerNPI)
      return;

    this.currentPayor = this.allPayors.find(v=>v.id===this.filters.payorId)!;

    this.svcUtil.showLoading = true; 

    this.svcData.getClaimsForBatching(this.currentAuthority,this.currentCustomer,this.currentPayor,this.filters.providerNPI)
    .subscribe(data => {
      this.claims = data;
      for(const c of this.claims) {
        // build filter lists
        if(this.allGeoZips.indexOf(c.locationGeoZip)===-1)
            this.allGeoZips.push(c.locationGeoZip);
        if(this.allPlanTypes.indexOf(c.planType)===-1)
            this.allGeoZips.push(c.planType);

        for(const t of c.cptCodes){
          // build filter lists
          const f = this.claimCPTs.find(v=>v.cptCode===t.cptCode);
          if(!f){
            const b = new ClaimCPTBatchVM(t);
            b.count++;
            this.claimCPTs.push(b);
          } else {
            f.count++;
          }
          
          //this.filteredCPTs.push(new AuthorityDisputeCPTVM(t,c,this.currentAuthority!));
        }
      }

      this.claimCPTs.sort(UtilService.SortByClaimCPTCode);
      this.applySecondaryFilters();
    },
    err => UtilService.HandleServiceErr(err,this.svcUtil,this.svcToast),
    () => this.svcUtil.showLoading = false
    );
  }

  planTypeChange() {
    this.applySecondaryFilters();
  }

  providerChange() {
    this.resetVars('provider');
    if(!this.currentAuthority||!this.currentEntity||!this.filters.providerNPI)
      return;

    this.svcUtil.showLoading = true;
    this.svcData.getPayorsForBatching(this.currentAuthority,this.currentCustomer,this.currentEntity,this.filters.providerNPI)
    .subscribe(data => {
        this.allPayors = data;
        this.allPayors.sort(UtilService.SortByName);
      }, 
      err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast), 
      () => this.svcUtil.showLoading = false
    );
  }
  
  resetVars(s:string) {
    switch(s) {
      // @ts-ignore
      case 'authority':
        this.allCustomers.length = 0;
        this.filters.customerId = null;
        this.currentCustomer = new Customer();

      // @ts-ignore
      case 'customer':
        this.allEntities.length = 0;
        this.currentEntity = null;
        this.filters.entityNPI = '';

      // @ts-ignore
      case 'entity':
        this.filters.providerNPI = '';
        this.allProviders.length = 0;
        this.filters.providerNPI = '';

      // @ts-ignore
      case 'provider':
        this.allPayors.length = 0;
        this.currentPayor = null;
        this.filters.payorId = null;

      // @ts-ignore
      case 'payor':
        this.filteredCPTs.length=0;
        this.claimCPTs.length=0;
        this.dtOptions.data=[];
        this.rerender();
    }
  }

  rerender(): void {
    // Call the dtTrigger to rerender current data again before destroying
    this.dtTrigger.next();

    if(this.dtElement?.dtInstance)
    {
      this.dtElement.dtInstance.then((dtInstance: DataTables.Api) => {
        // Destroy the table first
        dtInstance.clear();
        dtInstance.destroy();
        
        setTimeout(() => {
          try {
            dtInstance.columns.adjust();
          }catch(err){
            console.error('Error in rerender!');
            console.error(err);
          }
        },500);
      });
    } else {
      this.dtTrigger.next();
    }
  }

  saveClick() {
    alert('Under construction!');
  }

  statusChange() {

  }
}
