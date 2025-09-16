import { DatePipe, DecimalPipe } from '@angular/common';
import { Injectable } from '@angular/core';
import { LogLevel } from '@azure/msal-browser';
import { BehaviorSubject } from 'rxjs';
import { loggerCallback } from '../app.module';
import { ArbitrationCase } from '../model/arbitration-case';
import { ArbitrationCaseVM } from '../model/arbitration-case-vm';
import { IArbStats } from '../model/arbitrator';
import { Authority } from '../model/authority';
import { AuthorityBenchmarkDetails } from '../model/authority-benchmark-details';
import { AuthorityTrackingDetail } from '../model/authority-tracking-detail';
import { CalculatorVariables } from '../model/calculator-variables';
import { Holiday } from '../model/holiday';
import { ICreatedOn } from '../model/icreated-on';
import { IModifier } from '../model/imodifier';
import { IName, IPatientInfo } from '../model/iname';
import { IOrder } from '../model/iorder';
import { NotificationType } from '../model/notification-type-enum';
import { DiffService } from '../model/obj-diff';
import { ToastEnum } from '../model/toast-enum';
import { NgbDate } from '@ng-bootstrap/ng-bootstrap';
import { CaseArbitrator } from '../model/case-arbitrator';
import { CaseArchive } from '../model/case-archive';
import { IPayorName } from '../model/authority-payor-group-exclusion';
import { ToastService } from './toast.service';
import { HttpErrorResponse } from '@angular/common/http';
import { ClaimCPT } from '../model/claim-cpt';
import { ProviderVM } from '../model/provider-vm';
import { CMSCaseStatus } from '../model/arbitration-status-enum';
import { IArbitrationCase } from '../model/iarbitration-case';
import { BaseNote, Note } from '../model/note';
import { PlaceOfServiceCode } from '../model/place-of-service-code';
import { AuthorityDisputeVM } from '../model/authority-dispute';
import { Disputes } from '../model/disputes-data';

@Injectable({
  providedIn: 'root',
})
export class UtilService {
  private _showLoading$ = new BehaviorSubject<boolean>(true);
  public static DecimalPipe = new DecimalPipe('en-US');
  public static DatePipe = new DatePipe('en-US');
  public static Holidays: Holiday[] = [];
  public static LastSearches: ArbitrationCaseVM[] = [];
  public static LastDisputesSearches: Disputes[] = [];
  public static readonly NumericKeys = [
    8, 46, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 96, 97, 98, 99, 100, 101,
    102, 103, 104, 105, 110, 190,
  ];
  public static PendingAlerts: { level: ToastEnum; message: string }[] = [];
  public static readonly TAB = 9;
  public static readonly ENTER = 13;
  public static readonly UP = 38;
  public static readonly DOWN = 40;
  public static readonly LEFT = 37;
  public static readonly RIGHT = 39;
  public static readonly CONTROL_KEYS = [
    UtilService.TAB,
    UtilService.ENTER,
    UtilService.UP,
    UtilService.DOWN,
    UtilService.LEFT,
    UtilService.RIGHT,
  ];

  get showLoading$() {
    return this._showLoading$;
  }

  set showLoading(v: boolean) {
    this._showLoading$.next(v);
  }

  constructor() {}

  static AddDays(d: Date, days: number, t: string = 'Calendar') {
    if (days === 0) return d;

    // Calendar days
    if (t !== 'Workdays') {
      let date = new Date(d.valueOf());
      date.setDate(date.getDate() + days);
      return date;
    }

    // Workdays
    let _weekdays = [1, 2, 3, 4, 5];
    let businessDays = days;
    let currentDate = new Date(d.valueOf());
    const direction = days < 0 ? -1 : 1;

    while (businessDays != 0) {
      currentDate.setDate(currentDate.getDate() + direction);
      const h = UtilService.Holidays.find(
        (d) =>
          !!d.endDate &&
          !!d.startDate &&
          d.startDate.toDateString() === currentDate.toDateString()
      ); // needs work -> || (!!d.startDate && d.startDate <= currentDate && !!d.endDate && d.endDate >= currentDate));
      if (_weekdays.includes(currentDate.getDay()) && !h) {
        businessDays -= direction;
      }
    }
    return currentDate;
  }

  /* not needed yet - if needed, integrate Holidays
  static CalcBusinessDays(startDate:Date, endDate:Date) {
    let _weekdays = [0,1,2,3,4];
    //var wdArr= [];
    let businessDays = 0;
    let currentDate = startDate;
    while (currentDate <= endDate) {
      if ( _weekdays.includes(currentDate.getDay())){
        businessDays++; //wdArr.push(currentDate);
        //if you want to format it to yyyy-mm-dd
        //wdArr.push(currentDate.toISOString().split('T')[0]);
      }
      currentDate.setDate(currentDate.getDate() +1);
    }
    return businessDays; // wdArr;
  }
  */

