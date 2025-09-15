import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { ICaseImportFormat } from 'src/app/model/case-import-format';
import { ArbitrationCase } from 'src/app/model/arbitration-case';
import { VMArbitrationCPT } from 'src/app/model/vm-arbitration-cpt';
import { CaseDataService } from 'src/app/services/case-data.service';

@Component({
  selector: 'app-pre-arbitration',
  templateUrl: './pre-arbitration.component.html',
  styleUrls: ['./pre-arbitration.component.css']
})
export class PreArbitrationComponent implements OnInit {
  analyze = false; // flag for continuing to next step after save
  canCalculate = false;
  canPaste = true;
  canReset = false;
  caseText = '';
  ehr = '';
  $destroyed = new Subject<void>();
  mpCase = new ArbitrationCase();

  pasteFormat: ICaseImportFormat = {id: 1, ehr: '', columns: 5, value: '[Service] [CPT] [Units] [GeoZip] [Payor Offer]'};
  isLoading = false;
  queueCount = 0;
  pastedText = '';
  summary:{arbFee:number,chargesCap:number,fhExtended:number,fh80th:number,hardCap:number,offerSpread:number,payorOffer:number,units:number} | undefined;
  tableData = new BehaviorSubject<VMArbitrationCPT[]>([]);
  //VARS = variables;

  @ViewChild("a") autofocus: ElementRef | undefined;

  constructor(private svcData:CaseDataService, private router:Router) { }

  ngAfterViewInit() {
    this.autofocus?.nativeElement.focus();
  }

  ngOnDestroy(): void {
    this.$destroyed.next();  
    this.$destroyed.complete();
    this.tableData.complete();
  }

  ngOnInit(): void {
    this.subscribeToData();
  }

  subscribeToData() {
    /*
    this.svcData.cases.pipe(takeUntil(this.$destroyed)).subscribe(data => {
      // transform the new cases into table data
      data.forEach(d => {
        console.log(d);
        if(!d.id) {
          console.error('Unable to creae new tracking case.');
          return;
        }

        this.resetClick();

        if(this.analyze) {
          alert('Click OK to open the new case in edit mode.');
          //this.router.navigate([],{state: this.mpCase});
        }
          
        
      });
    });

    this.svcData.benchmarks.subscribe({
      next: data => {
        console.log('DATA received');
      // add benchmark data to viewmodels
      if(this.queueCount > 0) {
        this.queueCount -= 1;
      }
      const td = this.tableData.value;

      if(data && data.length && td.length) {
          // find any case with matching code and update
          td.filter(n => !n.fhAllowedAmount).forEach(k => {
            const value = data.find(m => m.procedureCode === k.cptCode 
                                          && m.geozip === k.locationGeoZip.substring(0,3)
                                          && (m.modifier ?? '') === k.modifiers 
                                          && m.state === 'TX'); // todo: obviously we may need to add support for additional states
            if(value) { 
              k.fhAllowedAmount = value.allowed;
              k.fhExtendedAmount = k.units * value.allowed;
              k.fh80thPercentileAllowed = value._80th_Percentile_Charge;

              this.summary!.chargesCap += this.VARS.ChargesCapDiscount * value._80th_Percentile_Charge;
              this.summary!.fh80th += value._80th_Percentile_Charge;
              this.summary!.fhExtended += k.fhExtendedAmount;
      
            }

            });
          this.tableData.next(td);
      }
      this.isLoading = this.queueCount !== 0;
    },
    error: err => {
      console.error('subscription error:', err);
      this.isLoading = this.queueCount !== 0
    }
    });
    */
  }

  calcArbitrationFee(fee:number): number {
    return fee / this.tableData.getValue().length;
  }

  /** Convert case data into flat table data. */
  calculateClick() {
    /*
    if(!this.mpCase)
      return;
    
    this.isLoading = true;
    this.canReset = false;
    let year = new Date().getFullYear();
    const codes:VMArbitrationCPT[] = [];
    this.summary = {arbFee:0, chargesCap:0,fhExtended:0,fh80th:0,hardCap:0,offerSpread:0,payorOffer:0,units:0};

    this.mpCase.cptCodes.forEach(t => {
      const cpt = new VMArbitrationCPT();
      cpt.cptCode = t.cptCode;
      cpt.service = this.mpCase.serviceLine;
      cpt.units = t.units;
      cpt.locationGeoZip = t.locationGeoZip;
      cpt.payorOffer = t.payorArbitration!.payorOffer;

      if(t.service === 'IOM') {
        cpt.hardCodedCap = this.VARS.IOMOfferCap;
        cpt.arbitrationFee = this.VARS.IOM_ANESArbitrationFee;
        cpt.offerSpread = this.VARS.IOMOfferSpread;
      } else if(t.service === 'AUDX') {
        cpt.hardCodedCap = this.VARS.AUDXOfferCap;
        cpt.arbitrationFee = this.VARS.PAArbitrationFee;
        cpt.offerSpread = this.VARS.AUDXOfferSpread;
      } else {
        cpt.hardCodedCap = this.VARS.PA_AnesOfferCap;
        cpt.arbitrationFee = this.VARS.PAArbitrationFee;
        cpt.offerSpread = this.VARS.PA_AnesOfferSpread;
      }

      this.summary!.units += cpt.units;
      this.summary!.hardCap += cpt.hardCodedCap;

      codes.push(cpt);
      // query for latest benchmark data
      this.queueCount++;
      this.svcData.loadBenchmark(cpt.locationGeoZip.substring(0,3), cpt.cptCode, year, 'TX', false);
    });

    this.summary!.arbFee = codes[0].arbitrationFee;  // assuming the fee is the same on all pasted records - could monitor this and sum if not
    this.summary!.offerSpread = codes[0].offerSpread; // assuming spread is same for all if service line is same for all
    this.summary!.payorOffer += codes[0].payorOffer;  // assuming same offer value is on each line
    this.tableData.next(codes);
    */
  }

