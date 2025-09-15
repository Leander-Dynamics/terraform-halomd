import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AuthService } from './services/auth.service';
import { ToastService } from './services/toast.service';
import { UtilService } from './services/util.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent {
  destroyed$ = new Subject<void>();
  isLoading = true;
  title = 'MPowerHealth - Arbitration Calculator';
  constructor(public svcToast: ToastService,
    public authService: AuthService,
    public svcLoading: UtilService,
    public svcChange: ChangeDetectorRef) { }

  ngOnInit(): void {
    this.authService.updateLoggedInStatus();
    this.svcLoading.showLoading$.pipe(takeUntil(this.destroyed$)).subscribe(
      data => {
        if (data !== this.isLoading) {
          this.isLoading = data;
          this.svcChange.detectChanges();
        }
      }
    );
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  login() {
    this.authService.login();
  }

  logout() {
    this.authService.logout();
  }
}
