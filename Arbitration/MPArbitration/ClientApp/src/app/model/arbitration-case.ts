import { LogLevel } from "@azure/msal-browser";
import { loggerCallback } from "../app.module";
import { CMSCaseStatus } from "./arbitration-status-enum";
import { CaseArbitrator } from "./case-arbitrator";
import { CaseLog } from "./case-log";
import { CaseTracking } from "./case-tracking";
import { ClaimCPT } from "./claim-cpt";
import { IModifier } from "./imodifier";
import { IPatientInfo } from "./iname";
import { Negotiator } from "./negotiator";
import { Note } from "./note";
import { Notification } from "./notification";
import { OfferHistory } from "./offer-history";
import { Payor } from "./payor";
import { CaseSettlement } from "./case-settlement";
import { IEHRKey } from "./iehr-key";
import { NgbDate } from "@ng-bootstrap/ng-bootstrap";


// NOTE: Fields that are commented out currently have not apparent use or are duplicates
export class ArbitrationCase implements IEHRKey, IModifier, IPatientInfo {

  public get LastEOBDate() {
    // the business has not clearly defined what this date really represents
    // so this property is a facade in case they change their minds again
    return this.EOBDate;
  }
  public id = 0;
  public additionalPaidAmount = 0;
  public arbitrators: CaseArbitrator[] = [];

  /** must file for arbitration by this date, 90 days after first payment received */
  public arbitrationDeadlineDate: Date | undefined;


  /** pay the arbitrator by this date */
  public arbitratorPaymentDeadlineDate: Date | undefined;
  public arbitrationBriefDueDate: Date | undefined;

  public arbitrationFeeAmount = 0;
  public assignedUser = '';  // who's queue is this currently in?
  /** TDI AssignmentDeadlineDate */
  public assignmentDeadlineDate: Date | undefined;
  /** Abbreviation of the authority e.g. TX */
  public authority = '';
  /** TDI RequestID */
  public authorityCaseId = '';

  public authorityProviderFinalOfferAmount = 0;
  public authorityStatus = ''; //:ArbitrationStatus = ArbitrationStatus.Assigned_To_Facilitator;
  public authorityUserId = '';

  public awardedTo = '';
  /** allows overriding benchmarks */
  public benchmarkGeoZip = '';
  /** not editable in UI **/
  public calculatedPayorFinalOfferAmount = 0;
  public caseSettlements: CaseSettlement[] = [];
  //public settlementDetails:CaseSettlementDetails[] = [];

  /** e.g. MedSurant, Peak, etc */

  public customer = '';
  public cptCodes: ClaimCPT[] = [];
  public createdBy = '';
  public createdOn: Date | undefined;
  public daysOpen = 0;
  public disputedAmount = 0;
  public DOB: Date | undefined;
  public EHRNumber = '';
  public EHRSource = '';
  public encounterServiceNo = '';
  public entity = '';
  public entityNPI = '';
  public EOBDate: Date | undefined;
  public estimatedDisputedAmount = 0;
  public expectedArbFee = 0;
  public fh50thPercentileExtendedCharges = 0;
  public fh80thPercentileExtendedCharges = 0;
  public firstAppealDate: Date | undefined;
  public firstResponseDate: Date | undefined;
  public firstResponsePayment = 0;
  public hasArbitratorWarning = false;
  public history = '';
  public ineligibilityReasons = '';
  public ineligibilityAction = '';
  public informalTeleconferenceDate: Date | undefined;
  /** allows soft delete and history tracking to continue */
  public isDeleted = false;

  /** MPower field - indicates new values available on this case */

  public isUnread = false;

  /** This is strictly a ViewModel flag. Set this to true when updating an
   * existing record using new Authority info if you want the old info saved
   * into the CaseArchives table.
   */
  public keepAuthorityInfo = false;

  public locationGeoZip = '';

  public log: CaseLog[] = [];

  /** TDI MethodOfPayment */
  public methodOfPayment = '';
  public notifications: Notification[] = [];

  public NSACaseId = '';
  /* These NSA Ineligibility fields can be used in the future to track feedback from the NSA when they reject a claim that was deemed NSA eligible by the Payor
  public NSAIneligibilityAction = '';
  public NSAIneligibilityReasons = '';
  */

