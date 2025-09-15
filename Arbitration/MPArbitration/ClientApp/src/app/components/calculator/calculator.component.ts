import { ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ArbitrationCase } from 'src/app/model/arbitration-case';
import { CaseDataService } from 'src/app/services/case-data.service';
import { combineLatest, forkJoin, from, Observable, Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';
import { ClaimCPT } from 'src/app/model/claim-cpt';
import { UtilService } from 'src/app/services/util.service';
import { NgbCollapse, NgbDateAdapter, NgbDateParserFormatter, NgbInputDatepickerConfig, NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { ToastService } from 'src/app/services/toast.service';
import { ToastEnum } from 'src/app/model/toast-enum';
import { Benchmark } from 'src/app/model/benchmark';
import { CalculatorVariables } from 'src/app/model/calculator-variables';
import { NegotiatorComponent } from '../negotiator/negotiator.component';
import { Negotiator } from 'src/app/model/negotiator';
import { CaseWorkflowAction, CaseWorkflowParams } from 'src/app/model/case-workflow-params';
import { CustomDateParserFormatter, CustomNgbDateAdapter } from 'src/app/model/custom-date-handler';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import DataLabelsPlugin from 'chartjs-plugin-datalabels';
import { CMSCaseStatus } from 'src/app/model/arbitration-status-enum';
import { CaseFileVM } from 'src/app/model/case-file';
import { DataTableDirective } from 'angular-datatables';
import { AuthService } from 'src/app/services/auth.service';
import { Template } from '@angular/compiler/src/render3/r3_ast';
import { loggerCallback } from 'src/app/app.module';
import { LogLevel } from '@azure/msal-browser';
import { Note } from 'src/app/model/note';
import { AppUser, UserAccessType, UserRoleType } from 'src/app/model/app-user';
import { Authority } from 'src/app/model/authority';
import { TDIRequestDetails } from 'src/app/model/tdi-request-details';
import { CaseLog } from 'src/app/model/case-log';
import { Payor } from 'src/app/model/payor';
import { AddOfferComponent } from '../add-offer/add-offer.component';
import { OfferHistory } from 'src/app/model/offer-history';
import { Customer } from 'src/app/model/customer';
import { CaseTracking } from 'src/app/model/case-tracking';
import { AuthorityTrackingDetail } from 'src/app/model/authority-tracking-detail';
import { BenchmarkDataItem } from 'src/app/model/benchmark-data-item';
import { AuthorityBenchmarkDetails } from 'src/app/model/authority-benchmark-details';
import { BenchmarkDataset } from 'src/app/model/benchmark-dataset';
import { BenchmarkGraphSet } from 'src/app/model/benchmark-graph-set';
import { IArbitrator, IArbStats } from 'src/app/model/arbitrator';
import { AcceptOfferComponent } from '../accept-offer/accept-offer.component';
import { CaseDocumentType } from 'src/app/model/case-document-type-enum';
import { IKeyId } from 'src/app/model/iname';
import { HttpErrorResponse } from '@angular/common/http';
import { CaseArchive } from 'src/app/model/case-archive';
import { PlanType } from 'src/app/model/payor-group';
import { Entity } from 'src/app/model/entity';
import { SettlementDialogComponent } from '../settlement-dialog/settlement-dialog.component';
import { CaseSettlement } from 'src/app/model/case-settlement';
import { Notification, NotificationAttachment, NotificationDeliveryInfo, NotificationRecipient } from 'src/app/model/notification';
import { NotificationType } from 'src/app/model/notification-type-enum';
import { NotificationDeliveryDialogComponent } from '../notification-delivery-dialog/notification-delivery-dialog.component';
import { PlaceOfServiceCode } from 'src/app/model/place-of-service-code';
import { DisputeLinkVM } from 'src/app/model/dispute-link-vm';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-calculator',
  templateUrl: './calculator.component.html',
  styleUrls: ['./calculator.component.css'],
  providers: [
    { provide: NgbDateAdapter, useClass: CustomNgbDateAdapter },
    { provide: NgbDateParserFormatter, useClass: CustomDateParserFormatter },
    NgbInputDatepickerConfig
  ]
})
export class CalculatorComponent implements OnDestroy, OnInit {
  @ViewChild('archiveDialog')
  archiveModal: Template | undefined;

  @ViewChild('caseFile', { static: false })
  caseFile: ElementRef | undefined;

  @ViewChild('collapseLog', { static: false })
  collapseLog: NgbCollapse | undefined;

  @ViewChild('confirmationDialog')
  confirmationModal: Template | undefined;

  @ViewChild(BaseChartDirective)
  chart: BaseChartDirective | undefined;

  @ViewChild('calcForm', { static: false })
  calcForm!: NgForm;

  @ViewChild(DataTableDirective, { static: false })
  dtElement: DataTableDirective | undefined;

  readonly CMSCaseStatus = CMSCaseStatus;
  readonly NotificationType = NotificationType;
  activeAuthority: Authority | undefined | null;
  allAuthorities: Authority[] = [];
  allBenchmarks: Benchmark[] = [];
  allBenchmarkDatasets: BenchmarkDataset[] = [];
  allCustomers: Customer[] = [];
  allDSToggle: Array<BenchmarkGraphSet> = [];
  allGraphData: Array<{ datasetId: number, cpt: string, ext50thValue: number, ext80thValue: number }> = [];

  allLogs: CaseLog[] = [];
  allNSAStatuses: string[] = [];
  allowNavigation: Subject<boolean> = new Subject<boolean>();
  allPayors: Payor[] = [];
  arbCase: ArbitrationCase = new ArbitrationCase();
  allCalcVariables: CalculatorVariables[] = [];
  allCaseFileVMs = new Array<CaseFileVM>();
  allDocTypes = new Array<IKeyId>(); // ['Brief','Check','Correspondence','EOB','HCFA','NSARequestAttachment'];
  allEntities = new Array<Entity>();

  // this list of actions is just a way to normalize the wordy reasons into something easier to deal with when searching or reporting - applies to NSA and Local/State
  allNSAIneligibilityActions = ['Client Removed Assignment', 'Denial', 'Duplicate', 'Entity Closed', 'Facility is out-of-network', 'Ineligible Plan', 'Other - Manager Review', 'Paid in Full', 'Provider is in-network', 'State Arbitration', 'Timing']; allStateIneligibilityActions: Array<string> = []; // = ['Batching','Denial','Duplicate','High Reimbursement','Incorrect Claim Data','NSA','Other Payor is Primary','Out of State Policy','Patient Elected OON Services','Timing'];
  allNegotiators: Negotiator[] = [];
  allArchivedCases: CaseArchive[] = [];
  allPlaceCodes: PlaceOfServiceCode[] = [];

  allProviderTypes = ['Ambulatory Surgical Center', 'Anesthesiologist', 'Assistant Surgeon', 'Birthing Center', 'Doctor of Medicine', 'Doctor of Osteopathic Medicine', 'Emergency', 'Emergency Department Physician', 'Freestanding Emergency Medical Center', 'Hospital', 'Hospitalist', 'Neonatologist', 'Neurologist', 'Neuromonitor', 'Neurophysiologist', 'Nurse Anesthetist', 'Nurse Practitioner', 'Other', 'Pathologist', 'Physician Assistant', 'Radiologist', 'Surgeon'];
  archiveReason = '';
  authorityStatuses = ['Not Submitted'];

  calculatorVars: CalculatorVariables | undefined;
  canEdit = false;
  canCreate = false;
  canDelete = false;
  caseNotes = '';
  chartTheme = ['#F7B2B2', '#D9C29C', '#C5CE8E', '#B9D586', '#9DE573', '#99C579', '#78FA5B'];
  confirmationTitle = 'Confirm Navigation';
  confirmationMessage = 'Warning! You have unsaved changes. Close without saving?';
  currentCustomer: Customer | undefined;
  currentUser: AppUser | undefined;
  defaultBenchmark: AuthorityBenchmarkDetails | undefined;
  destroyed$ = new Subject<void>();
  documentType: CaseDocumentType | null = null;

  dtOptions: DataTables.Settings = {};
  dtTrigger: Subject<any> = new Subject<any>();

  hasPendingAcceptance = false;
  hideArbitrators = true;
  hideAuth = true;
  hideCaseDetail = true;
  hideCpt = false;
  hideDates = true;
  hideFiles = true;
  hideLog = true;
  hideMoreInfo = true;
  hideNotes = true;
  hideNSA = true;
  hideNSANotify = true;
  hideStateSettlement = false;
  hideWorkflowProgress = true;

  id = 0;
  isCaseActive = false;
  isConfirmNavigationOpen = false;
  isDev = false;
  isExcludedNPI = false;
  isGeoZipLocked = false;
  isLogLoading = false;
  isManager = false;
  isNegotiator = false;
  isNew = true;
  isNSA = false;
  isReporter = false;
  isSaving = false;
  isServiceLocked = true;
  isState = false;
  isZiplocked = false;

  Json = JSON;
  latestAuthorityData: TDIRequestDetails | undefined;
  lockAuthority = false;
  lockAuthorityId = false;
  lockNSAId = false; // TODO: Default this to true once the rules are more fleshed out
  lockNSA = false;
  modalOptions: NgbModalOptions | undefined;
  NSAAuthority: Authority | undefined;
  NSATrackingFieldsForUI: AuthorityTrackingDetail[] = [];
  NSATrackingObject: any = null;
  origCase: ArbitrationCase = new ArbitrationCase();
  newNote = '';
  previousCaseNumbers = 'none';
  proOrTech = '';
  resetAlerts = false;
  services: { name: string, serviceLine: string }[] = [];
  statuses = new Array<IKeyId>();
  trackingFieldsForUI: AuthorityTrackingDetail[] = [];

  trackingObject: any = null;

  testNotification: Notification | undefined;

  /*** Date structs for popups **
  arbBriefDueDate: string | undefined;
  arbDeadlineDate: string | undefined;
  assignmentDeadlineDate: string | undefined;
  DOB: string | undefined;
  EOBDate: string | undefined;
  firstAppealDate: string | undefined;
  firstResponseDate: string | undefined;
  informalTeleconferenceDate: string | undefined;
  paymentMadeDate: string | undefined;
  payorResolutionReqRcvdDate: string | undefined;
  providerPaidDate: string | undefined;
  requestDate: string | undefined;
  resolutionDeadlineDate: string | undefined;
  serviceDate: string | undefined;
  */

  /** CHART STUFF ***
  barChartOptions: ChartOptions = {
    responsive: true,
  };
  barChartLabels =  ['Apple', 'Banana', 'Kiwifruit', 'Blueberry', 'Orange', 'Grapes'];
  barChartType: ChartType = 'bar';
  barChartLegend = true;
  barChartPlugins = [];
  barChartData: ChartDataset[] = [
    { data: [45, 37, 60, 70, 46, 33], label: 'Best Fruits' }
  ];
  */
  /**** CHART STUFF ****/
  public barChartOptions: ChartConfiguration['options'] = {
    layout: {
      padding: {
        top: 20,
        bottom: 0,
        left: 20,
        right: 20
      }
    },
    responsive: true,
    events: [],
    // We use these empty structures as placeholders for dynamic theming.
    scales: {
      x: {
        ticks: {
          minRotation: 45,
          maxRotation: 90
        }
      },
      y: {
        min: 10
      }
    },
    plugins: {
      legend: {
        display: false,
      },
      datalabels: {
        anchor: 'end',
        align: 'end',
        formatter: (value, context) => {
          return '$' + context.chart.data.datasets[0].data[context.dataIndex];
        }
      }
    }
  };

  public barChartType: ChartType = 'bar';
  public barChartPlugins = [
    DataLabelsPlugin
  ];

  public barChartData: ChartData<'bar'> = {
    labels: ['Payor Final Offer', 'Provider Final Offer'],
    datasets: [
      { data: [0, 0] }
    ]
  };

  // events
  /*
  public chartClicked({ event, active }: { event?: ChartEvent, active?: {}[] }): void {
    console.log(event, active);
  }
  
  public chartHovered({ event, active }: { event?: ChartEvent, active?: {}[] }): void {
    console.log(event, active);
  }
  */
  /************** END CHART STUFF *************************/