  static CreateTrackingObject(a: AuthorityTrackingDetail[]) {
    const b = {} as any;
    a.forEach((f) => {
      if (f.trackingFieldName && f.trackingFieldType === 'Date') {
        b[f.trackingFieldName] = null;
      }
    });
    return b;
  }

  static DayDiff(d1: Date, d2: Date): number {
    // To calculate the time difference of two dates
    const Difference_In_Time = d2.getTime() - d1.getTime();

    // To calculate the no. of days between two dates
    return Difference_In_Time / (1000 * 3600 * 24);
  }

  static DownloadObjects(
    records: any[],
    filename: string,
    includeArbs: boolean = true
  ) {
    const skip = [
      'arbitrators',
      'benchmarks',
      'cptCodes',
      'log',
      'notes',
      'offerHistory',
      'payorEntity',
      'tracking',
    ];
    const r = records[0] as any;
    const fields = new Array<string>();

    for (let k of Object.keys(r)) {
      if (skip.indexOf(k) >= 0) continue;
      fields.push(k);
    }

    let csv = fields.join(',');
    if (includeArbs) csv += ',arbitrators\n';
    else csv += '\n';
    const fmt = new DatePipe('en-US');

    for (let r of records) {
      try {
        let row = r as any;
        for (let col of fields) {
          let t = typeof row[col];
          if (t === 'string') {
            if (isNaN(Date.parse(row[col])))
              csv += `"${row[col].replaceAll('"', '')}",`;
            else csv += `${fmt.transform(row[col], 'MM/dd/yyyy')},`;
          } else if (t === 'number') csv += `${row[col]},`;
          else if (row[col] instanceof Date && !isNaN(row[col].valueOf())) {
            csv += `${fmt.transform(row[col], 'MM/dd/yyyy')},`;
          } else if (row[col]) {
            csv += `"${row[col].toString().replaceAll('"', '')}",`;
          } else {
            csv += ',';
          }
        }
        // now add concatenated arbitrators
        if (includeArbs && row['arbitrators']?.length) {
          const arbs = row['arbitrators'] as CaseArbitrator[];
          const names = arbs.map((j) => j.arbitrator?.name);
          csv += names.join(';');
        }
      } catch (err) {
        csv += `,${err}`;
      }
      csv += '\n';
    }

    const hidden = document.createElement('a');
    hidden.target = '_blank';
    const dt = new Date().toISOString();
    hidden.download = !!filename
      ? `${dt}-${filename}.csv`
      : `${dt}-Arbit-Search-Results.csv`;
    const data = new Blob([csv], { type: 'data:text/csv' });
    var url = window.URL.createObjectURL(data);
    hidden.href = url;
    hidden.click();
  }

  static GetDifferences(obj1: any, obj2: any) {
    let d = DiffService(obj1, obj2);
    delete d.updatedOn;
    if (d.tracking && d.tracking.updatedOn) {
      delete d.tracking.updatedOn;
      if (!Object.keys(d.tracking).length) delete d.tracking;
    }
    return d;
  }

  /** This function now simply returns a new Date object using the local timezone. The
   * function name was left in place to avoid a large refactoring effort / cleanup for
   * the moment. Eventually, this will be removed.
   *
   */
  static GetUTCDate(s: Date | string | null | undefined): Date | undefined {
    if (!s) return undefined;

    if (s instanceof Date) return s;

    return new Date(s);
  }

  /** This function now simply returns a new Date object using the local timezone. The
   * function name was left in place to avoid a large refactoring effort / cleanup for
   * the moment. Eventually, this will be removed.
   *
   */
  static GetUTCDate2(s: Date | string | null | undefined): Date | undefined {
    if (!s) return undefined;

    if (s instanceof Date) return s;

    return new Date(s);
  }

