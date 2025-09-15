import { DatePipe } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  TemplateRef,
  ViewChild,
} from '@angular/core';
import { NgForm } from '@angular/forms';
import { LogLevel } from '@azure/msal-browser';
import {
  NgbDate,
  NgbDateAdapter,
  NgbDateParserFormatter,
  NgbInputDatepickerConfig,
  NgbModal,
  NgbModalOptions,
} from '@ng-bootstrap/ng-bootstrap';
import { DataTableDirective } from 'angular-datatables';
import { BehaviorSubject, combineLatest, Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';
import { loggerCallback } from 'src/app/app.module';
import { AppUser } from 'src/app/model/app-user';
import { ArbitrationCase } from 'src/app/model/arbitration-case';
import { ArbitrationCaseVM } from 'src/app/model/arbitration-case-vm';
import {
  ArbitrationResult,
  CMSCaseStatus,
} from 'src/app/model/arbitration-status-enum';
import { Authority } from 'src/app/model/authority';
import { CaseArbitrator } from 'src/app/model/case-arbitrator';
import {
  CaseWorkflowAction,
  CaseWorkflowParams,
} from 'src/app/model/case-workflow-params';
import {
  CustomDateParserFormatter,
  CustomNgbDateAdapter,
} from 'src/app/model/custom-date-handler';
import { Customer } from 'src/app/model/customer';
import { IKeyId } from 'src/app/model/iname';
import { Payor } from 'src/app/model/payor';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';
import { NgbOffcanvas } from '@ng-bootstrap/ng-bootstrap';
import { SummaryDialogComponent } from '../summary-dialog/summary-dialog.component';
import { Router } from '@angular/router';
import { environment } from 'src/environments/environment';
import {
  AuthorityDispute,
  AuthorityDisputeVM,
} from 'src/app/model/authority-dispute';

export enum ActiveSearchButton {
  None,
  ArbitratorRejections,
  BriefNeeded,
  CurrentCases,
  DueToAssignToday,
  DueToAssignTodayNSA,
  MyCurrentCases,
}

@Component({
  selector: 'app-case-search',
  templateUrl: './case-search.component.html',
  styleUrls: ['./case-search.component.css'],
  providers: [
    { provide: NgbDateAdapter, useClass: CustomNgbDateAdapter },
    { provide: NgbDateParserFormatter, useClass: CustomDateParserFormatter },
    NgbInputDatepickerConfig,
  ],
})
export class CaseSearchComponent implements OnDestroy, OnInit {
  @ViewChild('searchForm', { static: false }) searchForm!: NgForm;
  @ViewChild(DataTableDirective, { static: false })
  dtElement: DataTableDirective | undefined;

  readonly CMSCaseStatus = CMSCaseStatus;
  readonly ArbitrationResult = ArbitrationResult;
  ActiveSearchButton = ActiveSearchButton;
  activeSearchButton = ActiveSearchButton.None;
  allAuthorities: Authority[] = [];
  allCustomers: Customer[] = [];
  allPayors: Payor[] = [];
  allServices: { name: string; serviceLine: string }[] = [];
  allUsers: AppUser[] = [];
  arbitrator = '';
  assignedUser = '';
  assignmentDeadline: string | undefined;
  authority = '';
  authorityStatus = '';

  allAuthorityStatuses = new Array<string>();
  allNSAStatuses = new Array<string>();
  briefDate: string | undefined;

  currentUser: AppUser = new AppUser();
  DOB: string | undefined;
  requestDate: string | undefined;

  dtOptions: DataTables.Settings = {};
  dtTrigger: Subject<any> = new Subject<any>();

  dtDisputeOptions: DataTables.Settings = {};
  dtDisputeTrigger: Subject<any> = new Subject<any>();

  EHRNumber = '';
  customer = '';
  destroyed$ = new Subject<void>();
  includeClosed = false;
  includeInactive = false;
  isAdmin = false;
  isDev = false;
  isDisputeSearch = false;
  isManager = false;
  isNegotiator = false;
  modalOptions: NgbModalOptions | undefined;
  NSAStatus = '';
  origAuthorities: Authority[] = [];
  patientName = '';
  payorClaimNumber = '';
  payor: string | null = null;
  service: string | null = null;
  disputes$ = new BehaviorSubject<AuthorityDisputeVM[]>([]);
  disputes: AuthorityDisputeVM[] = [];
  records$ = new BehaviorSubject<ArbitrationCase[]>([]);
  records: ArbitrationCase[] = [];
  status: CMSCaseStatus = CMSCaseStatus.Search;
  statuses = new Array<IKeyId>();
  authNumber = '';

  get isLoading() {
    return this.svcUtil.showLoading$.getValue();
  }

  set isLoading(v: boolean) {
    this.svcUtil.showLoading = v;
  }

  constructor(
    private svcData: CaseDataService,
    private svcChangeDetection: ChangeDetectorRef,
    private svcToast: ToastService,
    private svcAuth: AuthService,
    private svcUtil: UtilService,
    private modalService: NgbModal,
    private router: Router,
    private offcanvasService: NgbOffcanvas
  ) {
    this.statuses = Object.values(CMSCaseStatus)
      .filter((value) => typeof value === 'string' && value !== 'Search')
      .map((key) => {
        const result = (key as string).split(/(?=[A-Z][a-z])/);
        return {
          id: (<any>CMSCaseStatus)[key] as number,
          key: result.join(' '),
        };
      });

    this.isDev = !environment.production;

    this.modalOptions = {
      backdrop: 'static',
      backdropClass: 'customBackdrop',
      keyboard: false,
    };
  }

  ngOnInit(): void {
    // when returning to this screen, show any pending alerts
    this.svcToast.resetAlerts();
    for (let a of UtilService.PendingAlerts)
      this.svcToast.showAlert(a.level, a.message);
    UtilService.PendingAlerts.length = 0;

    // paging: false
    this.dtOptions = {
      order: [
        [3, 'asc'],
        [4, 'asc'],
      ],
      pagingType: 'full_numbers',
      pageLength: 50,

      columnDefs: [
        { orderable: false, targets: 0 },
        { orderable: false, targets: 1 },
        { orderable: true, targets: 2 },
        { orderable: true, targets: 3, type: 'date' },
        { orderable: true, targets: 4 },
        { orderable: true, targets: 5 },
        { orderable: true, targets: 6, type: 'date' },
        { orderable: true, targets: 7 },
        { orderable: true, targets: 8 },
        { orderable: true, targets: 9 },
        { orderable: true, targets: 10 },
        { orderable: true, targets: 11 },
        { orderable: true, targets: 12 },
        { orderable: true, targets: 13 },
        { orderable: true, targets: 14, type: 'date' },
        { orderable: true, targets: 15 },
        { orderable: true, targets: 16 },
        { orderable: true, targets: 17 },
        { orderable: true, targets: 18 },
        { orderable: true, targets: 19 },
        { orderable: true, targets: 20 },
        { orderable: true, targets: 21 },
        { orderable: true, targets: 22 },
        { orderable: true, targets: 23 },
        { orderable: true, targets: 24 },
      ],
    };

    this.dtDisputeOptions = {
      order: [
        [1, 'asc'],
        [2, 'asc'],
      ],
      pagingType: 'full_numbers',
      pageLength: 50,

      columnDefs: [
        { orderable: false, targets: 0 },
        { orderable: false, targets: 1 },
        { orderable: true, targets: 2, type: 'date' },
        { orderable: true, targets: 3 },
        { orderable: true, targets: 4 },
        { orderable: true, targets: 5 },
        { orderable: true, targets: 6 },
        { orderable: true, targets: 7 },
        { orderable: true, targets: 8 },
        { orderable: true, targets: 9, type: 'date' },
        { orderable: true, targets: 10 },
        { orderable: true, targets: 11 },
        { orderable: true, targets: 12, type: 'date' },
        { orderable: true, targets: 13 },
        { orderable: true, targets: 14, type: 'date' },
        { orderable: true, targets: 14 },
      ],
    };

    this.subScribeToData();
    this.loadPrerequisites();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  authorityChange() {
    this.allAuthorityStatuses = [];
    this.authorityStatus = '';
    const a = this.allAuthorities.find(
      (v) => v.key.toLowerCase() === this.authority.toLowerCase()
    );
    if (a) {
      this.allAuthorityStatuses = a.statusList;
    }
  }

  loadPrerequisites() {
    this.svcUtil.showLoading = true;
    const authorities$ = this.svcData.loadAuthorities();
    const payors$ = this.svcData.loadPayors(true, false, false);
    const customers$ = this.svcData.loadCustomers();
    const users$ = this.svcData.loadUsers();
    const services$ = this.svcData.loadServices();

    combineLatest([
      authorities$,
      payors$,
      customers$,
      users$,
      services$,
    ]).subscribe(
      ([authorities, payors, customers, users, services]) => {
        this.origAuthorities = [...new Set(authorities)];
        this.origAuthorities.sort(UtilService.SortByName);
        this.allAuthorities = authorities.filter(
          (d) => d.key.toLowerCase() !== 'nsa'
        );
        this.allAuthorities.sort(UtilService.SortByName);
        const n = new Authority({ key: '_', name: '{empty}' });
        this.allAuthorities.splice(0, 0, n);
        this.allPayors = payors.filter((d) => d.id === d.parentId);
        this.allPayors.sort(UtilService.SortByName);
        this.allPayors.splice(
          0,
          0,
          new Payor({ id: -1, name: '{empty}', parentId: -1 })
        );

        this.allServices = services;

        this.allCustomers = customers;
        this.allCustomers.sort(UtilService.SortByName);
        const a = authorities.find((v) => v.key.toLowerCase() === 'nsa');
        if (a) {
          this.allNSAStatuses = a.statusList;
        }
        // if there's a previous search, restore it
        if (UtilService.LastSearches.length) {
          const criteria = UtilService.LastSearches.slice(-1)[0];
          this.fillForm(criteria);
          this.doSearch(criteria);
        } else {
          this.svcUtil.showLoading = false;
        }
        // users
        this.allUsers = users;
        this.allUsers.unshift(new AppUser({ email: '(unassigned)' }));
        this.allUsers.sort(UtilService.SortByEmail);
        this.svcChangeDetection.detectChanges();
        // warn if admin only?
        if (!this.allCustomers.length) {
          this.svcToast.showAlert(
            ToastEnum.warning,
            'No Customer data is currently available for searching. Ask an Administrator to verify your access.'
          );
        }
      },
      (err) =>
        this.svcToast.show(
          ToastEnum.danger,
          'Failed to load all of the dropdown box values.'
        )
    );
  }

  canSearch(): boolean {
    if (this.authority === '_' || this.customer === '{empty}') return true;
    const noStatus = (this.status ?? 99) === 99;
    const s =
      (this.assignmentDeadline || '').trim() +
      this.arbitrator.trim() +
      this.authNumber.trim() +
      this.authority.trim() +
      this.authorityStatus.trim() +
      this.assignedUser.trim() +
      (this.briefDate || '').trim() +
      this.customer.trim() +
      (this.DOB || '').trim() +
      this.EHRNumber.trim() +
      this.patientName.trim() +
      (this.payor || '').trim() +
      (this.service || '').trim() +
      this.payorClaimNumber.trim() +
      (this.requestDate || '').trim() +
      (this.NSAStatus || '').trim();
    if (
      !!this.NSAStatus &&
      s.length <= this.NSAStatus.trim().length &&
      noStatus
    )
      return false; // NSAStatus cannot be the only criteria
    if (
      !!this.authorityStatus &&
      s.length <= this.authorityStatus.trim().length &&
      noStatus
    )
      return false; // authorityStatus cannot be the only criteria
    if (
      !!this.authority.trim() &&
      s.length <= this.authority.trim().length &&
      noStatus
    )
      return false; // authority cannot be the only criteria
    if (
      !!this.customer.trim() &&
      s.length <= this.customer.trim().length &&
      noStatus
    )
      return false; // customer cannot be the only criteria
    if (
      !!this.payor &&
      !!this.payor.trim() &&
      s.length <= this.payor.trim().length &&
      noStatus
    )
      return false; // payor cannot be the only criteria
    if (!noStatus && s === '') return false; // status cannot be the only criteria
    return s !== '';
  }

  changeAssignedCustomer(c: ArbitrationCase) {
    const n = this.allCustomers.find(
      (d) => d.name.toLowerCase() === c.customer.toLowerCase()
    );
    if (!c.id || !c.customer || !n) {
      this.svcToast.show(
        ToastEnum.danger,
        'Unexpected parameters. Aborting update.'
      );
      return;
    }

    const wf = new CaseWorkflowParams();
    (wf.action = CaseWorkflowAction.AssignCustomer), (wf.caseId = c.id);
    wf.customerId = n.id;
    const msg = `case ${c.id} to ${c.customer}`;
    loggerCallback(
      LogLevel.Info,
      `CaseSearchComponent: Assigning case to customer...`
    );
    this.svcData
      .doWorkflowAction(wf)
      .pipe(take(1))
      .subscribe(
        (data) => {
          this.svcToast.show(ToastEnum.success, 'Assigned ' + msg);
        },
        (err) => {
          loggerCallback(LogLevel.Error, `Error assigning ${msg}`);
          loggerCallback(LogLevel.Error, err);
        }
      );
  }

  changeAssignedUser(c: ArbitrationCase) {
    const n = this.allUsers.find(
      (d) => d.email.toLowerCase() === c.assignedUser.toLowerCase()
    );
    if (!c.id || !c.assignedUser || !n) {
      this.svcToast.show(
        ToastEnum.danger,
        'Unexpected parameters. Aborting update.'
      );
      return;
    }

    const wf = new CaseWorkflowParams();
    (wf.action = CaseWorkflowAction.AssignUser), (wf.assignToId = n.id);
    wf.caseId = c.id;
    const msg = `case ${c.id} to ${c.assignedUser}`;
    loggerCallback(
      LogLevel.Info,
      `CaseSearchComponent: Assigning case to user...`
    );
    this.svcData
      .doWorkflowAction(wf)
      .pipe(take(1))
      .subscribe(
        (data) => {
          this.svcToast.show(ToastEnum.success, 'Assigned ' + msg);
        },
        (err) => {
          loggerCallback(LogLevel.Error, `Error assigning ${msg}`);
          loggerCallback(LogLevel.Error, err);
        }
      );
  }

  doSearch(criteria: ArbitrationCaseVM) {
    criteria.NSARequestDiscount = 0; // quirk to accomodate the fact this is nullable on the client side but not the server side
    const obs = {
      next: (data: any) => this.handleSearchData(data),
      complete: () => (this.svcUtil.showLoading = false),
      error: (err: any) => this.handleSearchErr(err),
    };
    if (this.isDisputeSearch) {
      this.svcData.searchDisputes(criteria).subscribe(obs);
    } else {
      this.svcData.searchCases(criteria).subscribe(obs);
    }
  }

  downloadResults() {
    const skip = [
      'arbitrators',
      'attachments',
      'benchmarks',
      'cptCodes',
      'cptViewmodels',
      'disputeCPTs',
      'fees',
      'log',
      'notes',
      'offerHistory',
      'payorEntity',
      'tracking',
    ];
    if (this.isDisputeSearch) skip.push('authority');
    const r = this.isDisputeSearch
      ? (this.disputes[0] as any)
      : (this.records[0] as any);
    const fields = new Array<string>();
    for (let k of Object.keys(r)) {
      if (skip.indexOf(k) >= 0) continue;
      fields.push(k);
    }

    let csv = fields.join(',');
    csv += ',arbitrators\n';
    const fmt = new DatePipe('en-US');
    const src = this.isDisputeSearch ? this.disputes : this.records;
    for (let r of src) {
      let row = r as any;
      for (let col of fields) {
        let t = typeof row[col];
        if (t === 'string') {
          if (isNaN(Date.parse(row[col])))
            csv += `"${row[col].replaceAll('"', '')}",`;
          else csv += `${fmt.transform(row[col], 'MM/dd/yyyy')},`;
        } else if (t === 'number') csv += `${row[col]},`;
        else if (row[col] instanceof Date && !isNaN(row[col].valueOf())) {
          csv += `${fmt.transform(row[col], 'MM/dd/yyyy')},`;
        } else if (row[col]) {
          csv += `"${row[col].toString().replaceAll('"', '')}",`;
        } else {
          csv += ',';
        }
      }
      // now add concatenated arbitrators
      if (row['arbitrators']?.length) {
        const arbs = row['arbitrators'] as CaseArbitrator[];
        const names = arbs.map((j) => j.arbitrator?.name);
        csv += names.join(';');
      }
      // trim final comma
      //csv=csv.slice(0,-1);
      csv += '\n';
    }
    const hidden = document.createElement('a');
    //hidden.href = 'data:text/csv;charset=utf-8;' + encodeURI(csv);
    hidden.target = '_blank';
    hidden.download = 'Arbitration_Search.csv';
    const data = new Blob([csv], { type: 'data:text/csv' });
    var url = window.URL.createObjectURL(data);
    hidden.href = url;
    hidden.click();
  }

  fillForm(criteria: ArbitrationCaseVM) {
    this.isDisputeSearch = criteria.isDisputeSearch;
    this.includeClosed = criteria.includeClosed;
    this.includeInactive = criteria.includeInactive;
    if (criteria.arbitrators.length)
      this.arbitrator = criteria.arbitrators[0].name;
    this.assignmentDeadline =
      criteria.assignmentDeadlineDate?.toLocaleDateString();
    this.assignedUser = criteria.assignedUser;
    this.EHRNumber = criteria.EHRNumber;
    this.patientName = criteria.patientName;
    this.payorClaimNumber = criteria.payorClaimNumber;
    this.authority = criteria.authority === '*' ? '' : criteria.authority;
    this.authorityChange();

    this.authorityStatus = criteria.authorityStatus;
    this.NSAStatus = criteria.NSAStatus;
    this.authNumber = criteria.authorityCaseId;
    this.customer = criteria.customer;
    this.DOB = criteria.DOB?.toLocaleDateString();
    this.briefDate = criteria.arbitrationBriefDueDate?.toLocaleDateString();
    this.requestDate = criteria.requestDate?.toLocaleDateString();
    this.status = criteria.status;
    this.payor = criteria.payor;
  }

  getCaseDate(e: NgbDate) {
    return e && e.year ? new Date(`${e.month}/${e.day}/${e.year}`) : undefined;
  }

  getClaimLinks(n: string) {
    const k = n.split(';');
    let h = '';
    for (let s of k) {
      h += `<a href="/calculator/${s}" title="Open Arbitration Case Id${s}" target="_blank">${s}</a>&nbsp;`;
    }
    return h;
  }

  getUserShort(u: string | undefined) {
    if (!u) return '* unassigned';
    const n = u.indexOf('@');
    if (n < 0) return u;
    return u.substring(0, n).replace('.', ' ');
  }

  handleSearchData(data: any) {
    this.isLoading = false;
    if (this.isDisputeSearch) {
      this.disputes = data;
      for (let d of this.disputes)
        d.authority = this.allAuthorities.find((v) => v.id === d.authorityId);
      this.disputes$.next(this.disputes);
    } else {
      this.records = data;
      this.records$.next(this.records);
    }
    this.rerender();

    if (!data || !data.length)
      this.svcToast.show(
        ToastEnum.info,
        'No records matched your query. Try including Closed or Inactive records?',
        'Search'
      );
  }

  handleSearchErr(err: any) {
    this.isLoading = false;
    let msg = '';
    if (err.status == '401') {
      msg = 'You are not authorized to search. Try logging in again.';
    } else {
      msg = err.error ?? err.message ?? err.statusText ?? err.toString();
    }
    this.svcToast.showAlert(ToastEnum.danger, msg);
    this.records$.next([]);
    this.records = [];
    this.rerender();
  }

  isDisputeSearchChange() {
    if (this.isDisputeSearch) {
      this.allAuthorities = this.origAuthorities.filter((d) => d.key !== '');
    } else {
      this.allAuthorities = this.origAuthorities.filter((d) => d.key !== 'nsa');
      const n = new Authority({ key: '_', name: '{empty}' });
      this.allAuthorities.splice(0, 0, n);
    }
  }

  loadSearch(a: ActiveSearchButton) {
    this.resetSearch();
    this.activeSearchButton = ActiveSearchButton.None;
    this.dtOptions.order = [
      [11, 'asc'],
      [12, 'asc'],
    ];
    const fed = a == ActiveSearchButton.DueToAssignTodayNSA ? 1 : 0;
    this.activeSearchButton = a;
    /* omitting this for now b/c datatables seems to have a bug when showing/hiding columns dynamically
    if(this.dtOptions.columnDefs)
      this.dtOptions.columnDefs[3].visible=false;
    */
    switch (a) {
      case ActiveSearchButton.ArbitratorRejections:
        this.isLoading = true;
        this.svcData.searchArbRejections().subscribe({
          next: (data: any) => this.handleSearchData(data),
          complete: () =>
            console.log('searchArbRejections subscription complete'),
          error: (err) => this.handleSearchErr(err),
        });
        break;

      case ActiveSearchButton.BriefNeeded:
        const d = UtilService.AddDays(new Date(), 7, 'Workdays');
        this.briefDate = d.toLocaleDateString();
        this.status = CMSCaseStatus.ActiveArbitrationBriefNeeded;
        this.dtOptions.order = [
          [4, 'asc'],
          [3, 'asc'],
        ];
        this.submitForm(this.searchForm);
        break;

      case ActiveSearchButton.CurrentCases:
        this.isLoading = true;
        this.svcData.searchCurrentCases().subscribe({
          next: (data: any) => this.handleSearchData(data),
          complete: () =>
            console.log('searchCurrentCases subscription complete'),
          error: (err) => this.handleSearchErr(err),
        });
        break;

      case ActiveSearchButton.DueToAssignToday:
      case ActiveSearchButton.DueToAssignTodayNSA:
        this.isLoading = true;
        this.dtOptions.order = [
          [4, 'asc'],
          [3, 'asc'],
        ];
        this.svcData.searchBriefDueSoon(fed).subscribe({
          next: (data: any) => this.handleSearchData(data),
          complete: () =>
            console.log('searchBriefDueSoon subscription complete'),
          error: (err) => this.handleSearchErr(err),
        });
        break;

      case ActiveSearchButton.MyCurrentCases:
        if (!this.currentUser) {
          this.svcToast.show(
            ToastEnum.danger,
            'Could not read your username from the current user profile. Contact your administrator!',
            'Search Error'
          );
          return;
        }
        this.isLoading = true;
        this.svcData.searchCurrentCases(this.currentUser.email).subscribe({
          next: (data: any) => this.handleSearchData(data),
          complete: () =>
            console.log('searchCurrentCases subscription complete'),
          error: (err) => this.handleSearchErr(err),
        });
        break;

      default:
        this.activeSearchButton = ActiveSearchButton.None;
        this.svcToast.show(ToastEnum.danger, 'Unexpected search option!');
        break;
    }
  }
  /* mergeClaims(r:ArbitrationCase) {
    if(!confirm('Are you sure?'))
      return false;

    this.svcUtil.showLoading = true;
    this.svcData.mergeArbitrationCases(r).subscribe(data => {
      this.svcToast.show(ToastEnum.success,'Refreshing search results...');
      
      // search again
      this.submitForm(this.searchForm);
    }, 
    err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast, false), 
    () => this.svcUtil.showLoading = false);
    return false;
  }
  */

  rerender(): void {
    if (this.dtElement?.dtInstance) {
      this.dtElement.dtInstance.then((dtInstance: DataTables.Api) => {
        // Destroy the table first
        dtInstance.clear();
        dtInstance.destroy();

        // Call the dtTrigger to rerender again
        if (this.isDisputeSearch) {
          this.dtDisputeTrigger.next();
          setTimeout(() => {
            try {
              dtInstance.columns.adjust();
            } catch (err) {
              console.error('Error in rerender!');
              console.error(err);
            }
          }, 500);
        } else {
          this.dtTrigger.next();
          setTimeout(() => {
            try {
              dtInstance.columns.adjust();
            } catch (err) {
              console.error('Error in rerender!');
              console.error(err);
            }
          }, 500);
        }
      });
    } else {
      if (this.isDisputeSearch) this.dtDisputeTrigger.next();
      else this.dtTrigger.next();
    }
  }

  resetSearch() {
    this.activeSearchButton = ActiveSearchButton.None;
    this.arbitrator = '';
    this.authority = '';
    this.allAuthorityStatuses.length = 0;
    this.assignmentDeadline = undefined;
    this.assignedUser = '';
    this.authorityStatus = '';
    this.NSAStatus = '';
    this.DOB = undefined;

    this.EHRNumber = '';
    this.customer = '';
    this.patientName = '';
    this.payorClaimNumber = '';
    this.payor = null;
    this.status = CMSCaseStatus.Search;
    this.authNumber = '';
    this.briefDate = undefined;
    this.requestDate = undefined;

    this.records = [];
    this.records$.next([]);
    this.rerender();
  }

  showSummary(c: ArbitrationCase) {
    const modalRef = this.modalService.open(SummaryDialogComponent);
    modalRef.componentInstance.claim = c;
  }

  subScribeToData() {
    this.svcAuth.currentUser$
      .pipe(takeUntil(this.destroyed$))
      .subscribe((data) => {
        if (data.email) {
          this.currentUser = data;
          this.isAdmin = !!this.currentUser.isAdmin;
          this.isManager = !!this.currentUser.isManager;
          this.isNegotiator = !!this.currentUser.isNegotiator;
        }
      });
  }

  showCustomSearch(content: TemplateRef<any>) {
    this.offcanvasService.open(content, {
      panelClass: 'wide-panel',
      position: 'start',
    });
  }

  submitForm(f: NgForm) {
    if (!this.canSearch()) {
      this.svcToast.show(
        ToastEnum.warning,
        'Not enough criteria to search',
        'Search Error'
      ); // validation should prevent this
      return;
    }

    this.offcanvasService.dismiss();
    this.svcToast.resetAlerts();
    this.isLoading = true;
    const criteria = new ArbitrationCaseVM();
    criteria.includeClosed = this.includeClosed;
    criteria.includeInactive = this.includeInactive;

    criteria.cptCodes = []; // TODO: We can add CPT searching very easily here
    if (this.arbitrator) {
      const a = new CaseArbitrator({
        name: this.arbitrator,
        phone: this.arbitrator,
        email: this.arbitrator,
      });
      criteria.arbitrators = [a];
    }
    criteria.arbitrationBriefDueDate = this.briefDate
      ? UtilService.GetUTCDate(this.briefDate)
      : undefined;
    criteria.assignmentDeadlineDate = this.assignmentDeadline
      ? UtilService.GetUTCDate(this.assignmentDeadline)
      : undefined;
    criteria.assignedUser = this.assignedUser;
    criteria.authority = this.authority;
    criteria.authorityStatus = this.authorityStatus;
    criteria.NSAStatus = this.NSAStatus;
    if (criteria.authority === '') {
      criteria.NSACaseId = this.authNumber; // convention
      criteria.authorityCaseId = '';
    } else {
      criteria.authorityCaseId = this.authNumber;
      criteria.NSACaseId = '';
    }
    criteria.customer = this.customer;
    criteria.DOB = this.DOB ? UtilService.GetUTCDate(this.DOB) : undefined;
    criteria.EHRNumber = this.EHRNumber;
    criteria.patientName = this.patientName;
    criteria.payor = this.payor ?? '';
    criteria.service = this.service ?? '';
    criteria.payorClaimNumber = this.payorClaimNumber;
    criteria.requestDate = this.requestDate
      ? UtilService.GetUTCDate(this.requestDate)
      : undefined;
    criteria.status = this.status;
    criteria.isDisputeSearch = this.isDisputeSearch;

    UtilService.LastSearches.push(criteria); //new ArbitrationCaseVM(criteria));
    this.doSearch(criteria);
  }

  workflowStatusChange() {
    if (this.status !== CMSCaseStatus.Search) {
      this.includeClosed = false;
      this.includeInactive = false;
    }
  }
}
