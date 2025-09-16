using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.CodeAnalysis;

namespace MPArbitration.Model
{

    public class AppUser
    {

        private List<AppRole> _AllAppRoles = new List<AppRole>();

        [NotMapped]
        public List<AppRole> AllAppRoles
        {
            get
            {
                if (!string.IsNullOrEmpty(this.Roles) && _AllAppRoles.Count == 0)
                {
                    // parse the roles
                    var r = GetAllRoles();

                    // put global roles directly on this user object
                    var IsManager = r.Contains("manager");
                    var IsNegotiator = r.Contains("negotiator");
                    //var isNSA = r.Contains("nsa"); // controls fine-grained restrictions for certain pieces of data
                    var IsReporter = r.Contains("reporter");
                    //var isState = r.Contains("state"); // controls fine-grained restrictions for certain pieces of data
                    var IsSystem = this.Roles == "system";

                    if (IsManager || IsNegotiator || IsReporter || IsSystem)
                        return _AllAppRoles; // granular roles ignored for users with global roles

                    //parse non-global roles if user is not in a global role
                    foreach (var item in r) {
                        this.AddGranularRole(item);
                    }
                }

                return _AllAppRoles;
            }
        }

        private string[] GetAllRoles()
        {
            return this.Roles.ToLower().Split(new char[] { ',', ';' });
        }

        [NotMapped]
        public bool HasGlobalCaseRole
        {
            get
            {
                var tmp = this.AllAppRoles; // force the flag build
                return this.IsManager || this.IsNegotiator || this.IsReporter || this.IsSystem;
            }
        }

        [NotMapped]
        public bool IsBriefApprover
        {
            get
            {
                return this.GetAllRoles().Contains("BriefApprover", StringComparer.OrdinalIgnoreCase);
            }
        }

        [NotMapped]
        public bool IsBriefPreparer
        {
            get
            {
                return this.GetAllRoles().Contains("BriefPrepaere", StringComparer.OrdinalIgnoreCase);
            }
        }

        [NotMapped]
        public bool IsBriefWriter
        {
            get
            {
                return this.GetAllRoles().Contains("BriefWriter", StringComparer.OrdinalIgnoreCase);
            }
        }

        [NotMapped]
        public bool IsAdmin {
            get
            {
                return this.GetAllRoles().Contains("admin");
            }
        }

        [NotMapped]
        public bool IsManager {
            get
            {
                return this.GetAllRoles().Contains("manager");
            }
        }

        [NotMapped]
        public bool IsNegotiator {
            get
            {
                return this.GetAllRoles().Contains("negotiator");
            }
        }

        [NotMapped]
        public bool IsNSA
        {
            get
            {
                return this.GetAllRoles().Contains("nsa");
            }
        }

        [NotMapped]
        public bool IsReporter
        {
            get
            {
                return this.GetAllRoles().Contains("reporter");
            }
        }

        [NotMapped]
        public bool IsState
        {
            get
            {
                return this.GetAllRoles().Contains("state");
            }
        }

        [NotMapped]
        public bool IsSystem
        {
            get
            {
                return this.Roles == "system";
            }
        }
        public bool IsLocalHost(HostString hostString)
        {
            return hostString.Host.Equals("localhost", StringComparison.InvariantCultureIgnoreCase);
        }

        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("email")]
        [StringLength(60)]
        public string Email { get; set; } = "";

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = false;

        [JsonPropertyName("JSON")]
        [StringLength(4096)]
        public string JSON { get; set; } = "{}";

        private string _roles = "";
        [JsonPropertyName("roles")]
        [StringLength(255)]
        public string Roles  // not-concatenated roles are global. customer-specific roles look like c|1|admin or c|2|manager. authority-specific roles are a|1|reporter
        {
            get { return _roles; }
            set
            {
                this._roles = value;
                this._AllAppRoles.Clear();
            }
        } 

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        // methods
        private bool AddGranularRole(string item) {
            if(item.IndexOf('|') == -1 || this.IsSystem) 
                return false;
        
            var parts = item.Split('|');
            if(parts.Length != 3)
                return false;

            if(!int.TryParse(parts[1], out int id))
                return false;

            if (!Enum.IsDefined(typeof(UserAccessType), parts[2].ToLower()))
                return false;

            /* NOTE: There is no recognized Customer Admin modality at this time. At some point
             * there could exist some Customer management such as Contact Info or other which could
             * be take over by a dedicated person or team. At that time the following conditional test 
             * will need to be adjusted.
            */
            var lvl = Enum.Parse<UserAccessType>(parts[2].ToLower());
            if(lvl == UserAccessType.admin)
                return false;
        
            var role = UserRoleType.Empty;
            if(parts[0] == "a")
                role = UserRoleType.Authority;
            else if(parts[0] == "c")
                role = UserRoleType.Customer;

            if(role == UserRoleType.Empty)
                return false;

            ClearEntityRoles(role, id);
            if(this._AllAppRoles == null)
                this._AllAppRoles = new List<AppRole>();
            this._AllAppRoles.Add(new AppRole(role, lvl, id));
            return true;
        }

        // Clears all Granular Roles for the specified Entity Id
        private void ClearEntityRoles(UserRoleType Roletype, int EntityId)
        {
            var r = this._AllAppRoles.Where(d => d.RoleType != Roletype || d.EntityId != EntityId).ToList();
            this._AllAppRoles = r;
        }
    }

    public enum UserRoleType
    {
        Empty,
        Global,
        Authority,
        Customer
    }

    public interface IAppRole
    {
        UserRoleType RoleType { get; set; }
        int EntityId { get; set; }
        UserAccessType AccessLevel { get; set; }
    }

    public interface IAppRoleVM
    {
        bool IsManager { get; set; }
        bool IsNegotiator { get; set; }
        bool IsReporter { get; set; }
    }

    public class AppRole : IAppRole
    {
        public UserRoleType RoleType { get; set; } = UserRoleType.Empty;
        public int EntityId { get; set; } = 0;
        public UserAccessType AccessLevel { get; set; } = UserAccessType.denied;

        public AppRole(UserRoleType role, UserAccessType access, int entity)
        {
            this.RoleType = role;
            this.AccessLevel = access;
            this.EntityId = entity;
        }
    }

    public enum UserAccessType
    {
        denied = 0, // none
        admin = 1,  // read, write, delete Cases and change system attributes - only valid at the Global level for now
        manager = 2,  // read, write Cases + assign Cases to Negotiators, manage Payors, manage some user security
        negotiator = 3, // read, write Cases
        reporter = 4 // read Cases
    }
}
