import {
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
} from '@angular/core';
import { AuthService } from '../services/auth.service';
import { AppUser, UserAccessType } from '../model/app-user';
import { takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { environment } from 'src/environments/environment';
import { GuardsCheckEnd, Router } from '@angular/router';
import { isTestEnv, getCurrentEnvName } from '../app.module';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css'],
})
export class NavMenuComponent implements OnDestroy, OnInit {
  destroyed$ = new Subject<void>();
  currentApplicationVersion = environment.appVersion.split('-')[0];
  currentUser: AppUser | undefined | null = null;
  isActive = false;
  isAdmin = false;
  isManager = false;
  isNSA = false;
  isNegotiator = false;
  isReporter = false;
  isExpanded = false;
  isTestEnv = false;
  lastDestination = '';
  showMyItems = false;
  useremail = '';
  username = '';
  jumpId = '';
  _loggedIn = false;
  domainName = '';
  get loggedIn(): boolean {
    return this._loggedIn;
  }

  jumpIdOrDisputeChange() {
    if (!!this.jumpId) {
      if (this.jumpId.toUpperCase().startsWith('DISP')) {
        // this.svcRouter.navigate(['/', 'batch', 'find'], {
        //   queryParams: { dispute: this.jumpId },
        // });
        this.svcRouter.navigate(['/', 'dispute', this.jumpId]);
      } else {
        this.svcRouter.navigate(['/', 'calculator', this.jumpId]);
      }
    }
  }

  @Input() set loggedIn(value: boolean | null) {
    if (!!value === this._loggedIn) return;

    this._loggedIn = !!value;
    if (this._loggedIn) {
      const nfo = this.svcAuth.getActiveAccount();
      this.username = nfo?.name ?? nfo?.username ?? 'Click to login';
      this.useremail = nfo?.username ?? this.username;
    } else {
      this.username = 'Click to login';
    }
  }

  @Output() loginClicked = new EventEmitter<void>();
  @Output() logoutClicked = new EventEmitter<void>();

  constructor(private svcAuth: AuthService, private svcRouter: Router) {
    this.isTestEnv = isTestEnv();
    this.domainName = getCurrentEnvName();
  }

  ngOnDestroy() {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  ngOnInit(): void {
    // in case we have to send the user off to Microsoft for login, let's try to send the back to their original destination
    this.svcRouter.events.pipe(takeUntil(this.destroyed$)).subscribe((evt) => {
      if (evt instanceof GuardsCheckEnd) {
        this.lastDestination = evt.urlAfterRedirects;
      }
    });

    // listen for a successful login
    this.svcAuth.loginSuccess$
      .pipe(takeUntil(this.destroyed$))
      .subscribe(() => {
        if (this.lastDestination) {
          console.log('Redirecting user to their original destination...');
          const d = this.lastDestination;
          this.lastDestination = '';
          this.svcRouter.navigateByUrl(d);
        }
      });

    this.svcAuth.currentUser$
      .pipe(takeUntil(this.destroyed$))
      .subscribe((data) => {
        if (!data.email || !data.id) return;
        this.currentUser = data;
        this.isActive = !!data.isActive;
        if (this.isActive) {
          this.isAdmin = !!data.isAdmin;
          this.isManager = !!data.isManager;
          this.isNegotiator = !!data.isNegotiator;
          this.isReporter = !!data.isManager;
          this.isNSA = !!data.isNSA;
          const canEdit = this.isManager || this.isNegotiator;
          if (!canEdit && data.appRoles) {
            const r = data.appRoles.find(
              (d) =>
                d.accessLevel === UserAccessType.manager ||
                d.accessLevel === UserAccessType.negotiator
            );
            if (r) {
              this.isManager =
                this.isManager || r.accessLevel === UserAccessType.manager;
              this.isNegotiator =
                this.isNegotiator ||
                r.accessLevel === UserAccessType.negotiator;
            }
          }
          this.showMyItems =
            this.isManager ||
            !!this.currentUser!.isBriefPreparer ||
            !!this.currentUser!.isBriefWriter;
        }
      });
  }

  collapse() {
    this.isExpanded = false;
  }
  /*
  getUsername() {
    if(this.loggedIn) {
      const nfo = this.svcAuth.getActiveAccount();
      return nfo?.name ?? nfo?.username;
    } else {
      return 'Click to login';
    }
  }
  */
  toggle() {
    this.isExpanded = !this.isExpanded;
  }

  login() {
    this.loginClicked.emit();
  }

  logout() {
    this.logoutClicked.emit();
  }
}
