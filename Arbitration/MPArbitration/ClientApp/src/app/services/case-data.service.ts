import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  BehaviorSubject,
  EMPTY,
  from,
  Observable,
  Observer,
  of,
  throwError,
} from 'rxjs';
import { ArbitrationCase } from '../model/arbitration-case';
import { TDIRequestDetails } from '../model/tdi-request-details';
import { CalculatorVariables } from '../model/calculator-variables';
import { Negotiator } from '../model/negotiator';
import { environment } from 'src/environments/environment';
import { ImportFieldConfig } from '../model/import-field-config';
import { CaseWorkflowParams } from '../model/case-workflow-params';
import { catchError, map, switchMap, take } from 'rxjs/operators';
import { CaseFile, CaseFileVM } from '../model/case-file';
import { ToastService } from './toast.service';
import { ToastEnum } from '../model/toast-enum';
import { AppUser } from '../model/app-user';
import { loggerCallback } from '../app.module';
import { LogLevel } from '@azure/msal-browser';
import { AuthorityDisputeNote, Note } from '../model/note';
import { Authority } from '../model/authority';
import { Payor } from '../model/payor';
import { CaseLog } from '../model/case-log';
import { Arbitrator } from '../model/arbitrator';
import { Customer } from '../model/customer';
import { AuthorityTrackingDetail } from '../model/authority-tracking-detail';
import { AuthorityBenchmarkDetails } from '../model/authority-benchmark-details';
import { Holiday } from '../model/holiday';
import { BenchmarkDataset } from '../model/benchmark-dataset';
import { BenchmarkDataItem } from '../model/benchmark-data-item';
import { AppHealth, AppHealthDetail } from '../model/app-health';
import { Notification } from '../model/notification';
import { NotificationType } from '../model/notification-type-enum';
import { ProcedureCode } from '../model/procedure-code';
import { Entity } from '../model/entity';
import { ArbitrationCaseVM } from '../model/arbitration-case-vm';
import { Template } from '../model/template';
import {
  MasterDataException,
  MasterDataExceptionType,
} from '../model/master-data-exception';
import { EntityVM } from '../model/entity-vm';
import { CaseArchive } from '../model/case-archive';
import { PayorGroup } from '../model/payor-group';
import { PayorGroupResponse } from '../model/payor-group-response';
import { AuthorityPayorGroupExclusion } from '../model/authority-payor-group-exclusion';
import { AppSettings } from '../model/app-settings';
import { CaseSettlement } from '../model/case-settlement';
import { JobQueueItem } from '../model/job-queue-item';
import { ArbitratorType } from '../model/arbitrator-type-enum';
import { AuthorityFee } from '../model/authority-fee';
import { ArbitratorFee } from '../model/arbitrator-fee';
import { ProviderVM } from '../model/provider-vm';
import {
  AuthorityDispute,
  AuthorityDisputeInit,
  AuthorityDisputeVM,
} from '../model/authority-dispute';
import {
  AuthorityDisputeAttachment,
  EMRClaimAttachment,
} from '../model/emr-claim-attachment';
import { PayorAddress } from '../model/payor-address';
import { AuthorityDisputeLog } from '../model/authority-dispute-log';
import { PlaceOfServiceCode } from '../model/place-of-service-code';
import { UtilService } from './util.service';
import { WorkQueueName } from '../model/work-queue-name-enum';
import { AuthorityDisputeWorkItem } from '../model/authority-dispute-work-item';
import { Disputes } from '../model/disputes-data';
import { Dispute } from '../model/dispute';

