import { Component, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { Negotiator } from 'src/app/model/negotiator';

@Component({
  selector: 'app-negotiator',
  templateUrl: './negotiator.component.html',
  styleUrls: ['./negotiator.component.css']
})
export class NegotiatorComponent implements OnInit {
  public contact:Negotiator = new Negotiator();
  public payorName = '';
  
  constructor(public activeModal: NgbActiveModal) { }

  ngOnInit(): void {
  }

}