  importClick() {
    const ehr = prompt('Enter EHR Number for lookup:');
    if(!ehr)
      return;

    alert('Feature not yet available');
  }

  /** Event handler to catch newly-pasted data. Sanitizes and converts the text into an array of MPCase. */
  pasteCases(e: ClipboardEvent) {
    /*
    try {
      let data = e.clipboardData;
      if(!data)
        return;
      this.pastedText = data.getData('text');
      if(this.pastedText) {
        this.parseCases();
        this.canCalculate = !!this.mpCase.cptCodes.length;  // todo: combine this with any previous save status
        this.canReset = true; // todo: again, combine with previous save status or locked state
        if(this.canCalculate){
          this.calculateClick();
        }
        console.log(this.mpCase);
      } else {
        console.log('No data detected during paste event');
      }
    }
    catch(err) {
      alert(err);
      this.resetClick();
    }
    */
  }

  parseCases() {
    /*
    if(!this.pastedText)
      return;

    // TODO: This should be moved to a service in case we need to reuse the logic later
    const fmt = this.pasteFormat;
    const expectedColumnCount = fmt.columns;
    const colSplitter = /[ \t]+/g;
    const rowSplitter = '\n';
    let rows = this.pastedText.split(rowSplitter);
    this.mpCase = new ArbitrationCase();

    try {
      rows.forEach(row => {
        if(!row)
          return; // skip blank rows

        const cols = row.split(colSplitter);
        
        if(cols.length === expectedColumnCount) {
          let cpt = this.mpCase.cptCodes.find(d => d.cptCode === cols[1]);
          if(cpt) {
            throw 'Cannot process duplicate CPT codes when doing a pre-Arbitration calculation';
          }
          cpt = new ClaimCPT();
          cpt.service = cols[0].trim();
          cpt.cptCode = cols[1].trim();
          cpt.units = parseFloat(cols[2].trim());
          cpt.locationGeoZip = cols[3].trim();
          cpt.payorArbitration = new PayorArbitration();
          cpt.payorArbitration.payorOffer = parseFloat(cols[4].replace(/[\$\,\r]/g,''));
          this.mpCase.cptCodes.push(cpt);
        } else {
          throw 'Unrecognized data format'
        }
      });

      if(this.mpCase.cptCodes.length) {
        //this.mpCase.arbitration = new Arbitration();
        let offer = 0;
        this.mpCase.cptCodes.map(d => offer+= d.payorArbitration!.payorOffer);
        this.mpCase.totalPayorOffer = offer;
        this.mpCase.service = this.mpCase.cptCodes[0].service;
        this.mpCase.serviceLine = this.mpCase.service.split(' /-')[0];
      }
    }
    catch(err) {
        this.resetClick();
        console.error(err);
        alert(err);
      }
    */
  }

  resetClick(prompt:boolean = false) {
    /*
    if(prompt && !confirm('Clear the current list of codes?'))
      return;
    this.summary = {arbFee:0, chargesCap:0,fhExtended:0,fh80th:0,hardCap:0,offerSpread:0,payorOffer:0,units:0};
    this.tableData.next([]);
    this.mpCase = new CMSCase();
    setTimeout(() => {
      this.canCalculate = false;
      this.canReset = false;
      this.caseText = '';
      this.isLoading = false;
      this.pastedText = '';
    },0);
    */
  }

  saveClick(analyze:boolean) {
    /*
    this.analyze = analyze;
    // update CMSCase with FH data
    const codes = this.tableData.getValue();
    if(!codes?.length){
      alert('Nothing to save!');
      return;
    }
    
    if(!!this.mpCase.arbitration)
      this.mpCase.arbitration.totalPayorOffer = codes[0].payorOffer;

    this.mpCase.cptCodes.forEach(v => {
      if(v.payorArbitration) {
        const td = codes.find(d => d.cptCode === v.cptCode);
        if(td) {
          //v.payorArbitration.chargesCap = this.VARS.ChargesCapDiscount * td.fh80thPercentileAllowed; // rounded to pennies
          //v.payorArbitration.hardCodedCap = td.hardCodedCap;
          v.payorArbitration._80thPercentileCharges = td.fh80thPercentileAllowed;
          v.payorArbitration.fhExtendedAmount = v.units * td.fhAllowedAmount;
          //v.payorArbitration.arbitrationFee = td.arbitrationFee;
          //v.payorArbitration.offerSpread = td.offerSpread;
        }
      }
    });
    this.svcData.createCMSCase(this.mpCase);
    */
  }

  variablesClick() {
    //const s = JSON.stringify(this.VARS);
    //alert(s);
  }
}
