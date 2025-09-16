import { Template } from '@angular/compiler/src/render3/r3_ast';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Authority, AuthorityStatusMappings } from 'src/app/model/authority';
import { NgbDate, NgbDateAdapter, NgbDateParserFormatter, NgbInputDatepickerConfig, NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, combineLatest, Subject, Subscription, timer } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';
import { AppUser } from 'src/app/model/app-user';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthorityTrackingDetail, AuthorityTrackingDetailScope } from 'src/app/model/authority-tracking-detail';
import { loggerCallback } from 'src/app/app.module';
import { LogLevel } from '@azure/msal-browser';
import { NgForm } from '@angular/forms';
import { AuthorityBenchmarkDetails } from 'src/app/model/authority-benchmark-details';
import { BenchmarkDataset } from 'src/app/model/benchmark-dataset';
import { BenchmarkDataItemVM } from 'src/app/model/benchmark-data-item-vm';
import { AuthorityCalculatorOption } from 'src/app/model/authority-calculator-enum';
import { ArbitrationResult, CMSCaseStatus } from 'src/app/model/arbitration-status-enum';
import { IKeyId } from 'src/app/model/iname';
import { CaseFileVM } from 'src/app/model/case-file';
import { FileUploadEventArgs } from 'src/app/model/file-upload-event-args';
import { AuthorityUser } from 'src/app/model/authority-user';
import { CustomDateParserFormatter, CustomNgbDateAdapter } from 'src/app/model/custom-date-handler';
import { AuthorityPayorGroupExclusion, AuthorityPayorGroupExclusionVM } from 'src/app/model/authority-payor-group-exclusion';
import { Payor } from 'src/app/model/payor';
import { JobQueueItem } from 'src/app/model/job-queue-item';
import { PayorGroupResponse } from 'src/app/model/payor-group-response';
import { BaseFee } from 'src/app/model/base-fee';
import { AddFeeComponent } from '../add-fee/add-fee.component';
import { AuthorityFee } from 'src/app/model/authority-fee';

@Component({
  selector: 'app-manage-authorities',
  templateUrl: './manage-authorities.component.html',
  styleUrls: ['./manage-authorities.component.css'],
  providers: [
    { provide: NgbDateAdapter, useClass: CustomNgbDateAdapter },
    { provide: NgbDateParserFormatter, useClass: CustomDateParserFormatter },
    NgbInputDatepickerConfig
  ]
})
export class ManageAuthoritiesComponent implements OnInit {
  @ViewChild('addDialog') addDialog: Template | undefined;
  @ViewChild('editDialog') editDialog: Template | undefined;
  @ViewChild('authorityForm', { static: false }) authorityForm!: NgForm;
  @ViewChild('groupsFile', {static: false}) groupsFile: ElementRef | undefined;
  @ViewChild('payorGroupModal', {static:false}) payorGroupModal: Template | undefined;
  @ViewChild('loadResult') basicModal: Template | undefined;
  activeAsOfDate: string | undefined;
  allAuthorityExclusions = new Array<AuthorityPayorGroupExclusionVM>();
  allFees$ = new BehaviorSubject<BaseFee[]>([]);
  allCaseFileVMs$ = new BehaviorSubject<CaseFileVM[]>([]);
  allFieldsForAllowed:string[] = [];
  allFieldsForCharges:string[] = [];
  allArbitrationResults = new Array<IKeyId>();
  allAuthorities: Authority[] = [];
  allBenchmarkDetails: BenchmarkDataItemVM[] = [];  // useful for the UI
  allBenchmarks: BenchmarkDataset[] = [];
  allCalculatorOptions = new Array<IKeyId>();
  allIneligibilityReasons = new Array<{tag:string,search:string}>();
  allPayors:Payor[] = [];
  allServices: {name:string,serviceLine:string}[] = [];
  allStatusMappings = new Array<AuthorityStatusMappings>();
  allValueFields:string[] = [];
  AuthorityTrackingDetailScope = AuthorityTrackingDetailScope;
  canEdit = false;
  currentAuthority: Authority | null = null;
  currentBenchmarkDetails: AuthorityBenchmarkDetails | null = null;
  currentJob:JobQueueItem|undefined;
  currentPayor: Payor | null = null;
  currentUser: AppUser | undefined;
  customerMappings = new Array<AuthorityUser>();

  destroyed$ = new Subject<void>();
  documentType = '';
  hideBenchmarks = true;
  hideCustomerMap = true;
  hideExclusions = true;
  hideStatusMap = true;
  hideTracking = true;
  isAdmin = false;
  isEditing = false;
  isError = false;
  isJobStalled = false;
  isManager = false;
  isDeveloper = false;
  jobMessage = 'Waiting to start...';
  loadTitle = '';
  loadMessage = 'Upload complete';

  modalOptions: NgbModalOptions | undefined;
  orig: Authority | null = null;
  newAuthorityUser = new AuthorityUser();
  newBenchmark = new AuthorityBenchmarkDetails();
  newPayorGroupNumber = '';
  newPayorId = 0;
  newIsNSAIneligible = false;
  newIsStateIneligible = false;
  newTracking = new AuthorityTrackingDetail();
  AuthorityCalculatorOption = AuthorityCalculatorOption;
  newIneligibilitySearch = '';
  newIneligibilityTag = '';
  recalcAll=false;
  showUpload = false;
  statKeys = new Array<string>();
  status:CMSCaseStatus | null = null;
  statuses = new Array<IKeyId>();
  sub$:Subscription|undefined;
  
