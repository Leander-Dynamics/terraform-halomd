import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { CaseSettlement } from 'src/app/model/case-settlement';
import { NgbActiveModal, NgbDate, NgbDateAdapter, NgbDateParserFormatter, NgbInputDatepickerConfig } from '@ng-bootstrap/ng-bootstrap';
import { ArbitrationCase } from 'src/app/model/arbitration-case';
import { Authority } from 'src/app/model/authority';
import { UtilService } from 'src/app/services/util.service';
import { CustomDateParserFormatter, CustomNgbDateAdapter } from 'src/app/model/custom-date-handler';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { ToastEnum } from 'src/app/model/toast-enum';
import { VMArbitrationCPT } from 'src/app/model/vm-arbitration-cpt';
import { CaseSettlementCPT } from 'src/app/model/case-settlement-cpt';
import { IKeyId } from 'src/app/model/iname';
import { ProcedureCode } from 'src/app/model/procedure-code';
import { Payor } from 'src/app/model/payor';

@Component({
  selector: 'app-settlement-dialog',
  templateUrl: './settlement-dialog.component.html',
  styleUrls: ['./settlement-dialog.component.css'],
  providers: [
    { provide: NgbDateAdapter, useClass: CustomNgbDateAdapter },
    { provide: NgbDateParserFormatter, useClass: CustomDateParserFormatter },
    NgbInputDatepickerConfig
  ]
})
export class SettlementDialogComponent implements OnInit {
  @ViewChild('caseID', { static: false })
  caseIDControl: ElementRef | undefined;

  allAuthorities:Authority[] = [];
  allAuthorityIDs:Array<IKeyId> = [{id:0, key:'Informal'}];
  allClaimCPTs:VMArbitrationCPT[] = [];
  allCPTDescriptions:Array<ProcedureCode> = [];
  allParties = ['Health Plan','Informal','Provider'];
  /*
  <option value="Health Plan">Health Plan</option>
                <option value="Informal">Informal</option>
                <option value="Provider">Provider</option>
  */
  settlement:CaseSettlement = new CaseSettlement();
  arbCase:ArbitrationCase = new ArbitrationCase();
  authorityChoices:Authority[] = [];
  isFormal = false;
  userIsManager = false;
  userIsNSA = false;
  userIsState = false;
  arbitrationDecisionDate: string | undefined;
  arbitratorReportSubmissionDate: string | undefined;
  partiesAwardNotificationDate: string | undefined;
  currentPayor:Payor | undefined;
  NSAAuthority:Authority | undefined;

  constructor(public activeModal: NgbActiveModal, public svcUtil:UtilService, 
              public svcData:CaseDataService, public svcToast: ToastService) { }

  ngOnInit(): void {
    if(!this.arbCase.id || !this.settlement.arbitrationCaseId || this.arbCase.id!==this.settlement.arbitrationCaseId || !this.settlement.payorId){
      this.svcToast.showAlert(ToastEnum.danger,'The Settlement and the ArbitrationCase objects are out of sync. Please contact support!');
      this.activeModal.dismiss();
    }
    if(!this.currentPayor){
      this.svcToast.showAlert(ToastEnum.danger,`Unable to locate a Payor with ID ${this.settlement.payorId}. Please contact support!`);
      this.activeModal.dismiss();
    }

    // update calculations
    if(this.settlement.grossSettlementAmount) {
      this.settlement.netSettlementAmount = this.settlement.grossSettlementAmount - this.arbCase.totalPaidAmount;
    }
    
    // limit the authority choice to what makes sense
    if(this.userIsState && this.allAuthorities.length) {
      const a = this.allAuthorities.find(v => v.key.toLowerCase() ===this.arbCase.authority.toLowerCase());
      if(!!a){
        this.authorityChoices.push(a);
        if(!!this.arbCase.authorityCaseId) {
          this.allAuthorityIDs.push({id:a.id, key:this.arbCase.authorityCaseId});
        }
      }
      // maybe we're editing an old CaseSettlement from an Authority that is no longer the claim's active authority...
      if((this.settlement.authorityId ?? 0) > 0 && (!a || a.id!==this.settlement.authorityId)){
        const z = this.allAuthorities.find(v => v.id === this.settlement.authorityId);
        if(!!z) {
          this.authorityChoices.push(z);
          if(this.arbCase.authorityCaseId) {
            this.allAuthorityIDs.push({id:z.id, key:this.arbCase.authorityCaseId});
          } 
        }
      }
    }

    // add the NSA authority if provided
    if(this.userIsState && !!this.NSAAuthority) {
      this.authorityChoices.push(this.NSAAuthority);
      if(this.arbCase.NSACaseId) {
        this.allAuthorityIDs.push({id:this.NSAAuthority.id, key:this.arbCase.NSACaseId});
      }
    }

    this.authorityChoices.sort(UtilService.SortByName);

    if(this.isFormal) {
        this.allParties.splice(1,1);  // remove the Informal choice
    } else if(!!this.settlement.offer) {
      // apply defaults for new Offers and do some sanity validation
      if(this.settlement.id===0) {
        if(!!this.settlement.offer.caseSettlementId){
          this.svcToast.showAlert(ToastEnum.danger,`The selected OfferHistory record (${this.settlement.offer.id}) is already attached to a different CaseSettlement record ${this.settlement.offer.caseSettlementId}! Please contact support for assistance.`);
          this.activeModal.dismiss();
        }
        this.settlement.grossSettlementAmount = this.settlement.offer.offerAmount;
      } else if(this.settlement.id !== this.settlement.offer.caseSettlementId) {
          this.svcToast.showAlert(ToastEnum.danger,`The selected Offer does not have a matching Settlement Id! Please contact support!`);
          this.activeModal.dismiss();
      }
    } 

    this.arbitrationDecisionDate = this.settlement.arbitrationDecisionDate ? this.settlement.arbitrationDecisionDate.toLocaleDateString() : undefined;
    this.partiesAwardNotificationDate = this.settlement.partiesAwardNotificationDate ? this.settlement.partiesAwardNotificationDate.toLocaleDateString() : undefined;
    this.arbitratorReportSubmissionDate = this.settlement.arbitratorReportSubmissionDate ? this.settlement.arbitratorReportSubmissionDate.toLocaleDateString() : undefined;

    // add CPT VMs for selection
    for(let p of this.arbCase.cptCodes.filter(v=>!v.isDeleted&&v.id>0&&v.isIncluded)) {
      const vm = new VMArbitrationCPT(p);
      const d = this.allCPTDescriptions.find(v=>v.code.toLowerCase()===p.cptCode.toLowerCase());
      vm.description = d?.description ?? 'N/A';
      vm.isSettled = this.isCodeInSettlement(vm);
      this.allClaimCPTs.push(vm);
    }
    
  }

