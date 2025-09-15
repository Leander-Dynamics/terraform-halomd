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
import { CalculatorVariables } from 'src/app/model/calculator-variables';
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
  NgbModalRef,
} from '@ng-bootstrap/ng-bootstrap';
import {
  AuthorityTrackingDetail,
  AuthorityTrackingDetailScope,
} from 'src/app/model/authority-tracking-detail';
import { Customer } from 'src/app/model/customer';
import { DataTableDirective } from 'angular-datatables';
import { AuthorityDisputeCPTVM } from 'src/app/model/authority-dispute-cpt';
import {
  AuthorityDispute,
  AuthorityDisputeVM,
} from 'src/app/model/authority-dispute';
import { take, takeUntil } from 'rxjs/operators';
import { IKeyId } from 'src/app/model/iname';
import {
  ArbitrationResult,
  CMSCaseStatus,
} from 'src/app/model/arbitration-status-enum';
import { CaseDocumentType } from 'src/app/model/case-document-type-enum';
import { CaseFileVM } from 'src/app/model/case-file';
import { AuthService } from 'src/app/services/auth.service';
import { FileUploadEventArgs } from 'src/app/model/file-upload-event-args';
import { AuthorityDisputeFee } from 'src/app/model/authority-dispute-fee';
import { FeeRecipient } from 'src/app/model/fee-recipient-enum';
import { ArbitratorType } from 'src/app/model/arbitrator-type-enum';
import { AppUser } from 'src/app/model/app-user';
import { Template } from '@angular/compiler/src/render3/r3_ast';
import { Arbitrator } from 'src/app/model/arbitrator';
import { CaseSettlement } from 'src/app/model/case-settlement';
import { Payor } from 'src/app/model/payor';
import { AuthorityDisputeNote, Note } from 'src/app/model/note';
import { AuthorityDisputeAttachment } from 'src/app/model/emr-claim-attachment';
import { AppSettings } from 'src/app/model/app-settings';
import { AuthorityDisputeLog } from 'src/app/model/authority-dispute-log';

@Component({
  selector: 'app-batch-calculator',
  templateUrl: './batch-calculator.component.html',
  styleUrls: ['./batch-calculator.component.css'],
  providers: [
    { provide: NgbDateAdapter, useClass: CustomNgbDateAdapter },
    { provide: NgbDateParserFormatter, useClass: CustomDateParserFormatter },
    NgbInputDatepickerConfig,
    Location,
  ],
})
export class BatchCalculatorComponent implements OnDestroy, OnInit {
  @ViewChild('batchForm', { static: false }) batchForm!: NgForm;
  @ViewChild(DataTableDirective, { static: false }) dtElement:
    | DataTableDirective
    | undefined;
  @ViewChild('confirmationDialog') confirmationModal: Template | undefined;
  @ViewChild('pickArbDialog') pickArbModal: Template | undefined;
  @ViewChild('payFeeDialog') payFeeModal: Template | undefined;
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
  allArbitrationResults = new Array<IKeyId>();
  allArbitrators = new Array<Arbitrator>();
  allAuthorityStatuses: string[] = [];
  allCaseFileVMs$ = new BehaviorSubject<CaseFileVM[]>([]);
  allDisputeFileVMs$ = new BehaviorSubject<CaseFileVM[]>([]);
  allowedAttachmentTypes = new Array<IKeyId>(); // ['Brief','Check','Correspondence','EOB','HCFA','NSARequestAttachment'];
  allNSAIneligibilityActions = [
    'Client Removed Assignment',
    'Denial',
    'Duplicate',
    'Entity Closed',
    'Facility is out-of-network',
    'FH Benchmark is below total allowed',
    'Ineligible Plan',
    'Other - Manager Review',
    'Paid in Full',
    'Provider is in-network',
    'State Arbitration',
    'Timing',
  ];
  allIneligibilityActions: Array<string> = [];
  allLogs: AuthorityDisputeLog[] = [];
  allUsers: AppUser[] = [];
  allWorkflowStatuses = new Array<IKeyId>();
  appSettings: AppSettings | undefined;
  ArbitrationResult = ArbitrationResult;
  ArbitratorType = ArbitratorType;
  arbTypeLabel = 'Arbitrator';
  authKey = '';
  canCreate = false;
  canEdit = false;
  calculatorVars: CalculatorVariables | undefined;
  caseNotes = '';
  CMSCaseStatus = CMSCaseStatus;
  confirmationTitle = 'Confirm Navigation';
  confirmationMessage =
    'Warning! You have unsaved changes. Close without saving?';
  currentAuthority: Authority | undefined;
  currentCustomer = new Customer();
  currentFee: AuthorityDisputeFee | undefined;
  currentPayor: Payor | undefined;
  currentUser: AppUser | undefined;
  displayDispute = new AuthorityDisputeVM(); // viewmodel
  disputeSettlements$ = new BehaviorSubject<Array<CaseSettlement>>([]);
  //dtOptions:DataTables.Settings = {};
  //dtTrigger: Subject<any> = new Subject<any>();
  destroyed$ = new Subject<void>();
  FeeRecipient = FeeRecipient;
  hideArbitrator = false;
  hideAttachments = false;
  hideDates = true;
  hideFees = false;
  hideFiles = false;
  hideLog = true;
  hideNotes = false;
  hideSettlements = false;
  id = 0;
  isAdmin = false;
  isBriefApprover = false;
  isBriefPreparer = false;
  isBriefWriter = false;
  isLogLoading = false;
  isManager = false;
  isNegotiator = false;
  isNew = true;
  isNSA = false;
  isReporter = false;
  isState = false;
  modalOptions: NgbModalOptions | undefined;
  newNote = '';
  NSAAuthority: Authority | undefined;
  NSATrackingFieldsForUI: AuthorityTrackingDetail[] = [];
  origDispute: AuthorityDispute | undefined;
  records$ = new BehaviorSubject<ArbitrationCase[]>([]);
  removedFeeIds: number[] = [];
  resetAlerts = false;
  selectedArbitrator: Arbitrator | undefined;
  showAddFormalSettlement = true;
  showHelp = false;
  trackingDetailsList: AuthorityTrackingDetail[] = [];
  trackingObject: any = null;

