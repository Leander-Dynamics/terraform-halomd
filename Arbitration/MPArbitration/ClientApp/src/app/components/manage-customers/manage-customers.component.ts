import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { combineLatest, Subject } from 'rxjs';
import { AppUser } from 'src/app/model/app-user';
import { Customer } from 'src/app/model/customer';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';
import { takeUntil } from 'rxjs/operators';
import { NgForm } from '@angular/forms';
import { Authority } from 'src/app/model/authority';
import { ActivatedRoute, Router } from '@angular/router';
import { Entity } from 'src/app/model/entity';

@Component({
  selector: 'app-manage-customers',
  templateUrl: './manage-customers.component.html',
  styleUrls: ['./manage-customers.component.css']
})
export class ManageCustomersComponent implements OnInit, OnDestroy {
  @ViewChild('customerForm', { static: false }) 
  customerForm!: NgForm;
  allAuthorities:Authority[] = [];
  allEntities:Entity[] = [];
  allCustomers:Customer[] = [];
  canEdit = true;
  orig:Customer | null = null;
  currentCustomer:Customer | null = null;
  currentUser:AppUser | undefined;
  customerName = '';
  destroyed$ = new Subject<void>();
  hideEntities = true;
  isAdmin = false;
  isManager = false;
  newEntity = new Entity();
  statKeys = new Array<string>();

  constructor(private svcData:CaseDataService, private svcToast:ToastService, 
    private svcUtil:UtilService, private svcAuth: AuthService, 
    private router:Router, private route:ActivatedRoute) { }

  ngOnInit(): void {
    this.subscribeToData();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
  }

  addCustomer() {
    let c = this.allCustomers.find(d=>d.id===0);
    if(c) {
      this.currentCustomer = c;
    } else {
      c = new Customer();
      c.name = "(new)";
      this.allCustomers.push(c);
      this.currentCustomer = c;
    }
  }

  cancelChanges() {
    if(!this.currentCustomer)
      return;
    if(!confirm('Are you sure you want to cancel?'))
      return;
    if(this.currentCustomer.id===0) {
      const i = this.allCustomers.findIndex(d => d.id === 0);
      this.allCustomers.splice(i,1);
      this.currentCustomer = null;
    } else if(this.orig) {
      this.currentCustomer.isActive = this.orig.isActive;
      this.currentCustomer.EHRSystem = this.orig.EHRSystem;
      this.currentCustomer.defaultAuthority = this.orig.defaultAuthority;
    }
    this.newEntity = new Entity();
    this.resetFormStatus();
  }

  customerSelected() {
    this.orig = null;
    this.newEntity = new Entity();
    this.allEntities = [];
    this.statKeys = [];

    if(!!this.currentCustomer){
      if(!!this.currentCustomer.stats) {      
        this.statKeys = Object.keys(this.currentCustomer.stats); //.filter(value => typeof value === 'string');
        this.statKeys.sort(UtilService.SortSimple);
      }
      this.orig = new Customer(this.currentCustomer);
      this.allEntities = this.currentCustomer.entities
    }   
  }

  deleteEntity(g:Entity) {
    if(!this.currentCustomer)
      return;
    if(!confirm('Are you sure you want to remove this Entity from the Customer? This will not affect existing claims or Payor Exclusions.'))
      return;

    this.svcUtil.showLoading = true;
    this.svcData.deleteEntity(this.currentCustomer,g).subscribe(data => {
      const n=this.currentCustomer!.entities.findIndex(d=>d.id===g.id);
        if(n > -1){
          this.currentCustomer!.entities.splice(n,1);
          this.allEntities = this.currentCustomer?.entities ?? [];
        }
      this.svcToast.show(ToastEnum.success,'Entity successfully deleted');
    },
    err => this. displayError(err),
    () => this.svcUtil.showLoading = false
    );
  }

  displayError(err:any) {
    this.svcUtil.showLoading = false;
    let msg = '';
    if(err.status == '401') {
      msg='Unauthorized operation.';
    } else {
      msg = err.error?.toString() ?? err.message ?? err.statusText ?? err.toString();
    }
    this.svcToast.showAlert(ToastEnum.danger, msg);
  }

  entityChange(j:Entity,isNew:boolean = false) {
    if(!this.currentCustomer || !j.name || !j.NPINumber)
      return;

    if(j.NPINumber.length!=10){
      this.svcToast.show(ToastEnum.warning,'NPI Number must be 10 digits');
      return;
    }

    if(!isNew) {
      const orig = new Entity(j);
      this.svcUtil.showLoading = true;
      this.svcData.updateEntity(j.id,j).subscribe(data => {
        const n=this.currentCustomer!.entities.findIndex(d=>d.id===data.id);
        if(n>-1){
          this.currentCustomer!.entities[n]=data;
        }
      },
      err => this.displayError(err),
      () => {
          this.allEntities = this.currentCustomer?.entities ?? [];
          this.svcUtil.showLoading = false;
      });
    } else {
      if(!!this.currentCustomer.entities.find(d=>d.NPINumber===j.name)){
        this.svcToast.show(ToastEnum.danger,'That NPI number is already in the Entity list!');
        return;
      }
      this.svcUtil.showLoading = true;
      j.customerId = this.currentCustomer.id;
      this.svcData.createEntity(j).subscribe(data => {
        if(!!data && data.id>0){
          this.currentCustomer!.entities.push(data);
          this.allEntities = this.currentCustomer!.entities;
          this.svcToast.show(ToastEnum.success,`Entity successfully added to ${this.currentCustomer?.name}`);
          this.newEntity = new Entity();
        } else {
          this.svcToast.show(ToastEnum.success,`Entity successfully added to ${this.currentCustomer?.name}`);
        }
      },
      err => this.displayError(err),
      () => this.svcUtil.showLoading = false
      );
      
    }
  }

