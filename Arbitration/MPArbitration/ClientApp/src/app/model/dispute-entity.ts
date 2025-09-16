export class DisputeEntity {
  public id = 0;
  public name = '';

  constructor(obj?: any) {
    if (!obj) return;

    Object.assign(this, JSON.parse(JSON.stringify(obj)));
  }
}