  isNaN: Function = Number.isNaN;
  number: Function = Number;
  constructor(private svcData: CaseDataService, private route: ActivatedRoute,
    private router: Router, private svcAuth: AuthService,
    private svcToast: ToastService, private modalService: NgbModal,
    private svcUtil: UtilService, private svcChangeDetection: ChangeDetectorRef) {

    this.isDev = !environment.production;

    //config: NgbInputDatepickerConfig, calendar: NgbCalendar,
    this.statuses = Object.values(CMSCaseStatus).filter(value => typeof value === 'string' && value !== 'Search' && value !== 'Unknown').map(key => {
      const result = (key as string).split(/(?=[A-Z][a-z])/);
      return { id: (<any>CMSCaseStatus)[key] as number, key: result.join(' ') };
    });

    this.allDocTypes = Object.values(CaseDocumentType).filter(value => typeof value === 'string').map(key => {
      const result = (key as string).split(/(?=[A-Z]+[a-z])/);
      return { id: (<any>CaseDocumentType)[key] as number, key: result.join(' ').replace('PatientEOB', 'Patient EOB').replace('Q P A', 'QPA').replace('I D R', 'IDR').replace('N S A', 'NSA').replace('O P ', 'OP ').replace('eEOB', 'e EOB').replace('I D C', 'ID C') };
    });

    this.modalOptions = {
      backdrop: 'static',
      backdropClass: 'customBackdrop',
      keyboard: false,
    }
    /* uncomment below to test the notification section easily by going to New Case
    this.testNotification = new Notification();
    this.testNotification.id = 1;
    this.testNotification.arbitrationCaseId = 1;
    this.testNotification.cc = 'test@abcd.com';
    this.testNotification.customer='MPOWERHealth';
    this.testNotification.payorClaimNumber='02023FFFFFFFFFF0x00';
    this.testNotification.notificationType = NotificationType.NSANegotiationRequest;
    this.testNotification.to='kristin.kleinfelder@halomd.com';
    this.testNotification.sentOn=new Date('2023-10-13T19:46:36.5239814Z');
    this.testNotification.status='success';
    this.testNotification.submittedBy='john.baldwin@mpowerhealth.com';
    this.testNotification.submittedOn=new Date('2023-10-13T19:44:31.9198345Z');
    this.testNotification.updatedBy='noreply@appregistration.local';
    this.testNotification.updatedOn=new Date('2023-10-13T19:46:36.5239814Z');
    this.testNotification.JSON='{"payorId":2,"supplements":[],"delivery":{"deliveredOn":"2023-10-16T01:30:47Z","deliveryId":"","deliveryMethod":"","message":"","messageId":"RZ0YHTFnT2KVJ3ynLZ2QWQ","processedOn":"2023-10-16T01:30:21.8319837\u002B00:00","sender":"sendgrid","status":"delivered","attachments":[{"fileName":"85317-nsarequestattachment-08152023 siemer joshua khan   fl.pdf","fileSize":5003552},{"fileName":"85317-nsarequestattachment-information on the parties and items.pdf","fileSize":63072},{"fileName":"85317-nsarequestattachment-open negotiation notice.pdf","fileSize":59680},{"fileName":"85317-eob-08152023 siemer joshua khan   fl.pdf","fileSize":5003552}],"recipients":[{"to_email":"ashonta.whitehead@halomd.com","msg_id":"RZ0YHTFnT2KVJ3ynLZ2QWQ.filterdrecv-55f55bfc97-9m9rj-1-652C922D-2D.2","clicks_count":0,"last_event_time":"2023-10-16T01:30:57Z","status":"delivered","opens_count":1},{"to_email":"federalnsa@aetna.com","msg_id":"RZ0YHTFnT2KVJ3ynLZ2QWQ.filterdrecv-55f55bfc97-9m9rj-1-652C922D-2D.0","clicks_count":0,"last_event_time":"2023-10-16T01:30:47Z","status":"delivered","opens_count":0},{"to_email":"medsurantarbitrationnsa@halomd.com","msg_id":"RZ0YHTFnT2KVJ3ynLZ2QWQ.filterdrecv-55f55bfc97-9m9rj-1-652C922D-2D.1","clicks_count":0,"last_event_time":"2023-10-16T01:30:40Z","status":"delivered","opens_count":1}]}}';
    */
  }

  refreshLog() {
    this.allLogs.length = 0;
    this.isLogLoading = true;
    this.svcData.loadCaseLog(this.arbCase.id).subscribe(
      data => {
        this.isLogLoading = false;
        this.allLogs = data;
        this.allLogs.sort(UtilService.SortByCreatedOnDesc);
        this.allLogs.forEach(g => g.details = g.details.replaceAll("\\u0022", '"'));
      },
      err => {
        this.isLogLoading = false;
        this.svcToast.show(ToastEnum.danger, err.message ?? err.toString());
      });
  }

