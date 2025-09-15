import { Component, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastService } from 'src/app/services/toast.service';

@Component({
  selector: 'app-add-offer',
  templateUrl: './add-offer.component.html',
  styleUrls: ['./add-offer.component.css']
})
export class AddOfferComponent implements OnInit {
  allOfferSources = ['Email','EHR','Fax','Phone','Text','Other'];
  allOfferTypes = ['Payor','Provider'];
  fh80 = 0;
  hasSettlements = false;
  isManager = false;
  name='Edit';
  offerId = 0;
  notes = '';
  offerAmount = 0;
  offerSource:string | null = null;
  offerType:string | null = null;
  wasOfferAccepted = false;
  
  constructor(public activeModal: NgbActiveModal, private svcToast:ToastService) { }

  ngOnInit(): void {
  }

  isOfferLow() {
    return this.offerAmount > 0 && this.offerType==='Provider' && this.fh80 > 0 && this.offerAmount < this.fh80;
  }

  selectAll(e:any) {
    e?.target?.select();
  }
}
