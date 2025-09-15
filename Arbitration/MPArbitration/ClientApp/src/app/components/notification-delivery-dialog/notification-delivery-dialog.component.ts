import { Component, Input, OnInit } from '@angular/core';
import { Notification, NotificationAttachment, NotificationDeliveryInfo, NotificationRecipient } from 'src/app/model/notification';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-notification-delivery-dialog',
  templateUrl: './notification-delivery-dialog.component.html',
  styleUrls: ['./notification-delivery-dialog.component.css']
})
export class NotificationDeliveryDialogComponent implements OnInit {
  @Input() notification = new Notification();

  _delivery:NotificationDeliveryInfo|undefined;
  
  get delivery(): NotificationDeliveryInfo {
    if(!this._delivery)
      this._delivery = this.notification.Delivery;
    return this._delivery;
  }

  constructor(public activeModal: NgbActiveModal) { }

  ngOnInit(): void {
    
  }

}
