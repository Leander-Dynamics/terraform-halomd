import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, CanDeactivate, RouterStateSnapshot, UrlTree } from "@angular/router";
import { Observable } from "rxjs";

type canDeactivateType = Observable<boolean | UrlTree> | 
                            Promise<boolean | UrlTree> |
                            boolean | UrlTree;

export interface CanComponentDeactivate {
    canDeactivate: () => canDeactivateType;
}

@Injectable({
    providedIn: 'root'
})
export class ModelDirtyGuard implements CanDeactivate<CanComponentDeactivate> {
    
    public canDeactivate(component: CanComponentDeactivate, 
        route: ActivatedRouteSnapshot, 
        state: RouterStateSnapshot): canDeactivateType {
        return component.canDeactivate ? component.canDeactivate() : true;
    }
}