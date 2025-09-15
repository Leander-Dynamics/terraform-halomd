import { Template } from '@angular/compiler/src/render3/r3_ast';
import { ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LogLevel } from '@azure/msal-browser';
import { NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { BehaviorSubject, combineLatest, Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';
import { loggerCallback } from 'src/app/app.module';
import { AppUser } from 'src/app/model/app-user';
import { CaseFileVM } from 'src/app/model/case-file';
import { DocumentTemplate } from 'src/app/model/document-template';
import { Negotiator } from 'src/app/model/negotiator';
import { NotificationType } from 'src/app/model/notification-type-enum';
import { Payor } from 'src/app/model/payor';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';
import { NegotiatorComponent } from '../negotiator/negotiator.component';
import { AngularEditorConfig } from '@kolkov/angular-editor/public-api';
import { IKeyId } from 'src/app/model/iname';
import { FileUploadEventArgs } from 'src/app/model/file-upload-event-args';
import { EntityVM } from 'src/app/model/entity-vm';
import { PayorGroup, PlanType } from 'src/app/model/payor-group';
import { PayorGroupResponse } from 'src/app/model/payor-group-response';
import { PayorAddress } from 'src/app/model/payor-address';

@Component({
  selector: 'app-manage-payors',
  templateUrl: './manage-payors.component.html',
  styleUrls: ['./manage-payors.component.css']
})
export class ManagePayorsComponent implements OnDestroy, OnInit {
  @ViewChild('addressModal', {static:false}) addressModal: Template | undefined;
  @ViewChild('caseFile', { static: false }) caseFile: ElementRef | undefined;
  @ViewChild('exclusionModal', {static:false}) exclusionModal: Template | undefined;
  @ViewChild('groupsFile', {static: false}) groupsFile: ElementRef | undefined;
  @ViewChild('loadResult') basicModal: Template | undefined;
  @ViewChild('payorGroupModal', {static:false}) payorGroupModal: Template | undefined;
  @ViewChild('payorForm', { static: false }) payorForm!: NgForm;
  @ViewChild('payorModal', {static:false}) payorModal: Template | undefined;
  @ViewChild('copyDialog', {static:false}) copyDialog: Template | undefined;

  allCaseFileVMs$ = new BehaviorSubject<CaseFileVM[]>([]);
  allEntities:EntityVM[] = [];
  allPayors:Payor[] = [];
  allPrimePayors:Payor[] = [];
  allNegotiators:Negotiator[] = [];
  allStates:{key:string,name:string}[] = [];
  allTemplateTypes = ['NSANegotiationRequest','NSANegotiationRequestAttachment'];
  NotificationType = NotificationType;
  allNotificationTypes = new Array<IKeyId>();
  canEdit = true;
  chkCopyLevel = '';
  currentPayor:Payor | null = null;
  currentPayorId = 0;
  currentTemplate:DocumentTemplate|null|undefined = null;
  currentUser:AppUser | undefined;
  destroyed$ = new Subject<void>();
  documentType = 'NSARequestAttachment';
  currentEntity:EntityVM | null = null;
  fltTemplateType:NotificationType|null = null;
  hideAddresses = true;
  hideConfiguration = true;
  hideExclusions = true;
  hideFiles = true;
  hideGroups = true;
  hideHTML = true;
  hideNegotiators = true;
  hideTemplates = true;
  isAdmin = false;
  isError = false;
  isManager = false;
  isTemplateDirty = false;
  loadTitle = '';
  loadMessage = 'Upload complete';
  modalOptions:NgbModalOptions | undefined;
  newAddress = new PayorAddress();
  newPayorGroupName = '';
  newPayorGroupNumber = '';
  newGroupPlanType:PlanType | null = null;
  newIsNSAIneligible = false;
  newIsStateIneligible = false;

  orig: Payor | null = null;
  payorName = '';
  parentId: number | null = null;
  planTypes:IKeyId[] = [];
  showHelp = false;
  showUpload = false;

  editorConfig: AngularEditorConfig = {
      editable: true,
        spellcheck: true,
        height: 'auto',
        minHeight: '600px',
        maxHeight: 'auto',
        width: 'auto',
        minWidth: '0',
        translate: 'yes',
        enableToolbar: true,
        showToolbar: true,
        placeholder: 'Create your document here...',
        defaultParagraphSeparator: '',
        defaultFontName: 'Calibri',
        defaultFontSize: '4',
        fonts: [
          {class: 'calibri', name: 'Calibri'},
          {class: 'sans-serif',name:'Sacramento'},
          {class: 'times-new-roman', name: 'Times New Roman'}
        ],
      uploadUrl: 'v1/image',
      sanitize: false,
      toolbarPosition: 'top',
      toolbarHiddenButtons: [
        [
          'strikeThrough',
          'subscript',
          'superscript',
          'justifyFull'
        ],
        [
          'customClasses',
          'insertImage',
          'insertVideo'
        ]
      ]
  };
  
  allTemplates:DocumentTemplate[] = [];

  constructor(private svcData:CaseDataService, private svcToast:ToastService, 
    private svcModal: NgbModal, private svcUtil:UtilService, 
    private svcAuth: AuthService, private router:Router,
    private route:ActivatedRoute, private svcChangeDetector:ChangeDetectorRef) { 
    this.modalOptions = {
      backdrop:'static',
      backdropClass:'customBackdrop',
      keyboard: false,
    }

    this.allNotificationTypes = Object.values(NotificationType).filter(value => typeof value === 'string' && value !== 'Search' && value !== 'Unknown').map(key => {
      const result = (key as string); //.split(/(?=[A-Z][a-z])/);
      return { id: (<any>NotificationType)[key] as number, key: result }; //.join(' ')
    });

    this.planTypes = [{id:0,key:'Fully Insured'},{id:1,key:'Self-Funded'},{id:2,key:'Self-Funded (Opt-In)'}];
  }

  ngOnInit(): void {
    this.svcUtil.showLoading = true;
    this.subscribeToData();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
    this.allCaseFileVMs$.complete();
  }
  
  addAddress() {
    if(!this.currentPayor||!this.currentPayor.id)
      return;
    this.newAddress = new PayorAddress();
    this.svcModal.open(this.addressModal, this.modalOptions).result.then(data => {
      if(!this.isAddressTypeUnique()||!this.newAddress.addressType||!this.newAddress.name||!this.newAddress.addressLine1||!this.newAddress.city||!this.newAddress.stateCode||!this.newAddress.zipCode){
        this.svcToast.show(ToastEnum.danger,'Something unexpected happened. Please try adding the address again.');
        return;
      }
      this.newAddress.stateCode = this.newAddress.stateCode.toUpperCase();
      this.currentPayor!.addresses.push(this.newAddress);
      this.payorForm.form.markAsTouched();
      this.payorForm.form.markAsDirty();
    },
    reason => console.log('Canceled Add Payor Address'));
  }

  verifyNewAddressType() {
    if(this.isAddressTypeUnique())
      return;
    this.svcToast.show(ToastEnum.danger,'Address type must be unique per Payor!');
  }

  isAddressTypeUnique() {
    if(!this.currentPayor||!this.currentPayor.addresses.length)
      return true;
    return !this.currentPayor.addresses.find(v=>v.addressType===this.newAddress.addressType);
  }

  addFile(e:any) {
    if(!(e instanceof FileUploadEventArgs))
      return;
    const args = e as FileUploadEventArgs;

    if (!args.file || !args.documentType || !this.currentPayor)
      return;
    
    this.svcUtil.showLoading = true;
    const lowDt = e.documentType.toLowerCase();

    this.svcData.uploadEntityDocument(args.file, this.currentPayor.id, e.documentType.toLowerCase(),this.currentPayor).subscribe(
      {
        next: (data: any) => {
          e.element.value = '';
          this.documentType = '';
          this.svcToast.show(ToastEnum.success, 'File uploaded successfully!');
          const m = this.allCaseFileVMs$.getValue();
          const n = new CaseFileVM({
            blobName: `${lowDt}-payor-${this.currentPayor!.id}-${e.filename.toLowerCase()}`,
            createdOn: new Date(),
            DocumentType: lowDt,
            UpdatedBy: this.svcAuth.getActiveAccount()?.name || 'system'
          });
          m.push(n);
          this.allCaseFileVMs$.next(m);
        },
        error: (err) => {
          this.svcUtil.showLoading = false;
          this.svcToast.show(ToastEnum.danger, err, 'Upload Failed');
        }, 
        complete: () => this.svcUtil.showLoading = false
      }
    );
  }

  addExclusion() {
    if(!this.allEntities.length) {
      this.svcUtil.showLoading = true;
      this.svcData.getAllEntityVMs().subscribe(data => {
        if(!data||!data.length) {
          this.svcToast.show(ToastEnum.warning,'The Entity list is empty. Add an Entity to a Customer before creating exclusions.');
          return;
        }
        this.allEntities = data;
        this.allEntities.sort(UtilService.SortByName);
        this.svcUtil.showLoading = false;
        this.showNewEntityModal();
      },
      err => {
        this.svcUtil.showLoading = false;
        loggerCallback(LogLevel.Error,'Error loading the list of Entities. Error to follow.');
        loggerCallback(LogLevel.Error,err);
        this.svcToast.show(ToastEnum.danger,'Error loading the list of Entities. Please try again.');
      });
    } else {
      this.showNewEntityModal();
    }
  }

  addGroup() {
    if(this.currentPayorId<1||!this.currentPayor)
      return;
    this.newGroupPlanType = null;
    this.newPayorGroupName = '';
    this.newPayorGroupNumber = '';
    this.newIsNSAIneligible = false;
    this.newIsStateIneligible = false;
    this.svcModal.open(this.payorGroupModal, this.modalOptions).result.then(data => {
      if(this.newGroupPlanType===null||!this.newPayorGroupName||!this.newPayorGroupNumber){
        this.svcToast.show(ToastEnum.danger,'Unexpected: One or more required values missing.');
        return;
      }
      
      const grp = new PayorGroup({groupName: this.newPayorGroupName, groupNumber: this.newPayorGroupNumber, id: 0, payorId: this.currentPayorId, planType:this.newGroupPlanType, isNSAIneligible: this.newIsNSAIneligible, isStateIneligible:this.newIsStateIneligible});
      this.svcUtil.showLoading = true;
      this.svcData.createPayorGroup(this.currentPayor!.id, grp).subscribe(data => {
        this.currentPayor!.payorGroups.push(data);
        this.svcToast.show(ToastEnum.success,'Payor Group added successfully!');
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger, 'Error creating a new PayorGroup');
      },
      () => this.svcUtil.showLoading = false
      );
    },
    err => console.warn('Add PayorGroup canceled'));
  }

  addPayor() {
    this.payorName = '';
    this.parentId = null;
    this.svcModal.open(this.payorModal, this.modalOptions).result.then(data => {
      if(this.payorName.length < 3 || this.parentId===null)
        return;
      this.svcUtil.showLoading = true;
      this.svcData.createPayor(new Payor({name: this.payorName, NSARequestEmail:'noreply@mpowerhealth.com', parentId:this.parentId, sendNSARequests:false})).subscribe(rec => {
        this.allPayors.push(rec);
        this.allPayors.sort(UtilService.SortByName);
        this.allPrimePayors = this.allPayors.filter(d=>d.parentId===d.id);
        this.currentPayor = this.allPayors.find(v => v.id === rec.id) ?? null;
        this.currentPayorId = this.currentPayor!.id;
        this.payorChange();
        this.svcToast.show(ToastEnum.success,'New Payor created successfully!');
      },
      err => {
        this.svcUtil.showLoading = false;
        if(!err)
          return;
        this.svcToast.show(ToastEnum.danger,'Error creating Payor! Please try again.');
      },
      () => this.svcUtil.showLoading = false
      );
    },
    err => console.warn('Add Payor canceled'));
  }

  addNegotiator() {
    if(!this.currentPayor)
      return;
    let contact = new Negotiator();
    contact.id = 0;
    contact.organization = this.currentPayor.name;
    contact.payorId = this.currentPayor.id;
    const modalRef = this.svcModal.open(NegotiatorComponent);
    modalRef.componentInstance.name = 'addNegotiator';
    modalRef.componentInstance.payorName = this.currentPayor.name;
    modalRef.componentInstance.contact = contact;
    modalRef.closed.subscribe(data => {
      this.svcUtil.showLoading = true;
      this.svcData.createNegotiator(contact).subscribe(rec => {
        this.allNegotiators.push(rec);
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.success,'New Negotiator added to ' + this.currentPayor?.name);
      },
      err => {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger,'Error creating Negotiator" ' + err.message ?? err);
      });
    });
  }

  addTemplate() {
    const name=prompt('New Template Name:');
    if(!name)
      return;
    const tmp = this.currentPayor?.templates.find(d=>d.name.toLowerCase()===name.toLowerCase()&&d.notificationType===this.fltTemplateType);
    if(!!tmp) {
      this.svcToast.show(ToastEnum.danger,'The current Payor already has a Template with that name and type. Please enter a different name.');
      return;
    }
    this.currentTemplate = new DocumentTemplate({name:name, notificationType:this.fltTemplateType});
    this.allTemplates.push(this.currentTemplate);

  }

  cancelChanges() {
    if(!this.currentPayor)
      return;
    if(!confirm('Are you sure you want to cancel?'))
      return;
    if(this.currentPayor.id===0) {
      const i = this.allPayors.findIndex(d => d.id === 0);
      this.allPayors.splice(i,1);
      this.currentPayor = null;
    } else if(!!this.orig) {
      /*
      this.currentPayor.isActive = this.orig.isActive;
      this.currentPayor.parentId = this.orig.parentId;
      this.currentPayor.name = this.orig.name;
      this.currentPayor.NSARequestEmail = this.orig.NSARequestEmail;
      this.currentPayor.negotiators = this.orig.negotiators;
      this.currentPayor.sendNSARequests = this.orig.sendNSARequests;
      this.currentPayor.JSON = this.orig.JSON;
      */
     this.currentPayor = new Payor(this.orig);
      this.allTemplates = this.fltTemplateType===null ? [] : this.currentPayor.templates.filter(d=>d.notificationType===this.fltTemplateType);
      if(this.currentTemplate) {
        this.currentTemplate = this.allTemplates.find(d=>d.notificationType===this.currentTemplate?.notificationType&&d.name.toLowerCase()===this.currentTemplate?.name.toLowerCase()) ?? null;
      }
    }
    this.resetFormStatus();
  }

  caseFileChanged(e: any) {
    e.target.blur();
  }

  copyTemplate() {
    this.chkCopyLevel = '';
    const ct = this.currentTemplate;
    if(!ct)
      return;

    const ctName = ct.name.toLowerCase();
    let log = '';

    this.svcModal.open(this.copyDialog, this.modalOptions).result.then(data => {
      const active = (this.chkCopyLevel.toLowerCase()==='active');
      const prime = (this.chkCopyLevel.toLowerCase()==='prime');

      if(!confirm(`Are you sure you want to copy the currently-selected template to ${this.chkCopyLevel} Payors? This will take awhile and CANNOT be canceled or undone!`))
        return;

      this.svcUtil.showLoading = true;
      this.svcData.loadPayors(active,true,true).subscribe(data => {
        if(!data||!data.length){
          alert('No Payors returned from endpoint!');
          return;
        }

        let count = 0;
        for(const payor of data){
          if(prime && payor.id!==payor.parentId)
            continue; // skip
          if(active && !payor.isActive)
            continue; // skip
          if(payor.id===this.currentPayor?.id)
            continue; // skip the source payor of course

          //const temp = payor.templates.find(v=>v.notificationType===ct.notificationType && v.name.toLowerCase()===ctName);
          //if(!!temp) 
          payor.updateTemplate(ct);
          //else
          //  payor.templates.push(Object.assign({}, ct));

          count++;
          this.svcData.updatePayor(payor).subscribe( 
            rec => {
              log+=`* Copied template to ${payor.name}\n<br />`;
              const i = this.allPayors.findIndex(v=>v.id===payor.id);
              if(i>=0)
                this.allPayors[i] = rec;
              count--;
              if(count==0) {
                this.svcUtil.showLoading = false;
                this.loadTitle = 'Copy Template Operation';
                this.loadMessage = log;
                this.showResults();
              }  
            }, 
            err => {
              count--;
              log+=`ERROR copying template to ${payor.name}: ` + (err.error?.title ?? err.message) + '\n<br />';
              if(count==0) {
                this.svcUtil.showLoading = false;
                this.loadTitle = 'Copy Template Operation';
                this.loadMessage = log;
                this.showResults();
              }
            }
          ); 
        }
      },
      err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast)
      );
    },
    err => console.warn('Copy template canceled')
    );
  }

  deleteAddress(g:PayorAddress){
    if(!this.currentPayor)
      return;
    const a = this.currentPayor.addresses.indexOf(g);
    const b = g.id;
    if(a==-1)
      return;
    if(!confirm('ARE YOU SURE you want to permantently delete this address?\n\nThis will happen immediately and you cannot recover the address!'))
      return;

    this.svcUtil.showLoading = true;
    this.svcData.deletePayorAddress(g).subscribe(dtat => {
      this.svcToast.show(ToastEnum.success,'Address removed successfully');
      this.currentPayor!.addresses.splice(a,1);
      const oid = this.orig?.addresses.findIndex(v=>v.id===b) ?? -1;
      if(oid>-1)
        this.orig?.addresses.splice(oid,1);
    },
    err => UtilService.HandleServiceErr(err, this.svcUtil,this.svcToast),
    () => this.svcUtil.showLoading = false
    );
    
  }

  deleteExclusion(g:EntityVM) {
    if(this.currentPayor?.removeExclusion(g)){
      this.payorForm.form.markAsDirty();
      this.payorForm.form.markAsTouched();
      this.svcToast.showAlert(ToastEnum.warning,'Save changes to thie Payor to remove the Entity Exclusion');
    }
  }

  deleteFile(e:any) {
    if(!this.currentPayor?.id || !e || !(e instanceof CaseFileVM))
      return;

    const f = e as CaseFileVM;
    if (!confirm('Are you sure you want to permanently delete this resource file from the Payor?'))
      return;
    
    this.svcData.deleteEntityFile(this.currentPayor.id, f.DocumentType, f.blobName, this.currentPayor).subscribe(data => {
      this.svcToast.show(ToastEnum.success, 'File deleted');
      const a = this.allCaseFileVMs$.getValue();
      const ndx = a.findIndex(d => d.blobName === f.blobName);
      if (ndx > -1) {
        a.splice(ndx, 1);
        this.allCaseFileVMs$.next(a);
      }
    },
      err => this.svcToast.showAlert(ToastEnum.danger, err.message ?? err.toString())
    );
  }

  deleteGroup(g:PayorGroup) {

  }

  editorKeyUp(e:any) {
    if(this.payorForm.dirty)
      return;
    this.payorForm.form.markAsDirty();
    this.payorForm.form.markAsTouched();
    this.isTemplateDirty = true;
    this.svcToast.show(ToastEnum.warning,'Click Save Changes to save or Cancel to undo ALL changes to the template.');
  }

  fileSelected():boolean {
    return this.groupsFile?.nativeElement.files.length ? true : false;
  }

  fileSelectionChanged() {
    loggerCallback(LogLevel.Verbose,'Upload file selection changed'); // this triggers a blur/change detection or else the button won't light up
  }
  /*
  fltPayorsWithoutSelf() {
    if(!this.currentPayor)
      return [];
    return this.allPayors.filter(v => v.id !== this.currentPayor!.id);
  }
  */
  fltTemplateTypeChange(){
    if(this.currentPayor&&this.currentTemplate?.html) {
      this.currentPayor.updateTemplate(this.currentTemplate);
      this.currentTemplate = null;
    }
    this.allTemplates = (!this.currentPayor||this.fltTemplateType===null) ? [] : this.currentPayor.templates.filter(d=>d.notificationType===this.fltTemplateType);
  }

  handleServiceErr(err:any) {
    let msg = '';
    msg = err?.error?.title ?? err?.error ?? err?.message ?? err?.statusText ?? err.toString();
    this.svcToast.showAlert(ToastEnum.danger, msg);
    this.svcUtil.showLoading = false;
    window.scrollTo({top:0,behavior:'smooth'});
  }

  loadPrerequisites() {
    this.svcUtil.showLoading = true;
    const payors$ = this.svcData.loadPayors(false,false,false);
    const authorities$ = this.svcData.loadAuthorities();
    combineLatest([payors$,authorities$]).subscribe(([payors,authorities]) => {
      this.allPayors = payors;
      this.allPayors.sort(UtilService.SortByName);
      this.allPrimePayors = payors.filter(d=>d.parentId===d.id);
      this.allStates = authorities.filter(v=>v.key.toLowerCase()!=='nsa').map(d=>{
        return {key:d.key, name:d.name};
      });
      this.allStates.sort(UtilService.SortByName);
    },
    err => {
      this.svcUtil.showLoading = false;
      this.svcToast.showAlert(ToastEnum.danger, 'Unable to load the list of Payors');
    },
    () => this.svcUtil.showLoading = false
    );
  }

  negotiatorChanged(n:Negotiator) {
    if(n.id < 1)
      return;
    this.svcUtil.showLoading = true;
    this.svcData.updateNegotiator(n).subscribe(
      data => {
        const g = this.allNegotiators.findIndex(d => d.id === data.id);
        if(g > -1) {
          this.allNegotiators[g] = data;
        }
        this.svcToast.show(ToastEnum.success,'Negotiator updated successfully');
      },
      err =>  {
        this.svcUtil.showLoading = false;
        this.svcToast.show(ToastEnum.danger, err.message ?? err.toString());
      },
      () => this.svcUtil.showLoading = false
    );
  }

  onSubmit() {
    if (!this.payorForm?.valid) 
      return false;
    if(!!this.currentPayor?.NSARequestEmail) {
      for(const email of this.currentPayor?.NSARequestEmail.split(';')){
        if(!!email && !UtilService.IsEmailValid(email)){
          this.svcToast.show(ToastEnum.danger,email+' is not a valid email address');
          return false;
        }
      }
    }
    this.saveChanges();
    return true;
  }

  parentPayorChange() {
    if (!this.currentPayorId || !this.currentPayor)
      return;
    var parent = this.allPayors?.find(p => p.id == this.currentPayor?.parentId);
    this.currentPayor.NSARequestEmail = parent?.NSARequestEmail ?? '';
  }

  payorChange() {
    this.allNegotiators.length = 0;
    this.allTemplates.length = 0;
    this.currentTemplate = null;
    this.fltTemplateType = null;
    this.orig = null;
    const vms = new Array<CaseFileVM>();
    this.allCaseFileVMs$.next(vms);
    this.allTemplates.length = 0;

    if(!this.currentPayorId)
      return;
    const p$ = this.svcData.loadPayorById(this.currentPayorId);
    const f$ = this.svcData.loadPayorFiles(this.currentPayorId);

    this.svcUtil.showLoading = true;
    combineLatest([p$,f$]).subscribe(([p,f]) => {
      this.allNegotiators = !!p ? p.negotiators : [];
      
      this.orig = !!p ? new Payor(p) : null;
      this.currentPayor = p;
      this.hideConfiguration = false;
      console.log(this.currentPayor.payorGroups);

      f.forEach(cf => {
        const vm = new CaseFileVM(cf.tags);
        vm.blobName = cf.blobName;
        vm.createdOn = cf.createdOn;
        vms.push(vm);
      });
      this.allCaseFileVMs$.next(vms);
      this.resetFormStatus();
      this.svcChangeDetector.detectChanges();
    },
    err => this.handleServiceErr(err),
    () => this.svcUtil.showLoading = false);
  }

  resetFormStatus() {
    Object.keys(this.payorForm.controls).forEach((key) => {
      const control = this.payorForm.controls[key];
      control.markAsPristine();
      control.markAsUntouched();
    });
    this.isTemplateDirty = false;
  }
  
  saveChanges() {
    if(!this.currentPayor)
      return;

    if(this.currentPayor.id === 0) {
      this.svcToast.showAlert(ToastEnum.warning,'Payor creation not yet available but is coming soon!');
      /* create
      this.svcData.createAuthority(this.currentAuthority).subscribe(rec => {
        this.updateCollection(0, rec);
        this.svcToast.show(ToastEnum.success,'New Customer created successfully!');
        this.resetFormStatus();
      },
      err => {
        this.svcUtil.showLoading = false;
        loggerCallback(LogLevel.Error, err);
        this.svcToast.show(ToastEnum.danger,'Error creating Customer! Please try again.');
      },
      () => this.svcUtil.showLoading = false
      );
      */
    } else if(this.currentPayor.id > 0) {
      // update
      
      if(!!this.currentTemplate)
        this.currentPayor.updateTemplate(this.currentTemplate);
      this.svcUtil.showLoading = true;
      this.svcData.updatePayor(this.currentPayor).subscribe(rec => {
        this.updateCollection(rec.id, rec);
        this.svcToast.show(ToastEnum.success,'Payor updated successfully!');
        this.resetFormStatus();
      },
      err => {
        this.handleServiceErr(err);
      },
      () => this.svcUtil.showLoading = false
      );
    }
  }

  selectAll(e:any) {
    e?.target?.select();
  }

  showNewEntityModal() {
    this.currentEntity = null;
    this.svcModal.open(this.exclusionModal, this.modalOptions).result.then(data => {
      if(this.currentEntity===null || this.currentEntity.name.length < 3 || this.currentEntity.NPINumber.length<10){
        this.svcToast.show(ToastEnum.danger,'Entity name or NPI number were too short!');
        return;
      }
      this.currentPayor!.addExclusion(this.currentEntity);  
      this.payorForm.form.markAsDirty();
      this.payorForm.form.markAsTouched();
      this.svcToast.showAlert(ToastEnum.warning,'Save changes to thie Payor to keep the new Exclusion(s)');
    },
    err => console.warn('Add Exclusion canceled'));
  }

  showResults() {
    const opts = Object.assign({}, this.modalOptions);
    opts.size= 'lg';
    this.svcModal.open(this.basicModal, opts);
  }

  subscribeToData() {
    // listen for loading of user info
    this.svcAuth.currentUser$.pipe(takeUntil(this.destroyed$)).subscribe(data => {
      this.isManager = !!data.isManager;
      this.isAdmin = !!data.isAdmin
      this.canEdit = this.isAdmin || this.isManager;
      if(!this.canEdit) {
        UtilService.PendingAlerts.push({level:ToastEnum.danger, message:'Insufficient privileges for managing the Payors list.'});
        this.router.navigate(['']);
        return;
      }
      this.currentUser = data;
      this.route.params.pipe(takeUntil(this.destroyed$)).subscribe(data => {
        this.loadPrerequisites();
      });
    });
  }

  updateCollection(id:number,rec:Payor) {
    let ndx = -1;
    const oldId = this.currentPayor?.id ?? -1;
    
    this.allPayors.forEach((item, index) => {
      if (item.id === id) {
        this.allPayors[index] = rec;
        ndx = index;
      }
    });

    this.currentPayor = ndx > -1 ? this.allPayors[ndx] : null;
    
    if(this.currentPayor === null || id !== oldId) {
      this.payorChange();
    } else {
      // persist UI state
      this.orig = new Payor(this.currentPayor);
      this.allTemplates = this.fltTemplateType===null ? [] : this.currentPayor.templates.filter(d=>d.notificationType===this.fltTemplateType);
      if(this.currentTemplate) {
        this.currentTemplate = this.allTemplates.find(d=>d.notificationType===this.currentTemplate?.notificationType&&d.name.toLowerCase()===this.currentTemplate?.name.toLowerCase()) ?? null;
      }
    }

    this.allPayors.sort(UtilService.SortByName);
    this.allPrimePayors = this.allPayors.filter(d=>d.parentId===d.id);
  }
  
  uploadGroups() {
    this.isError = false;
    const files = this.groupsFile?.nativeElement.files;
    const f:File = files && files.length ? files[0] : undefined;
    if(!files||!f) {
      this.svcToast.show(ToastEnum.danger,'No file selected');
      return;
    }

    if(!f.name.toLowerCase().endsWith('.csv')) {
      this.isError = true;
      this.loadTitle = "Document Type Error";
      this.loadMessage = "Invalid document type. Only CSV is supported at this time.";
      this.svcModal.open(this.basicModal, this.modalOptions);
      return;
    }
      
    this.svcUtil.showLoading = true;
    this.svcData.uploadPayorGroups(f).subscribe(
      { 
        next: (data: PayorGroupResponse) => {
          this.loadMessage = data.message;
          this.loadTitle = "Upload Complete";
          console.log(data);
          this.showResults();
          if(this.groupsFile)
            this.groupsFile.nativeElement.value = '';

          // update current payor object w/new groups
          if(!!this.currentPayor){
            const ndx = this.allPayors.findIndex(d=>d.id===this.currentPayor!.id);
            if(ndx>-1) {
              this.svcData.loadPayorById(this.currentPayor.id).subscribe(rec => {
                this.allPayors.splice(ndx,1,rec);
                this.currentPayor = rec;
              },
              err => UtilService.HandleServiceErr(err, this.svcUtil, this.svcToast),
              () => this.svcUtil.showLoading = false
              );
            } else {
              this.svcUtil.showLoading = false;
            }
          }
          this.showUpload = false;
        },
        error: (err) => {
            this.svcUtil.showLoading = false;
            this.isError = true;
            if(typeof err.error == 'object')
              this.loadMessage = err.error.title;
            else
              this.loadMessage = err.error ?? err.message ?? err.statusText ?? err.toString();
            this.loadTitle = "Upload Failed";
            this.showResults();
            if(this.groupsFile)
              this.groupsFile.nativeElement.value = '';
            this.showUpload = false;
        },
        complete: () => this.svcUtil.showLoading = false
      }
    );
  }

  updateGroup(g:PayorGroup) {
    if(!this.currentPayor?.id)
      return;
    this.svcUtil.showLoading = true;
    this.svcData.updatePayorGroup(this.currentPayor?.id,g).subscribe(data => {
      const ndx = this.currentPayor?.payorGroups.indexOf(g);
      if(!!ndx){
        this.currentPayor?.payorGroups.splice(ndx,1,data);
        this.svcToast.show(ToastEnum.success,'Payor Group updated successfully!');
      }
      },
      err => this.handleServiceErr(err),
      () => this.svcUtil.showLoading = false
    );
  }

  viewFile(e:any) {
    if(!this.currentPayor||!e||!(e instanceof CaseFileVM))
      return;
    const f = e as CaseFileVM;
    this.svcData.downloadEntityFile(this.currentPayor.id, f.blobName, this.currentPayor).pipe(take(1)).subscribe(res => {
      const fileURL = URL.createObjectURL(res);
      window.open(fileURL, '_blank');
    });
  }
  
}
