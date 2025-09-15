import { Injectable } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  CanActivate,
  CanActivateChild,
  CanLoad,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { LogLevel } from '@azure/msal-browser';
import { map } from 'jquery';
import { Observable, of, pipe } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { loggerCallback } from '../app.module';
import { AuthService } from '../services/auth.service';
import { CaseDataService } from '../services/case-data.service';
import { ToastService } from '../services/toast.service';
import { UtilService } from '../services/util.service';
import { AppUser } from './app-user';
import { ToastEnum } from './toast-enum';

@Injectable({
  providedIn: 'root',
})
export class HasUserProfileGuard implements CanActivate {
  constructor(
    private svcAuth: AuthService,
    private svcData: CaseDataService,
    private router: Router,
    private svcUtil: UtilService,
    private svcToast: ToastService
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ):
    | boolean
    | UrlTree
    | Observable<boolean | UrlTree>
    | Promise<boolean | UrlTree> {
    if (this.svcAuth.currentUser$.getValue().email) return of(true);
    else {
      loggerCallback(LogLevel.Info, 'canActivate requesting data');
      return this.svcData.getCurrentUser().pipe(
        tap((data: AppUser) => {
          if (data) this.svcAuth.currentUser$.next(new AppUser(data));
        }),

        switchMap((data: AppUser) => {
          if (data && data.isActive) {
            return of(true);
          } else {
            return of(this.router.createUrlTree(['/', 'noprofile']));
          }
        }),
        catchError((err) => {
          this.svcUtil.showLoading = false;
          this.svcToast.showAlert(
            ToastEnum.danger,
            'Unable to retrieve your App Profile. Is your account inactive?'
          );
          loggerCallback(
            LogLevel.Error,
            'Error fetching current user profile in HasProfileGuard'
          );
          loggerCallback(LogLevel.Error, err);
          this.router.navigate(['noprofile']);
          return of(false);
        })
      );
    }
  }
}