  constructor(private svcData: CaseDataService,
    private svcToast: ToastService,
    private svcUtil: UtilService,
    private route: ActivatedRoute,
    private svcModal: NgbModal,
    private svcAuth: AuthService,
    private router:Router) {
    this.modalOptions = {
      backdrop: 'static',
      backdropClass: 'customBackdrop',
      keyboard: false,
    };
    
    this.allArbitrationResults = Object.values(ArbitrationResult).filter(value => typeof value === 'string').map(key => {
      const result = (key as string).split(/(?=[A-Z][a-z])/);
      return { id: (<any>ArbitrationResult)[key] as number, key: result.join(' ') };
    });

    this.allCalculatorOptions = Object.values(AuthorityCalculatorOption).filter(value => typeof value === 'string').map(key => { 
      const result = (key as string).split(/(?=[A-Z][a-z])/);
      return {id: (<any>AuthorityCalculatorOption)[key] as number, key: result.join(' ') }; 
    });

    this.statuses = Object.values(CMSCaseStatus).filter(value => typeof value === 'string' && value !== 'Search' && value !== 'Unknown').map(key => {
      const result = (key as string).split(/(?=[A-Z][a-z])/);
      return { id: (<any>CMSCaseStatus)[key] as number, key: result.join(' ') };
    });

  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
    this.sub$?.unsubscribe();
    this.allFees$.complete();
    this.allCaseFileVMs$.complete();
  }

  ngOnInit(): void {
    this.subscribeToData();
  }

  loadPrerequisites() {
    this.svcUtil.showLoading = true;
    const authorities$ = this.svcData.loadAuthorities();
    const benchmarks$ = this.svcData.loadBenchmarkDatasets();
    const services$ = this.svcData.loadServices();
    const payors$ = this.svcData.loadPayors(true, true, false); 
    const jobs$ = this.svcData.getJobQueueItemsByType('recalculate');

    combineLatest([authorities$, benchmarks$, services$, payors$, jobs$]).subscribe(
      ([authorities, benchmarks, services, payors, jobs]) => {
        // payors
        this.allPayors = payors.filter(d=>d.parentId===d.id);
        this.allPayors.sort(UtilService.SortByName);

        this.allBenchmarks = benchmarks;
        this.allBenchmarks.sort(UtilService.SortByName);
        // authorities
        this.allAuthorities = authorities;
        this.allAuthorities.sort(UtilService.SortByName);
        this.allAuthorities.forEach(d=>{
          if(d.trackingDetails.length>0)
            d.trackingDetails.sort(UtilService.SortByOrder);
        });
        // services
        this.allServices = services;
        this.allServices.sort(UtilService.SortByName);
        // running jobs
        if(jobs.length)
          this.checkForRunningJob(jobs);
      },
      err => {
        this.svcUtil.showLoading = false;
        loggerCallback(LogLevel.Error, err);
        this.svcToast.show(ToastEnum.danger, err.message ?? err.toString());
      },
      () => this.svcUtil.showLoading = false
    );
  }

  activeAsOfDateChange(e:any){
    if(!this.currentAuthority)
      return;
    if(!!e.target.value){
      const date = new Date(e.target.value);
      if(!date || !UtilService.IsDateValid(date)){
        this.svcToast.show(ToastEnum.danger,'The entered value does not appear to be a valid date!');
        this.currentAuthority.activeAsOf = undefined;
      }
    }
    if(!this.currentAuthority.activeAsOf)
      this.setActiveAsOfDate(null);
  }

  addAuthority(): void {}

