import { Template } from '@angular/compiler/src/render3/r3_ast';
import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LogLevel } from '@azure/msal-browser';
import { NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { loggerCallback } from 'src/app/app.module';
import { AppUser } from 'src/app/model/app-user';
import { CalculatorVariables } from 'src/app/model/calculator-variables';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'app-manage-calculator-variables',
  templateUrl: './manage-calculator-variables.component.html',
  styleUrls: ['./manage-calculator-variables.component.css']
})
export class ManageCalculatorVariablesComponent implements OnDestroy, OnInit {
  //@ViewChild('varsForm', { static: false }) 
  //varsForm!: NgForm;
  @ViewChild('addDialog') 
  addDialog: Template | undefined;
  
  allVariables = new Array<CalculatorVariables>();
  canEdit = false;
  currentUser:AppUser | undefined;
  currentVars: CalculatorVariables | undefined;
  destroyed$ = new Subject<void>();
  isAdmin = false;
  isManager = false;
  modalOptions:NgbModalOptions | undefined;
  serviceLine = '';
  arbitrationFee = 0; // dollars
  chargesCapDiscount = 0;  // percentage
  createdOn:Date|undefined;
  nsaOfferDiscount = 0;
  nsaOfferBaseValueFieldname = '';
  offerCap = 0;  // e.g. 35,000
  offerSpread = 0;   // percentage

  constructor(private svcData:CaseDataService, 
    private svcToast:ToastService,
    private svcUtil:UtilService,
    private router: Router,
    private svcModal: NgbModal,
    private svcAuth: AuthService) { 

      this.modalOptions = {
        backdrop:'static',
        backdropClass:'customBackdrop',
        keyboard: false,
      };
  }

  ngOnInit(): void {
    this.svcUtil.showLoading = true;
    this.subscribeToData();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
  }

  addVars(v:CalculatorVariables) {
    if(!v)
      return;

    this.currentVars = v;
    this.serviceLine = v.serviceLine;
    this.arbitrationFee = v.arbitrationFee;
    this.chargesCapDiscount = v.chargesCapDiscount;
    this.nsaOfferDiscount = v.nsaOfferDiscount;
    this.nsaOfferBaseValueFieldname = v.nsaOfferBaseValueFieldname;
    this.offerCap = v.offerCap;
    this.offerSpread = v.offerSpread;

    this.svcModal.open(this.addDialog, this.modalOptions).result.then(data => {
      
      this.svcUtil.showLoading = true;
      this.svcData.createAppVars(new CalculatorVariables({arbitrationFee: this.arbitrationFee, chargesCapDiscount:this.chargesCapDiscount, nsaOfferDiscount:this.nsaOfferDiscount, nsaOfferBaseValueFieldname:this.nsaOfferBaseValueFieldname, offerCap:this.offerCap, offerSpread:this.offerSpread, serviceLine: this.serviceLine}))
      .subscribe(rec => {
        const idx = this.allVariables.findIndex(v => v.serviceLine === rec.serviceLine);
        this.allVariables.splice(idx,1);
        this.allVariables.push(rec);
        this.allVariables.sort(UtilService.SortByCreatedOnDesc);
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.success,'New Formula Variables created successfully!');
      },
      err => console.warn('Add Variables canceled'),
      () => this.svcUtil.showLoading = false
      );
    },
    reason => {
      loggerCallback(LogLevel.Info,'Add Variables Version canceled');
    });
  }

  hasNewValues() {
    if(!this.currentVars)
      return false;

    const cv = this.currentVars;
    // user must change at least one of the values or we don't allow saving
    let isInvalid = this.arbitrationFee === cv.arbitrationFee && this.chargesCapDiscount === cv.chargesCapDiscount && this.offerCap === cv.offerCap && this.offerSpread === cv.offerSpread && this.nsaOfferDiscount === cv.nsaOfferDiscount && this.nsaOfferBaseValueFieldname == cv.nsaOfferBaseValueFieldname;
    return !isInvalid;
  }

  selectAll(e:any) {
    e?.target?.select();
  }

  subscribeToData() {
    // listen for loading of user info
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(data => {
      this.isManager = !!data.isManager;
      this.isAdmin = !!data.isAdmin;
      this.canEdit = this.isAdmin || this.isManager;
      if(!this.canEdit) {
        UtilService.PendingAlerts.push({level:ToastEnum.danger, message:'Insufficient privileges for managing the Calculator Formulas.'});
        this.router.navigate(['']);
        return;
      }

      this.currentUser = data;
      this.svcData.loadCalculatorVariables()
      .subscribe(data => {
        this.allVariables = data;
        this.allVariables.sort(UtilService.SortByCreatedOnDesc);
        this.svcUtil.showLoading = false;
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger, err.message ?? err.toString());
      },
      () => this.svcUtil.showLoading = false);
    });
  }
}
