import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { CalculatorComponent } from './components/calculator/calculator.component';
//import { BatchCalculatorComponent } from './components/batch-calculator/batch-calculator.component';
import { CaseSearchComponent } from './components/case-search/case-search.component';
//import { PreArbitrationComponent } from './components/pre-arbitration/pre-arbitration.component';
import { DataUploadComponent } from './components/data-upload/data-upload.component';
import { NgbDatepickerModule, NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { NgSelectModule } from '@ng-select/ng-select';
import { NegotiatorComponent } from './components/negotiator/negotiator.component';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import {
  IPublicClientApplication,
  PublicClientApplication,
  InteractionType,
  BrowserCacheLocation,
  LogLevel,
} from '@azure/msal-browser';
import {
  MsalGuard,
  MsalInterceptor,
  MsalBroadcastService,
  MsalInterceptorConfiguration,
  MsalModule,
  MsalService,
  MSAL_GUARD_CONFIG,
  MSAL_INSTANCE,
  MSAL_INTERCEPTOR_CONFIG,
  MsalGuardConfiguration,
  MsalRedirectComponent,
} from '@azure/msal-angular';
import { environment } from '../environments/environment';
import { FileImportConfigComponent } from './components/file-import-config/file-import-config.component';
import { NgChartsModule } from 'ng2-charts';
import { OffersComparisonChartComponent } from './components/offers-comparison-chart/offers-comparison-chart.component';
import { LoginComponent } from './components/login/login.component';
import { DataTablesModule } from 'angular-datatables';
import { ModelDirtyGuard } from './model/model-dirty-guard';
import { ConfirmationDialogComponent } from './components/confirmation-dialog/confirmation-dialog.component';
import { ManageUsersComponent } from './components/manage-users/manage-users.component';
import { HasUserProfileGuard } from './model/can-activate-guard';
import { NoProfileComponent } from './components/no-profile/no-profile.component';
import { ManagePayorsComponent } from './components/manage-payors/manage-payors.component';
import { AddOfferComponent } from './components/add-offer/add-offer.component';
import { ManageCalculatorVariablesComponent } from './components/manage-calculator-variables/manage-calculator-variables.component';
import { ManageArbitratorsComponent } from './components/manage-arbitrators/manage-arbitrators.component';
import { ManageCustomersComponent } from './components/manage-customers/manage-customers.component';
import { ManageAuthoritiesComponent } from './components/manage-authorities/manage-authorities.component';
import { FilterPipe } from './model/filter.pipe';
import { ManageBenchmarksComponent } from './components/manage-benchmarks/manage-benchmarks.component';
import { AcceptOfferComponent } from './components/accept-offer/accept-offer.component';
import { DocParserComponent } from './components/doc-parser/doc-parser.component';
import { BatchBuilderComponent } from './components/batch-builder/batch-builder.component';
import { AppHealthComponent } from './components/app-health/app-health.component';
import { DocPreviewComponent } from './components/doc-preview/doc-preview.component';
import { AngularEditorModule } from '@kolkov/angular-editor';
import { FileUploadComponent } from './components/file-upload/file-upload.component';
import { TemplateBuilderComponent } from './components/template-builder/template-builder.component';
import { ManageExceptionsComponent } from './components/manage-exceptions/manage-exceptions.component';
import { SummaryDialogComponent } from './components/summary-dialog/summary-dialog.component';
import { SettlementDialogComponent } from './components/settlement-dialog/settlement-dialog.component';
import { NotificationDeliveryDialogComponent } from './components/notification-delivery-dialog/notification-delivery-dialog.component';
import { DelayedHoverDirective } from './directives/delayed-hover.directive';
import { AddFeeComponent } from './components/add-fee/add-fee.component';
import { ListFeesComponent } from './components/list-fees/list-fees.component';
import { BatchCalculatorComponent } from './components/batch-calculator/batch-calculator.component';
import { CreateDisputeComponent } from './components/create-dispute/create-dispute.component';
import { AddArbitratorComponent } from './components/add-arbitrator/add-arbitrator.component';
import { ListSettlementsComponent } from './components/list-settlements/list-settlements.component';
import { AddSettlementComponent } from './components/add-settlement/add-settlement.component';
import { UserWorkQueueComponent } from './components/user-work-queue/user-work-queue.component';
import { UpdateDisputeQueueItemComponent } from './components/update-dispute-queue-item/update-dispute-queue-item.component';
import { ManageDisputesComponent } from './components/manage-disputes/manage-disputes.component';
import { DisputeDetailComponent } from './components/dispute-detail/dispute-detail.component';
import { PaginationModule, PaginationConfig } from 'ngx-bootstrap/pagination';
import { TooltipModule } from 'ngx-bootstrap/tooltip';

const isIE =
  window.navigator.userAgent.indexOf('MSIE ') > -1 ||
  window.navigator.userAgent.indexOf('Trident/') > -1; // Remove this line to use Angular Universal

export function getCurrentEnvName() {
  const domainName = new URL(window.location.href.toString()).hostname;

  switch (domainName) {
    case 'localhost' || 'arbitrationpoccalculator':
      return 'Test';
    case 'arbit-dev.halomd.com':
      return 'Dev';
    case 'arbitapi-dev.halomd.com':
      return 'Dev (API)';
    case 'arbit-qa.halomd.com':
      return 'QA';
    case 'arbitapi-qa.halomd.com':
      return 'QA (API)';
    default:
      return 'Prod';
  }
}

export function isTestEnv() {
  const addr = window.location.href.toString().toLowerCase();
  return addr.indexOf('localhost') > -1 ||
    addr.indexOf('arbitrationpoccalculator') > -1 ||
    addr.indexOf('arbit-dev.halomd.com') > -1 ||
    addr.indexOf('arbitapi-dev.halomd.com') > -1 ||
    addr.indexOf('arbit-qa.halomd.com') > -1 ||
    addr.indexOf('arbitapi-qa.halomd.com') > -1
    ? true
    : false;
}

export function loggerCallback(logLevel: LogLevel, message: any) {
  let msg = '';
  if (message.error) msg = message.error;
  else if (message.toString) msg = message.toString();
  else msg = '' + message;
  if (logLevel === LogLevel.Error) console.error(msg);
  else if (logLevel === LogLevel.Warning) console.warn(msg);
  else console.log(msg);
}

export function MSALInstanceFactory(): IPublicClientApplication {
  return new PublicClientApplication({
    auth: {
      clientId: environment.clientId,
      authority: `https://login.microsoftonline.com/${environment.tenantName}`,
      redirectUri: environment.redirectUrl,
    },
    cache: {
      cacheLocation: BrowserCacheLocation.LocalStorage,
      storeAuthStateInCookie: isIE, // set to true for IE 11
    },
    system: {
      loggerOptions: {
        loggerCallback,
        logLevel: LogLevel.Trace,
        piiLoggingEnabled: false,
      },
    },
  });
}

export function MSALInterceptorConfigFactory(): MsalInterceptorConfiguration {
  const protectedResourceMap = new Map<string, Array<string>>();
  protectedResourceMap.set('https://graph.microsoft.com/v1.0/me', [
    'user.read',
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/authorities`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/batching`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/briefs`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/customers`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/arbitration`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/arbitrators`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/benchmark`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/cases`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/mde`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/notes`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/notifications`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/payors`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/procedurecodes`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/settlements`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/templates`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  protectedResourceMap.set(`${environment.redirectUrl}/api/Dispute`, [
    `api://${environment.clientId}/access_as_user`,
  ]);
  return {
    interactionType: InteractionType.Popup,
    protectedResourceMap,
  };
}

export function MSALGuardConfigFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Popup,
    authRequest: {
      scopes: [`api://${environment.clientId}/access_as_user`],
    },
  };
}
//{ path: '', component: CaseSearchComponent, pathMatch: 'full', canActivate: [MsalGuard,HasUserProfileGuard], canDeactivate: [ModelDirtyGuard] },
//BatchCalculatorComponent,
//{ path: 'batch', component: BatchCalculatorComponent, canActivate: [MsalGuard,HasUserProfileGuard], canDeactivate: [ModelDirtyGuard]},
@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    BatchCalculatorComponent,
    CalculatorComponent,
    CaseSearchComponent,
    DataUploadComponent,
    FilterPipe,
    NegotiatorComponent,
    FileImportConfigComponent,
    OffersComparisonChartComponent,
    LoginComponent,
    ConfirmationDialogComponent,
    ManageUsersComponent,
    NoProfileComponent,
    ManagePayorsComponent,
    AddOfferComponent,
    ManageCalculatorVariablesComponent,
    ManageArbitratorsComponent,
    ManageCustomersComponent,
    ManageAuthoritiesComponent,
    ManageBenchmarksComponent,
    AcceptOfferComponent,
    DocParserComponent,
    BatchBuilderComponent,
    AppHealthComponent,
    DocPreviewComponent,
    FileUploadComponent,
    TemplateBuilderComponent,
    ManageExceptionsComponent,
    SummaryDialogComponent,
    SettlementDialogComponent,
    NotificationDeliveryDialogComponent,
    DelayedHoverDirective,
    AddFeeComponent,
    ListFeesComponent,
    CreateDisputeComponent,
    AddArbitratorComponent,
    ListSettlementsComponent,
    AddSettlementComponent,
    UserWorkQueueComponent,
    UpdateDisputeQueueItemComponent,
    ManageDisputesComponent,
    DisputeDetailComponent,
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    NgbModule,
    NgbDatepickerModule,
    MsalModule,
    NgChartsModule,
    DataTablesModule,
    AngularEditorModule,
    NgSelectModule,
    PaginationModule,
    TooltipModule.forRoot(),
    RouterModule.forRoot([
      { path: '', pathMatch: 'full', redirectTo: 'search' },
      {
        path: 'arbs',
        component: ManageArbitratorsComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'authorities',
        component: ManageAuthoritiesComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'batch',
        component: BatchCalculatorComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'batch/:id',
        component: BatchCalculatorComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'notifications',
        component: BatchBuilderComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'benchmarks',
        component: ManageBenchmarksComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'calculator',
        component: CalculatorComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'calculator/:id',
        component: CalculatorComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'customers',
        component: ManageCustomersComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'docparser',
        component: DocParserComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'formulas',
        component: ManageCalculatorVariablesComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'importconfig',
        component: FileImportConfigComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'mde',
        component: ManageExceptionsComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      { path: 'noprofile', component: NoProfileComponent },
      {
        path: 'payors',
        component: ManagePayorsComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'search',
        component: CaseSearchComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'sysinfo',
        component: AppHealthComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'templates',
        component: TemplateBuilderComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'todo',
        component: UserWorkQueueComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'upload',
        component: DataUploadComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'users',
        component: ManageUsersComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'disputes',
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
        component: ManageDisputesComponent,
      },
      {
        path: 'dispute',
        component: DisputeDetailComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      {
        path: 'dispute/:id',
        component: DisputeDetailComponent,
        canActivate: [MsalGuard, HasUserProfileGuard],
        canDeactivate: [ModelDirtyGuard],
      },
      { path: '**', redirectTo: 'search' },
    ]),
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: MsalInterceptor,
      multi: true,
    },
    {
      provide: MSAL_INSTANCE,
      useFactory: MSALInstanceFactory,
    },
    {
      provide: MSAL_GUARD_CONFIG,
      useFactory: MSALGuardConfigFactory,
    },
    {
      provide: MSAL_INTERCEPTOR_CONFIG,
      useFactory: MSALInterceptorConfigFactory,
    },
    ModelDirtyGuard,
    MsalService,
    MsalGuard,
    HasUserProfileGuard,
    MsalBroadcastService,
    PaginationConfig,
  ],
  bootstrap: [AppComponent, MsalRedirectComponent],
})
export class AppModule { }
