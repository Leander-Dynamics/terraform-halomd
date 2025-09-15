import { Template } from '@angular/compiler/src/render3/r3_ast';
import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { DataTableDirective } from 'angular-datatables';
import { BehaviorSubject, combineLatest, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AppUser } from 'src/app/model/app-user';
import { Authority } from 'src/app/model/authority';
import { Customer } from 'src/app/model/customer';
import { Entity } from 'src/app/model/entity';
import { MasterDataException, MasterDataExceptionType } from 'src/app/model/master-data-exception';
import { Payor } from 'src/app/model/payor';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'app-manage-exceptions',
  templateUrl: './manage-exceptions.component.html',
  styleUrls: ['./manage-exceptions.component.css']
})
export class ManageExceptionsComponent implements OnDestroy, OnInit {
  @ViewChild(DataTableDirective, {static: false})
  dtElement: DataTableDirective | undefined;
  @ViewChild('payorModal', {static:false})
  payorModal: Template | undefined;

  destroyed$ = new Subject<void>();
  allAuthorities:Authority[] = [];
  allCustomers:Customer[] = [];
  allPayors:Payor[] = [];
  
  currentUser:AppUser = new AppUser();
  
  dtOptions: DataTables.Settings = {};
  dtTrigger: Subject<any> = new Subject<any>();

  isAdmin = false;
  isManager = false;
  modalOptions:NgbModalOptions | undefined;
  NSARequestEmail = '';
  parentId:number | null = null;
  payorName = '';
  records$ = new BehaviorSubject<MasterDataException[]>([]);
  sendNSARequests = false;
  //records:MasterDataException[] = [];
  
  constructor(private svcModal: NgbModal, private svcData: CaseDataService, 
    private svcToast: ToastService, private svcAuth:AuthService, 
    private svcUtil: UtilService) { 
      this.modalOptions = {
        backdrop:'static',
        backdropClass:'customBackdrop',
        keyboard: false,
      }
    }

  ngOnInit(): void { 
    // when returning to this screen, show any pending alerts
    this.svcToast.resetAlerts();
    for(let a of UtilService.PendingAlerts)
      this.svcToast.showAlert(a.level,a.message);
    UtilService.PendingAlerts.length = 0;

    // paging: false
    this.dtOptions = {
      order: [
        [4, 'asc']
      ],
      pagingType: 'full_numbers',
      pageLength: 50,
      columnDefs: [
        { orderable:false, targets:0, width:'2rem',
          render:(data,type,full,meta)=> {
          return full.isResolved ? '<i class="arb-pointer text-success fs-3 bi bi-check-square"></i>':'<i class="arb-pointer text-success fs-3 bi bi-square"></i>';
          }
        },

        { orderable: true, targets:1, width:'10rem', 
          render: (data,type,full,meta)=> {
            return MasterDataExceptionType[full.exceptionType];
          }
        },

        { orderable: true, data:'createdOn', targets:2, width:'6rem',
          type:'date',
          render:(data,type,full,meta) => {
            return !!full.createdOn ? full.createdOn.toLocaleString() : '';
          }
        },

        { orderable: true, data:'updatedOn', targets:3, width:'6rem',
          type:'date',
          render:(data,type,full,meta) => {
            return !!full.updatedOn ? full.updatedOn.toLocaleString() : '';
          }
        },

        { orderable: true, data:'updatedBy',targets:4, width:'11rem'},

        { orderable: true, data:'message', targets:5}
      ],
      data:[],
      rowCallback: (row:Node,data:any[]|Object,index:number) => {
        const self = this;
        // Unbind first in order to avoid any duplicate handler
        // (see https://github.com/l-lin/angular-datatables/issues/87)
        // Note: In newer jQuery v3 versions, `unbind` and `bind` are 
        // deprecated in favor of `off` and `on`
        $('td:nth-child(1)', row).off('click');
        $('td:nth-child(1)', row).on('click', () => {
          self.fixData(data);
        });
        return row;
      }
    };

    this.subScribeToData();
    this.loadPrerequisites();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
    this.dtTrigger.complete();
    this.records$.complete();
  }

