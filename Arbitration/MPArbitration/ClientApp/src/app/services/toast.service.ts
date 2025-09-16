import { Injectable } from '@angular/core';
import { AlertInfo } from '../model/alert-info';
import { ToastEnum } from '../model/toast-enum';
import { ToastInfo } from '../model/toast-info';

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  alerts: AlertInfo[] = [];
  toasts: ToastInfo[] = [];

  remove(toast: ToastInfo) {
    this.toasts = this.toasts.filter(t => t != toast);
  }

  show(level: ToastEnum = ToastEnum.success, body: string, header: string = '', delay: number = 3000) {
    const v = 'text-light bg-' + ToastEnum[level];
    this.toasts.push({ header, body, delay, class: v});

  }

  showAlert(level: ToastEnum = ToastEnum.danger, message: string,toTop = true): void {
    if(!this.alerts.find(d=>d.message.toLowerCase()===message.toLowerCase()&&d.type===ToastEnum[level]))  // prevent duplicates
    {
      const type = ToastEnum[level];
      this.alerts.push({type, message});
    }
    if(toTop)
      window.scrollTo({top:0,behavior:'smooth'});
  }

  closeAlert(alert: AlertInfo) {
    this.alerts.splice(this.alerts.indexOf(alert), 1);
  }

  resetAlerts() {
    this.alerts.length = 0;
  }
}
