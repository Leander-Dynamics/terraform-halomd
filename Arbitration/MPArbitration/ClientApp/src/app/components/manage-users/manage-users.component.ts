import { Template } from '@angular/compiler/src/render3/r3_ast';
import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LogLevel } from '@azure/msal-browser';
import { NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, combineLatest, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { loggerCallback } from 'src/app/app.module';
import { AppRole, AppUser, GranularRoleVM, IAppRole, UserRoleType, UserAccessType, IAppRoleVM } from 'src/app/model/app-user';
import { Authority } from 'src/app/model/authority';
import { Customer } from 'src/app/model/customer';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'app-manage-users',
  templateUrl: './manage-users.component.html',
  styleUrls: ['./manage-users.component.css']
})
export class ManageUsersComponent implements OnDestroy, OnInit {
  @ViewChild('addDialog') 
  addDialog: Template | undefined;
  allAuthorities = new Array<Authority>();
  allCustomers = new Array<Customer>();
  newUser = new AppUser();
  allUsers: AppUser[] = [];
  canEdit = false;
  destroyed$ = new Subject<void>();
  isLoading = false;
  modalOptions:NgbModalOptions | undefined;
  output$ = new BehaviorSubject<AppUser>(this.newUser);
  user:AppUser | undefined;
  userGranularRoles = new Array<GranularRoleVM>();
  visibleUserId = 0;

  constructor(private svcData:CaseDataService, 
              private svcToast:ToastService,
              private svcUtil:UtilService,
              private svcModal: NgbModal,
              private route: ActivatedRoute,
              private router: Router,
              private svcAuth: AuthService) { 

                this.modalOptions = {
                  backdrop:'static',
                  backdropClass:'customBackdrop',
                  keyboard: false,
                };
  }

  ngOnInit(): void {
    this.subscribeToData();
  }

