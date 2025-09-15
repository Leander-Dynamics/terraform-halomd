import { DisputeLinkVM } from './dispute-link-vm';
import { IModifier } from './imodifier';

export class ClaimCPT implements IModifier {
  public id = 0;
  public arbitrationCaseId = 0;
  public cptCode: string = '';
  public createdBy = '';
  public createdOn: Date | undefined;
  public disputes: DisputeLinkVM[] = [];

  // Fair Health CPT Data
  public fh50thPercentileCharges = 0; // i.e. Allowable Base amount
  public fh50thPercentileExtendedCharges = 0; // uses 50th only

  public fh80thPercentileCharges = 0; // i.e. 80th percentile base amount
  public fh80thPercentileExtendedCharges = 0;

  //public fhExtendedAdjustedAmount = 0;  // could apply a weight or cap to this but so far unused
  //public fhProviderFinalOfferNotToExceed = 0;  // capped by formula or app variable dependent on service line - not used at cpt level yet
  //public fhProviderFinalOffer= 0; // calculated offer using a formula - not used at cpt level yet

  /** MPower field - allows soft delete and history tracking to continue */
  public isDeleted = false;
  public isEligible = false; // eligible for arbitration
  public isIncluded = true;
  public isLocked = false; // prevents editing of this CPT

  public modifiers: string = ''; // EHR flags
  public modifier26_YN = false; // todo: move to a Viewmodel
  public paidAmount = 0; // how much the payor has already paid on this CPT
  public patientRespAmount = 0; // how much the patient is responsible form
  public providerChargeAmount = 0; // how much was originally charged for this CPT

  public units: number = 1;
  public updatedOn: Date | undefined;
  public updatedBy = '';

  constructor(obj?: any) {
    if (!obj) return;
    Object.assign(this, JSON.parse(JSON.stringify(obj)));
    if (this.createdOn) this.createdOn = new Date(obj.createdOn);
    if (this.updatedOn) this.updatedOn = new Date(obj.updatedOn);
  }
}

export class ClaimCPTBatchVM extends ClaimCPT {
  count = 0;
  constructor(obj: ClaimCPT) {
    super(obj);
  }
}
