import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  TemplateRef,
  ViewChild,
} from '@angular/core';
import { NgForm } from '@angular/forms';
import { PageChangedEvent } from 'ngx-bootstrap/pagination';
import {
  NgbCalendar,
  NgbDate,
  NgbDateAdapter,
  NgbDateParserFormatter,
  NgbInputDatepickerConfig,
  NgbModal,
  NgbModalOptions,
  NgbOffcanvas,
} from '@ng-bootstrap/ng-bootstrap';
import {
  BehaviorSubject,
  Observable,
  Subject,
  combineLatest,
  fromEvent,
  of,
} from 'rxjs';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  filter,
  switchMap,
  take,
  takeUntil,
  tap,
} from 'rxjs/operators';
import { AppUser } from 'src/app/model/app-user';

import {
  CustomDateParserFormatter,
  CustomNgbDateAdapter,
} from 'src/app/model/custom-date-handler';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthService } from 'src/app/services/auth.service';
import { DisputeDataService } from 'src/app/services/dispute-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';
import { environment } from 'src/environments/environment';
import { Template } from 'src/app/model/template';

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
  selector: 'app-manage-disputes',
  templateUrl: './manage-disputes.component.html',
  styleUrls: ['./manage-disputes.component.css'],
  providers: [
    { provide: NgbDateAdapter, useClass: CustomNgbDateAdapter },
    { provide: NgbDateParserFormatter, useClass: CustomDateParserFormatter },
    NgbInputDatepickerConfig,
  ],
})
export class ManageDisputesComponent
  implements OnDestroy, OnInit, AfterViewInit
{
  @ViewChild('searchForm', { static: false }) searchForm!: NgForm;
  @ViewChild('confirmationDialog') confirmationDialog: Template | undefined;
  @ViewChild('disputeSearchInput', { static: true })
  disputeSearchInput: ElementRef;

  confirmationTitle = '';
  confirmationMessage = '';
  currentUser: AppUser = new AppUser();

  isAdmin = false;
  isManager = false;
  isNegotiator = false;
  isDev = false;
  modalOptions: NgbModalOptions | undefined;

  allCustomers: any = [];
  disputeStatuses: any = [];
  disputeEntities: any = [];
  disputeCertifiedEntities: any = [];
  disputeProvidersNPI: any = [];
  disputeBriefApprovers: any = [];

  disputeArbitIds$: Observable<any> = of([]);
  arbitIdsLoading = false;
  arbitIdsSelectInput$ = new Subject<string>();
  minLengthArbitId = 3;

  totalItems: number = 0;
  currentPage: number = 0;
  pageSize: number = 50;
  pageSizes: number[] = [10, 25, 50, 100, 200];
  showingResults = '';
  maxSize: number = 8; // Maximum number of page links to display

  disputeNumber: string = '';
  customer = '';
  disputeStatus = '';
  entity = '';
  certifiedEntity = '';
  providerNPI = '';
  arbitId = '';
  payor = '';
  briefApprover = '';
  isDisputeSearch = false;

  previousValues: { [key: number]: string } = {};
  previousBriefValues: { [key: number]: string } = {};

  hoveredDate: NgbDate | null = null;
  briefDueDateFrom: NgbDate | null = null;
  briefDueDateTo: NgbDate | null = null;

  destroyed$ = new Subject<void>();
  disputes$ = new BehaviorSubject<any[]>([]);
  disputes: any[] = [];

  get isLoading() {
    return this.svcUtil.showLoading$.getValue();
  }

  set isLoading(v: boolean) {
    this.svcUtil.showLoading = v;
  }

  constructor(
    private svcModal: NgbModal,
    private svcDisputeData: DisputeDataService,
    private svcAuth: AuthService,
    private svcToast: ToastService,
    private svcUtil: UtilService,
    private offcanvasService: NgbOffcanvas,
    private svcChangeDetection: ChangeDetectorRef,
    private calendar: NgbCalendar,
    public formatter: NgbDateParserFormatter
  ) {
    this.isDev = !environment.production;
    this.disputeSearchInput = {} as ElementRef;
    this.modalOptions = {
      backdrop: 'static',
      backdropClass: 'customBackdrop',
      keyboard: false,
    };
  }

  ngOnInit(): void {
    this.svcToast.resetAlerts();
    for (let a of UtilService.PendingAlerts)
      this.svcToast.showAlert(a.level, a.message);
    UtilService.PendingAlerts.length = 0;

    this.subScribeToData();
    this.getDisputeData();
    this.loadPrerequisites();
    this.loadArbitIds();
  }

  ngAfterViewInit() {
    // server-side search
    fromEvent(this.disputeSearchInput.nativeElement, 'keyup')
      .pipe(
        debounceTime(150),
        filter((e: any) => e.key === 'Enter'),
        distinctUntilChanged(),
        tap(() => {
          this.disputeNumber = this.disputeSearchInput.nativeElement.value;
          this.currentPage = 0;
          this.getDisputeData();
        })
      )
      .subscribe();
  }

  onPageChange(event: PageChangedEvent): void {
    const startItem = (event.page - 1) * event.itemsPerPage;
    const endItem = event.page * event.itemsPerPage;
    this.showingResults = `${startItem + 1} to ${endItem}`;

    this.currentPage = event.page - 1;
    this.getDisputeData();
  }

  onPageSizeChange($event: any): void {
    this.pageSize = Number($event.target.value);
    this.currentPage = 0;
    this.getDisputeData();
  }

  onDisputeNumberSearch($event: any): void {
    if ($event.target.value === '') {
      this.disputeNumber = $event.target.value;
      this.currentPage = 0;
      this.getDisputeData(false);
    }
  }

  getDisputeData(isLoading = true) {
    this.svcUtil.showLoading = isLoading;
    const disputes$ = this.svcDisputeData.getDisputeList(
      this.currentPage,
      this.pageSize,
      this.disputeNumber,
      this.customer,
      this.disputeStatus,
      this.entity,
      this.certifiedEntity,
      this.briefApprover,
      this.providerNPI,
      this.arbitId,
      this.getDateFormat(this.formatter.format(this.briefDueDateFrom)),
      this.getDateFormat(this.formatter.format(this.briefDueDateTo))
    );

    disputes$.subscribe(
      (resp: any) => {
        if (!resp.disputes || !resp.disputes.length) {
          this.showingResults = `0 to 0`;
          this.svcToast.show(
            ToastEnum.info,
            'No records matched your query. Try including Closed or Inactive records?',
            'Search'
          );
          this.svcUtil.showLoading = false;
        }

        const start = resp.pagerInfo.pageNumber * resp.pagerInfo.pageSize + 1;
        const end = resp.pagerInfo.nextPage * resp.pagerInfo.pageSize;

        this.showingResults = `${start} to ${end}`;

        this.disputes = resp.disputes.map((d: any) => {
          return {
            ...d,
            oldDisputeStatus: d.disputeStatus,
          };
        });

        this.totalItems = resp.pagerInfo.totalRecords;
        this.svcUtil.showLoading = false;
      },
      (err) => this.handleSearchErr(err)
    );
  }

  subScribeToData() {
    // listen for loading of user info
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

  loadPrerequisites() {
    const customers$ = this.svcDisputeData.getDisputeMasterCustomer();
    const disputeStatus$ = this.svcDisputeData.getDisputeMasterDisputeStatus();
    const disputeBriefApprover$ =
      this.svcDisputeData.getDisputeMasterBriefApprover();
    const disputeEntity$ = this.svcDisputeData.getDisputeMasterEntity();
    const disputeCertifiedEntity$ =
      this.svcDisputeData.getDisputeMasterCertifiedEntity();
    const disputeProviderNPI$ =
      this.svcDisputeData.getDisputeMasterProviderNPI();
    const disputeArbitIDs$ = this.svcDisputeData.getDisputeArbitIds('');

    combineLatest([
      customers$,
      disputeStatus$,
      disputeBriefApprover$,
      disputeEntity$,
      disputeCertifiedEntity$,
      disputeProviderNPI$,
      disputeArbitIDs$,
    ]).subscribe(
      ([
        customers,
        disputeStatus,
        disputeBriefApprover,
        disputeEntity,
        disputeCertifiedEntity,
        disputeProviderNPI,
        disputeArbitIDs,
      ]) => {
        this.allCustomers = customers;
        this.disputeStatuses = disputeStatus;
        this.disputeEntities = disputeEntity;
        this.disputeBriefApprovers = disputeBriefApprover;
        this.disputeCertifiedEntities = disputeCertifiedEntity;
        this.disputeProvidersNPI = disputeProviderNPI;
        this.disputeArbitIds$ = of(disputeArbitIDs);

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

  loadArbitIds() {
    this.arbitIdsSelectInput$
      .pipe(
        filter((res) => {
          return res !== null && res.length >= this.minLengthArbitId;
        }),
        distinctUntilChanged(),
        debounceTime(800),
        tap(() => (this.arbitIdsLoading = true)),
        switchMap((term) => {
          return this.svcDisputeData.getDisputeArbitIds(term).pipe(
            catchError(() => of([])), // empty list on error
            tap(() => (this.arbitIdsLoading = false))
          );
        })
      )
      .subscribe((data) => {
        this.disputeArbitIds$ = of(data);
      });
  }

  closeCustomSearch() {
    if (!this.isDisputeSearch) {
      this.customer = '';
      this.disputeStatus = '';
      this.entity = '';
      this.certifiedEntity = '';
      this.briefApprover = '';
      this.providerNPI = '';
      this.arbitId = '';
      this.payor = '';
    }
    this.offcanvasService.dismiss('Cross click');
  }

  showCustomSearch(content: TemplateRef<any>) {
    this.offcanvasService.open(content, {
      panelClass: 'wide-panel',
      position: 'start',
      // backdrop: false,
      // scroll: true,
    });
  }

  onDateSelection(date: NgbDate) {
    if (!this.briefDueDateFrom && !this.briefDueDateTo) {
      this.briefDueDateFrom = date;
    } else if (
      this.briefDueDateFrom &&
      !this.briefDueDateTo &&
      date &&
      date.after(this.briefDueDateFrom)
    ) {
      this.briefDueDateTo = date;
    } else {
      this.briefDueDateTo = null;
      this.briefDueDateFrom = date;
    }
  }

  isHovered(date: NgbDate) {
    return (
      this.briefDueDateFrom &&
      !this.briefDueDateTo &&
      this.hoveredDate &&
      date.after(this.briefDueDateFrom) &&
      date.before(this.hoveredDate)
    );
  }

  isInside(date: NgbDate) {
    return (
      this.briefDueDateTo &&
      date.after(this.briefDueDateFrom) &&
      date.before(this.briefDueDateTo)
    );
  }

  isRange(date: NgbDate) {
    return (
      date.equals(this.briefDueDateFrom) ||
      (this.briefDueDateTo && date.equals(this.briefDueDateTo)) ||
      this.isInside(date) ||
      this.isHovered(date)
    );
  }

  validateInput(currentValue: NgbDate | null, input: string): NgbDate | null {
    const parsed = this.formatter.parse(input);
    return parsed && this.calendar.isValid(NgbDate.from(parsed))
      ? NgbDate.from(parsed)
      : currentValue;
  }

  handleSearchErr(err: any) {
    this.isLoading = false;
    let msg = '';
    if (err.status == '401') {
      msg = 'You are not authorized to search. Try logging in again.';
    } else {
       msg = err.error && err.error.Exception ? err.error.Exception : err.error ?? err.message ?? err.statusText ?? err.toString();
      if (err.status == '500') {
        this.svcToast.show(
            ToastEnum.danger,
            msg,
            'Internal Server Error');
      }
    }
    this.svcUtil.showLoading = false;
    this.svcToast.showAlert(ToastEnum.danger, msg);
    this.disputes$.next([]);
    this.disputes = [];
  }

  updateDisputeStatusAndBriefApprover(
    dispute: any,
    index: number,
    disputeStatus: string,
    briefApprover: string,
    type: string
  ) {
    this.svcDisputeData
      .updateDisputeStatusByID({
        disputeId: dispute.id,
        disputeStatus: disputeStatus,
        briefApprover: briefApprover,
      })
      .pipe(take(1))
      .subscribe(
        (resp: any) => {
          this.svcToast.show(
            ToastEnum.success,
            type === 'status'
              ? resp.message
              : resp.message.replace('Dispute status', 'Brief Approver')
          );
          this.previousValues[index] = disputeStatus;
          this.previousBriefValues[index] = briefApprover;
          dispute.disputeStatus = disputeStatus;
          dispute.briefApprover = briefApprover;
        },
        (err) => {
          this.svcToast.show(ToastEnum.danger, err.message);
          this.svcUtil.showLoading = false;
        },
        () => {
          this.getDisputeData();
        }
      );
  }

  changeDisputeStatus(event: any, c: any, index: number): void {
    this.confirmationTitle = 'Confirm dispute status update ';
    this.confirmationMessage =
      'Are you sure you want to change the dispute status?';
    const disputeStatusValue = event;
    const oldValue = this.previousValues[index] || c.disputeStatus;

    // Temporarily store the new value to prevent immediate change
    event = oldValue;

    this.svcModal.open(this.confirmationDialog, this.modalOptions).result.then(
      (data) => {
        this.svcUtil.showLoading = true;
        this.updateDisputeStatusAndBriefApprover(
          c,
          index,
          disputeStatusValue,
          c.briefApprover,
          'status'
        );
      },
      (err) => {
        c.disputeStatus = oldValue;
      }
    );
  }

  changeBriefApprover(event: any, c: any, index: number): void {
    this.confirmationTitle = 'Confirm brief approver update ';
    this.confirmationMessage =
      'Are you sure you want to change the brief approver?';
    const briefApproverValue = event;
    const oldValue = this.previousBriefValues[index] || c.briefApprover;

    // Temporarily store the new value to prevent immediate change
    event = oldValue;

    this.svcModal.open(this.confirmationDialog, this.modalOptions).result.then(
      (data) => {
        this.svcUtil.showLoading = true;
        this.updateDisputeStatusAndBriefApprover(
          c,
          index,
          c.disputeStatus,
          briefApproverValue,
          'approver'
        );
      },
      (err) => {
        c.briefApprover = oldValue;
      }
    );
  }

  getDateFormat(inputDate: any) {
    if (inputDate) {
      // Split the input date into components
      let [month, day, year] = inputDate.split('/');

      // Create a new Date object
      let date = new Date(year, month - 1, day); // month is zero-based

      return date.toISOString();
    } else {
      return '';
    }
  }
  canSearch(): boolean {
    const briefDueDateFrom = this.getDateFormat(
      this.formatter.format(this.briefDueDateFrom)
    );
    const briefDueDateTo = this.getDateFormat(
      this.formatter.format(this.briefDueDateTo)
    );

    const s =
      (briefDueDateFrom || '').trim() +
      (briefDueDateTo || '').trim() +
      this.customer +
      this.disputeStatus +
      this.entity +
      this.certifiedEntity +
      this.briefApprover +
      this.arbitId +
      this.providerNPI;

    if (s === '') return false;
    return s !== '';
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
    this.svcToast.resetAlerts();
    this.isDisputeSearch = true;
    this.isLoading = true;
    this.getDisputeData();
    this.offcanvasService.dismiss();
  }

  resetSearch(isReset: boolean) {
    // this.disputeSearchInput = {} as ElementRef;
    this.disputeNumber = '';
    this.customer = '';
    this.disputeStatus = '';
    this.briefDueDateFrom = null;
    this.briefDueDateTo = null;
    this.entity = '';
    this.certifiedEntity = '';
    this.briefApprover = '';
    this.providerNPI = '';
    this.arbitId = '';
    this.payor = '';

    if (this.isDisputeSearch) {
      this.isDisputeSearch = false;
      this.getDisputeData();
    }
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }
}
