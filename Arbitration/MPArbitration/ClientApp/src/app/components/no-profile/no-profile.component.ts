import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from 'src/app/services/auth.service';
import { takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'app-no-profile',
  templateUrl: './no-profile.component.html',
  styleUrls: ['./no-profile.component.css']
})
export class NoProfileComponent implements OnInit {
  destroyed$ = new Subject<void>();
  
  constructor(private svcAuth:AuthService, private route:ActivatedRoute, private router:Router, private svcUtil:UtilService) { }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
  }

  ngOnInit(): void {
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(user => {
      if(user.isActive) {
        this.router.navigate(['']);
      }
    });
    this.svcUtil.showLoading = false;
    
  }

}
