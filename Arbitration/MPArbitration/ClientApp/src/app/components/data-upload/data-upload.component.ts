import { Template } from '@angular/compiler/src/render3/r3_ast';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Authority } from 'src/app/model/authority';
import { NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { CaseDataService } from 'src/app/services/case-data.service';
import { UtilService } from 'src/app/services/util.service';
import { loggerCallback } from 'src/app/app.module';
import { LogLevel } from '@azure/msal-browser';
import { ImportLogVM } from 'src/app/model/import-log-vm';
import { take, takeUntil} from 'rxjs/operators';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from 'src/app/services/auth.service';
import { Subject, Subscription, combineLatest, timer } from 'rxjs';
import { ToastEnum } from 'src/app/model/toast-enum';
import { ToastService } from 'src/app/services/toast.service';
import { JobQueueItem } from 'src/app/model/job-queue-item';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'data-upload',
  templateUrl: './data-upload.component.html',
  styleUrls: ['./data-upload.component.css']
})
export class DataUploadComponent implements OnInit, OnDestroy {
  activeJobs:JobQueueItem[] = [];
  allAuthorities:Authority[] = [];
  allLogs:ImportLogVM[] = [];
  authority = '';
  canUpload = false;
  currentFilename = '';
  currentJob:JobQueueItem|undefined;
  destroyed$ = new Subject<void>();
  isError = false;
  isAdmin = false;
  isLoading = false;
  isManager = false;
  isDev = false;
  jobMessage = 'Waiting to start...';
  loadTitle = '';
  loadMessage = 'Successfully updated records';
  modalOptions:NgbModalOptions | undefined;
  sub$:Subscription|undefined;
  @ViewChild('summaryFile', {static: false}) summaryFile: ElementRef | undefined;

  @ViewChild('loadResult') basicModal: Template | undefined;

  constructor(private svcData:CaseDataService, private svcModal: NgbModal, 
              private svcUtil:UtilService, private router:Router,
              private svcAuth:AuthService, private route:ActivatedRoute,
              private svcToast:ToastService) {
    this.modalOptions = {
      backdrop:'static',
      backdropClass:'customBackdrop',
      keyboard: false,
    };
    this.isDev = !environment.production;
    /*
    this.currentFilename=`c:\\blahblah\\this is a filename.csv`;
    this.currentJob=new JobQueueItem();
    this.currentJob.JSON = JSON.stringify({jobType:'import|tx',lastUpdated:new Date(),message:'Uploading some stuff and telling you about it',startTime:new Date(),status:'processing'});
    */
   }

  ngOnInit(): void { 
    this.subscribeToData();
    this.svcToast.resetAlerts();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
    this.sub$?.unsubscribe();
  }

  authorityChange() {
    this.sub$?.unsubscribe();
    this.sub$ = undefined;
    this.currentJob = undefined;
    
    if(this.summaryFile)
      this.summaryFile.nativeElement.value = '';

    if(!this.authority)
      return;

    this.refreshLogFiles();
  }

  fileSelected():boolean {
    return this.summaryFile?.nativeElement.files.length ? true : false;
  }

  fileSelectionChanged() {
    this.currentFilename = '';
    loggerCallback(LogLevel.Verbose,'Upload file selection changed'); // this triggers a blur/change detection or else the button won't light up
  }

  checkActiveJobs() {
    this.canUpload = !this.activeJobs.length;
    if(!this.canUpload)
      this.svcToast.showAlert(ToastEnum.danger, `Only one job may run at a time and there are currently ${this.activeJobs.length} job(s) running. Wait a few minutes and click Refresh or contact Arbit Support.`);
  }

  loadPrerequisites() {
    this.allAuthorities.length = 0;
    const auths$ = this.svcData.loadAuthorities();
    const jobs$ = this.svcData.getJobQueueItemsByType('active');

    combineLatest([auths$, jobs$]).subscribe(([auths,jobs]) => {
      this.allAuthorities.push(...auths);
      if(this.isDev){
        this.allAuthorities.push(new Authority({key:'dsp-d',name:'Dispute Detail Records'}));
        this.allAuthorities.push(new Authority({key:'dsp-h',name:'Dispute Header Records'}));
        this.allAuthorities.push(new Authority({key:'dsp-f',name:'Dispute Fees Records'}));
        this.allAuthorities.push(new Authority({key:'dsp-n',name:'Dispute Notes Records'}));
      }
      this.allAuthorities.push(new Authority({key:'ehr-d',name:'EHR Detail Records'}));
      this.allAuthorities.push(new Authority({key:'ehr-h',name:'EHR Header Records'}));
      this.allAuthorities.push(new Authority({key:'proccodes',name:'Procedure Codes List'}));
      this.allAuthorities.sort(UtilService.SortByName);
      this.activeJobs = jobs;
      this.checkActiveJobs();
    }, 
    err => {
      this.svcUtil.showLoading = false;
      this.svcToast.showAlert(ToastEnum.danger, err);
      console.error(err);
    }, 
    () => this.svcUtil.showLoading = false
    );
  }

