export class DetailedDisputeCPTLog {
  // public id = 0;
  public changeLogID = 0;
  public transactionID = 0;
  public tableName = '';
  public activity = '';
  public previousValue = [];
  public newValue = [];
  public createdBy = '';
  public createdDate: Date | undefined;

  constructor(obj?: any) {
    if (!obj) return;
    Object.assign(this, JSON.parse(JSON.stringify(obj)));
    if (this.createdDate) this.createdDate = new Date(obj.createdDate);
  }
}
