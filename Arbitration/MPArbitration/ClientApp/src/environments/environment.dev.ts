export class environment {
  static production = false;
  static appVersion = require('../../package.json').version + '-def';
  static get redirectUrl() { 
    let h=location.hostname;
    h+= !!location.port ? ':' + location.port : '';
    return location.protocol + `//${h}`; 
  } //'https://localhost:44473';}
  //export const environment = {
  //  production: false,
  //  appVersion: require('../../package.json').version + '-dev',
    //redirectUrl: 'https://arbitrationpoccalculator.azurewebsites.net',
  static clientId = 'e6ddd06c-eb88-47fb-8579-185b2436a2cb';
  static tenantName = '2e09f3a3-0520-461f-8474-052a8ed7814a';
  }