  static ISODateToLocal(s: string): Date | undefined {
    const p = s.split('-');
    let r = undefined;
    if (p.length === 3) {
      r = new Date(`${p[0]}/${p[1]}/${p[2]}`);
    }
    return r;
  }
  /* Assume the date is UTC when parsing even if trailing 'Z' is missing
  static GetUTCDate2(s: Date | null | string | undefined): Date|undefined {
    if (!s)
      return undefined;
    try {
      if (typeof s == 'string') {
        if (s.indexOf('T') > -1) {
          const z = s.slice(-1);
          if (z === 'Z')
            return new Date(s); //returns the exact Date w/o mutating it
          if (isNaN(parseInt(z)))
            return undefined; // garbage
          return new Date(s + 'Z'); // assume value is UTC so add the Z to it
        } else if (s.split('/').length === 3) {
          //const b = new Date(s);
          //return new Date(Date.UTC(b.getFullYear(),b.getMonth()-1,b.getDate()));
          const p = s.split('/');
          return new Date(Date.UTC(2023, parseInt(p[0]) - 1, parseInt(p[1]))); // treat as UTC
        } else {
          return new Date(s); // what is this?
        }
      }
      return s;
    } catch (err) {
      return undefined;
    }
  }

  static GetUTCDate(s: Date | string | undefined): Date|undefined {
    if (!s)
      return undefined;

    if (s instanceof Date) {
      //if(s.getHours() === 0)
      //  s.setHours(6);
      return s;
    }

    try {
      if (s.indexOf('T') > -1) {
        const z = s.slice(-1);
        if (z === 'Z' || !isNaN(parseInt(z))) {
          // convert the ISO date into a local date by ignoring the time portion
          const p = s.substring(0, s.search('T')).split('-');
          return new Date(parseInt(p[0]), parseInt(p[1]) - 1, parseInt(p[2]));
        }
        return undefined; // garbage
      } else if (s.split('/').length === 3) {
        //const p=s.split('/');
        // convert the date string into a local date
        return new Date(s);
        //return new Date(Date.UTC(2023,parseInt(p[0])-1,parseInt(p[1]))); // treat as UTC
      } else {
        console.error('GetUTCDate: Unexpected format!');
        return new Date(s); // what is this?
      }
    } catch (err) {
      return undefined;
    }
  }
  */

  static GetCalculatedValue(
    fld: string,
    templateType: NotificationType,
    arbCase: ArbitrationCase,
    calcVars: CalculatorVariables,
    stateAuth: Authority | undefined,
    cpt: ClaimCPT | undefined = undefined
  ) {
    if ((arbCase as any)[fld]) return (arbCase as any)[fld];
    if ((calcVars as any)[fld]) return (calcVars as any)[fld];

    if (fld.toLowerCase() === 'global.today') {
      return new Date();
    }

    switch (templateType) {
      // The below values are in the context of an NSA Open Request notification - this is why fh80th is used. This will eventually be configurable on the server.
      case NotificationType.NSANegotiationRequest:
        if (fld === 'settlementReduction') {
          const disc =
            arbCase.NSARequestDiscount !== null &&
            arbCase.NSARequestDiscount > 0 &&
            arbCase.NSARequestDiscount < 0.99
              ? 1 - arbCase.NSARequestDiscount
              : 1 - (calcVars?.nsaOfferDiscount ?? 0);
          return (
            UtilService.DecimalPipe.transform((1 - disc) * 100, '.0') + '%'
          );
        }
        if (fld === 'calculatedNSAOffer') {
          const disc =
            arbCase.NSARequestDiscount !== null &&
            arbCase.NSARequestDiscount > 0 &&
            arbCase.NSARequestDiscount < 0.99
              ? 1 - arbCase.NSARequestDiscount
              : 1 - (calcVars?.nsaOfferDiscount ?? 0);
          if (!!cpt) {
            var bm = (cpt as any)[calcVars.nsaOfferBaseValueFieldname] ?? 0;
            if (cpt.providerChargeAmount < bm) {
              console.warn(
                'nsaOfferBaseValueFieldname ' +
                  bm +
                  'is less then providerChargeAmount' +
                  cpt.providerChargeAmount +
                  ', so using providerChargeAmount '
              );
              bm = cpt.providerChargeAmount;
            }

            return UtilService.DecimalPipe.transform(bm * disc, '1.2-2');
          } else {
            return UtilService.DecimalPipe.transform(
              UtilService.GetCPTValueSum(
                arbCase.cptCodes,
                calcVars.nsaOfferBaseValueFieldname
              ) * disc,
              '1.2-2'
            );
          }
        }
        if (fld === 'benchmarkTitle') {
          const b = stateAuth?.benchmarks.find((v) => v.isDefault);
          return !!b && b.benchmark ? b.benchmark.name : '_____';
        }
        break;
    }
    return '___';
  }

  static GetCaseDate(e: NgbDate | null | string | undefined) {
    if (!(e instanceof NgbDate)) return undefined;
    return !!e && !!e.year
      ? new Date(`${e.month}/${e.day}/${e.year}`)
      : undefined;
  }

