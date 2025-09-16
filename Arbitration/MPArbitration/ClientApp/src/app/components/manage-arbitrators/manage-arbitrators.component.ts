import { Template } from '@angular/compiler/src/render3/r3_ast';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, combineLatest, forkJoin, Subject } from 'rxjs';
import { AppUser } from 'src/app/model/app-user';
import { Arbitrator } from 'src/app/model/arbitrator';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';
import { takeUntil } from 'rxjs/operators';
import { ToastEnum } from 'src/app/model/toast-enum';
import { BaseFee } from 'src/app/model/base-fee';
import { AddFeeComponent } from '../add-fee/add-fee.component';
import { ArbitratorFee } from 'src/app/model/arbitrator-fee';
import { ArbitratorType } from 'src/app/model/arbitrator-type-enum';
import { IKeyId } from 'src/app/model/iname';
import { AddArbitratorComponent } from '../add-arbitrator/add-arbitrator.component';

@Component({
  selector: 'app-manage-arbitrators',
  templateUrl: './manage-arbitrators.component.html',
  styleUrls: ['./manage-arbitrators.component.css']
})
export class ManageArbitratorsComponent implements OnDestroy, OnInit {
  //@ViewChild('addDialog') addDialog: Template | undefined;
  @ViewChild('arbitratorsFile', {static: false}) arbitratorsFile: ElementRef | undefined;
  @ViewChild('loadResult') basicModal: Template | undefined;
  @ViewChild('uploadDialog') uploadDialog: Template | undefined;
  allArbitrators = new Array<Arbitrator>();
  //allArbRoles = ['Arbitrator','Mediator'];
  allArbTypes = new Array<IKeyId>();
  allFees$ = new BehaviorSubject<BaseFee[]>([]);
  allServices: string[] = [];
  canEdit = false;
  currentUser:AppUser | undefined;
  currentArb: Arbitrator | undefined;
  destroyed$ = new Subject<void>();
  fileSelected = false;
  isAdmin = false;
  isEditing = false;
  isError = false;
  isManager = false;
  isNegotiator = false;
  isReporter = false;
  loadTitle = '';
  loadMessage = 'Upload complete';
  modalOptions:NgbModalOptions | undefined;
  newArb = new Arbitrator();
  //selectedRoles = new Array<string>();
  selectedServices = new Array<string>();

  ArbitratorType = ArbitratorType;

  constructor(private svcData:CaseDataService, 
              private svcToast:ToastService,
              private svcUtil:UtilService,
              private route: ActivatedRoute,
              private svcModal: NgbModal,
              private svcAuth: AuthService,
              private router:Router) { 

    this.modalOptions = {
      backdrop:'static',
      backdropClass:'customBackdrop',
      keyboard: false,
    };

    this.allArbTypes = Object.values(ArbitratorType).filter(value => typeof value === 'string').map(key => { 
      const result = (key as string).split(/(?=[A-Z][a-z])/);
      return {id: (<any>ArbitratorType)[key] as number, key: result.join(' ') }; 
    });
  }

  ngOnInit(): void {
    this.subscribeToData();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
    this.allFees$.complete();
  }

  /*
  addFee(e:any){
    if(!this.currentArb||!this.currentArb.id)
      return;

    const ref = this.svcModal.open(AddFeeComponent, {...this.modalOptions,size:'md'});
    ref.componentInstance.feeType = 'Arbitrator';
    ref.result.then(data => {
      this.svcUtil.showLoading = true;
      const fee = new ArbitratorFee(data);
      fee.arbitratorId = this.currentArb!.id;
      this.svcData.createArbitratorFee(this.currentArb!, fee).subscribe(data => {
        this.currentArb!.fees.push(data);
        this.allFees$.next(this.currentArb!.fees);
      },
      err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
      () => this.svcUtil.showLoading = false);
    },
    reason => this.svcToast.show(ToastEnum.info, 'Add Fee canceled'));
  }
  */
  
