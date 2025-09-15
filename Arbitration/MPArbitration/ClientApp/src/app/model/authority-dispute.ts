import { LogLevel } from '@azure/msal-browser';
import { loggerCallback } from '../app.module';
import { UtilService } from '../services/util.service';
import { ArbitrationResult, CMSCaseStatus } from './arbitration-status-enum';
import { AuthorityDisputeFee } from './authority-dispute-fee';
import {
  AuthorityDisputeCPT,
  AuthorityDisputeCPTVM,
} from './authority-dispute-cpt';
import { Arbitrator } from './arbitrator';
import { NgbDate } from '@ng-bootstrap/ng-bootstrap';
import { Authority } from './authority';
import { AuthorityDisputeNote } from './note';
import { AuthorityDisputeAttachment } from './emr-claim-attachment';

export class AuthorityDisputeInit {
  public auth = '';
  public authorityCaseId = ''; // aka dispute number
  public claims = ''; // List of AritrationCase ID numbers (aka Arbit IDs)
  public cpt = ''; // Either a single CPT value or an asterisk

  constructor(obj?: any) {
    if (!obj) return;
    Object.assign(this, JSON.parse(JSON.stringify(obj)));
  }
}

export class AuthorityDispute {
  public id = 0;
  public arbitrationResult: ArbitrationResult = ArbitrationResult.None;
  public arbitrator: Arbitrator | undefined;
  public arbitratorId = 0;
  public arbitratorSelectedOn: Date | undefined;
  public attachments: AuthorityDisputeAttachment[] = [];
  public authority: Authority | undefined;
  public authorityId = 0;
  public authorityCaseId = ''; // aka "Dispute Number"
  public authorityStatus = '';
  public briefApprovedOn: Date | undefined;
  public briefApprovedBy = '';

  public briefPreparer = '';
  public briefPreparationCompletedOn: Date | undefined;

  public briefWriter = '';
  public briefWriterCompletedOn: Date | undefined;

  public cptViewmodels: AuthorityDisputeCPTVM[] = [];
  public createdBy = '';
  public createdOn: Date | undefined;

  public disputeCPTs: AuthorityDisputeCPT[] = [];
  public fees = new Array<AuthorityDisputeFee>();
  public ineligibilityAction = '';
  public ineligibilityReasons = '';
  public notes: AuthorityDisputeNote[] = [];

  public submissionDate: Date | undefined;
  public trackingValues = '{}';
  public workflowStatus: CMSCaseStatus = CMSCaseStatus.New;
  public updatedBy = '';
  public updatedOn: Date | undefined;

  public get briefApproverShort(): string {
    if (!this.briefApprovedBy) return '';
    return UtilService.ToTitleCase(
      this.briefApprovedBy
        .substring(0, this.briefApprovedBy.indexOf('@'))
        .replaceAll('.', ' ')
    );
  }
  public get briefPreparerShort(): string {
    if (!this.briefPreparer) return '';
    return UtilService.ToTitleCase(
      this.briefPreparer
        .substring(0, this.briefPreparer.indexOf('@'))
        .replaceAll('.', ' ')
    );
  }

  public get briefWriterShort(): string {
    if (!this.briefWriter) return '';
    return UtilService.ToTitleCase(
      this.briefWriter
        .substring(0, this.briefWriter.indexOf('@'))
        .replaceAll('.', ' ')
    );
  }

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

  // legacy tracking name support
  public get ArbitratorSelectedOn() {
    return this.arbitratorSelectedOn;
  }
  public set ArbitratorSelectedOn(value: Date | undefined) {
    this.arbitratorSelectedOn = value;
  }

  public get BriefApprovedOn() {
    return this.briefApprovedOn;
  }
  public set BriefApprovedOn(value: Date | undefined) {
    this.briefApprovedOn = value;
  }

  public get BriefDueDate() {
    if (
      !this.trackingValues ||
      !this.trackingValues.startsWith('{') ||
      !this.trackingValues.endsWith('}')
    )
      return undefined;
    const values = JSON.parse(this.trackingValues);
    return values['ArbitrationBriefDueOn'];
  }

  public get BriefPreparationCompletedOn() {
    return this.briefPreparationCompletedOn;
  }
  public set BriefPreparationCompletedOn(value: Date | undefined) {
    this.briefPreparationCompletedOn = value;
  }

  public get BriefWriterCompletedOn() {
    return this.briefWriterCompletedOn;
  }
  public set BriefWriterCompletedOn(value: Date | undefined) {
    this.briefWriterCompletedOn = value;
  }