  static GetCPTValueSum(
    codes: ClaimCPT[] | undefined,
    s: string | undefined
  ): number {
    if (!s || !codes || !codes.length) return 0;

    let sum = 0;

    if (s === 'charge') {
      codes.map(
        (c) =>
          (sum = UtilService.RoundMoney(
            sum + (c.isIncluded ? c.providerChargeAmount : 0)
          ))
      );
    } else if (s === 'paid') {
      codes.map(
        (c) =>
          (sum = UtilService.RoundMoney(
            sum + (c.isIncluded ? c.paidAmount : 0)
          ))
      );
    } else if (s === 'patient') {
      codes.map(
        (c) =>
          (sum = UtilService.RoundMoney(
            sum + (c.isIncluded ? c.patientRespAmount : 0)
          ))
      );
    } else if (s === 'fh50Ext') {
      codes.map(
        (c) =>
          (sum = UtilService.RoundMoney(
            sum + (c.isIncluded ? c.fh50thPercentileExtendedCharges : 0)
          ))
      );
    } else if (s === 'fh80Ext') {
      codes.map(
        (c) =>
          (sum = UtilService.RoundMoney(
            sum + (c.isIncluded ? c.fh80thPercentileExtendedCharges : 0)
          ))
      );
    } else if (s === 'fh50thPercentileExtendedCharges') {
      // summing up for Offer Base ()
      codes.map(
        (c) =>
          (sum = UtilService.RoundMoney(
            sum +
              (c.isIncluded
                ? c.providerChargeAmount > c.fh50thPercentileExtendedCharges
                  ? c.fh50thPercentileExtendedCharges
                  : c.providerChargeAmount
                : 0)
          ))
      );
    } else if (s === 'fh80thPercentileExtendedCharges') {
      // summing up for Offer Base ()
      codes.map(
        (c) =>
          (sum = UtilService.RoundMoney(
            sum +
              (c.isIncluded
                ? c.providerChargeAmount > c.fh80thPercentileExtendedCharges
                  ? c.fh80thPercentileExtendedCharges
                  : c.providerChargeAmount
                : 0)
          ))
      );
    } else {
      // support the dynamic assignment of a raw CPT field as defined in the calculator variables
      const test = codes[0] as any;
      if (typeof test[s] != 'undefined') {
        codes.map(
          (c) =>
            (sum = UtilService.RoundMoney(
              sum + (c.isIncluded ? ((c as any)[s] as number) : 0)
            ))
        );
      }
    }
    console.warn('GetCPTValueSum: ' + s + ' ' + sum);
    return UtilService.RoundMoney(sum);
  }

  /*
  static GetUTCAsLocaleDateString(t: Date, r: string = 'US') {
    if (!r || r === 'US')
      return `${t.getUTCMonth() + 1}/${t.getUTCDate()}/${t.getUTCFullYear()}`;
    return t.toLocaleDateString();
  }
  */
  static ExtractMessageFromErr(err: any): string {
    let msg = '';
    if (err instanceof HttpErrorResponse) {
      if (err.error) {
        msg =
          typeof err.error == 'string'
            ? err.error
            : err.error.title ?? err.error.Exception ?? err.toString();
      } else {
        msg = err.message;
      }
      return msg;
    } else if (err.message) {
      return err.message;
    }
    return (
      msg ??
      err?.error?.title ??
      err.error ??
      err.message ??
      err.statusText ??
      err.Exception ??
      err.toString()
    );
  }

  static CurrencyBlur(e: any) {
    UtilService.FixTo2Digits(e.target);
  }

  // not the "Angular" way - should be a directive
  static FixTo2Digits(target: any) {
    if (!target) return;
    const el = $(target);
    if (!el) return;
    // get the current input value
    let correctValue = el.val().toString();

    //if there are no decimal places add trailing zeros
    if (correctValue.indexOf('.') === -1) {
      correctValue += '.00';
    } else {
      const ss = correctValue.toString().split('.');
      if (ss[1].length === 1) {
        //if there is only one number after the decimal add a trailing zero
        correctValue += '0';
      } else if (ss[1].length > 2) {
        //if there is more than 2 decimal places round backdown to 2
        correctValue = parseFloat($(el).val()).toFixed(2);
      }
    }

    //update the value of the input with our conditions
    $(el).val(correctValue);
  }

  static FocusNextElement(m: HTMLElement) {
    //add all elements we want to include in our selection
    var focussableElements =
      'a:not([disabled]), button:not([disabled]), input[type=text]:not([disabled]), [tabindex]:not([disabled]):not([tabindex="-1"])';

    const focussable = Array.prototype.filter.call(
      document.querySelectorAll(focussableElements),
      function (element) {
        //check for visibility while always include the current activeElement
        return (
          element.offsetWidth > 0 || element.offsetHeight > 0 || element === m
        );
      }
    );
    const index = focussable.indexOf(m);
    if (index > -1) {
      const nextElement = focussable[index + 1] || focussable[0];
      nextElement.focus();
    }
  }

