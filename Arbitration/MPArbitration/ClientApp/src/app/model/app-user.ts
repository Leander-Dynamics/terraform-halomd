import { UtilService } from "../services/util.service";

export class GranularRoleVM implements IAppRole, IAppRoleVM {
    public name = '';
    public accessLevel:UserAccessType = UserAccessType.denied;
    public entityId = 0;
    public roleType:UserRoleType = UserRoleType.Empty;

    public userId = 0;
    public isManager:boolean | undefined = false;
    public isNegotiator:boolean | undefined = false;
    public isReporter:boolean | undefined = false;

    constructor(obj?:any) {
        if(!obj)
            return;

        Object.assign(this, JSON.parse(JSON.stringify(obj)));
    }
}

export enum UserRoleType {
    Empty,
    Global,
    Authority,
    Customer
}

export enum UserAccessType {
    denied = 0, // none
    admin = 1,  // read, write, delete Cases and change system attribs - only valid at the Global level for now
    manager = 2,  // read, write Cases + assign Cases to Negotiators, manage Payors, manage some user security
    negotiator = 3, // read, write Cases
    reporter = 4 // read Cases
}

export interface IAppRole {
    roleType:UserRoleType;
    entityId:number;
    accessLevel: UserAccessType;
}

export interface IAppRoleVM {
    isManager:boolean | undefined;
    isNegotiator:boolean | undefined;
    isReporter:boolean | undefined;

}

export class AppRole implements IAppRole {
    public roleType:UserRoleType = UserRoleType.Empty;
    public entityId = 0;
    public accessLevel = UserAccessType.denied;

    constructor(role:UserRoleType, access:UserAccessType, entity:number = 0) {
        this.roleType = role;
        this.accessLevel = access;
        this.entityId = entity;
    }
}

export class AppUser {
    /*
    static get ALL_ROLES() {
        return ['Admin','Manager','Negotiator','Reporter'];
        // or ...
        const roles = Object.values(UserAccessType).filter(value => typeof value === 'string').map(key => {
            const result = (key as string).split(/(?=[A-Z][a-z])/);
            return { id: (<any>UserAccessType)[key] as number, key: result.join(' ') };
          });
    }
    */
    public id = 0;
    public appRoles:AppRole[] | undefined = new Array<AppRole>();
    public currentVersion = '';
    public email = '';
    public isActive = false;
    public JSON = '{}';
    public roles = '';
    public updatedBy = '';
    public updatedOn: Date|undefined;
    
    // some viewmodel properties
    public isAdmin:boolean | undefined = false;
    public isManager:boolean | undefined = false;
    public isNegotiator:boolean | undefined = false;
    public isNSA:boolean | undefined = false;
    public isReporter:boolean | undefined = false;
    public isState:boolean | undefined = false;
    public isBriefApprover:boolean|undefined = false;
    public isBriefPreparer:boolean|undefined = false;
    public isBriefWriter:boolean|undefined = false;
    public get emailLowerCase() {
        return this.email.toLowerCase();
    }
    public get shortName():string{
        if(!this.email)
            return '';
        const n = this.email.indexOf('@');
        if(n < 0)
            return this.email;
        return UtilService.ToTitleCase(this.email.substring(0,n).replace('.',' '));
    }
    
    private addGranularRole(item:string):boolean {
        if(item.indexOf('|') === -1) 
            return false;
        
        const parts = item.split('|');
        if(parts.length !== 3)
            return false;

        const id = parseInt(parts[1]);
        if(isNaN(id))
            return false;
        
        const lvl:UserAccessType | undefined = (UserAccessType as any)[parts[2].toLowerCase()];
        /* NOTE: There is no recognized Customer Admin modality at this time. At some point
         * there could exist some Customer management such as Contact Info or other which could
         * be take over by a dedicated person or team. At that time the following conditional test 
         * will need to be adjusted.
        */
        if(!lvl || lvl === UserAccessType.admin)
            return false;
        
        let role = UserRoleType.Empty;
        if(parts[0] === 'a')
            role = UserRoleType.Authority;
        else if(parts[0] === 'c')
            role = UserRoleType.Customer;

        if(role === UserRoleType.Empty)
            return false;

        this.clearEntityRoles(role, id);
        if(!this.appRoles)
            this.appRoles = new Array<AppRole>();
        this.appRoles.push(new AppRole(role, lvl, id));
        return true;
    }

    /* prob gonna just keep the global roles in the root since other code depends on this and it is handy to work with
    private addGlobalRole(t:UserAccessType) {
        this.clearGlobalRoles();
        if(!this.appRoles)
            this.appRoles = new Array<AppRole>();
        this.appRoles.push(new AppRole(RoleType.Global, t));
    }
    */
   
    // Clears all Granular Roles for the specified Entity Id
    private clearEntityRoles(t:UserRoleType, authorityId:number) {
        if(!this.appRoles)
            return;
        
        const r = this.appRoles.filter(d => d.roleType !== t || d.entityId !== authorityId);
        this.appRoles = r;
    }

    /* not needed at this time
    private clearGlobalRoles() {
        if(!this.appRoles)
            return;
        const r = this.appRoles.filter(d=>d.roleType !== RoleType.Global);
        this.appRoles = r;
    }
    */

    constructor(obj?: any) {
        if(!obj)
            return;
        Object.assign(this, JSON.parse(JSON.stringify(obj)));
        
        try {
            if(this.updatedOn)
                this.updatedOn = new Date(obj.updatedOn);

            if (this.roles) {
                const r = this.roles.toLowerCase().split(/[,;]+/);

                // put global roles directly on this user object
                this.isAdmin = r.indexOf('admin') > -1;
                this.isManager = r.indexOf('manager') > -1;
                this.isNegotiator = r.indexOf('negotiator') > -1;
                this.isNSA = r.indexOf('nsa') > -1;
                this.isReporter = r.indexOf('reporter') > -1;
                this.isState = r.indexOf('state') > -1;
                this.isBriefApprover = r.indexOf('briefapprover') > -1;
                this.isBriefPreparer = r.indexOf('briefpreparer') > -1;
                this.isBriefWriter = r.indexOf('briefwriter') > -1;

                // setup AppRoles array - removed this for now since easier to detect granular roles if appRoles doesn't contain global roles
                //if(this.isManager) { this.addGlobalRole(UserAccessType.manager) }
                //if(this.isNegotiator) { this.addGlobalRole(UserAccessType.negotiator) }
                //if(this.isReporter) { this.addGlobalRole(UserAccessType.reporter) }

                // allow admin to exist in addition to one of the other global roles
                //if(this.isAdmin) { this.appRoles.push(new AppRole(RoleType.Global, UserAccessType.admin)) }

                if(this.isManager||this.isNegotiator||this.isReporter)
                    return;

                //parse non-global roles if user is not in a global role
                for(const item of r) {
                    this.addGranularRole(item);
                }
            }
            
        } catch (err) {
            console.error('Error in AppUser construction');
            console.error(err);
        }
    }
}