using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Text;

namespace MPExternalDisputeAPI.Model
{
    public class MPBaseController : ControllerBase
    {
        internal static readonly string[] VALID_ROLES = { "admin", "briefapprover", "briefpreparer", "briefwriter", "manager", "negotiator", "nsa", "reporter", "state" };
        protected readonly ArbitrationDbContext _context;
        protected IConfiguration _configuration;

        public MPBaseController(ArbitrationDbContext context, IConfiguration configuration)
        {
            _configuration = configuration;
            _context = context;
        }
    }
}
