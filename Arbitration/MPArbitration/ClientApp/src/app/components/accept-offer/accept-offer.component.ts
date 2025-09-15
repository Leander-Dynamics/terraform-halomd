import { Component, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-accept-offer',
  templateUrl: './accept-offer.component.html',
  styleUrls: ['./accept-offer.component.css']
})
export class AcceptOfferComponent implements OnInit {
  offerType = '';
  notes = '';
  payorOffer = 0;
  providerOffer = 0;

  constructor(public activeModal: NgbActiveModal) { }

  ngOnInit(): void { 
  }

  selectAll(e:any) {
    e?.target?.select();
  }
}
