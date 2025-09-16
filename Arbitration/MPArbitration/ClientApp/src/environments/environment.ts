// This file can be replaced during build by using the `fileReplacements` array.
// `ng build` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.
export class environment {
  static production = false;
  static appVersion = require('../../package.json').version + '-def';
  static get redirectUrl() { 
    let h=location.hostname;
    h+= !!location.port ? ':' + location.port : '';
    return location.protocol + `//${h}`; 
  } //'https://localhost:44473';}
  static clientId = 'e6ddd06c-eb88-47fb-8579-185b2436a2cb';
  static tenantName = '2e09f3a3-0520-461f-8474-052a8ed7814a';

}

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/plugins/zone-error';  // Included with Angular CLI.
