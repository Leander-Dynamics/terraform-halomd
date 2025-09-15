import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NgbActiveModal, NgbDateAdapter, NgbDateParserFormatter, NgbInputDatepickerConfig } from '@ng-bootstrap/ng-bootstrap';
import { Subject, combineLatest } from 'rxjs';
import { Authority } from 'src/app/model/authority';
import { AuthorityDisputeVM } from 'src/app/model/authority-dispute';
import { CaseSettlement } from 'src/app/model/case-settlement';
import { CaseSettlementCPT, CaseSettlementCPTVM } from 'src/app/model/case-settlement-cpt';
import { CustomDateParserFormatter, CustomNgbDateAdapter } from 'src/app/model/custom-date-handler';
import { IKeyId } from 'src/app/model/iname';
import { Payor } from 'src/app/model/payor';
import { ProcedureCode } from 'src/app/model/procedure-code';
import { ToastEnum } from 'src/app/model/toast-enum';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'add-settlement',
  templateUrl: './add-settlement.component.html',
  styleUrls: ['./add-settlement.component.css'],
  providers: [
    { provide: NgbDateAdapter, useClass: CustomNgbDateAdapter },
    { provide: NgbDateParserFormatter, useClass: CustomDateParserFormatter },
    NgbInputDatepickerConfig
  ]
})
export class AddSettlementComponent implements OnDestroy, OnInit {
  allCPTDescriptions:Array<ProcedureCode> = [];
  allCPTViewmodels = new Array<CaseSettlementCPTVM>();
  allParties = ['Health Plan','Provider'];  
  canEdit = true;
  currentAuthority:Authority|undefined;
  currentDispute = new AuthorityDisputeVM();
  currentPayor:Payor | undefined;
  currentSettlement:CaseSettlement = new CaseSettlement(); // viewmodel - supplies methods for date pickers
  destroyed$ = new Subject<void>();
  isFormal = false;
  isManager = false;
  isNSA = false;
  isState = false;
  totalSettlementAmount = 0;

