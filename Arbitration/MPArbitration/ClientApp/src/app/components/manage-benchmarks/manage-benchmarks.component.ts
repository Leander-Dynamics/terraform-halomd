import { Template } from '@angular/compiler/src/render3/r3_ast';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LogLevel } from '@azure/msal-browser';
import { NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { loggerCallback } from 'src/app/app.module';
import { AppUser } from 'src/app/model/app-user';
import { BenchmarkDataset } from 'src/app/model/benchmark-dataset';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'app-manage-benchmarks',
  templateUrl: './manage-benchmarks.component.html',
  styleUrls: ['./manage-benchmarks.component.css']
})
export class ManageBenchmarksComponent implements OnInit {
  @ViewChild('benchmarkForm', { static: false }) benchmarkForm!: NgForm;
  @ViewChild('benchmarksFile', {static: false}) benchmarksFile: ElementRef | undefined;
  @ViewChild('loadResult') basicModal: Template | undefined;
  allBenchmarks:BenchmarkDataset[] = [];
  canEdit = true;
  orig:BenchmarkDataset | null = null;
  currentBenchmark:BenchmarkDataset | null = null;
  currentItemCount = 0;
  currentUser:AppUser | undefined;
  benchmarkName = '';
  destroyed$ = new Subject<void>();
  isAdmin = false;
  isManager = false;
  isError = false;
  loadTitle = '';
  loadMessage = 'Upload complete';
  modalOptions:NgbModalOptions | undefined;
  showUpload = false;

  constructor(private svcData:CaseDataService, private svcToast:ToastService, 
    private svcUtil:UtilService, private svcAuth: AuthService, 
    private svcModal: NgbModal, private router:Router,
    private route: ActivatedRoute) { 
      this.modalOptions = {
        backdrop:'static',
        backdropClass:'customBackdrop',
        keyboard: false,
      };
    }

  ngOnInit(): void {
    this.subscribeToData();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
  }

  addBenchmark() {
    let c = this.allBenchmarks.find(d=>d.id===0);
    if(c) {
      this.currentBenchmark = c;
    } else {
      c = new BenchmarkDataset();
      c.name = "(new)";
      this.allBenchmarks.push(c);
      this.currentBenchmark = c;
    }
  }

  cancelChanges() {
    if(!this.currentBenchmark)
      return;
    if(!confirm('Are you sure you want to cancel?'))
      return;
    if(this.currentBenchmark.id===0) {
      const i = this.allBenchmarks.findIndex(d => d.id === 0);
      this.allBenchmarks.splice(i,1);
      this.currentBenchmark = null;
    } else if(this.orig) {
      this.currentBenchmark.dataYear = this.orig.dataYear;
      this.currentBenchmark.isActive = this.orig.isActive;
      this.currentBenchmark.key = this.orig.key;
      this.currentBenchmark.name = this.orig.name;
      this.currentBenchmark.valueFields = this.orig.valueFields;
      this.currentBenchmark.vendor = this.orig.vendor;
    }
    this.resetFormStatus();
  }

  benchmarkSelected() {
    if(!!this.currentBenchmark) {
      this.orig = new BenchmarkDataset(this.currentBenchmark);
      this.svcData.loadBenchmarkItemCount(this.currentBenchmark).subscribe(rec => {
        this.currentItemCount = rec;
      }, err => {
          loggerCallback(LogLevel.Error, 'Error getting benchmark item count');
          loggerCallback(LogLevel.Error, err);
      });
    } else {
      this.orig = null;
      this.currentItemCount = 0;
    }
  }

  fileSelected():boolean {
    return this.benchmarksFile?.nativeElement.files.length ? true : false;
  }

  fileSelectionChanged() {
    loggerCallback(LogLevel.Verbose,'Upload file selection changed'); // this triggers a blur/change detection or else the button won't light up
  }

  loadPrerequisites() {
    this.svcUtil.showLoading = true;
    this.svcData.loadBenchmarkDatasets().subscribe(data => {
      this.allBenchmarks = data;
      this.allBenchmarks.sort(UtilService.SortByName);
    },
    err => {
      this.svcUtil.showLoading = false;
      this.svcToast.showAlert(ToastEnum.danger, 'Unable to load the list of Benchmark Datasets');
    },
    () => this.svcUtil.showLoading = false
    );
  }

