import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { combineLatest, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AppHealth, AppHealthDetail } from 'src/app/model/app-health';
import { AppSettings } from 'src/app/model/app-settings';
import { AppUser } from 'src/app/model/app-user';
import { Customer } from 'src/app/model/customer';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'app-app-health',
  templateUrl: './app-health.component.html',
  styleUrls: ['./app-health.component.css']
})
export class AppHealthComponent implements OnInit, OnDestroy {
  //allStateIneligibilityActions:Array<string> = []; //['Batching','Denial','Duplicate','High Reimbursement','Incorrect Claim Data','NSA','Other Payor is Primary','Out of State Policy','Patient Elected OON Services','Timing'];
  allCustomers: Customer[] = [];
  customer:Customer|null = null;
  metrics = new AppHealth();
  currentUser:AppUser = new AppUser();
  destroyed$ = new Subject<void>();
  isAdmin = false;
  isManager = false;
  isDeveloper = false;
  nsia = '';
  settings:AppSettings = new AppSettings();
  
  constructor(private svcData:CaseDataService, private svcToast:ToastService, 
    private svcUtil:UtilService, private svcAuth:AuthService) { }

  ngOnInit(): void {
    this.loadPrerequisites();

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

  ngOnDestroy() {
    this.destroyed$.next();  
    this.destroyed$.complete();
  }

  addSIA() {
    if(!this.canAddSIA())
      return; 
    if(!confirm(`Add "${this.nsia}" as a new State Ineligibility Action?`))
      return;
    this.svcUtil.showLoading = true;
    const a = this.settings.stateActionList;
    a.push(this.nsia);
    this.settings.stateActionList = a;
    this.svcData.updateAppSettings(this.settings)
      .subscribe(data => {
        this.settings = data;
        this.nsia = '';
        this.svcToast.show(ToastEnum.success,'State Ineligibility Action added successfully!');
      },
        err => this.handleServiceErr(err),
        () => this.svcUtil.showLoading = false
      );
  }

  canAddSIA() {
    if(!this.nsia || this.settings.stateActionList.indexOf(this.nsia)>-1)
      return false;
    if(this.nsia.startsWith(' ') || this.nsia.endsWith(' '))
      return false;
    return true;
  }

  fixSomething(s:string) {
    if(s==='dupes')
      this.svcToast.show(ToastEnum.info,'This feature is no longer available');
    if(s==='notifications')
      this.regenerateNotificationPDFs();
  }

  regenerateNotificationPDFs() {
    this.svcUtil.showLoading = true;
    this.svcData.fixNotifications().subscribe(data => {
      console.log(data);
      this.svcToast.showAlert(ToastEnum.info,'PDFs regenerated.');
    },
    err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast), 
    () => this.svcUtil.showLoading = false);
  }
  /*
  cleanDupes() {
    this.svcUtil.showLoading = true;
    this.svcData.cleanDupes().subscribe(data => {
      console.log(data);
      this.svcToast.showAlert(ToastEnum.info,'Merge complete. Check console for details.');
    },
    err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast), 
    () => this.svcUtil.showLoading = false);
  }
  */
  getHealthItems(s:string):boolean {
    if(!s)
      return false;

    this.svcUtil.showLoading = true;
    this.svcData.getHealthItems(s).subscribe(data => {
      if(!data || !data.length) {
        this.svcToast.show(ToastEnum.info,'No records matched the query');
        return;
      }
      this.exportHealthItems(data);
    },
    err => this.handleServiceErr(err),
    () => this.svcUtil.showLoading = false
    );

    return false;
  }

  exportHealthItems(data:AppHealthDetail[],filename:string = 'Arbit-AppHealth') {
    UtilService.DownloadObjects(data,filename,false);
  }

  handleServiceErr(err:any) {
    let msg = err instanceof HttpErrorResponse ? err.error.title : null;
    msg = msg ?? err.error ?? err.message ?? err.statusText ?? err;
    this.svcUtil.showLoading = false;
    this.svcToast.showAlert(ToastEnum.danger, msg);
  }

  customerChange() {
    this.svcUtil.showLoading = true;
    this.svcData.getSystemHealth(this.customer).subscribe(data => {
      this.metrics = data;
    },
    err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
    () => this.svcUtil.showLoading = false);
  }

  loadPrerequisites() {
    this.svcUtil.showLoading = true;
    const health$ = this.svcData.getSystemHealth();
    const settings$ = this.svcData.getAppSettings();
    const customers$ = this.svcData.loadCustomers();

    combineLatest([health$,settings$,customers$]).subscribe(
      ([health,settings,customers]) => {
        this.metrics = health;
        this.settings = settings;
        this.allCustomers = customers;
        this.allCustomers.sort(UtilService.SortByName);
      },
      err => this.handleServiceErr(err),
      () => this.svcUtil.showLoading = false
    );
  }

  refresh() {
    this.loadPrerequisites();
  }
}
