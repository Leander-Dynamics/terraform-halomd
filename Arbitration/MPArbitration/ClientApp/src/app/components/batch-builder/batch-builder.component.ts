import { ChangeDetectorRef, Component, OnInit, RendererFactory2, ViewChild } from '@angular/core';
import { NgbDateAdapter, NgbDateParserFormatter, NgbInputDatepickerConfig, NgbModal, NgbModalOptions, NgbPopover } from '@ng-bootstrap/ng-bootstrap';
import { DataTableDirective } from 'angular-datatables';
import { BehaviorSubject, combineLatest, Subject } from 'rxjs';
import { AppUser } from 'src/app/model/app-user';
import { ArbitrationCase } from 'src/app/model/arbitration-case';
import { Authority } from 'src/app/model/authority';
import { DocumentTemplate } from 'src/app/model/document-template';
import { EntityCasesVM, EntityVM } from 'src/app/model/entity-vm';
import { NotificationType } from 'src/app/model/notification-type-enum';
import { Notification } from 'src/app/model/notification';
import { NSACaseVM } from 'src/app/model/nsa-case-vm';
import { Payor } from 'src/app/model/payor';
import { PayorEntitiesVM } from 'src/app/model/payor-cases-vm';
import { ToastEnum } from 'src/app/model/toast-enum';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';
import { DocPreviewComponent } from '../doc-preview/doc-preview.component';
import { CalculatorVariables } from 'src/app/model/calculator-variables';
import { BenchmarkDataset } from 'src/app/model/benchmark-dataset';
import { AuthorityTrackingDetail } from 'src/app/model/authority-tracking-detail';
import { Router } from '@angular/router';
import { INotificationDocument } from 'src/app/model/notification-document';
import { CustomDateParserFormatter, CustomNgbDateAdapter } from 'src/app/model/custom-date-handler';
import { CaseFileVM } from 'src/app/model/case-file';
import { Template } from '@angular/compiler/src/render3/r3_ast';

@Component({
  selector: 'app-batch-builder',
  templateUrl: './batch-builder.component.html',
  styleUrls: ['./batch-builder.component.css'],
  providers: [
    { provide: NgbDateAdapter, useClass: CustomNgbDateAdapter },
    { provide: NgbDateParserFormatter, useClass: CustomDateParserFormatter },
    NgbInputDatepickerConfig
  ]
})
export class BatchBuilderComponent implements OnInit {
  @ViewChild('confirmationDialog') confirmationModal: Template | undefined;
  @ViewChild(DataTableDirective, { static: false })
  dtElement: DataTableDirective | undefined;
  activeDocTemplate = NotificationType.NSANegotiationRequest;
  allAuthorities: Authority[] = [];
  allBenchmarkDatasets: BenchmarkDataset[] = [];
  allCalcVars: CalculatorVariables[] = [];
  allCustomers: { name: string, counter: number, arbitCasesCount: number }[] = [];
  allEntities: { isEnabled: boolean, name: string, counter: number }[] = [];
  allNSAPayorsVM: Array<PayorEntitiesVM> = [];
  allPayors: Payor[] = [];
  allUnsentNotifications: Notification[] = [];
  confirmationTitle = 'Batch Notification Failed';
  confirmationMessage = '';
  customer = '';
  entity = '';
  currentUser: AppUser | undefined;
  deadlineFilter: string | undefined;
  destroyed$ = new Subject<void>();
  dtOptions: DataTables.Settings = {};
  dtTrigger: Subject<any> = new Subject<any>();
  isAdmin = false;
  isManager = false;

  modalOptions: NgbModalOptions | undefined;
  NotificationType = NotificationType;
  notifyType: NotificationType = NotificationType.Unknown;
  NSATrackingFieldsForUI = new Array<AuthorityTrackingDetail>();
  records$ = new BehaviorSubject<Array<PayorEntitiesVM>>([]);
  records: Array<PayorEntitiesVM> = [];

  showActions = false;
  showBulkReady = false;
  showHelp = false;
  showSendEmail = false;

  get today(): Date {
    let t = new Date();
    t.setHours(0, 0, 0, 0);
    return t;
  }

  constructor(private svcData: CaseDataService, private svcToast: ToastService,
    private svcUtil: UtilService, private modalService: NgbModal,
    private router: Router, private svcChangeDetector: ChangeDetectorRef) {

    this.deadlineFilter = new Date().toISOString().slice(0, 10);

    this.modalOptions = {
      backdrop: 'static',
      backdropClass: 'customBackdrop',
      keyboard: false,
      fullscreen: true,
    };
  }