  subscribeToData() {
    this.svcUtil.showLoading = true;

    // listen for loading of user info
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(data => {
      this.canEdit=!!data.isAdmin;
      if(!data.isActive){
        UtilService.PendingAlerts.push({level:ToastEnum.danger, message:'Only active users may view the current Users list.'});
        this.router.navigate(['']);
        return;
      }
      
      this.route.params.pipe(takeUntil(this.destroyed$)).subscribe(data => {
        this.loadPrerequisites();
      });
    });
  }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
    this.output$.complete();
  }

  addUser() {
    this.newUser = new AppUser();
    this.svcModal.open(this.addDialog, this.modalOptions).result
    .then(data => {
      if(this.newUser.email) {
        this.svcUtil.showLoading = true;
        
        this.svcData.createUser(this.newUser).subscribe(
          data => {
            this.svcToast.show(ToastEnum.success,`User ${data.email} created successfully!`);
          },
          err => {
            this.svcToast.show(ToastEnum.danger, err.message);
            this.svcUtil.showLoading = false;
          },
          () => {
            this.svcUtil.showLoading = false;
          }
        )
      }
    },
    err => {
      loggerCallback(LogLevel.Info,'Create User canceled');
    });
  }

  getAccessLevel(g:IAppRoleVM):UserAccessType {
    if(g.isManager)
      return UserAccessType.manager;
    else if(g.isNegotiator)
      return UserAccessType.negotiator;
    else if(g.isReporter)
      return UserAccessType.reporter;
    else
      return UserAccessType.denied;
  }

  flattenAppRoles(roles:Array<IAppRole>|undefined) {
    let r = '';

    if(!roles?.length)
      return r;
    
    for(const a of roles)
    {
      r+= a.roleType===UserRoleType.Authority ? 'a|':'';
      r+= a.roleType===UserRoleType.Customer ? 'c|':'';
      r+= a.entityId+'|';
      r+= UserAccessType[a.accessLevel]+';'
    }
    return r.slice(0,-1);
  }

  getGlobalRoles(u:AppUser) {
    const a = u.isAdmin?'admin;':'';
    const b = u.isManager?'manager;':'';
    const c = u.isNegotiator?'negotiator;':'';
    const d = u.isReporter?'reporter;':'';
    const e = u.isNSA?'nsa;':'';
    const f = u.isState?'state;':'';
    const g = u.isBriefApprover?'briefapprover;':'';
    const h = u.isBriefPreparer?'briefpreparer;':'';
    const i = u.isBriefWriter?'briefwriter;':'';
    return (a+b+c+d+e+f+g+h+i); //.slice(0,-1);
  }

  hasGlobalRoles(u:AppUser) {
    return u.isManager || u.isNegotiator || u.isReporter;
  }

  loadPrerequisites() {

    this.svcUtil.showLoading = true;
    const authorities$ = this.svcData.loadAuthorities();
    const customers$ = this.svcData.loadCustomers();
    const users$ = this.svcData.loadUsers();

    combineLatest([authorities$,customers$,users$]).subscribe(
      ([authorities,customers,users]) => {

      this.allAuthorities = authorities;
      this.allAuthorities.sort(UtilService.SortByName);
      
      this.allCustomers = customers;
      this.allCustomers.sort(UtilService.SortByName);
      
      this.allUsers = users;
      this.allUsers.sort(UtilService.SortByEmail);  
    },
    err => {
      this.svcUtil.showLoading = false;
      this.svcToast.showAlert(ToastEnum.danger,'Failed to load necessary pre-requisites for managing Users!');
      this.router.navigate(['/search']);
    },
    () => {
      this.svcUtil.showLoading = false;
    });
  }

  saveChanges(u:AppUser) {
    this.svcData.updateUser(u).subscribe(
      data => {
        if(data.email.toLowerCase() === this.svcAuth.currentUser$.getValue().email.toLowerCase()) {
          this.svcAuth.currentUser$.next(data); // immediately enforce current user's new settings
        }
        this.svcToast.show(ToastEnum.success,`User ${data.email} updated successfully!`);
        u=data;
        if(u.id===this.visibleUserId) {
          this.toggleGranular(u,!this.hasGlobalRoles(u));
        }
      },
      err => {
        this.svcToast.show(ToastEnum.danger, err.message);
        this.svcUtil.showLoading = false;
      },
      () => {
        this.svcUtil.showLoading = false;
      }
    )
  }

  setAccessFlags(g:IAppRoleVM, r:string='') {
    // unset all but the selected role
    g.isManager = (g.isManager && r==='manager');
    g.isNegotiator = (g.isNegotiator && r==='negotiator');
    g.isReporter = (g.isReporter && r==='reporter');
  }

  setUserRoles(u:AppUser) {
    let globe = this.getGlobalRoles(u);
    if(!u.isManager&&!u.isNegotiator&&!u.isReporter) {
      let flat = this.flattenAppRoles(u.appRoles);
      globe+= flat;
    }
    globe = globe.endsWith(';') ? globe.slice(0,-1) : globe
    u.roles = globe;
  }

  /** Fired when user expands/collapses a user's granular roles */
  toggleGranular(u:AppUser,keepOpen:boolean=false) {
    this.userGranularRoles.length = 0; // clear out temp view model collection
    
    this.visibleUserId = this.visibleUserId === u.id && !keepOpen ? 0 : u.id;
    if(!this.visibleUserId)
      return;
    
    for(const customer of this.allCustomers) {
      let j = new GranularRoleVM({name: customer.name, entityId: customer.id, userId: u.id, isManager: false, isNegotiator: false, isReporter: false, roleType:UserRoleType.Customer});
      const r = u.appRoles?.find(d=>d.roleType === UserRoleType.Customer && d.entityId == customer.id);
      if(r) {
        j.isManager = (r.accessLevel === UserAccessType.manager);
        j.isNegotiator = (r.accessLevel === UserAccessType.negotiator);
        j.isReporter = (r.accessLevel === UserAccessType.reporter);
        j.accessLevel = r.accessLevel;
      }
      this.userGranularRoles.push(j);
    }
  }

  toggleGranularRole(u:AppUser, g:GranularRoleVM, r:string) {
    if(!r)
      return;

    this.setAccessFlags(g,r);
    g.accessLevel = this.getAccessLevel(g);
    u.appRoles = this.userGranularRoles.filter(b => b.accessLevel!=UserAccessType.denied).map(d => new AppRole(d.roleType, d.accessLevel, d.entityId));

    this.setUserRoles(u);
    this.saveChanges(u);
  }
  
  /** Fired when user changes a global role */
  toggleRole(u: AppUser,r: string = '') {
    if(u.email.toLowerCase() === this.svcAuth.currentUser$.getValue().email.toLowerCase()){
      if(!confirm('You are about to modify your own profile. ARE YOU SURE?')){
        const x = this.allUsers.findIndex(d => d.id === u.id);
        this.allUsers[x] = new AppUser(this.svcAuth.currentUser$.getValue());
        return;
      }
    }
    
    if(r&&r!=='nsa'&&r!=='state'&&r!=='briefapprover'&&r!=='briefpreparer'&&r!=='briefwriter')
      this.setAccessFlags(u,r);
    
    this.setUserRoles(u);
    this.saveChanges(u);
  }

}