  refreshLogFiles(show:boolean = true) {
    this.allLogs.length = 0;
    if(this.authority==='proccodes')
      return;
    this.isLoading = show;
    const jobs$ = this.svcData.getJobQueueItemsByType('active');
    const logs$ = this.svcData.loadUploadLogs(this.authority);
    combineLatest([jobs$,logs$]).subscribe(([jobs,logs]) => {
        this.allLogs = logs.map(v=> new ImportLogVM(v));
        this.allLogs.sort(UtilService.SortByCreatedOnDesc);
        this.activeJobs = jobs;
        this.checkActiveJobs();
      },
      err => {
        console.error(err);
        this.isLoading = false;
      },
      () => this.isLoading = false
    );
  }

  refreshStatus() {
    if(!this.currentJob)
      return;
    this.svcData.getJobQueueItem(this.currentJob.id).subscribe(
      data => {
        this.currentJob = data;
        this.jobMessage = data.jobStatus.message.replace(`\n`,'<br />');
        if(this.currentJob.jobStatus.status === 'finished' || this.currentJob.jobStatus.status === 'error') {
          this.activeJobs = [];
          this.canUpload = true;
          return;
        }
        
        this.sub$ = timer(10000).subscribe(val => this.refreshStatus());
        
      },
      err => {
        this.currentJob = undefined;
        this.sub$?.unsubscribe();
        this.sub$ = undefined;
      },
      () => this.svcUtil.showLoading = false
    );
  }

  showResults() {
    this.refreshLogFiles(false);
    this.svcModal.open(this.basicModal, this.modalOptions);
  }

  subscribeToData() {
    // listen for loading of user info
    this.svcUtil.showLoading = true;
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(data => {
      if(data.id < 1)
        return;

      this.isManager = !!data.isActive && !!data.isManager;
      this.isAdmin = !!data.isActive && !!data.isAdmin;
      if(!this.isAdmin && !this.isManager){
        UtilService.PendingAlerts.push({level:ToastEnum.danger, message:'Insufficient privileges for uploading Case data.'});
        this.router.navigate(['']);
        return;
      }
      
      this.route.params.pipe(takeUntil(this.destroyed$)).subscribe(data => {
        if(this.isAdmin || this.isManager)
          this.loadPrerequisites();
      });
    });
  }

  upload() {
    this.isError = false;
    const files = this.summaryFile?.nativeElement.files; // e.target?.files;
    const f:File = files && files.length ? files[0] : undefined;

    if(f) {
      if(!f.name.toLowerCase().endsWith('.csv')) {
        this.isError = true;
        this.loadTitle = "Document Type Error";
        this.loadMessage = "Invalid document type. Only CSV is supported at this time.";
        this.svcModal.open(this.basicModal, this.modalOptions);
        return;
      }

      this.currentFilename = f.name;

      if(this.authority!=='proccodes') {
        this.uploadAuthorityData(f, this.authority);
        return;
      }

      // eventually this can go back to the prior version when JobQueueItem status monitoring is implemented everywhere on the server
      this.svcUtil.showLoading = true;
      this.svcData.uploadSystemData(f).subscribe(
        { 
          next: (data: any) => {
            this.svcUtil.showLoading = false;
            this.loadMessage = data;
            this.loadTitle = "Upload Complete"
            this.showResults();
            if(this.summaryFile)
              this.summaryFile.nativeElement.value = '';
            },
            error: (err) => {
              this.svcUtil.showLoading = false;
              this.isError = true;
              this.loadMessage = err.error; // 'Be sure you are sending a CSV file with the correct column headers above the data!';
              this.loadTitle = "Upload Failed";
              this.showResults();
              if(this.summaryFile)
                this.summaryFile.nativeElement.value = '';
            }
        }
      );

    } else {
      this.isError = true;
      this.loadMessage = 'Unable to read file. Be sure you are sending a CSV file with the correct column headers above the data!';
      this.loadTitle = "Error";
      this.showResults();
    }
  }

  uploadAuthorityData(f:File,authority:string) {
    this.svcUtil.showLoading = true;
    this.currentJob = undefined;
    this.svcData.uploadAuthorityData(f, authority).subscribe(data => {
      if(this.summaryFile)
        this.summaryFile.nativeElement.value = '';

      this.currentJob = new JobQueueItem(data);
      if(this.currentJob.jobStatus.status !== 'finished') {
        this.activeJobs = [this.currentJob];
        this.canUpload = false;
        this.sub$ = timer(10000).subscribe(val => this.refreshStatus());
      }
    },
    err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
    () => this.svcUtil.showLoading = false);
  }

  viewFile(f:ImportLogVM) {
    if(!this.authority)
      return;
    let a = this.authority;
    // some overrides for system files
    if(this.authority === 'ehr-h')
      a = 'EHRHeader';
    else if(this.authority === 'ehr-d')
      a = 'EHRDetail';
    else if(this.authority==='dsp-d')
      a = 'DisputeDetail';
    else if(this.authority==='dsp-h')
      a = 'DisputeHeader';
    else if(this.authority==='dsp-f')
      a = 'DisputeFee';
    else if(this.authority==='dsp-n')
      a = 'DisputeNote';
    
    this.svcData.downloadLog(a,f.blobName).pipe(take(1)).subscribe(res => {
      const fileURL = URL.createObjectURL(res);
      window.open(fileURL, '_blank');
    });
  }
}