  static HandleGridNav(event: any) {
    const charCode = event.which ? event.which : event.keyCode;

    if (this.CONTROL_KEYS.indexOf(charCode) === -1) return true;

    const m = event.target; // as HTMLElement;
    const t = m.id.split('_');
    if (t.length < 2) return true;

    let value = t[t.length - 1];
    if (isNaN(value)) return true;

    value = parseInt(value);

    if (charCode === this.ENTER) {
      this.FocusNextElement(m as HTMLElement);
    } else if (charCode === this.UP) {
      if (value) {
        t[t.length - 1] = value - 1;
        const z = document.getElementById(t.join('_'));
        if (!!z) z.focus();
      }
      return false;
    } else if (charCode === this.DOWN) {
      t[t.length - 1] = value + 1;
      const z = document.getElementById(t.join('_'));
      if (!!z) z.focus();
      return false;
    }
    /* see insane SO thread about why this chicanery is necessary:
    // https://stackoverflow.com/questions/21177489/selectionstart-selectionend-on-input-type-number-no-longer-allowed-in-chrome
    else if(charCode === this.LEFT){
      console.log(`start: ${event.target.selectionStart}`);
    } else if(charCode === this.RIGHT){
      console.log(`start: ${event.target.selectionStart}`);
    }
    */
    return true;
  }

  static HandleGridNavNumeric(event: any) {
    const charCode = event.which ? event.which : event.keyCode;
    console.log(charCode);
    // Only Numbers 0-9
    if (this.CONTROL_KEYS.indexOf(charCode) > -1) {
      const r = this.HandleGridNav(event);
      if (!r) {
        event.preventDefault();
      }
      return true;
    } else if (this.NumericKeys.indexOf(charCode) === -1) {
      event.preventDefault();
      return false;
    } else {
      return true;
    }
  }