  addRecord() {
    let newArb = new Arbitrator();
    newArb.isActive = true;
    this.isEditing = false;
    const i = this.svcModal.open(AddArbitratorComponent, {...this.modalOptions,size:'lg'});
    i.componentInstance.isEditing = false;
    i.componentInstance.allServices = this.allServices;
    i.componentInstance.currentArb = newArb;
    i.componentInstance.canEditFees = this.canEdit;
    i.componentInstance.canAddFees = true;
    i.componentInstance.feeType = 'Authority';
    i.result.then(data => {
      const a = new Arbitrator(newArb);
      this.svcUtil.showLoading = true;
      this.svcData.createArbitrator(newArb).subscribe(rec => {
        this.allArbitrators.push(rec);
        this.currentArb = rec;
        this.allArbitrators.sort(UtilService.SortByName);
        this.svcUtil.showLoading=false;
        this.svcToast.show(ToastEnum.success,'New Arbitrator created successfully!');
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger,'Error creating new Arbitrator! Please try again.');
      },
      () => this.svcUtil.showLoading = false
      );
    },
    err => console.warn('Add Arbitrator canceled'));
  }

  /*
  editFee(e:any) {
    if(!this.currentArb||isNaN(e))
      return;

    const f = this.currentArb.fees.find(v => v.id===e);
    if(!f)
      return;
    const ref = this.svcModal.open(AddFeeComponent, {...this.modalOptions,size:'md'});
    ref.componentInstance.fee = f as BaseFee;
    ref.componentInstance.feeType = 'Arbitrator';
    ref.result.then(data => {
      this.svcUtil.showLoading = true;
      this.svcData.updateArbitratorFee(this.currentArb!, f).subscribe(data => {
        const n = this.currentArb!.fees.findIndex(v => v.id===data.id);
        this.currentArb!.fees.splice(n,1,data);
        this.allFees$.next(this.currentArb!.fees);
      },
      err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
      () => this.svcUtil.showLoading = false);
    },
    reason => this.svcToast.show(ToastEnum.info, 'Add Fee canceled'));
  }
  */

  editRecord(v:Arbitrator) {
    if(!v)
      return;
    const orig = new Arbitrator(v);
    this.isEditing = true;
    this.currentArb = v;

    const i = this.svcModal.open(AddArbitratorComponent, {...this.modalOptions,size:'lg'});
    i.componentInstance.allServices = this.allServices;
    i.componentInstance.isEditing = true;
    i.componentInstance.currentArb = v;
    i.componentInstance.canEditFees = this.canEdit;
    i.componentInstance.canAddFees = true;
    i.componentInstance.feeType = 'Authority';

    i.result.then(data => {
      this.svcUtil.showLoading = true;
      const a = new Arbitrator(this.currentArb);
      this.svcData.updateArbitrator(a).subscribe(rec => {
        rec.fees = this.currentArb!.fees;
        const idx=this.allArbitrators.findIndex(d=>d.id===rec.id);
        if(idx>=0) {
          this.allArbitrators[idx] = rec;
          this.currentArb = this.allArbitrators[idx];
        }

        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.success,'Arbitrator updated successfully!');
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger,'Error updating Arbitrator! Please try again.');
      },
      () => this.svcUtil.showLoading = false
      );
    },
    reason => {
      const idx=this.allArbitrators.findIndex(d=>d.id===orig.id);
      if(idx>=0) {
        this.allArbitrators[idx] = orig;
        this.allArbitrators[idx].fees = this.currentArb!.fees;
      }
      console.warn('Edit Arbitrator canceled');
    });
  }
  
  fileSelectionChanged() {
    this.fileSelected = this.arbitratorsFile?.nativeElement.files.length ? true : false;
  }

  getFeeValues(fees:ArbitratorFee[]|undefined){
    if(!fees||!fees.length)
      return '$0.00';
    return fees.map(v=>`\$${v.feeAmount}.00`).join(';');
  }

  loadPrerequisites() {
    this.svcUtil.showLoading = true;
    const arbs$ = this.svcData.loadArbitrators(true);
    const services$ = this.svcData.loadServices();
    combineLatest([arbs$,services$])
    .subscribe(([arbs,services]) => {
      this.allArbitrators = arbs;
      this.allArbitrators.sort(UtilService.SortByName);
      this.allServices = [...new Set(services.map(item => item.serviceLine))];
      this.allServices.sort(UtilService.SortSimple);
      this.svcUtil.showLoading = false;
    },
    err => {
      this.svcUtil.showLoading = false;
      this.svcToast.show(ToastEnum.danger, err.message ?? err.toString());
    },
    () => this.svcUtil.showLoading = false
    );
  }

  selectAll(e:any) {
    e?.target?.select();
  }

  showResults() {
    this.svcModal.open(this.basicModal, this.modalOptions);
  }

  showUpload() {
    this.fileSelected = false;
    this.svcModal.open(this.uploadDialog, this.modalOptions).result.then(data => {
      this.svcUtil.showLoading = true;
      this.upload(data);
    },
    err => console.warn('Arbitrator upload canceled'));
    
  }

  subscribeToData() {
    // listen for loading of user info
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(data => {
      this.isManager = !!data.isManager;
      this.isAdmin = !!data.isAdmin;
      this.isNegotiator = !!data.isNegotiator;
      this.isReporter = !!data.isReporter;
      this.canEdit = !!data.isManager || !!data.isAdmin;
      if(!data.isActive) {
        UtilService.PendingAlerts.push({level:ToastEnum.danger, message:'Insufficient privileges for viewing the list of Arbitrators.'});
        this.router.navigate(['']);
        return;
      }
      this.currentUser = data;
      
      this.route.params.pipe(takeUntil(this.destroyed$)).subscribe(data => {
        this.loadPrerequisites();
      });
    });
  }

  toggleService(e:any) {
    const i = this.selectedServices.indexOf(e.target.value);
    if(i>=0)
      this.selectedServices.splice(i,1);
    if(e.target.checked)
      this.selectedServices.push(e.target.value);
    this.newArb.eliminateForServices = this.selectedServices.join(';');
  }
  /*
  toggleRole(e:any) {
    const i = this.selectedRoles.indexOf(e.target.value);
    if(i>=0)
      this.selectedRoles.splice(i,1);
    if(e.target.checked)
      this.selectedRoles.push(e.target.value);
    this.newArb.role = this.selectedRoles.join(';');
  }
  */
  upload(data:any) {
    
    this.isError = false;
    const files = data?.files;
    const f:File = files && files.length ? files[0] : undefined;

    if(f) {
      if(!f.name.toLowerCase().endsWith('.json')) {
        this.isError = true;
        this.loadTitle = "Document Type Error";
        this.loadMessage = "Invalid document type. Only .json is supported at this time.";
        this.svcModal.open(this.basicModal, this.modalOptions);
        return;
      }
      
      this.svcUtil.showLoading = true;
      this.svcData.uploadArbitrators(f).subscribe(
        { 
          next: (data: any) => {
            this.svcUtil.showLoading = false;
            this.loadMessage = data;
            this.loadTitle = "Upload Complete"
            this.showResults();
            if(this.arbitratorsFile)
              this.arbitratorsFile.nativeElement.value = '';
            },
            error: (err) => {
              this.svcUtil.showLoading = false;
              this.isError = true;
              this.loadMessage = err.message || err.toString();
              this.loadTitle = "Upload Failed";
              this.showResults();
              if(this.arbitratorsFile)
                this.arbitratorsFile.nativeElement.value = '';
            }
        }
      );
    } else {
      this.svcUtil.showLoading = false;
      this.isError = true;
      this.loadMessage = 'Unable to read file. Be sure you are sending a JSON file with the correct structure!';
      this.loadTitle = "Error";
      this.showResults();
    }
  }
}