  public NSARequestDiscount: number | null = null; // UI / VM hack 

  public NSAStatus = '';
  public NSATracking = '';
  public NSAWorkflowStatus: CMSCaseStatus = CMSCaseStatus.New;

  public notes: Note[] = [];
  public offerHistory: OfferHistory[] = [];

  public originalBilledAmount = 0.0;

  public patientName = '';

  public patientShareAmount = 0.0;

  public paymentMadeDate: Date | undefined;

  public paymentReferenceNumber = '';
  /** De-normalized, legacy value */
  public payor = '';

  public payorClaimNumber = '';

  public payorEntity: Payor | null = null;

  public payorNegotiator: Negotiator | null = null;

  public payorNegotiatorId: number | null = null;

  public payorFinalOfferAmount = 0;

  public payorGroupName = '';

  public payorGroupNo = '';

  public payorNSAIneligibilityAction = '';

  public payorNSAIneligibilityReasons = '';

  public payorId: number | null = null;

  public payorResolutionRequestReceivedDate: Date | undefined;

  public planPaidAmount = 0.0;
  /** e.g. Fully Insured, Self-Funded */
  public planType = '';

  public policyNumber = '';
  /** e.g. EMO, HMO, PPO */
  public policyType = '';
  /** calculated value */
  public projectedProfitFromFormalArb = 0;

  public providerFinalOfferAmount = 0;

  /** manual override */
  public providerFinalOfferAdjustedAmount = 0;
  /** calculated offer amount */
  public providerFinalOfferCalculatedAmount = 0;

  /** calculated */
  public providerFinalOfferNotToExceed = 0;
  /** Doctor's name */
  public providerName = '';

  public providerNegotiator = ''; // internal user
  /** Doctor's NPI license number */
  public providerNPI = '';

  public providerPaidDate: Date | undefined;

  public providerType = '';
  /** calculated */
  public reasonableAmount = 0.0;

  /** Removed per DevOps #828 */
  //public renderingProvider = '';

  public receivedFromCustomer: Date | undefined;

  public requestDate: Date | undefined;

  public requestType = '';

  public resolutionDeadlineDate: Date | undefined;

  public service = '';  // long form e.g. "IOM Pro"

  public serviceDate: Date | undefined;

  public serviceLine = '';  // shortened e.g. IOM

  public serviceLocationCode = '';

  public NegotiationNoticeDeadline: Date | undefined;

  /** work flow control field */
  public status: CMSCaseStatus = CMSCaseStatus.New;  // new > submitted > offered > responded > accepted > scheduled > prepared > settled > closed


  public submittedBy = '';
  /** calculated */
  public totalPaidAmount = 0;
  /** calculated */
  public totalChargedAmount = 0;
  /** calculated dates for deadlines */
  public tracking: CaseTracking | null = null;

  public updatedOn: Date | undefined;

  public updatedBy = '';
  /** post-case question for metrics */
  public wasIneligibilityDenied = false;
  /** post-case question*/
  public wasDisputeSettledOutsideOfArbitration = false;
  /** post-case question*/
  public wasDisputeSettledWithTeleconference = false;

  /* Properties to support different casing for tracking */
  public get DateOfService(): Date | undefined {
    return this.serviceDate;
  }
  public set DateOfService(value: Date | undefined) {
    this.serviceDate = value;
  }
  public get dateOfService(): Date | undefined {
    return this.serviceDate;
  }
  public set dateOfService(value: Date | undefined) {
    this.serviceDate = value;
  }

  public get eobDate(): Date | undefined {
    return this.EOBDate;
  }
  public set eobDate(value: Date | undefined) {
    this.EOBDate = value;
  }

  public get FirstAppealDate(): Date | undefined {
    return this.firstAppealDate;
  }
  public set FirstAppealDate(value: Date | undefined) {
    this.firstAppealDate = value;
  }

  public get FirstResponseDate(): Date | undefined {
    return this.firstResponseDate;
  }
  public set FirstResponseDate(value: Date | undefined) {
    this.firstResponseDate = value;
  }

  // Picker properties (aka Viewmodel properties)

