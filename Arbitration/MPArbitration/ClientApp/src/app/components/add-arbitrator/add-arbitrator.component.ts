import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { NgbActiveModal, NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, Subject } from 'rxjs';
import { Arbitrator } from 'src/app/model/arbitrator';
import { ArbitratorType } from 'src/app/model/arbitrator-type-enum';
import { BaseFee } from 'src/app/model/base-fee';
import { IKeyId } from 'src/app/model/iname'; 
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';
import { AddFeeComponent } from '../add-fee/add-fee.component';
import { ArbitratorFee } from 'src/app/model/arbitrator-fee';
import { ToastEnum } from 'src/app/model/toast-enum';

@Component({
  selector: 'add-arbitrator',
  templateUrl: './add-arbitrator.component.html',
  styleUrls: ['./add-arbitrator.component.css']
})
export class AddArbitratorComponent implements OnInit {
  @Input()
  canAddFees = false;
  @Input()
  canEditFees = false;
  @Input()
  feeType = '';
  @Input() 
  isEditing = false;

  @Output()
  onAddFee = new EventEmitter<void>();
  @Output()
  onEditFee = new EventEmitter<number>();

  allArbTypes = new Array<IKeyId>();
  allFees$ = new BehaviorSubject<BaseFee[]>([]);
  allServices = new Array<string>();
  destroyed$ = new Subject<void>();
  modalOptions:NgbModalOptions | undefined;
  currentArb = new Arbitrator();
  selectedServices = new Array<string>();
  wasFeeChanged = false;

  constructor(public activeModal: NgbActiveModal,
              private svcData:CaseDataService, 
              private svcToast:ToastService,
              private svcUtil:UtilService,
              private svcModal: NgbModal) {

    this.modalOptions = {
      backdrop:'static',
      backdropClass:'customBackdrop',
      keyboard: false,
    };
    this.allArbTypes = Object.values(ArbitratorType).filter(value => typeof value === 'string').map(key => { 
      const result = (key as string).split(/(?=[A-Z][a-z])/);
      return {id: (<any>ArbitratorType)[key] as number, key: result.join(' ') }; 
    });
   }

  ngOnInit(): void {
    this.allFees$.next(this.currentArb.fees);
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
    this.allFees$.complete();
  }

  addFee(e:any){
    if(!this.currentArb)
      return;

    const ref = this.svcModal.open(AddFeeComponent, {...this.modalOptions,size:'md'});
    ref.componentInstance.feeType = 'Arbitrator';
    ref.result.then(data => {
      this.svcUtil.showLoading = true;
      const fee = new ArbitratorFee(data);
      fee.arbitratorId = this.currentArb.id;
      this.svcData.createArbitratorFee(this.currentArb, fee).subscribe(data => {
        this.currentArb.fees.push(data);
        this.allFees$.next(this.currentArb.fees);
        this.wasFeeChanged = true;
      },
      err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
      () => this.svcUtil.showLoading = false);
    },
    reason => this.svcToast.show(ToastEnum.info, 'Add Fee canceled'));
  }

  
  editFee(e:any) {
    if(!this.currentArb||isNaN(e))
      return;

    const f = this.currentArb.fees.find(v => v.id===e);
    if(!f)
      return;
    const ref = this.svcModal.open(AddFeeComponent, {...this.modalOptions,size:'md'});
    ref.componentInstance.fee = f as BaseFee;
    ref.componentInstance.feeType = 'Arbitrator';
    ref.result.then(data => {
      this.svcUtil.showLoading = true;
      this.svcData.updateArbitratorFee(this.currentArb, f).subscribe(data => {
        const n = this.currentArb!.fees.findIndex(v => v.id===data.id);
        this.currentArb!.fees.splice(n,1,data);
        this.allFees$.next(this.currentArb.fees);
        this.wasFeeChanged = true;
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger,UtilService.ExtractMessageFromErr(err));
      },
      () => this.svcUtil.showLoading = false);
    },
    reason => this.svcToast.show(ToastEnum.info, 'Add Fee canceled'));
  }

  selectAll(e:any) {
    e?.target?.select();
  }

  toggleService(e:any) {
    const i = this.selectedServices.indexOf(e.target.value);
    if(i>=0)
      this.selectedServices.splice(i,1);
    if(e.target.checked)
      this.selectedServices.push(e.target.value);
    this.currentArb.eliminateForServices = this.selectedServices.join(';');
  }

}