  ngOnInit(): void {
    this.svcUtil.showLoading = true;
    this.loadPrerequisites();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
    this.records$.complete();
  }

  addNSACaseVM(data: ArbitrationCase): NSACaseVM {
    let payorVM = this.records.find(d => d.name.toLowerCase() === data.payor.toLowerCase());
    const p = this.allPayors.find(g => g.name.toLowerCase() === data.payor.toLowerCase());
    if (!payorVM) {
      payorVM = new PayorEntitiesVM();
      payorVM.NSAEmail = p?.NSARequestEmail || '{missing!}';
      payorVM.name = data.payor;
      this.allNSAPayorsVM.push(payorVM);
    }
    let entity = payorVM.entities.find(g => g.name.toLowerCase() === data.entity.toLowerCase() && g.NPINumber === data.entityNPI);
    if (!entity) {
      entity = new EntityCasesVM();
      entity.name = data.entity;
      entity.NPINumber = data.entityNPI;
      payorVM.entities.push(entity);
    }
    let caseVM = entity.cases.find(d => d.id === data.id);
    if (!caseVM) {
      caseVM = new NSACaseVM(data); //Object.assign({}, data));
      this.setNSATrackingObject(caseVM);
      entity.cases.push(caseVM); // TODO: Create server-side VMs to reduce what data comes back in the search since we don't need the entire ArbitrationCase record here
      //payorVM.count++;
    } else {
      caseVM = new NSACaseVM(data); //Object.assign({}, data));
      this.setNSATrackingObject(caseVM);
    }

    const vars: CalculatorVariables | undefined = this.allCalcVars.find(x => x.serviceLine === data.serviceLine);
    let disc = (data.NSARequestDiscount !== null && data.NSARequestDiscount > 0 && data.NSARequestDiscount < .99) ? 1 - data.NSARequestDiscount : 1 - (vars?.nsaOfferDiscount ?? 0);
    caseVM.calculatedNSAOffer = UtilService.GetCPTValueSum(data.cptCodes, vars?.nsaOfferBaseValueFieldname) * disc;
    return caseVM;
  }

  cancelNotification(data: any, c: NSACaseVM) {
    const n = this.allUnsentNotifications.find(d => d.arbitrationCaseId === c.id);
    if (!n) {
      console.error('Unexpected: Unable to find an unsent notification matching the NSACaseVM!');
      return
    }
    this.svcUtil.showLoading = true;
    this.svcData.deleteNotification(n).subscribe(data => {
      const ndx = this.allUnsentNotifications.indexOf(n);
      if (ndx !== -1)
        this.allUnsentNotifications.splice(ndx, 1);
      c.isNotificationQueued = false;
      this.svcToast.show(ToastEnum.success, 'Notification successfully canceled.');
    },
      err => {
        if (err.status === 404) {
          this.refreshVM(c);
          return;
        }
        this.handleServiceErr(err);
        this.svcUtil.showLoading = false;
      },
      () => this.svcUtil.showLoading = false
    );

    // TODO: Require a reason to remove a notification e.g. Settled, Expired, etc. ?
  }

  applyFilters(updateEntities: boolean = true) {

    this.deselectAll();
    const incAll = !this.entity && !this.showBulkReady;
    if (updateEntities) {
      this.allEntities.forEach(v => v.isEnabled = incAll || !this.customer);
    }

    // get distinct list of payors that have at least one case w/matching customer or date
    const payorEntitiesVM: PayorEntitiesVM[] = incAll ? JSON.parse(JSON.stringify(this.allNSAPayorsVM))
      : JSON.parse(JSON.stringify(
        this.allNSAPayorsVM
          .filter(g => g.entities
            .filter(b => b.cases
              .find(h => (!this.customer || h.record?.customer.toLowerCase() === this.customer.toLowerCase())
              )
            )
          )
      )
      );

    for (const payerEntityVM of payorEntitiesVM) {
      let i = 0;
      for (const entityCaseVM of payerEntityVM.entities) {
        if (!this.entity || entityCaseVM.name === this.entity) {
          let filtered = incAll ? entityCaseVM.cases : entityCaseVM.cases.filter(h => {
            return (!this.customer || h.record?.customer === this.customer) &&
              (!this.entity || h.record?.entity === this.entity) &&
              (!this.showBulkReady || (!this.hasZeroValueBenchmark(h.record) && !!h.calculatedNSAOffer))
          });
          entityCaseVM.cases = filtered;
          i += entityCaseVM.cases.length;
        } else {
          entityCaseVM.cases = [];
        }
      }
      payerEntityVM.count = i;
      payerEntityVM.entities = payerEntityVM.entities.filter(g => g.cases.length);
    }
    this.records = payorEntitiesVM.filter(g => g.count);
    this.records$.next(this.records);
    if (incAll || !this.customer)
      return;

    if (updateEntities) {
      for (const p of JSON.parse(JSON.stringify(this.allNSAPayorsVM))) {
        for (const n of p.entities) {
          if (!!(n as EntityCasesVM).cases.find(v => !!v.record && v.record.customer === this.customer)) {
            this.allEntities.find(g => g.name === n.name)!.isEnabled = true;
          }
        }
      }
    }
  }

