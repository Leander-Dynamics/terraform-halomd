import { Component, OnInit, TemplateRef, ViewEncapsulation } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AngularEditorConfig } from '@kolkov/angular-editor';
import { NgbOffcanvas } from '@ng-bootstrap/ng-bootstrap';
import { combineLatest, Subject } from 'rxjs';
import { Authority } from 'src/app/model/authority';
import { Customer } from 'src/app/model/customer';
import { MultiSelectVM } from 'src/app/model/multiselect-vm';
import { Payor } from 'src/app/model/payor';
import { Template } from 'src/app/model/template';
import { ToastEnum } from 'src/app/model/toast-enum';
import { AuthService } from 'src/app/services/auth.service';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'app-template-builder',
  templateUrl: './template-builder.component.html',
  styleUrls: ['./template-builder.component.css'],
	encapsulation: ViewEncapsulation.None,
})
export class TemplateBuilderComponent implements OnInit {
  destroyed$ = new Subject<void>();

  allAuthorities:MultiSelectVM[] = [];
  allCustomers:MultiSelectVM[] = [];
  allEntities:MultiSelectVM[] = [];
  allPayors:MultiSelectVM[] = [];
  
  canEdit = true;

  currentTemplate: Template|undefined;

  // criteria vars - will move to TemplateComponentVM later
  authority = '*';
  description = '';
  isTemplateDirty = false;
  name = '{new}';
  componentType = '';
  html = '';
  procedureCodes = '';

  editorConfig: AngularEditorConfig = {
    editable: true,
      spellcheck: true,
      height: 'auto',
      minHeight: '600px',
      maxHeight: '100%',
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
  constructor(private svcData: CaseDataService, private offcanvasService: NgbOffcanvas,
    private svcToast: ToastService, private activatedRoute:ActivatedRoute,
    private svcAuth:AuthService, private svcUtil: UtilService) { }

  ngOnInit(): void {
    this.loadPrerequisites();
  }

  ngOnDestroy(): void {
    this.destroyed$.next();  
    this.destroyed$.complete();
  }

  editorKeyUp(e:any) {
    if(!this.isTemplateDirty)
      this.svcToast.show(ToastEnum.warning,'Click Save Changes to save or Cancel to undo ALL changes to the template.');
    this.isTemplateDirty = true;
  }

  enforceMultiRules(m:MultiSelectVM,coll:MultiSelectVM[]) {
     if(m.key==='*') {
      //unselect the rest
      if(m.isSelected)
        coll.forEach(d=>d.isSelected=(d.key==='*'));
      else 
        m.isSelected = (coll.length===1); // cannot unselect All when there are no other options
     } else {
      if(m.isSelected){
        const all = coll.find(d=>d.key==='*');
        if(!!all) { all.isSelected=false }
      } else if(!coll.find(d=>d.isSelected)){
        const all = coll.find(d=>d.key==='*');
        if(!!all) { all.isSelected=true }
      }
     }
  }

  loadPrerequisites() {
    const authorities$ = this.svcData.loadAuthorities();
    const payors$ = this.svcData.loadPayors();
    const customers$ = this.svcData.loadCustomers();

    combineLatest([authorities$,payors$,customers$]).subscribe(
      ([authorities,payors,customers]) => {
      
      this.allAuthorities = authorities.map(d=> new MultiSelectVM(d.name,d.id,d.key,false,d));
      this.allAuthorities.sort(UtilService.SortByName);
      this.allAuthorities.unshift(new MultiSelectVM('All',0,'*',true,undefined));
      this.allPayors = payors.map(d=>new MultiSelectVM(d.name,d.id,d.id.toString(),false,d));
      this.allPayors.sort(UtilService.SortByName);
      this.allPayors.unshift(new MultiSelectVM('All',0,'*',true,undefined));
      this.allCustomers = customers.map(d=>new MultiSelectVM(d.name,d.id,d.id.toString(),false,d));
      this.allCustomers.sort(UtilService.SortByName);
      this.allCustomers.unshift(new MultiSelectVM('All',0,'*',true,undefined));
      
      for(const c of customers){
        for(const n of c.entities) {
          this.allEntities.push(new MultiSelectVM(n.name,n.id,n.NPINumber,false,n));
        }
      }
      this.allEntities.sort(UtilService.SortByName);
      this.allEntities.unshift(new MultiSelectVM('All',0,'*',true,undefined));
      
    },
    err => {
      this.svcToast.show(ToastEnum.danger,'Failed to load all criteria choies.');
      this.svcUtil.showLoading = false;
    },
    () => this.svcUtil.showLoading = false
    );

  }

  
	openCriteria(content: TemplateRef<any>) {
		this.offcanvasService.open(content, { position: 'end' });
	}

  submitCriteria(f:NgForm) {
    this.offcanvasService.dismiss();
  }
}
