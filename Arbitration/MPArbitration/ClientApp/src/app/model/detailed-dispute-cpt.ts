import { IModifier } from './imodifier';

export class DetailedDisputeCPT {
  public id = 0;
  public arbitId = 0;
  public disputeNumber = '';
  public cptCode: string = '';
  public benchmarkAmount = 0;
  public providerOfferAmount = 0;
  public payorOfferAmount = 0;
  public awardAmount = 0;
  public payorClaimNumber = '';
  public prevailingParty = '';

  public createdBy = '';
  public createdOn: Date | undefined;
  public updatedOn: Date | undefined;
  public updatedBy = '';
  public isAddCPT = false;
  public isShowLink = false;

  constructor(obj?: any) {
    if (!obj) return;
    Object.assign(this, JSON.parse(JSON.stringify(obj)));
    if (this.createdOn) this.createdOn = new Date(obj.createdOn);
    if (this.updatedOn) this.updatedOn = new Date(obj.updatedOn);
  }
}