  public get arbitrationBriefDueDateForPicker() {
    return !!this.arbitrationBriefDueDate ? this.arbitrationBriefDueDate.toLocaleDateString() : undefined;
  }
  public set arbitrationBriefDueDateForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.arbitrationBriefDueDate = undefined;
    this.arbitrationBriefDueDate = new Date(value + '');
  }

  public get assignmentDeadlineDateForPicker() {
    return !!this.assignmentDeadlineDate ? this.assignmentDeadlineDate.toLocaleDateString() : undefined;
  }
  public set assignmentDeadlineDateForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.assignmentDeadlineDate = undefined;
    this.assignmentDeadlineDate = new Date(value + '');
  }

  public get DOBForPicker() {
    return !!this.DOB ? this.DOB.toLocaleDateString() : undefined;
  }
  public set DOBForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.DOB = undefined;
    this.DOB = new Date(value + '');
  }

  public get eobDateForPicker() {
    return !!this.eobDate ? this.eobDate.toLocaleDateString() : undefined;
  }
  public set eobDateForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.eobDate = undefined;
    this.eobDate = new Date(value + '');
  }

  public get firstAppealDateForPicker() {
    return !!this.firstAppealDate ? this.firstAppealDate.toLocaleDateString() : undefined;
  }
  public set firstAppealDateForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.firstAppealDate = undefined;
    this.firstAppealDate = new Date(value + '');
  }

  public get firstResponseDateForPicker() {
    return !!this.firstResponseDate ? this.firstResponseDate.toLocaleDateString() : undefined;
  }
  public set firstResponseDateForPicker(value: NgbDate | null | string | undefined) {
    /*
    if(!value) {
        this.firstResponseDate = undefined;
        return;
    }
    const d=new Date(value.toString());
    if(!d||d.getFullYear()<2000)
        return;
    
    this.firstResponseDate = d;
    */
    if (!value)
      this.firstResponseDate = undefined;
    this.firstResponseDate = new Date(value + '');
  }

  public get informalTeleconferenceDateForPicker() {
    return !!this.informalTeleconferenceDate ? this.informalTeleconferenceDate.toLocaleDateString() : undefined;
  }
  public set informalTeleconferenceDateForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.informalTeleconferenceDate = undefined;
    this.informalTeleconferenceDate = new Date(value + '');
  }

  public get paymentMadeDateForPicker() {
    return !!this.paymentMadeDate ? this.paymentMadeDate.toLocaleDateString() : undefined;
  }
  public set paymentMadeDateForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.paymentMadeDate = undefined;
    this.paymentMadeDate = new Date(value + '');
  }

  public get payorResolutionRequestReceivedDateForPicker() {
    return !!this.payorResolutionRequestReceivedDate ? this.payorResolutionRequestReceivedDate.toLocaleDateString() : undefined;
  }
  public set payorResolutionRequestReceivedDateForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.payorResolutionRequestReceivedDate = undefined;
    this.payorResolutionRequestReceivedDate = new Date(value + '');
  }

  public get providerPaidDateForPicker() {
    return !!this.providerPaidDate ? this.providerPaidDate.toLocaleDateString() : undefined;
  }
  public set providerPaidDateForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.providerPaidDate = undefined;
    this.providerPaidDate = new Date(value + '');
  }

  public get requestDateForPicker() {
    return !!this.requestDate ? this.requestDate.toLocaleDateString() : undefined;
  }
  public set requestDateForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.requestDate = undefined;
    this.requestDate = new Date(value + '');
  }

  public get resolutionDeadlineDateForPicker() {
    return !!this.resolutionDeadlineDate ? this.resolutionDeadlineDate.toLocaleDateString() : undefined;
  }
  public set resolutionDeadlineDateForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.resolutionDeadlineDate = undefined;
    this.resolutionDeadlineDate = new Date(value + '');
  }

  public get serviceDateForPicker() {
    return !!this.serviceDate ? this.serviceDate.toLocaleDateString() : undefined;
  }
  public set serviceDateForPicker(value: NgbDate | null | string | undefined) {
    if (!value)
      this.serviceDate = undefined;
    this.serviceDate = new Date(value + '');
  }

  constructor(obj?: any) {
    if (!obj)
      return;
    // TODO: Ensure Status enum correctly initialized
    Object.assign(this, JSON.parse(JSON.stringify(obj)));
    try {
      this.authority = this.authority.toLowerCase();
      if (!this.NSAWorkflowStatus)
        this.NSAWorkflowStatus = CMSCaseStatus.New;
      if (this.NSARequestDiscount !== null && (this.NSARequestDiscount > .99 || this.NSARequestDiscount < 0))
        this.NSARequestDiscount = null;
      if (this.arbitrationDeadlineDate)
        this.arbitrationDeadlineDate = new Date(obj.arbitrationDeadlineDate);
      if (this.arbitrationBriefDueDate)
        this.arbitrationBriefDueDate = new Date(obj.arbitrationBriefDueDate);
      if (this.assignmentDeadlineDate)
        this.assignmentDeadlineDate = new Date(obj.assignmentDeadlineDate);

      // preserve the time and convert to locale
      if (obj.createdOn)
        this.createdOn = new Date(obj.createdOn);
      if (this.DOB)
        this.DOB = new Date(obj.DOB);
      if (this.EOBDate)
        this.EOBDate = new Date(obj.EOBDate);
      if (this.firstAppealDate)
        this.firstAppealDate = new Date(obj.firstAppealDate);
      if (this.firstResponseDate)
        this.firstResponseDate = new Date(obj.firstResponseDate);
      if (this.informalTeleconferenceDate)
        this.informalTeleconferenceDate = new Date(obj.informalTeleconferenceDate);
      if (this.paymentMadeDate)
        this.paymentMadeDate = new Date(obj.paymentMadeDate);
      if (this.payorResolutionRequestReceivedDate)
        this.payorResolutionRequestReceivedDate = new Date(obj.payorResolutionRequestReceivedDate);
      if (this.providerPaidDate)
        this.providerPaidDate = new Date(obj.providerPaidDate);
      if (this.receivedFromCustomer)
        this.receivedFromCustomer = new Date(obj.receivedFromCustomer);
      if (this.requestDate)
        this.requestDate = new Date(obj.requestDate);
      if (this.resolutionDeadlineDate)
        this.resolutionDeadlineDate = new Date(obj.resolutionDeadlineDate);
      if (this.serviceDate)
        this.serviceDate = new Date(obj.serviceDate);

      // preserve the time and convert to locale
      if (obj.updatedOn)
        this.updatedOn = new Date(obj.updatedOn);

      if (this.payorEntity)
        this.payorEntity = new Payor(this.payorEntity);
      if (this.payorNegotiator)
        this.payorNegotiator = new Negotiator(this.payorNegotiator);
      if (this.tracking)
        this.tracking = new CaseTracking(this.tracking);

      // instantiate ClaimCPT array
      if (this.cptCodes.length) {
        const cc: ClaimCPT[] = [];
        this.cptCodes.forEach(d => cc.push(new ClaimCPT(d)));
        this.cptCodes = cc;
      } else {
        this.cptCodes.push(new ClaimCPT());
      }

      // instantiate CaseArbitrator array
      this.hasArbitratorWarning = false;
      if (this.arbitrators.length) {
        const ca: CaseArbitrator[] = [];
        this.arbitrators.forEach(d => {
          ca.push(new CaseArbitrator(d));
          if (d.isActive && d.isLastResort || d.arbitrator?.isLastResort)
            this.hasArbitratorWarning = true;
        });
        this.arbitrators = ca;
      }

      // instantiate CaseLog array
      if (this.log.length) {
        const cl: CaseLog[] = [];
        this.log.forEach(d => cl.push(new CaseLog(d)));
        this.log = cl;
      }
      // instantiate Note array
      if (this.notes.length) {
        const n: Note[] = [];
        this.notes.forEach(d => n.push(new Note(d)));
        this.notes = n;
      }
      // instantiate Notifications array
      if (this.notifications.length) {
        const n: Notification[] = [];
        this.notifications.forEach(d => n.push(new Notification(d)));
        this.notifications = n;
      }
      // instantiate CaseSettlementDetails array
      if (this.caseSettlements.length) {
        const n: CaseSettlement[] = [];
        this.caseSettlements.forEach(d => n.push(new CaseSettlement(d)));
        this.caseSettlements = n;
      }
    } catch (err) {
      loggerCallback(LogLevel.Error, 'Unable to instantiate ArbitrationCase');
      console.error(err);
    }
  }
}
