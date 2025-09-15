import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Location } from '@angular/common';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  BehaviorSubject,
  Observable,
  Subject,
  combineLatest,
  from,
} from 'rxjs';
import { ArbitrationCase } from 'src/app/model/arbitration-case';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { ToastEnum } from 'src/app/model/toast-enum';
import {
  CustomDateParserFormatter,
  CustomNgbDateAdapter,
} from 'src/app/model/custom-date-handler';
import { UtilService } from 'src/app/services/util.service';
import { Authority } from 'src/app/model/authority';
import {
  NgbDateAdapter,
  NgbDateParserFormatter,
  NgbInputDatepickerConfig,
  NgbModal,
  NgbModalOptions,
} from '@ng-bootstrap/ng-bootstrap';
import { AuthorityTrackingDetail } from 'src/app/model/authority-tracking-detail';
import { Customer } from 'src/app/model/customer';
import { DataTableDirective } from 'angular-datatables';
import {
  DetailedDispute,
  DetailedDisputeVM,
} from 'src/app/model/detailed-dispute';
import { takeUntil } from 'rxjs/operators';
import { AuthService } from 'src/app/services/auth.service';
import { FeeRecipient } from 'src/app/model/fee-recipient-enum';
import { AppUser } from 'src/app/model/app-user';
import { Template } from '@angular/compiler/src/render3/r3_ast';
import { Arbitrator } from 'src/app/model/arbitrator';
import { CaseSettlement } from 'src/app/model/case-settlement';
import { Payor } from 'src/app/model/payor';
import { DisputeDataService } from 'src/app/services/dispute-data.service';
import { DetailedDisputeCPT } from 'src/app/model/detailed-dispute-cpt';
import { loggerCallback } from 'src/app/app.module';
import { LogLevel } from '@azure/msal-browser';
import { DetailedDisputeCPTLog } from 'src/app/model/detailed-dispute-cpt-log';

@Component({
  selector: 'app-dispute-detail',
  templateUrl: './dispute-detail.component.html',
  styleUrls: ['./dispute-detail.component.css'],
  providers: [
    { provide: NgbDateAdapter, useClass: CustomNgbDateAdapter },
    { provide: NgbDateParserFormatter, useClass: CustomDateParserFormatter },
    NgbInputDatepickerConfig,
    Location,
  ],
})
export class DisputeDetailComponent implements OnDestroy, OnInit {
  @ViewChild('disputeForm', { static: false }) disputeForm!: NgForm;
  @ViewChild(DataTableDirective, { static: false }) dtElement:
    | DataTableDirective
    | undefined;
  @ViewChild('confirmationNavigationDialog') confirmationNavModal:
    | Template
    | undefined;
  @ViewChild('confirmationDialog') confirmationModal: Template | undefined;
  readonly TAB = 9;
  readonly ENTER = 13;
  readonly UP = 38;
  readonly DOWN = 40;
  readonly LEFT = 37;
  readonly RIGHT = 39;
  readonly CONTROL_KEYS = [
    this.TAB,
    this.ENTER,
    this.UP,
    this.DOWN,
    this.LEFT,
    this.RIGHT,
  ];
  confirmationNavigationTitle = 'Confirm navigation';
  confirmationNavigationMessage =
    'Warning! You have unsaved changes. Close without saving?';
  confirmationTitle = '';
  confirmationMessage = '';
  currentCustomer = new Customer();
  currentPayor: Payor | undefined;
  currentUser: AppUser | undefined;
  displayDispute = new DetailedDisputeVM(); // viewmodel
  disputeSettlements$ = new BehaviorSubject<Array<CaseSettlement>>([]);
  destroyed$ = new Subject<void>();
  FeeRecipient = FeeRecipient;

  hideDates = false;
  hideFee = false;
  hideBrief = false;
  hideCPT = false;
  hideCPTLog = false;
  hideLog = false;

  id = 0;
  isAdmin = false;
  isLogLoading = false;
  isManager = false;
  isNegotiator = false;
  isNSA = false;
  isReporter = false;
  isState = false;
  modalOptions: NgbModalOptions | undefined;
  NSAAuthority: Authority | undefined;
  NSATrackingFieldsForUI: AuthorityTrackingDetail[] = [];
  origDispute: DetailedDispute | undefined;
  records$ = new BehaviorSubject<ArbitrationCase[]>([]);
  removedFeeIds: number[] = [];
  resetAlerts = false;
  selectedArbitrator: Arbitrator | undefined;
  showAddFormalSettlement = true;
  showHelp = false;
  showCPTWarning = true;
  allCustomers: any = [];
  allDisputeStatuses: any = [];
  allEntities: any = [];
  allCertifiedEntities: any = [];
  allProvidersNPI: any = [];
  allServiceLines: any = [];
  allPayors: any = [];