  public get isClosed() {
    const w = this.workflowStatus;
    return (
      w === CMSCaseStatus.ClosedPaymentReceived ||
      w === CMSCaseStatus.ClosedPaymentWithdrawn ||
      w === CMSCaseStatus.Ineligible ||
      w === CMSCaseStatus.SettledArbitrationHealthPlanWon ||
      w === CMSCaseStatus.SettledArbitrationNoDecision ||
      w === CMSCaseStatus.SettledArbitrationPendingPayment ||
      w === CMSCaseStatus.SettledInformalPendingPayment ||
      w === CMSCaseStatus.SettledOutsidePendingPayment
    );
  }

  public get SubmissionDate() {
    return this.submissionDate;
  }
  public set SubmissionDate(value: Date | undefined) {
    this.submissionDate = value;
  }

  public get wasArbitrationCompleted() {
    // the AuthorityStatus to WorkflowStatus mapping becomes so important here
    return (
      this.workflowStatus === CMSCaseStatus.SettledArbitrationHealthPlanWon ||
      this.workflowStatus === CMSCaseStatus.SettledArbitrationNoDecision ||
      this.workflowStatus === CMSCaseStatus.SettledArbitrationPendingPayment
    );
  }

  constructor(obj?: any) {
    if (!obj) return;

    Object.assign(this, JSON.parse(JSON.stringify(obj)));
    try {
      if (this.arbitratorSelectedOn)
        this.arbitratorSelectedOn = new Date(obj.arbitratorSelectedOn);
      if (this.briefApprovedOn)
        this.briefApprovedOn = new Date(obj.briefApprovedOn);
      if (this.briefPreparationCompletedOn)
        this.briefPreparationCompletedOn = new Date(
          obj.briefPreparationCompletedOn
        );
      if (this.briefWriterCompletedOn)
        this.briefWriterCompletedOn = new Date(obj.briefWriterCompletedOn);
      if (this.createdOn) this.createdOn = new Date(obj.createdOn);
      if (this.submissionDate)
        this.submissionDate = new Date(obj.submissionDate);
      else this.submissionDate = new Date();
      if (this.updatedOn) this.updatedOn = new Date(obj.updatedOn);

      // objects
      if (!!obj.authority) this.authority = new Authority(obj.authority);
      if (!!this.attachments.length) {
        const attachments: AuthorityDisputeAttachment[] = [];
        this.attachments.forEach((v) =>
          attachments.push(new AuthorityDisputeAttachment(v))
        );
        this.attachments = attachments;
      }
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
      if (!!this.notes.length) {
        const notes: AuthorityDisputeNote[] = [];
        this.notes.forEach((v) => notes.push(new AuthorityDisputeNote(v)));
        this.notes = notes;
      }
    } catch (err) {
      loggerCallback(LogLevel.Error, 'Unable to instantiate Entity');
      console.error(err);
    }
  }
}

export class AuthorityDisputeVM extends AuthorityDispute {
  customers = '';
  linkedClaimIDs = '';
  patientNames = '';

  // NOTE: It is a violation of the current business rules to have multiple values
  // of any of the following fields but this is not the place to take that on by
  // blowing up the application. -wa 12Dec2023
  public get customer() {
    if (!!this.cptViewmodels.length) {
      const p = [...new Set(this.cptViewmodels.map((v) => v.customer))];
      return p.length > 1 ? '(Multiple)' : p[0];
    }
    if (this.customers.indexOf(';') === -1) return this.customers;
    const a = this.customers.split(';');
    return a.filter((n, i) => a.indexOf(n) === i).join(';');
  }

  public get entity() {
    const p = [...new Set(this.cptViewmodels.map((v) => v.entity))];
    return p.length > 1 ? '(Multiple)' : p[0];
  }

  public get entityNPI() {
    const p = [...new Set(this.cptViewmodels.map((v) => v.entityNPI))];
    return p.length > 1 ? '(Multiple)' : p[0];
  }

  public get payor() {
    const p = [...new Set(this.cptViewmodels.map((v) => v.payor))];
    return p.length > 1 ? '(Multiple)' : p[0];
  }

  public get patientName() {
    if (this.patientNames.indexOf(';') === -1) return this.patientNames;
    const a = this.patientNames.split(';');
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
    this.linkedClaimIDs = obj.linkedClaimIDs;
    this.patientNames = obj.patientNames;

    //this.payor = obj.payor ?? '';
    //this.serviceLine = obj.serviceLine ?? '';
  }
}
