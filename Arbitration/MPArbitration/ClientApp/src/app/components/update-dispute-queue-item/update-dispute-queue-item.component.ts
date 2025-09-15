import { Component, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { WorkQueueName } from 'src/app/model/work-queue-name-enum';

@Component({
  selector: 'app-update-dispute-queue-item',
  templateUrl: './update-dispute-queue-item.component.html',
  styleUrls: ['./update-dispute-queue-item.component.css']
})
export class UpdateDisputeQueueItemComponent implements OnInit {
  isReassigning = false;
  notes = '';
  title = 'Update work item';
  workQueue:WorkQueueName = WorkQueueName.None;

  constructor(public activeModal: NgbActiveModal) { }

  ngOnInit(): void {
  }

}