  clearDeadlineFilter() {
    this.deadlineFilter = undefined;
    this.applyFilters();
  }

  countSelected() {
    return this.getSelectedIDs().length;
  }

  createNotification(data: any, c: NSACaseVM) {
    const n = new Notification();
    n.arbitrationCaseId = c.id;
    n.notificationType = data.templateType;
    this.svcUtil.showLoading = true;

    this.svcData.createNotification(n).subscribe(r => {
      if (r.id > 0) {
        this.allUnsentNotifications.push(r);
        c.isNotificationQueued = true;
        this.svcToast.show(ToastEnum.success, 'Notification queued successfully!');
      }
    },
      err => {
        this.handleServiceErr(err);
        this.svcUtil.showLoading = false;
      },
      () => this.svcUtil.showLoading = false
    );
  }

  deselectAll() {
    // TODO: Could make this better by accepting a "preserveIfPossible" param
    // that checks each claim against the current set of criteria e.g. isFilterMatch(claim)
    for (const p of this.allNSAPayorsVM) {
      for (const j of p.entities) {
        for (const c of j.cases) {
          c.isSelected = false;
        }
      }
    }
  }

  displayPreviewModal(t: DocumentTemplate, c: NSACaseVM, templates: DocumentTemplate[]) {
    const modalRef = this.modalService.open(DocPreviewComponent, this.modalOptions);
    const i = modalRef.componentInstance;
    const n: Notification | undefined = c.isNotificationQueued ? this.getNotification(c.id, this.notifyType) : undefined;
    if (!!n) {
      i.isReviewing = true;
      i.documents = new Array<INotificationDocument>();
      i.documents.push(n);
      if (n.supplements.length) {
        i.documents = i.documents.concat(n.supplements);
      }
    } else {
      i.isReviewing = false;
      i.templates = templates;
      i.templateName = t.name;
      i.templateType = t.notificationType;
      i.NSATrackingObject = c.NSATrackingObject;
    }
    //i.content = c.isNotificationQueued && !!n ? n.html : '';
    i.arbCase = c.record;
    i.currentVars = this.allCalcVars.find(d => d.serviceLine.toLowerCase() === c.record?.serviceLine.toLowerCase());
    i.stateAuth = this.allAuthorities.find(d => d.key.toLowerCase() === c.record?.authority.toLowerCase());

    modalRef.closed.subscribe(data => {
      if (c.isNotificationQueued) {
        this.cancelNotification(i, c);
      } else {
        this.createNotification(i, c);
      }
    });
  }

  getNotification(id: number, notifyType: NotificationType): Notification | undefined {
    // TODO: Replace this cached version of the preview with an on-demand fetch if others are working the same queues
    // OR
    // Fetch all notifications in the background periodically and update the isNotificationQueued and allVMs collections in real time
    return this.allUnsentNotifications.find(n => n.arbitrationCaseId === id && n.notificationType === notifyType);
  }

  getSettlementReduction(c: NSACaseVM) {
    if (this.notifyType === NotificationType.NSANegotiationRequest) {  // can reuse the function for other types of notifications
      if (!c.record || !c.fh50thPercentileExtendedCharges)
        return '0%';
      const calcVars = this.allCalcVars.find(v => v.serviceLine.toLowerCase() === c.record?.serviceLine.toLowerCase());
      const auth = this.allAuthorities.find(d => d.key.toLowerCase() === c.record?.authority.toLowerCase());
      if (auth && calcVars)
        return UtilService.GetCalculatedValue('settlementReduction', this.activeDocTemplate, c.record, calcVars, auth);
    }
    return '0%';
  }

  getAttachmentCount(c: NSACaseVM): number {
    return c.attachments?.length ?? -1;
  }

  getFilteredEntities() {
    return this.allEntities.filter(v => !!v.isEnabled || v.name === this.entity);
  }

