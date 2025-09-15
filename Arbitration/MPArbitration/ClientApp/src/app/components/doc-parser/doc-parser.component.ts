import { Template } from '@angular/compiler/src/render3/r3_ast';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { LogLevel } from '@azure/msal-browser';
import { NgbModal, NgbModalOptions } from '@ng-bootstrap/ng-bootstrap';
import { loggerCallback } from 'src/app/app.module';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'app-doc-parser',
  templateUrl: './doc-parser.component.html',
  styleUrls: ['./doc-parser.component.css']
})
export class DocParserComponent implements OnInit {
  @ViewChild('loadResult') basicModal: Template | undefined;
  @ViewChild('summaryFile', { static: false }) summaryFile: ElementRef | undefined;
  ehrDocType = 'BCBSTXEOB';
  isError = false;
  loadTitle = '';
  loadMessage = 'Successfully updated records';
  modalOptions: NgbModalOptions | undefined;
  outputJSON = '{}';

  constructor(private svcData: CaseDataService, private svcModal: NgbModal,
              private svcUtil: UtilService, private router: Router, 
              private svcToast: ToastService) {
    this.modalOptions = {
      backdrop: 'static',
      backdropClass: 'customBackdrop',
      keyboard: false,
    };
  }

  ngOnInit(): void {
    this.svcUtil.showLoading=false;
  }


  docTypeChange() {
    this.outputJSON = '{}';
    if (this.summaryFile)
      this.summaryFile.nativeElement.value = '';

    if (!this.ehrDocType)
      return;
  }

  fileSelected(): boolean {
    return this.summaryFile?.nativeElement.files.length ? true : false;
  }

  fileSelectionChanged() {
    this.summaryFile?.nativeElement.blur();
    this.outputJSON='';
    loggerCallback(LogLevel.Verbose, 'Upload file selection changed'); // this triggers a blur/change detection or else the button won't light up
  }
  async parse() {
    const files = this.summaryFile?.nativeElement.files;
    const f: File = files && files.length ? files[0] : undefined;

    if (!f) {
      this.isError = true;
      this.loadMessage = 'Unable to read file. Be sure you are selecting a true TXT file that contains only ascii characters.';
      this.loadTitle = "Error";
      this.svcModal.open(this.basicModal, this.modalOptions);
      return;
    }

    if (!f.name.toLowerCase().endsWith('.txt')) {
      this.isError = true;
      this.loadTitle = "Document Type Error";
      this.loadMessage = "Invalid document type. Only TXT is supported at this time.";
      this.svcModal.open(this.basicModal, this.modalOptions);
      return;
    }

    let work = await f.text();
    const obj = {
      claims: new Array()
    };

    const headerExp = /PATIENT:[\W\s]*((?:[\w\'\-]*[ \W]){1,3})[\r\s\w\W]*?IDENTIFICATION NO:\s*([\w-]*)[\r\s\w\W]*?CLAIM NO:\s*(\w*)/gm; // (Patient Name) (Patient Id) (Payor Claim Number)
    const outerTableExp = /CLAIM NO:\s*(\w)*[\r\s\w\W]*?AMOUNT PAID TO PROVIDER/gm;
    const tableRowsExp = /(([01]\d\/[0-2]\d-[01]\d\/[0-2]\d\/2\d)[\s\w]*?(NOP)\s*(\S*)\s*(\S*)\s*(\S*)\s*([\d,.]*) C \d\)\s*([.\d]*)[ \W\w\d]{0,6}\s*([.\d]*)\r*)+/gm;

    for(const m of work.matchAll(headerExp)){
      if(m.length>3) {
        const claim = {
          patientName: m[1].trim(),
          ehrNumber: m[2].trim(),
          payor: 'BCBSTX',
          payorClaimNumber: m[3].trim(),
          procedureCodes: new Array()
        };
        obj.claims.push(claim);
      }
    }

    // process CPTs
    for(const h of obj.claims) {
      for(const t of work.matchAll(outerTableExp)) {
        for(const row of t[0].matchAll(tableRowsExp)) {
          const cpt = {fromTo: row[2], psPay: row[3], procedureCode: row[4], amountBilled: row[5], allowable: row[6], notCovered: row[7], ineligible: row[8], amountPaid: row[9]};
          h.procedureCodes.push(cpt);
        }
      }
    }

    if(!obj.claims.length)
      return;

    this.outputJSON = JSON.stringify(obj,null,3).replaceAll('\n','<br/>');
}
}
