import { Component, OnInit, Sanitizer } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { combineLatest } from 'rxjs';
import { ArbitrationCase } from 'src/app/model/arbitration-case';
import { Authority } from 'src/app/model/authority';
import { AuthorityTrackingDetail } from 'src/app/model/authority-tracking-detail';
import { BenchmarkDataset } from 'src/app/model/benchmark-dataset';
import { CalculatorVariables } from 'src/app/model/calculator-variables';
import { CaseDocumentType } from 'src/app/model/case-document-type-enum';
import { CaseFileVM } from 'src/app/model/case-file';
import { DocumentTemplate } from 'src/app/model/document-template';
import { INotificationDocument } from 'src/app/model/notification-document';
import { NotificationType } from 'src/app/model/notification-type-enum';
import { ProcedureCode } from 'src/app/model/procedure-code';
import { ToastEnum } from 'src/app/model/toast-enum';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';


@Component({
  selector: 'app-doc-preview',
  templateUrl: './doc-preview.component.html',
  styleUrls: ['./doc-preview.component.css']
})
export class DocPreviewComponent implements OnInit {
  allAuthorities:Authority[] = [];
  allBenchmarkDatasets: BenchmarkDataset[] = [];
  allCalcVars:CalculatorVariables[] = [];
  allCaseFileVMs = new Array<CaseFileVM>();
  arbCase = new ArbitrationCase();
  content = '';
  currentItem: DocumentTemplate | INotificationDocument | undefined;
  currentVars:CalculatorVariables|undefined;
  documents = new Array<INotificationDocument>();
  isReviewing = false;
  items = new Array<string>();
  nsaAuth: Authority|undefined;
  NSATrackingObject:any = {};
  previewType:NotificationType = NotificationType.NSANegotiationRequest;
  stateAuth: Authority|undefined;
  trackingFieldsForUI = new Array<AuthorityTrackingDetail>(); 
  templates = new Array<DocumentTemplate>();
  templateName = '';
  templateType = NotificationType.Unknown;

  get html() {
    return this.sanitizer.bypassSecurityTrustHtml(this.content);
  }

  constructor(private svcData:CaseDataService,
              private svcUtil:UtilService, 
              public activeModal: NgbActiveModal, 
              private sanitizer:DomSanitizer, 
              private router: Router,
              private svcToast:ToastService) { }

  ngOnInit(): void {
    this.loadPrerequisites();
  }

  loadPrerequisites() {
    if(this.arbCase.id < 1)
      return;
    if(!this.templates.length&&!this.documents.length)
      return;

    const dt:string = this.previewType===NotificationType.NSANegotiationRequest ? CaseDocumentType[CaseDocumentType.NSARequestAttachment]:'';

    // set up for browsing pre-rendered content
    if(this.documents.length) { 
      this.currentItem = this.documents[0];
      this.content = this.currentItem.html;
      this.items = this.documents.map(d=>d.name);
      this.svcData.getCaseFiles(this.arbCase.id, dt).subscribe(files=> {
        
        files.forEach(cf => {
          const vm = new CaseFileVM(cf.tags);
          vm.blobName = cf.blobName;
          vm.createdOn = cf.createdOn;
          this.allCaseFileVMs.push(vm);
        });
      },
      err => this.handleServiceErr(err),
      () => {
        this.svcUtil.showLoading = false;
         // fetch any payor files, which come from blob store, asynchronously so we don't hold up things
         if(!!this.arbCase.payorId) {
          this.svcData.loadPayorFiles(this.arbCase.payorId, dt).subscribe(pf => {
            pf.forEach(cf => {
              const vm = new CaseFileVM(cf.tags);
              vm.blobName = cf.blobName;
              vm.DocumentType = 'payor';
              vm.createdOn = cf.createdOn;
              this.allCaseFileVMs.push(vm);
            });
          },
          err => console.warn('Could not fetch list of Payor attachments:',err),
          );
        }
      });
      return;
    }

    // set up for rendering previews using templates
    this.currentItem = this.templates[0]; //.find(d=>d.name===this.templateName&&d.notificationType===this.templateType);
    this.items = this.templates.map(d=>d.name);
    this.allCaseFileVMs.length = 0;

    this.svcUtil.showLoading = true;
    const authorities$ = this.svcData.loadAuthorities();
    const benchmarks$ = this.svcData.loadBenchmarkDatasets();
    const calcvars$ = this.svcData.loadCalculatorVariables();
    const cpts$ = this.svcData.getCPTDescriptions(this.arbCase.id);
    const files$ = this.svcData.getCaseFiles(this.arbCase.id, dt);
    const customer$ = this.svcData.loadCustomerByName(this.arbCase.customer);
      
    combineLatest([authorities$,benchmarks$,calcvars$,cpts$,files$,customer$]).subscribe(
      ([authorities,benchmarks,calcvars,cpts,files,customer]) => {
        this.allAuthorities = authorities.filter(v=>v.key.toLowerCase()!=='nsa');
        this.allBenchmarkDatasets = benchmarks;

        // load up the benchmarks that each authority has access to
        for (const a of this.allAuthorities){
          for (const b of a.benchmarks){
            const bm = this.allBenchmarkDatasets.find(g => g.id === b.benchmarkDatasetId);
            if(bm) {
              b.benchmark = bm;
            }
          }
        }
        
        this.nsaAuth = authorities.find(d=>d.key.toLowerCase()==='nsa');
        this.stateAuth = authorities.find(d=>d.key.toLowerCase()===this.arbCase.authority.toLowerCase());
        this.allCalcVars = calcvars;
        this.currentVars = this.allCalcVars.find(v => v.serviceLine.toLowerCase() === this.arbCase.serviceLine.toLowerCase());
        
        files.forEach(cf => {
          const vm = new CaseFileVM(cf.tags);
          vm.blobName = cf.blobName;
          vm.createdOn = cf.createdOn;
          this.allCaseFileVMs.push(vm);
        });

        // fetch any payor files, which come from blob store, asynchronously so we don't hold up things
        if(!!this.arbCase.payorId) {
          this.svcData.loadPayorFiles(this.arbCase.payorId,dt).subscribe(pf => {
            pf.forEach(cf => {
              const vm = new CaseFileVM(cf.tags);
              vm.blobName = cf.blobName;
              vm.DocumentType = 'payor';
              vm.createdOn = cf.createdOn;
              this.allCaseFileVMs.push(vm);
            });
          },
          err => console.warn('Could not fetch list of Payor attachments:',err),
          );
        }

        if(this.currentItem instanceof DocumentTemplate && this.currentVars && this.stateAuth){
          
          // remove the CPTs that are not marked for inclusion
          this.arbCase.cptCodes.forEach(d => {
            if(!d.isIncluded){
              const ndx = cpts.findIndex(j=>j.code === d.cptCode);
              if(ndx>-1)
                cpts.splice(ndx,1);
            }
          });
          this.arbCase.cptCodes = this.arbCase.cptCodes.filter(d=>d.isIncluded);
          
          this.updateCPTs(this.arbCase, cpts, this.currentVars);
          let obj = this.arbCase as any;
          obj['Customer_$_nsaReplyTo'] = customer?.NSAReplyTo;
          this.content = UtilService.MergeTemplateData(this.currentItem.html, this.currentItem.notificationType, obj, this.currentVars, this.stateAuth);
        } else {
          const msg ='Could not locate the referenced template or all required objects to generate a preview. Verify Calculator Variables and Authority.';
          console.error(msg);
          this.svcToast.showAlert(ToastEnum.danger,msg);
        }
      },
      err => this.handleServiceErr(err),
      () => this.svcUtil.showLoading = false
    );
  }