  allLogs: any[] = [];

  isInvoiceLink = false;
  isBriefSubLink = false;
  allBriefApprovers: any = [];

  constructor(
    private svcData: CaseDataService,
    private svcDisputeData: DisputeDataService,
    private router: Router,
    private svcToast: ToastService,
    private svcUtil: UtilService,
    private svcModal: NgbModal,
    private route: ActivatedRoute,
    private svcAuth: AuthService,
    private location: Location
  ) {
    this.modalOptions = {
      backdrop: 'static',
      backdropClass: 'customBackdrop',
      keyboard: false,
    };
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  ngOnInit(): void {
    this.subscribeToData();
    // are we creating or loading?
    this.route.params.pipe(takeUntil(this.destroyed$)).subscribe((data) => {
      if (data.id) {
        this.loadPrerequisites(data.id);
      } else {
        // force user to either pass in an ID or some initial starting values since the
        // origin of all new disputes will be an external link for now
        UtilService.PendingAlerts.push({
          level: ToastEnum.danger,
          message: 'Missing dispute identifier in URL. Redirecting to home...',
        });
        this.router.navigateByUrl('/');
      }
    });
  }

  canDeactivate(): Observable<boolean> {
    return this.isNavigationAllowed();
  }

  currencyBlur(e: any) {
    this.fixTo2Digits(e.target);
  }

  cptCodeChanged(m: any) {
    this.disputeForm.form.markAsTouched();
    this.disputeForm.form.markAsDirty();
  }

  // not the "Angular" way - should be a directive
  fixTo2Digits(target: any) {
    if (!target) return;
    const el = $(target);
    if (!el) return;
    // get the current input value
    let correctValue = el.val().toString();

    //if there are no decimal places add trailing zeros
    if (correctValue.indexOf('.') === -1) {
      correctValue += '.00';
    } else {
      const ss = correctValue.toString().split('.');
      if (ss[1].length === 1) {
        //if there is only one number after the decimal add a trailing zero
        correctValue += '0';
      } else if (ss[1].length > 2) {
        //if there is more than 2 decimal places round backdown to 2
        correctValue = parseFloat($(el).val()).toFixed(2);
      }
    }

    //update the value of the input with our conditions
    $(el).val(correctValue);
  }

  focusNextElement(m: HTMLElement) {
    //add all elements we want to include in our selection
    var focussableElements =
      'a:not([disabled]), button:not([disabled]), input[type=text]:not([disabled]), [tabindex]:not([disabled]):not([tabindex="-1"])';

    const focussable = Array.prototype.filter.call(
      document.querySelectorAll(focussableElements),
      function (element) {
        //check for visibility while always include the current activeElement
        return (
          element.offsetWidth > 0 || element.offsetHeight > 0 || element === m
        );
      }
    );
    const index = focussable.indexOf(m);
    if (index > -1) {
      const nextElement = focussable[index + 1] || focussable[0];
      nextElement.focus();
    }
  }

  confirmLinkEdit(linkName: string) {
    linkName === 'invoiceLink'
      ? (this.isInvoiceLink = false)
      : (this.isBriefSubLink = false);
  }
  
  confirmArbitEdit(id: any) {
    this.displayDispute.disputeCPTs.find((m) => {
      if (m.arbitId === id) {
        m.isShowLink = true;
      }
    })
  }

  initDisputeCPTs(data: any) {
    this.origDispute = new DetailedDispute(data);
    this.displayDispute = new DetailedDisputeVM(data);

    this.isInvoiceLink = !!data.feeInvoiceLink;
    this.isBriefSubLink = !!data.briefSubmissionLink;

    this.disputeForm.form.markAsUntouched();
    this.disputeForm.form.markAsPristine();

    // this is a hack to correct the number of decimals - need to find time to make a directive to do it
    setTimeout(() => {
      let names: string[] = [];
      if (this.displayDispute.disputeCPTs.length) {
        names = [
          'benchmarkAmount',
          'providerOfferAmount',
          'payorOfferAmount',
          'awardAmount',
        ];
        for (let i = 0; i < this.displayDispute.disputeCPTs.length; i++) {
          this.displayDispute.disputeCPTs[i]['isShowLink'] = false;
          for (const n of names) {
            let d = document.getElementById(`${n}_${i}`);
            if (d) this.fixTo2Digits($(d));
          }
        }
      }
    }, 250);
  }

  isNavigationAllowed(beforeunloadEvent = false): Observable<boolean> {
    return from(
      new Promise<boolean>((resolve) => {
        if (this.disputeForm && this.disputeForm.dirty) {
          // && !this.isSaving
          if (beforeunloadEvent) {
            resolve(false);
          } else {
            this.svcModal
              .open(this.confirmationNavModal, this.modalOptions)
              .result.then(
                (data) => {
                  if (this.resetAlerts) {
                    this.svcToast.resetAlerts();
                  }
                  resolve(true);
                },
                () => resolve(false)
              );
          }
        } else {
          resolve(true);
        }
      })
    );
  }

  loadPrerequisites(disputeId: string = '') {
    if (!disputeId) {
      UtilService.PendingAlerts.push({
        level: ToastEnum.danger,
        message: 'Bad or missing Dispute identifier.',
      });
      this.router.navigateByUrl('/');
      return;
    }
    this.svcUtil.showLoading = true;

    const data$ = this.svcDisputeData.getDisputeDetail(disputeId);
    const payors$ = this.svcData.loadPayors(true, false, false);
    const customers$ = this.svcDisputeData.getDisputeMasterCustomer();
    const disputeStatus$ = this.svcDisputeData.getDisputeMasterDisputeStatus();
    const disputeBriefApprover$ =
      this.svcDisputeData.getDisputeMasterBriefApprover();
    const disputeEntity$ = this.svcDisputeData.getDisputeMasterEntity();
    const disputeCertifiedEntity$ =
      this.svcDisputeData.getDisputeMasterCertifiedEntity();
    const disputeProviderNPI$ =
      this.svcDisputeData.getDisputeMasterProviderNPI();
    const disputeServiceLine$ =
      this.svcDisputeData.getDisputeMasterServiceLine();

    combineLatest([
      data$,
      payors$,
      customers$,
      disputeStatus$,
      disputeBriefApprover$,
      disputeEntity$,
      disputeCertifiedEntity$,
      disputeProviderNPI$,
      disputeServiceLine$,
    ]).subscribe(
      ([
        data,
        payors,
        customers,
        disputeStatus,
        disputeBriefApprover,
        disputeEntity,
        disputeCertifiedEntity,
        disputeProviderNPI,
        disputeServiceLine,
      ]) => {
        this.initDisputeCPTs(data);

        this.allPayors = payors;
        this.allPayors.sort(UtilService.SortByName);

        this.allCustomers = customers;
        this.allDisputeStatuses = disputeStatus;
        this.allEntities = disputeEntity;
        this.allBriefApprovers = disputeBriefApprover;
        this.allCertifiedEntities = disputeCertifiedEntity;
        this.allProvidersNPI = disputeProviderNPI;
        this.allServiceLines = disputeServiceLine;
      },
      (err) => {
        UtilService.PendingAlerts.push({
          level: ToastEnum.danger,
          // message: UtilService.ExtractMessageFromErr(err),
          message: 'Dispute not found.',
        });
        this.router.navigateByUrl('/');
      },
      () => (this.svcUtil.showLoading = false)
    );
  }

  onDateSelect(e: any) {
    if (!!e.target && !!e.target.value) {
      const date = new Date(e.target.value);
      if (!date || !UtilService.IsDateValid(date)) {
        this.svcToast.show(
          ToastEnum.danger,
          'The entered value does not appear to be a valid date!'
        );
      }
    }

    this.disputeForm.form.markAsDirty();
  }

  saveChanges() {
    this.confirmationTitle = 'Confirm changes';
    this.confirmationMessage = 'Are you sure you want to save these changes?';
    this.svcModal.open(this.confirmationModal, this.modalOptions).result.then(
      () => {
        this.svcUtil.showLoading = true;
        this.displayDispute.updatedBy = this.currentUser?.email ?? '';
        this.displayDispute.updatedOn = new Date();

        this.svcDisputeData
          .updateDetailedDispute(this.displayDispute)
          .subscribe(
            (resp: any) => {
              this.svcToast.show(
                ToastEnum.success,
                resp.message ?? 'Dispute updated successfully.',
                'Success'
              );

              this.svcToast.resetAlerts();
                this.svcDisputeData
              .getDisputeDetail(resp.data.disputeNumber)
              .subscribe((resp) => {
                this.initDisputeCPTs(resp);
              });
            },
            (err) => {
              UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast);
              this.svcUtil.showLoading = false;
            },
            () => (this.svcUtil.showLoading = false)
          );
      },
      (err) => {
        // this.svcToast.show(
        //   ToastEnum.danger,
        //   'Dispute update cancelled.',
        //   'Failure'
        // );
        loggerCallback(LogLevel.Info, 'Dispute update cancelled.');
      }
    );
  }