  getSelectedIDs() {
    const IDs = new Array<number>();
    for (const p of this.records) {
      for (const j of p.entities) {
        for (const c of j.cases) {
          if (c.isSelected)
            IDs.push(c.id);
        }
      }
    }
    return IDs;
  }

  handleServiceErr(err: any) {
    let msg = '';
    msg = err?.error?.title ?? err?.error ?? err?.message ?? err?.statusText ?? 'Unknown error';
    this.svcToast.showAlert(ToastEnum.danger, msg);
    //this.records$.next([]);
    //this.records = [];
    this.svcUtil.showLoading = false;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  hasZeroValueBenchmark(c: ArbitrationCase | undefined): boolean {
    if (!c || !c.cptCodes.filter(v => v.isEligible).length)
      return false;

    const vars: CalculatorVariables | undefined = this.allCalcVars.find(x => x.serviceLine === c.serviceLine);
    if (!vars) {
      console.warn(`Unable to compute Bulk Claim readiness without app variables for ServiceLine ${c.serviceLine}`);
      return false;
    }

    const bad = c.cptCodes.find(v => v.isEligible && !(v as any)[vars?.nsaOfferBaseValueFieldname]);
    return !bad;
  }

  isAllNSAChecked(e: EntityCasesVM) {
    if (!e.name)
      return false;
    const b = e.cases.find(d => !d.isSelected);
    return !b;
  }

  loadPendingNSA() {
    this.svcUtil.showLoading = true;
    this.activeDocTemplate = NotificationType.NSANegotiationRequest; // determines the context of some on-screen variables
    this.svcData.searchNeedsNSARequest(this.customer, this.deadlineFilter!!).subscribe(data => {
      this.showActions = !!data.length;
      const hidden = data.filter(c => this.allCustomers.map(item => item.name.toLowerCase()).indexOf(c.customer.toLowerCase()) === -1);
      if (hidden.length)
        this.svcToast.showAlert(ToastEnum.danger, 'Excluded ' + hidden.length + ' Cases due to missing NSA reply to Email');

      this.parseNSACases(data.filter(c => this.allCustomers.map(item => item.name.toLowerCase()).indexOf(c.customer.toLowerCase()) > -1));
      if (!!this.customer || !!this.deadlineFilter || this.entity)
        this.applyFilters();
    },
      err => this.handleServiceErr(err),
      () => this.svcUtil.showLoading = false);
  }

  loadPrerequisites() {
    this.svcUtil.showLoading = true;
    const authorities$ = this.svcData.loadAuthorities();
    const benchmarks$ = this.svcData.loadBenchmarkDatasets();
    const calcvars$ = this.svcData.loadCalculatorVariables();
    const payors$ = this.svcData.loadPayors(true, false, false);
    const customers$ = this.svcData.loadCustomers();
    const notifications$ = this.svcData.getUnsentNotifications();

    combineLatest([authorities$, benchmarks$, calcvars$, payors$, customers$, notifications$]).subscribe(
      ([authorities, benchmarks, calcvars, payors, customers, notifications]) => {
        this.allAuthorities = authorities.filter(v => v.key.toLowerCase() !== 'nsa');
        this.allBenchmarkDatasets = benchmarks;
        for (const a of this.allAuthorities) {
          for (const b of a.benchmarks) {
            const bm = this.allBenchmarkDatasets.find(g => g.id === b.benchmarkDatasetId);
            if (bm) {
              b.benchmark = bm;
            }
          }
        }
        this.allCalcVars = calcvars;
        this.allUnsentNotifications = notifications;
        this.allPayors = payors;
        this.allPayors.sort(UtilService.SortByName);
        const bc = customers.filter(d => !d.NSAReplyTo).map(d => d.name);
        if (bc.length)
          this.svcToast.showAlert(ToastEnum.danger, `Cases for these customers are excluded (no NSAReplyTo): ${bc.join(', ')}`);
        customers.filter(d => !!d.NSAReplyTo).forEach(d => this.allCustomers.push({ name: d.name, counter: 0, arbitCasesCount: d.arbitCasesCount })); // weed out Customers w/o NSAReplyTo
        this.allCustomers.sort(UtilService.SortByName);
        const nsa = authorities.find(d => d.key.toLowerCase() === 'nsa');
        if (!nsa) {
          this.svcToast.showAlert(ToastEnum.danger, "Critical Error: NSA authority configuration not found. Contact IT Support immediately!");
          this.router.navigateByUrl('/');
          return;
        }
        this.NSATrackingFieldsForUI = nsa.trackingDetails.filter(d => !d.isHidden);
      },
      err => this.handleServiceErr(err),
      () => this.svcUtil.showLoading = false
    );
  }

  parseNSACases(cases: ArbitrationCase[]): void {
    this.allEntities.length = 0;
    this.allNSAPayorsVM.length = 0;
    this.notifyType = NotificationType.NSANegotiationRequest;
    let vars: CalculatorVariables | undefined;
    const skips = new Array<any>();
    this.allCustomers.forEach(x => x.counter = 0);
    this.allEntities.forEach(x => x.counter = 0);

    cases.map(arbitCase => {
      const nSACaseVM = new NSACaseVM(arbitCase); //Object.assign({}, arbitCase));
      this.setNSATrackingObject(nSACaseVM);

      try {
        if (this.allCustomers.findIndex(x => x.name.trim().toLowerCase() === arbitCase.customer.trim().toLowerCase())) {
          var foundAt = this.allCustomers.findIndex(v => v.name.trim().toLowerCase() === arbitCase.customer.trim().toLowerCase());
          this.allCustomers[foundAt].counter++;
        }
      } catch (ex) {
        console.warn("Customer Name:" + arbitCase.customer + " " + arbitCase.id);
        this.allEntities.forEach(x => console.warn(x.name));
      }

      let payorEntitiesVM = this.allNSAPayorsVM.find(nsaPayer => nsaPayer.name.toLowerCase() === arbitCase.payor.toLowerCase());
      if (!payorEntitiesVM) {
        const payer = this.allPayors.find(payer => payer.name.toLowerCase() === arbitCase.payor.toLowerCase());
        payorEntitiesVM = new PayorEntitiesVM();
        payorEntitiesVM.NSAEmail = payer?.NSARequestEmail || '{missing!}';
        payorEntitiesVM.name = arbitCase.payor;
        this.allNSAPayorsVM.push(payorEntitiesVM);
      }
      let entityCasesVM = payorEntitiesVM.entities.find(g => g.name.toLowerCase() === arbitCase.entity.toLowerCase() && g.NPINumber === arbitCase.entityNPI);
      if (!entityCasesVM) {
        entityCasesVM = new EntityCasesVM();
        entityCasesVM.name = arbitCase.entity;
        entityCasesVM.NPINumber = arbitCase.entityNPI;
        payorEntitiesVM.entities.push(entityCasesVM);

      }
      if (entityCasesVM) {
        // update the list of Entities for filtering
        if (this.allEntities.findIndex(x => x.name === arbitCase.entity) === -1) {
          this.allEntities.push({ isEnabled: true, name: entityCasesVM.name, counter: 1 });
        } else {
          var foundAt = this.allEntities.findIndex(x => x.name === arbitCase.entity);
          this.allEntities[foundAt].counter++;
        }
      }

      vars = this.allCalcVars.find(x => x.serviceLine.toLowerCase() === arbitCase.serviceLine.toLowerCase());
      let nsaOfferDiscount = vars?.nsaOfferDiscount ?? 0;
      this.setDiscount(nSACaseVM, arbitCase, nsaOfferDiscount, vars);
      const z = this.getNotification(arbitCase.id, this.notifyType);
      nSACaseVM.isNotificationQueued = !!z;
      entityCasesVM.cases.push(nSACaseVM);
      payorEntitiesVM.count++;
    });
    //this.allEntities = [...new Set(this.allEntities)];
    this.allEntities.sort(UtilService.SortByName);
    this.sortNSAPayorsVM();
    this.records$.next(this.allNSAPayorsVM);
    this.records = this.allNSAPayorsVM;

  }

  setDiscount(vm: NSACaseVM, ac: ArbitrationCase, nsaOfferDiscount: number, vars: CalculatorVariables | undefined) {
    let discount = ac.NSARequestDiscount !== null && ac.NSARequestDiscount > 0 ? ac.NSARequestDiscount : nsaOfferDiscount;
    // NOTE: This is only appropriate to NSA Open Request negotiation with the Payor and no other negotiation / authority at this time. 10/3/2023 with affect
    vm.calculatedNSAOffer = !!vars ? UtilService.RoundMoney(UtilService.GetCPTValueSum(ac.cptCodes, vars.nsaOfferBaseValueFieldname) * (1 - discount)) : 0;
  }

  sortNSAPayorsVM() {
    this.allNSAPayorsVM.sort(UtilService.SortByName);
    for (const a of this.allNSAPayorsVM) {
      a.entities.sort(UtilService.SortByName);
      for (const t of a.entities) {
        t.cases.sort(UtilService.SortByPatientName);
      }
    }
  }

  openPreviewModal(p: Payor, c: NSACaseVM) {
    const t = p.templates.find(d => d.notificationType === NotificationType.NSANegotiationRequest);
    const others = p.templates.filter(d => d.notificationType === NotificationType.NSANegotiationRequest || d.notificationType === NotificationType.NSANegotiationRequestAttachment);
    if (t)
      this.displayPreviewModal(t, c, others);
    else
      this.svcToast.show(ToastEnum.danger, `Template type 'NSANegotiationRequest' is not available for ${c.payor}`);
  }

  previewNSAMail(c: NSACaseVM) {
    // get latest data
    this.svcUtil.showLoading = true;
    this.svcData.loadNotificationByClaimId(c.id, NotificationType.NSANegotiationRequest).subscribe(data => {
      if (!!data && data.id > 0) {
        const x = this.allUnsentNotifications.find(d => d.id === data.id && !d.isDeleted);
        if (x) {
          this.allUnsentNotifications.splice(this.allUnsentNotifications.indexOf(x), 1, data);
        } else {
          this.svcToast.show(ToastEnum.info, 'Someone else queued up this notification since you last refreshed. Showing preview of queued documents.');
          this.allUnsentNotifications.push(data);
        }
        c.isNotificationQueued = true;
      }
    },
      err => this.handleServiceErr(err),
      () => {
        const p = this.allPayors.find(d => d.name.toLowerCase() === c.payor.toLowerCase());
        if (p?.templates.length === 0) {
          this.svcData.getPayorTemplates(p).subscribe(data => {
            const temp = JSON.parse(data);
            if (!temp.templates || !temp.templates.length) {
              this.svcToast.show(ToastEnum.danger, `Template type 'NSANegotiationRequest' is not available for ${c.payor}`);
              return;
            }
            for (let dt of temp.templates) {
              p.updateTemplate(dt as DocumentTemplate);
            }
            this.openPreviewModal(p, c);
          },
            err => this.handleServiceErr(err),
            () => this.svcUtil.showLoading = false
          );
        } else {
          this.svcUtil.showLoading = false;
          this.openPreviewModal(p!, c);
        }
      });
  }

  markClaimAsQueued(id: number, queued: boolean) {
    for (let p of this.records) {
      for (let e of p.entities) {
        const c = e.cases.find(v => v.id === id);
        if (!!c) {
          c.isNotificationQueued = queued;
          return;
        }
      }
    }
  }

  queueAllSelected(nt: NotificationType) {
    if (!confirm('ARE YOU SURE you want to immediately queue up all selected claims for notification?\n\nYou will not see a preview of the claims first!'))
      return;

    this.svcUtil.showLoading = true;
    const IDs = this.getSelectedIDs();
    const n = IDs.map(v => new Notification({ arbitrationCaseId: v, notificationType: nt }));

    this.svcData.createNotificationBatch(n).subscribe(
      data => {
        // TODO: mark all selected entries with isNotificationQueued = true
        for (let id of IDs) {
          this.markClaimAsQueued(id, true);
        }
        this.svcToast.showAlert(ToastEnum.success, data);
      },
      err => {
        console.log(err);
        this.confirmationMessage = err.status == '422' ? err.error : err.message ?? err;
        this.modalService.open(this.confirmationModal, this.modalOptions);
        this.svcUtil.showLoading = false;
      },
      () => this.svcUtil.showLoading = false);
  }

  rerender(): void {
    if (this.dtElement?.dtInstance) {
      this.dtElement.dtInstance.then((dtInstance: DataTables.Api) => {
        // Destroy the table first
        dtInstance.clear();
        dtInstance.destroy();

        // Call the dtTrigger to render again
        this.dtTrigger.next();
        setTimeout(() => {
          try {
            dtInstance.columns.adjust();
          } catch (err) {
            console.error('Error in rendering again!');
            console.error(err);
          }
        }, 500);
      });
    } else {
      this.dtTrigger.next();
    }
  }

  refreshVM(c: NSACaseVM) {
    const queued$ = this.svcData.loadNotificationByClaimId(c.id, this.notifyType);
    const claim$ = this.svcData.loadCaseById(c.id);
    combineLatest([claim$, queued$]).subscribe(([claim, queued]) => {
      if (!!queued && queued?.status !== 'pending') {
        this.removeCase(c);
        this.svcToast.show(ToastEnum.info, `Notification for Case Id ${c.id} was already delivered.`);
        return;
      }
      if (claim && claim.id) {
        this.updateVM(c, claim, c.isNotificationQueued);
        this.svcChangeDetector.detectChanges();
      }
    });
  }

  removeCase(c: NSACaseVM) {
    for (const p of this.allNSAPayorsVM) {
      for (const b of p.entities) {
        const vm = b.cases.find(d => d.id === c.id);
        if (vm) {
          b.cases.splice(b.cases.indexOf(vm, 1));
          return;
        }
      }
    }
  }

  setNSATrackingObject(c: NSACaseVM) {
    if (c.record?.NSATracking) {
      c.NSATrackingObject = JSON.parse(c.record.NSATracking);
    } else if (this.NSATrackingFieldsForUI.length) {
      c.NSATrackingObject = UtilService.CreateTrackingObject(this.NSATrackingFieldsForUI);
    }
    /* recalculation only messing things up at present
    if (c.NSATrackingObject) {
      c.NSATrackingObject = UtilService.TransformTrackingObject(this.NSATrackingFieldsForUI, c.NSATrackingObject, false);
      UtilService.UpdateTrackingCalculations(this.NSATrackingFieldsForUI, c.NSATrackingObject, true, c.record);
    }
    */
  }

  updateVM(c: NSACaseVM, data: ArbitrationCase, isQueued: boolean = false) {
    // find by id and remove it if payor / entity / NPI are different, if deadline passed, or if the Notification was already sent out (and alert this with a toast)
    let nSACaseVM: NSACaseVM | undefined;
    for (const payerEntityVM of this.allNSAPayorsVM) {
      for (const entityCase of payerEntityVM.entities) {
        nSACaseVM = entityCase.cases.find(d => d.id === data.id);
        if (nSACaseVM) {  // make sure we aren't trying to update an object that's not even in the collection
          const tmp = new NSACaseVM(data); //Object.assign({}, data));
          this.setNSATrackingObject(tmp);
          const strTodaysDate = `${this.today.getMonth() + 1}/${this.today.getDate()}/${this.today.getFullYear()}`;
          const todaysDate = new Date(strTodaysDate);
          todaysDate.setDate(todaysDate.getDate() - 1);
          const isExpired = !!tmp.NegotiationNoticeDeadline && tmp.NegotiationNoticeDeadline < todaysDate;
          if (entityCase.name !== data.entity || entityCase.NPINumber !== data.entityNPI || payerEntityVM.name !== data.payor || isExpired) {
            entityCase.cases.splice(entityCase.cases.indexOf(nSACaseVM), 1); // remove from current hierarchy
            if (!isExpired) {
              const n = this.addNSACaseVM(data); // add to another Entity or Payor group
              n.isNotificationQueued = isQueued;
            }
          } else {
            // update nSACaseVM in place
            NSACaseVM.update(nSACaseVM, data);
            this.setNSATrackingObject(nSACaseVM);
            const calcVars = this.allCalcVars.find(x => x.serviceLine.toLowerCase() === data.serviceLine.toLowerCase());
            let nsaOfferDiscount = calcVars?.nsaOfferDiscount ?? 0;
            this.setDiscount(nSACaseVM, data, nsaOfferDiscount, calcVars); // possibly needs DRYing out with above addNSACaseVM call
          }
        }
      }
      if (nSACaseVM)
        break;
    }

    // update counts and headers
    let i = 0;
    for (const payerEntityVM of this.allNSAPayorsVM) {
      i = payerEntityVM.entities.length;
      payerEntityVM.count = 0;
      for (let x = i - 1; x >= 0; x--) {
        const y = payerEntityVM.entities[x].cases.length;
        if (!y)
          payerEntityVM.entities.splice(x, 1);
        else
          payerEntityVM.count += y;
      }
    }

    this.allNSAPayorsVM = this.allNSAPayorsVM.filter(x => x.count > 0);
    this.sortNSAPayorsVM();
    /*
    this.records$.next(this.allNSAPayorsVM);
    this.records = this.allNSAPayorsVM;
    */
    this.applyFilters();
  }

  sendNSAEmail() {
    this.svcToast.show(ToastEnum.info, 'Not yet available');
  }
  /*
  sortCases(a:NSACaseVM,b:NSACaseVM) {
    if(a.providerName<b.providerName)
      return -1;
    if(a.providerName>b.providerName)
      return 1;
    return 0;
  }
  */
  /* if this is put back into use, look at toggleNSAEntity for hints on keeping cache selections in sync
  toggleAllEntity(e:EntityCasesVM) {
    const t = !this.isAllNSAChecked(e);
    for(const a of e.cases)
      a.isSelected = t;
  }
  */

  findRecordById(id: number) {
    for (const rec of this.records) {
      for (const n of rec.entities) {
        const s = n.cases.find(v => v.id === id);
        if (s)
          return s;
      }
    }
    return;
  }

  toggleNSAEntity(p: PayorEntitiesVM, e: EntityCasesVM) {
    e.isExpanded = !e.isExpanded;
    //e.cases.sort(this.sortCases);
    // update cached object
    const entityCaseVM = this.allNSAPayorsVM.find(d => d.name.toLowerCase() === p.name.toLowerCase())?.entities.find(g => g.name.toLowerCase() === e.name.toLowerCase() && g.NPINumber === e.NPINumber);
    // NOTE: Remember that either Customer or Deadline filter must be selected in order to activate the Files count!
    if (!!entityCaseVM && entityCaseVM !== e) {
      entityCaseVM.isExpanded = !entityCaseVM.isExpanded;
      if (entityCaseVM.cases.length && entityCaseVM.isExpanded && !!this.deadlineFilter) {
        //if(confirm('Load files list for these claims?')) {
        let i = 0;

        let selectedDate = new Date(this.deadlineFilter + ' GMT-0600');
        entityCaseVM.cases.forEach(async nSACaseVM => {

          try {
            if (!!nSACaseVM.NegotiationNoticeDeadline && nSACaseVM.NegotiationNoticeDeadline.toDateString() === selectedDate.toDateString()) {
              i++;
              let data = await this.svcData.getCaseFiles(nSACaseVM.id).toPromise();
              i--;
              if (data.length) {
                const x = this.findRecordById(nSACaseVM.id);
                x!.attachments.length = 0;
                data.forEach(cf => {
                  let caseFileVM = new CaseFileVM(cf.tags);
                  caseFileVM.blobName = cf.blobName;
                  caseFileVM.createdOn = cf.createdOn;
                  x!.attachments.push(caseFileVM);
                });
              }
              if (i === 0)
                this.svcChangeDetector.detectChanges();
              /*
              .subscribe(data => {
                
              },
              err=> {
                console.error(err);
                this.svcToast.show(ToastEnum.danger,`Unable to load the list of file attachments for ${c.payorClaimNumber}`);
              });
              */
            }
          } catch (e) {
            i--;
            if (i === 0)
              this.svcChangeDetector.detectChanges();
            console.error(e);
          }
        });
        //}
      }
    }
  }

  filesListHTML = '';

  showFileList(c: NSACaseVM) {
    const t = `Files List for ${c.id}`;

    if (!c.attachments.length) {
      this.filesListHTML = 'None';
    } else {
      this.filesListHTML = '<ul>';
      c.attachments.forEach(d => this.filesListHTML += `<li>(${d.DocumentType}) ${d.blobName}`);
      this.filesListHTML += '</ul>';
    }
    return t;
    //const tblId = 'tbl_' + c.payor.replaceAll(' ','-') + '_' + c.record!.entity.replaceAll(' ','-');
    //const cell = document.querySelector(`tr[data-record-id="${c.id}"]`);
    //if(!cell)
    //  return;



  }

  toggleNSAPayor(p: PayorEntitiesVM) {
    p.isExpanded = !p.isExpanded;
    // update the cached object too
    const c = this.allNSAPayorsVM.find(d => d.name.toLowerCase() === p.name.toLowerCase());
    if (!!c && c !== p)
      c.isExpanded = !c.isExpanded;
  }

  updateNSACheckCount() {
    this.showSendEmail = false;
    for (const p of this.records) {
      for (const j of p.entities) {
        this.showSendEmail = !!j.cases.find(d => d.isNotificationQueued);
        if (this.showSendEmail)
          return;
      }
    }
  }

  valueSelected(r: any) {
    let value = 0;
    // NOTE: This entire UI component is geared around NSA which is why the following references the calculatedNSAOffer value
    if (!!r.entities && r.entities.length) {
      for (const p of r.entities) {
        const f = (p as EntityCasesVM).cases.filter(d => d.isSelected);
        f.forEach(d => value += d.calculatedNSAOffer);
      }
    } else if (r.cases && r.cases.length) {
      const f = (r.cases as NSACaseVM[]).filter(d => d.isSelected);
      f.forEach(d => value += d.calculatedNSAOffer);
    }
    return value;
  }
}
