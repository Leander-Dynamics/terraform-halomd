import { NgbDate } from '@ng-bootstrap/ng-bootstrap';
import { loggerCallback } from '../app.module';
import { LogLevel } from '@azure/msal-browser';
import { AuthorityDisputeFee } from './authority-dispute-fee';

export class Disputes {
  public arbitId = null;
  isDisputeSearch = false;
  public id = 0;
  public disputeNumber = '';
  public disputeStatus = '';
  public feeAmountAdmin = 0;
  public feeAmountEntity = 0;
  public feeAmountTotal = 0;
  // public disputeWorkFlowStatus = '';
  public customer = '';
  public entity = '';
  public entityNPI = '';
  public providerNPI = '';
  // public payor = '';
  public certifiedEntity = '';
  // public submissionDate: Date | undefined;
  // public formalReceivedDate: Date | undefined;
  // public IDRESelectionDate: Date | undefined;
  // public awardDate: Date | undefined;

  // public feeRequestDate: Date | undefined;
  // public feeDueDate: Date | undefined;
  // public feeDue = '';
  // public feeAmountAdmin = '';
  // public feeAmountEntity = '';
  // public feeAmountTotal = '';

  // public feePaidDate: Date | undefined;
  // public feePaidAmount = '';

  public briefDueDateFrom: string | null = null;
  public briefDueDateTo: string | null = null;

  // public briefDueDate = '';
  // public briefSubmissionlink = '';

  // public comments = '';

  // public createdBy = '';
  // public createdOn: Date | undefined;

  // public disputeCPTs = [];
  // public fees = new Array<AuthorityDisputeFee>();
  // public updatedBy = '';
  // public updatedOn: Date | undefined;

  constructor(obj?: any) {
    if (!obj) return;
    this.isDisputeSearch = !!obj.isDisputeSearch;
    Object.assign(this, JSON.parse(JSON.stringify(obj)));
  }
}
export class DisputesData {
  public disputes: Disputes | null = null;
  public pagerInfo = {
    nextPage: 0,
    pageNumber: 0,
    pageSize: 0,
    previousPage: 0,
    totalRecords: 0,
  };

  constructor(obj?: any) {
    if (!obj) return;

    Object.assign(this, JSON.parse(JSON.stringify(obj)));
    if (this.disputes) this.disputes = new Disputes(this.disputes);
  }
}