  onSubmit() {
    if (!this.disputeForm.valid) return false;
    this.saveChanges();
    return true;
  }

  addCpt(disputeNumber: string) {
    const cpt = new DetailedDisputeCPT({
      arbitId: 0,
      awardAmount: 0,
      benchmarkAmount: 0,
      cptCode: '',
      disputeNumber,
      id: 0,
      payorOfferAmount: 0,
      prevailingParty: '',
      providerOfferAmount: 0,
      updatedBy: this.currentUser?.email ?? '',
      updatedOn: new Date(),
      isAddCPT: true,
      isShowLink: true
    });
    this.displayDispute.disputeCPTs.push(cpt);
    this.disputeForm.form.markAsDirty();

    setTimeout(() => {
      const a = document.getElementsByClassName('arbit-focus');
      for (let i = 0; i < a.length; i++) {
        let n = a[i] as HTMLInputElement;
        if (!n.value) {
          n.focus();
          return;
        }
      }
    }, 0);
  }

  disableDeleteCPTFromDispute(): boolean {
    if (!this.isAdmin && !this.isManager) {
      console.warn("User don't have permission");
      return true;
    }
    return false;
  }

  deleteCPTFromDispute(
    index: number,
    dispute: DetailedDispute,
    cptId: any,
    cptCode: any,
    arbitId: any
  ) {
    if (!cptId) {
      this.displayDispute.disputeCPTs.splice(index, 1);
      return;
    }
    this.confirmationTitle = 'Confirm delete';
    this.confirmationMessage = 'Are you sure you want to delete this record?';
    this.svcModal.open(this.confirmationModal, this.modalOptions).result.then(
      () => {
        this.svcUtil.showLoading = true;
        this.svcDisputeData.deleteDisputeCPTbyId(dispute.id, cptId).subscribe(
          (respone) => {
            this.svcToast.show(
              ToastEnum.success,
              respone.message ??
                'DisputeCPT data has been deleted successfully',
              'Success'
            );
            this.svcDisputeData
              .getDisputeDetail(dispute.disputeNumber)
              .subscribe((resp) => {
                this.initDisputeCPTs(resp);
              });
          },
          (err) => {
            console.error('Could not delete DisputeCPT ' + err.status);
            this.svcToast.show(
              ToastEnum.danger,
              'Could not delete DisputeCPT',
              'Failure'
            );
            UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast);
            this.svcUtil.showLoading = false;
          },
          () => (this.svcUtil.showLoading = false)
        );
      },
      (err) => {
        loggerCallback(LogLevel.Info, 'DisputeCPT delete cancelled.');
      }
    );
  }

  refreshLog() {
    this.allLogs.length = 0;
    this.isLogLoading = true;
    this.svcDisputeData.getDisputeLogsById(this.displayDispute.id).subscribe(
      (data: any) => {
        this.isLogLoading = false;

        this.allLogs = data;
        // this.allLogs.sort(UtilService.SortByCreatedOnDesc);
        this.allLogs.forEach((g) => {
          g.previousValue = g.previousValue.replaceAll('\\u0022', '"');
          g.newValue = g.newValue.replaceAll('\\u0022', '"');
        });
      },
      (err) => {
        this.isLogLoading = false;
        this.svcToast.show(ToastEnum.danger, err.message ?? err.toString());
      }
    );
  }

  resetFormStatus() {
    this.disputeForm.form.markAsUntouched();
    this.disputeForm.form.markAsPristine();
    Object.keys(this.disputeForm.controls).forEach((key) => {
      const control = this.disputeForm.controls[key];
      control.markAsUntouched();
      control.markAsPristine();
    });
  }

  selectAll(e: any) {
    e?.target?.select();
  }

  onArbitChanged(e: any) {
    if (!e.target.value.match(/^[0-9]*$/)) {
      e.target.value = e.target.value.replace(/[^0-9]/g, '');
    }
  }

  subscribeToData() {
    // listen for loading of user info
    this.svcAuth.currentUser$
      .pipe(takeUntil(this.destroyed$))
      .subscribe((data) => {
        this.isAdmin = !!data.isAdmin;
        this.isManager = !!data.isManager;
        this.isNegotiator = !!data.isNegotiator;
        this.isNSA = !!data.isNSA;
        this.isReporter = !!data.isReporter;
        this.isState = !!data.isState;
        this.currentUser = data;
      });
  }

  undoChanges() {
    const data = new DetailedDispute(this.origDispute);
    this.initDisputeCPTs(data);
    this.removedFeeIds = [];
    this.svcToast.resetAlerts();
  }

  get prevailingPartySource() {
    return [this.displayDispute.entity, this.displayDispute.payor];
  }
}
