import { IKeyId, IName } from "./iname";
import { ISelected } from "./iselected";

export class MultiSelectVM implements IName, IKeyId, ISelected {
    constructor(public name:string, public id:number, public key:string, public isSelected:boolean, public object:any){}
}