  static HandleServiceErr(
    err: any,
    svcUtil: UtilService,
    svcToast: ToastService,
    scrollToTop: boolean = false
  ) {
    svcUtil.showLoading = false;
    svcToast.showAlert(
      ToastEnum.danger,
      UtilService.ExtractMessageFromErr(err)
    );
    if (scrollToTop) window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  static IsEmailValid(email: string) {
    if (!email) return false;
    return !!String(email)
      .toLowerCase()
      .match(
        /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/
      );
  }

  static IsValidUSDate(v: string) {
    if (!v) return false;
    return !!String(v).match(
      /^(0?[1-9]|1[0-2])\/(0?[1-9]|1\d|2\d|3[01])\/(19|20)\d{2}$/
    );
  }
  static IsAnOpenStatus(wfs: CMSCaseStatus): boolean {
    return (
      wfs === CMSCaseStatus.ActiveArbitrationBriefCreated ||
      wfs === CMSCaseStatus.ActiveArbitrationBriefNeeded ||
      wfs === CMSCaseStatus.ActiveArbitrationBriefSubmitted ||
      wfs === CMSCaseStatus.InformalInProgress ||
      wfs === CMSCaseStatus.New ||
      wfs === CMSCaseStatus.Open
    );
    //wfs === CMSCaseStatus.DetermineAuthority ||
  }

  static MergeTemplateData(
    html: string,
    templateType: NotificationType,
    src: any,
    calcVars: CalculatorVariables,
    stateAuth: Authority
  ): string {
    if (templateType !== NotificationType.NSANegotiationRequest || !src)
      return html;

    let clone = html;
    // process any tables first to strip out the compound tokens
    try {
      let rx = /<tbody .*data-mp-tbody="(\w+)"\W+.*<\/tbody>/g;
      let obj: any = Object.assign({}, src);
      if (html.match(rx)) {
        for (const v of html.matchAll(rx)) {
          if (v.length > 1) {
            // verify that src has a collection matching the data-mp attribute
            const rowSrc = v[1]; // should match a collection property name
            const coll = (obj as any)[rowSrc];
            if (coll && coll.length) {
              const tbl = document.createElement('table');
              tbl.innerHTML = v[0];
              if (!tbl.rows.length) continue;
              const newRows = [];
              const body = tbl.tBodies[0];
              for (let rec of coll) {
                const flatRec: any = UtilService.Squish(v[1], rec, obj);
                for (let i = 0; i < body.rows.length; i++) {
                  newRows.push(
                    UtilService.ReplaceHtmlTokens(
                      body.rows[i].innerHTML,
                      templateType,
                      flatRec,
                      calcVars,
                      stateAuth
                    )
                  );
                }
              }
              // replace the tBody rows with our new ones
              for (let i = 0; i < tbl.rows.length; i++) body.rows[0].remove();
              for (let n of newRows) body.insertRow().innerHTML = n;
              clone = clone.replace(v[0], body.outerHTML);
            }
          }
        }
      }

      // process the rest of the document
      html = this.ReplaceHtmlTokens(
        clone,
        templateType,
        src,
        calcVars,
        stateAuth
      );
    } catch (err) {
      console.error(err);
    }
    return html;
  }

  /** Creates new properties on target using a prefix and a source object
   * @prefix Text value to prefix onto all new properties
   * @from Source object that provides the new properties and value
   * @target The object receiving the new properties
   * @delim The delimiter placed between prefix and original property name
   * @suffix Optional value to be placed at the end. Useful for repeatedly squishing
   * into the same object and producing a running index at the end or appending another key value.
   */
  static Squish(
    prefix: string,
    src: any,
    target: any,
    delim: string = '_$_',
    suffix: string = ''
  ): any {
    let obj = Object.assign({}, target);
    for (let name of Object.getOwnPropertyNames(src))
      obj[prefix + delim + name + suffix] = src[name];

    return obj;
  }

  // TODO: Could UnSquish if necessary by just deleting all properties that start with a given prefix (and maybe delimiter)

  static PadDigit(num: number, minDigits: number = 2) {
    return num.toLocaleString('en-US', {
      minimumIntegerDigits: minDigits,
      useGrouping: false,
    });
  }

  static ParseArbitratorStats(statistics: string): IArbStats[] {
    if (statistics) {
      // parse stats
      try {
        const stats: IArbStats[] = JSON.parse(statistics);
        return stats;
      } catch {
        loggerCallback(
          LogLevel.Warning,
          'Unable to parse Arbitrator Statistics'
        );
      }
    }
    return [];
  }

  static ReplaceHtmlTokens(
    html: string,
    templateType: NotificationType,
    src: any,
    calcVars: CalculatorVariables,
    stateAuth: Authority
  ) {
    let rx = /(\{\S+?\})/g;
    for (const v of html.matchAll(rx)) {
      if (v.length) {
        const fld = v[0].replace('{', '').replace('}', '');
        let tmp = UtilService.GetCalculatedValue(
          fld,
          templateType,
          src,
          calcVars,
          stateAuth
        );
        if (
          typeof tmp === 'string' &&
          tmp.length > 6 &&
          (tmp.indexOf('/') > -1 || tmp.indexOf('-') > -1)
        ) {
          let tmpD = Date.parse(tmp);
          if (!Number.isNaN(tmpD))
            tmp = UtilService.ToUSDateString(new Date(tmp));
        } else if (tmp.toLocaleDateString) {
          tmp = UtilService.ToUSDateString(tmp);
        }
        html = html.replaceAll(v[0], tmp);
      }
    }
    return html;
  }

  static RoundMoney(m: number) {
    return Math.floor(m * 100 + 0.5) / 100;
  }

  static SortByArbitrationCaseId(a: IArbitrationCase, b: IArbitrationCase) {
    if (a.arbitrationCaseId < b.arbitrationCaseId) return 1;
    if (a.arbitrationCaseId > b.arbitrationCaseId) return -1;
    return 0;
  }

  static SortByClaimCPTCode(a: ClaimCPT, b: ClaimCPT) {
    if (a.cptCode < b.cptCode) return -1;
    if (b.cptCode < a.cptCode) return 1;
    return 0;
  }

  static SortByCodeNumber(
    a: PlaceOfServiceCode,
    b: PlaceOfServiceCode
  ): number {
    if ((a.codeNumber ?? 0) < (b.codeNumber ?? 0)) return -1;
    if ((a.codeNumber ?? 0) > (b.codeNumber ?? 0)) return 1;
    return 0;
  }

  static SortByCreatedOn(a: ICreatedOn, b: ICreatedOn): number {
    if ((a.createdOn ?? 0) < (b.createdOn ?? 0)) return -1;
    if ((a.createdOn ?? 0) > (b.createdOn ?? 0)) return 1;
    return 0;
  }

  static SortByCreatedOnDesc(a: ICreatedOn, b: ICreatedOn): number {
    if ((a.createdOn ?? 0) < (b.createdOn ?? 0)) return 1;
    if ((a.createdOn ?? 0) > (b.createdOn ?? 0)) return -1;
    return 0;
  }

  static SortByEmail(a: { email: string }, b: { email: string }): number {
    if (a.email.toLowerCase() < b.email.toLowerCase()) return -1;
    else if (a.email.toLowerCase() > b.email.toLowerCase()) return 1;
    else return 0;
  }

  static SortById(a: { id: number }, b: { id: number }): number {
    if (a.id < b.id) return -1;
    else if (a.id > b.id) return 1;
    else return 0;
  }

  static SortByName(a: IName, b: IName): number {
    if (a.name.toLowerCase() < b.name.toLowerCase()) return -1;
    else if (a.name.toLowerCase() > b.name.toLowerCase()) return 1;
    else return 0;
  }

  static SortByOrder(a: IOrder, b: IOrder): number {
    if (a.order < b.order) return -1;
    else if (a.order > b.order) return 1;
    else return 0;
  }

  static SortByService(
    a: AuthorityBenchmarkDetails,
    b: AuthorityBenchmarkDetails
  ): number {
    if (a.service.toLowerCase() < b.service.toLowerCase()) return -1;
    else if (a.service.toLowerCase() > b.service.toLowerCase()) return 1;
    else return 0;
  }

  static SortByPatientName(a: IPatientInfo, b: IPatientInfo): number {
    if (a.patientName.toLowerCase() < b.patientName.toLowerCase()) return -1;
    else if (a.patientName.toLowerCase() > b.patientName.toLowerCase())
      return 1;
    else return 0;
  }

  static SortByPayorName(a: IPayorName, b: IPayorName): number {
    if (a.payorName.toLowerCase() < b.payorName.toLowerCase()) return -1;
    else if (a.payorName.toLowerCase() > b.payorName.toLowerCase()) return 1;
    else return 0;
  }

  static SortByProviderName(a: ProviderVM, b: ProviderVM): number {
    if (a.providerName.toLowerCase() < b.providerName.toLowerCase()) return -1;
    else if (a.providerName.toLowerCase() > b.providerName.toLowerCase())
      return 1;
    else return 0;
  }

  static SortByTrackingFieldName(
    a: AuthorityTrackingDetail,
    b: AuthorityTrackingDetail
  ): number {
    if (a.trackingFieldName.toLowerCase() < b.trackingFieldName.toLowerCase())
      return -1;
    else if (
      a.trackingFieldName.toLowerCase() > b.trackingFieldName.toLowerCase()
    )
      return 1;
    else return 0;
  }

  static SortByUpdatedOn(a: IModifier, b: IModifier): number {
    if ((a.updatedOn ?? 0) < (b.updatedOn ?? 0)) return 1;
    if ((a.updatedOn ?? 0) > (b.updatedOn ?? 0)) return -1;
    return 0;
  }

  static SortByUpdatedOnDesc(a: IModifier, b: IModifier): number {
    if ((a.updatedOn ?? 0) < (b.updatedOn ?? 0)) return -1;
    if ((a.updatedOn ?? 0) > (b.updatedOn ?? 0)) return 1;
    return 0;
  }

  static SortSimple(a: string, b: string): number {
    if (a.toLowerCase() < b.toLowerCase()) return -1;
    else if (a.toLowerCase() > b.toLowerCase()) return 1;
    else return 0;
  }

  static StringifyCaseNotes(notes: BaseNote[]): string {
    let value = '';
    if (notes.length) {
      notes.sort(UtilService.SortByUpdatedOn);
      for (const n of notes) {
        value += `* (${n.updatedOn?.toLocaleString()} by ${n.updatedBy}) - ${
          n.details
        }\n`;
      }
    }
    return value.trim();
  }

  static SyncTrackingToCase(
    t: AuthorityTrackingDetail[] | undefined,
    obj: any,
    arb: ArbitrationCase | null
  ) {
    if (!obj || !t || !t.length || !arb) return;

    for (let d of t.filter((b) => b.mapToCaseField)) {
      try {
        if (arb.hasOwnProperty(d.mapToCaseField)) {
          switch (d.trackingFieldType) {
            case 'Date':
              if (!!obj[d.trackingFieldName]) {
                const value = new Date(obj[d.trackingFieldName]);
                (arb as any)[d.mapToCaseField] = UtilService.IsDateValid(value)
                  ? value
                  : null;
              } else {
                (arb as any)[d.mapToCaseField] = null; //obj[d.trackingFieldName];
              }
              break;
            case 'Number':
              (arb as any)[d.mapToCaseField] = obj[d.trackingFieldName]
                ? parseFloat(obj[d.trackingFieldName])
                : 0.0;
              break;
            default:
              (arb as any)[d.mapToCaseField] = obj[d.trackingFieldName].toString
                ? obj[d.trackingFieldName].toString()
                : '';
          }
        } else {
          loggerCallback(
            LogLevel.Warning,
            `Unable to sync Tracking field ${d.trackingFieldName}. Case field ${d.mapToCaseField} not found.`
          );
        }
      } catch (err) {
        loggerCallback(
          LogLevel.Error,
          `Unable to sync Tracking field ${d.trackingFieldName} to Case field ${d.mapToCaseField}. Error to follow:`
        );
        loggerCallback(LogLevel.Error, err);
      }
    }
  }

  static TransformTrackingObject(
    t: AuthorityTrackingDetail[] | undefined,
    obj: any,
    convertDatesToStrings: boolean = false
  ) {
    if (!obj) {
      return obj;
    }

    if (!t) {
      // TODO: attempt a default transformation based on which parsing actions pass - this really should never be used but offers a possible fallback for older records
      return obj;
    } else {
      // copy any matching properties on the existing object to the new one and then properly set its typed value
      const newTracking = UtilService.CreateTrackingObject(t);

      // create an empty tracking object based on current configuration
      for (let d of t) {
        switch (d.trackingFieldType) {
          case 'Date':
            if (!!obj[d.trackingFieldName]) {
              const value = new Date(obj[d.trackingFieldName]);
              newTracking[d.trackingFieldName] = UtilService.IsDateValid(value)
                ? value
                : null;
            } else {
              newTracking[d.trackingFieldName] = null;
            }
            break;
          case 'Number':
            newTracking[d.trackingFieldName] = obj[d.trackingFieldName]
              ? parseFloat(obj[d.trackingFieldName])
              : 0.0;
            break;
          default:
            newTracking[d.trackingFieldName] = obj[d.trackingFieldName].toString
              ? obj[d.trackingFieldName].toString()
              : '';
        }
      }
      return newTracking;
    }
  }

  static UpdateTrackingCalculations(
    t: AuthorityTrackingDetail[] | undefined,
    obj: any,
    convertDatesToStrings: boolean = false,
    target: ArbitrationCase | AuthorityDisputeVM | null = null
  ) {
    if (!obj) {
      return;
    }

    if (!t) return; // TODO: attempt a default transformation based on which parsing actions pass - this likely shouldn't ever be used but offers a possible fallback for older records

    // convert any date strings to actual local date
    for (let d of t.filter(
      (v) => !v.referenceFieldName && v.trackingFieldType === 'Date'
    )) {
      if (
        !!obj[d.trackingFieldName] &&
        typeof obj[d.trackingFieldName] == 'string'
      )
        obj[d.trackingFieldName] = new Date(obj[d.trackingFieldName]);
    }
    // Create a tracking object based on Authority tracking configurations
    let changesFound = true;
    let iterations = 0;
    while (changesFound && iterations < 10) {
      // failsafe
      iterations++;
      changesFound = false;
      for (let d of t.filter((v) => !!v.referenceFieldName)) {
        switch (d.trackingFieldType) {
          case 'Date':
            // allows referencing the ArbitrationCase object for values
            const pv = (target as any)[d.referenceFieldName];
            let value: Date | undefined =
              !!target && !!pv ? new Date(pv) : undefined;

            // if the referenced field is contained in the tracking object itself, use that instead
            if (!!obj[d.referenceFieldName])
              value = new Date(obj[d.referenceFieldName]);

            if (
              !!value &&
              UtilService.IsDateValid(value) &&
              !!d.unitsFromReference
            ) {
              const calc = UtilService.AddDays(
                value,
                d.unitsFromReference,
                d.unitsType
              );
              if (
                !obj[d.trackingFieldName] ||
                calc.valueOf() !== value.valueOf()
              ) {
                obj[d.trackingFieldName] = calc;
                changesFound = true;
              }
            } else {
              if (obj[d.trackingFieldName] !== null) {
                obj[d.trackingFieldName] = null;
                changesFound = true;
              }
            }
            break;
          case 'Number':
            let calc = 0.0;
            if (!!obj[d.referenceFieldName] && d.unitsFromReference) {
              calc =
                parseFloat(obj[d.referenceFieldName]) + d.unitsFromReference;
            }
            if (obj[d.trackingFieldName] !== calc) {
              obj[d.trackingFieldName] = 0.0;
              changesFound = true;
            }
            break;
          default:
            obj[d.trackingFieldName] = obj[d.referenceFieldName].toString
              ? obj[d.referenceFieldName].toString()
              : '';
        }
      }
    }
  }

  static ToUSDateString(date: Date) {
    return UtilService.DatePipe.transform(date, 'MM/dd/yyyy');
  }

  static IsDateValid(date: any, max: number = 5): boolean {
    let tf = date instanceof Date && !Number.isNaN(date);
    tf = tf
      ? Math.abs(new Date().getFullYear() - date.getFullYear()) < max
      : false;
    return tf;
  }

  static ToTitleCase(str: string) {
    'use strict';
    var smallWords =
      /^(a|an|and|as|at|but|by|en|for|if|in|nor|of|on|or|per|the|to|v.?|vs.?|via)$/i;
    var alphanumericPattern = /([A-Za-z0-9\u00C0-\u00FF])/;
    var wordSeparators = /([ :–—-])/;

    return str
      .split(wordSeparators)
      .map(function (current, index, array) {
        if (
          /* Check for small words */
          current.search(smallWords) > -1 &&
          /* Skip first and last word */
          index !== 0 &&
          index !== array.length - 1 &&
          /* Ignore title end and subtitle start */
          array[index - 3] !== ':' &&
          array[index + 1] !== ':' &&
          /* Ignore small words that start a hyphenated phrase */
          (array[index + 1] !== '-' ||
            (array[index - 1] === '-' && array[index + 1] === '-'))
        ) {
          return current.toLowerCase();
        }

        /* Ignore intentional capitalization */
        if (current.substring(1).search(/[A-Z]|\../) > -1) {
          return current;
        }

        /* Ignore URLs */
        if (array[index + 1] === ':' && array[index + 2] !== '') {
          return current;
        }

        /* Capitalize the first letter */
        return current.replace(alphanumericPattern, function (match) {
          return match.toUpperCase();
        });
      })
      .join('');
  }
}
