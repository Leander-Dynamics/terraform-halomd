import { Component, OnInit } from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { Subject, combineLatest } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AppUser } from 'src/app/model/app-user';
import { ArbitrationCase } from 'src/app/model/arbitration-case';
import { Authority } from 'src/app/model/authority';
import { AuthorityDispute } from 'src/app/model/authority-dispute';
import { Customer } from 'src/app/model/customer';
import { ToastEnum } from 'src/app/model/toast-enum';
import { WorkQueueName } from 'src/app/model/work-queue-name-enum';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';
import { UpdateDisputeQueueItemComponent } from '../update-dispute-queue-item/update-dispute-queue-item.component';
import { AuthorityDisputeNote } from 'src/app/model/note';
import { AuthorityDisputeWorkItem } from 'src/app/model/authority-dispute-work-item';

@Component({
  selector: 'app-user-work-queue',
  templateUrl: './user-work-queue.component.html',
  styleUrls: ['./user-work-queue.component.css']
})
export class UserWorkQueueComponent implements OnInit {
  allAuthorities = new Array<Authority>();
  allBriefApprovals = new Array<AuthorityDispute>();
  allBriefCreations = new Array<AuthorityDispute>();
  allBriefPreps = new Array<AuthorityDispute>();
  allCustomers = new Array<Customer>();
  allDisputes = new Array<AuthorityDispute>();
  allUsers = new Array<AppUser>();
  currentUser:AppUser = new AppUser();
  destroyed$ = new Subject<void>();
  hideBriefApprover = false;
  hideBriefPreparer = false;
  hideBriefWriter = false;
  isAdmin = false;
  isBriefApprover = false;
  isBriefPreparer = false;
  isBriefWriter = false;
  isDev = false;
  isManager = false;
  isNegotiator = false;
  showActions = true;
  showHelp = false;

  readonly WorkQueueName = WorkQueueName;

  constructor(private svcData: CaseDataService, private svcAuth:AuthService,
    private svcToast: ToastService,  private svcUtil: UtilService, 
    private modalService: NgbModal) { }

    ngOnDestroy(): void {
      this.destroyed$.next();  
      this.destroyed$.complete();
    }

  ngOnInit(): void {
    this.subScribeToData();
    this.loadPrerequisites();
  }

  applyFilters() {}

  getNextDispute(queue:WorkQueueName,authority:Authority|undefined){
    this.svcUtil.showLoading = true;
    if(!authority)
      authority = this.allAuthorities.find(v=>v.key.toLowerCase()==='nsa');
    this.svcData.getNextQueueItem(authority!,queue).subscribe(
      data => {
        this.allDisputes.push(data);
        this.updateQueues();
      },
      err => {
        this.svcUtil.showLoading = false;
        if(err.status===404){
          this.svcToast.show(ToastEnum.warning,'The queue is currently empty. Try again later.');
        } else {
          UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast);
        }
      },
      () => this.svcUtil.showLoading = false
    );
  }

  loadPrerequisites() {
    this.svcUtil.showLoading = true;
    const authorities$ = this.svcData.loadAuthorities();
    const disputes$ = this.svcData.getCurrentDisputeQueueItems(undefined,WorkQueueName.All);
    const users$ = this.svcData.loadUsers();
    
    combineLatest([authorities$,disputes$,users$]).subscribe(
      ([authorities,disputes,users]) => {
        this.allAuthorities = authorities;
        this.allAuthorities.sort(UtilService.SortByName);
        // disputes
        this.allDisputes = disputes;
        this.updateQueues();
        // users
        this.allUsers = users;
        this.allUsers.unshift(new AppUser({email:'(unassigned)'}));
        this.allUsers.sort(UtilService.SortByEmail);  
      },
      err => UtilService.HandleServiceErr(err,this.svcUtil,this.svcToast),
      () => this.svcUtil.showLoading = false
    );
  }

  getAssignedUser(queue:WorkQueueName,record:AuthorityDispute|ArbitrationCase) {
    if(record instanceof AuthorityDispute) {
      switch(queue) {
        case WorkQueueName.DisputeBriefApprover:
          return this.currentUser.email;
        case WorkQueueName.DisputeBriefPreparer:
          return record.briefPreparer;
        case WorkQueueName.DisputeBriefWriter:
          return record.briefWriter;
      }
    }

    return '';
  }

  markComplete(queue:WorkQueueName,record:AuthorityDispute|ArbitrationCase) {
    if(record instanceof AuthorityDispute) {
      // complete the AuthorityDispute
      const modalRef = this.modalService.open(UpdateDisputeQueueItemComponent);
      modalRef.componentInstance.title = 'Work Completed';
      modalRef.componentInstance.notes = '';
      modalRef.componentInstance.workQueue = queue;
      modalRef.componentInstance.isReassigning = false;
      const assignedTo = this.getAssignedUser(queue,record);

      modalRef.closed.subscribe(data => {
        let notes = modalRef.componentInstance.notes;
        const update = new AuthorityDisputeWorkItem();
        update.assignedUser = assignedTo;
        update.disputeId = record.id;
        update.workQueue = queue;
        
        if(!!notes) {
          const note = new AuthorityDisputeNote();
          note.details = notes;
          note.authorityDisputeId = record.id;
          note.id = 0;
          update.note = note;
        } 

        this.svcUtil.showLoading = true;
        this.svcData.updateDisputeWorkItem(update).subscribe(
          data => {
            this.svcToast.show(ToastEnum.success, 'Item completed successfully!');
            const i = this.allDisputes.findIndex(v=>v.id===record.id);
            this.allDisputes.splice(i,1);
            this.updateQueues();
          },
          err => UtilService.HandleServiceErr(err,this.svcUtil,this.svcToast),
          () => this.svcUtil.showLoading = false
        )
      });
    }
  }
  
  refreshAll() {
    this.svcUtil.showLoading = true;
    this.svcData.getCurrentDisputeQueueItems(undefined,WorkQueueName.All).subscribe(
      data => {
        this.allDisputes = data;
        this.updateQueues();
      },
      err => UtilService.HandleServiceErr(err,this.svcUtil,this.svcToast),
      () => this.svcUtil.showLoading = false
    );
  }

  subScribeToData() {
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(
      data => {
        if(data.email) {
          this.currentUser = data;
          this.isAdmin = !!this.currentUser.isAdmin;
          this.isManager = !!this.currentUser.isManager;
          this.isNegotiator = !!this.currentUser.isNegotiator;
          this.isBriefApprover = !!this.currentUser.isBriefApprover;
          this.isBriefPreparer = !!this.currentUser.isBriefPreparer;
          this.isBriefWriter = !!this.currentUser.isBriefWriter;
        }
      }
    );
  }

  updateQueues() {
    this.allBriefCreations = this.allDisputes.filter(v=>v.briefWriter.toLowerCase()===this.currentUser.email&&!v.briefWriterCompletedOn);
    this.allBriefPreps = this.allDisputes.filter(v=>v.briefPreparer.toLowerCase()===this.currentUser.email&&!v.briefPreparationCompletedOn);
    if(this.isBriefApprover || this.isManager)
      this.allBriefApprovals = this.allDisputes.filter(v=>!!v.briefWriterCompletedOn&&!v.briefApprovedOn);
  }
}