  public get canAddFees(): boolean {
    return (
      !!this.origDispute &&
      !!this.origDispute.id &&
      UtilService.IsAnOpenStatus(this.origDispute.workflowStatus)
    );
  }

  constructor(
    private svcData: CaseDataService,
    private router: Router,
    private svcToast: ToastService,
    private svcUtil: UtilService,
    private svcModal: NgbModal,
    private route: ActivatedRoute,
    private svcAuth: AuthService,
    private location: Location
  ) {
    this.allArbitrationResults = Object.values(ArbitrationResult)
      .filter((value) => typeof value === 'string')
      .map((key) => {
        const result = (key as string).split(/(?=[A-Z][a-z])/);
        return {
          id: (<any>ArbitrationResult)[key] as number,
          key: result.join(' '),
        };
      });

    this.allWorkflowStatuses = Object.values(CMSCaseStatus)
      .filter(
        (value) =>
          typeof value === 'string' && value !== 'Search' && value !== 'Unknown'
      )
      .map((key) => {
        const result = (key as string).split(/(?=[A-Z][a-z])/);
        return {
          id: (<any>CMSCaseStatus)[key] as number,
          key: result.join(' '),
        };
      });

    this.allowedAttachmentTypes = Object.values(CaseDocumentType)
      .filter((value) => typeof value === 'string')
      .map((key) => {
        const result = (key as string).split(/(?=[A-Z]+[a-z])/);
        return {
          id: (<any>CaseDocumentType)[key] as number,
          key: result
            .join(' ')
            .replace('PatientEOB', 'Patient EOB')
            .replace('Q P A', 'QPA')
            .replace('I D R', 'IDR')
            .replace('N S A', 'NSA')
            .replace('O P ', 'OP ')
            .replace('eEOB', 'e EOB')
            .replace('I D C', 'ID C'),
        };
      });

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
        // queryString params may user is attempting to create a new Dispute
        const id = data.id.toLowerCase();
        if (id === 'find') {
          this.loadPrerequisites(this.route.snapshot.queryParams.dispute);
        } else if (id === 'create') {
          const qClaims = this.route.snapshot.queryParams.claims;
          const qCPT = this.route.snapshot.queryParams.cpt;
          const qAuthority =
            this.route.snapshot.queryParams.auth?.toLowerCase(); // which authority are we creating this for
          const qAuthorityCaseId = this.route.snapshot.queryParams.aid; // optional - can init the "dispute id"

          if (!qClaims || !qCPT || !qAuthority) {
            UtilService.PendingAlerts.push({
              level: ToastEnum.danger,
              message:
                'Not enough parameters to create a new batch. Provide claims, cpt and auth.',
            });
            this.router.navigateByUrl('/');
          } else {
            this.initDispute(qClaims, qCPT, qAuthority);
          }
        } else if (!isNaN(id)) {
          // load existing batch using integer id
          this.id = Number(id);
          this.loadPrerequisites();
        }
      } else {
        // force user to either pass in an ID or some initial starting values since the
        // origin of all new batches will be an external link for now
        UtilService.PendingAlerts.push({
          level: ToastEnum.danger,
          message: 'Missing batch identifier in URL. Redirecting to home...',
        });
        this.router.navigateByUrl('/');
      }
    });
  }

  addFee() {
    if (!this.canAddFees) return;

    this.svcToast.show(ToastEnum.info, 'Coming soon!');
    /*
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
    */
  }

  addDisputeFile(e: any) {
    if (!(e instanceof FileUploadEventArgs)) return;

    const args = e as FileUploadEventArgs;

    if (!args.file || !args.documentType) return;

    this.svcUtil.showLoading = true;

    const lowDt = e.documentType;
    const ada = new AuthorityDisputeAttachment({
      id: 0,
      authorityDisputeId: this.displayDispute.id,
      blobName: args.filename,
      docType: lowDt,
    });

    this.svcData.uploadDisputeDocument(args.file, ada).subscribe({
      next: (data) => {
        e.element.value = '';
        this.svcToast.show(ToastEnum.success, 'File uploaded successfully!');
        const attachments = this.allDisputeFileVMs$.getValue();
        const n = new CaseFileVM({
          AuthorityCaseId: data.authorityDisputeId,
          blobName: data.blobName, //`${lowDt}-authority-${this.currentAuthority!.id}-${e.filename.toLowerCase()}`,
          createdOn: new Date(),
          DocumentType: data.docType,
          Id: data.id + '',
          UpdatedBy: data.updatedBy,
        });
        const ndx = attachments.findIndex((v) => v.Id === n.Id);
        if (ndx > -1) attachments.splice(ndx, 1);
        attachments.push(n);
        this.allDisputeFileVMs$.next(attachments);
      },
      error: (err) => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger, err, 'Upload Failed');
      },
      complete: () => (this.svcUtil.showLoading = false),
    });
  }

  arbitratorChange() {
    if (!this.selectedArbitrator) return;
    this.svcUtil.showLoading = true;
    this.svcData.getArbitratorById(this.selectedArbitrator.id).subscribe(
      (data) => {
        const i = this.allArbitrators.findIndex((v) => v.id === data.id);
        this.allArbitrators[i].fees = data.fees;
        this.displayDispute.arbitratorSelectedOn = new Date();
      },
      (err) => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(
          ToastEnum.danger,
          'Error retrieving the list of Fees for the selected Arbitrator! Try refreshing the page and trying again.'
        );
      },
      () => (this.svcUtil.showLoading = false)
    );
  }

  authorityStatusChange() {}

  briefApproved() {
    if (!this.currentUser) return;
    if (
      !this.displayDispute.briefPreparationCompletedOn ||
      !this.displayDispute.briefWriterCompletedOn
    ) {
      this.svcToast.show(
        ToastEnum.danger,
        'Cannot approve a Brief that has not been Prepared and Writen!'
      );
      return;
    }
    this.displayDispute.briefApprovedBy = this.currentUser.email;
    this.displayDispute.briefApprovedOn = new Date();
    this.svcToast.showAlert(
      ToastEnum.warning,
      'To fully Approve, please save your changes'
    );
  }

  briefPrepCompletion(isDone: boolean) {
    if (
      !!this.displayDispute.briefWriterCompletedOn ||
      !!this.displayDispute.briefApprovedOn
    )
      return;
    if (isDone) this.displayDispute.briefPreparationCompletedOn = new Date();
    else this.displayDispute.briefPreparationCompletedOn = undefined;

    this.batchForm.form.markAsTouched();
    this.batchForm.form.markAsDirty();
  }

  briefWriterCompletion(isDone: boolean) {
    if (!!this.displayDispute.briefApprovedOn) return;
    if (isDone) this.displayDispute.briefWriterCompletedOn = new Date();
    else this.displayDispute.briefWriterCompletedOn = undefined;

    this.batchForm.form.markAsTouched();
    this.batchForm.form.markAsDirty();
  }

  canDeactivate(): Observable<boolean> {
    return this.isNavigationAllowed();
  }

  caseFileChanged(e: any) {}

  createNote() {
    if (!this.newNote) return;
    if (this.isNew || this.displayDispute.id < 1) {
      this.svcToast.show(
        ToastEnum.warning,
        'Save the current Dispute before adding Notes.'
      );
      return;
    }
    this.svcUtil.showLoading = true;
    const n = new AuthorityDisputeNote({
      id: 0,
      authorityDisputeId: this.displayDispute.id,
      details: this.newNote,
    });

    this.svcData.createDisputeNote(n).subscribe(
      (data) => {
        this.svcToast.show(ToastEnum.success, 'Note added successfully!');
        this.displayDispute.notes.push(data);
        this.caseNotes = UtilService.StringifyCaseNotes(
          this.displayDispute.notes
        );
        this.newNote = '';
      },
      (err) => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
      () => (this.svcUtil.showLoading = false)
    );
  }

  currencyBlur(e: any) {
    this.fixTo2Digits(e.target);
  }

  deleteFee(f: AuthorityDisputeFee) {
    this.svcToast.show(ToastEnum.info, 'Coming soon!');
  }

  deleteDisputeFile(e: any) {
    if (!this.currentAuthority?.id || !e || !(e instanceof CaseFileVM)) return;

    const f = e as CaseFileVM;
    if (
      !confirm(
        'Are you sure you want to permanently delete this resource file from the Dispute?'
      )
    )
      return;

    this.svcUtil.showLoading = true;
    const ada = new AuthorityDisputeAttachment({
      id: parseInt(f.Id),
      blobLink: '',
      blobName: f.blobName,
      docType: f.docType,
      authorityDisputeId: this.displayDispute.id,
    });
    this.svcData.deleteDisputeFile(ada).subscribe(
      (data) => {
        this.svcToast.show(ToastEnum.success, 'File deleted');
        const a = this.allDisputeFileVMs$.getValue();
        const ndx = a.findIndex((d) => parseInt(d.Id) === ada.id);
        if (ndx > -1) {
          a.splice(ndx, 1);
          this.allDisputeFileVMs$.next(a);
        }
      },
      (err) => {
        this.svcUtil.showLoading = false;
        this.svcToast.showAlert(
          ToastEnum.danger,
          err.message ?? err.toString()
        );
      },
      () => (this.svcUtil.showLoading = false)
    );
  }

  /** Sends requests to the server to delete fees that were removed by switching Arbitrators */
  deleteOldFees() {
    if (this.removedFeeIds.length === 0) return;

    for (let id of this.removedFeeIds) {
      this.svcData.deleteAuthorityDisputeFee(id).subscribe(
        (data) => console.log(`Removed AuthorityDisputeFee ${id} successfully`),
        (err) =>
          console.error(
            `Unable to delete AuthorityDisputeFee ${id}: ${UtilService.ExtractMessageFromErr(
              err
            )}`
          )
      );
    }
    this.removedFeeIds = [];
  }

  disputeNumberChange() {
    this.displayDispute.authorityCaseId =
      this.displayDispute.authorityCaseId.toUpperCase();
  }

  editFee(e: any) {}

  feeChanged(f: AuthorityDisputeFee) {
    this.batchForm.form.markAsTouched();
    this.batchForm.form.markAsDirty();
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

  configureIneligibilityActions() {
    this.allIneligibilityActions.length = 0;

    if (this.displayDispute.authority?.key.toLowerCase() === 'nsa') {
      // swap in the NSA ineligibility options to drive the UI
      this.allIneligibilityActions = JSON.parse(
        JSON.stringify(this.allNSAIneligibilityActions)
      );
    } else if (!!this.appSettings) {
      // swap in the State ineligibility options
      this.allIneligibilityActions = JSON.parse(
        JSON.stringify(this.appSettings.stateActionList)
      );
    }

    if (
      !!this.displayDispute.ineligibilityAction &&
      this.allIneligibilityActions.indexOf(
        this.displayDispute.ineligibilityAction
      ) == -1
    ) {
      this.allIneligibilityActions.push(
        this.displayDispute.ineligibilityAction
      );
    }
    this.allIneligibilityActions.sort(UtilService.SortSimple);
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

  getArbitratorSelection() {
    if (!!this.displayDispute.arbitrator) {
      this.selectedArbitrator = this.allArbitrators.find(
        (v) => v.id === this.displayDispute.arbitrator!.id
      );
    }
    const mref = this.svcModal.open(this.pickArbModal);
    mref.result.then(
      (data) => {
        if (!this.selectedArbitrator) return;
        const fees = this.displayDispute.fees.find(
          (v) => v.feeRecipient === FeeRecipient.Arbitrator
        );
        if (!!fees) {
          if (
            !confirm(
              'Warning! This will replace all Arbitrator/Entity Fees currently associated with this Dispute. ARE YOU SURE?'
            )
          )
            return;
          this.removedFeeIds = this.displayDispute.fees
            .filter((v) => v.feeRecipient === FeeRecipient.Arbitrator)
            .map((d) => d.id);
          this.displayDispute.fees = this.displayDispute.fees.filter(
            (v) => v.feeRecipient !== FeeRecipient.Arbitrator
          );
        }

        this.displayDispute.arbitrator = this.selectedArbitrator;
        this.displayDispute.arbitratorId = this.selectedArbitrator.id;
        for (let f of this.selectedArbitrator.fees) {
          let fee = new AuthorityDisputeFee();
          fee.baseFee = f;
          fee.baseFeeId = f.id;
          fee.authorityDisputeId = this.displayDispute.id;
          fee.amountDue = f.feeAmount; // TODO: Prob need a getter on the fee that calculates this based on CPTs or other
          //NOTE: fee.dueOn is calculated by the API
          fee.feeRecipient = FeeRecipient.Arbitrator;
          fee.isRefundable = f.isRefundable;
          fee.isRequired = f.isRequired;
          this.displayDispute.fees.push(fee);
        }
        this.batchForm.form.markAsTouched();
        this.batchForm.form.markAsDirty();
        this.svcToast.showAlert(
          ToastEnum.warning,
          "You must click 'Save Changes' to keep the Arbitrator and Fees."
        );
        this.svcToast.show(
          ToastEnum.info,
          'Arbitrator fees added. Click Save Changes to keep.'
        );
      },
      (reason) => console.log('Canceled arbitrator selection')
    );
  }

  handleGridNav(event: any) {
    const charCode = event.which ? event.which : event.keyCode;

    if (this.CONTROL_KEYS.indexOf(charCode) === -1) return true;

    const m = event.target; // as HTMLElement;
    const t = m.id.split('_');
    if (t.length < 2) return true;

    let value = t[t.length - 1];
    if (isNaN(value)) return true;

    value = parseInt(value);

    if (charCode === this.ENTER) {
      this.focusNextElement(m as HTMLElement);
    } else if (charCode === this.UP) {
      if (value) {
        t[t.length - 1] = value - 1;
        const z = document.getElementById(t.join('_'));
        if (!!z) z.focus();
      }
      return false;
    } else if (charCode === this.DOWN) {
      t[t.length - 1] = value + 1;
      const z = document.getElementById(t.join('_'));
      if (!!z) z.focus();
      return false;
    }
    /* see insane SO thread about why this chicanery is necessary:
    // https://stackoverflow.com/questions/21177489/selectionstart-selectionend-on-input-type-number-no-longer-allowed-in-chrome
    else if(charCode === this.LEFT){
      console.log(`start: ${event.target.selectionStart}`);
    } else if(charCode === this.RIGHT){
      console.log(`start: ${event.target.selectionStart}`);
    }
    */
    return true;
  }

  readonly numericKeys = [
    8, 46, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 96, 97, 98, 99, 100, 101,
    102, 103, 104, 105, 110, 190,
  ];

  handleGridNavNumeric(event: any) {
    const charCode = event.which ? event.which : event.keyCode;
    console.log(charCode);
    // Only Numbers 0-9
    if (this.CONTROL_KEYS.indexOf(charCode) > -1) {
      const r = this.handleGridNav(event);
      if (!r) {
        event.preventDefault();
      }
      return true;
    } else if (this.numericKeys.indexOf(charCode) === -1) {
      event.preventDefault();
      return false;
    } else {
      return true;
    }
  }

  initAuthority(auth: Authority) {
    this.trackingDetailsList.length = 0;
    this.currentAuthority = auth;
    this.authKey = auth.key.toLowerCase();
    if (this.authKey === 'nsa') this.arbTypeLabel = 'Certified Entity';

    this.allAuthorityStatuses = this.currentAuthority!.statusList;
    this.trackingDetailsList = this.currentAuthority.trackingDetails.filter(
      (v) =>
        v.scope === AuthorityTrackingDetailScope.All ||
        v.scope == AuthorityTrackingDetailScope.AuthorityDispute
    );

    if (!this.trackingDetailsList.length) {
      UtilService.PendingAlerts.push({
        level: ToastEnum.danger,
        message:
          'Unexpected Authority Dispute data: Missing Authority tracking configurations!',
      });
      this.router.navigate(['/', 'search']);
      return;
    }
  }

  initDispute(claims: string, cpt: string, authKey: string) {
    this.svcUtil.showLoading = true;
    this.svcData.initAuthorityDispute(claims, cpt, authKey).subscribe(
      (data) => {
        // only pull down other stuff if we init successfully
        const settings$ = this.svcData.getAppSettings();
        const users$ = this.svcData.loadUsers();
        combineLatest([settings$, users$]).subscribe(
          ([settings, users]) => {
            // users
            this.allUsers = users;
            this.allUsers.sort(UtilService.SortByEmail);
            // AppSettings
            this.appSettings = settings;
            // let's go!
            this.initViewModel(data);
          },
          (err) => {
            UtilService.PendingAlerts.push({
              level: ToastEnum.danger,
              message: UtilService.ExtractMessageFromErr(err),
            });
            this.router.navigateByUrl('/');
          }
        );
      },
      (err) => {
        UtilService.PendingAlerts.push({
          level: ToastEnum.danger,
          message: UtilService.ExtractMessageFromErr(err),
        });
        this.router.navigateByUrl('/');
      },
      () => (this.svcUtil.showLoading = false)
    );
  }

  initViewModel(data: AuthorityDispute) {
    this.canEdit = false;
    this.allAuthorityStatuses.length = 0;
    this.isBriefApprover = false;
    this.isBriefPreparer = false;
    this.isBriefWriter = false;
    this.trackingObject = null;

    if (!data.authorityId || !data.authority) {
      UtilService.PendingAlerts.push({
        level: ToastEnum.danger,
        message: 'Unexpected Authority Dispute data: Missing Authority info!',
      });
      this.router.navigate(['/', 'search']);
      return;
    }

    if (data.cptViewmodels.length === 0) {
      UtilService.PendingAlerts.push({
        level: ToastEnum.danger,
        message: 'Unable to locate any associated CPTs for the dispute number',
      });
      this.router.navigate(['/', 'search']);
      return;
    }

    // get list of files in the background
    const claimIDs = new Array<number>();
    data.cptViewmodels.forEach((v) => {
      if (!v.claimCPT) return;
      const x = v.claimCPT?.arbitrationCaseId;
      if (x === 0) return;
      if (claimIDs.indexOf(x) !== -1) return;
      claimIDs.push(x);
    });

    this.initAuthority(data.authority);

    this.origDispute = new AuthorityDispute(data);
    this.displayDispute = new AuthorityDisputeVM(data);

    this.svcData.getClaimAttachmentEntries(claimIDs).subscribe(
      (rec) => {
        const values = rec.map((v) => new CaseFileVM(v));
        this.allCaseFileVMs$.next(values);
      },
      (err) => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast)
    );

    // post-init cleanup
    this.isNew = !data.id;
    this.canEdit =
      ((this.authKey === 'nsa' && this.isNSA) ||
        (this.authKey !== 'nsa' && this.isState)) &&
      (this.isManager || this.isNegotiator) &&
      UtilService.IsAnOpenStatus(this.origDispute.workflowStatus);

    this.batchForm.form.markAsUntouched();
    this.batchForm.form.markAsPristine();

    this.setTrackingObject(this.trackingDetailsList);

    this.configureIneligibilityActions();

    if (!this.isNew) {
      this.caseNotes = UtilService.StringifyCaseNotes(
        this.displayDispute.notes
      );
      const values = this.displayDispute.attachments.map(
        (data) =>
          new CaseFileVM({
            AuthorityCaseId: data.authorityDisputeId,
            blobName: data.blobName, //`${lowDt}-authority-${this.currentAuthority!.id}-${e.filename.toLowerCase()}`,
            createdOn: new Date(),
            DocumentType: data.docType,
            Id: data.id + '',
            UpdatedBy: data.updatedBy,
          })
      );
      this.allDisputeFileVMs$.next(values);

      // set role flags for use by UI
      this.isBriefApprover = !!this.currentUser?.isBriefApprover;
      this.isBriefPreparer =
        this.currentUser?.emailLowerCase ===
        this.displayDispute.briefPreparer.toLowerCase();
      this.isBriefWriter =
        this.currentUser?.emailLowerCase ===
        this.displayDispute.briefWriter.toLowerCase();

      // fix up the URL after creation
      const r = this.route.snapshot.params.id.toLowerCase();
      if (r === 'create' || r === 'find') {
        //Build URL Tree
        const urlTree = this.router.createUrlTree([`/batch/${data.id}`], {
          relativeTo: this.route,
          queryParams: undefined,
          queryParamsHandling: '',
        });

        //Update the URL
        this.location.replaceState(urlTree.toString(), '');
      }
    }
    // this is a hack to correct the number of decimals - need to find time to make a directive to do it
    setTimeout(() => {
      let names: string[] = [];
      if (this.displayDispute.cptViewmodels.length) {
        names = ['finalOfferAmount', 'benchmarkOverride'];
        for (let i = 0; i < this.displayDispute.cptViewmodels.length; i++) {
          for (const n of names) {
            let d = document.getElementById(`${n}_${i}`);
            if (d) this.fixTo2Digits($(d));
          }
        }
      }
      if (this.displayDispute.fees.length) {
        names = ['amountDue', 'refundableAmount', 'refundAmount'];
        for (let i = 0; i < this.displayDispute.fees.length; i++) {
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
        if (this.batchForm && this.batchForm.dirty) {
          // && !this.isSaving
          if (beforeunloadEvent) {
            resolve(false);
          } else {
            this.svcModal
              .open(this.confirmationModal, this.modalOptions)
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

  loadPrerequisites(caseId: string = '') {
    if (!caseId && !this.id) {
      UtilService.PendingAlerts.push({
        level: ToastEnum.danger,
        message: 'Bad or missing Dispute identifier.',
      });
      this.router.navigateByUrl('/');
      return;
    }

    const users$ = this.svcData.loadUsers();
    const data$ = !caseId
      ? this.svcData.getAuthorityDispute(this.id)
      : this.svcData.findAuthorityDispute(caseId);
    const settings$ = this.svcData.getAppSettings();
    //const settlements$ = this.svcData.getSettlementsByDisputeId(this.id);

    combineLatest([users$, data$, settings$]).subscribe(
      ([users, data, settings]) => {
        this.initViewModel(data);
        // users
        this.allUsers = users;
        this.allUsers.sort(UtilService.SortByEmail);

        // case settlements - test using ArbitrationCaseId 93319
        //this.disputeSettlements$.next(settlements);

        // AppSettings
        this.appSettings = settings;
      },
      (err) => {
        UtilService.PendingAlerts.push({
          level: ToastEnum.danger,
          message: UtilService.ExtractMessageFromErr(err),
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
        if (!!this.trackingObject[e.target.id] && e.target.id)
          this.trackingObject[e.target.id] = null;
      }
    }

    this.batchForm.form.markAsDirty();
    UtilService.UpdateTrackingCalculations(
      this.trackingDetailsList,
      this.trackingObject,
      false,
      this.displayDispute
    );
  }

  onSubmit() {
    if (!this.batchForm.valid || !this.displayDispute) return false;

    if (!this.displayDispute.cptViewmodels.length) return; // should not happen

    // basic client-side validation - could possibly move this to the object class and return a list of validation messages to show as alerts
    let isValid = true;
    if (
      this.displayDispute.cptViewmodels.find(
        (v) => !v.calculatedOfferAmount && !v.finalOfferAmount
      )
    ) {
      isValid = false;
      this.svcToast.showAlert(
        ToastEnum.danger,
        'One or more lines do not have valid offers!'
      );
    }

    if (
      this.displayDispute.cptViewmodels.find((v) => !v.effectiveBenchmarkAmount)
    ) {
      isValid = false;
      this.svcToast.showAlert(
        ToastEnum.danger,
        'One or more lines do not have a Benchmark. Benchmarks are required.'
      );
    }

    isValid =
      isValid &&
      !!this.displayDispute.authorityCaseId &&
      !!this.displayDispute.submissionDate;
    if (!isValid) {
      this.svcToast.show(
        ToastEnum.danger,
        'The record contains missing or invalid information. Cannot save.'
      );
      return false;
    }

    // save NSA tracking info
    this.displayDispute.trackingValues = JSON.stringify(this.trackingObject);

    this.svcUtil.showLoading = true;

    // pick the right endpoint
    if (this.displayDispute.id < 1) {
      this.svcData.createAuthorityDispute(this.displayDispute).subscribe(
        (data) => {
          this.svcToast.show(
            ToastEnum.success,
            'Dispute created successfully!'
          );
          this.svcToast.resetAlerts();
          this.initViewModel(data);
        },
        (err) => {
          UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast);
        },
        () => (this.svcUtil.showLoading = false)
      );
    } else {
      this.svcData.updateAuthorityDispute(this.displayDispute).subscribe(
        (data) => {
          if (this.removedFeeIds) {
            data.fees = data.fees.filter(
              (v) => this.removedFeeIds.indexOf(v.id) === -1
            );
            this.deleteOldFees(); // background task
          }
          this.svcToast.show(
            ToastEnum.success,
            'Dispute updated successfully!'
          );
          this.svcToast.resetAlerts();
          this.initViewModel(data);
        },
        (err) => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
        () => (this.svcUtil.showLoading = false)
      );
    }
    return true;
  }

  openSettlementDialog(e: CaseSettlement) {
    console.log(e);
  }

  payFee(f: AuthorityDisputeFee) {
    if (!f || !!f.paidOn || !this.currentUser) return;
    this.currentFee = f;
    const mref = this.svcModal.open(this.payFeeModal);
    mref.closed.subscribe(
      (data) => {
        if (
          !this.currentFee?.paymentMethod ||
          !this.currentFee?.paymentReferenceNumber
        )
          return;
        this.currentFee.paidOn = new Date();
        this.currentFee.paidBy = this.currentUser!.email;
        this.batchForm.form.markAsTouched();
        this.batchForm.form.markAsDirty();
        this.svcToast.showAlert(
          ToastEnum.info,
          'Your Fee Payment will not be recorded until you click Save Changes.'
        );
      },
      (err) => console.error(err)
    );
  }

  /* rerender(): void {
    // Call the dtTrigger to rerender current data again before destroying
    //this.dtTrigger.next();

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
  */

  refreshLog() {
    this.allLogs.length = 0;
    this.isLogLoading = true;
    this.svcData.loadDisputeLog(this.displayDispute.id).subscribe(
      (data) => {
        this.isLogLoading = false;
        this.allLogs = data;
        this.allLogs.sort(UtilService.SortByCreatedOnDesc);
        this.allLogs.forEach(
          (g) => (g.details = g.details.replaceAll('\\u0022', '"'))
        );
      },
      (err) => {
        this.isLogLoading = false;
        this.svcToast.show(ToastEnum.danger, err.message ?? err.toString());
      }
    );
  }

  resetFormStatus() {
    this.batchForm.form.markAsUntouched();
    this.batchForm.form.markAsPristine();
    Object.keys(this.batchForm.controls).forEach((key) => {
      const control = this.batchForm.controls[key];
      control.markAsUntouched();
      control.markAsPristine();
    });
  }

  selectAll(e: any) {
    e?.target?.select();
  }

  selectArbitratorClick() {
    if (!this.allArbitrators.length) {
      this.svcUtil.showLoading = true;
      const t =
        this.authKey === 'nsa' ? ArbitratorType.CertifiedEntity : undefined;
      this.svcData.loadArbitrators(false, t, true).subscribe(
        (data) => {
          if (!data.length) {
            const m =
              this.authKey === 'nsa' ? 'Certified Entities' : 'Arbitrators';
            this.svcToast.show(
              ToastEnum.warning,
              `No active ${m} available. Use Manage Arbitrators to add or activate them.`,
              '',
              5000
            );
            return;
          }
          this.allArbitrators = data.filter((v) =>
            this.isNSA
              ? v.arbitratorType === ArbitratorType.CertifiedEntity
              : v.arbitratorType !== ArbitratorType.CertifiedEntity
          );
          this.allArbitrators.sort(UtilService.SortByName);
          this.getArbitratorSelection();
        },
        (err) => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
        () => (this.svcUtil.showLoading = false)
      );
    } else {
      this.getArbitratorSelection();
    }
  }

  setSubmissionDate(e: any) {}

  settlementAdded(settlement: CaseSettlement) {
    console.log(settlement);
  }

  setTrackingObject(details: AuthorityTrackingDetail[]) {
    this.trackingObject = null;
    if (!details.length) {
      UtilService.PendingAlerts.push({
        level: ToastEnum.danger,
        message: `Tracking Details configuration is empty. Cannot continue loading this Dispute.`,
      });
      this.router.navigateByUrl('/');
      return;
    }

    if (!!this.displayDispute.trackingValues) {
      try {
        this.trackingObject = JSON.parse(this.displayDispute.trackingValues);
      } catch (err) {
        this.svcToast.showAlert(
          ToastEnum.danger,
          'The Tracking info for this Dispute could not be parsed and must be reset! Either re-enter the fields or contact Tech Support immediately.'
        );
      }
    }

    if (!this.trackingObject)
      this.trackingObject = UtilService.CreateTrackingObject(details);

    if (this.trackingObject) {
      this.trackingObject = UtilService.TransformTrackingObject(
        details,
        this.trackingObject,
        true
      );
      const cloned = Object.assign({}, this.trackingObject);
      if (!this.displayDispute.isClosed) {
        UtilService.UpdateTrackingCalculations(
          details,
          this.trackingObject,
          false,
          this.displayDispute
        );
        if (JSON.stringify(cloned) !== JSON.stringify(this.trackingObject)) {
          // note: this appears to be getting triggered du to a difference in millisecond precision e.g. T12:00:00.1Z vs T12:00:00.100Z
          this.batchForm.form.markAsDirty();
          this.batchForm.form.markAsTouched();
          this.svcToast.showAlert(
            ToastEnum.danger,
            'The calculated State Dates and Deadlines appear different than the saved values. Please click Save Changes to keep the on-screen calculations.'
          );
        }
      }
    }
  }

  submissionDateChanged() {
    UtilService.UpdateTrackingCalculations(
      this.trackingDetailsList,
      this.trackingObject,
      false,
      this.displayDispute
    );
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

  syncCPTValue(vm: AuthorityDisputeCPTVM, s: string) {
    const objs = this.displayDispute.cptViewmodels.filter(
      (v) =>
        v.claimCPT?.arbitrationCaseId !== vm.claimCPT?.arbitrationCaseId &&
        v.claimCPT?.cptCode === vm.claimCPT?.cptCode &&
        v.geoZip === vm.geoZip
    );
    if (!objs.length) return;
    for (let b of objs)
      if (s === 'bmo') b.benchmarkOverride = vm.benchmarkOverride;
      else if (s === 'fo') b.finalOfferAmount = vm.finalOfferAmount;
  }

  toggleFeeDetails(f: AuthorityDisputeFee) {
    f.isExpanded = !f.isExpanded;
  }

  undoChanges() {
    if (this.isNew) {
      if (!confirm('Cancel the creation of this new Dispute?')) return;
      this.router.navigate(['/search']);
      return;
    }

    if (!confirm('ARE YOU SURE you want to Undo all of your unsaved changes?'))
      return;

    const data = new AuthorityDispute(this.origDispute);
    this.initViewModel(data);
    this.removedFeeIds = [];
    this.svcToast.resetAlerts();
  }

  viewFile(e: any) {
    if (!this.currentAuthority || !e || !(e instanceof CaseFileVM)) return;
    const f = e as CaseFileVM;
    this.svcData
      .downloadPDFForBatch(f.AuthorityCaseId, f.blobName)
      .pipe(take(1))
      .subscribe((res) => {
        const fileURL = URL.createObjectURL(res);
        window.open(fileURL, '_blank');
      });
  }

  DisableDeleteCPTFromDispute(): boolean {
    if (!this.isAdmin && !this.isManager) {
      console.warn("User don't have permission");
      return true;
    }
    if (!this.canAddFees) {
      var id = 0;
      if (this.origDispute?.workflowStatus != null) {
        id = this.origDispute?.workflowStatus;
      }
      console.warn(
        'Can not delete this CPTFromDispute. Dispute Status: ' +
          CMSCaseStatus[id]
      );
      return true;
    }
    return false;
  }

  DeleteCPTFromDispute(
    authorityDisputeId: number,
    claimCPTId: any,
    cptCode: any,
    arbitrationCaseId: any
  ) {
    console.log(
      'DeleteCPTFromDispute called for authorityDisputeId: ' +
        authorityDisputeId +
        ', claimCPTId: ' +
        claimCPTId
    );
    var sure = confirm('Are you sure to delete this record.');
    if (!sure) {
      console.warn('DeleteCPTFromDispute action was canceled by user');
    } else {
      console.warn(
        'DeleteCPTFromDispute action was confirmed by user authorityDisputeId: ' +
          authorityDisputeId +
          ', claimCPTId: ' +
          claimCPTId
      );
      console.log(
        'calling deleteCPTFromDispute authorityDisputeId: ' +
          authorityDisputeId +
          ', claimCPTId: ' +
          claimCPTId
      );
      var res = this.svcData
        .deleteCPTFromDispute(authorityDisputeId, claimCPTId)
        .subscribe({
          next: (respone) => {
            console.warn(
              respone.status +
                'CPTFromDispute is deleted authorityDisputeId: ' +
                authorityDisputeId +
                ', claimCPTId: ' +
                claimCPTId
            );
            UtilService.PendingAlerts.push({
              level: ToastEnum.danger,
              message: 'CPTFromDispute is deleted',
            });
            location.reload();
          },
          error: (error) => {
            console.error('Could not delete CPTFromDispute ' + error.status);
            UtilService.PendingAlerts.push({
              level: ToastEnum.danger,
              message: 'Could not delete CPTFromDispute',
            });
          },
        });
    }
  }
}