  ngOnInit(): void {
    /*
    this.dtOptions = {
      order: [
        [0, 'asc']
      ],
      dom: 'rt',
      paging: false
    };
    */
    //this.statusKeys = Object.keys(this.CMSCaseStatus).filter(f => !isNaN(Number(f)));
    this.svcUtil.showLoading = true;

    this.route.params.pipe(takeUntil(this.destroyed$)).subscribe(data => {
      this.svcToast.resetAlerts();

      this.origCase = new ArbitrationCase();
      // transform the new cases into table data
      if (data.id) {
        const id = parseInt(data.id);
        if (id > 0) {
          this.isNew = false;
          this.id = id;
          this.svcUtil.showLoading = true;
          this.loadSettings();
        } else {
          UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: 'Invalid Id' });
          this.router.navigate(['/', 'search']);
        }
      } else {
        const p = this.route.snapshot.queryParams;
        if (p.auth && p.aid) {
          this.svcData.getCaseIdByAuthority(p.auth, p.aid).subscribe(id => {
            if (!id) {
              UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: 'Invalid Authority CaseId' });
              this.router.navigate(['/', 'search']);
            } else {
              this.router.navigate(['/', 'calculator', id]);
            }
          },
            err => {
              const msg = 'Error loading Case: ' + err?.statusText || err?.message || err;
              UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: UtilService.ExtractMessageFromErr(err) });
              this.router.navigate(['/', 'search']);
            });
        } else {
          this.isNew = true;
          this.hideCaseDetail = false;
          this.hideMoreInfo = false;
          this.id = 0;
          this.arbCase.authorityStatus = '';
          this.arbCase.NSAStatus = 'Pending NSA Negotiation Request';
          // TODO: Replace with user's personal preferences (cookie)
          //this.arbCase.customer = 'Phoenix';
          this.svcUtil.showLoading = true;
          this.loadSettings();
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
    this.dtTrigger.unsubscribe();
  }

  acceptOffer() {
    let py = this.getLastOffer('payor');
    let pr = this.getLastOffer('provider');

    if (!py && this.arbCase.payorFinalOfferAmount > 0) {
      this.calcForm.form.markAsDirty();
      py = this.createOffersUsingClaimData('Payor', this.arbCase.payorFinalOfferAmount);
      this.svcToast.showAlert(ToastEnum.info, 'Payor Offer automatically created. Save changes to keep or Cancel to undo.');
    }
    if (!pr && this.arbCase.providerFinalOfferAmount > 0) {
      this.calcForm.form.markAsDirty();
      pr = this.createOffersUsingClaimData('Provider', this.arbCase.providerFinalOfferAmount);
      this.svcToast.showAlert(ToastEnum.info, 'Provider Offer automatically created. Save changes to keep or Cancel to undo.');
    }
    if (!pr && !py) {
      this.svcToast.show(ToastEnum.danger, 'No Offers found to accept and no offer amounts on this Claim. Create a Payor or Provider offer and try again.', 'Missing Offer', 5000);
      return;
    }

    const modalRef = this.modalService.open(AcceptOfferComponent);
    modalRef.componentInstance.name = 'acceptOffer';
    modalRef.componentInstance.notes = '';
    modalRef.componentInstance.payorOffer = !!py ? py.offerAmount : 0;
    modalRef.componentInstance.providerOffer = !!pr ? pr.offerAmount : 0;
    modalRef.componentInstance.offerType = ''; // user chooses

    modalRef.closed.subscribe(data => {
      let who = modalRef.componentInstance.offerType;
      let amt = 0;
      if (who === 'Payor') {
        if (!py)
          return;
        amt = modalRef.componentInstance.payorOffer;
        py.wasOfferAccepted = true;
        py.notes = modalRef.componentInstance.notes;
      } else if (who === 'Provider') {
        if (!pr)
          return;
        amt = modalRef.componentInstance.providerOffer;
        pr.wasOfferAccepted = true;
        pr.notes = modalRef.componentInstance.notes;
      } else {
        this.svcToast.showAlert(ToastEnum.danger, 'Unable to detect the Offer selection! Reload the page and try again or contact technical support.');
        return;
      }

      this.hasPendingAcceptance = true;
      this.calcForm.form.markAsDirty();
      this.svcToast.showAlert(ToastEnum.success, `Accepted the ${who}'s offer of ${amt}`);
      this.svcToast.showAlert(ToastEnum.warning, 'Offer Acceptance is queued for saving. You MUST click Save Changes to finalize!');
      this.scrollToTop();
    });
  }

  addCpt() {
    const cpt = new ClaimCPT();
    this.arbCase.cptCodes.push(cpt);
    setTimeout(() => {
      const a = document.getElementsByClassName('cpt-code');
      for (let i = 0; i < a.length; i++) {
        let n = (a[i] as HTMLInputElement);
        if (!n.value) {
          n.focus();
          return;
        }
      }
    }, 0)
  }

  addFile() {
    if (!this.caseFile || !this.caseFile.nativeElement || this.documentType === null) {
      this.svcToast.show(ToastEnum.warning, 'Unable to read caseFile element contents or docunentType is NULL');
      return;
    }
    const DocTypeLowerCase = CaseDocumentType[this.documentType];
    const ne = this.caseFile.nativeElement as HTMLInputElement
    const files = ne.files; // e.target?.files;
    const fileToUpload: File | undefined = files && files.length ? files[0] : undefined;
    if (!fileToUpload)
      return;
    const fileNameLowerCase = fileToUpload.name.toLowerCase();
    if (!fileNameLowerCase.endsWith('.pdf') && !fileNameLowerCase.endsWith('.tif') && !fileNameLowerCase.endsWith('.tiff')) {
      this.svcToast.show(ToastEnum.danger, 'Only PDF, TIF and TIFF files are allowed', 'Unsupported File Type', 4000);
      return;
    }

    this.isSaving = true;
    this.svcUtil.showLoading = true;

    this.svcData.uploadCaseDocument(fileToUpload, this.arbCase.id, DocTypeLowerCase).subscribe(
      {
        complete: () => {

        },

        next: (data: any) => {
          ne.value = '';
          this.documentType = null;
          this.isSaving = false;
          this.svcUtil.showLoading = false;
          this.svcToast.show(ToastEnum.success, 'File uploaded successfully!');
          const n = new CaseFileVM({
            blobName: `${this.arbCase.id}-${DocTypeLowerCase}-${fileNameLowerCase}`,
            createdOn: new Date(),
            AuthorityCaseId: this.arbCase.authorityCaseId,
            DocumentType: DocTypeLowerCase,
            EHRNumber: this.arbCase.EHRNumber,
            UpdatedBy: this.svcAuth.getActiveAccount()?.name || 'system'
          });
          this.allCaseFileVMs.push(n);
        },
        error: (err) => {
          this.isSaving = false;
          this.svcUtil.showLoading = false;
          this.svcToast.show(ToastEnum.danger, err.error, 'Upload Failed');
        }
      }
    );
  }

  addNegotiator() {
    let contact = this.allNegotiators.find(d => d.id === 0) ?? new Negotiator();
    contact.id = 0;
    const payor = this.allPayors.find(d => d.id === this.arbCase.payorId);
    if (!payor)
      return;
    contact.organization = payor.name;
    contact.payorId = payor.id;
    const modalRef = this.modalService.open(NegotiatorComponent);
    modalRef.componentInstance.name = 'addNegotiator';
    modalRef.componentInstance.payorName = payor.name;
    modalRef.componentInstance.contact = contact;
    modalRef.closed.subscribe(data => {
      this.svcUtil.showLoading = true;
      this.svcData.createNegotiator(contact).subscribe(rec => {
        this.allNegotiators.push(rec);
        this.calcForm.form.markAsDirty();
        this.arbCase.payorNegotiatorId = rec.id;
        this.arbCase.payorNegotiator = rec;
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.success, 'New Negotiator added to ' + payor.name);
      },
        err => {
          this.svcUtil.showLoading = false;
          this.svcToast.show(ToastEnum.danger, 'Error creating Negotiator" ' + err.message ?? err);
        });
    });
  }

  addOffer(t: string) {
    const po = this.arbCase.offerHistory.find(d => d.id === 0 && d.offerType.toLowerCase() === t.toLowerCase());
    this.openOfferDialog(t, po);
  }

  addSettlementFromOffer(po: OfferHistory) {
    if (!this.arbCase.id || !this.arbCase.payorId)
      return; // cannot add settlements until the claim is saved
    const d = new CaseSettlement();
    d.arbitrationCaseId = this.arbCase.id;
    d.payorId = this.arbCase.payorId;
    d.prevailingParty = 'Informal';
    d.offer = po;
    this.openSettlementDialog(d);
  }

  addFormalSettlement() {
    if (!this.arbCase.id || !this.arbCase.payorId)
      return; // cannot add settlements until the claim is saved
    const d = new CaseSettlement();
    d.arbitrationCaseId = this.arbCase.id;
    d.payorId = this.arbCase.payorId;

    this.openSettlementDialog(d);
  }

  applyGranularSecurity(data: ArbitrationCase) {
    if (!this.currentUser?.appRoles)
      return;

    if (this.isNew && !this.isManager && !this.isNegotiator && !this.arbCase.customer) {
      const r = this.currentUser.appRoles.find(d => d.roleType === UserRoleType.Customer && (d.accessLevel === UserAccessType.manager || d.accessLevel === UserAccessType.negotiator));
      if (!r || !this.allCustomers.length) {
        UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: 'Your account profile cannot create new cases. Contact your manager to request Manager permission.' });
        this.router.navigate(['/']);
        return;
      }
      this.canEdit = true;
      return;
    }

    const customer = this.allCustomers.find(d => d.name.toLowerCase() === data.customer.toLowerCase());
    if (!this.canEdit && this.currentUser && this.currentUser.appRoles && customer) {

      const r = this.currentUser.appRoles.find(d => d.roleType === UserRoleType.Customer && d.entityId === customer.id);
      if (r) {
        this.isManager = this.isManager || r.accessLevel === UserAccessType.manager;
        this.isNegotiator = this.isNegotiator || r.accessLevel === UserAccessType.negotiator;
        this.isReporter = this.isReporter || r.accessLevel === UserAccessType.reporter;
        this.canEdit = this.isManager || this.isNegotiator;
      } else {
        this.svcToast.showAlert(ToastEnum.warning, 'Unexpected! User security is inconsistent with permissions assigned to this case. Please notify IT Support.');
      }
    }
  }

  authorityChange() {
    if (!this.isNew && this.arbCase.authorityCaseId) {
      this.archiveReason = '';
      const mref = this.modalService.open(this.archiveModal);
      mref.closed.subscribe(data => {
        this.arbCase.keepAuthorityInfo = true;
        this.authorityChanged();
      },
        err => console.error(err)
      );

      mref.dismissed.subscribe(data => {
        this.arbCase.keepAuthorityInfo = false;
        const n = new Note();
        n.details = this.archiveReason;
        n.updatedBy = this.currentUser?.email ?? '';
        n.updatedOn = new Date();
        this.arbCase.notes.push(n);
        this.caseNotes = UtilService.StringifyCaseNotes(this.arbCase.notes);
        this.authorityChanged();
      },
        err => console.error(err)
      )
    } else {
      this.authorityChanged();
    }
  }

  authorityChanged() {
    this.arbCase.authorityCaseId = '';
    this.arbCase.authorityStatus = 'Not Submitted';
    this.arbCase.ineligibilityAction = '';
    this.arbCase.ineligibilityReasons = '';
    this.arbCase.status = CMSCaseStatus.Open;
    this.setActiveAuthority();
    this.calcForm.form.markAsDirty();
    this.calcForm.form.markAsTouched();
    if (!this.isNew)
      this.svcToast.showAlert(ToastEnum.info, 'Authority Case Info was reset. Click the "Cancel Changes" button at the top of the page to Undo.');
  }

  authorityStatusChange() {
    this.svcChangeDetection.detectChanges();
  }

  canAddCpt(): boolean {
    if (!this.arbCase)
      return false;
    else {
      const c = this.arbCase.cptCodes.find(d => !d.cptCode);
      return !c;
    }
  }

  canAddOffers() {
    if (!this.isNegotiator && !this.isManager)
      return false;
    if (!this.isState && !this.isNSA)
      return false;
    if (!this.arbCase.cptCodes.find(v => v.id > 0 && v.isIncluded))
      return false;
    const h = this.arbCase.offerHistory.find(d => d.wasOfferAccepted);
    if (!!h)
      return false;
    const IsTotallyClosed = this.calcForm?.form.dirty ||
      this.hasPendingAcceptance ||
      this.arbCase.status === CMSCaseStatus.ClosedPaymentReceived ||
      this.arbCase.status === CMSCaseStatus.ClosedPaymentWithdrawn ||
      this.arbCase.status === CMSCaseStatus.SettledArbitrationPendingPayment ||
      this.arbCase.status === CMSCaseStatus.SettledInformalPendingPayment ||
      this.arbCase.status === CMSCaseStatus.SettledOutsidePendingPayment ||
      this.arbCase.NSAWorkflowStatus === CMSCaseStatus.ClosedPaymentReceived ||
      this.arbCase.NSAWorkflowStatus === CMSCaseStatus.ClosedPaymentWithdrawn ||
      this.arbCase.NSAWorkflowStatus === CMSCaseStatus.SettledArbitrationPendingPayment ||
      this.arbCase.NSAWorkflowStatus === CMSCaseStatus.SettledInformalPendingPayment ||
      this.arbCase.NSAWorkflowStatus === CMSCaseStatus.SettledOutsidePendingPayment;
    return !IsTotallyClosed;
  }

  canAddSettlement(isFormal: boolean = true) {
    if (!this.calcForm || this.calcForm.form.dirty)
      return false;
    if (!this.arbCase.payorId)
      return false;
    if (!this.arbCase.cptCodes.find(v => v.id > 0 && v.isIncluded))
      return false;
    if (isFormal && (!!this.arbCase.authorityCaseId || !!this.arbCase.NSACaseId))
      return true;
    return !isFormal;
  }

  cancelChanges() {
    this.resetAlerts = true;
    if (this.isNew) {
      this.router.navigate(['/search']);
    } else {
      this.canDeactivate().subscribe(d => {
        if (d) {
          this.resetFormStatus();
          this.svcToast.resetAlerts();
          this.svcData.loadCaseById(this.id).subscribe(data => this.caseLoaded(data), err => { err.message = `Unable to reload Case Id ${this.id}.`; this.caseLoadError(err); });
        }
      });
    }
  }

  canDeactivate(): Observable<boolean> {
    return this.isNavigationAllowed();
  }

  caseFileChanged(e: any) {
    e.target.blur();
  }

  caseIsActive(): boolean {
    if (!this.arbCase.authorityCaseId)
      return false;
    const a = this.arbCase.authorityStatus.toLowerCase();
    const r = /.*(assigned|submitted|not settled).*/;
    //const m = a.match(r);
    if (r.test(a))
      return true;
    const b = this.arbCase.status;
    return b == CMSCaseStatus.New ||
      b == CMSCaseStatus.Open ||
      b == CMSCaseStatus.DetermineAuthority ||
      b == CMSCaseStatus.MissingInformation ||
      b == CMSCaseStatus.ActiveArbitrationBriefCreated ||
      b == CMSCaseStatus.ActiveArbitrationBriefNeeded ||
      b == CMSCaseStatus.ActiveArbitrationBriefSubmitted ||
      b == CMSCaseStatus.InformalInProgress ||
      b == CMSCaseStatus.PendingArbitration;
  }

  caseLoaded(data: ArbitrationCase) {
    if (this.isNew) {
      if (data.id > 0) {
        this.router.navigate(['/', 'calculator', data.id.toString()]);  // update the browser url with a loop back call
        return;
      }
      return; // the ArbitrationCase will have id=0 if it is new
    }

    if (!this.isNew && data.id !== this.id) {
      UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: 'Unexpected Calculator component condition. Redirecting to the Search page.' });
      this.router.navigate(['/search']);
      return;
    }

    this.caseNotes = '';
    this.newNote = '';
    const isSaving = this.isSaving;

    if (this.isSaving) {
      this.isSaving = false;
      this.resetFormStatus(); // resets form flags such as dirty that may be needed by some of the following fn calls
      this.svcToast.resetAlerts();
    }

    if (!this.isNew && data.id === this.id) {
      this.hideDates = !!data.authority && !isSaving;

      // apply bad data fixes - this may need to expand to its own function
      if (data.service === 'IOM PRO')
        data.service = 'IOM Pro';
      if (data.providerType === 'Neruologist')
        data.providerType = 'Neurologist';
      else if (data.providerType === 'Neruomonitor')
        data.providerType = 'Neuromonitor';

      if (!!data.ineligibilityAction && this.allStateIneligibilityActions.indexOf(data.ineligibilityAction) == -1) {
        this.allStateIneligibilityActions.push(data.ineligibilityAction);
        this.allStateIneligibilityActions.sort(UtilService.SortSimple);
      }

      // unknown Provider Type?
      if (this.arbCase.providerType && this.allProviderTypes.indexOf(this.arbCase.providerType) === -1)
        this.allProviderTypes.splice(0, 0, this.arbCase.providerType);

      // apply granular security
      this.applyGranularSecurity(data);
      this.hasPendingAcceptance = false;
      this.isZiplocked = data.cptCodes.length > 0 && (!!data.locationGeoZip || !!data.benchmarkGeoZip) ? true : false;
      this.arbCase = data;
      this.canDelete = this.isManager && !this.arbCase.notifications.length;
      this.origCase = new ArbitrationCase(data);

      if (this.arbCase.cptCodes.length === 0) {
        const cpt = new ClaimCPT();
        this.arbCase.cptCodes.push(cpt);
      } else {
        this.loadRelatedDisputes();
      }

      this.caseNotes = UtilService.StringifyCaseNotes(this.arbCase.notes);

      // has log? sort it descending
      if (this.arbCase.log.length) {
        this.arbCase.log.sort(UtilService.SortByCreatedOnDesc);
      }
      // has offer history?
      if (this.arbCase.offerHistory.length) {
        this.arbCase.offerHistory.sort(UtilService.SortByUpdatedOn);
      }

      // make sure drop downs have something to connect to
      this.setActiveAuthority();

      this.setActivePayor();
      let pid = this.arbCase.payorId ?? 0;
      if (pid > 1) {
        this.loadNegotiators();
      }

      this.setActiveCustomer();

      this.setNSATrackingObject();

      this.setIneligibilityAction();

      // explicitly unlock any incomplete fields due to whatever reason
      if (!!this.arbCase.service && !this.arbCase.service.startsWith(this.arbCase.serviceLine)) {
        this.serviceChange();
        this.resetFormStatus();
      }

      // fix old NSA discount overrides that were saved as zero - zero is never used as of 6-1-2023
      this.calculatorVars = this.allCalcVariables.find(c => c.serviceLine.toLowerCase() === data.serviceLine.toLowerCase());
      if (this.isNSA && data.NSAStatus === 'Pending NSA Negotiation Request' && !data.NSARequestDiscount && this.arbCase.NSARequestDiscount !== data.NSARequestDiscount) {
        this.clearNSARequestDicsount();
        this.svcToast.showAlert(ToastEnum.warning, 'NSA Request Discount reset to the default. Verify this is correct before submitting the NSA Open Negotiation Request.')
        this.calcForm.form.markAsDirty();
        this.calcForm.form.markAsTouched();
      }

      this.isServiceLocked = false; // !!this.arbCase.service && !!this.arbCase.serviceLine; removed per DevOps task 1374.

      // filter out inactive customers as a choice
      this.allCustomers = this.allCustomers.filter(d => d.isActive || d.name == this.arbCase.customer);

      // has authority and case number?
      if (this.arbCase.authority && this.arbCase.authorityCaseId && !!this.activeAuthority?.website)
        this.loadLatestAuthorityData();  // gets the latest import/upload record from the external system

      this.showWarnings();

      // enhance the CaseSettlementDetails "viewmodel"
      this.arbCase.caseSettlements.forEach(v => {
        const a = this.allAuthorities.find(b => b.id == v.authorityId);
        v.authorityKey = a?.key.toUpperCase() ?? '';
      });
      this.svcUtil.showLoading = false;

      // fetch case files since this is not a new record
      //this.isLoadingFiles = true;
      this.reloadBenchmarks();
      //this.svcData.loadCaseFiles(this.id, this.allCaseFiles$, this.svcToast);
      this.loadCaseFiles();
      this.loadSettlements();

      if (this.arbCase.isUnread) {
        const wf = new CaseWorkflowParams();
        wf.action = CaseWorkflowAction.MarkRead;
        wf.caseId = this.arbCase.id;
        this.svcData.doWorkflowAction(wf).pipe(take(1)).subscribe(data => {
          loggerCallback(LogLevel.Info, `Marked ${this.arbCase.id} read`);
        },
          err => loggerCallback(LogLevel.Error, err)
        );
      }
      // load the list of archive records associated with this claim
      this.svcData.loadCaseArchives(this.id).subscribe(data => {
        if (data.length) {
          this.allArchivedCases = data;
          this.allArchivedCases.sort(UtilService.SortByArbitrationCaseId);
          this.previousCaseNumbers = this.allArchivedCases.map(d => d.authorityCaseId).join(', ');
        } else {
          this.previousCaseNumbers = 'none';
        }
      },
        err => console.error(err)
      );
      // do a delayed check for form validity and expand all sections if 
      setTimeout(() => {
        this.hideCaseDetail = this.calcForm.valid ?? true;
        this.hideMoreInfo = this.hideCaseDetail;
      }, 100);

    } else {
      UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: 'Unexpected Calculator component condition. Redirecting to the Search page.' });
      this.router.navigate(['/search']);
    }
  }

  caseLoadError(err: any) {
    let msg = err instanceof HttpErrorResponse ? err.error.title : null;
    msg = msg ?? err.error ?? err.message ?? err.statusText ?? err;
    this.isSaving = false;
    this.svcUtil.showLoading = false;
    if (err.status === 404) {
      UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: UtilService.ExtractMessageFromErr(err) });
      this.router.navigateByUrl('/');
    } else {
      this.svcToast.showAlert(ToastEnum.danger, msg);
    }
  }

  checkForExistingClaim() {
    if (!this.arbCase.payorClaimNumber)
      return;
    const exclude = this.isNew ? 0 : this.arbCase.id;
    return;
    this.svcData.checkForActivePayorClaimNumber(this.arbCase.payorClaimNumber, exclude).pipe(take(1))
      .subscribe(data => {
        if (data && data > 0) {
          this.svcToast.showAlert(ToastEnum.danger, `An Active Case already exists with Claim Number ${this.arbCase.payorClaimNumber}. Click <a href="/calculator/${data}" title="Open Case ${data}">here</a> to open it.`);
          this.arbCase.payorClaimNumber = '';
        }
      }, err => {
        if (err.status === 404)
          return;
        console.error(err);
        this.svcToast.show(ToastEnum.danger, err.statusText ?? err);
      });
  }

  clearCPT(cpt: ClaimCPT) {
    cpt.fh50thPercentileCharges = 0;
    cpt.fh50thPercentileExtendedCharges = 0;
    cpt.fh80thPercentileCharges = 0;
    cpt.fh80thPercentileExtendedCharges = 0;
  }

  clearNSARequestDicsount() {
    this.arbCase.NSARequestDiscount = !!this.calculatorVars ? this.calculatorVars.nsaOfferDiscount : 0;
  }

  confirmUnlockZip() {
    if (confirm('WARNING: Changing the Location Geo-Zip will replace all Fair Health charges with charges for the new location. Continue?')) {
      this.isZiplocked = false;
    }
  }

  cptChanged(cpt: ClaimCPT) {
    this.calcForm.form.markAsDirty();

    let geo = !!this.arbCase.benchmarkGeoZip ? this.arbCase.benchmarkGeoZip : this.arbCase.locationGeoZip;
    if (!this.defaultBenchmark || geo.length < 3 || !cpt.cptCode || !this.activeAuthority || !this.activeAuthority.benchmarks?.length) {
      this.clearCPT(cpt);
      this.recalc();
      this.updateGraph(); // TODO: not really sure if we'll ever get here
      return;
    }

    geo = geo.substring(0, 3);
    const defId = this.defaultBenchmark.benchmarkDatasetId;
    let callCount = 0;

    for (const abm of this.activeAuthority.benchmarks.filter(d => d.service === this.arbCase.service)) {
      callCount++;
      this.svcData.loadBenchmarks(abm.benchmarkDatasetId, geo, cpt.cptCode, cpt.modifier26_YN).subscribe(data => {
        callCount--;
        if (!data) {
          this.svcToast.show(ToastEnum.warning, `No Benchmark data for CPT ${cpt.cptCode} in dataset ${abm.benchmarkDatasetId}`);
          cpt.fh50thPercentileCharges = 0;
          cpt.fh50thPercentileExtendedCharges = 0;
          cpt.fh80thPercentileCharges = 0;
          cpt.fh80thPercentileExtendedCharges = 0;
        } else {

          if (abm.benchmarkDatasetId === defId) {
            this.updateCptCalculations(this.defaultBenchmark!, data, cpt);  // this updates the one CPT record that's saved for this case - the "default"
            this.recalc();
          }

          // update the graph data with this value
          console.log(`Updating CPT ${cpt.cptCode} with data from benchmark ${abm.benchmarkDatasetId} : id ${data.id} benchmarks ${data.benchmarks}`);
          this.allDSToggle.find(t => t.datasetId == abm.benchmarkDatasetId)?.addInsertItem(abm, data, cpt);
        }
        if (!callCount) {
          this.updateGraph();
        }
      });
    }
  }

  createNote() {
    if (!this.newNote)
      return;
    if (this.isNew || this.arbCase.id < 1) {
      this.svcToast.show(ToastEnum.warning, 'Save the current Case before adding Notes.')
      return;
    }
    this.svcUtil.showLoading = true;
    const n = new Note();
    n.details = this.newNote;

    this.svcData.createNote(this.arbCase.id, n).subscribe(
      data => {
        this.arbCase.notes.push(data);
        this.caseNotes = UtilService.StringifyCaseNotes(this.arbCase.notes);
        this.svcToast.show(ToastEnum.success, 'Note added successfully!');
        //this.caseNotes += this.caseNotes ? '\n' : '';
        //this.caseNotes += `* (${data.updatedOn?.toLocaleString()} by ${this.currentUser?.email}) - ${data.details}`;
        this.newNote = '';
      },
      err => this.caseLoadError(err),
      () => this.svcUtil.showLoading = false
    );
  }

  createOffersUsingClaimData(offerType: string, offerAmount: number) {
    const offer = new OfferHistory();
    offer.arbitrationCaseId = this.arbCase.id;
    offer.notes = 'Created automatically';
    offer.offerSource = 'EHR';
    offer.updatedOn = new Date();

    offer.offerAmount = offerAmount;
    offer.offerType = offerType;
    this.arbCase.offerHistory.unshift(offer);
    return offer;
  }

  customerChange() {
    /*
    if (this.arbCase.customer === 'MPOWERHealth')
      this.arbCase.EHRSource = 'USMON';
    else
      this.arbCase.EHRSource = 'external';

    this.canEdit = !!this.currentUser?.isManager || !!this.currentUser?.isNegotiator;
    */

    this.setActiveCustomer();
    this.arbCase.entity = '';
    this.arbCase.entityNPI = '';

    this.arbCase.EHRSource = !!this.currentCustomer ? this.currentCustomer!.EHRSystem : 'unknown';
    this.applyGranularSecurity(this.arbCase);
  }

  dayDiff(d1: Date | undefined): number {
    if (!d1)
      return 0; // trigger red background for missing dates
    const d2 = new Date(); // today
    return UtilService.DayDiff(d2, d1);
  }

  deleteClaim() {
    if (!confirm('ARE YOU SURE?'))
      return;
    this.svcUtil.showLoading = true;
    this.svcData.deleteClaim(this.id, this.arbCase).subscribe(
      data => this.router.navigateByUrl('/'),
      err => this.caseLoadError(err),
      () => this.svcUtil.showLoading = false);
  }

  deleteFile(f: CaseFileVM) {
    if (!confirm('Are you sure you want to permanently delete this file from case ' + this.arbCase.id))
      return;
    this.svcData.deleteBlob(this.arbCase.id, f.blobName).subscribe(data => {
      this.svcToast.show(ToastEnum.success, 'File deleted');
      const ndx = this.allCaseFileVMs.findIndex(d => d.blobName.toLowerCase() === f.blobName.toLowerCase());
      if (ndx > -1) {
        this.allCaseFileVMs.splice(ndx, 1);
      }
    },
      err => this.caseLoadError(err)
    );
  }

  editOffer(t: string) {
    const amt = t === 'Payor' ? this.arbCase.payorFinalOfferAmount : this.arbCase.providerFinalOfferAmount;
    const po = this.arbCase.offerHistory.find(d => d.id > 0 && d.offerType === t && d.offerAmount === amt);
    if (!po) {
      this.svcToast.show(ToastEnum.danger, `Unable to locate existing ${t} offer to edit!`);
      return;
    }
    this.openOfferDialog(t, po);
  }

  entityChange() {
    if (!!this.arbCase.entityNPI) {
      const nt = this.allEntities.find(v => v.NPINumber === this.arbCase.entityNPI);
      this.arbCase.entity = nt!.name;
      this.setIsExcludedNPI();
    }
  }

  firstAppealDateChange() {
    if (!this.arbCase.firstAppealDate)
      this.arbCase.firstResponseDate = undefined;
  }

  firstResponseDateChange() {
    this.arbCase.arbitrationDeadlineDate = this.arbCase.firstResponseDate ? UtilService.AddDays(this.arbCase.firstResponseDate, 90) : undefined;
  }

  getArbStats(a: IArbitrator): IArbStats {
    if (!a)
      return { cases: 0, won: 0, lost: 0, service: this.arbCase.serviceLine };
    const s = a.allStats.find(d => d.service.toLowerCase() === this.arbCase.serviceLine.toLowerCase());
    if (!s)
      return { cases: 0, won: 0, lost: 0, service: this.arbCase.serviceLine };
    return s;
  }

  getArbWinPct(s: IArbStats): number {
    if (!s.cases || !s.won)
      return 0;
    return (s.won / s.cases) * 100;
  }

  getDaysRemaining(s: string | undefined) {
    if (!s)
      return '';

    const d1 = new Date(s)
    if (!d1)
      return '';
    const d2 = new Date();
    if (d2 >= d1)
      return '';
    const diff = d1.getTime() - d2.getTime();
    const days = Math.ceil(diff / (1000 * 3600 * 24));
    if (days > 31)
      return '';
    return `(in ${days} days)`
  }

  getDefaultBenchmarkConfig(): AuthorityBenchmarkDetails | undefined {
    if (!this.activeAuthority || !this.activeAuthority.benchmarks?.length || !this.arbCase.service)
      return undefined;

    return this.activeAuthority.benchmarks.find(a => a.isDefault && a.service.toLowerCase() === this.arbCase.service.toLowerCase());
  }

  getFormalProfit(s: string) {
    if (s === 'provider') {
      return 1;
    } else {
      return 0;
    }
  }

  getInitialAllowedAmount(): number {
    const n: number = this.arbCase.firstResponsePayment + this.arbCase.patientShareAmount;
    return n;
  }

  getLastOffer(type: string) {
    const hist = this.arbCase.offerHistory;
    hist.sort(UtilService.SortByUpdatedOn);
    for (let d = 0; d < hist.length; d++) {
      if (hist[d].offerType.toLowerCase() === type)
        return hist[d];
    }
    return null;
  }

  getNSARequestOffer() {
    const disc = (this.arbCase.NSARequestDiscount !== null && this.arbCase.NSARequestDiscount > 0 && this.arbCase.NSARequestDiscount <= .99) ? 1 - this.arbCase.NSARequestDiscount : 1 - (this.calculatorVars?.nsaOfferDiscount ?? 0);
    const sum = this.getSum(this.calculatorVars?.nsaOfferBaseValueFieldname);
    return sum * disc;
  }

  /**
   * 
   * @param s A switch parameter for requesting different sums
   * @returns The requested sum
   */
  getSum(s: string | undefined) {
    return UtilService.GetCPTValueSum(this.arbCase.cptCodes, s);
  }

  readonly TAB = 9;
  readonly ENTER = 13;
  readonly UP = 38;
  readonly DOWN = 40;
  readonly LEFT = 37;
  readonly RIGHT = 39;
  readonly CONTROL_KEYS = [this.TAB, this.ENTER, this.UP, this.DOWN, this.LEFT, this.RIGHT];

  focusNextElement(m: HTMLElement) {
    //add all elements we want to include in our selection
    var focussableElements =
      'a:not([disabled]), button:not([disabled]), input[type=text]:not([disabled]), [tabindex]:not([disabled]):not([tabindex="-1"])';

    const focussable = Array.prototype.filter.call(
      document.querySelectorAll(focussableElements),
      function (element) {
        //check for visibility while always include the current activeElement
        return (
          element.offsetWidth > 0 ||
          element.offsetHeight > 0 ||
          element === m
        );
      }
    );
    const index = focussable.indexOf(m);
    if (index > -1) {
      const nextElement = focussable[index + 1] || focussable[0];
      nextElement.focus();
    }

  }

  getDisputeLinks(cpt: ClaimCPT) {
    if (!cpt.disputes.length)
      return '';
    let h = '';
    for (let d of cpt.disputes) {
      h += `<a href="/batch/${d.authorityDisputeId}" target="_blank" title="Open Dispute in a new tab">${d.authorityCaseId}</a><br />`
    }
    return h.substring(0, h.length - 6);
  }

  handleGridNav(event: any) {
    const charCode = (event.which) ? event.which : event.keyCode;

    if (this.CONTROL_KEYS.indexOf(charCode) === -1)
      return true;

    const m = event.target; // as HTMLElement;
    const t = m.id.split('_');
    if (t.length < 2)
      return true;

    let value = t[t.length - 1];
    if (isNaN(value))
      return true;

    value = parseInt(value);

    if (charCode === this.ENTER) {
      this.focusNextElement(m as HTMLElement);
    } else if (charCode === this.UP) {
      if (value) {
        t[t.length - 1] = value - 1;
        const z = document.getElementById(t.join('_'));
        if (!!z)
          z.focus();
      }
      return false;
    } else if (charCode === this.DOWN) {
      t[t.length - 1] = value + 1;
      const z = document.getElementById(t.join('_'));
      if (!!z)
        z.focus();
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

  readonly numericKeys = [8, 46, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 110, 190];

  handleGridNavNumeric(event: any) {
    const charCode = (event.which) ? event.which : event.keyCode;
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

  hasOffer(t: string) {
    if (!this.arbCase.offerHistory.length)
      return false;
    return !!this.arbCase.offerHistory.find(v => v.offerType.toLowerCase() === t.toLowerCase());
  }

  isCaseLocked() {
    return !this.calcForm?.form.dirty && (this.hasPendingAcceptance ||
      this.arbCase.status === CMSCaseStatus.ClosedPaymentReceived ||
      this.arbCase.status === CMSCaseStatus.ClosedPaymentWithdrawn //||
      //this.arbCase.status === CMSCaseStatus.Ineligible || // removed per User Story 1074
      //this.arbCase.status === CMSCaseStatus.SettledArbitrationPendingPayment ||
      //this.arbCase.status === CMSCaseStatus.SettledInformalPendingPayment ||
      //this.arbCase.status === CMSCaseStatus.SettledOutsidePendingPayment
    );
  }

  isNSACaseLocked() {
    return !this.calcForm?.form.dirty &&
      (this.arbCase.NSAWorkflowStatus === CMSCaseStatus.ClosedPaymentReceived ||
        this.arbCase.NSAWorkflowStatus === CMSCaseStatus.ClosedPaymentWithdrawn ||
        this.arbCase.NSAWorkflowStatus === CMSCaseStatus.SettledArbitrationPendingPayment ||
        this.arbCase.NSAWorkflowStatus === CMSCaseStatus.SettledInformalPendingPayment ||
        this.arbCase.NSAWorkflowStatus === CMSCaseStatus.SettledOutsidePendingPayment);
  }

  isNavigationAllowed(beforeunloadEvent = false): Observable<boolean> {
    return from(new Promise<boolean>((resolve) => {
      if (this.calcForm && this.calcForm.dirty && !this.isSaving) {
        if (beforeunloadEvent) {
          resolve(false);
        } else {
          this.modalService.open(this.confirmationModal, this.modalOptions).result.then(data => {
            if (this.resetAlerts) {
              this.svcToast.resetAlerts();
            }
            resolve(true);
          }, () => resolve(false));
        }
      } else {
        resolve(true);
      }
    }));
  }

  lastEOBChange() {
    UtilService.UpdateTrackingCalculations(this.NSAAuthority?.trackingDetails, this.NSATrackingObject, false, this.arbCase);
  }

  loadCaseFiles() {
    if (!this.id)
      return;

    this.allCaseFileVMs.length = 0;

    this.svcData.getCaseFiles(this.id).subscribe(data => {
      data.forEach(cf => {
        const vm = new CaseFileVM(cf.tags);
        vm.blobName = cf.blobName;
        vm.createdOn = cf.createdOn;
        this.allCaseFileVMs.push(vm);
      });
    },
      err => {
        console.error(err);
        this.svcToast.show(ToastEnum.danger, `Unable to load the list of file attachments`);
      });
  }

  /** Loads the last import record, e.g. TDI Import, used to update this case. 
   * If the case has never been updated with an import file/record, this will return nothing. */
  loadLatestAuthorityData() {
    this.latestAuthorityData = undefined;
    this.svcData.loadAuthorityCase(this.arbCase.authority, this.arbCase.authorityCaseId).subscribe(
      data => {
        if (!!data)
          this.latestAuthorityData = new TDIRequestDetails(data)
      },
      err => console.log(err)
    )
  }

  loadNegotiators() {
    this.allNegotiators.length = 0;
    if (!this.arbCase?.payorId)
      return;
    this.svcData.loadPayorById(this.arbCase.payorId, true)
      .subscribe(data => {
        this.allNegotiators = !!data ? data.negotiators : [];
        if (this.arbCase.payorNegotiatorId)
          this.arbCase.payorNegotiator = this.allNegotiators.find(d => d.id === this.arbCase.payorNegotiatorId) ?? null;
      });
  }

  loadPrerequisites() {
    const payors$ = this.svcData.loadPayors(true, false, false);
    const authorities$ = this.svcData.loadAuthorities();
    const calcvars$ = this.svcData.loadCalculatorVariables();
    const customers$ = this.svcData.loadCustomers();
    const benchmarks$ = this.svcData.loadBenchmarkDatasets();
    const services$ = this.svcData.loadServices();

    combineLatest([payors$, authorities$, calcvars$, customers$, benchmarks$, services$]).subscribe(
      ([payors, authorities, calcvars, customers, benchmarks, services]) => {

        // payors
        this.allPayors = payors;
        this.allPayors.sort(UtilService.SortByName);
        // authorities
        this.allAuthorities = authorities.filter(v => v.key.toLowerCase() !== 'nsa');
        this.allAuthorities.sort(UtilService.SortByName);
        const nsa = authorities.find(v => v.key.toLowerCase() === 'nsa');
        this.NSAAuthority = nsa ? new Authority(nsa) : undefined;
        if (!this.NSAAuthority) {
          UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: 'Critical Error: NSA authority configuration not found. Contact IT Support immediately!' });
          this.router.navigateByUrl('/');
          return;
        } else {
          this.allNSAStatuses = this.NSAAuthority.statusList;
          this.NSATrackingFieldsForUI = this.NSAAuthority.trackingDetails.filter(d => !d.isHidden);
          this.setNSATrackingObject();
        }
        // calculator variables
        this.allCalcVariables = calcvars;
        // list of active customers or customer that matches the one on the case
        this.allCustomers = this.isNew ? customers.filter(d => d.isActive) : customers;
        this.allCustomers.sort(UtilService.SortByName);

        // list of all benchmarks
        this.allBenchmarkDatasets = benchmarks;
        // list of all service lines
        this.services = services;
      },
      err => console.error('loadPrerequisites combineLatest failed', err),
      () => {
        this.subscribeToData();
        if (!this.isNew) {
          this.svcData.loadCaseById(this.id).subscribe(data => this.caseLoaded(data), err => {
            UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: `Unable to locate a case for Id ${this.id}.` });
            this.router.navigate(['/', 'search']);
          });
        } else {
          this.arbCase.authorityStatus = 'Not Submitted';
          this.applyGranularSecurity(this.arbCase);
          if (!!this.testNotification)
            this.arbCase.notifications = [this.testNotification];
          this.svcUtil.showLoading = false;
        }
        this.svcChangeDetection.detectChanges();
      });
  }

  loadRelatedDisputes() {
    const ids = this.arbCase.cptCodes.map(v => v.id);
    this.svcData.findAuthorityDisputesByCPTs(ids).subscribe(
      data => {
        this.arbCase.cptCodes.forEach(a => {
          const disputes = data.filter(b => !!b.disputeCPTs.find(c => c.claimCPTId === a.id));
          if (!!disputes.length) {
            a.disputes = disputes.map(v => new DisputeLinkVM({
              authorityDisputeId: v.id,
              authorityCaseId: v.authorityCaseId,
              authorityId: v.authorityId,
              authorityKey: v.authority?.key,
              authorityName: v.authority?.name,
              claimCPTId: a.id,
              cptCode: a.cptCode,
              disputeStatus: v.workflowStatus
            }));
          }
        })
      },
      err => console.error(err)
    )
  }

  loadSettings() {
    const placeCodes$ = this.svcData.loadPlaceOfServiceCodes();
    const settings$ = this.svcData.getAppSettings();
    combineLatest([placeCodes$, settings$]).subscribe(([placeCodes, settings]) => {
      this.allPlaceCodes = placeCodes; // presorted
      this.allStateIneligibilityActions = settings.stateActionList;
      this.allStateIneligibilityActions.sort(UtilService.SortSimple);
      this.loadPrerequisites();
    },
      err => {
        UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: 'Unable to load critical application settings. Contact Tech Support immediately!' });
        this.router.navigateByUrl('/');
      });
  }

  loadSettlements() {
    if (!this.arbCase.id)
      return;

    this.allCaseFileVMs.length = 0;

    this.svcData.getSettlementsByCaseId(this.id).subscribe(
      data => {
        this.arbCase.caseSettlements = data;
        this.arbCase.caseSettlements.forEach(v => {
          let k = this.allAuthorities.find(a => a.id == v.authorityId)?.key ?? '';
          v.authorityKey = k.toUpperCase();
        });
      },
      err => console.log('Error loading CaseSettlements')
    );
  }

  moveBenchmark(b: BenchmarkGraphSet, i: number = 1) {
    const n = this.allDSToggle.find(d => d.order === b.order + 1);
    if (n)
      n.order = b.order;
    b.order += 1;
    this.allDSToggle.sort(UtilService.SortByOrder);
    this.updateGraph();
  }

  NSARequestDiscChange(e: Event) {
    let v = this.arbCase.NSARequestDiscount;
    if (v !== null && v >= 1)
      this.arbCase.NSARequestDiscount = v / 100;

    if (this.arbCase.NSARequestDiscount === null || (this.arbCase.NSARequestDiscount <= 0 || this.arbCase.NSARequestDiscount > .99)) {
      this.arbCase.NSARequestDiscount = !!this.calculatorVars ? this.calculatorVars.nsaOfferDiscount : 0;
      this.svcToast.show(ToastEnum.danger, 'Invalid NSA Request Discount! Value reset.')
    }
    const t = e.target as any;
    if (t?.value === '') {
      this.arbCase.NSARequestDiscount = !!this.calculatorVars ? this.calculatorVars.nsaOfferDiscount : 0;
    }
  }

  NSAStatusChange() {
    this.lockNSAId = false; // for now, just let the users manage this since the rules are still unclear - !this.isNew && !!this.arbCase.NSACaseId && (!this.calcForm.form.controls['NSACaseId'].dirty || this.arbCase.NSAStatus==='Ineligible');
  }
  /*
  NSARequestOverrideChange() {
    // fh80th * N = NSAOpenRequestOffer
    if(!this.arbCase.NSAOpenRequestOffer||!this.arbCase.fh80thPercentileExtendedCharges)
      return;
    let disc = this.arbCase.NSAOpenRequestOffer / this.arbCase.fh80thPercentileExtendedCharges;
    disc = UtilService.RoundMoney(disc);
    disc = disc >=1 ? disc * -1 : 1-disc;
    this.arbCase.NSARequestDiscount = disc;
  }
  */
  onDateSelect(e: any) {
    if (!!e.target && !!e.target.value) {
      const date = new Date(e.target.value);
      if (!date || !UtilService.IsDateValid(date)) {
        this.svcToast.show(ToastEnum.danger, 'The entered value does not appear to be a valid date!');
        if (!!this.trackingObject[e.target.id] && e.target.id)
          this.trackingObject[e.target.id] = null;
      }
    }

    this.calcForm.form.markAsDirty();
    UtilService.UpdateTrackingCalculations(this.activeAuthority?.trackingDetails, this.trackingObject, false, this.arbCase);
  }

  onNSADateSelect(e: any) {
    if (!!e.target && !!e.target.value) {
      const date = new Date(e.target.value);
      if (!date || !UtilService.IsDateValid(date)) {
        this.svcToast.show(ToastEnum.danger, 'The entered value does not appear to be a valid date!');
        if (!!this.NSATrackingObject[e.target.id] && e.target.id)
          this.NSATrackingObject[e.target.id] = null;
      }
    }

    this.calcForm.form.markAsDirty();
    UtilService.UpdateTrackingCalculations(this.NSAAuthority?.trackingDetails, this.NSATrackingObject, false, this.arbCase);
  }

  onNavigationConfirmed(e: any) {
    this.allowNavigation.next(e);
  }

  onSubmit() {
    if (!this.calcForm.valid)
      return false;

    this.isSaving = true;
    this.svcUtil.showLoading = true;
    let changes: any;
    try {
      // save NSA tracking info 
      this.arbCase.NSATracking = JSON.stringify(this.NSATrackingObject);

      // process the tracking data for non-TDI cases
      if (this.arbCase.authority && this.arbCase.authority !== 'tx' && this.activeAuthority?.trackingDetails && this.trackingObject) {
        if (!this.arbCase.tracking) {
          this.arbCase.tracking = new CaseTracking();
        }
        UtilService.SyncTrackingToCase(this.activeAuthority?.trackingDetails, this.trackingObject, this.arbCase);
        this.arbCase.tracking.trackingValues = JSON.stringify(this.trackingObject);
      } else {
        this.arbCase.tracking = null;
      }
    } catch (err: any) {
      loggerCallback(LogLevel.Error, err);
      this.svcToast.show(ToastEnum.danger, "Could not read all of the date fields (ArbitrationBriefDueDate, DOB, EOB, FirstAppealDate, FirstResponseDate, ResolutionDeadlineDate, ServiceDate)", 'Date Parsing Error');
    }

    if (this.arbCase.id > 0)
      changes = UtilService.GetDifferences(this.origCase, this.arbCase);  // generate a change object that doesn't contain the Notes field since Notes are serialized anyway

    // preserve any new Notes but do not send back altered notes
    this.arbCase.notes = this.arbCase.notes.filter(v => v.id === 0);

    // save any text in the newNote field
    if (this.newNote.length > 5) {
      const nn = new Note({ details: this.newNote });
      this.arbCase.notes.push(nn);
    }

    // ensure NSARequestDiscount has a value
    this.arbCase.NSARequestDiscount = !this.arbCase.NSARequestDiscount ? (!!this.calculatorVars ? this.calculatorVars.nsaOfferDiscount : 0) : this.arbCase.NSARequestDiscount;

    // pick the right endpoint
    if (this.arbCase.id < 1) {
      this.svcData.createArbitrationCase(this.arbCase).subscribe(data => {
        this.svcToast.show(ToastEnum.success, 'Case created successfully!');
        this.caseLoaded(data);
      }, err => {
        this.caseLoadError(err);
      });
    } else {
      // log the changes
      console.log(`${this.currentUser?.email} made changes:`, changes);
      this.arbCase.log.push(new CaseLog({ action: 'UIUpdate', details: JSON.stringify(changes) }));
      this.svcData.updateArbitrationCase(this.arbCase).subscribe(data => {
        this.svcToast.show(ToastEnum.success, 'Case updated successfully!');
        this.caseLoaded(data);
      },
        err => this.caseLoadError(err)
      );
    }
    return true;
  }

  openExternalLink(s: string) {
    if (!this.arbCase.providerName)
      return;
    const names = this.arbCase.providerName.split(' ');
    names.shift();
    const name = names.join(' ');
    window.open(`https://nneuro.sharepoint.com/CV/Forms/AllItems.aspx?view=7&q=${name}`, '_blank');
  }

  openOfferDialog(t: string, po: OfferHistory | undefined = undefined) {
    const isEditing = po ? true : false;
    if (!isEditing && t.toLowerCase() === 'payor' && !this.arbCase.payorNegotiatorId) {
      if (!confirm('Are you sure you want to continue without assigning a Payor Negotiator?'))
        return;
    }
    const modalRef = this.modalService.open(AddOfferComponent);
    modalRef.componentInstance.name = 'Add'; //'addOffer';
    modalRef.componentInstance.hasSettlements = !!this.arbCase.caseSettlements.length;
    modalRef.componentInstance.offerType = t;
    modalRef.componentInstance.offerId = 0;
    modalRef.componentInstance.isManager = this.isManager;
    modalRef.componentInstance.fh80 = this.getNSARequestOffer();
    if (po) {
      modalRef.componentInstance.name = 'Edit';
      modalRef.componentInstance.offerAmount = po.offerAmount;
      modalRef.componentInstance.offerSource = po.offerSource;
      modalRef.componentInstance.wasOfferAccepted = po.wasOfferAccepted;
      modalRef.componentInstance.offerId = po.id;
    }

    modalRef.closed.subscribe(data => {
      const offer = po ?? new OfferHistory();
      offer.arbitrationCaseId = this.arbCase.id;
      offer.notes = modalRef.componentInstance.notes;
      offer.offerAmount = modalRef.componentInstance.offerAmount;
      offer.offerSource = modalRef.componentInstance.offerSource;
      offer.offerType = modalRef.componentInstance.offerType;
      offer.wasOfferAccepted = modalRef.componentInstance.wasOfferAccepted;
      offer.updatedOn = new Date();
      if (!isEditing) {
        this.arbCase.offerHistory.unshift(offer);
      }
      if (offer.offerType === 'Payor') {
        this.arbCase.payorFinalOfferAmount = offer.offerAmount;
      } else {
        this.arbCase.providerFinalOfferAdjustedAmount = offer.offerAmount;
      }
      this.recalc();
      this.calcForm.form.markAsDirty();
      this.svcToast.show(ToastEnum.success, `${t} offer queued for saving. Offer field updated.`);
      this.svcToast.showAlert(ToastEnum.warning, 'Offer history is queued for saving. Be sure to click Save Changes to keep it!');
      this.scrollToTop();
    });
  }

  openSettlementDialog(d: CaseSettlement) {
    const isFormal = d.prevailingParty !== 'Informal';
    let payor = this.allPayors.find(d => d.id === this.arbCase.payorId);
    if (!!payor && payor.id !== payor.parentId)
      this.allPayors.find(d => d.id === payor!.parentId);

    this.svcUtil.showLoading = true;
    this.svcData.getCPTDescriptions(this.arbCase.id).subscribe(cpts => {
      this.svcUtil.showLoading = false;
      const modalRef = this.modalService.open(SettlementDialogComponent);
      const ci = modalRef.componentInstance;
      ci.allCPTDescriptions = cpts;
      ci.settlement = d;
      ci.currentPayor = payor;
      ci.arbCase = this.arbCase;
      ci.allAuthorities = this.allAuthorities;
      ci.NSAAuthority = this.NSAAuthority;
      ci.isFormal = isFormal;
      ci.userIsManager = this.isManager;
      ci.userIsNSA = this.currentUser?.isNSA;
      ci.userIsState = this.currentUser?.isState;
      modalRef.closed.subscribe(data => {
        const s = data as CaseSettlement;
        const f = this.arbCase.caseSettlements.find(v => v.id === s.id);
        if (!f)
          this.arbCase.caseSettlements.push(s);
      });
    },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger, 'Unable to retrieve CPT descriptions. See console for errors.');
        console.error(err);
      });
  }

  payorChange() {
    if (!!this.origCase.payorId && !confirm('WARNING: Changing the Payor will clear out Group, Negotiator, and Entity information! Continue?')) {
      this.arbCase.payorId = this.origCase.payorId;
      return;
    }

    this.arbCase.payorGroupName = '';
    this.arbCase.payorGroupNo = '';
    this.arbCase.payorNegotiator = null;
    this.arbCase.payorEntity = null;
    this.allNegotiators.length = 0;
    this.arbCase.payorNegotiatorId = null;
    if (this.arbCase.payorId === null) {
      this.arbCase.payor = '';
      return;
    } else {
      this.arbCase.payor = this.allPayors.find(d => d.id === this.arbCase.payorId)!.name;
    }

    this.loadNegotiators();

  }

  payorGroupNumberChange() {
    if (!this.arbCase || !this.arbCase.payorId || !this.arbCase.payorGroupNo)
      return;
    let p = this.allPayors.find(d => d.id === this.arbCase.payorId);
    if (!!p && p.id !== p.parentId)
      p = this.allPayors.find(d => d.id === p!.parentId);
    if (p) {
      this.svcData.getPayorGroup(p, this.arbCase.payorGroupNo).subscribe(data => {
        if (!data || !data?.id)
          return;
        this.arbCase.payorGroupName = data.groupName;
        if (data.planType === PlanType.FullyInsured)
          this.arbCase.planType = 'Fully Insured';
        else if (data.planType === PlanType.SelfFunded)
          this.arbCase.planType = 'Self-Funded';
        else if (data.planType === PlanType.SelfFundedOptIn)
          this.arbCase.planType = 'Self-Funded (Opt-In)'
      },
        err => this.caseLoadError(err)
      );
    }
  }

  payorNegotiatorChange() {
    if (this.arbCase.payorNegotiatorId) {
      this.arbCase.payorNegotiator = this.allNegotiators.find(d => d.id === this.arbCase.payorNegotiatorId) ?? null;
    } else {
      this.arbCase.payorNegotiatorId = null;
    }
  }

  recalc() {
    let fh50 = 0, fh80 = 0, tca = 0, tpa = 0, pra = 0.0;
    //const prevOffer = !!this.arbCase.NSAOpenRequestOffer ? this.arbCase.NSAOpenRequestOffer : this.getNSARequestOffer();
    this.arbCase.cptCodes.filter(d => d.isIncluded).map(c => {
      fh50 = UtilService.RoundMoney(fh50 + c.fh50thPercentileExtendedCharges);
      fh80 = UtilService.RoundMoney(fh80 + c.fh80thPercentileExtendedCharges);
      tca = UtilService.RoundMoney(tca + c.providerChargeAmount);
      tpa = UtilService.RoundMoney(tpa + c.paidAmount);
      pra = UtilService.RoundMoney(pra + c.patientRespAmount);
    });

    this.arbCase.fh50thPercentileExtendedCharges = fh50;
    this.arbCase.fh80thPercentileExtendedCharges = fh80;
    this.arbCase.totalChargedAmount = tca;
    this.arbCase.totalPaidAmount = tpa;
    this.arbCase.patientShareAmount = pra;

    // check calculation vars before each recalculate
    if (this.arbCase.serviceLine !== this.calculatorVars?.serviceLine) {
      this.calculatorVars = this.allCalcVariables.find(c => c.serviceLine.toLowerCase() === this.arbCase.serviceLine.toLowerCase());
    }


    /* Provider Final Offer Amount calculation
       from Mandy's Excel workbook:
        MAX( MIN( ( ([Fair Health Extended Allowed Amount] - [Payor Final Offer])*(1-[Offer Spread])) + [Fair Health Extended Allowed Amount],[Provider Final Offer Not to Exceed]), [Payor Final Offer])
    */

    let fhWithSpread = 0, notToEx = 0;
    if (this.calculatorVars) {
      /* Provider Not To Exceed */
      this.arbCase.providerFinalOfferNotToExceed = UtilService.RoundMoney(Math.min(this.calculatorVars.offerCap, this.arbCase.fh80thPercentileExtendedCharges * this.calculatorVars.chargesCapDiscount));
      this.arbCase.providerFinalOfferNotToExceed = UtilService.RoundMoney(this.arbCase.providerFinalOfferNotToExceed);
      notToEx = this.arbCase.providerFinalOfferNotToExceed;
      fhWithSpread = UtilService.RoundMoney(((fh50 - this.arbCase.payorFinalOfferAmount) * (1 - this.calculatorVars.offerSpread)) + fh50);
    }

    this.arbCase.providerFinalOfferCalculatedAmount = Math.max(Math.min(fhWithSpread, notToEx), this.arbCase.payorFinalOfferAmount);

    // fix all of the values due to probable extra decimals
    this.arbCase.providerFinalOfferCalculatedAmount = UtilService.RoundMoney(this.arbCase.providerFinalOfferCalculatedAmount);
    if (this.arbCase.providerFinalOfferAdjustedAmount) {
      this.arbCase.providerFinalOfferAmount = this.arbCase.providerFinalOfferAdjustedAmount;
    }

  }

  reloadBenchmarks(updateCPTs: boolean = false) {
    if (!this.arbCase.cptCodes.length)
      return;

    let geo = this.arbCase.benchmarkGeoZip ? this.arbCase.benchmarkGeoZip : this.arbCase.locationGeoZip;
    geo = geo.length < 3 ? '' : geo.substring(0, 3);
    if (!geo || !this.activeAuthority) {
      this.svcToast.show(ToastEnum.danger, 'Cannot reload Benchmarks until Authority and GeoZip are selected');
      return;
    }

    const dds = this.getDefaultBenchmarkConfig();
    const ddsId = dds ? dds.benchmarkDatasetId : 0;
    if (ddsId === 0) {
      this.svcToast.show(ToastEnum.danger, 'The selected Authority and ServiceLine combination does not have a default Benchmark assigned!');
      return;
    }

    const calls$ = new Array<Observable<BenchmarkDataItem | null>>();

    for (const cpt of this.arbCase.cptCodes.filter(d => !!d.cptCode)) {
      for (const bm of this.activeAuthority.benchmarks.filter(e => e.service === this.arbCase.service)) {  // .filter(d=>d.isActive)  <- need to find a way to do this...prob need to join BenchmarkDataSets with AuthorityBenchmarks and filter on isActive server-side
        calls$.push(this.svcData.loadBenchmarks(bm.benchmarkDatasetId, geo, cpt.cptCode, cpt.modifier26_YN));
      }
    }

    if (calls$.length) {
      this.svcUtil.showLoading = true;
      forkJoin(calls$).subscribe(data => {
        this.svcUtil.showLoading = false;
        let markDirty = false;
        if (data && data.length) {
          const marks = data.filter(j => !!j);
          if (!marks.length) { this.svcToast.show(ToastEnum.warning, 'No benchmark values found for the current GeoZip') }
          for (const bmk of marks) {
            const cpt = this.arbCase.cptCodes.find(d => d.cptCode.toLowerCase() === bmk?.procedureCode.toLowerCase());
            if (updateCPTs && bmk!.benchmarkDatasetId === ddsId) {
              markDirty = true;
              this.updateCptCalculations(dds!, bmk!, cpt!); // update the ClaimCPT record w/fresh values
            }
            // update the graph data
            const abm = this.activeAuthority!.benchmarks.find(d => d.benchmarkDatasetId === bmk?.benchmarkDatasetId && d.service.toLowerCase() === this.arbCase.service.toLowerCase()) /// .filter(d=>d.isActive)  <- need to find a way to do this...prob need to join BenchmarkDataSets with AuthorityBenchmarks and filter on isActive server-side
            this.allDSToggle.find(h => h.datasetId === bmk?.benchmarkDatasetId)?.addInsertItem(abm!, bmk!, cpt!);
          }
        }
        this.recalc();
        this.updateGraph();
        if (markDirty)
          this.calcForm.form.markAsDirty();
      });
    } else {
      this.recalc();
    }
  }

  resetFormStatus() {
    this.calcForm.form.markAsUntouched();
    this.calcForm.form.markAsPristine();
    Object.keys(this.calcForm.controls).forEach((key) => {
      const control = this.calcForm.controls[key];
      control.markAsUntouched();
      control.markAsPristine();
    });
  }

  _beforeBMGeoZip = '';

  resetBMZip() {
    if (this.arbCase.benchmarkGeoZip.length < 3 || this.arbCase.benchmarkGeoZip.length > 5)
      this.arbCase.benchmarkGeoZip = '';
    if (this.arbCase.benchmarkGeoZip !== this._beforeBMGeoZip) {
      this.arbCase.cptCodes.forEach(d => {
        //this.clearCPT(d);
        // request new benchmark
        this.cptChanged(d);
      });
      this.svcToast.showAlert(ToastEnum.warning, 'If you save changes, Benchmarks will recalculate and save. This will not update pending Notifications!');
    }
  }

  resetZip() {
    this.arbCase.cptCodes.forEach(d => {
      //this.clearCPT(d);
      // request new benchmark
      this.cptChanged(d);
    });

    this.isZiplocked = (this.arbCase.locationGeoZip.length > 2 || this.arbCase.benchmarkGeoZip.length > 2);
  }

  selectAll(e: any) {
    e?.target?.select();
  }

  serviceChange() {
    const s = this.services.find(d => d.name.toLowerCase() === this.arbCase.service.toLowerCase());
    this.arbCase.serviceLine = s?.serviceLine ?? '';
    this.calculatorVars = this.allCalcVariables.find(c => c.serviceLine.toLowerCase() === this.arbCase.serviceLine.toLowerCase());
    // Clean up bad discount from server - data imports could do something weird
    if (this.arbCase.NSARequestDiscount !== null && (this.arbCase.NSARequestDiscount <= 0 || this.arbCase.NSARequestDiscount > .99)) {
      this.arbCase.NSARequestDiscount = !!this.calculatorVars ? this.calculatorVars.nsaOfferDiscount : 0;
    }

    this.setActiveAuthority();
    this.calcForm.form.markAsDirty();
    this.calcForm.form.markAsTouched();
    this.svcChangeDetection.detectChanges();
  }

  getServiceLocationName() {
    if (!this.arbCase.serviceLocationCode)
      return '';

    return this.allPlaceCodes.find(v => v.codeNumber === this.arbCase.serviceLocationCode)?.name ?? '';
  }

  setActiveAuthority() {
    if (this.arbCase.authority && this.allAuthorities.length) {
      this.activeAuthority = this.allAuthorities.find(d => d.key.toLowerCase() === this.arbCase.authority.toLowerCase());
      const invalidAuthStatus = (this.arbCase.authorityStatus === 'Not Submitted' && !!this.arbCase.authorityCaseId);

      this.lockAuthority = !!this.activeAuthority?.website; // !this.isManager && !invalidAuthStatus - removed per Deborah 5/Jun2023
      this.isCaseActive = this.caseIsActive();
      // TX batch exception
      if (this.arbCase.authority.toLowerCase() === 'tx' && this.arbCase.authorityCaseId.indexOf('B') > -1)
        this.lockAuthority = false;

      if (invalidAuthStatus)
        this.svcToast.showAlert(ToastEnum.warning, 'Warning: The Authority Case Number should be empty if the Authority Status is Not Submitted. This Case will not save successfully.');

      this.lockAuthorityId = !this.isNew && !invalidAuthStatus && (!!this.arbCase.authorityCaseId || !this.activeAuthority?.isActive);

      this.lockNSA = false; // let users manage until we know more - !this.isNew && (this.calcForm.form.dirty || !!this.NSAAuthority?.website); 
      this.lockNSAId = false; // let users manage - !this.isNew && !!this.arbCase.NSACaseId && (!this.calcForm.form.controls['NSACaseId'].dirty || this.arbCase.NSAStatus==='Ineligible');

      if (this.activeAuthority) {
        // setup status choices
        this.authorityStatuses = this.activeAuthority.statusList;
        if (!!this.arbCase.authorityStatus) {
          const f = this.authorityStatuses.find(k => k.toLowerCase() === this.arbCase.authorityStatus.toLowerCase());
          if (!f)
            this.authorityStatuses.push(this.arbCase.authorityStatus); // in case the list of statuses changes w/o being synced to the db
        }

        // setup date tracking scheme
        // NOTE / TODO: How to deal with a State that may decided to go from bifurcated to NSA only? Historical records...? Version date schema?
        this.trackingFieldsForUI = this.activeAuthority.trackingDetails.filter(d => !d.isHidden);
        this.setTrackingObject();
        // setup benchmarks for graphing
        this.allDSToggle.length = 0;
        this.defaultBenchmark = this.getDefaultBenchmarkConfig();
        for (const d of this.activeAuthority.benchmarks) {
          if (!this.allDSToggle.find(g => g.datasetId === d.benchmarkDatasetId)) {
            const bm = this.allBenchmarkDatasets.find(g => g.id === d.benchmarkDatasetId) ?? { dataYear: 0, name: 'Benchmark 1', id: d.benchmarkDatasetId, isActive: true };
            const bgs = new BenchmarkGraphSet(d.benchmarkDatasetId, bm.name, bm.dataYear);
            bgs.isVisible = bm.isActive;
            this.allDSToggle.push(bgs);
          }
        }
        // normalize the order values
        for (let x = 0; x < this.allDSToggle.length; x++) {
          this.allDSToggle[x].order = x;
        }
      }
    } else {
      this.trackingFieldsForUI.length = 0;
      this.allDSToggle.length = 0;
      this.defaultBenchmark = undefined;
    }
  }

  setActiveCustomer() {
    this.allEntities = [];
    this.currentCustomer = this.allCustomers.find(v => v.name.toLowerCase() === this.arbCase.customer.toLowerCase());
    if (!!this.currentCustomer) {
      this.currentCustomer.entities.sort(UtilService.SortByName);
      this.allEntities = this.currentCustomer.entities;
      let nt: Entity | undefined;

      if (this.arbCase.id > 0) {
        if (!!this.arbCase.entityNPI) {
          nt = this.allEntities.find(v => v.NPINumber === this.arbCase.entityNPI);
        }
        if (!nt && !!this.arbCase.entity) {
          nt = this.allEntities.find(v => v.name.toLowerCase() === this.arbCase.entity.toLowerCase());
          if (!!nt) {
            this.arbCase.entityNPI = nt.NPINumber;
            this.calcForm.form.markAsDirty();
            this.calcForm.form.markAsTouched();
            this.svcToast.showAlert(ToastEnum.warning, 'The Entity NPI was automatically adjusted to match the Entity Name. Verify this is correct before saving changes or ask a Manager for assistance.');
          }
        }
        if (!nt && (!!this.arbCase.entity || !!this.arbCase.entityNPI)) {
          nt = new Entity({ NPINumber: this.arbCase.entityNPI, name: this.arbCase.entity });
          this.allEntities.push(nt);
          this.svcToast.showAlert(ToastEnum.danger, 'The Entity on this Claim does not match a known Customer Entity! Either add the Entity or choose a different one in the Case Detail section.');
        }
      }
    }
  }

  setEOBDate() {
    if (!!this.NSAAuthority)
      UtilService.UpdateTrackingCalculations(this.NSAAuthority.trackingDetails, this.NSATrackingObject, false, this.arbCase);
  }

  checkIneligibleActionList() {
    if (this.arbCase && this.arbCase.ineligibilityAction && this.allStateIneligibilityActions.indexOf(this.arbCase.ineligibilityAction) === -1) {
      this.allStateIneligibilityActions.push(this.arbCase.ineligibilityAction); // when the existing value was removed as a choice
      this.allStateIneligibilityActions.sort(UtilService.SortSimple);
    }
    if (this.arbCase && this.arbCase.payorNSAIneligibilityAction && this.allNSAIneligibilityActions.indexOf(this.arbCase.payorNSAIneligibilityAction) === -1) {
      this.allNSAIneligibilityActions.push(this.arbCase.payorNSAIneligibilityAction);
      this.allNSAIneligibilityActions.sort(UtilService.SortSimple);
    }
    // remove State as an option for NSA Ineligibility reason
    if (!this.activeAuthority?.isActive) {
      const ndx = this.allNSAIneligibilityActions.findIndex(d => d.toLowerCase() === 'state arbitration');
      if (ndx > -1)
        this.allNSAIneligibilityActions.splice(ndx, 1);
    }

  }

  setIneligibilityAction() {
    if (this.arbCase?.ineligibilityReasons.toLowerCase() === 'n/a')
      this.arbCase.ineligibilityReasons = '';
    // Payor Ineligibility
    if (this.arbCase && this.arbCase.ineligibilityReasons && !this.arbCase.ineligibilityAction) {
      // try to auto-select the action
      if (this.arbCase.ineligibilityReasons.indexOf('self-funded health plan governed by ERISA') > -1)
        this.arbCase.ineligibilityAction = 'NSA';
      else if (this.arbCase.ineligibilityReasons.indexOf('certificate of insurance or other evidence of coverage, was not issued') > -1)
        this.arbCase.ineligibilityAction = 'Out of State Policy';
      else if (this.arbCase.ineligibilityReasons.indexOf("patient and claim information entered doesn't match our records") > -1)
        this.arbCase.ineligibilityAction = 'Mismatch Information';
      else if (this.arbCase.ineligibilityReasons.indexOf('request was made less than 20 days or more than 90 days after payment') > -1)
        this.arbCase.ineligibilityAction = 'Timing';
      else if (this.arbCase.ineligibilityReasons.indexOf('The services billed were denied under the plan.') > -1)
        this.arbCase.ineligibilityAction = 'Denial';
      else if (this.arbCase.ineligibilityReasons.indexOf('This is a duplicate request') > -1)
        this.arbCase.ineligibilityAction = 'Duplicate';

      if (this.arbCase.ineligibilityAction) {
        this.calcForm.form.markAsDirty();
        this.svcToast.showAlert(ToastEnum.info, 'The Ineligibility Action value was automatically adjusted! Save the changes to confirm.');
      }
    }
    this.checkIneligibleActionList();
  }

  showDeliveryDetails(n: Notification) {
    const deliveryModalOptions: NgbModalOptions = {
      backdrop: 'static',
      backdropClass: 'customBackdrop',
      keyboard: false,
      size: 'xl'
    };
    const modalRef = this.modalService.open(NotificationDeliveryDialogComponent, deliveryModalOptions);
    modalRef.componentInstance.notification = n;
  }

  subscribeToData() {
    // listen for loading of user info
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(data => {
      this.canEdit = !!(data.isManager || data.isNegotiator);
      this.isManager = !!data.isManager;
      this.isNegotiator = !!data.isNegotiator;
      this.isNSA = !!data.isNSA;
      this.isReporter = !!data.isReporter;
      this.isState = !!data.isState;
      this.currentUser = data;
    });
  }

  /** 
   * Self-healing function
  */
  setActivePayor() {
    if (!this.allPayors.length || !this.arbCase || (!this.arbCase.payor && !this.arbCase.payorId))
      return;

    const p = (this.arbCase.payorId ?? 0) > 0 ? this.allPayors.find(d => d.id === this.arbCase.payorId) : this.allPayors.find(d => d.name.toLowerCase() === this.arbCase.payor.toLowerCase());
    if (!p) {
      this.arbCase.payorId = null;
      this.arbCase.payor = '';
      this.svcToast.showAlert(ToastEnum.warning, 'Unable to match the payor to a known entity. Please re-select Payor.');
      return;
    }

    if (p.name.toLowerCase() !== this.arbCase.payor.toLowerCase() || p.id !== this.arbCase.payorId) {
      const z = this.arbCase.payor;
      this.arbCase.payorId = p.id;
      this.arbCase.payor = p.name;
      if (this.calcForm) {
        this.calcForm.form.markAsDirty();
        this.calcForm.form.markAsTouched();
        this.svcToast.showAlert(ToastEnum.warning, `Payor name and PayorId synchronized. (Payor name was '${z}'.) Please save changes to keep this update.`);
      }
    }
    this.setIsExcludedNPI();
  }

  scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  setIsExcludedNPI(p: Payor | undefined = undefined) {
    this.isExcludedNPI = false;

    if (!p) {
      p = (this.arbCase.payorId ?? 0) > 0 ? this.allPayors.find(d => d.id === this.arbCase.payorId) : this.allPayors.find(d => d.name.toLowerCase() === this.arbCase.payor.toLowerCase());
    }

    // show Exclusion warning
    if (!!p && p.excludedEntities.length) {
      this.isExcludedNPI = !!p.excludedEntities.find(d => d.NPINumber === this.arbCase.entityNPI);
      if (this.isExcludedNPI)
        this.svcToast.showAlert(ToastEnum.danger, "Warning! The Entity assigned to this claim is in the Payor's arbitration exclusion list!");
    }
  }

  setNSATrackingObject() {
    this.NSATrackingObject = null;
    if (!this.NSAAuthority) {
      UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: 'The NSA Authority profile is missing or unavailable. Cannot continue loading this Claim. Contact Tech Support immediately.' });
      this.router.navigateByUrl('/');
      return;
    }

    if (this.arbCase.NSATracking) {
      try {
        this.NSATrackingObject = JSON.parse(this.arbCase.NSATracking);
        // TODO: Detect if the tracking schema is changed. If so, set a UI flag to lock the tracking section and make the user manually choose to update it
      } catch (err) {
        this.svcToast.showAlert(ToastEnum.danger, 'The NSA Tracking info for this Case could not be parsed and must be reset! Either re-enter the fields or contact Tech Support immediately.');
      }
    }

    if (!this.NSATrackingObject && this.NSAAuthority.trackingDetails.length) {
      this.NSATrackingObject = UtilService.CreateTrackingObject(this.NSAAuthority.trackingDetails);
    }

    if (this.NSATrackingObject) {
      this.NSATrackingObject = UtilService.TransformTrackingObject(this.NSAAuthority.trackingDetails, this.NSATrackingObject, true);
      const cloned = Object.assign({}, this.NSATrackingObject);
      UtilService.UpdateTrackingCalculations(this.NSAAuthority.trackingDetails, this.NSATrackingObject, false, this.arbCase);
      this.NSATrackingObject = UtilService.TransformTrackingObject(this.NSAAuthority.trackingDetails, this.NSATrackingObject, true);
      if (JSON.stringify(cloned) !== JSON.stringify(this.NSATrackingObject)) {
        this.calcForm.form.markAsDirty();
        this.calcForm.form.markAsTouched();
        this.svcToast.showAlert(ToastEnum.danger, 'The calculated NSA Dates and Deadlines are different than the saved values. Please click Save Changes to keep the on-screen calculations.');
      }
    }
  }

  setTrackingObject() {
    this.trackingObject = null;
    if (!this.activeAuthority) {
      UtilService.PendingAlerts.push({ level: ToastEnum.danger, message: `The Authority profile is missing or unavailable for ${this.arbCase.authority}. Cannot continue loading this Claim. Contact Tech Support immediately!` });
      this.router.navigateByUrl('/');
      return;
    }

    if (this.arbCase.tracking && this.arbCase.tracking.trackingValues) {
      try {
        this.trackingObject = JSON.parse(this.arbCase.tracking.trackingValues);
        // TODO: Detect if the tracking schema is changed. If so, set a UI flag to lock the tracking section and make the user manually choose to update it
      } catch (err) {
        this.svcToast.showAlert(ToastEnum.danger, 'The Tracking info for this Case could not be parsed and must be reset! Either re-enter the fields or contact Tech Support immediately.');
      }
    }

    if (!this.trackingObject && this.arbCase.authority) {
      const a = this.allAuthorities.find(d => d.key.toLowerCase() === this.arbCase.authority.toLowerCase());
      if (a && a.trackingDetails.length) {
        this.trackingObject = UtilService.CreateTrackingObject(a.trackingDetails);
      }
    }

    if (this.trackingObject) {
      this.trackingObject = UtilService.TransformTrackingObject(this.activeAuthority.trackingDetails, this.trackingObject, true);
      const cloned = Object.assign({}, this.trackingObject);
      UtilService.UpdateTrackingCalculations(this.activeAuthority.trackingDetails, this.trackingObject, false, this.arbCase);
      if (JSON.stringify(cloned) !== JSON.stringify(this.trackingObject)) {
        this.calcForm.form.markAsDirty();
        this.calcForm.form.markAsTouched();
        this.svcToast.showAlert(ToastEnum.danger, 'The calculated State Dates and Deadlines are different than the saved values. Please click Save Changes to keep the on-screen calculations.');
      }
    }
  }

  showButton(c: string) {
    const s = this.arbCase.status;
    const n = this.arbCase.NSAWorkflowStatus;
    switch (c) {
      case 'acceptoffer':
        if (!this.arbCase.offerHistory?.length)
          return false;
        if (this.arbCase.offerHistory.find(d => d.wasOfferAccepted))
          return false;
        if (!this.arbCase.payorFinalOfferAmount && !this.arbCase.providerFinalOfferAmount)
          return false;
        const isStateClosed = s === CMSCaseStatus.ClosedPaymentReceived ||
          s === CMSCaseStatus.ClosedPaymentWithdrawn ||
          s === CMSCaseStatus.Ineligible ||
          s === CMSCaseStatus.Search ||
          s === CMSCaseStatus.SettledArbitrationPendingPayment ||
          s === CMSCaseStatus.SettledInformalPendingPayment ||
          s === CMSCaseStatus.SettledOutsidePendingPayment;
        const isNSAClosed = n === CMSCaseStatus.ClosedPaymentReceived ||
          n === CMSCaseStatus.ClosedPaymentWithdrawn ||
          n === CMSCaseStatus.Ineligible ||
          n === CMSCaseStatus.Search ||
          n === CMSCaseStatus.SettledArbitrationPendingPayment ||
          n === CMSCaseStatus.SettledInformalPendingPayment ||
          n === CMSCaseStatus.SettledOutsidePendingPayment;
        return !isStateClosed || !isNSAClosed;

      default:
        return false;
    }
  }

  showWarnings() {
    if (!this.arbCase.serviceLine || !this.arbCase.service)
      this.svcToast.showAlert(ToastEnum.danger, 'Service or ServiceLine are missing!');
    if (this.arbCase.hasArbitratorWarning)
      this.svcToast.showAlert(ToastEnum.danger, 'There appears to be a Last Resort Arbitrator still active on this case!');
  }

  toggleDataset(a: any) {
    this.updateGraph();
  }
  /*
  toggleDate(s: string) {
    const d = document.getElementById(s) as any;
    d.toggle();
  }
  */
  updateCptCalculations(abd: AuthorityBenchmarkDetails, bm: BenchmarkDataItem, cpt: ClaimCPT): boolean {
    let updatesMade = false;
    if (abd && bm && abd.payorAllowedField && abd.providerChargesField) {
      updatesMade = true;
      cpt.fh50thPercentileCharges = bm.values[abd.payorAllowedField] || 0;
      cpt.fh50thPercentileExtendedCharges = Math.floor((cpt.units * cpt.fh50thPercentileCharges * 100) + .5) / 100;
      cpt.fh80thPercentileCharges = bm.values[abd.providerChargesField] || 0;
      cpt.fh80thPercentileExtendedCharges = Math.floor((cpt.units * cpt.fh80thPercentileCharges * 100) + .5) / 100;
    } else {
      this.clearCPT(cpt);
    }
    return updatesMade;
  }

  updateGraph() {
    // collect and validate required data
    let offer = this.arbCase.providerFinalOfferAdjustedAmount ? this.arbCase.providerFinalOfferAdjustedAmount : this.arbCase.providerFinalOfferCalculatedAmount;
    offer = Math.round(offer);
    const hasFR = this.arbCase.fh50thPercentileExtendedCharges > 0;

    // reset
    this.barChartData = {
      labels: ['Payor Final Offer', 'Provider Final Offer'],
      datasets: [
        {
          data: [0, 0],
          backgroundColor: [this.chartTheme[0], this.chartTheme.slice(-1)[0]]
        }
      ]
    };

    if (this.arbCase.payorFinalOfferAmount || hasFR || offer) {
      const ds = this.barChartData.datasets[0];
      // put the payor and provider offers on the "ends"
      ds.data = [Math.round(this.arbCase.payorFinalOfferAmount), offer];  // Math.round(this.arbCase.fh50thPercentileExtendedCharges), 
      // sort the graph data
      this.allDSToggle.sort(UtilService.SortByOrder);
      for (const g of this.allDSToggle) {
        if (!g.isVisible)
          continue;
        ds.data.splice(-1, 0, Math.round(g.payorAllowedTotal));  //ad the benchmark value just before the offer (last)
        this.barChartData.labels!.splice(-1, 0, g.name);
      }
      // assign colors
      const f = ds.data.length - 1;
      ds.backgroundColor = this.chartTheme.slice(0, f);
      ds.backgroundColor = ds.backgroundColor.concat(this.chartTheme.slice(-1));

    } else {
      this.svcToast.showAlert(ToastEnum.warning, `Insufficient data for graphs. Check Payor Final Offer Amount, CPT [Allow] checkboxes, then click Reload Benchmarks.`);
      loggerCallback(LogLevel.Warning, 'Not enough data to update graph');
    }


    // draw the range gage canvas    
    let pct = .5;
    if (hasFR && this.arbCase.providerFinalOfferAmount > this.arbCase.fh50thPercentileExtendedCharges) {
      pct = (this.arbCase.fh50thPercentileExtendedCharges - this.arbCase.payorFinalOfferAmount) / (this.arbCase.providerFinalOfferAmount - this.arbCase.payorFinalOfferAmount);
    }
    const canvas = document.getElementById("rangeGauge") as any;
    const ctx = canvas.getContext("2d");
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.moveTo(30, 50);

    ctx.beginPath();
    ctx.strokeStyle = "dodgerblue";
    ctx.lineWidth = 4;
    ctx.lineTo(30, 25);
    ctx.lineTo(30, 75);
    ctx.stroke();

    ctx.lineWidth = 1;
    ctx.font = "16px Calibri";
    ctx.fillText("Payor", 8, 11);
    ctx.strokeText("Payor", 8, 11);
    ctx.fillText(`$${Math.round(this.arbCase.payorFinalOfferAmount)}`, 8, 99);
    ctx.strokeText(`$${Math.round(this.arbCase.payorFinalOfferAmount)}`, 8, 99);
    ctx.stroke();

    ctx.beginPath();
    ctx.lineWidth = 4;
    ctx.strokeStyle = "black";
    ctx.beginPath();
    ctx.moveTo(30, 50);
    ctx.lineTo(370, 50);
    ctx.stroke();

    ctx.beginPath();
    ctx.strokeStyle = "dodgerblue";
    ctx.lineTo(370, 25);
    ctx.lineTo(370, 75);
    ctx.stroke();

    ctx.beginPath();
    ctx.lineWidth = 1;
    ctx.font = "16px Calibri";
    ctx.fillText("Provider", 340, 11);
    ctx.strokeText("Provider", 340, 11);

    ctx.fillText(`$${Math.round(this.arbCase.providerFinalOfferAmount)}`, 340, 99);
    ctx.strokeText(`$${Math.round(this.arbCase.providerFinalOfferAmount)}`, 340, 99);

    if (hasFR) {
      ctx.strokeStyle = "limegreen";
      const stickPos = Math.trunc(340 * pct);
      ctx.fillText("F&R", stickPos + 30 - 14, 30);
      ctx.strokeText("F&R", stickPos + 30 - 14, 30);
      ctx.fillText(`$${Math.round(this.arbCase.fh50thPercentileExtendedCharges)}`, stickPos + 30 - 20, 80);
      ctx.strokeText(`$${Math.round(this.arbCase.fh50thPercentileExtendedCharges)}`, stickPos + 30 - 20, 80);
      ctx.stroke();

      ctx.beginPath();
      ctx.lineWidth = 4;

      ctx.moveTo(stickPos + 30, 35);
      ctx.lineTo(stickPos + 30, 65);
      ctx.stroke();
    }
  }

  validateNSAId() {
    if (!this.arbCase.NSACaseId) {
      this.arbCase.NSAStatus = '';
      return;
    }
    this.svcData.checkForAuthorityCase('nsa', this.arbCase.NSACaseId).pipe(take(1))
      .subscribe(data => {
        if (data) {
          this.svcToast.showAlert(ToastEnum.danger, `NSA Case number ${this.arbCase.NSACaseId} is already assigned to another <a href="/calculator/${data}" title="Click to open NSA Case ${this.arbCase.NSACaseId}">Claim</a>.`);
          this.arbCase.NSACaseId = '';
        }
      }, err => {
        console.error(err);
        this.svcToast.show(ToastEnum.danger, 'Error validating the NSA Case Id!');
      });
  }

  viewFile(f: CaseFileVM) {
    this.svcData.downloadPDF(this.arbCase.id, f.blobName).pipe(take(1)).subscribe(res => {
      const fileURL = URL.createObjectURL(res);
      window.open(fileURL, '_blank');
    });
  }

  workflowStatusChange(a: string) {
    const f = a === 'nsa' ? 'NSAWorkflowStatus' : 'status';
    const action = a === 'nsa' ? 'payorNSAIneligibilityAction' : 'ineligibilityAction';
    const reason = a === 'nsa' ? 'payorNSAIneligibilityReasons' : 'ineligibilityReasons';
    if (this.arbCase[f] !== CMSCaseStatus.Ineligible && this.origCase[f] === CMSCaseStatus.Ineligible) {
      this.svcUtil.showLoading = true;
      this.svcData.checkForActivePayorClaimNumber(this.arbCase.payorClaimNumber, this.arbCase.id).subscribe(data => {
        if (!!data) {
          this.arbCase[f] = CMSCaseStatus.Ineligible;
          this.svcToast.showAlert(ToastEnum.danger, `Cannot reactivate this Claim. There is already an Active claim with the same Payor Claim Number. Click <a href="/calculator/${data}" title="Click to open Claim ${this.arbCase.payorClaimNumber}">here</a> to open it.`);
        } else if (!!this.arbCase[action] || !!this.arbCase[reason]) {
          this.arbCase[action] = '';
          this.arbCase[reason] = '';
          this.svcToast.show(ToastEnum.warning, 'Ineligibility Action and Reasons were reset. Cancel changes to undo.', 'Warning', 5000);
        }
      },
        err => {
          this.svcToast.show(ToastEnum.danger, 'Error checking for duplicate Active cases. Reloading the page and re-try your action.');
          this.svcUtil.showLoading = false;
        },
        () => this.svcUtil.showLoading = false
      );
    } else if (f === 'NSAWorkflowStatus' && this.arbCase[f] === CMSCaseStatus.Ineligible) {
      this.arbCase.NSAStatus = 'Ineligible';
    }
  }
}