  addEntity(npi:string,customer:string,obj:MasterDataException) {
    const c = this.allCustomers.find(v=>v.name.toLowerCase()===customer.toLowerCase());
    if(!c) {
      this.svcToast.show(ToastEnum.danger,`The Customer "${customer}" could not be located. Cannot add an Entity to an unknown Customer!`);
      return;
    }
    const t = c.entities.find(v=>v.NPINumber===npi);
    if(!!t) {
      this.svcToast.show(ToastEnum.warning,`${customer} appears to already have an Entity with NPI number ${npi}`);
      if(!confirm('Mark this Data Exception as resolved?'))
        return;
      this.toggleResolved(obj);
    }

    const name = prompt('Required: Enter a name for this Entity i.e. Entity Name. Leave blank to cancel this operation.');
    if(!name) {
      this.svcToast.show(ToastEnum.warning,'Operation cancelled.');
      return;
    }
    const taxId = prompt('Required: Enter the Owner Tax ID for this Entity. (Leave blank to cancel this operation.)');
    if(!taxId) {
      this.svcToast.show(ToastEnum.warning,'Operation cancelled.');
      return;
    }
    const ownerName = prompt('Optional: Enter the Owner Name for this Entity.');
    
    // Add the new entity
    const nt = new Entity({customerId:c.id,name:name,NPINumber:npi,ownerName:ownerName,ownerTaxId:taxId});
    this.svcUtil.showLoading = true;
    this.svcData.createEntity(nt).subscribe(data =>{
      this.toggleResolved(obj);
      this.svcToast.show(ToastEnum.success,`Entity successfuly added to ${name}`);
    },
    err => this.displayError(err)
    );
  }

  displayError(err:any) {
    this.svcUtil.showLoading = false;
    let msg = '';
    if(err.status == '401') {
      msg='Unauthorized operation.';
    } else {
      msg = err.message ?? err.statusText ?? err.toString();
    }
    this.svcToast.showAlert(ToastEnum.danger, msg);
  }

  fixData(rec:any) {
    if(!this.dtElement?.dtInstance)
      return;
    
    const obj = rec as MasterDataException;

    if(obj.isResolved){
      if(!confirm('That issue was already marked as resolved. Undo this change?'))
        return;
      this.toggleResolved(obj);
      return;
    }

    if(obj.exceptionType === MasterDataExceptionType.MDMissingPayor) {
      if(confirm('Fix this missing Payor issue now?')) {
        this.fixMissingPayor(obj);
        return;
      }
    }

    if(obj.exceptionType === MasterDataExceptionType.UnknownEntity){
      if(confirm('Fix this Unknown Entity issue now by adding the Entity to the Customer?')) {
        this.fixUnknownEntity(obj);
        return;
      }
    }

    if(!confirm('Mark this issue as resolved?'))
      return;
    
    this.toggleResolved(obj);
  }

  fixUnknownEntity(mde:MasterDataException) {
    // EntityNPI 1134737216 does not match an Entity for Customer MPOWERHealth. Claim not added.
    const r1 = /EntityNPI (\d{10})/g;
    const r2 = /Customer (.*)\. Claim/g;
    const d = mde.message;
    let matches = d.matchAll(r1);
    let nt = '';
    let cust = '';
    for(const match of matches) {
      nt = match.length===2 ? match[1] : '';
    }
    matches = d.matchAll(r2);
    for(const match of matches) {
      cust = match.length===2 ? match[1] : '';
    }
    if(!!nt && !!cust)
      this.addEntity(nt,cust,mde);
    else
      this.svcToast.show(ToastEnum.danger,'Unable to decode the EntityNPI and/or Customer from the Message. You may need to add this Entity manually.');
  }

