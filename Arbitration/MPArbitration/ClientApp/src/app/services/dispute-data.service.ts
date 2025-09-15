import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { environment } from 'src/environments/environment';
import { catchError, map, switchMap, take } from 'rxjs/operators';
import { loggerCallback } from '../app.module';
import { LogLevel } from '@azure/msal-browser';
import { DetailedDispute } from '../model/detailed-dispute';
import { DetailedDisputeCPTLog } from '../model/detailed-dispute-cpt-log';
import { DisputesData } from '../model/disputes-data';

@Injectable({
  providedIn: 'root',
})
export class DisputeDataService {
  private _baseUrl = '';

  static headerDict = {
    'Content-Type': 'application/json',
    Accept: 'application/json',
    'Access-Control-Allow-Headers': 'Content-Type',
  };

  static REQUEST_HEADERS_TEXT = new HttpHeaders()
    .set('Content-Type', 'text/plain;charset=utf-8')
    .set('Accept', 'text/plain')
    .set('Access-Control-Allow-Headers', 'Content-Type');

  static REQUEST_OPTIONS_JSON = {
    headers: new HttpHeaders(DisputeDataService.headerDict),
  };

  constructor(private http: HttpClient) {
    //, @Inject('BASE_URL') baseUrl: string
    this._baseUrl = environment.redirectUrl; //baseUrl;
  }

