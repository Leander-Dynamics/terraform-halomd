import { LogLevel } from "@azure/msal-browser";
import { UtilService } from "../services/util.service";
import { loggerCallback } from "../app.module";
import { BaseFee } from "./base-fee";

export enum FeeRecipient {
    Arbitrator,
    Authority
}

export class AuthorityDisputeFee {
    public id = 0;
    public amountDue = 0;
    public authorityDisputeId = 0;
    public baseFee:BaseFee|undefined;
    public baseFeeId = 0;
    public createdBy ='';
    //public createdOn:Date|undefined;
    public dueOn:Date|undefined;
    public feeRecipient = FeeRecipient.Arbitrator;
    public invoiceLink = '';
    public invoiceReceivedOn:Date|undefined;
    public isExpanded = false;
    public isRefundable = false;
    public isRequired = false;
    public paidBy = '';
    public paidOn:Date|undefined;
    public paymentMethod = '';
    public paymentReferenceNumber = '';
    public paymentRequestedOn:Date|undefined;
    public refundableAmount = 0;
    public refundAmount = 0;
    public refundDueOn:Date|undefined;
    public refundedOn:Date|undefined;
    public refundedTo='';
    public refundMethod = '';
    public refundReferenceNumber='';
    public refundRequestedBy='';
    public refundRequestedOn:Date|undefined;
    public updatedBy = '';
    public updatedOn:Date|undefined;
    public wasRefunded = false;
    public wasRefundRequested = false;

    public get dueOnForPicker() {
        return !!this.dueOn ? this.dueOn.toLocaleDateString() : undefined;
    }
    public set dueOnForPicker(value:any){
        if(!value)
            this.dueOn = undefined;
        if(UtilService.IsValidUSDate(value))
            this.dueOn = new Date(value);
    }

    public get invoiceReceivedOnForPicker() {
        return !!this.invoiceReceivedOn ? this.invoiceReceivedOn.toLocaleDateString() : undefined;
    }
    public set invoiceReceivedOnForPicker(value:any){
        if(!value)
            this.invoiceReceivedOn = undefined;
        if(UtilService.IsValidUSDate(value))
            this.invoiceReceivedOn = new Date(value);
    }

    public get feeName():string {
        return !!this.baseFee ? this.baseFee.feeName : '';
    }
    public get paidByShort():string{
        if(!this.paidBy)
            return '';
        return UtilService.ToTitleCase(this.paidBy.substring(0,this.paidBy.indexOf('@')).replaceAll('.',' '));
    }

    public get paidOnForPicker() {
        return !!this.paidOn ? this.paidOn.toLocaleDateString() : undefined;
    }
    public set paidOnForPicker(value:any){
        if(!value)
            this.paidOn = undefined;
        if(UtilService.IsValidUSDate(value))
            this.paidOn = new Date(value);
    }

    public get paymentRequestedOnForPicker() {
        return !!this.paymentRequestedOn ? this.paymentRequestedOn.toLocaleDateString() : undefined;
    }
    public set paymentRequestedOnForPicker(value:any){
        if(!value)
            this.paymentRequestedOn = undefined;
        if(UtilService.IsValidUSDate(value))
            this.paymentRequestedOn = new Date(value);
    }

    public get refundDueOnForPicker() {
        return !!this.refundDueOn ? this.refundDueOn.toLocaleDateString() : undefined;
    }
    public set refundDueOnForPicker(value:any){
        if(!value)
            this.refundDueOn = undefined;
        if(UtilService.IsValidUSDate(value))
            this.refundDueOn = new Date(value);
    }

    public get refundedOnForPicker() {
        return !!this.refundedOn ? this.refundedOn.toLocaleDateString() : undefined;
    }
    public set refundedOnForPicker(value:any){
        if(!value)
            this.refundedOn = undefined;
        if(UtilService.IsValidUSDate(value))
            this.refundedOn = new Date(value);
    }

    public get refundRequestedOnForPicker() {
        return !!this.refundRequestedOn ? this.refundRequestedOn.toLocaleDateString() : undefined;
    }
    public set refundRequestedOnForPicker(value:any){
        if(!value)
            this.refundRequestedOn = undefined;
        if(UtilService.IsValidUSDate(value))
            this.refundRequestedOn = new Date(value);
    }

    constructor(obj?:any) {
        if(!obj)
            return;
    
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        try {
            if(this.invoiceReceivedOn)
                this.invoiceReceivedOn = new Date(obj.invoiceReceivedOn);
            if(this.dueOn)
                this.dueOn = new Date(obj.dueOn); 
            if(this.paidOn)
                this.paidOn = new Date(obj.paidOn);
            if(this.paymentRequestedOn)
                this.paymentRequestedOn = new Date(obj.paymentRequestedOn);
            if(this.refundDueOn)
                this.refundDueOn = new Date(obj.refundDueOn);
            if(this.refundedOn)
                this.refundedOn = new Date(obj.refundedOn);
            if(this.refundRequestedOn)
                this.refundRequestedOn = new Date(obj.refundRequestedOn);
            if(this.updatedOn)
                this.updatedOn = new Date(obj.updatedOn);

            // objects
            if(!!obj.baseFee){
                this.baseFee = new BaseFee(obj.baseFee);
                this.baseFeeId = this.baseFee.id;
            }

        } catch(err) {
            loggerCallback(LogLevel.Error, 'Unable to instantiate Entity');
            console.error(err);
        }
    }
}