  getStat(key:string){
    if(!this.currentCustomer || !this.currentCustomer.stats)
      return 0;
    return (this.currentCustomer!.stats as any)[key];
  }

  loadPrerequisites() {
    const auth$ = this.svcData.loadAuthorities();
    const cust$ = this.svcData.loadCustomers();
    combineLatest([auth$,cust$]).subscribe(([auth,cust]) => {
      this.allAuthorities = auth.filter(d=>!d.key || d.key.toLowerCase()!=='nsa');
      this.allAuthorities.sort(UtilService.SortByName);
      this.allCustomers = cust;
      this.allCustomers.sort(UtilService.SortByName);
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.showAlert(ToastEnum.danger, 'Unable to load the list of Customers');
      },
      () => this.svcUtil.showLoading = false
    );
  }

  isNewEntityValid() {
    return !!this.newEntity.name || !!this.newEntity.NPINumber ? this.newEntity.isValid : true;
  }

  onSubmit(): boolean {
    // see if this would create a duplicate Entity
    if(!!this.currentCustomer&&this.newEntity.isValid) {
      if(!!this.currentCustomer.entities.find(d=>d.name.toLowerCase()===this.newEntity.name.toLowerCase() || d.NPINumber.toLowerCase()===this.newEntity.NPINumber.toLowerCase())) {
        this.svcToast.show(ToastEnum.danger,'This would create a duplicate Entity for the current Customer.');
        return false;
      }
    }

    if (!this.customerForm?.valid) 
      return false;
    if(!this.isNewEntityValid())
      return false;
    this.saveChanges();
    return true;
  }

  resetFormStatus() {
    this.newEntity = new Entity();
    Object.keys(this.customerForm.controls).forEach((key) => {
      const control = this.customerForm.controls[key];
      control.markAsPristine();
      control.markAsUntouched();
    });
  }

  saveChanges() {
    if(!this.currentCustomer)
      return;

    if(this.newEntity.isValid) {
      this.currentCustomer.entities.push(this.newEntity);
    }

    this.svcUtil.showLoading = true;
    if(this.currentCustomer.id === 0) {
      // create
      this.svcData.createCustomer(this.currentCustomer).subscribe(rec => {
        this.updateCustomerCollection(0, rec);
        this.svcToast.show(ToastEnum.success,'New Customer created successfully!');
        this.resetFormStatus();
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger,'Error creating Customer! Please try again.');
      },
      () => this.svcUtil.showLoading = false
      );

    } else if(this.currentCustomer.id > 0) {
      // update
      this.svcData.updateCustomer(this.currentCustomer.id, this.currentCustomer).subscribe(rec => {
        this.updateCustomerCollection(rec.id, rec);
        this.svcToast.show(ToastEnum.success,'Customer updated successfully!');
        this.resetFormStatus();
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger,'Error updating Customer: ' + err.message);
      },
      () => this.svcUtil.showLoading = false
      );
    }
  }
  
  selectAll(e:any) {
    e?.target?.select();
  }

  subscribeToData() {
    // listen for loading of user info
    this.svcUtil.showLoading = true;
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(data => {
      this.isManager = !!data.isManager;
      this.isAdmin = !!data.isAdmin;

      // Edit is now restricted purely to Admins because:
      // 1. EHR imports use the common customer name, not an ID
      //   This was a conscious choice to make the system easier to maintain
      // 2. Changing the name does not cascade the change to existing Cases.
      //   Because these records need a chain of custody, changing this value on
      //   all existing records should not be done whimsically.
      this.canEdit = this.isAdmin||this.isManager; 
      if(!this.canEdit){
        UtilService.PendingAlerts.push({level:ToastEnum.danger, message:'Insufficient privileges for managing the Customers list.'});
        this.router.navigate(['']);
        return;
      }
      this.currentUser = data;
      
      this.route.params.pipe(takeUntil(this.destroyed$)).subscribe(data => {
        this.loadPrerequisites();
      });
    });
  }

  public trackById(index: number, item: Entity) {
    return item.id;
  }

  updateCustomerCollection(id:number,rec:Customer) {
    let ndx = -1;
    this.allCustomers.forEach((item, index) => {
      if (item.id === id) {
        this.allCustomers[index] = rec;
        ndx = index;
      }
    });

    this.currentCustomer = ndx > -1 ? this.allCustomers[ndx] : null;
    this.customerSelected();
    
    this.allCustomers.sort(UtilService.SortByName);
  }
}