@Injectable({
  providedIn: 'root',
})
export class CaseDataService {
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
    headers: new HttpHeaders(CaseDataService.headerDict),
  };

  constructor(private http: HttpClient) {
    //, @Inject('BASE_URL') baseUrl: string
    this._baseUrl = environment.redirectUrl; //baseUrl;
  }

  //get importFieldConfigs() {
  //  return this._importFieldConfigs.asObservable();
  //}
  checkForAuthorityCase(key: string, id: string): Observable<number> {
    if (!key || !id) {
      return throwError(new Error('Invalid parameters'));
    }

    return this.http.get<number>(
      `${this._baseUrl}/api/cases/chkcase?key=${key}&id=${id}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  checkForActivePayorClaimNumber(
    id: string,
    exclude: number = 0
  ): Observable<number> {
    if (!id) {
      return throwError(new Error('Invalid parameter'));
    }

    return this.http.get<number>(
      `${this._baseUrl}/api/cases/claim?id=${id}&exclude=${exclude}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }
  /*
  cleanDupes() {
    const headers = CaseDataService.REQUEST_HEADERS_TEXT;
    return this.http.get(`${this._baseUrl}/api/cases/mergeall`,{ headers, responseType:'text' as const});
  }
  */
  fixNotifications() {
    const headers = CaseDataService.REQUEST_HEADERS_TEXT;
    return this.http.get(`${this._baseUrl}/api/notifications/regen`, {
      headers,
      responseType: 'text' as const,
    });
  }

  createAppVars(v: CalculatorVariables) {
    if (v.id !== 0 || !v.serviceLine)
      return throwError('Bad id or serviceLine');

    const r$ = this.http
      .post<CalculatorVariables>(
        `${this._baseUrl}/api/arbitration/app/vars`,
        v,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new CalculatorVariables(data));
        }),
        catchError((err) => {
          loggerCallback(LogLevel.Error, `Error creating CalculatorVariables`);
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  createArbitrationCase(pa: ArbitrationCase) {
    if (!pa || pa.id > 0)
      return throwError(
        new Error('Invalid or pre-existing case. Try update instead.')
      );
    if (pa.NSARequestDiscount === null)
      return throwError(
        new Error(
          'Unexpected internal condition:  NSARequestDiscount is NULL. Cannot save.'
        )
      );

    return this.http
      .post<ArbitrationCase>(`${this._baseUrl}/api/cases`, pa)
      .pipe(
        switchMap((data) => {
          //let notFound = true;
          const rec = new ArbitrationCase(data);
          return of(rec);
          // if data store already contains the record, replace it
          /*
        this.dataStore.cases.forEach((item, index) => {
          if (item.id === rec.id) {
            this.dataStore.cases[index] = rec;
            notFound = false;
          }
        });

        if (notFound) {
          this.dataStore.cases.push(rec);
        }

        this._cases.next(this.dataStore.cases);
        if (output$)
          output$.next(rec);
        */
        }),
        catchError((err) => {
          loggerCallback(LogLevel.Error, `Error creating ArbitrationCase`);
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createArbitrator(p: Arbitrator) {
    return this.http
      .post<Arbitrator>(
        `${this._baseUrl}/api/arbitrators`,
        p,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Arbitrator(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating Arbitrator with email ${p.email}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createAuthorityBenchmarkDetail(a: Authority, d: AuthorityBenchmarkDetails) {
    // TODO: Add this type of error handling to the rest of the service
    if (a.id < 1 || d.benchmarkDatasetId < 1 || d.id !== 0)
      return throwError(new Error('Invalid Authority or Benchmark data'));

    return this.http
      .post<AuthorityBenchmarkDetails>(
        `${this._baseUrl}/api/authorities/item/byid/${a.id}/benchmarks`,
        d,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new AuthorityBenchmarkDetails(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating AuthorityBenchmarkDetails for Authority ${d.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createAuthorityDispute(dispute: AuthorityDispute) {
    // basic triage before bothering the server
    if (dispute.disputeCPTs.length > 0)
      return throwError(
        'DisputeCPTs cannot be used to initialize an AuthorityDispute.'
      );
    if (dispute.cptViewmodels.length === 0)
      return throwError('At least one AuthorityDisputeCPTVM is required.');
    if (dispute.cptViewmodels.find((v) => !v.claimCPT || v.claimCPT.id < 1))
      return throwError('All DisputeCPTs require a valid, attached ClaimCPT');
    if (
      !dispute.authorityCaseId ||
      !dispute.authorityId ||
      !dispute.submissionDate
    )
      return throwError(
        'Missing authorityCaseId, authorityId or submissionDate!'
      );

    const uri = `${this._baseUrl}/api/batching`;
    const d = JSON.parse(JSON.stringify(dispute));
    if (d.fees?.length > 0) {
      for (let f of d.fees) {
        delete f.baseFee;
      }
    }
    const r$ = this.http.post<AuthorityDispute>(uri, d).pipe(
      switchMap((data) => of(new AuthorityDispute(data))),
      catchError((err) => {
        loggerCallback(
          LogLevel.Error,
          'Could not construct a new AuthorityDispute'
        );
        loggerCallback(LogLevel.Error, err);
        return throwError(err);
      })
    );
    return r$;
  }

  createArbitratorFee(
    a: Arbitrator,
    f: ArbitratorFee
  ): Observable<ArbitratorFee> {
    if (!a.id || f.arbitratorId !== a.id)
      return throwError('The Arbitrator id values do not match!');
    return this.http
      .post<ArbitratorFee>(
        `${this._baseUrl}/api/arbitrators/fees`,
        f,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new ArbitratorFee(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating ArbitratorFee ${f.feeName} for Arbitrator ${a.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createAuthorityFee(a: Authority, f: AuthorityFee): Observable<AuthorityFee> {
    if (!a.id || f.authorityId !== a.id)
      return throwError('The Authority id values do not match!');
    return this.http
      .post<AuthorityFee>(
        `${this._baseUrl}/api/authorities/fees`,
        f,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new AuthorityFee(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating AuthorityFee ${f.feeName} for Authority ${a.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createAuthorityTrackingDetail(a: Authority, d: AuthorityTrackingDetail) {
    return this.http
      .post<AuthorityTrackingDetail>(
        `${this._baseUrl}/api/authorities/item/byid/${a.id}/tracking`,
        d,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new AuthorityTrackingDetail(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating AuthorityTrackingDetail field ${d.trackingFieldName} for Authority ${a.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createBenchmarkDataset(p: BenchmarkDataset) {
    return this.http
      .post<BenchmarkDataset>(
        `${this._baseUrl}/api/benchmark/source`,
        p,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new BenchmarkDataset(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating Benchmark Dataset with name ${p.name}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createCustomer(p: Customer) {
    return this.http
      .post<Customer>(
        `${this._baseUrl}/api/customers`,
        p,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Customer(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating Customer with name ${p.name}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createDisputeNote(n: AuthorityDisputeNote) {
    if (n.authorityDisputeId < 1 || n.id !== 0)
      return throwError('Invalid authorityDisputeId or note id!');
    return this.http
      .post<AuthorityDisputeNote>(
        `${this._baseUrl}/api/notes/dispute`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new AuthorityDisputeNote(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating AuthorityDisputeNote for AuthorityDisputeId ${n.authorityDisputeId}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createEntity(n: Entity) {
    if (n.id !== 0)
      return throwError(new Error('The id for a new record must be zero!'));
    if (n.customerId < 1) return throwError(new Error('Invalid customerId!'));
    return this.http
      .post<Entity>(
        `${this._baseUrl}/api/customers/entity`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Entity(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating a new Entity for Customer ${n.customerId}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createMultiSettlement(settlements: CaseSettlement[]) {
    let tmp = settlements.find(
      (v) =>
        v.arbitrationCaseId < 1 ||
        v.id !== 0 ||
        !v.authorityId ||
        v.authorityId < 1 ||
        v.payorId < 1 ||
        v.isDeleted
    );
    if (!!tmp)
      return throwError(
        new Error(
          'Invalid id, isDeleted, payorId, or authorityId for CaseSettlement object!'
        )
      );

    tmp = settlements.find(
      (v) =>
        !v.arbitrationDecisionDate ||
        !v.prevailingParty ||
        !v.arbitratorReportSubmissionDate ||
        !v.partiesAwardNotificationDate
    );
    if (!!tmp)
      return throwError(new Error('CaseSettlement validation failed!'));

    tmp = settlements.find(
      (v) =>
        !v.caseSettlementCPTs.length ||
        !!v.caseSettlementCPTs.find((b) => b.isDeleted)
    );
    if (!!tmp)
      return throwError(
        new Error(
          'CaseSettlementCPTs validation failed. Was a deleted one included by mistake?'
        )
      );

    return this.http
      .post<CaseSettlement[]>(
        `${this._baseUrl}/api/settlements/multi`,
        settlements,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new CaseSettlement(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error adding new CaseSettlement objects as a batch.`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createNegotiator(n: Negotiator) {
    return this.http
      .post<Negotiator>(
        `${this._baseUrl}/api/arbitration/app/negotiators`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Negotiator(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating Negotiator for PayerId ${n.payorId}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createNote(cid: number, n: Note) {
    return this.http
      .post<Note>(
        `${this._baseUrl}/api/notes/claim/${cid}`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Note(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating Note for ArbitrationCaseId ${cid}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createNotification(n: Notification) {
    return this.http
      .post<Notification>(
        `${this._baseUrl}/api/notifications`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Notification(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating Notification for ArbitrationCaseId ${n.arbitrationCaseId}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createNotificationBatch(n: Notification[]) {
    if (!n.length) return throwError('Cannot batch an empty collection');

    return this.http.post(`${this._baseUrl}/api/notifications/batch`, n, {
      responseType: 'text' as const,
    });
  }

  createPayor(p: Payor) {
    return this.http
      .post<Payor>(
        `${this._baseUrl}/api/payors`,
        p,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Payor(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating Payor with name ${p.name}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  /** Create an AuthorityPayorGroupException that marks a Payor Group as ineligible in certain jurisdictions
   *
   * @param id AuthorityId
   * @param group AuthorityPayorGroupException
   * @returns AuthorityPayorGroupException
   */
  createAuthorityPayorGroupExclusion(
    id: number,
    group: AuthorityPayorGroupExclusion
  ) {
    return this.http
      .post<AuthorityPayorGroupExclusion>(
        `${this._baseUrl}/api/authorities/item/byid/${id}/pge`,
        group,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new AuthorityPayorGroupExclusion(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating AuthorityPayorGroupException for PayorId ${id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createPayorGroup(id: number, group: PayorGroup) {
    return this.http
      .post<PayorGroup>(
        `${this._baseUrl}/api/payors/${id}/groups`,
        group,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new PayorGroup(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating PayorGroup for PayorId ${id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createTemplate(t: Template) {
    if (t.id !== 0)
      return throwError(
        new Error('Invalid Id! Did you mean to call updateTemplate?')
      );
    if (!t.name || !t.html)
      return throwError(new Error('Name and HTML are required fields!'));

    return this.http
      .post<Template>(
        `${this._baseUrl}/api/templates`,
        t,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Template(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating Template with name ${t.name}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createUser(u: AppUser) {
    return this.http
      .post<AppUser>(`${this._baseUrl}/api/arbitration/app/users`, u)
      .pipe(
        switchMap((data) => {
          if (data && data.id) {
            return of(new AppUser(data));
          } else {
            return of(u);
          }
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error creating AppUser with email ${u.email}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  deleteAuthorityDisputeFee(id: number) {
    if (id < 1)
      return throwError(
        new Error('deleteAuthorityDisputeFee: Missing or invalid identifier')
      );
    return this.http.delete<void>(
      `${this._baseUrl}/api/batching/${id}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  deleteAuthorityPayorGroupExclusion(g: AuthorityPayorGroupExclusion) {
    if (g.id < 1 || g.authorityId < 1 || g.payorId < 1)
      return throwError(
        new Error(
          'deleteAuthorityPayorGroupExclusion: Missing or invalid identifier'
        )
      );
    return this.http.delete<void>(
      `${this._baseUrl}/api/authorities/item/byid/${g.authorityId}/pge/${g.id}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  deleteAuthorityBenchmark(authorityId: number, authorityBenchmarkId: number) {
    if (authorityId < 1 || authorityBenchmarkId < 1)
      return throwError(
        new Error('deleteAuthorityBenchmark: Invalid identifier')
      );

    return this.http.delete<void>(
      `${this._baseUrl}/api/authorities/item/byid/${authorityId}/benchmarks/${authorityBenchmarkId}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  deleteBlob(id: number, name: string) {
    return this.http.delete<void>(
      `${this._baseUrl}/api/cases/blob?id=${id}&name=${name}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  deleteClaim(id: number, data: ArbitrationCase) {
    if (id !== data.id)
      return throwError(new Error('Id and data.Id do not match!'));
    return this.http.delete<void>(
      `${this._baseUrl}/api/cases?id=${id}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  deleteDisputeFile(ada: AuthorityDisputeAttachment) {
    if (
      ada.authorityDisputeId < 1 ||
      ada.id < 1 ||
      !ada.blobName ||
      !ada.docType
    )
      return throwError('Invalid parameters');
    return this.http.delete<void>(
      `${this._baseUrl}/api/batching/blob?aaid=${ada.id}&did=${ada.authorityDisputeId}&dt=${ada.docType}&name=${ada.blobName}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  deleteEntity(customer: Customer, data: Entity) {
    if (data.id < 1 || data.customerId < 1 || data.customerId !== customer.id)
      return throwError(new Error('Invalid or mismatched identifiers'));

    return this.http.delete<void>(
      `${this._baseUrl}/api/customers/${customer.id}/entity/${data.id}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  deleteNotification(n: Notification) {
    return this.http.delete<Notification>(
      `${this._baseUrl}/api/notifications?id=${n.id}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  deleteEntityFile(
    id: number,
    docType: string,
    name: string,
    entity: Authority | Payor
  ) {
    const ep = entity instanceof Authority ? 'authorities' : 'payors';
    return this.http.delete<void>(
      `${this._baseUrl}/api/${ep}/blob?id=${id}&cdt=${docType}&name=${name}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  deletePayorAddress(a: PayorAddress) {
    if (a.id < 1 || a.payorId < 1) return throwError('Illegal id value');
    return this.http.delete<void>(
      `${this._baseUrl}/api/payors/address?pid=${a.payorId}&aid=${a.id}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  downloadLog(a: string, name: string): Observable<Blob> {
    const options = { responseType: 'blob' as 'json' };
    return this.http
      .get<Blob>(
        `${this._baseUrl}/api/arbitration/app/import/blob/${a}?name=${name}`,
        options
      )
      .pipe(
        map((res) => {
          return new Blob([res], { type: 'text/plain' });
        })
      );
  }

  downloadPDF(id: number, name: string): Observable<Blob> {
    const options = { responseType: 'blob' as 'json' };
    return this.http
      .get<Blob>(
        `${this._baseUrl}/api/cases/blob?id=${id}&name=${name}`,
        options
      )
      .pipe(
        map((res) => {
          return new Blob([res], { type: 'application/pdf' });
        })
      );
  }

  downloadPDFForBatch(id: string, name: string): Observable<Blob> {
    const options = { responseType: 'blob' as 'json' };
    return this.http
      .get<Blob>(
        `${this._baseUrl}/api/batching/blob?id=${id}&name=${name}`,
        options
      )
      .pipe(
        map((res) => {
          return new Blob([res], { type: 'application/pdf' });
        })
      );
  }

  downloadEntityFile(
    id: number,
    name: string,
    entity: Authority | Payor
  ): Observable<Blob> {
    const options = { responseType: 'blob' as 'json' };
    const ep = entity instanceof Authority ? 'authorities' : 'payors';
    return this.http
      .get<Blob>(
        `${this._baseUrl}/api/${ep}/blob?id=${id}&name=${name}`,
        options
      )
      .pipe(
        map((res) => {
          return new Blob([res], { type: 'application/pdf' });
        })
      );
  }

  doWorkflowAction(wf: CaseWorkflowParams) {
    return this.http.post(
      `${this._baseUrl}/api/cases/wf`,
      wf,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  findAuthorityDispute(id: string) {
    const r$ = this.http
      .get<AuthorityDispute>(
        `${this._baseUrl}/api/batching/find/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new AuthorityDispute(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load AuthorityDispute');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  findDispute(id: string) {
    const r$ = this.http
      .get<Dispute>(
        `${this._baseUrl}/api/batching/find/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new Dispute(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Dispute');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  findAuthorityDisputesByCPTs(cptIds: number[], includeClosed: boolean = true) {
    if (!cptIds.length) return throwError('Invalid search parameter!');

    const filter = !includeClosed ? '?all=false' : '';
    return this.http
      .post<AuthorityDisputeVM[]>(
        `${this._baseUrl}/api/batching/rel${filter}`,
        cptIds,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) =>
          of(data ? data.map((a) => new AuthorityDisputeVM(a)) : [])
        ),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err); //of(new Array<ArbitrationCase>())
        })
      );
  }

  getAllEntities() {
    let s = `${this._baseUrl}/api/customers/entity/items`;
    const r$ = this.http
      .get<Entity[]>(s, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(data.map((a) => new Entity(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Unable to load the list of Entities across all Customers`
          );
          loggerCallback(LogLevel.Error, err);
          return of([]);
        })
      );
    return r$;
  }

  getAllEntityVMs() {
    let s = `${this._baseUrl}/api/customers/entity/items`;
    const r$ = this.http
      .get<Entity[]>(s, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(data.map((a) => new EntityVM(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Unable to load the list of Entities across all Customers`
          );
          loggerCallback(LogLevel.Error, err);
          return of([]);
        })
      );
    return r$;
  }

  getAppSettings() {
    const r$ = this.http
      .get<AppSettings>(
        `${this._baseUrl}/api/arbitration/app/settings`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new AppSettings(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load AppSettings!');
          loggerCallback(LogLevel.Error, err);
          return of(new AppSettings());
        })
      );
    return r$;
  }

  getAuthorityDispute(id: number) {
    const r$ = this.http
      .get<AuthorityDispute>(
        `${this._baseUrl}/api/batching/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new AuthorityDispute(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load AuthorityDispute');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getDispute(id: number) {
    const r$ = this.http
      .get<Dispute>(
        `${this._baseUrl}/api/batching/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new Dispute(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Dispute');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getClaimAttachmentEntries(claims: number[]) {
    if (!claims.length)
      return throwError('At least one claim id must be provided');
    if (claims.length > 20)
      return throwError('Maximum number of claim values exceeded');

    const r$ = this.http
      .post<EMRClaimAttachment[]>(`${this._baseUrl}/api/cases/files`, claims)
      .pipe(
        switchMap((data) => of(data.map((v) => new EMRClaimAttachment(v)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load EMRClaimAttachments');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getCustomersForBatching(a: Authority) {
    if (a.key.toLowerCase() !== 'nsa')
      return throwError('Only NSA is supported for batching at this time');

    const r$ = this.http
      .get<Customer[]>(
        `${this._baseUrl}/api/batching/customers?a=${a.id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new Customer(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not retrieve a list of Customers with claims ready to batch!'
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  /**
   *
   * @param a Authority
   * @param c Customer
   * @param pay Payor
   * @param npi ProviderNPI
   * @returns ArbitrationCase[] including CPTs where isIncluded is true
   */
  getClaimsForBatching(a: Authority, c: Customer, pay: Payor, npi: string) {
    if (a.key.toLowerCase() !== 'nsa')
      return throwError('Only NSA is supported for batching at this time');

    const r$ = this.http
      .get<ArbitrationCase[]>(
        `${this._baseUrl}/api/batching/claims?a=${a.id}&c=${c.id}&pay=${pay.id}&pv=${npi}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new ArbitrationCase(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not retrieve a list of ArbitrationCase ready to batch!'
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getEntitiesForBatching(a: Authority, c: Customer) {
    if (a.key.toLowerCase() !== 'nsa')
      return throwError('Only NSA is supported for batching at this time');

    const r$ = this.http
      .get<Entity[]>(
        `${this._baseUrl}/api/batching/entities?a=${a.id}&c=${c.id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new Entity(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not retrieve a list of Entities with claims ready to batch!'
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getCurrentDisputeQueueItems(
    authority: Authority | undefined,
    workQueue: WorkQueueName,
    assignedTo: string = ''
  ) {
    const authorityId = !!authority ? authority.id : 0;
    let uri = `${this._baseUrl}/api/batching/queue/current/${authorityId}/${workQueue}?at=`;
    if (!!assignedTo) {
      if (!UtilService.IsEmailValid(assignedTo))
        return throwError('Invalid email address!');
      uri += assignedTo;
    }
    const r$ = this.http
      .get<AuthorityDispute[]>(uri, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(data.map((a) => new AuthorityDispute(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not retrieve the currently assigned AuthorityDisputes for the given parameters'
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getNextQueueItem(
    authority: Authority,
    workQueue: WorkQueueName,
    assignTo: string = ''
  ) {
    let uri = `${this._baseUrl}/api/batching/queue/next/${authority.id}/${workQueue}?at=`;
    if (!!assignTo) {
      if (!UtilService.IsEmailValid(assignTo))
        return throwError('Invalid email address!');
      uri += assignTo;
    }
    const r$ = this.http
      .get<AuthorityDispute>(uri, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(new AuthorityDispute(data))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not retrieve the next AuthorityDispute for the given parameters'
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getPayorsForBatching(a: Authority, c: Customer, e: Entity, npi: string) {
    if (a.key.toLowerCase() !== 'nsa')
      return throwError('Only NSA is supported for batching at this time');

    const r$ = this.http
      .get<Payor[]>(
        `${this._baseUrl}/api/batching/payors?a=${a.id}&c=${c.id}&e=${e.id}&pv=${npi}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new Payor(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not retrieve a list of Payors with claims ready to batch!'
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getPayorTemplates(p: Payor): Observable<string> {
    if (p.id < 1) return throwError('Bad Payor id');
    const headers = CaseDataService.REQUEST_HEADERS_TEXT;
    return this.http.get(`${this._baseUrl}/api/payors/${p.id}/templates`, {
      headers,
      responseType: 'text' as const,
    });
  }

  getProvidersForBatching(a: Authority, c: Customer, e: Entity) {
    if (a.key.toLowerCase() !== 'nsa')
      return throwError('Only NSA is supported for batching at this time');

    const r$ = this.http
      .get<ProviderVM[]>(
        `${this._baseUrl}/api/batching/providers?a=${a.id}&c=${c.id}&e=${e.id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new ProviderVM(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not retrieve a list of Providers with claims ready to batch!'
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getCaseIdByAuthority(auth: string, aid: string): Observable<number> {
    if (!auth || !aid) {
      return throwError(new Error('Invalid parameters!'));
    }

    return this.http.get<number>(
      `${this._baseUrl}/api/cases/byauthority?aid=${aid}&auth=${auth}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  getCaseFiles(id: number, docType = '') {
    if (!Number.isInteger(id) || id < 1) {
      return throwError(new Error('Invalid parameters'));
    }

    let s = `${this._baseUrl}/api/cases/files/${id}`;
    if (docType) s += `?docType=${docType}`;

    const r$ = this.http
      .get<CaseFile[]>(s, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(data.map((a) => new CaseFile(a)))),
        catchError((err) => {
          let k = `Could not get CaseFile[] using id ${id}`;
          if (docType) k += ` and docType ${docType}`;
          loggerCallback(LogLevel.Error, k);
          loggerCallback(LogLevel.Error, err);
          return of([]);
        })
      );
    return r$;
  }

  getCPTDescriptions(id: number) {
    return this.http.get<ProcedureCode[]>(
      `${this._baseUrl}/api/cases/codes/${id}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  getCPTDescriptionsForDispute(id: number) {
    return this.http.get<ProcedureCode[]>(
      `${this._baseUrl}/api/batching/codes?disp=${id}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  getCurrentUser() {
    return this.http.get<AppUser>(
      `${this._baseUrl}/api/arbitration/app/currentuser`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  getArbitratorById(id: number) {
    if (id < 1) return throwError('Invalid Id!');

    let uri = `${this._baseUrl}/api/arbitrators/${id}`;
    const r$ = this.http
      .get<Authority>(uri, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(new Arbitrator(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Arbitrator by id:');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getAuthorityById(id: number, withStats: boolean = false) {
    if (id < 1) return throwError('Invalid Id!');

    let uri = `${this._baseUrl}/api/authorities/item/byid/${id}`;
    if (withStats) uri += '?stats=true';
    const r$ = this.http
      .get<Authority>(uri, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(new Authority(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Authority by id:');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getAuthorityByKey(key: string, withStats: boolean = false) {
    let uri = `${this._baseUrl}/api/authorities/item/bykey/${key}`;
    if (withStats) uri += '?stats=true';
    const r$ = this.http
      .get<Authority>(uri, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(new Authority(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Authority by key:');
          loggerCallback(LogLevel.Error, err);
          return of(null);
        })
      );
    return r$;
  }

  getHealthItems(r: string, c: Customer | undefined = undefined) {
    const reports = [
      'chg',
      'cust',
      'dob',
      'eob',
      'ent',
      'frd',
      'pat',
      'pcn',
      'prov',
      'rfc',
      'svc',
    ];
    if (reports.indexOf(r) === -1)
      return throwError(new Error('Invalid report'));
    let url = `${this._baseUrl}/api/arbitration/app/health/items?r=${r}`;
    if (!!c?.name) url += `&c=${c.name}`;
    return this.http.get<Array<AppHealthDetail>>(
      url,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  getJobQueueItem(id: number) {
    const r$ = this.http
      .get<JobQueueItem>(
        `${this._baseUrl}/api/arbitration/app/jobs/byId/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new JobQueueItem(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not get JobQueueItem:');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getJobQueueItemsByType(t: string) {
    const r$ = this.http
      .get<JobQueueItem[]>(
        `${this._baseUrl}/api/arbitration/app/jobs/byType/${t}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new JobQueueItem(a)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not get JobQueueItems:');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getMasterDataExceptions(includeResolved: boolean = false) {
    const r$ = this.http
      .get<MasterDataException[]>(
        `${this._baseUrl}/api/mde/items?resolved=${includeResolved}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new MasterDataException(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not load MasterDataExceptions:'
          );
          loggerCallback(LogLevel.Error, err);
          return of(new Array<MasterDataException>());
        })
      );
    return r$;
  }

  getPayorGroup(p: Payor, g: string) {
    if (p.id < 1 || !g) return throwError(new Error('Invalid parameters'));
    const r$ = this.http
      .get<PayorGroup>(
        `${this._baseUrl}/api/payors/${p.id}/groups/${g}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new PayorGroup(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not get PayorGroup:');
          loggerCallback(LogLevel.Error, err);
          return of(null);
        })
      );
    return r$;
  }

  /** Search CaseSettlements by ArbitrationCases.Id (not to be confused with searching by Authority Case Id!)
   */
  getSettlementsByCaseId(id: number) {
    if (!Number.isInteger(id) || id < 1)
      return throwError(new Error('Invalid Id'));

    const r$ = this.http
      .get<CaseSettlement[]>(
        `${this._baseUrl}/api/settlements/find?arbId=${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new CaseSettlement(a)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getSettlementById(id: number) {
    if (!Number.isInteger(id) || id < 1)
      return throwError(new Error('Invalid Id'));

    const r$ = this.http
      .get<CaseSettlement>(
        `${this._baseUrl}/api/settlements/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new CaseSettlement(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getSystemHealth(c: Customer | null = null) {
    let url = `${this._baseUrl}/api/arbitration/app/health`;
    if (!!c?.name) url += `?c=${c.name}`;
    return this.http.get<AppHealth>(url, CaseDataService.REQUEST_OPTIONS_JSON);
  }

  getTemplateById(id: number) {
    if (!Number.isInteger(id) || id < 1)
      return throwError(new Error('Invalid Id'));

    const r$ = this.http
      .get<Template>(
        `${this._baseUrl}/api/templates/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new Template(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  getUnsentNotifications(
    customer: string = '',
    ntype: NotificationType = NotificationType.Unknown
  ) {
    let uri = `${this._baseUrl}/api/notifications/unsent?NoHTML=true`;
    let q = '?';
    if (customer) {
      q += `customer=${customer}`;
    }
    if (ntype !== NotificationType.Unknown) {
      q += q.length > 1 ? '&' : '';
      q += `t=${ntype}`;
    }
    const r$ = this.http
      .get<Notification[]>(
        uri + (q.length > 1 ? q : ''),
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new Notification(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not load unsent Notification records:'
          );
          loggerCallback(LogLevel.Error, err);
          return of(new Array<Notification>());
        })
      );
    return r$;
  }

  initAuthorityDispute(claims: string, cpt: string, authKey: string) {
    // basic triage before bothering the server
    if ((claims.replaceAll(',', '') + authKey).search(/\W/) > -1)
      return throwError(
        'Illegal characters found in starting parameters. Unable to initialize Dispute!'
      );
    if (cpt !== '*' && (cpt.length > 10 || cpt.search(/\W/) > -1))
      return throwError(
        'Illegal characters found in starting parameters. Unable to initialize Dispute!'
      );
    const uri = `${this._baseUrl}/api/batching/init`;
    const r$ = this.http
      .post<AuthorityDispute>(
        uri,
        new AuthorityDisputeInit({ claims: claims, cpt: cpt, auth: authKey })
      )
      .pipe(
        switchMap((data) => of(new AuthorityDispute(data))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not initialize a new AuthorityDispute for the given parameters:'
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  loadArbitrators(
    includeFees: boolean = false,
    arbType: ArbitratorType | undefined = undefined,
    activeOnly: boolean = false
  ) {
    let url = `${this._baseUrl}/api/arbitrators?`;

    if (includeFees) url += 'f=true&';
    if (!!arbType) url += `t=${ArbitratorType[arbType]}&`;
    if (activeOnly) url += `a=true&`;

    url = url.slice(0, -1);

    const r$ = this.http
      .get<Arbitrator[]>(url, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(data.map((a) => new Arbitrator(a)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Arbitrator records:');
          loggerCallback(LogLevel.Error, err);
          return of(new Array<Arbitrator>());
        })
      );
    return r$;
  }

  loadAuthorities() {
    const r$ = this.http
      .get<Authority[]>(
        `${this._baseUrl}/api/authorities`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new Authority(a)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Authority records');
          loggerCallback(LogLevel.Error, err);
          return of(new Array<Authority>());
        })
      );
    return r$;
  }

  loadAuthorityCase(authority: string, caseNumber: string) {
    if (authority.toLowerCase() === 'tx')
      return this.http.get<TDIRequestDetails>(
        `${this._baseUrl}/api/arbitration/authority/${authority}/${caseNumber}`
      );
    else return EMPTY;
  }

  loadAuthorityFiles(id: number, docType = '') {
    if (!Number.isInteger(id) || id < 1) {
      return throwError(new Error('Invalid parameters'));
    }

    let s = `${this._baseUrl}/api/authorities/files/${id}`;
    if (docType) s += `?docType=${docType}`;

    const r$ = this.http
      .get<CaseFile[]>(s, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(data.map((d) => new CaseFile(d)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Authority files');
          loggerCallback(LogLevel.Error, err);
          return of(new Array<CaseFile>());
        })
      );
    return r$;
  }

  loadBatchLookupOpenClaims(customerId: number, payorId: number, npi: string) {
    let s = `${this._baseUrl}/api/cases/lookup/claims?c=${customerId}&p=${payorId}&r=${npi}`;
    const r$ = this.http
      .get<ArbitrationCase[]>(s, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(data.map((d) => new ArbitrationCase(d)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not load ArbitrationCase records'
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }
  /*
  loadBenchmark(geozip: string, procedureCode: number | string, year: number | string, state: string = 'TX', mod26: boolean = false):Observable<Benchmark | null> {
    if (!year || year < 2020 || year > 2050 || !geozip || !procedureCode) {
      throw 'Invalid parameter'
    }

    state = state || 'TX';
    const mfr = mod26 ? '26' : '';

    // check the cache
    const bm = this.dataStore.benchmarks.find(b => b.geozip === geozip && b.reportYear === year && b.procedureCode === procedureCode && b.modifier === mfr);
    if (bm) {
      return of(bm);
    }

    let sql = `benchmark/legacy?pc=${procedureCode}&gz=${geozip}&yr=${year}&s=${state}&m26=${mod26}`;
    const r$ =this.http.get<Benchmark>(`${this._baseUrl}/${sql}`).pipe(
      switchMap(data => {
          let c = new Benchmark(data);
          this.dataStore.benchmarks.push(c);
          return of(c);
      }),
      catchError(err => {
        loggerCallback(LogLevel.Error, err);
        return of(null);
      })
    );
    return r$;
  }
  */
  loadBenchmarks(
    datasetId: number,
    geozip: string,
    procedureCode: number | string,
    mod26: boolean = false
  ): Observable<BenchmarkDataItem | null> {
    const r$ = this.http
      .get<BenchmarkDataItem>(
        `${this._baseUrl}/api/benchmark?ds=${datasetId}&p=${procedureCode}&g=${geozip}&m26=${mod26}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(!!data ? new BenchmarkDataItem(data) : null)),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not load Benchmark Dataset record'
          );
          loggerCallback(LogLevel.Error, err);
          return of(null);
        })
      );
    return r$;
  }

  loadBenchmarkDatasets() {
    const r$ = this.http
      .get<BenchmarkDataset[]>(
        `${this._baseUrl}/api/benchmark/source`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new BenchmarkDataset(a)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not load Benchmark Dataset records'
          );
          loggerCallback(LogLevel.Error, err);
          return of(new Array<BenchmarkDataset>());
        })
      );
    return r$;
  }

  loadBenchmarkItemCount(b: BenchmarkDataset) {
    if (!b || b.id < 1)
      return throwError(new Error('Invalid BenchmarkDataset'));
    return this.http.get<number>(
      `${this._baseUrl}/api/benchmark/source/${b.id}/count`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  /** NOTE: Return values are not cached in case stakeholder wants to change the variables in the middle of the day / before a user's session naturally ends */
  loadCalculatorVariables() {
    const r$ = this.http
      .get<CalculatorVariables[]>(`${this._baseUrl}/api/arbitration/app/vars`)
      .pipe(
        switchMap((data) => of(data.map((a) => new CalculatorVariables(a)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load CalculatorVariables');
          loggerCallback(LogLevel.Error, err);
          return of(new Array<CalculatorVariables>());
        })
      );
    return r$;
  }

  loadCaseById(id: number) {
    if (!Number.isInteger(id) || id < 1)
      return throwError(new Error('Invalid Case Id'));

    const r$ = this.http
      .get<ArbitrationCase>(
        `${this._baseUrl}/api/cases/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new ArbitrationCase(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  loadCaseArchives(id: number) {
    if (!Number.isInteger(id) || id < 1)
      return throwError(new Error('Invalid Case Id'));

    const r$ = this.http
      .get<CaseArchive[]>(
        `${this._baseUrl}/api/cases/${id}/archives`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new CaseArchive(a)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  loadCaseFiles(
    id: number,
    output$: BehaviorSubject<CaseFile[]>,
    svc: ToastService
  ): void {
    if (!Number.isInteger(id) || id < 1 || !output$) {
      throw 'Invalid parameters';
    }

    this.http
      .get<CaseFile[]>(
        `${this._baseUrl}/api/cases/files/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .subscribe(
        (data) => {
          if (data.length) {
            const cf: CaseFile[] = [];
            data.forEach((d) => cf.push(new CaseFile(d)));
            output$.next(cf);
          }
        },
        (err) => {
          loggerCallback(LogLevel.Error, 'Could not load CaseFiles records');
          loggerCallback(LogLevel.Error, err);
          svc.showAlert(
            ToastEnum.danger,
            'Error loading files for Case Id ' +
            id +
            '. See console for more details.'
          );
        }
      );
  }

  loadEntitiesByName(name: string) {
    if (!name) return throwError(new Error('Invalid search'));
    const r$ = this.http
      .get<Entity[]>(
        `${this._baseUrl}/api/customers/entity/find?name=${name}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new Entity(a)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Entity records:');
          loggerCallback(LogLevel.Error, err);
          return of(new Array<Entity>());
        })
      );
    return r$;
  }

  loadEntityById(id: number) {
    if (!Number.isInteger(id) || id < 1)
      return throwError(new Error('Invalid Id'));

    const r$ = this.http
      .get<Entity>(
        `${this._baseUrl}/api/customers/entity/byid/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new Entity(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  loadEntityByNPI(npi: string) {
    if (!npi) return throwError(new Error('Invalid NPI'));

    const r$ = this.http
      .get<Entity>(
        `${this._baseUrl}/api/customers/entity/bynpi?npi=${npi}}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new Entity(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  loadPayorFiles(id: number, docType = '') {
    if (!Number.isInteger(id) || id < 1) {
      return throwError(new Error('Invalid parameters'));
    }

    let s = `${this._baseUrl}/api/payors/files/${id}`;
    if (docType) s += `?docType=${docType}`;

    const r$ = this.http
      .get<CaseFile[]>(s, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(data.map((d) => new CaseFile(d)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Payor files');
          loggerCallback(LogLevel.Error, err);
          return of(new Array<CaseFile>());
        })
      );
    return r$;
  }

  loadCaseLog(id: number) {
    if (!Number.isInteger(id) || id < 1) {
      return throwError(new Error('Invalid Case Id'));
    }

    const r$ = this.http
      .get<CaseLog[]>(
        `${this._baseUrl}/api/cases/log/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((d) => new CaseLog(d)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load CaseLog records');
          loggerCallback(LogLevel.Error, err);
          return of(new Array<CaseLog>());
        })
      );
    return r$;
  }

  loadDisputeLog(id: number) {
    if (!Number.isInteger(id) || id < 1) {
      return throwError(new Error('Invalid Case Id'));
    }

    const r$ = this.http
      .get<AuthorityDisputeLog[]>(
        `${this._baseUrl}/api/batching/log/${id}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((d) => new AuthorityDisputeLog(d)))),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            'Could not load AuthorityDisputeLog records'
          );
          loggerCallback(LogLevel.Error, err);
          return of(new Array<AuthorityDisputeLog>());
        })
      );
    return r$;
  }

  loadCustomers() {
    const r$ = this.http
      .get<Customer[]>(
        `${this._baseUrl}/api/customers`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new Customer(a)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Customer records');
          loggerCallback(LogLevel.Error, err);
          return of(new Array<Customer>());
        })
      );
    return r$;
  }

  loadCustomerByName(name: string) {
    const r$ = this.http
      .get<Customer>(
        `${this._baseUrl}/api/customers/find?name=${name}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new Customer(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Customer record');
          loggerCallback(LogLevel.Error, err);
          return of(undefined);
        })
      );
    return r$;
  }

  loadHolidays() {
    const r$ = this.http
      .get<Holiday[]>(
        `${this._baseUrl}/api/arbitration/app/holidays`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new Holiday(a)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load Holiday records');
          loggerCallback(LogLevel.Error, err);
          return of(new Array<Holiday>());
        })
      );
    return r$;
  }

  loadImportConfig(src: string) {
    if (!src) return throwError(new Error('Src is required!'));

    return this.http
      .get<ImportFieldConfig[]>(
        `${this._baseUrl}/api/arbitration/app/fieldconfig?source=${src}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          const r = new Array<ImportFieldConfig>();
          if (data && data.length) {
            for (const d of data) {
              r.push(new ImportFieldConfig(d));
            }
          }
          return of(r);
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error retrieving the list of ImportFieldConfig objects`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  /** Fetch the list of Negotiators for a given Payor Id
   * @pid Payor Id
   * Removed. Now handled by the api/payors endpoint
   */
  /*
  loadNegotiators(pid: number, a: boolean = true) {
    const r$ = this.http.get<Negotiator[]>(`${this._baseUrl}/api/arbitration/app/negotiators/${pid}?activeOnly=${a}`, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(switchMap(
        data => of(data.map(a => new Negotiator(a)))
      ),
        catchError(err => {
          loggerCallback(LogLevel.Error, err);
          return of(new Array<Negotiator>())
        }));
    return r$;
  }
  */

  loadNotificationByClaimId(
    id: number,
    docType: NotificationType
  ): Observable<Notification | null> {
    if (id < 1) return throwError(new Error('Invalid id!'));

    const r$ = this.http
      .get<Notification>(
        `${this._baseUrl}/api/notifications/queued?c=${id}&t=${docType}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => (!!data ? of(new Notification(data)) : of(null))),
        catchError((err) => {
          if (err.status == '404') return of(null);
          return throwError(err);
        })
      );
    return r$;
  }

  loadPayorById(id: number, a: boolean = true) {
    if (!Number.isInteger(id) || id < 1)
      return throwError(new Error('Invalid Payor Id'));

    const r$ = this.http
      .get<Payor>(
        `${this._baseUrl}/api/payors/${id}?activeOnly=${a}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new Payor(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  lookupPayorByName(name: string) {
    const r$ = this.http
      .get<Payor>(
        `${this._baseUrl}/api/payors/lookup/${name}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(new Payor(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  /** Fetch the list of Payors
   *
   */
  loadPayors(
    activeOnly: boolean = true,
    includeGroups: boolean = false,
    includeTemplates: boolean = true
  ) {
    const r$ = this.http
      .get<Payor[]>(
        `${this._baseUrl}/api/payors?active=${activeOnly}&groups=${includeGroups}&templates=${includeTemplates}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new Payor(a)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return of(new Array<Payor>());
        })
      );
    return r$;
  }

  Cache_PlaceOfServiceCodes: PlaceOfServiceCode[] = [];
  loadPlaceOfServiceCodes(): Observable<PlaceOfServiceCode[]> {
    if (this.Cache_PlaceOfServiceCodes.length > 0)
      return of(this.Cache_PlaceOfServiceCodes);

    const r$ = this.http
      .get<PlaceOfServiceCode[]>(
        `${this._baseUrl}/api/arbitration/app/emr/pos`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((result) => {
          this.Cache_PlaceOfServiceCodes = result.map(
            (a) => new PlaceOfServiceCode(a)
          );
          this.Cache_PlaceOfServiceCodes.sort(UtilService.SortByCodeNumber);
          return of(this.Cache_PlaceOfServiceCodes);
        }),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return of(new Array<PlaceOfServiceCode>());
        })
      );
    return r$;
  }

  loadServices(): Observable<{ name: string; serviceLine: string }[]> {
    const services: { name: string; serviceLine: string }[] = [];
    services.push({ name: 'ANES', serviceLine: 'ANES' });
    services.push({ name: 'ER Center', serviceLine: 'ER' });
    services.push({ name: 'ER Service', serviceLine: 'ER' });
    services.push({ name: 'ER Toxicology', serviceLine: 'Toxicology' });
    services.push({ name: 'Hospitalist', serviceLine: 'Observation' });
    services.push({ name: 'IOM Pro', serviceLine: 'IOM' });
    services.push({ name: 'IOM Tech', serviceLine: 'IOM' });
    services.push({ name: 'PA', serviceLine: 'PA' });
    services.push({ name: 'PA Classic', serviceLine: 'PA' });
    services.push({ name: 'PA Hybrid', serviceLine: 'PA' });
    return of(services);
  }

  /** Fetch the list of Templates without the HTML field
   *
   */
  loadTemplates(activeOnly: boolean = true) {
    const r$ = this.http
      .get<Template[]>(
        `${this._baseUrl}/api/templates`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => of(data.map((a) => new Template(a)))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return of(new Array<Template>());
        })
      );
    return r$;
  }

  loadUploadLogs(a: string) {
    return this.http.get<CaseFile[]>(
      `${this._baseUrl}/api/arbitration/app/import/log/${a}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  loadUsers() {
    const r$ = this.http
      .get<AppUser[]>(
        `${this._baseUrl}/api/arbitration/app/users`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          const users: AppUser[] = [];
          data.forEach((d) => users.push(new AppUser(d)));
          return of(users);
        }),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return of(new Array<AppUser>());
        })
      );
    return r$;
  }
  /*
  mergeArbitrationCases(pa: ArbitrationCase) {
    if (!pa)
      return throwError(new Error('Object has no value. Nothing to do!'));
    
    return this.http.post<ArbitrationCase>(`${this._baseUrl}/api/cases/merge`, pa)
      .pipe(switchMap(data => {
        const rec = new ArbitrationCase(data);
        return of(rec);
      }),
      catchError((err) => {
        loggerCallback(LogLevel.Error, 'Could not merge records');
        loggerCallback(LogLevel.Error, err);
        return throwError(err);
      })
    );
  }
  */
  recalcAuthority(
    auth: Authority,
    activeOnly: boolean = false
  ): Observable<JobQueueItem> {
    const uri = `${this._baseUrl}/api/authorities/item/bykey/${auth.key}/recalc?aa=${activeOnly}`;

    const r$ = this.http
      .get<JobQueueItem>(uri, CaseDataService.REQUEST_OPTIONS_JSON)
      .pipe(
        switchMap((data) => of(new JobQueueItem(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not get JobQueueItem:');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
    return r$;
  }

  searchArbRejections() {
    return this.http
      .get<ArbitrationCase[]>(
        `${this._baseUrl}/api/cases/arbrejections`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) =>
          of(data ? data.map((a) => new ArbitrationCase(a)) : [])
        ),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err); //of(new Array<ArbitrationCase>())
        })
      );
  }

  searchBriefDueSoon(fed: number) {
    return this.http
      .get<ArbitrationCase[]>(
        `${this._baseUrl}/api/cases/briefduesoon?fed=${fed}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) =>
          of(data ? data.map((a) => new ArbitrationCase(a)) : [])
        ),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err); //of(new Array<ArbitrationCase>())
        })
      );
  }

  /** Search all cases. Does not return IsDeleted cases. */
  searchCases(criteria: ArbitrationCase | ArbitrationCaseVM) {
    if (!criteria.authority) criteria.authority = '*';
    const includeClosed = !!(criteria as any).includeClosed;
    const includeInactive = !!(criteria as any).includeInactive;
    return this.http
      .post<ArbitrationCase[]>(
        `${this._baseUrl}/api/cases/search?inactive=${includeInactive}&closed=${includeClosed}`,
        criteria,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) =>
          of(data ? data.map((a) => new ArbitrationCase(a)) : [])
        ),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err); //of(new Array<ArbitrationCase>())
        })
      );
  }

  /** Search all cases. Does not return IsDeleted cases. */
  searchCases2(criteria: ArbitrationCase | ArbitrationCaseVM) {
    if (!criteria.authority) criteria.authority = '*';
    const includeClosed = !!(criteria as any).includeClosed;
    const includeInactive = !!(criteria as any).includeInactive;
    return this.http
      .post<ArbitrationCase[]>(
        `${this._baseUrl}/api/cases/search2?inactive=${includeInactive}&closed=${includeClosed}`,
        criteria,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) =>
          of(data ? data.map((a) => new ArbitrationCase(a)) : [])
        ),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err); //of(new Array<ArbitrationCase>())
        })
      );
  }

  /** CurrentCases are cases not already closed, deleted, settled or rejected by both NSA and local authorities */
  searchCurrentCases(u: string = 'none') {
    return this.http
      .get<ArbitrationCase[]>(
        `${this._baseUrl}/api/cases/currentcases?u=${u}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) =>
          of(data ? data.map((a) => new ArbitrationCase(a)) : [])
        ),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  /** Search all cases. Does not return IsDeleted cases. */
  searchDisputes(
    criteria: ArbitrationCase | ArbitrationCaseVM
  ): Observable<AuthorityDisputeVM[]> {
    if (!criteria.authority) criteria.authority = '*';
    const includeClosed = !!(criteria as any).includeClosed;
    const includeInactive = !!(criteria as any).includeInactive;
    return this.http
      .post<AuthorityDisputeVM[]>(
        `${this._baseUrl}/api/batching/search?inactive=${includeInactive}&closed=${includeClosed}`,
        criteria,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) =>
          of(data ? data.map((a) => new AuthorityDisputeVM(a)) : [])
        ),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err); //of(new Array<ArbitrationCase>())
        })
      );
  }

  searchNeedsNSARequest(customer: string, deadlineFilter: string) {
    return this.http
      .get<ArbitrationCase[]>(
        `${this._baseUrl}/api/cases/needsnsarequest?deadlineFilter=${deadlineFilter}&customer=${customer}`,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) =>
          of(data ? data.map((a) => new ArbitrationCase(a)) : [])
        ),
        catchError((err) => {
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateArbitrationCase(pa: ArbitrationCase) {
    if (!pa)
      return throwError(new Error('Object has no value. Nothing to save!'));

    if (pa.NSARequestDiscount === null)
      return throwError(
        new Error(
          'Unexpected internal condition:  NSARequestDiscount is NULL. Cannot save.'
        )
      );

    if (pa.id < 1)
      return throwError(
        new Error('Cannot call update on a New record. Try Create instead.')
      );

    return this.http
      .put<ArbitrationCase>(`${this._baseUrl}/api/cases/${pa.id}`, pa)
      .pipe(
        switchMap((data) => {
          const rec = new ArbitrationCase(data);
          return of(rec);
        }),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not update ArbitrationCase');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateArbitrator(n: Arbitrator) {
    return this.http
      .put<Arbitrator>(
        `${this._baseUrl}/api/arbitrators`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Arbitrator(data));
        }),
        catchError((err) => {
          loggerCallback(LogLevel.Error, `Error updating Arbitrator ${n.id}`);
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateAuthority(id: number, a: Authority) {
    // trim leading and trailing spaces and ensure the value of Not Submitted is present
    //const v = a.statusValues.split(/[;,]/).map(v=>v.trim());
    let v = a.statusList;
    if (a.key.toLowerCase() !== 'nsa' && v.indexOf('Not Submitted') === -1) {
      v.unshift('Not Submitted');
      a.statusValues = v.join(';');
    }
    // explicitly handle some viewmodel fields that only exist in JSON
    let j: any = {};
    try {
      j = JSON.parse(a.JSON);
    } catch { }
    j.defaultSubmittedStatus = a.defaultSubmittedStatus;
    j.defaultUnsubmittedStatus = a.defaultUnsubmittedStatus;
    a.JSON = JSON.stringify(j);
    const b: any = Object.assign({}, a);
    delete b.defaultSubmittedStatus;
    delete b.defaultUnsubmittedStatus;

    return this.http
      .put<Authority>(
        `${this._baseUrl}/api/authorities/item/byid/${id}`,
        b,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Authority(data));
        }),
        catchError((err) => {
          loggerCallback(LogLevel.Error, `Error updating Authority ${a.id}`);
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateAuthorityBenchmarkDetail(a: Authority, d: AuthorityBenchmarkDetails) {
    // TODO: Add this type of error handling to the rest of the service
    if (
      a.id < 1 ||
      d.benchmarkDatasetId < 1 ||
      d.id === 0 ||
      d.authorityId !== a.id
    )
      return throwError(new Error('Invalid Authority or Benchmark data'));

    return this.http
      .put<AuthorityBenchmarkDetails>(
        `${this._baseUrl}/api/authorities/item/byid/${a.id}/benchmarks`,
        d,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new AuthorityBenchmarkDetails(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating AuthorityBenchmarkDetails for Authority ${a.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateAuthorityDispute(dispute: AuthorityDispute) {
    // basic triage before bothering the server
    if (dispute.disputeCPTs.length > 0)
      return throwError(
        'DisputeCPTs cannot be used to update an AuthorityDispute. Remove them before calling this method.'
      );
    if (dispute.cptViewmodels.length === 0)
      return throwError('At least one AuthorityDisputeCPTVM is required.');
    if (dispute.cptViewmodels.find((v) => v.claimCPTId < 1))
      return throwError(
        'All AuthorityDisputeCPTVM objects require a ClaimCPTId.'
      );
    if (
      !dispute.authorityCaseId ||
      !dispute.authorityId ||
      !dispute.submissionDate
    )
      return throwError(
        'Missing authorityCaseId, authorityId or submissionDate!'
      );

    for (let f of dispute.fees) f.baseFee = undefined;

    const uri = `${this._baseUrl}/api/batching`;
    const r$ = this.http.put<AuthorityDispute>(uri, dispute).pipe(
      switchMap((data) => of(new AuthorityDispute(data))),
      catchError((err) => {
        loggerCallback(
          LogLevel.Error,
          'Could not construct a new AuthorityDispute'
        );
        loggerCallback(LogLevel.Error, err);
        return throwError(err);
      })
    );
    return r$;
  }

  updateArbitratorFee(
    a: Arbitrator,
    f: ArbitratorFee
  ): Observable<ArbitratorFee> {
    if (!a.id || f.arbitratorId !== a.id || f.id < 1)
      return throwError('The id values are not correct!');
    return this.http
      .put<ArbitratorFee>(
        `${this._baseUrl}/api/arbitrators/fees`,
        f,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new ArbitratorFee(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating AuthorityFee ${f.id}-${f.feeName} for Authority ${a.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateAuthorityFee(a: Authority, f: AuthorityFee): Observable<AuthorityFee> {
    if (!a.id || f.authorityId !== a.id || f.id < 1)
      return throwError('The id values are not correct!');
    return this.http
      .put<AuthorityFee>(
        `${this._baseUrl}/api/authorities/fees`,
        f,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new AuthorityFee(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating AuthorityFee ${f.id}-${f.feeName} for Authority ${a.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  deleteAuthorityTrackingDetail(a: Authority, id: number) {
    if (id < 1) return throwError(new Error('Missing or invalid identifier'));
    return this.http.delete<void>(
      `${this._baseUrl}/api/authorities/item/byid/${a.id}/tracking/${id}`,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  updateAuthorityTrackingDetail(a: Authority, d: AuthorityTrackingDetail) {
    return this.http
      .put<AuthorityTrackingDetail>(
        `${this._baseUrl}/api/authorities/item/byid/${a.id}/tracking`,
        d,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new AuthorityTrackingDetail(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating AuthorityTrackingDetail field ${d.trackingFieldName} for Authority ${a.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateBenchmarkDataset(id: number, n: BenchmarkDataset) {
    return this.http
      .put<BenchmarkDataset>(
        `${this._baseUrl}/api/benchmark/source/${id}`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new BenchmarkDataset(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating Benchmark Dataset ${n.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateCustomer(id: number, n: Customer) {
    return this.http
      .put<Customer>(
        `${this._baseUrl}/api/customers/${id}`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Customer(data));
        }),
        catchError((err) => {
          loggerCallback(LogLevel.Error, `Error updating Customer ${n.id}`);
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateDisputeWorkItem(item: AuthorityDisputeWorkItem) {
    return this.http.put(
      `${this._baseUrl}/api/batching/queue/complete`,
      item,
      CaseDataService.REQUEST_OPTIONS_JSON
    );
  }

  updateEntity(id: number, n: Entity) {
    return this.http
      .put<Entity>(
        `${this._baseUrl}/api/customers/entity/${id}`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Entity(data));
        }),
        catchError((err) => {
          loggerCallback(LogLevel.Error, `Error updating Entity ${n.id}`);
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateFieldConfig(f: ImportFieldConfig) {
    if (!f)
      return throwError(new Error('Object has no value. Nothing to save!'));

    if (f.id < 1)
      return throwError(new Error('Cannot call update on a New record.'));

    return this.http
      .put<ImportFieldConfig>(
        `${this._baseUrl}/api/arbitration/app/fieldconfig/${f.id}`,
        f,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new ImportFieldConfig(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating ImportFieldConfig ${f.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateNegotiator(n: Negotiator) {
    return this.http
      .put<Negotiator>(
        `${this._baseUrl}/api/arbitration/app/negotiators`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Negotiator(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating Negotiator for PayerId ${n.payorId}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updatePayor(n: Payor) {
    return this.http
      .put<Payor>(
        `${this._baseUrl}/api/payors`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Payor(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating Payor with Id ${n.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updatePayorGroup(id: number, group: PayorGroup) {
    if (id !== group.payorId) return throwError(new Error('PayorId mismatch'));
    if (group.id < 1) return throwError(new Error('Invalid Id'));
    return this.http
      .put<PayorGroup>(
        `${this._baseUrl}/api/payors/${id}/groups`,
        group,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new PayorGroup(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating PayorGroup with Id ${group.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateMasterDataException(id: number, n: MasterDataException) {
    if (n.id < 1 || id !== n.id) return throwError(new Error('Invalid Id!'));
    if (n.id !== id)
      return throwError(
        new Error('Id mismatch when updating MasterDataException')
      );
    if (n.exceptionType === MasterDataExceptionType.Unknown)
      return throwError(new Error('Cannot update an Unknown exception type'));
    return this.http
      .put<MasterDataException>(
        `${this._baseUrl}/api/mde/items/${id}`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new MasterDataException(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating MasterDataException with Id ${n.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  createSettlement(settlement: CaseSettlement) {
    if (
      settlement.arbitrationCaseId < 1 ||
      settlement.id !== 0 ||
      (settlement?.authorityId ?? 0) < 0 ||
      settlement.payorId < 1 ||
      settlement.isDeleted
    )
      return throwError(new Error('Invalid id or isDeleted parameter!'));

    return this.http
      .post<CaseSettlement>(
        `${this._baseUrl}/api/settlements`,
        settlement,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new CaseSettlement(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error adding a new CaseSettlement for ArbitrationCaseId ${settlement.arbitrationCaseId}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateSettlement(settlement: CaseSettlement) {
    if (
      settlement.arbitrationCaseId < 1 ||
      settlement.id < 1 ||
      (settlement?.authorityId ?? 0) < 0 ||
      settlement.payorId < 1 ||
      settlement.isDeleted
    )
      return throwError(new Error('Invalid id or isDeleted parameter!'));

    return this.http
      .put<CaseSettlement>(
        `${this._baseUrl}/api/settlements`,
        settlement,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new CaseSettlement(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating CaseSettlement with Id ${settlement.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateTemplate(id: number, n: Template) {
    if (n.id < 1 || id !== n.id) return throwError(new Error('Invalid Id!'));
    if (!n.name || !n.html)
      return throwError(new Error('Name and HTML are required fields!'));
    return this.http
      .put<Template>(
        `${this._baseUrl}/api/Templates/${id}`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new Template(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating Template with Id ${n.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  // changes broadcast via dataStore updates
  updateUser(u: AppUser) {
    if (!u) {
      return throwError(new Error('updateUser called with empty object'));
    }
    const n = new AppUser(u);
    // remove viewmodel properties if they exist - no reason to send these to the server
    delete n.isAdmin;
    delete n.isManager;
    delete n.isNegotiator;
    delete n.isNSA;
    delete n.isReporter;
    delete n.isState;
    delete n.appRoles;

    return this.http
      .put(
        `${this._baseUrl}/api/arbitration/app/users/${u.id}`,
        n,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new AppUser(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating AppUser with id ${n.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  updateAppSettings(s: AppSettings) {
    if (s.id < 1) return throwError(new Error('Invalid AppSettings Id'));

    return this.http
      .put(
        `${this._baseUrl}/api/arbitration/app/settings`,
        s,
        CaseDataService.REQUEST_OPTIONS_JSON
      )
      .pipe(
        switchMap((data) => {
          return of(new AppSettings(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error updating AppSettings with id ${s.id}`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  uploadArbitrators(f: File): Observable<any> {
    // Create form data
    const formData = new FormData();

    // Store form name as "file" with file data
    formData.append('file', f);

    // Make http post request over api
    // with formData as req
    return this.http.post(`${this._baseUrl}/api/arbitrators/import`, formData, {
      responseType: 'text',
      headers: {
        contentType: 'multipart/form-data',
      },
    });
  }

  uploadAuthorityData(f: File, authority: string): Observable<JobQueueItem> {
    // Create form data
    const formData = new FormData();

    // Store form name as "file" with file data
    let uri = `${this._baseUrl}/api/arbitration/import`;
    formData.append('file', f);
    formData.append('authority', authority);

    return this.http.post<JobQueueItem>(uri, formData, {
      responseType: 'json',
      headers: {
        contentType: 'multipart/form-data',
      },
    });
  }

  uploadSystemData(
    f: File,
    effectiveDate: Date | null = null
  ): Observable<any> {
    // Create form data
    const formData = new FormData();

    // Store form name as "file" with file data
    formData.append('file', f);

    let uri = `${this._baseUrl}/api/procedurecodes/import`;

    if (effectiveDate) {
      uri += '?ed=' + JSON.stringify(effectiveDate);
    }

    return this.http.post(uri, formData, {
      responseType: 'text',
      headers: {
        contentType: 'text/csv',
      },
    });
  }

  uploadBenchmarkData(f: File, id: number, key: string): Observable<any> {
    if (!f || id < 1) return throwError(new Error('Invalid data for request'));

    // Create form data
    const formData = new FormData();

    // Store form name as "file" with file data
    formData.append('file', f);
    formData.append('key', key);
    // Make http post request over api
    // with formData as req
    return this.http.post(
      `${this._baseUrl}/api/benchmark/import/${id}`,
      formData,
      {
        responseType: 'text',
        headers: {
          contentType: 'text/csv',
        },
      }
    );
  }

  uploadCaseDocument(
    fileToUpload: File,
    id: number,
    docType: string
  ): Observable<any> {
    if (id < 1) return throwError(new Error('Bad upload parameters'));
    if (!fileToUpload) return throwError(new Error('No file content found'));

    // Create form data
    const formData = new FormData();

    // Store form name as "file" with file data
    formData.append('file', fileToUpload);

    // Make http post request over api
    // with formData as req
    return this.http.post(
      `${this._baseUrl}/api/cases/blob?id=${id}&docType=${docType}`,
      formData,
      {
        responseType: 'text',
        headers: {
          contentType: 'application/pdf',
        },
      }
    );
  }

  uploadDisputeDocument(fileToUpload: File, ada: AuthorityDisputeAttachment) {
    if (ada.id !== 0) return throwError(new Error('Id must be zero!'));
    if (ada.authorityDisputeId < 1)
      return throwError(new Error('Bad Dispute Id parameter'));
    if (!fileToUpload) return throwError(new Error('No file content found'));

    const formData = new FormData();
    formData.append('file', fileToUpload);

    return this.http
      .post<AuthorityDisputeAttachment>(
        `${this._baseUrl}/api/batching/blob?id=${ada.authorityDisputeId}&cdt=${ada.docType}`,
        formData
      )
      .pipe(
        switchMap((data) => of(new AuthorityDisputeAttachment(data))),
        catchError((err) => {
          loggerCallback(LogLevel.Error, 'Could not load EMRClaimAttachments');
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  deleteCPTFromDispute(
    authorityDisputeId: number,
    claimCPTId: number
  ): Observable<any> {
    if (!authorityDisputeId || !claimCPTId)
      return throwError(new Error('Bad upload parameters'));
    var url = `${this._baseUrl}/api/batching/AuthorityDisputeCPT?authorityDisputeId=${authorityDisputeId}&claimCPTId=${claimCPTId}`;
    console.warn('calling :' + url);
    return this.http.delete(url, { observe: 'response' });

    //return this.http.delete(url, {observe: 'response'}).subscribe({
    //  next: respone => {
    //    console.warn(respone);
    //  },
    //  error: error => {
    //    console.error('Could not delete CPTFromDispute')
    //    loggerCallback(LogLevel.Error, 'Could not delete CPTFromDispute');
    //    loggerCallback(LogLevel.Error, error);

    //    //svc.showAlert(ToastEnum.danger, 'Could not delete CPTFromDispute');
    //  }
    //});
  }

  uploadEntityDocument(
    f: File,
    id: number,
    docType: string,
    entity: Authority | Payor
  ): Observable<any> {
    if (!id || !docType) return throwError(new Error('Bad upload parameters'));
    if (!f) return throwError(new Error('No file content found'));

    const ep = entity instanceof Payor ? 'payors' : 'authorities';
    const formData = new FormData();
    formData.append('file', f);

    return this.http.post(
      `${this._baseUrl}/api/${ep}/blob?id=${id}&cdt=${docType}`,
      formData,
      {
        responseType: 'text',
        headers: {
          contentType: 'application/pdf',
        },
      }
    );
  }

  uploadPayorGroups(f: File): Observable<PayorGroupResponse> {
    const formData = new FormData();
    // Store file data onto form as "file"
    formData.append('file', f);

    return this.http
      .post<PayorGroupResponse>(
        `${this._baseUrl}/api/payors/groups/import`,
        formData,
        {
          headers: {
            contentType: 'text/csv',
          },
        }
      )
      .pipe(
        switchMap((data) => {
          return of(new PayorGroupResponse(data));
        }),
        catchError((err) => {
          loggerCallback(LogLevel.Error, `Error uploading Payor Groups`);
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }

  uploadAuthorityPayorGroupExclusions(f: File): Observable<PayorGroupResponse> {
    const formData = new FormData();
    // Store file data onto form as "file"
    formData.append('file', f);

    return this.http
      .post<PayorGroupResponse>(
        `${this._baseUrl}/api/authorities/pge/import`,
        formData,
        {
          headers: {
            contentType: 'text/csv',
          },
        }
      )
      .pipe(
        switchMap((data) => {
          return of(new PayorGroupResponse(data));
        }),
        catchError((err) => {
          loggerCallback(
            LogLevel.Error,
            `Error uploading Payor Group Exclusions`
          );
          loggerCallback(LogLevel.Error, err);
          return throwError(err);
        })
      );
  }
}
