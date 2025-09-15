import { ElementRef } from "@angular/core";

export class FileUploadEventArgs {
    element: any;
    file: File|undefined;
    filename = '';
    documentType = '';
}