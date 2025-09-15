import { LogLevel } from '@azure/msal-browser';
import { loggerCallback } from '../app.module';
import { AuthorityDisputeFee } from './authority-dispute-fee';
import {
  AuthorityDisputeCPT,
  AuthorityDisputeCPTVM,
} from './authority-dispute-cpt';
import { NgbDate } from '@ng-bootstrap/ng-bootstrap';

export class DisputeInit {
  public auth = '';
  public disputeNumber = ''; // aka dispute number
  public claims = ''; // List of AritrationCase ID numbers (aka Arbit IDs)
  public cpt = ''; // Either a single CPT value or an asterisk

  constructor(obj?: any) {
    if (!obj) return;
    Object.assign(this, JSON.parse(JSON.stringify(obj)));
  }
}

export class Dispute {
  public id = 0;
  public disputeNumber = '';
  public disputeStatus = '';
  public disputeWorkFlowStatus = '';
  public customer = '';
  public entity = '';
  public entityNPI = '';
  public payor = '';
  public certifiedEntity = '';
  public submissionDate: Date | undefined;
  public formalReceivedDate: Date | undefined;
  public IDRESelectionDate: Date | undefined;
  public awardDate: Date | undefined;

  public feeRequestDate: Date | undefined;
  public feeDueDate: Date | undefined;
  public feeDue = '';
  public feeAmountAdmin = '';
  public feeAmountEntity = '';
  public feeAmountTotal = '';

  public feePaidDate: Date | undefined;
  public feePaidAmount = '';

  public briefDueDate = '';
  public briefSubmissionlink = '';

  public comments = '';

  public cptViewmodels: AuthorityDisputeCPTVM[] = [];
  public createdBy = '';
  public createdOn: Date | undefined;

  public disputeCPTs: AuthorityDisputeCPT[] = [];
  public fees = new Array<AuthorityDisputeFee>();
  public updatedBy = '';
  public updatedOn: Date | undefined;

  public get submissionDateForPicker() {
    return !!this.submissionDate
      ? this.submissionDate.toLocaleDateString()
      : undefined;
  }
  public set submissionDateForPicker(
    value: NgbDate | null | string | undefined
  ) {
    if (!value) this.submissionDate = undefined;
    this.submissionDate = new Date(value + '');
  }

  constructor(obj?: any) {
    if (!obj) return;

    Object.assign(this, JSON.parse(JSON.stringify(obj)));
    try {
      if (this.createdOn) this.createdOn = new Date(obj.createdOn);
      if (this.submissionDate)
        this.submissionDate = new Date(obj.submissionDate);
      else this.submissionDate = new Date();
      if (this.updatedOn) this.updatedOn = new Date(obj.updatedOn);

      // objects
      if (!!this.cptViewmodels.length)
        this.cptViewmodels = this.cptViewmodels.map(
          (v) => new AuthorityDisputeCPTVM(v)
        );
      if (!!this.disputeCPTs.length)
        this.disputeCPTs = this.disputeCPTs.map(
          (v) => new AuthorityDisputeCPT(v)
        );
      if (!!this.fees.length) {
        const fees: AuthorityDisputeFee[] = [];
        this.fees.forEach((v) => fees.push(new AuthorityDisputeFee(v)));
        this.fees = fees;
      }
    } catch (err) {
      loggerCallback(LogLevel.Error, 'Unable to instantiate Entity');
      console.error(err);
    }
  }
}

export class DisputeVM extends Dispute {
  customers = '';

  public get currentCustomer() {
    if (!!this.cptViewmodels.length) {
      const p = [...new Set(this.cptViewmodels.map((v) => v.customer))];
      return p.length > 1 ? '(Multiple)' : p[0];
    }
    if (this.customers.indexOf(';') === -1) return this.customers;
    const a = this.customers.split(';');
    return a.filter((n, i) => a.indexOf(n) === i).join(';');
  }

  public get serviceLine() {
    const s = [...new Set(this.cptViewmodels.map((v) => v.serviceLine))];
    return s.length > 1 ? '(Multiple)' : s[0];
  }

  constructor(obj?: any) {
    super(obj);
    if (!obj) return;

    this.customers = obj.customers;
  }
}