  onSubmit(): boolean {
    if (!this.benchmarkForm?.valid) 
      return false;
      
    this.saveChanges();
    return true;
  }

  resetFormStatus() {
    Object.keys(this.benchmarkForm.controls).forEach((key) => {
      const control = this.benchmarkForm.controls[key];
      control.markAsPristine();
      control.markAsUntouched();
    });
  }

  saveChanges() {
    if(!this.currentBenchmark)
      return;

    this.svcUtil.showLoading = true;
    if(this.currentBenchmark.id === 0) {
      // create
      this.svcData.createBenchmarkDataset(this.currentBenchmark).subscribe(rec => {
        this.updateBenchmarkDatasetCollection(0, rec);
        this.svcToast.show(ToastEnum.success,'New Benchmark Dataset created successfully!');
        this.resetFormStatus();
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger,err.error || 'Error creating Benchmark Dataset! Please try again.');
      },
      () => this.svcUtil.showLoading = false
      );

    } else if(this.currentBenchmark.id > 0) {
      // update
      this.svcData.updateBenchmarkDataset(this.currentBenchmark.id, this.currentBenchmark).subscribe(rec => {
        this.updateBenchmarkDatasetCollection(rec.id, rec);
        this.svcToast.show(ToastEnum.success,'Benchmark Dataset updated successfully!');
        this.resetFormStatus();
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger,err.error || 'Error updating Benchmark Dataset: ' + err.message);
      },
      () => this.svcUtil.showLoading = false
      );
    }
  }
  
  selectAll(e:any) {
    e?.target?.select();
  }

  showResults() {
    this.svcModal.open(this.basicModal, this.modalOptions);
  }
  
  subscribeToData() {
    this.svcUtil.showLoading = true;
    // listen for loading of user info
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(data => {
      this.isManager = !!data.isManager;
      this.isAdmin = !!data.isAdmin
      this.canEdit = this.isAdmin || this.isManager;
      if(!this.canEdit){
        UtilService.PendingAlerts.push({level:ToastEnum.danger, message:'Insufficient privileges for managing the Benchmarks list.'});
        this.router.navigate(['']);
        return;
      }

      this.currentUser = data;
      this.route.params.pipe(takeUntil(this.destroyed$)).subscribe(data => {
        this.loadPrerequisites();
      });
    });
  }

  updateBenchmarkDatasetCollection(id:number,rec:BenchmarkDataset) {
    let ndx = -1;
    this.allBenchmarks.forEach((item, index) => {
      if (item.id === id) {
        this.allBenchmarks[index] = rec;
        ndx = index;
      }
    });

    this.currentBenchmark = ndx > -1 ? this.allBenchmarks[ndx] : null;
    this.benchmarkSelected();
    
    this.allBenchmarks.sort(UtilService.SortByName);
  }

  upload() {
    if(!this.currentBenchmark)
      return;
    this.isError = false;
    const files = this.benchmarksFile?.nativeElement.files; // e.target?.files;
    const f:File = files && files.length ? files[0] : undefined;

    if(f) {
      if(!f.name.toLowerCase().endsWith('.csv')) {
        this.isError = true;
        this.loadTitle = "Document Type Error";
        this.loadMessage = "Invalid document type. Only CSV is supported at this time.";
        this.svcModal.open(this.basicModal, this.modalOptions);
        return;
      }
      
      this.svcUtil.showLoading = true;
      this.svcData.uploadBenchmarkData(f,this.currentBenchmark.id,this.currentBenchmark.key).subscribe(
        { 
          next: (data: any) => {
            this.svcUtil.showLoading = false;
            this.loadMessage = data;
            this.loadTitle = "Upload Complete"
            this.showResults();
            if(this.benchmarksFile)
              this.benchmarksFile.nativeElement.value = '';
            this.showUpload = false;
            },
            error: (err) => {
              this.svcUtil.showLoading = false;
              this.isError = true;
              this.loadMessage = err.message || err.toString(); // 'Be sure you are sending a CSV file with the correct column headers above the data!';
              this.loadTitle = "Upload Failed";
              this.showResults();
              if(this.benchmarksFile)
                this.benchmarksFile.nativeElement.value = '';
              this.showUpload = false;
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
}
