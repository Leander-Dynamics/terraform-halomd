import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';
import { BaseFee } from 'src/app/model/base-fee';
import { DeadlineType } from 'src/app/model/deadline-type-enum';
import { FeeType } from 'src/app/model/fee-type-enum';

@Component({
  selector: 'list-fees',
  templateUrl: './list-fees.component.html',
  styleUrls: ['./list-fees.component.css']
})
export class ListFeesComponent implements OnInit {
  @Input()
  allFees$ = new BehaviorSubject<BaseFee[]>([]);
  @Input()
  canAdd = false;
  @Input()
  canEdit = false;
  @Input()
  feeType = '';

  @Output()
  onAddFee = new EventEmitter<void>();

  @Output()
  onEditFee = new EventEmitter<number>();

  destroyed$ = new Subject<void>();
  DeadlineType = DeadlineType;
  FeeType = FeeType;
  hideFees = true;

  constructor() { }

  ngOnInit(): void {
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
    this.allFees$.complete();
  }

  addFee() {
    this.onAddFee.emit();
  }

  editFee(f:BaseFee) {
    this.onEditFee.emit(f.id);
  }
}