  confirmCancellation() {
    if(!confirm('ARE YOU SURE you want to Cancel The Notification? (This action will be logged.)'))
      return;
    this.activeModal.close();
  }
  
  confirmDelivery() {
    if(!confirm('Queue this notification for automated delivery?'))
      return;
    this.activeModal.close();
  }

  dismiss() {
    if(this.activeModal)
      this.activeModal.dismiss();
    else
      this.router.navigateByUrl('/');
  }

  getBlobName(n:string) {
    const p = n.split('-');
    if(p.length>2){
      p.splice(0,1); // by convention, the name has two prefixed pieces of metadata 
      p.splice(0,1);
    }
    return p.join('-');
  }

  handleServiceErr(err:any) {
    let msg = '';
    msg = err?.error?.title ?? err?.error ?? err?.message ?? err?.statusText ?? 'Unknown error';
    this.svcToast.showAlert(ToastEnum.danger, msg);
    this.svcUtil.showLoading = false;
    window.scrollTo({top:0,behavior:'smooth'});
  }

  switchTemplates(name:string) {
    this.content = '';
    if(this.templates.length){
      this.currentItem = this.templates.find(d=>d.name.toLowerCase()===name.toLowerCase());
      if(!this.currentItem || !this.currentItem.html || !this.currentVars || !this.stateAuth)
        return;
      this.content = UtilService.MergeTemplateData(this.currentItem.html, this.templateType, this.arbCase, this.currentVars, this.stateAuth);     
    } else {
      this.currentItem = this.documents.find(d=>d.name.toLowerCase()===name.toLowerCase());
      this.content = this.currentItem?.html ?? '';
    }
  }

  updateCPTs(c:ArbitrationCase, cpts:ProcedureCode[], calcVars:CalculatorVariables) {
    if(!c.cptCodes.length)
      return;
    let discount = (c.NSARequestDiscount !== null && c.NSARequestDiscount > 0 && c.NSARequestDiscount < .99) ? 1-c.NSARequestDiscount : 1-(calcVars?.nsaOfferDiscount ?? 0);
    for(let m of c.cptCodes) {
      (m as any)['calculatedNSAOffer'] = UtilService.GetCalculatedValue('calculatedNSAOffer', NotificationType.NSANegotiationRequest, c, calcVars, this.stateAuth, m); //UtilService.DecimalPipe.transform((m.fh80thPercentileExtendedCharges * discount),'1.2-2');
      (m as any)['description'] = cpts.find(d=>d.code===m.cptCode)?.description;
    }
  }
}
