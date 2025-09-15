import { Component, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { CaseDocumentType } from 'src/app/model/case-document-type-enum';
import { CaseFileVM } from 'src/app/model/case-file';
import { FileUploadEventArgs } from 'src/app/model/file-upload-event-args';
import { IKeyId } from 'src/app/model/iname';
import { ToastEnum } from 'src/app/model/toast-enum';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';

@Component({
  selector: 'app-file-upload',
  templateUrl: './file-upload.component.html',
  styleUrls: ['./file-upload.component.css']
})
export class FileUploadComponent implements OnDestroy, OnInit {
  @Input()
  allCaseFileVMs$ = new BehaviorSubject<CaseFileVM[]>([]);
  @Input()
  canEdit = false;
  @Input()
  disableUpload = true;
  @Input()
  allDocTypes = new Array<IKeyId>();
  @Input()
  isAdmin = false;
  @Input()
  title = 'Files and Documents';
  @Output()
  onFileAdded = new EventEmitter<FileUploadEventArgs>();
  @Output()
  onFileDelete = new EventEmitter<CaseFileVM>();
  @Output()
  onViewFile = new EventEmitter<CaseFileVM>();
  @ViewChild('caseFile', { static: false })
  caseFile: ElementRef | undefined;
  @Input()
  hideFiles = true;

  documentType:CaseDocumentType|null = null;

  constructor(private svcData:CaseDataService, private svcToast:ToastService) {
    this.allDocTypes = Object.values(CaseDocumentType).filter(value => typeof value === 'string').map(key => { 
      const result = (key as string).split(/(?=[A-Z]+[a-z])/);
      return { id: (<any>CaseDocumentType)[key] as number, key: result.join(' ').replace('I D R','IDR').replace('N S A','NSA').replace('O P ','OP ').replace('eEOB','e EOB') };
    });
   }

  ngOnDestroy(): void {
    this.allCaseFileVMs$.complete();
  }

  ngOnInit(): void {}

  addFile() {
    if (!this.caseFile || !this.caseFile.nativeElement || this.documentType===null)
      return;
    
    const ne = this.caseFile.nativeElement as HTMLInputElement
    const files = ne.files; // e.target?.files;
    const f: File | undefined = files && files.length ? files[0] : undefined;
    if (!f)
      return;
    const low = f.name.toLowerCase();
    if (!low.endsWith('.pdf') && !low.endsWith('.tif') && !low.endsWith('.tiff')) {
      this.svcToast.show(ToastEnum.danger, 'Only PDF, TIF and TIFF files are allowed', 'Unsupported File Type', 4000);
      return;
    }


    const args = new FileUploadEventArgs();
    args.documentType = CaseDocumentType[this.documentType];
    args.element = this.caseFile.nativeElement;
    args.file = f;
    args.filename = f.name;

    this.onFileAdded.emit(args);
  }

  caseFileChanged(e: any) {
    e.target.blur();
  }

  deleteFile(f: CaseFileVM) {
    this.onFileDelete.emit(f);
  }

  viewFile(f: CaseFileVM) {
    if(!f||!f.blobName)
      return;
    this.onViewFile.emit(f);
  }

}
