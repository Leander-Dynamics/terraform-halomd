import { Component, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { BaseFee } from 'src/app/model/base-fee';
import { DeadlineType } from 'src/app/model/deadline-type-enum';
import { FeeType } from 'src/app/model/fee-type-enum';
import { IKeyId } from 'src/app/model/iname';

@Component({
  selector: 'app-add-fee',
  templateUrl: './add-fee.component.html',
  styleUrls: ['./add-fee.component.css']
})
export class AddFeeComponent implements OnInit {
  allDeadlineTypes = new Array<IKeyId>();
  allFeeTypes = new Array<IKeyId>();
  fee = new BaseFee();
  feeType = 'Authority';
  name='Edit';
  FeeType = FeeType;
  
  constructor(public activeModal: NgbActiveModal) { 

    this.allDeadlineTypes = Object.values(DeadlineType).filter(value => typeof value === 'string').map(key => { 
      const result = (key as string).split(/(?=[A-Z]+[a-z])/);
      return { id: (<any>DeadlineType)[key] as number, key: result.join(' ') };
    });

    this.allFeeTypes = Object.values(FeeType).filter(value => typeof value === 'string').map(key => { 
      const result = (key as string).split(/(?=[A-Z]+[a-z])/);
      return { id: (<any>FeeType)[key] as number, key: result.join(' ')};
    });
  }

  ngOnInit(): void {
    this.name=this.fee.id?'Edit':'Add';
  }

  selectAll(e:any) {
    e?.target?.select();
  }
}
