import { LogLevel } from '@azure/msal-browser';
import { loggerCallback } from '../app.module';
import { DetailedDisputeCPT } from './detailed-dispute-cpt';
// import { NgbDate } from '@ng-bootstrap/ng-bootstrap';

export class DetailedDisputeInit {
  public auth = '';
  public disputeNumber = '';
  public cpt = ''; // Either a single CPT value or an asterisk

  constructor(obj?: any) {
    if (!obj) return;
    Object.assign(this, JSON.parse(JSON.stringify(obj)));
  }
}

export class DetailedDispute {
  public id = 0;
  public arbitId = 0;
  public disputeNumber = '';
  public disputeStatus = '';
  public disputeWorkFlowStatus = '';

  public customer = '';
  public entity = '';
  public entityNPI = '';
  public payor = '';
  public certifiedEntity = '';
  public submissionDate: Date | undefined;

  public idreSelectionDate: Date | undefined;
  public formalReceivedDate: Date | undefined;
  public briefAssignedDate: Date | undefined;

  public feeRequestDate: Date | undefined;
  public feeDueDate: Date | undefined;
  public feeAmountAdmin = 0;
  public feeAmountEntity = 0;
  public feeAmountTotal = 0;
  public feeInvoiceLink = '';
  public feePaidDate: Date | undefined;
  public awardDate: Date | undefined;
  public feePaidAmount = 0;

  public briefDueDate: Date | undefined;
  public briefSubmissionLink = '';
  public briefApprover = '';

  public createdBy = '';
  public createdOn: Date | undefined;

  public updatedBy = '';
  public updatedOn: Date | undefined;

  public comments = '';
  public serviceLine = '';

  public disputeCPTs: DetailedDisputeCPT[] = [];
  // public fees = new Array<DetailedDisputeFee>();

  public get SubmissionDate() {
    return this.submissionDate;
  }
  public set SubmissionDate(value: any) {
    this.submissionDate = new Date(value);
  }
  public get BriefDueDate() {
    return this.briefDueDate;
  }
  public set BriefDueDate(value: any) {
    this.briefDueDate = new Date(value);
  }
  public get IDRESelectionDate() {
    return this.idreSelectionDate;
  }
  public set IDRESelectionDate(value: any) {
    this.idreSelectionDate = new Date(value);
  }
  public get FormalReceivedDate() {
    return this.formalReceivedDate;
  }
  public set FormalReceivedDate(value: any) {
    this.formalReceivedDate = new Date(value);
  }

  public get BriefAssignedDate() {
    return this.briefAssignedDate;
  }
  public set BriefAssignedDate(value: any) {
    this.briefAssignedDate = new Date(value);
  }

  public get FeeRequestDate() {
    return this.feeRequestDate;
  }
  public set FeeRequestDate(value: any) {
    this.feeRequestDate = new Date(value);
  }
  public get FeeDueDate() {
    return this.feeDueDate;
  }
  public set FeeDueDate(value: any) {
    this.feeDueDate = new Date(value);
  }
  public get FeePaidDate() {
    return this.feePaidDate;
  }
  public set FeePaidDate(value: any) {
    this.feePaidDate = new Date(value);
  }
  public get AwardDate() {
    return this.awardDate;
  }
  public set AwardDate(value: any) {
    this.awardDate = new Date(value);
  }

  constructor(obj?: any) {
    if (!obj) return;

    Object.assign(this, JSON.parse(JSON.stringify(obj)));
    try {
      if (this.createdOn) this.createdOn = new Date(obj.createdOn);
      if (this.updatedOn) this.updatedOn = new Date(obj.updatedOn);

      if (this.submissionDate)
        this.submissionDate = new Date(obj.submissionDate);

      if (this.briefDueDate) this.briefDueDate = new Date(obj.briefDueDate);

      if (this.idreSelectionDate)
        this.idreSelectionDate = new Date(obj.idreSelectionDate);

      if (this.formalReceivedDate)
        this.formalReceivedDate = new Date(obj.formalReceivedDate);

      if (this.briefAssignedDate)
        this.briefAssignedDate = new Date(obj.briefAssignedDate);

      if (this.feeRequestDate)
        this.feeRequestDate = new Date(obj.feeRequestDate);

      if (this.feeDueDate) this.feeDueDate = new Date(obj.feeDueDate);

      if (this.feePaidDate) this.feePaidDate = new Date(obj.feePaidDate);

      if (this.awardDate) this.awardDate = new Date(obj.awardDate);

      if (!this.feeAmountAdmin) this.feeAmountAdmin = 0;
      if (!this.feeAmountEntity) this.feeAmountEntity = 0;
      if (!this.feeAmountTotal) this.feeAmountTotal = 0;

      if (!!this.disputeCPTs.length)
        this.disputeCPTs = this.disputeCPTs.map(
          (v) => new DetailedDisputeCPT(v)
        );
    } catch (err) {
      loggerCallback(LogLevel.Error, 'Unable to instantiate Entity');
      console.error(err);
    }
  }
}

export class DetailedDisputeVM extends DetailedDispute {
  customers = '';

  constructor(obj?: any) {
    super(obj);
    if (!obj) return;

    this.customers = obj.customers;
    this.payor = obj.payor ?? '';
    this.serviceLine = obj.serviceLine ?? '';
  }
}
