import { Inject, Injectable } from '@angular/core';
import { MsalService, MsalBroadcastService, MSAL_GUARD_CONFIG, MsalGuardConfiguration } from '@azure/msal-angular';
import { AccountInfo, AuthenticationResult, EventType, InteractionStatus, InteractionType, LogLevel, PopupRequest, RedirectRequest } from '@azure/msal-browser';
import { Log } from 'oidc-client';
import { BehaviorSubject, Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';
import { loggerCallback } from '../app.module';
import { AppUser } from '../model/app-user';
import { CaseDataService } from './case-data.service';
import { UtilService } from './util.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  currentUser$ = new BehaviorSubject<AppUser>(new AppUser());
  loggedIn$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  loginSequenceBegin$ = new BehaviorSubject<null>(null);
  loginSuccess$ = new BehaviorSubject<null>(null);
  private loggedIn = false;
  private readonly _destroying$ = new Subject<void>();

  constructor(
    @Inject(MSAL_GUARD_CONFIG) private msalGuardConfig: MsalGuardConfiguration,
    private authService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private svcData: CaseDataService) {
    this.msalBroadcastService.msalSubject$.pipe(takeUntil(this._destroying$))
      .subscribe(msg => {
        if (msg.eventType === EventType.LOGIN_SUCCESS) {
          this.setLoggedIn();
          this.checkAndSetActiveAccount();
          this.loginSuccess$.next(null);
        } else if (msg.eventType === EventType.LOGIN_START) {
          this.loginSequenceBegin$.next(null);
        }
      });
  }

  destroy() {
    this._destroying$.next(undefined);
    this._destroying$.complete();
  }

  getActiveAccount(): AccountInfo | null {
    return this.authService.instance.getActiveAccount();
  }

  login() {
    if (this.msalGuardConfig.interactionType === InteractionType.Popup) {
      this.loginWithPopup();
    } else {
      this.loginWithRedirect();
    }
  }

  private checkAndSetActiveAccount() {
    /**
    * If no active account set but there are accounts signed in, sets first account to active account
    * To use active account set here, subscribe to inProgress$ first in your component
    * Note: Basic usage demonstrated. Your app may require more complicated account selection logic
    */
    let activeAccount = this.authService.instance.getActiveAccount();

    if (!activeAccount && this.authService.instance.getAllAccounts().length > 0) {
      let accounts = this.authService.instance.getAllAccounts();
      this.authService.instance.setActiveAccount(accounts[0]);
    }


    if (activeAccount) {
      if (!this.loggedIn) {
        loggerCallback(LogLevel.Info, 'ActiveAccount detected but not logged in. Exiting...');

        return;
      }
      // now that we are authenticated, get our account profile from the API
      if (!UtilService.Holidays.length) {
        this.svcData.loadHolidays().subscribe(data => UtilService.Holidays = data);
      }
      const u = this.currentUser$.getValue();
      if (!u || !u.id) {
        //this.svcData.getCurrentUser().subscribe(data => this.currentUser$.next(new AppUser(data)));  // the canAcivateGuard will take care of loading the profile
      } else {
        console.log('Unexpected! currentUser$.getValue() returns', this.currentUser$.getValue());
      }
      this.loggedIn$.next(true);
    } else {

      loggerCallback(LogLevel.Info, 'Arbit AuthService: Waiting for server response...');

    }
  }

  private loginWithPopup() {
    console.log('Entering loginWithPopup');
    if (this.msalGuardConfig.authRequest) {
      this.authService.loginPopup({ ...this.msalGuardConfig.authRequest } as PopupRequest)
        .subscribe((response: AuthenticationResult) => {
          this.authService.instance.setActiveAccount(response.account);
        });
    } else {
      this.authService.loginPopup()
        .subscribe((response: AuthenticationResult) => {
          this.authService.instance.setActiveAccount(response.account);
        });
    }
  }

  private loginWithRedirect() {
    console.log('Entering loginWithRedirect');
    if (this.msalGuardConfig.authRequest) {
      this.authService.loginRedirect({ ...this.msalGuardConfig.authRequest } as RedirectRequest).subscribe(data => {
        console.log('authService.loginRedirect.Complete:');
        console.log(data);
      });
    } else {
      this.authService.loginRedirect().subscribe(data => {
        console.log('authService.loginRedirect.Complete:');
        console.log(data);
      });
    }
  }

  logout() {
    this.authService.logout();
  }

  private setLoggedIn() {
    this.loggedIn = this.authService.instance.getAllAccounts().length > 0;
  }

  updateLoggedInStatus() {
    this.msalBroadcastService.inProgress$
      .pipe(
        filter((status: InteractionStatus) => status === InteractionStatus.None),
        takeUntil(this._destroying$)
      )
      .subscribe((v) => {
        this.setLoggedIn();
        this.checkAndSetActiveAccount();
      });
  }
}