  addBenchmark(a:BenchmarkDataItemVM) {
    if(!this.currentAuthority || !a)
      return;

    this.newBenchmark = new AuthorityBenchmarkDetails(a);
    this.newBenchmark.id = 0; // prev line may copy a bad id over - new recs always have id = 0
    this.newBenchmark.service = a.availableForService;
    this.isEditing = false;
    // setup ui selections
    const r = /[;,| ]/;
    this.allFieldsForAllowed = a.valueFields.split(r);
    this.allFieldsForCharges = a.valueFields.split(r);
    this.svcModal.open(this.editDialog, {...this.modalOptions,size:'xl'}).result.then(data => {
      this.svcUtil.showLoading = true;
      this.svcData.createAuthorityBenchmarkDetail(this.currentAuthority!, this.newBenchmark)
      .subscribe(rec => {
        this.currentAuthority!.benchmarks.push(rec);
        this.currentAuthority!.benchmarks.sort(UtilService.SortByService);
        this.authorityChange();
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.success,'Benchmark successfully added to this Authority');
      },
      err => console.warn('Add Benchmark canceled'),)
    });
  }
  
  addExclusion() {
    if(!this.currentAuthority)
      return;

    this.currentPayor = null;
    this.newPayorGroupNumber = '';
    this.newIsNSAIneligible = false;
    this.newIsStateIneligible = false;

  this.svcModal.open(this.payorGroupModal, this.modalOptions).result.then(data => {
    if(!this.currentPayor)
      return;
    this.svcUtil.showLoading = true;
    const grp = new AuthorityPayorGroupExclusion({authorityId:this.currentAuthority!.id,groupNumber:this.newPayorGroupNumber,isNSAIneligible:this.newIsNSAIneligible,isStateIneligible:this.newIsStateIneligible,payorId:this.currentPayor!.id});
    this.svcData.createAuthorityPayorGroupExclusion(this.currentAuthority!.id, grp).subscribe(rec => {
      this.currentAuthority!.authorityGroupExclusions.push(rec);
      this.allAuthorityExclusions.push(new AuthorityPayorGroupExclusionVM({...rec, ...{payorName: this.currentPayor!.name}}));
      this.allAuthorityExclusions.sort(UtilService.SortByPayorName);
      this.svcUtil.showLoading = false;
      this.svcToast.show(ToastEnum.success,'New Authority Payor Group Exclusion created successfully!');
    },
    err => this.handleServiceErr(err),
    () => this.svcUtil.showLoading = false
    );
    
  },
  err => {
    loggerCallback(LogLevel.Error,'Error using Add Payor Group Exclusion modal');
    loggerCallback(LogLevel.Error, err);
    this.svcToast.show(ToastEnum.danger, err.message ?? err.toString());
  });
  }

  addFee(e:any){
    if(this.currentAuthority===null)
      return;

    this.svcModal.open(AddFeeComponent, {...this.modalOptions,size:'md'}).result.then(data => {
      this.svcUtil.showLoading = true;
      const fee = new AuthorityFee(data);
      fee.authorityId = this.currentAuthority!.id;
      this.svcData.createAuthorityFee(this.currentAuthority!, fee).subscribe(data => {
        this.currentAuthority!.fees.push(data);
        this.allFees$.next(this.currentAuthority!.fees);
      },
      err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
      () => this.svcUtil.showLoading = false);
    },
    reason => this.svcToast.show(ToastEnum.info, 'Add Fee canceled'));
  }

  addFile(e:any) {
    if(!(e instanceof FileUploadEventArgs))
      return;
    const args = e as FileUploadEventArgs;

    if (!args.file || !args.documentType || !this.currentAuthority)
      return;
    
    this.svcUtil.showLoading = true;
    const lowDt = e.documentType.toLowerCase();

    this.svcData.uploadEntityDocument(args.file, this.currentAuthority.id, e.documentType.toLowerCase(),this.currentAuthority).subscribe(
      {
        next: (data: any) => {
          e.element.value = '';
          this.documentType = '';
          this.svcToast.show(ToastEnum.success, 'File uploaded successfully!');
          const m = this.allCaseFileVMs$.getValue();
          const n = new CaseFileVM({
            blobName: `${lowDt}-authority-${this.currentAuthority!.id}-${e.filename.toLowerCase()}`,
            createdOn: new Date(),
            DocumentType: lowDt,
            UpdatedBy: this.svcAuth.getActiveAccount()?.name || 'system'
          });
          m.push(n);
          this.allCaseFileVMs$.next(m);
        },
        error: (err) => {
          this.svcUtil.showLoading = false;
          this.svcToast.show(ToastEnum.danger, err, 'Upload Failed');
        }, 
        complete: () => this.svcUtil.showLoading = false
      }
    );
  }

  addRecord() {
    if(!this.currentAuthority)
      return;
    this.newTracking = new AuthorityTrackingDetail();
    this.newTracking.authorityId = this.currentAuthority.id;
    this.isEditing = false;
    this.svcModal.open(this.addDialog, this.modalOptions).result.then(data => {
      this.svcUtil.showLoading = true;
      const g = this.currentAuthority!.trackingDetails.length ?? 0;
      this.newTracking.order = g;
      this.svcData.createAuthorityTrackingDetail(this.currentAuthority!, this.newTracking)
      .subscribe(rec => {
        this.currentAuthority!.trackingDetails.push(rec);
        this.currentAuthority!.trackingDetails.sort(UtilService.SortByOrder);
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.success,'New Authority Tracking Configuration created successfully!');
      },
      err => console.warn('Add record canceled')
      );
    },
    err => {
      loggerCallback(LogLevel.Error,'Error using Authority Tracking Detail modal');
      loggerCallback(LogLevel.Error, err);
      this.svcToast.show(ToastEnum.danger, err.message ?? err.toString());
    });
  }

  authorityChange() {
    this.statKeys = [];
    this.activeAsOfDate = undefined;
    this.newAuthorityUser = new AuthorityUser();
    this.customerMappings = new Array<AuthorityUser>();
    this.orig = !!this.currentAuthority ? new Authority(this.currentAuthority) : null;
    this.hideBenchmarks = !!this.currentAuthority ? this.hideBenchmarks : false;
    this.allAuthorityExclusions = [];
    this.allIneligibilityReasons.length = 0;
    this.allStatusMappings.length = 0;
    this.allBenchmarkDetails.length = 0;
    this.allFieldsForAllowed.length = 0;
    this.allFieldsForCharges.length = 0;
    this.newIneligibilityTag = '';
    this.newIneligibilitySearch = '';
    const vms = new Array<CaseFileVM>();
    this.allCaseFileVMs$.next(vms);
    this.allFees$.next(new Array<BaseFee>());

    if(!this.currentAuthority)
      return;

    this.customerMappings = this.currentAuthority.customerMappings;
    this.activeAsOfDate = this.currentAuthority?.activeAsOf?.toLocaleDateString() ?? undefined;
    const f$ = this.svcData.loadAuthorityFiles(this.currentAuthority.id);
    const x$ = this.svcData.getAuthorityByKey(this.currentAuthority.key,true);
    this.svcUtil.showLoading = true;

    combineLatest([f$, x$]).subscribe(([f, x]) => {
      // build exclusionVM array
      if(!!x) {
        this.allAuthorityExclusions = x.authorityGroupExclusions.map(rec => {
          let p = this.allPayors.find(v => v.id === rec.payorId);
          let name = !!p ? p.name : '???';
          return new AuthorityPayorGroupExclusionVM({...rec, ...{payorName: name}});
        });
        this.allAuthorityExclusions.sort(UtilService.SortByPayorName);
        if(!!x.stats) {      
          this.statKeys = Object.keys(x.stats); //.filter(value => typeof value === 'string');
          this.statKeys.sort(UtilService.SortSimple);
          this.currentAuthority!.stats = x.stats;
        }
        this.allFees$.next(x.fees);
      }
      f.forEach(cf => {
        const vm = new CaseFileVM(cf.tags);
        vm.blobName = cf.blobName;
        vm.createdOn = cf.createdOn;
        vms.push(vm);
      });
      this.allCaseFileVMs$.next(vms);
    },
    err => UtilService.HandleServiceErr(err,this.svcUtil,this.svcToast),
    () => this.svcUtil.showLoading = false);

    for(let b of this.allBenchmarks) {
      for(let s of this.allServices) {
        let n = this.currentAuthority.benchmarks.find(d => d.benchmarkDatasetId === b.id && d.service === s.name);
        let vm = n ? new BenchmarkDataItemVM({...b, ...n}) : new BenchmarkDataItemVM(b);
        vm.id = n ? n.id : 0;
        vm.authorityId = this.currentAuthority.id;
        vm.benchmarkDatasetId = b.id;
        vm.availableForService = s.name;
        this.allBenchmarkDetails.push(vm);
      }
    }
    this.allBenchmarkDetails.sort(this.sortVMs);
    // duplicate some settings into the viewmodel 
    for(let r of this.currentAuthority.ineligibilityReasons)
      this.allIneligibilityReasons.push({tag:r.tag, search:r.search});
    for(let r of this.currentAuthority.statusList) {
      const m = this.currentAuthority.statusMappings.find(v=>v.authorityStatus.toLowerCase()===r.toLowerCase());
      if(!m)
        this.allStatusMappings.push({arbitrationResult:ArbitrationResult.None, authorityStatus:r, workflowStatus:null});
      else
        this.allStatusMappings.push(m);
    }
  }

  cancelChanges() {
    if(!this.currentAuthority)
      return;
    if(!confirm('Are you sure you want to cancel?'))
      return;
    if(this.currentAuthority.id===0) {
      const i = this.allAuthorities.findIndex(d => d.id === 0);
      this.allAuthorities.splice(i,1);
      this.currentAuthority = null;
    } else if(this.orig) {
      this.currentAuthority.isActive = this.orig.isActive;
      this.currentAuthority.name = this.orig.name;
      this.currentAuthority.website = this.orig.website;
    }
    this.resetFormStatus();
  }

  checkForRunningJob(jobs:JobQueueItem[]) {
    jobs.sort(UtilService.SortById);
    const j = jobs[0];
    if(!j.jobStatus.lastUpdated)
      return;

    this.isJobStalled = this.checkForStalledJob(j);

    if(this.isJobStalled){
      this.svcToast.showAlert(ToastEnum.warning, `Recalculation Job ${j.id} is Stalled. Contact the system adminstrator.`);
    } else {
      this.currentJob = j;
      this.jobMessage = j.jobStatus.message.replace(`\n`,'<br />');
      this.sub$ = timer(10000).subscribe(val => this.refreshJobStatus());
    }
  }

  checkForStalledJob(j:JobQueueItem): boolean {
    if(!j.jobStatus.lastUpdated)
      return false;
    const mins = 60000 * 6;
    const d = new Date();
    const stallTime = new Date(d.getTime() - mins);
    return j.jobStatus.lastUpdated < stallTime;
  }

  customerMapChange(r:AuthorityUser,isNew:boolean = false) {
    if(!this.currentAuthority || !r.userId || !r.customerName)
      return;
    if(isNew) {
      if(!this.currentAuthority.addCustomerMapping(this.newAuthorityUser)) {
        this.svcToast.showAlert(ToastEnum.danger,'Unable to add the Portal Mapping. Are you sure the UserId is unique?');
        return;
      } else {
        this.newAuthorityUser = new AuthorityUser();
        this.customerMappings = this.currentAuthority.customerMappings;
        this.svcToast.showAlert(ToastEnum.warning,"Authority information changed. Click 'Save Changes' to keep them.");  
      }
    } else {
      // update existing
      this.currentAuthority.updateCustomerMapping(r);
    }
    this.authorityForm.form.markAsDirty();
    this.authorityForm.form.markAsTouched();
    this.svcToast.showAlert(ToastEnum.warning,"Authority information changed. Click 'Save Changes' to keep them.");
  }
  
  decOrder(d:AuthorityTrackingDetail) {
    if(!this.currentAuthority||!d||d.order<=0)
      return;

    d.order-=1;
    // update
    this.svcData.updateAuthorityTrackingDetail(this.currentAuthority, d).subscribe(rec => {
      this.currentAuthority?.trackingDetails.sort(UtilService.SortByOrder);
      },
      err => {
        this.svcUtil.showLoading = false;
        loggerCallback(LogLevel.Error, err);
        this.svcToast.show(ToastEnum.danger,'Error updating Authority: ' + err.message);
      },
      () => this.svcUtil.showLoading = false
    );
  }

  deleteCustomerMapping(r:AuthorityUser){
    if(this.currentAuthority && this.currentAuthority.removeCustomerMapping(r)) {
      this.customerMappings = this.currentAuthority.customerMappings;
      this.authorityForm.form.markAsDirty();
      this.authorityForm.form.markAsTouched();
      this.svcToast.showAlert(ToastEnum.warning,"Authority information changed. Click 'Save Changes' to keep them.");
    }
  }

  deleteExclusion(x:AuthorityPayorGroupExclusionVM){
    if(!confirm(`Are you sure you want to delete the exclusion for ${x.groupNumber}:`))
      return;
    this.svcUtil.showLoading = true;
    this.svcData.deleteAuthorityPayorGroupExclusion(x).subscribe(() => {
      const ndx = this.allAuthorityExclusions.findIndex(d => d.id === x.id);
      if(ndx > -1)
        this.allAuthorityExclusions.splice(ndx, 1);
      this.svcToast.show(ToastEnum.success, 'Exclusion successfully deleted');
    },
    err => this.handleServiceErr(err),
    () => this.svcUtil.showLoading = false
    );
  }

  deleteFile(e:any) {
    if(!this.currentAuthority?.id || !e || !(e instanceof CaseFileVM))
      return;

    const f = e as CaseFileVM;
    if (!confirm('Are you sure you want to permanently delete this resource file from the Authority?'))
      return;
    
    this.svcUtil.showLoading = true;
    this.svcData.deleteEntityFile(this.currentAuthority.id, f.DocumentType, f.blobName, this.currentAuthority).subscribe(data => {
      this.svcToast.show(ToastEnum.success, 'File deleted');
      const a = this.allCaseFileVMs$.getValue();
      const ndx = a.findIndex(d => d.blobName === f.blobName);
      if (ndx > -1) {
        a.splice(ndx, 1);
        this.allCaseFileVMs$.next(a);
      }
    },
    err => {
      this.svcUtil.showLoading = false;
      this.svcToast.showAlert(ToastEnum.danger, err.message ?? err.toString());
    },
    () => this. svcUtil.showLoading = false
    );
  }

  deleteRecord(v:AuthorityTrackingDetail) {
    if(!this.currentAuthority)
      return;
    if(!confirm('ARE YOU SURE you want to remove a tracking field from ALL OPEN CLAIMS? If you make a mistake on an UNCALCULATED field, the data will be lost PERMANENTLY.'))
      return;

    if(!confirm('Last chance. Are you sure? You will need to re-calculate the claims for this Authority with the Recalc Tracking button after this modification.'))
      return;

    this.svcUtil.showLoading = true;
    this.svcData.deleteAuthorityTrackingDetail(this.currentAuthority, v.id)
    .subscribe(() => {
      const idx = this.currentAuthority!.trackingDetails.indexOf(v);
      if(idx >= 0) {
        this.currentAuthority!.trackingDetails.splice(idx,1);
      }
      this.svcToast.show(ToastEnum.success,'Tracking Configuration removed successfully');
    },
    err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
    () => this.svcUtil.showLoading = false);
  }
  
  editBenchmark(a:BenchmarkDataItemVM) {
    if(!this.currentAuthority || !a)
      return;
    
    this.newBenchmark = new AuthorityBenchmarkDetails(a);
    this.isEditing = true;
    // setup ui selections
    const r = /[;,| ]/;
    this.allFieldsForAllowed = a.valueFields.split(r);
    this.allFieldsForCharges = a.valueFields.split(r);
    if(this.allFieldsForAllowed.indexOf(this.newBenchmark.payorAllowedField)==-1)
      this.newBenchmark.payorAllowedField = '';
    if(this.allFieldsForCharges.indexOf(this.newBenchmark.providerChargesField)==-1)
      this.newBenchmark.providerChargesField = '';
    this.svcModal.open(this.editDialog, {...this.modalOptions,size:'xl'}).result.then(data => {
      this.svcUtil.showLoading = true;
      this.svcData.updateAuthorityBenchmarkDetail(this.currentAuthority!, this.newBenchmark)
      .subscribe(rec => {
        this.isEditing = false;
        // replace current benchmark
        const i = this.currentAuthority!.benchmarks.findIndex(d => d.id === rec.id);
        if(i > -1) {
          this.currentAuthority!.benchmarks[i] = new AuthorityBenchmarkDetails(rec);
        }
        // update the vm item
        this.authorityChange();
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.success,'Benchmark updated successfully');
      },
      err => console.warn('Edit Benchmark canceled'),)
    });
  }

  editFee(e:any) {
    if(this.currentAuthority===null||isNaN(e))
      return;

    const f = this.currentAuthority.fees.find(v => v.id===e);
    if(!f)
      return;
    const ref = this.svcModal.open(AddFeeComponent, {...this.modalOptions,size:'md'});
    ref.componentInstance.fee = f as BaseFee;
    ref.componentInstance.feeType = 'Authority';
    ref.result.then(data => {
      this.svcUtil.showLoading = true;
      this.svcData.updateAuthorityFee(this.currentAuthority!, f).subscribe(data => {
        const n = this.currentAuthority!.fees.findIndex(v => v.id===data.id);
        this.currentAuthority!.fees.splice(n,1,data);
        this.allFees$.next(this.currentAuthority!.fees);
      },
      err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
      () => this.svcUtil.showLoading = false);
    },
    reason => this.svcToast.show(ToastEnum.info, 'Add Fee canceled'));
  }

  editRecord(v:AuthorityTrackingDetail) {
    if(!v || !this.currentAuthority)
      return;
    this.isEditing = true;
    this.newTracking = new AuthorityTrackingDetail(v);
    this.svcModal.open(this.addDialog, this.modalOptions).result.then(data => {
      this.svcUtil.showLoading = true;
      this.svcData.updateAuthorityTrackingDetail(this.currentAuthority!, this.newTracking)
      .subscribe(rec => {
        const idx = this.currentAuthority!.trackingDetails.findIndex(d=>d.id === rec.id);
        if(idx >= 0) {
          this.currentAuthority!.trackingDetails[idx] = rec;
        }

        this.svcToast.show(ToastEnum.success,'Authority Tracking Configuration updated successfully!');
      },
      err => console.warn('Edit Tracking canceled'),
      () => this.svcUtil.showLoading = false
      );
    },
    err => {
      loggerCallback(LogLevel.Error,'Error using Authority Tracking Configuration modal to edit');
      loggerCallback(LogLevel.Error, err);
      this.svcToast.show(ToastEnum.danger, err.message ?? err.toString());
    });
  }

  exclusionChange(x:AuthorityPayorGroupExclusion) {

  }

  fileSelected():boolean {
    return this.groupsFile?.nativeElement.files.length ? true : false;
  }

  fileSelectionChanged() {
    loggerCallback(LogLevel.Verbose,'Upload file selection changed'); // this triggers a blur/change detection or else the button won't light up
  }

  getReferenceFieldNames():string[] {
    if(!this.currentAuthority || !this.newTracking)
      return [];
    const vals = this.currentAuthority?.trackingDetails.filter(d => d.trackingFieldName !== this.newTracking.trackingFieldName).map(d => d.trackingFieldName);
    vals.push('EOBDate');
    vals.push('FirstResponseDate');
    vals.push('FirstAppealDate');
    vals.push('SubmissionDate');
    vals.sort(UtilService.SortSimple);
    return vals;
  }

  getStat(key:string){
    if(!this.currentAuthority || !this.currentAuthority.stats)
      return 0;
    return (this.currentAuthority!.stats as any)[key];
  }
  
  handleServiceErr(err:any) {
    let msg = '';
    msg = err?.error?.title ?? err?.error ?? err?.message ?? err?.statusText ?? err.toString();
    this.svcToast.showAlert(ToastEnum.danger, msg);
    this.svcUtil.showLoading = false;
    window.scrollTo({top:0,behavior:'smooth'});
  }

  incOrder(d:AuthorityTrackingDetail) {
    if(!this.currentAuthority || !this.currentAuthority.trackingDetails || !this.currentAuthority.trackingDetails.length)
      return;
    if(!d || d.order >= this.currentAuthority.trackingDetails.length-1)
      return;

    d.order+=1;
    // update
    this.svcData.updateAuthorityTrackingDetail(this.currentAuthority, d).subscribe(rec => {
      this.currentAuthority?.trackingDetails.sort(UtilService.SortByOrder);
    },
    err => {
      this.svcUtil.showLoading = false;
      loggerCallback(LogLevel.Error, err);
      this.svcToast.show(ToastEnum.danger,'Error updating Authority: ' + err.message);
    },
    () => this.svcUtil.showLoading = false
    );
    
  }

  ineligibilitySearchChange(r:{search:string,tag:string}) {
    if(!this.currentAuthority || !r.search || !r.tag)
      return;
    this.currentAuthority.ineligibilityReasons = this.allIneligibilityReasons;
    this.saveChanges();
  }

  isActiveChange() {
    if(!this.currentAuthority || this.currentAuthority?.isActive)
      return;
    this.currentAuthority.activeAsOf = undefined;
    this.activeAsOfDate = undefined;
  }

  newIneligibilityChange() {
    if(!this.currentAuthority || !this.newIneligibilitySearch || !this.newIneligibilityTag)
      return;
    const r = this.currentAuthority.ineligibilityReasons;
    r.push({search:this.newIneligibilitySearch, tag:this.newIneligibilityTag});
    this.currentAuthority.ineligibilityReasons = r;
    this.saveChanges();
  }

  recalc() {
    if(!this.currentAuthority)
      return;
    const k = this.currentAuthority.key.toLowerCase();
    //if(k==='tx')    {
    //  this.svcToast.showAlert(ToastEnum.warning,'The TX authority does not need recalculating. It has a dedicated set of date fields.');
    //  return;
    //}

    if(!!this.currentJob){
      this.svcToast.showAlert(ToastEnum.warning,'There is already a recalculation job running. Only one recalculation job can run at a time.');
      return;
    }

    let msg = `This will recalculate all ${this.currentAuthority.name} claim deadlines where the `;
    msg += k==='nsa' ? `NSA Status equals 'Pending NSA Negotiation Request' or 'Submitted NSA Negotiation Request'` : `Authority Workflow Status is not closed (final).`;
    msg += ' It could take a while to run. Continue?';

    if(!confirm(msg))
      return;

    this.svcUtil.showLoading = true;
    const aa = this.recalcAll; // && this.isDeveloper; removed this restriction because a) the feature needs to be fully available and b) the operators of this application need to take responsibility for its use
    this.svcData.recalcAuthority(this.currentAuthority,!aa).subscribe(
      data => {
        this.currentJob = new JobQueueItem(data);
        if(this.currentJob.jobStatus.status !== 'finished') {
          this.sub$ = timer(10000).subscribe(val => this.refreshJobStatus());
        }
      },
      err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
      () => this.svcUtil.showLoading = false
    );
  }

  refreshJobStatus() {
    if(!this.currentJob)
      return;
    this.svcData.getJobQueueItem(this.currentJob.id).subscribe(
      data => {
        this.isJobStalled = this.checkForStalledJob(data);
        if(this.isJobStalled){
          this.currentJob = undefined;
          this.svcToast.showAlert(ToastEnum.warning, `Recalculation Job ${data.id} is Stalled. Contact the system adminstrator.`);
          return;
        }

        this.currentJob = data;
        this.jobMessage = data.jobStatus.message.replace(`\n`,'<br />');
        if(this.currentJob.jobStatus.status === 'finished') { 
          const j = JSON.parse(this.currentJob.JSON);
          const le = j['lastError'] ?? '';
          const tr = j['totalRecords'] ?? 0;
          const pr = j['recordsProcessed'] ?? 0;
          const msg = `Recalculate completed. Records found: ${tr}. Records recalculated: ${pr}. ` + (!!le ? `Last error: ${le}`:'');
          this.svcToast.showAlert(ToastEnum.success,msg);
          this.currentJob = undefined;
          return;
        }
        this.sub$ = timer(10000).subscribe(val => this.refreshJobStatus());
        
      },
      err => {
        this.currentJob = undefined;
        this.sub$?.unsubscribe();
        this.sub$ = undefined;
      },
      () => this.svcUtil.showLoading = false
    );
  }

  workflowStatusChange(s:AuthorityStatusMappings) {
    if(!this.currentAuthority || !s.authorityStatus || s.workflowStatus === null)
      return;
    this.currentAuthority.statusMappings = this.allStatusMappings;
    this.saveChanges();
  }

  onSubmit() {
    if (!this.authorityForm?.valid) 
      return false;
      
    this.saveChanges();
    return true;
  }

  payorAllowedChange() {

  }

  payorChange() {
    this.newPayorGroupNumber = '';
  }

  providerChargesChange() {

  }

  removeBenchmark(a:BenchmarkDataItemVM) {
    if(!this.currentAuthority)
      return;
    if(!confirm(`Are you sure you want to remove this Benchmark from ${a.name}?`))
      return;

    this.svcData.deleteAuthorityBenchmark(a.authorityId, a.id).subscribe(() => {
      this.svcUtil.showLoading = true;
      // find and remove
      const i = this.currentAuthority!.benchmarks.findIndex(d => d.id === a.id);
      this.currentAuthority!.benchmarks.splice(i,1);
      this.authorityChange();
      this.svcUtil.showLoading = false;
      this.svcToast.show(ToastEnum.success,'Benchmark removed from Authority');
    },
    err => {
      this.svcUtil.showLoading = false;
      loggerCallback(LogLevel.Error, err);
      this.svcToast.show(ToastEnum.danger,'Unable to remove the Benchmark. Please try again.');
    });
  }

  resetFormStatus() {
    Object.keys(this.authorityForm.controls).forEach((key) => {
      const control = this.authorityForm.controls[key];
      control.markAsPristine();
      control.markAsUntouched();
    });
  }

  saveChanges() {
    if(!this.currentAuthority)
      return;

    this.svcUtil.showLoading = true;
    if(this.currentAuthority.id === 0) {
      alert('Authority creation not yet available!');
      /* create
      this.svcData.createAuthority(this.currentAuthority).subscribe(rec => {
        this.updateCollection(0, rec);
        this.svcToast.show(ToastEnum.success,'New Customer created successfully!');
        this.resetFormStatus();
      },
      err => {
        this.svcUtil.showLoading = false;
        loggerCallback(LogLevel.Error, err);
        this.svcToast.show(ToastEnum.danger,'Error creating Customer! Please try again.');
      },
      () => this.svcUtil.showLoading = false
      );
      */
    } else if(this.currentAuthority.id > 0) {
      // update
      this.svcData.updateAuthority(this.currentAuthority.id, this.currentAuthority).subscribe(rec => {
        this.updateCollection(rec.id, rec);
        this.svcToast.show(ToastEnum.success,'Authority updated successfully!');
        this.resetFormStatus();
      },
      err => {
        this.svcUtil.showLoading = false;
        loggerCallback(LogLevel.Error, err);
        this.svcToast.show(ToastEnum.danger,'Error updating Authority: ' + err.message);
      },
      () => this.svcUtil.showLoading = false
      );
    }
  }

  selectAll(e: any) {
    e?.target?.select();
  }
  
  setActiveAsOfDate(e: NgbDate | null) {
    if(!this.currentAuthority)
      return;
    this.currentAuthority.activeAsOf = UtilService.GetCaseDate(e);
  }

  showResults() {
    const opts = Object.assign({}, this.modalOptions);
    opts.size= 'lg';
    this.svcModal.open(this.basicModal, opts);
  }

  sortVMs(a:BenchmarkDataItemVM,b:BenchmarkDataItemVM){
    if(a.dataYear<b.dataYear)
      return 1;
    if(a.dataYear>b.dataYear)
      return -1;
    if(a.availableForService<b.availableForService)
      return -1;
    if(a.availableForService>b.availableForService)
      return 1;
    return 0;
  }

  subscribeToData() {
    this.svcUtil.showLoading = true;
    // listen for loading of user info
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(data => {
      this.isAdmin = !!data.isAdmin;
      this.isManager = !!data.isManager;
      this.canEdit = this.isAdmin||this.isManager;
      this.currentUser = data;
      this.isDeveloper = location.host.toLowerCase().startsWith("localhost");
      this.route.params.pipe(takeUntil(this.destroyed$)).subscribe(data => {
        this.loadPrerequisites();
      });
    });
  }

  public trackById(index: number, item: AuthorityUser) {
    return item.userId;
  }

  trackingFieldNameChanged() {
    this.newTracking.trackingFieldType = '';
    this.newTracking.referenceFieldName = '';
    this.newTracking.unitsFromReference = 0;
    this.newTracking.unitsType = '';
    if(!this.currentAuthority)
      return;
    const f = this.currentAuthority.trackingDetails.filter(d => d.trackingFieldName===this.newTracking.trackingFieldName);
    if(f.length){
      const n = this.newTracking.trackingFieldName;
      this.newTracking.trackingFieldName = '';
      alert(`The Tracking Field Name "${n}" is already in use. Use a different name or edit the exising configuration.\n\nThe form will be reset.`);
    }
  }
  

  updateCollection(id:number,rec:Authority) {
    let ndx = -1;
    this.allAuthorities.forEach((item, index) => {
      if (item.id === id) {
        this.allAuthorities[index] = rec;
        ndx = index;
      }
    });

    this.currentAuthority = ndx > -1 ? this.allAuthorities[ndx] : null;
    this.authorityChange();
    
    this.allAuthorities.sort(UtilService.SortByName);
  }
  
  uploadGroups() {
    this.isError = false;
    const files = this.groupsFile?.nativeElement.files;
    const f:File = files && files.length ? files[0] : undefined;
    if(!files||!f) {
      this.svcToast.show(ToastEnum.danger,'No file selected');
      return;
    }

    if(!f.name.toLowerCase().endsWith('.csv')) {
      this.isError = true;
      this.loadTitle = "Document Type Error";
      this.loadMessage = "Invalid document type. Only CSV is supported at this time.";
      this.svcModal.open(this.basicModal, this.modalOptions);
      return;
    }
      
    this.svcUtil.showLoading = true;
    this.svcData.uploadAuthorityPayorGroupExclusions(f).subscribe(
      { 
        next: (data: PayorGroupResponse) => {
          this.loadMessage = data.message;
          this.loadTitle = "Upload Complete";
          console.log(data);
          this.showResults();
          if(this.groupsFile)
            this.groupsFile.nativeElement.value = '';

          // update current payor object w/new groups
          if(!!this.currentPayor){
            const ndx = this.allPayors.findIndex(d=>d.id===this.currentPayor!.id);
            if(ndx>-1) {
              this.svcData.loadPayorById(this.currentPayor.id).subscribe(rec => {
                this.allPayors.splice(ndx,1,rec);
                this.currentPayor = rec;
              },
              err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
              () => this.svcUtil.showLoading = false
              );
            } else {
              this.svcUtil.showLoading = false;
            }
          }
          this.showUpload = false;
        },
        error: (err) => {
            this.svcUtil.showLoading = false;
            this.isError = true;
            if(typeof err.error == 'object')
              this.loadMessage = err.error.title;
            else
              this.loadMessage = err.error ?? err.message ?? err.statusText ?? err.toString();
            this.loadTitle = "Upload Failed";
            this.showResults();
            if(this.groupsFile)
              this.groupsFile.nativeElement.value = '';
            this.showUpload = false;
        },
        complete: () => this.svcUtil.showLoading = false
      }
    );
  }

  viewFile(e:any) {
    if(!this.currentAuthority||!e||!(e instanceof CaseFileVM))
      return;
    const f = e as CaseFileVM;
    this.svcData.downloadEntityFile(this.currentAuthority.id, f.blobName, this.currentAuthority).pipe(take(1)).subscribe(res => {
      const fileURL = URL.createObjectURL(res);
      window.open(fileURL, '_blank');
    });
  }
}
