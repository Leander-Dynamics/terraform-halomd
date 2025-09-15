import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, Subject, combineLatest } from 'rxjs';
import { AuthorityDisputeVM } from 'src/app/model/authority-dispute';
import { CaseSettlement } from 'src/app/model/case-settlement';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';
import { AddSettlementComponent } from '../add-settlement/add-settlement.component';
import { Payor } from 'src/app/model/payor';
import { CMSCaseStatus } from 'src/app/model/arbitration-status-enum';

@Component({
  selector: 'list-settlements',
  templateUrl: './list-settlements.component.html',
  styleUrls: ['./list-settlements.component.css']
})
export class ListSettlementsComponent implements OnDestroy, OnInit {
  @Input()
  addSettlementHelpText = 'Requires 1+ Allowed CPTs, an Authority Case # and no unsaved changes.';
  @Input()
  canAddFormal = false;
  @Input()
  currentDispute = new AuthorityDisputeVM();
  @Input()
  hideSettlement = false;
  @Input()
  isManager = false;
  @Input()
  isNegotiator = false;
  @Input()
  showAddFormal = false;
  @Output()
  onSettlementAdded = new EventEmitter<CaseSettlement>();
  @Output()
  onOpenSettlement = new EventEmitter<CaseSettlement>();

  caseSettlements$ = new BehaviorSubject<Array<CaseSettlement>>([]);
  currentPayor:Payor|undefined;
  destroyed$ = new Subject();
  isLoading = false;
  modalOptions: NgbModalOptions | undefined;
  
  constructor(private svcData: CaseDataService,
    private svcToast: ToastService, 
    private svcModal: NgbModal,
    private svcUtil: UtilService) {

      this.modalOptions = {
        backdrop: 'static',
        backdropClass: 'customBackdrop',
        keyboard: false,
        size: 'lg'
      };
  }

  ngOnInit(): void {
    if(!this.currentDispute||!this.currentDispute.cptViewmodels.length){
      console.warn('ListSettlementsComopnent not properly initialzed.');
      return;
    }
    this.loadPrerequisites();
  }

  ngOnDestroy() {
    this.caseSettlements$.complete();
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  addFormalSettlement() {
    //this.onAddSettlement.emit();
    if(!this.currentDispute.authority)
      return;
    const dialog = this.svcModal.open(AddSettlementComponent,this.modalOptions);
    dialog.componentInstance.currentPayor = this.currentPayor;
    dialog.componentInstance.currentDispute = this.currentDispute;
    dialog.componentInstance.isFormal = true;
    dialog.result.then(data => {
      console.log(data);
      this.currentDispute.workflowStatus = CMSCaseStatus.SettledArbitrationPendingPayment;
      this.currentDispute.authorityStatus = 'Arbitration Complete';
      
    },
    reason => console.log('AddSettlementDialog cancelled')
    );
  }

  getSum(s:string){
    return 0;
  }

  loadPrerequisites() {
    this.isLoading = true;
    const payor$ = this.svcData.loadPayorById(this.currentDispute.cptViewmodels[0].payorId);
    combineLatest([payor$]).subscribe(([payor]) => {
      this.currentPayor = payor;
    },
    err => {
      UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast);
      this.isLoading = false;
    },
    () => this.isLoading = false
    );
  }

  openSettlementDialog(g:CaseSettlement){
    this.onOpenSettlement.emit(g);
  }
}