  authorityChange() {
    if(this.settlement.authorityId===0) 
      return;
    
    const c = this.allAuthorityIDs.find(v=>v.id===this.settlement.authorityId);
    if(!c) {
      this.settlement.authorityCaseId='';
      return;
    }
    const a = this.authorityChoices.find(v=>v.id===this.settlement.authorityId);
    if(confirm(`The active Case ID for ${a!.name} is ${c.key}. Do you want to use it?`))
      this.settlement.authorityCaseId = c.key;
    
    setTimeout(()=> this.caseIDControl?.nativeElement.focus(), 200);
  }

  grossSettlementAmountChange() {
    this.settlement.netSettlementAmount = this.settlement.grossSettlementAmount - this.arbCase.totalPaidAmount;
  }

  hasCPTSelection() {
    return !!this.settlement.caseSettlementCPTs.find(v => !v.isDeleted);
  }

  isCodeInSettlement(c:VMArbitrationCPT) {
    return !!this.settlement.caseSettlementCPTs.find(v=>!v.isDeleted&&v.claimCPTId===c.id);
  }

  saveChanges() {
    const hasOffer = (!!this.settlement.offer&&this.settlement.offer.id>0);
    const xtra = hasOffer?' and related Offer':'';
    if(!confirm(`Save changes to this Settlement${xtra}? (This cannot be undone!)`))
      return;

    if(hasOffer)
      this.settlement.offer!.wasOfferAccepted = true;

    const obs$ = this.settlement.id === 0 ? this.svcData.createSettlement(this.settlement) : this.svcData.updateSettlement(this.settlement);

    this.svcUtil.showLoading = true;
    obs$.subscribe(data => {
      this.settlement = data;
      this.svcToast.show(ToastEnum.success, 'Case Settlement saved successfully!');
    },
    err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast, false),
    () => {
      this.svcUtil.showLoading = false;  
      this.activeModal.close(this.settlement);
    });
  }

  selectAll(e: any) {
    e?.target?.select();
  }

  setAwardDate(e: NgbDate) {
    this.settlement.partiesAwardNotificationDate = UtilService.GetCaseDate(e);
  }

  setDecisionDate(e: NgbDate) {
    this.settlement.arbitrationDecisionDate = UtilService.GetCaseDate(e);
  }

  setSubmissionDate(e: NgbDate) {
    this.settlement.arbitratorReportSubmissionDate = UtilService.GetCaseDate(e);
  }

  toggleSettledCPT(p:VMArbitrationCPT) {
    const t = this.settlement.caseSettlementCPTs.find(v => v.claimCPTId === p.id);
    if(p.isSettled) {
      if(t) {
        t.isDeleted = false;
      } else {
        const n = new CaseSettlementCPT({caseSettlementId: this.settlement.id, claimCPTId: p.id});
        this.settlement.caseSettlementCPTs.push(n);
      }
    } else if(!!t) {
        t.isDeleted = true;
    }
  }
}
