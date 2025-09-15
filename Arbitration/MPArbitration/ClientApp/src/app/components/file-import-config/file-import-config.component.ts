import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { loggerCallback } from 'src/app/app.module';
import { Authority } from 'src/app/model/authority';
import { ImportFieldConfig } from 'src/app/model/import-field-config';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'app-file-import-config',
  templateUrl: './file-import-config.component.html',
  styleUrls: ['./file-import-config.component.css']
})
export class FileImportConfigComponent implements OnDestroy, OnInit {
  allAuthorities = new Array<Authority>();
  authority = '';
  fileSource = '';
  destroyed$ = new Subject<void>();
  isAdmin = false;
  ImportActions = ['Always','Ignore','Only When Empty','Never With Empty'];
  records$ = new BehaviorSubject<ImportFieldConfig[]>([]);
  recCount = 0;

  constructor(private svcData:CaseDataService, private svcToast:ToastService, 
              private svcUtil:UtilService, private svcAuth:AuthService, 
              private router:Router) { }

  ngOnInit(): void {
    this.subscribeToData();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
    this.records$.complete();
  }

  loadPrerequisites() {
    this.svcData.loadAuthorities().subscribe(data => {
      this.allAuthorities = data;
      this.allAuthorities.sort(UtilService.SortByName);
    },
    err => {
      this.svcToast.showAlert(ToastEnum.danger,'Unable to load Authorities');
      console.error(err);
      this.svcUtil.showLoading = false;
    },
    () => this.svcUtil.showLoading = false
    );
  }

  recChanged(rec: ImportFieldConfig,flag:string ='') {
    this.svcUtil.showLoading = true;
    this.recCount = 0;
    if(flag){
      rec.isBoolean= flag==='bool' ? rec.isBoolean : false;
      rec.isDate= flag ==='date' ? rec.isDate : false;
      rec.isNumeric= flag === 'numeric' ? rec.isNumeric : false;
    }
    this.svcData.updateFieldConfig(rec).subscribe(data => {
      const recs = this.records$.getValue();
      const i = recs.findIndex(d => d.id === data.id);
      if(i > -1) {
        recs[i] = data;
        this.records$.next(recs);
        this.svcToast.show(ToastEnum.success, 'Configuration updated successfully', 'Record Saved')
      } else {
        this.svcToast.show(ToastEnum.warning,'Unexpected error! Could not locate saved record by index.','Warning');
      }
    },
    err => {
      this.svcUtil.showLoading = false;
      this.svcToast.show(ToastEnum.danger,'Error saving!');
    },
    () => {
      this.svcUtil.showLoading = false;
    });
  }

  sourceChange() {
    const reqAuth = this.fileSource==='RequestDetails'||this.fileSource==='CaseSync'; // show the authority picker
    this.authority = !reqAuth ? '' : this.authority;
    
    if(this.fileSource && (!reqAuth || this.authority)) {
      const a = this.authority ? this.authority + '-' : '';
      this.svcUtil.showLoading = true;
      this.svcData.loadImportConfig(a + this.fileSource).subscribe(data => {
        this.records$.next(data);
        this.recCount = data.length;
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger,'Error loading configuration!');
      },
      () => this.svcUtil.showLoading = false
      );
    } else {
      this.records$.next([]);
    }
  }

  subscribeToData() {
   // listen for loading of user info
   this.svcUtil.showLoading = true;
   this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(data => {
     if((!data.isAdmin&&!data.isManager)||!data.isActive){
       UtilService.PendingAlerts.push({level:ToastEnum.danger, message:'Insufficient privileges for managing the File Import Configuration.'});
       this.router.navigate(['']);
       return;
     }
     this.isAdmin=!!data.isAdmin;
     this.loadPrerequisites();
   },
   err => {
      UtilService.PendingAlerts.push({level:ToastEnum.danger, message:'Error retrieving data from the server. Returning to search page.'});
      this.router.navigate(['']);
   });
  }
}