  getDisputeList(
    page: number,
    size: number,
    disputeNumber: string,
    customer: string,
    disputeStatus: string,
    entity: string,
    certifiedEntity: string,
    briefApprover: string,
    providerNPI: string,
    arbitId: string,
    briefDueDateFrom: any,
    briefDueDateTo: any
  ) {
    let params = new HttpParams()
      .set('PageNumber', page)
      .set('PageSize', size)
      .set('DisputeNumber', disputeNumber)
      .set('Customer', customer)
      .set('DisputeStatus', disputeStatus)
      .set('Entity', entity)
      .set('CertifiedEntity', certifiedEntity)
      .set('BriefApprover', briefApprover)
      .set('EntityNPI', providerNPI)
      .set('ArbitID', arbitId)
      .set('BriefDueDateFrom', briefDueDateFrom)
      .set('BriefDueDateTo', briefDueDateTo);

    const r$ = this.http
      .get<DisputesData[]>(`${this._baseUrl}/api/Dispute/getDisputeList`, {
        params,
        headers: new HttpHeaders(DisputeDataService.headerDict),
      })
      .pipe(
        switchMap((data: any) => of(data)),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Disputes:');
          loggerCallback(LogLevel.Error, err);
          // return of(new DisputesData());
          return throwError(err);
        })
      );
    return r$;
  }

  getDisputeMasterCustomer() {
    const r$ = this.http
      .get<{
        data: any[];
      }>(
        `${this._baseUrl}/api/Dispute/getDisputeMasterCustomer`,
        DisputeDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((resp) => of(resp.data)),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Customer records');
          loggerCallback(LogLevel.Error, err);
          // return of(new Array());
          return throwError(err);
        })
      );
    return r$;
  }

  getDisputeMasterDisputeStatus() {
    const r$ = this.http
      .get<{
        data: any[];
      }>(
        `${this._baseUrl}/api/Dispute/getDisputeMasterDisputeStatus`,
        DisputeDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((resp) => of(resp.data)),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Dispute status:');
          loggerCallback(LogLevel.Error, err);
          // return of(new Array());
          return throwError(err);
        })
      );
    return r$;
  }

  getDisputeMasterBriefApprover() {
    const r$ = this.http
      .get<{
        data: any[];
      }>(
        `${this._baseUrl}/api/Dispute/getBriefApprover/users`,
        DisputeDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((resp) => of(resp.data)),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Brief approver:');
          loggerCallback(LogLevel.Error, err);
          // return of(new Array());
          return throwError(err);
        })
      );
    return r$;
  }

  getDisputeMasterEntity() {
    const r$ = this.http
      .get<{
        data: any[];
      }>(
        `${this._baseUrl}/api/Dispute/getDisputeMasterEntity`,
        DisputeDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((resp) => of(resp.data)),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Dispute entity:');
          loggerCallback(LogLevel.Error, err);
          // return of(new Array());
          return throwError(err);
        })
      );
    return r$;
  }

  getDisputeMasterCertifiedEntity() {
    const r$ = this.http
      .get<{
        data: any[];
      }>(
        `${this._baseUrl}/api/Dispute/getDisputeMasterCertifiedEntity`,
        DisputeDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((resp) => of(resp.data)),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not load Dispute certified entity:'
          );
          loggerCallback(LogLevel.Error, err);
          // return of(new Array());
          return throwError(err);
        })
      );
    return r$;
  }

  getDisputeMasterProviderNPI() {
    const r$ = this.http
      .get<{
        data: any[];
      }>(
        `${this._baseUrl}/api/Dispute/getDisputeMasterProviderNPI`,
        DisputeDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((resp) => of(resp.data)),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not load Dispute provider npi:'
          );
          loggerCallback(LogLevel.Error, err);
          // return of(new Array());
          return throwError(err);
        })
      );
    return r$;
  }

  getDisputeArbitIds(searchInput = ''): Observable<any> {
    let uri = `${this._baseUrl}/api/Dispute/getDisputeArbitIds`;
    if (searchInput != '') {
      uri += `?searchInput=${searchInput}`;
    }
    const r$ = this.http
      .get<{
        data: any[];
      }>(uri, DisputeDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((resp) => of(resp.data)),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not load Dispute provider npi:'
          );
          loggerCallback(LogLevel.Error, err);
          // return of(new Array());
          return throwError(err);
        })
      );
    return r$;
  }

  getDisputeMasterServiceLine() {
    const r$ = this.http
      .get<{
        data: any[];
      }>(
        `${this._baseUrl}/api/Dispute/getDisputeMasterServiceLine`,
        DisputeDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((resp) => of(resp.data)),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not load Dispute service line:'
          );
          loggerCallback(LogLevel.Error, err);
          // return of(new Array());
          return throwError(err);
        })
      );
    return r$;
  }
  updateDisputeStatusByID(wf: any) {
    let uri = `${this._baseUrl}/api/Dispute/UpdateDisputeStatusByID/${wf.disputeId}?disputeStatus=${wf.disputeStatus}`;

    if (wf.briefApprover != null) {
      uri += `&briefApprover=${wf.briefApprover}`;
    }

    return this.http.put(uri, {}, DisputeDataService.REQUEST_OPTIONS_JSON);
  }

  getDisputeDetail(id: string) {
    const r$ = this.http
      .get<{
        data: DetailedDispute;
      }>(
        `${this._baseUrl}/api/Dispute/getDisputeByDisputeNumber/${id}`,
        DisputeDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((resp: any) => {
          if (resp.statusCode === 404) return throwError(resp);
          return of(new DetailedDispute(resp.data));
        }),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Dispute');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  updateDetailedDispute(dispute: DetailedDispute) {
    // for (let f of dispute.fees) f.baseFee = undefined;

    const uri = `${this._baseUrl}/api/Dispute/updateDispute`;
    const r$ = this.http
      .put(uri, dispute, DisputeDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(data)),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not construct a new DetailedDispute'
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  deleteDisputeCPTbyId(disputeId: number, cptId: number): Observable<any> {
    if (!disputeId || !cptId)
      return throwError(new Error('Bad upload parameters'));
    // var url = `${this._baseUrl}/api/Dispute/deleteDisputeCPTbyId?disputeId=${disputeId}&cptId=${cptId}`;
    var url = `${this._baseUrl}/api/Dispute/deleteDisputeCPTbyId/${cptId}`;
    console.warn('calling :' + url);
    return this.http.delete(url, { observe: 'response' });
  }

  getDisputeLogsById(id: number) {
    if (!Number.isInteger(id) || id < 1) {
      return throwError(new Error('Invalid Dispute Id'));
    }

    const r$ = this.http
      .get<{
        data: DetailedDisputeCPTLog[];
      }>(
        `${this._baseUrl}/api/Dispute/getDisputeLogsById/${id}`,
        DisputeDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((resp: any) => {
          if (resp.statusCode === 404) return throwError(resp);
          return of(resp.data);
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not load DetailedDisputeCPTLog records'
          );
          loggerCallback(LogLevel.Error, err);
          // return of(new Array<DetailedDisputeCPTLog>());
          return throwError(err);
        })
      );
    return r$;
  }
}
