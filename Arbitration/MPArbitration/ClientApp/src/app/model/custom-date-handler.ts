import { Injectable } from "@angular/core";
import { LogLevel } from "@azure/msal-browser";
import { NgbDateAdapter, NgbDateParserFormatter, NgbDateStruct } from "@ng-bootstrap/ng-bootstrap";
import { loggerCallback } from "../app.module";

/**
 * This Service handles how the date is represented in scripts i.e. ngModel.
 */
 @Injectable()
 export class CustomNgbDateAdapter extends NgbDateAdapter<string> {
 
   readonly DELIMITER = '/';
 
   fromModel(value: string|Date|null|undefined): NgbDateStruct | null {
     if (!!value) {
      if(value instanceof Date)
        return { month: value.getMonth()+1, day: value.getDate(), year: value.getFullYear() };
      
       const date = value.split(this.DELIMITER);
       return {
         day : parseInt(date[1], 10),
         month : parseInt(date[0], 10),
         year : parseInt(date[2], 10)
       };
     }
     return null;
   }
 
  toModel(date: NgbDateStruct | null): string | null {
    // loggerCallback(LogLevel.Verbose, `toModel date: ${date}`);
    return date ? date.month + this.DELIMITER + date.day + this.DELIMITER + date.year : null;
  }
}
 
 
/**
 * This Service handles how the date is rendered and parsed from keyboard i.e. in the bound input field.
 */
@Injectable()
export class CustomDateParserFormatter extends NgbDateParserFormatter {

  readonly DELIMITER = '/';

  parse(value: string|Date|undefined|null): NgbDateStruct | null {
    if (!!value) {
      if(value instanceof Date)
        return { month: value.getMonth()+1, day: value.getDate(), year: value.getFullYear() };
      
      const parts = value.split('/');
      if (parts.length === 3 && this.isNumber(parts[0]) && this.isNumber(parts[1]) && this.isNumber(parts[2])) {
        return { month: parseInt(parts[0]), day: parseInt(parts[1]), year: parseInt(parts[2]) };
      }
    }
    return null;
  }

  format(date: NgbDateStruct | Date | null): string {
    if(date instanceof Date)
      return `${this.padNumber(date.getMonth()+1)}${this.DELIMITER}${this.padNumber(date.getDate())}${this.DELIMITER}${date.getFullYear()}`
    return !!date && this.isNumber(date.day) && this.isNumber(date.month) && this.isNumber(date.year)
      ? `${this.padNumber(date.month)}${this.DELIMITER}${this.padNumber(date.day)}${this.DELIMITER}${date.year}`
      : '';
  }

  private isNumber(value: any): value is number {
    return !isNaN(parseInt(value));
  }

  private padNumber(value: number) {
    if (this.isNumber(value)) {
      return `0${value}`.slice(-2);
    } else {
      return '';
    }
  }
}