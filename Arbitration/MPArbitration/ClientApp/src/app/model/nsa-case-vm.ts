import { UtilService } from "../services/util.service";
import { ArbitrationCase } from "./arbitration-case";
import { CaseFileVM } from "./case-file";
import { IPatientInfo } from "./iname";

export class NSACaseVM implements IPatientInfo {
  isSelected = false;
  attachments: Array<CaseFileVM> = [];
  id = 0;
  fh50thPercentileExtendedCharges = 0;
  fh80thPercentileExtendedCharges = 0;

  EOBDate: Date | undefined;
  isNotificationQueued = false;

  isValidForNSAOpenRequest = false;
  NegotiationNoticeDeadline: Date | undefined = undefined;

  patientName = '';
  payor = '';
  payorClaimNumber = '';
  planType = '';
  policyType = '';
  providerName = '';
  providerNPI = '';
  record: ArbitrationCase | undefined;
  totalAllowedAmount = 0;
  calculatedNSAOffer = 0;

  _NSATrackingObject: any;
  get NSATrackingObject(): any {
    return this._NSATrackingObject;
  }

  set NSATrackingObject(value: any) {
    this._NSATrackingObject = value;

  }

  constructor(obj?: ArbitrationCase) {
    if (!obj)
      return;

    NSACaseVM.update(this, obj);
  }

  private _isValidForNSAOpenRequest(): boolean {
    if (!this.record)
      return false;
    return !!this.record.authority &&
      !!this.record.serviceLine &&
      //!!this.record.fh80thPercentileExtendedCharges && // should this be testing for 'undefined' ?
      !!this.record.payorClaimNumber &&
      !!this.record.providerName &&
      !!this.record.providerNPI &&
      !!this.record.entity &&
      !!this.record.entityNPI &&
      !!this.record.NSATracking &&
      !this.record.isDeleted;
    /* DevOps 1511
    !!this.record.planType && 
    this.record.planType !== 'Unknown' &&
     */
  }

  static update(s: NSACaseVM, obj: ArbitrationCase) {
    s.id = obj.id;
    s.patientName = obj.patientName;
    s.payor = obj.payor;
    s.payorClaimNumber = obj.payorClaimNumber;
    s.planType = obj.planType;
    s.policyType = obj.policyType;
    s.providerName = obj.providerName;
    s.providerNPI = obj.providerNPI;
    s.record = obj;
    s.fh50thPercentileExtendedCharges = obj.fh50thPercentileExtendedCharges;
    s.fh80thPercentileExtendedCharges = obj.fh80thPercentileExtendedCharges;

    if (typeof (obj.NegotiationNoticeDeadline) == 'string') {
      s.NegotiationNoticeDeadline = new Date(obj.NegotiationNoticeDeadline);
    } else {
      s.NegotiationNoticeDeadline = obj.NegotiationNoticeDeadline;
    }


    s.EOBDate = obj.EOBDate;
    s.isValidForNSAOpenRequest = s._isValidForNSAOpenRequest();
    let sum = 0;
    obj.cptCodes.map(v => sum += (v.paidAmount + v.patientRespAmount));
    s.totalAllowedAmount = sum;
  }
}
