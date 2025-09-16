export class environment {
  static production = true;
  static appVersion = require('../../package.json').version;
  static get redirectUrl() { 
    let h=location.hostname;
    h+= !!location.port ? ':' + location.port : '';
    return location.protocol + `//${h}`; 
  }
  //redirectUrl: 'https://arbitration.mpowerhealth.com',
  static clientId = 'feb1fc35-5267-4946-bc23-d8da34237e90';
  static tenantName = '2e09f3a3-0520-461f-8474-052a8ed7814a';
};