  constructor(public activeModal: NgbActiveModal, 
              public svcUtil:UtilService, 
              public svcData:CaseDataService, 
              public svcToast: ToastService) { 
  }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
  }

  ngOnInit(): void {
    if(!this.currentDispute||!this.currentDispute.authorityCaseId||!this.currentDispute.authority||!this.currentPayor||!this.currentDispute.cptViewmodels.length){
      this.svcToast.show(ToastEnum.danger,'Settlement component missing key initialization value(s)!');
      this.activeModal.dismiss();
    }

    this.currentAuthority = this.currentDispute.authority;
    this.currentSettlement = new CaseSettlement(); // in case the modal opener tries to pass in an existing one!
    this.currentSettlement.authorityCaseId = this.currentDispute.authorityCaseId;
    
    this.loadPrerequisites();
  }

  awardAmountChanged(c:CaseSettlementCPTVM){
    const code=c.cptCode.toLowerCase();
    // sync perUnit settlement value across matching cpt codes
    for(let a of this.allCPTViewmodels.filter(v=>v.claimCPTId!==c.claimCPTId&&v.cptCode.toLowerCase()===code)){
      a.perUnitAwardAmount = c.perUnitAwardAmount;
    }
    this.calculateTotalSettlementAmount();
  }

  calculateTotalSettlementAmount() {
    this.totalSettlementAmount=0;
    this.allCPTViewmodels.map(v=>this.totalSettlementAmount+=(v.units*v.perUnitAwardAmount));
  }

  currencyBlur(e:any) {
    UtilService.FixTo2Digits(e.target);
  }

  getSettlementsFromViewmodels():CaseSettlement[]{
    const settlements = new Array<CaseSettlement>();
    this.allCPTViewmodels.sort(UtilService.SortByArbitrationCaseId);
    let s = new CaseSettlement();

    for(let c of this.allCPTViewmodels)
    {
      if(c.arbitrationCaseId !== s.arbitrationCaseId){
        let gross = 0;
        this.allCPTViewmodels.filter(b=>b.arbitrationCaseId===c.arbitrationCaseId).map(v=>gross+=(v.units*v.perUnitAwardAmount));
        s = new CaseSettlement({
          arbitrationCaseId: c.arbitrationCaseId,
          authorityId: this.currentAuthority?.id,
          authorityCaseId: this.currentDispute.authorityCaseId,
          arbitrationDecisionDate: this.currentSettlement.arbitrationDecisionDate,
          arbitratorReportSubmissionDate: this.currentSettlement.arbitratorReportSubmissionDate,
          grossSettlementAmount: gross,
          JSON: '{}',
          notes: this.currentSettlement.notes,
          partiesAwardNotificationDate: this.currentSettlement.partiesAwardNotificationDate,
          payorClaimNumber: c.payorClaimNumber,
          payorId: this.currentPayor!.parentId,
          prevailingParty: this.currentSettlement.prevailingParty,
          reasonableAmount: 0,
          totalSettlementAmount: gross
        });
        settlements.push(s);
      }
      s.caseSettlementCPTs.push(new CaseSettlementCPT(c));
    }

    return settlements;
  }

  handleGridNav(event:any){
    UtilService.HandleGridNav(event);
  }

  handleGridNavNumeric(event:any) {
    UtilService.HandleGridNavNumeric(event);
  }

  hasInvalidCPTSelection() {
    return !!this.allCPTViewmodels.find(v=>!v.perUnitAwardAmount);
  }

  loadPrerequisites(){
    this.svcUtil.showLoading = true;
    const descriptions$ =this.svcData.getCPTDescriptionsForDispute(this.currentDispute.id);
    
    combineLatest([descriptions$]).subscribe(([descriptions]) => {
      // Make a UNIQUE set of CPT viewmodels to power this dialog
      // 1. An AuthorityDispute can consist of duplicate CPTs spanning multiple Claims due to batching
      // 2. Add the descriptions to the codes for display purposes
      // 3. Easy to cancel all changes by just dismissing everything from these copies

      this.allCPTDescriptions = descriptions;

      for(let p of this.currentDispute.cptViewmodels.filter(x=>!!x.claimCPT)) {
        const d = this.allCPTDescriptions.find(v=>v.code.toLowerCase()===p.claimCPT!.cptCode.toLowerCase());
        const cpt = new CaseSettlementCPTVM({
          caseSettlementId: 0, 
          arbitrationCaseId: p.claimCPT?.arbitrationCaseId,
          claimCPTId: p.claimCPT!.id,
          cptCode: p.claimCPT!.cptCode,
          description: d?.description ?? 'N/A',
          id: 0,
          payorClaimNumber: p.payorClaimNumber,
          perUnitAwardAmount: 0,
          units: p.claimCPT!.units
        });
        
        this.allCPTViewmodels.push(cpt);
      }
      this.calculateTotalSettlementAmount();
    },
    err => {
      UtilService.HandleServiceErr(err, this.svcUtil,this.svcToast);
      this.activeModal.dismiss(err);
    },
    () => this.svcUtil.showLoading = false
    );
  }

  saveChanges() {
    if(!confirm('This will save your changes immediately. Continue?'))
      return;

    const settlements = this.getSettlementsFromViewmodels();
    console.log(settlements);
    
    

    this.svcUtil.showLoading = true;
    this.svcData.createMultiSettlement(settlements).subscribe(data => {
      const newSettlements = data;
      this.svcToast.show(ToastEnum.success, 'Dispute Settlement(s) saved successfully!');
      this.activeModal.close(newSettlements);
    },
    err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast, false),
    () => {
      this.svcUtil.showLoading = false;  
    });
    
  }

  selectAll(e: any) {
    e?.target?.select();
  }
}
