import { ArbitrationCase } from './arbitration-case';
import { Authority } from './authority';
import { ClaimCPT } from './claim-cpt';
import { IModifier } from './imodifier';

// Benchmark that's calculated using prior Payor reimbursement amounts
export class PayorBenchmark {
  public id = 0;
  public name = '';
  public paymentYear = 1901;
  public providerNPI = '';
  public geoZip = '';
  public geoRegion = '';
  public amount = 0;
}

export class AuthorityDisputeCPT implements IModifier {
  public id = 0;

  public addeddBy = '';
  public addedOn: Date | undefined;
  public authorityDisputeId = 0; // parent record

  public awardAmount = 0;

  public benchmarkAmount = 0;
  public benchmarkDataItemId = 0; // soft link to benchmark used to
  public benchmarkDatasetId = 0; // soft link to benchmark set
  protected benchmarkOverrideAmount = 0;
  public calculatedOfferAmount = 0;
  public finalOfferAmount = 0;

  public claimCPTId = 0;
  public claimCPT: ClaimCPT | undefined;
  public createdBy = '';
  //public createdOn:Date|undefined;
  public serviceLineDiscount = 0;
  public updatedOn: Date | undefined;
  public updatedBy = '';

  constructor(obj?: any) {
    if (!obj) return;
    Object.assign(this, JSON.parse(JSON.stringify(obj)));
    if (this.addedOn) this.addedOn = new Date(obj.addedOn);
    if (this.updatedOn) this.updatedOn = new Date(obj.updatedOn);
    if (!!obj.claimCPT) this.claimCPT = new ClaimCPT(obj.claimCPT);
  }
}

export class AuthorityDisputeCPTVM extends AuthorityDisputeCPT {
  public customer = '';
  public description = '';
  public entity = '';
  public entityNPI = '';

  public geoRegion = '';
  public geoZip = '';

  //public isAwarded = false;  // simple UI viewmodel flag

  // calculated at runtime by comparing
  // CPTs in a dispute to those contained
  // in the CaseSettlementCPTs collection for
  // the claims referenced in a dispute
  //public isSettled = false;

  public notificationDate: Date | undefined;
  public paymentMethod = '';
  public payor = '';
  public payorClaimNumber = '';
  public payorId = 0;
  public planType = '';
  public providerName = '';
  public providerNPI = '';

  public serviceDate: Date | undefined;
  public serviceLine = '';

  _claimExpiration: Date | undefined;
  _trackingData = '{}';

  // per-unit
  public get effectiveBenchmarkAmount(): number {
    if (this.benchmarkOverrideAmount > 0) return this.benchmarkOverrideAmount;
    return this.benchmarkAmount;
  }

  public get benchmarkOverride() {
    return this.benchmarkOverrideAmount;
  }
  public set benchmarkOverride(value: number) {
    this.benchmarkOverrideAmount = value <= 0 ? 0 : value;
    this.calculatedOfferAmount = Number(
      (this.effectiveBenchmarkAmount * (1 - this.serviceLineDiscount)).toFixed(
        2
      )
    );
  }

  public get claimExpirationDate(): string {
    if (!!this._claimExpiration) return this._claimExpiration.toISOString();
    if (!this._trackingData) return '';

    const j = JSON.parse(this._trackingData);
    if (j['ArbitrationFilingDeadline'])
      return new Date(j['ArbitrationFilingDeadline']).toISOString();
    if (j['ArbitrationFilingStartDate'])
      return new Date(j['ArbitrationFilingStartDate']).toISOString();

    return '';
  }

  public get finalOfferDiscount(): number {
    if (!this.finalOfferTotal || !this.effectiveBenchmarkAmount) return 0;
    const num = 1 - this.finalOfferAmount / this.effectiveBenchmarkAmount;
    return Number(num.toFixed(2));
  }

  public get finalOfferTotal(): number {
    if (!this.claimCPT || this.claimCPT.units < 1) return 0;
    const eff =
      this.finalOfferAmount > 0
        ? this.finalOfferAmount
        : this.calculatedOfferAmount;
    return Number((eff * this.claimCPT.units).toFixed(2));
  }

  public static Create(
    obj: ClaimCPT,
    claim: ArbitrationCase,
    auth: Authority
  ): AuthorityDisputeCPTVM {
    var vm = new AuthorityDisputeCPTVM(obj);

    vm.planType = claim.planType;
    vm.geoZip = claim.locationGeoZip;

    const key = auth.key.toLowerCase();
    if (key === 'tx') vm._claimExpiration = claim.arbitrationDeadlineDate;

    if (key === 'nsa') vm._trackingData = claim.NSATracking;
    else if (!!claim.tracking) vm._trackingData = claim.tracking.trackingValues;

    return vm;
  }

  constructor(obj?: any) {
    super(obj);
    if (!obj) return;

    // fill in other properties manually because...JS weak sauce!
    if (obj.notificationDate)
      this.notificationDate = new Date(obj.notificationDate);
    if (obj.serviceDate) this.serviceDate = new Date(obj.serviceDate);

    this.customer = obj.customer ?? '';
    this.entity = obj.entity ?? '';
    this.entityNPI = obj.entityNPI ?? '';
    this.geoRegion = obj.geoRegion ?? '';
    this.geoZip = obj.geoZip ?? '';
    //this.isAwarded = !!obj.isAwarded;
    //this.isSettled = !!obj.isSettled;
    this.payor = obj.payor ?? '';
    this.payorClaimNumber = obj.payorClaimNumber ?? '';
    this.payorId = obj.payorId ?? 0;
    this.planType = obj.planType ?? '';
    this.providerName = obj.providerName ?? '';
    this.providerNPI = obj.providerNPI ?? '';
    this.serviceLine = obj.serviceLine ?? '';
  }
}
