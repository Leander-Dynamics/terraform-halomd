import { Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import { ArbitrationCase } from 'src/app/model/arbitration-case';
import { ToastEnum } from 'src/app/model/toast-enum';
import { CaseDataService } from 'src/app/services/case-data.service';
import { ToastService } from 'src/app/services/toast.service';
import { UtilService } from 'src/app/services/util.service';

@Component({
  selector: 'app-summary-dialog',
  templateUrl: './summary-dialog.component.html',
  styleUrls: ['./summary-dialog.component.css']
})
export class SummaryDialogComponent implements OnInit {
  @Input() claim = new ArbitrationCase();
  @ViewChild('cardTop', { static: false }) cardTop: ElementRef | undefined;

  CPTs = '';
  geoZip = '';
  providerOffer = 0;
  payorOffer = 0;
  specialty = '';

  constructor(private svcToast: ToastService, private svcData: CaseDataService, private svcUtil:UtilService) { }

  ngOnInit(): void {
    this.svcUtil.showLoading = true;
    this.svcData.loadCaseById(this.claim.id).subscribe(data => { // fetch the CPT codes
      this.claim = data;
      // make local vars to bind to ui
      const a = this.claim.cptCodes.filter(v=>v.isIncluded).map(v=>v.cptCode);
      this.CPTs = a.join(';');

      let prov = this.claim.offerHistory.filter(v=>v.offerType.toLowerCase()==='provider');
      prov.sort(UtilService.SortByUpdatedOn);
      let payor = this.claim.offerHistory.filter(v=>v.offerType.toLowerCase()==='payor');
      payor.sort(UtilService.SortByUpdatedOn);
      this.providerOffer = prov[0]?.offerAmount ?? 0;
      this.payorOffer = payor[0]?.offerAmount ?? 0;

      this.geoZip = !!data.benchmarkGeoZip ? data.benchmarkGeoZip : data.locationGeoZip;

      if(data.service==='IOM Pro')
        this.specialty = 'Dedicated Interpreting Physician for intraoperative neuromonitoring (NIOM)';
      else if(data.service==='IOM Tech')
        this.specialty = 'Neurology';
      else if(data.service==='PA')
        this.specialty = 'Surgical Physician Assistant specializing in orthopedics / spine / brain procedures';
      else if(data.serviceLine ==='ER')
        this.specialty = 'Emergency department physician';
    },
    err => {
      console.error(err);
      this.svcUtil.showLoading = false;
    },
    () => this.svcUtil.showLoading = false
    );
  }

  copyToClipboard() {
    if(!this.cardTop?.nativeElement)
      return;
    let range = document.createRange();
    range.selectNode(this.cardTop!.nativeElement);
    window.getSelection()?.removeAllRanges();
    window.getSelection()?.addRange(range);
    document.execCommand("copy");
    window.getSelection()?.removeAllRanges();
    this.svcToast.show(ToastEnum.success,'Summary copied to clipboard. Use CTRL-v to paste');
  }
}