  payorParentChanged() {
    if(this.parentId == null || this.parentId > 0){
      this.NSARequestEmail='noreply@halomd.com';
      this.sendNSARequests = false;
    }
  }

  fixMissingPayor(mde:MasterDataException) {
    this.payorName = mde.data.trim();
    this.parentId = null;
    this.NSARequestEmail='noreply@mpowerhealth.com';
    this.sendNSARequests = false;
    this.svcModal.open(this.payorModal, this.modalOptions).result.then(data => {
      if(this.payorName.length < 3)
        return;
      this.svcUtil.showLoading = true;
      this.svcData.createPayor(new Payor({name: this.payorName.trim(), NSARequestEmail:this.NSARequestEmail.trim(), parentId:this.parentId, sendNSARequests:this.sendNSARequests})).subscribe(rec => {
        this.allPayors.push(rec);
        this.allPayors.sort(UtilService.SortByName);
        this.svcToast.show(ToastEnum.success,'Payor created successfully!');
        this.toggleResolved(mde);
      },
      err => {
        this.svcUtil.showLoading = false;
        if(!err)
          return;
        this.svcToast.show(ToastEnum.danger,'Error creating Payor! Please try again.');
      },
      () => this.svcUtil.showLoading = false
      );
    },
    err => console.warn('Add Payor canceled'));
  }

  loadPrerequisites() {
    this.svcUtil.showLoading = true;
    const authorities$ = this.svcData.loadAuthorities();
    const mde$ = this.svcData.getMasterDataExceptions(true);
    const payors$ = this.svcData.loadPayors();
    const customers$ = this.svcData.loadCustomers();
    
    combineLatest([authorities$,mde$,payors$,customers$]).subscribe(
      ([authorities,mde,payors,customers]) => {
      
      this.allAuthorities = authorities;
      this.allAuthorities.sort(UtilService.SortByName);
      this.allPayors = payors;
      this.allPayors.sort(UtilService.SortByName);
      this.allCustomers = customers;
      this.allCustomers.sort(UtilService.SortByName);
      this.dtOptions.data = mde;
      this.dtTrigger.next();
    },
    err => {
      this.svcToast.show(ToastEnum.danger,'Failed to load all of the dropdown box values.');
      this.svcUtil.showLoading = false;
    },
    () => this.svcUtil.showLoading = false);
  }

  reload() {
    // refresh table row
    this.dtElement?.dtInstance.then((dtInstance: DataTables.Api) => {
      this.svcUtil.showLoading = true;
      this.svcData.getMasterDataExceptions(false).subscribe(data => { 
        dtInstance.clear();
        dtInstance.destroy();
        this.dtOptions.data = data;
        // Call the dtTrigger to rerender again
        this.dtTrigger.next();
      },
      err => this.displayError(err),
      () => this.svcUtil.showLoading=false
    )
    });
  }

  selectAll(e:any) {
    e?.target?.select();
  }

  subScribeToData() {
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(
      data => {
        if(data.email) {
          this.currentUser = data;
          this.isAdmin = !!this.currentUser.isAdmin;
          this.isManager = !!this.currentUser.isManager;
        }
      }
    );
  }

  toggleResolved(obj:MasterDataException){
    this.svcUtil.showLoading=true;
    obj.isResolved = !obj.isResolved;
    this.svcData.updateMasterDataException(obj.id, obj).subscribe(
      result => {
        // refresh data object
        const ndx = this.dtOptions.data!.findIndex(g=>g.id===result.id);
        this.dtOptions.data![ndx] = result;
        
        // refresh table row
        this.dtElement?.dtInstance.then((dtInstance: DataTables.Api) => {
          const rows:any = dtInstance.row((idx:any,data:any,node:any) => {
            return data.id === obj.id;
          });
          const r = dtInstance.row(rows[0]);
          r.data(result);
          r.invalidate().draw();
        });
      },
      err=> this.displayError(err),
      () => this.svcUtil.showLoading=false
    );
  }
}
