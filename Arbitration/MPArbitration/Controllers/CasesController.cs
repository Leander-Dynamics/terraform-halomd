using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MPArbitration.Model;
using MPArbitration.Utility;
using System.Data.SqlClient;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MPArbitration.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CasesController : MPBaseController
    {
        private readonly ILogger<CasesController> _logger;
        private IImportDataSynchronizer _synchronizer;

        #region Constructor
        public CasesController(ILogger<CasesController> logger, ArbitrationDbContext context, IConfiguration configuration, IImportDataSynchronizer synchronizer) : base(context, configuration)
        {
            _logger = logger;
            _synchronizer = synchronizer;
        }
        #endregion


        // GET: <ArbitrationCase>
        /// <summary>
        /// Fetch an ArbitrationCase by its PrimaryKey value.
        /// </summary>
        /// <param name="id">Integer PrimaryKey</param>
        /// <returns>Single, matching ArbitrationCase</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ArbitrationCase>> GetArbitrationCase(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }
            try
            {
                var claim = await _context.ArbitrationCases
                            .AsNoTracking()
                            .Include(c => c.Arbitrators)
                            //.Include(d => d.SettlementDetails.Where(g=>!g.IsDeleted))
                            .Include(c => c.Tracking)
                            .Include(c => c.CPTCodes)
                            .Include(d => d.Notes)
                            .Include(d => d.Notifications.Where(g => !g.IsDeleted))
                            .Include(d => d.OfferHistory)  //.Include(d => d.Log)  // unbelievably slow to include this!
                            .Where(d => !d.IsDeleted
                                        && d.Id == id
                                        && (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)))
                            .FirstOrDefaultAsync();

                if (claim == null)
                    return NotFound();

                // fetch some view-model properties for arbitrators and include them
                foreach (var ca in claim.Arbitrators)
                {
                    var a = await _context.Arbitrators.AsNoTracking().FirstOrDefaultAsync(d => d.Id == ca.ArbitratorId);
                    if (a != null)
                    {
                        ca.EliminateForServices = a.EliminateForServices;
                        ca.Email = a.Email;
                        ca.Name = a.Name;
                        ca.Notes = a.Notes;
                        ca.Phone = a.Phone;
                        ca.Statistics = a.Statistics;
                        ca.IsLastResort = a.IsLastResort;
                    }
                }

                if (claim.PayorNegotiatorId.HasValue && claim.PayorNegotiatorId.Value > 0)
                    claim.PayorNegotiator = await _context.Negotiators.FindAsync(claim.PayorNegotiatorId);

                // This is an example of dealing with dates that need to be translated from the server to a local time
                // (basically CST for now) since that's where these dates are originating in "the real world".
                // To make this more consistent, the Import routines need to require a time zone specifier to be posted
                // along with the import files (if it cannot be determined from the HTTP request) so the
                // hours can be adjusted to UTC when being saved to the database
                /*
                calc.DOB = Utilities.GetAsCSTDate(calc.DOB, false);
                calc.FirstResponseDate = Utilities.GetAsCSTDate(calc.FirstResponseDate, false);
                calc.ServiceDate = Utilities.GetAsCSTDate(calc.ServiceDate, false);
                */

                // These dates are always UTC before being persisted to storage so when sending them
                // back to an API client, the Kind attribute needs to be set to UTC so JSON adds the 'Z'
                if (claim.CreatedOn.HasValue)
                    claim.CreatedOn = DateTime.SpecifyKind(claim.CreatedOn.Value, DateTimeKind.Utc);
                if (claim.UpdatedOn.HasValue)
                    claim.UpdatedOn = DateTime.SpecifyKind(claim.UpdatedOn.Value, DateTimeKind.Utc);

                await Extensions.EnsureHolidays(_context);

                var nsa = await _context.Authorities.AsNoTracking().Include(b => b.TrackingDetails).FirstOrDefaultAsync(d => d.Key == "nsa");
                var changed2 = Utilities.FixStateArbitrationCaseDates(claim);
                bool changed = Utilities.ValidateTracking(claim, nsa, null);

                changed = changed || changed2;

                if (changed)
                {
                    claim.UpdatedBy = "system";
                    claim.UpdatedOn = DateTime.UtcNow;
                    _context.Entry(claim).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }

                return Ok(claim);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: <ArbitrationCase>
        /// <summary>
        /// Fetch a shallow ArbitrationCase by the PayorClaimNumber.
        /// This GET call is only used to verify that a Claim with a certain id exists, not to fetch the complete ArbitrationCase deep object.
        /// </summary>
        /// <param name="id">Integer PrimaryKey</param>
        /// <returns>Single, matching ArbitrationCase</returns>
        [HttpGet("claim")]
        public async Task<ActionResult<int>> GetArbitrationCaseByClaimId([FromQuery] string id, [FromQuery] int exclude = 0)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }

            var calc = await _context.ArbitrationCases
                .Where(d => !d.IsDeleted
                            //&& d.Status != ArbitrationStatus.Ineligible
                            && d.PayorClaimNumber == id
                            && (exclude == 0 || d.Id != exclude)
                            && (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)))
                .FirstOrDefaultAsync();

            if (calc == null)
                return 0;

            return Ok(calc.Id);
        }

        // GET: <ArbitrationCase>
        /// <summary>
        /// Returns True if there is any case without the IsDeleted flag in the system matching the given criteria. Not security trimmed.
        /// </summary>
        /// <param name="key">The Authority key</param>
        /// <param name="id">The Authority's Case Id</param>
        /// <returns>True or False</returns>
        [HttpGet("chkcase")]
        public async Task<ActionResult<int>> CheckForAuthorityCase([FromQuery] string key, string id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            bool isNSA = key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase);
            try
            {
                var calc = await _context.ArbitrationCases
                    .Where(d => !d.IsDeleted
                                && (isNSA || d.Authority == key)
                                && ((!isNSA && d.AuthorityCaseId == id) || (isNSA && d.NSACaseId == id)))
                    .FirstOrDefaultAsync();

                return Ok(calc?.Id ?? 0);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: <ArbitrationCase>
        /// <summary>
        /// Fetch a shallow ArbitrationCase by the PayorClaimNumber.
        /// This GET call is only used to verify that a case with a certain claim id exists, not to fetch the complete ArbitrationCase deep object.
        /// </summary>
        /// <param name="id">Integer PrimaryKey</param>
        /// <returns>Single, matching ArbitrationCase</returns>
        [HttpGet("byauthority")]
        public async Task<ActionResult<int>> GetArbitrationCaseIdByAuthority([FromQuery] string auth, [FromQuery] string aid)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (string.IsNullOrEmpty(auth) || string.IsNullOrEmpty(aid))
                return NoContent();

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }

            var calc = await _context.ArbitrationCases
                .Where(d => !d.IsDeleted
                            && d.Authority == auth
                            && d.AuthorityCaseId == aid
                            && (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)))
                .FirstOrDefaultAsync();

            if (calc == null)
                return NotFound();

            return Ok(calc.Id);
        }

        [HttpGet("arbrejections")]
        public async Task<ActionResult<IEnumerable<ArbitrationCase>>> GetPendingArbitratorRejections()
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }

            try
            {
                var cases = from c in _context.Set<ArbitrationCase>()
                            .Where(d => !d.IsDeleted &&
                            (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)) &&
                            d.Status != ArbitrationStatus.ClosedPaymentReceived &&
                            d.Status != ArbitrationStatus.ClosedPaymentWithdrawn &&
                            d.Status != ArbitrationStatus.Ineligible &&
                            d.Status != ArbitrationStatus.SettledArbitrationPendingPayment &&
                            d.Status != ArbitrationStatus.SettledInformalPendingPayment &&
                            d.Status != ArbitrationStatus.SettledOutsidePendingPayment)
                            .Include(g => g.Benchmarks).Include(g => g.Arbitrators)
                            select c;

                var results = await cases.AsNoTracking().AsSplitQuery().Where(x => x.Arbitrators.Count() > 1).ToListAsync();
                if (results.Count() > 0)
                {
                    foreach (var result in results)
                    {
                        foreach (var arb in result.Arbitrators)
                        {
                            arb.Arbitrator = await _context.Arbitrators.FindAsync(arb.ArbitratorId);
                        }
                    }

                    return Ok(results); // to include tracking data -> return Ok(cases.Include(j => j.Tracking));
                }
                else
                {
                    return NoContent();
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// CurrentCases refer to cases that haven't been Settled, Closed or marked Ineligible, as well as cases
        /// without the IsDeleted flag set to true.
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        [HttpGet("currentcases")]
        public async Task<ActionResult<IEnumerable<ArbitrationCase>>> GetCurrentCases([FromQuery] string u)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }

            if (u == "none")
                u = string.Empty;

            try
            {
                var cases = from c in _context.Set<ArbitrationCase>().Where(d =>
                            !d.IsDeleted &&
                            (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)) &&
                            (d.Status == ArbitrationStatus.ActiveArbitrationBriefCreated ||
                             d.Status == ArbitrationStatus.ActiveArbitrationBriefNeeded ||
                             d.Status == ArbitrationStatus.ActiveArbitrationBriefSubmitted ||
                             d.Status == ArbitrationStatus.DetermineAuthority ||
                             d.Status == ArbitrationStatus.InformalInProgress ||
                             d.Status == ArbitrationStatus.MissingInformation ||
                             d.Status == ArbitrationStatus.New ||
                             d.Status == ArbitrationStatus.Open ||
                             d.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefCreated ||
                             d.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded ||
                             d.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefSubmitted ||
                             d.NSAWorkflowStatus == ArbitrationStatus.DetermineAuthority ||
                             d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress ||
                             d.NSAWorkflowStatus == ArbitrationStatus.MissingInformation ||
                             d.NSAWorkflowStatus == ArbitrationStatus.New ||
                             d.NSAWorkflowStatus == ArbitrationStatus.Open) &&
                            (string.IsNullOrEmpty(u) || d.AssignedUser == u))
                            .Include(g => g.Arbitrators).Include(g => g.Benchmarks)
                            select c;

                var results = await cases.AsNoTracking().AsSplitQuery().ToListAsync();
                if (results.Count() > 2000)
                    return BadRequest("This would return over 2000 records. Please narrow your search.");

                if (results.Count() > 0)
                {
                    foreach (var result in results)
                    {
                        foreach (var arb in result.Arbitrators)
                        {
                            arb.Arbitrator = await _context.Arbitrators.FindAsync(arb.ArbitratorId);
                        }
                    }

                    return Ok(results); // to include tracking data -> return Ok(cases.Include(j => j.Tracking));
                }
                else
                {
                    return NoContent();
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/archives")]
        public async Task<ActionResult<IEnumerable<CaseArchive>>> GetCaseArchivesAsync(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }
            // TODO: The granular customer restrictions need to be implemented here
            try
            {
                return await _context.CaseArchives.AsNoTracking().Where(d => d.ArbitrationCaseId == id).ToArrayAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// TODO: This needs updating to differentiate between NSA briefs and State briefs. 
        /// It currently uses the single date field in the ArbitrationCase header but this doesn't work with "NSA cases".
        /// Since current version of Entity Framework doesn't work with JSON, make this into a raw SQL call that 
        /// uses a UNION of two queries involving both the NSATracking and CaseTracking values, mapping the
        /// records back into ArbitrationCase objects.
        /// </summary>
        /// <returns></returns>
        [HttpGet("BriefDueSoon")]
        public async Task<ActionResult<IEnumerable<ArbitrationCase>>> GetBriefDueSoon([FromQuery] int fed = 0)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }

            try
            {
                IQueryable<ArbitrationCase>? cases = null;

                if (fed == 0)
                {
                    cases = from c in _context.Set<ArbitrationCase>().Where(d =>
                                !d.IsDeleted &&
                                (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)) &&
                                //d.ArbitrationBriefDueDate.HasValue &&
                                //d.ArbitrationBriefDueDate.Value.Date.AddDays(-2) <= DateTime.Today &&
                                //d.ArbitrationBriefDueDate.Value.Date >= DateTime.Today &&
                                (d.Status == ArbitrationStatus.ActiveArbitrationBriefNeeded || d.Status == ArbitrationStatus.ActiveArbitrationBriefCreated))
                                .Include(g => g.Benchmarks).Include(g => g.Arbitrators)
                            select c;
                }
                else
                {
                    cases = from c in _context.Set<ArbitrationCase>().Where(d =>
                            !d.IsDeleted &&
                            (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)) &&
                            (d.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded || d.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefCreated))
                            .Include(g => g.Benchmarks).Include(g => g.Arbitrators)
                            select c;
                }

                var results = await cases.AsNoTracking().AsSplitQuery().Where(x => x.Arbitrators.Count() > 1).ToListAsync();
                if (results.Count() > 0)
                {
                    foreach (var result in results)
                    {
                        foreach (var arb in result.Arbitrators)
                        {
                            arb.Arbitrator = await _context.Arbitrators.FindAsync(arb.ArbitratorId);
                        }
                    }

                    return Ok(results); // to include tracking data -> return OK(cases.Include(j => j.Tracking));
                }
                else
                {
                    return NoContent();
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("NeedsNSARequest")]
        public async Task<ActionResult<IEnumerable<ArbitrationCase>>> GetNeedsNSARequestAsync(string? deadlineFilter, string? customer)
        {
            var user = await GetCurrentUser();

            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsManager && !user.IsNegotiator && !user.IsSystem)
                return Unauthorized("This endpoint requires global Manager or Negotiator permissions");

            try
            {
                DateOnly deadLine = DateOnly.FromDateTime(DateTime.Today);
                if (!string.IsNullOrEmpty(deadlineFilter))
                {
                    deadLine = DateOnly.FromDateTime(DateTime.Parse(deadlineFilter));
                }
                customer = (string.IsNullOrEmpty(customer)) ? "%" : customer.Trim();

                // This is a hack to cover the fact that we cannot directly query a dynamic JSON
                // field (NSATracking) without using a juicy, manual query like the one found in the authority recalculation routine.
                // We might shift to that if this one is insufficient but, for now, we know EOBDate + 29 work days is the deadline so
                // this -60 should cover everything that's legit.
                // The approach is to return slightly more records than necessary but we then 
                // allow the client side to recalculate the deadlines and then weed out the ones which have expired.
                var testInterval = -60;
                var cases = from c in _context.Set<ArbitrationCase>().Include(d => d.CPTCodes).Where(d =>
                            !d.IsDeleted &&
                            !string.IsNullOrEmpty(d.Payor) &&
                            d.NSAStatus == "Pending NSA Negotiation Request" &&
                            // NOTE: We cannot use DateTime.Today.AddWorkDays when dealing with Entity Framework because SQL does not support it yet.
                            // Therefore, it is up to the client to further refine the results to deal with dates slightly outside of the "approximate workdays" window of 48 calendar days

                            // NOTE: Per DevOps item 1200, we now use (last)EOBDate. -with affect 8/10/2023
                            EF.Functions.Like(d.Customer, customer) &&
                            (d.NegotiationNoticeDeadline == deadLine.ToDateTime(TimeOnly.MinValue)) &&
                            d.EOBDate.HasValue && d.EOBDate.Value > DateTime.Today.AddDays(testInterval) &&
                            d.NSAWorkflowStatus != ArbitrationStatus.ClosedPaymentReceived &&
                            d.NSAWorkflowStatus != ArbitrationStatus.ClosedPaymentWithdrawn &&
                            d.NSAWorkflowStatus != ArbitrationStatus.Ineligible &&
                            d.NSAWorkflowStatus != ArbitrationStatus.SettledArbitrationPendingPayment &&
                            d.NSAWorkflowStatus != ArbitrationStatus.SettledInformalPendingPayment &&
                            d.NSAWorkflowStatus != ArbitrationStatus.SettledOutsidePendingPayment &&
                            !d.AuthorityStatus.StartsWith("Settled") &&
                            !d.AuthorityStatus.StartsWith("Assigned") &&
                            !d.AuthorityStatus.StartsWith("Report")
                            )
                            join p in _context.Set<Payor>().Where(n => n.SendNSARequests && !string.IsNullOrEmpty(n.NSARequestEmail)) on c.Payor equals p.Name
                            select c;

                var results = await cases.AsNoTracking().ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /*
        [HttpGet("notify/OpenNegotiation/{authorityId}")]
        public async Task<ActionResult<IEnumerable<ArbitrationCase>>> GetOpenNegotiationClaimsAsync(int authorityId)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.HasGlobalCaseRole && !user.IsSystem)
                return Unauthorized("This endpoint requires global permissions");

            try
            {
                // This is a hack to cover the fact that we cannot directly query a dynamic JSON
                // field (NSATracking) without using a juicy, manual query like the one found in the authority recalculation routine.
                // We might shift to that if this one is insufficient but, for now, we know EOBDate + 29 work days is the deadline so
                // this -60 should cover everything that's legit.
                // The approach is to return slightly more records than necessary but we then 
                // allow the client side to recalculate the deadlines and then weed out the ones which have expired.
                var testInterval = -60;
                var cases = from c in _context.Set<ArbitrationCase>().Include(d => d.CPTCodes).Where(d =>
                            !d.IsDeleted &&
                            !string.IsNullOrEmpty(d.Payor) &&
                            d.NSAStatus == "Pending NSA Negotiation Request" &&
                            // NOTE: We cannot use DateTime.Today.AddWorkDays when dealing with Entity Framework because SQL does not support it yet.
                            // Therefore, it is up to the client to further refine the results to deal with dates slightly outside of the "approximate workdays" window of 48 calendar days

                            // NOTE: Per DevOps item 1200, we now use (last)EOBDate. -with affect 8/10/2023
                            d.EOBDate.HasValue &&
                            d.EOBDate.Value > DateTime.Today.AddDays(testInterval) &&
                            d.NSAWorkflowStatus != ArbitrationStatus.ClosedPaymentReceived &&
                            d.NSAWorkflowStatus != ArbitrationStatus.ClosedPaymentWithdrawn &&
                            d.NSAWorkflowStatus != ArbitrationStatus.Ineligible &&
                            d.NSAWorkflowStatus != ArbitrationStatus.SettledArbitrationPendingPayment &&
                            d.NSAWorkflowStatus != ArbitrationStatus.SettledInformalPendingPayment &&
                            d.NSAWorkflowStatus != ArbitrationStatus.SettledOutsidePendingPayment &&
                            !d.AuthorityStatus.StartsWith("Settled") &&
                            !d.AuthorityStatus.StartsWith("Assigned") &&
                            !d.AuthorityStatus.StartsWith("Report"))

                            join p in _context.Set<Payor>().Where(n => n.SendNSARequests && !string.IsNullOrEmpty(n.NSARequestEmail))
                            on c.Payor equals p.Name

                            select c;

                var results = await cases.AsNoTracking().ToListAsync();

                return OK(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        */

        /// <summary>
        /// This method replaces the old, slow Search method.
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="inactive"></param>
        /// <param name="closed"></param>
        /// <returns></returns>
        [HttpPost("search")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<IEnumerable<ArbitrationCase>>> SearchAsync([FromBody] ArbitrationCase criteria, [FromQuery] bool inactive = false, [FromQuery] bool closed = false)
        {
            if (criteria == null)
                return BadRequest("No query");

            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole && !user.IsSystem)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (!string.IsNullOrEmpty(criteria.Customer) && !allowedCustomerNames.Contains(criteria.Customer))
                    return new ArbitrationCase[] { };  // user has no access to the Customer's Cases so short-circuit the request
            }

            criteria.NSAWorkflowStatus = criteria.Status;

            // short-circuit the Authority search if the Authority is NSA
            criteria.Authority = criteria.Authority.ToLower();

            if (criteria.Authority == "*")
            {
                criteria.Authority = "";
            }

            ArbitrationCase[]? cases = null;

            try
            {

                var tmp = criteria.Arbitrators.Count() > 0 ? criteria.Arbitrators.First() : null;

                // Dynamic criteria construction
                IQueryable<ArbitrationCase> searchQ = _context.Set<ArbitrationCase>().Where(d => !d.IsDeleted); // exclude deleted recs

                // By default, only return "active" claims
                if (criteria.Status == ArbitrationStatus.Search && criteria.NSAWorkflowStatus == ArbitrationStatus.Search)
                {
                    // these appear so redundant but result in a better query string sent to SQL (possibly a better way but no time to poke LinqToSql to find it)
                    searchQ = searchQ.AddCondition(() => inactive && !closed, x => x.Status == ArbitrationStatus.ActiveArbitrationBriefCreated ||
                                                                       x.Status == ArbitrationStatus.ActiveArbitrationBriefNeeded ||
                                                                       x.Status == ArbitrationStatus.ActiveArbitrationBriefSubmitted ||
                                                                       x.Status == ArbitrationStatus.DetermineAuthority ||
                                                                       x.Status == ArbitrationStatus.InformalInProgress ||
                                                                       x.Status == ArbitrationStatus.MissingInformation ||
                                                                       x.Status == ArbitrationStatus.New ||
                                                                       x.Status == ArbitrationStatus.Open ||
                                                                       x.Status == ArbitrationStatus.PendingArbitration ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefCreated ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefSubmitted ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.DetermineAuthority ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.MissingInformation ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.New ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.Open ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.PendingArbitration ||
                                                                        x.Status == ArbitrationStatus.Ineligible ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.Ineligible);

                    searchQ = searchQ.AddCondition(() => !inactive && !closed, x => x.Status == ArbitrationStatus.ActiveArbitrationBriefCreated ||
                                                                       x.Status == ArbitrationStatus.ActiveArbitrationBriefNeeded ||
                                                                       x.Status == ArbitrationStatus.ActiveArbitrationBriefSubmitted ||
                                                                       x.Status == ArbitrationStatus.DetermineAuthority ||
                                                                       x.Status == ArbitrationStatus.InformalInProgress ||
                                                                       x.Status == ArbitrationStatus.MissingInformation ||
                                                                       x.Status == ArbitrationStatus.New ||
                                                                       x.Status == ArbitrationStatus.Open ||
                                                                       x.Status == ArbitrationStatus.PendingArbitration ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefCreated ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefSubmitted ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.DetermineAuthority ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.MissingInformation ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.New ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.Open ||
                                                                        x.NSAWorkflowStatus == ArbitrationStatus.PendingArbitration);
                    searchQ = searchQ.AddCondition(() => !inactive && closed, x => !(x.Status == ArbitrationStatus.Ineligible && x.NSAWorkflowStatus == ArbitrationStatus.Ineligible));
                }
                else
                {
                    searchQ = searchQ.AddCondition(() => true, x => x.Status == criteria.Status || x.NSAWorkflowStatus == criteria.Status);
                }

                /* Add other criteria if provided */
                searchQ = searchQ.AddCondition(() => criteria.NSACaseId == "{empty}", x => x.NSACaseId == "");
                searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.NSACaseId) && criteria.NSACaseId != "{empty}", x => x.NSACaseId.Contains(criteria.NSACaseId));

                searchQ = searchQ.AddCondition(() => criteria.AuthorityCaseId == "{empty}", x => x.AuthorityCaseId == "");
                //searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.Authority) && !string.IsNullOrEmpty(criteria.AuthorityCaseId) && criteria.AuthorityCaseId != "{empty}", x => x.AuthorityCaseId == criteria.AuthorityCaseId);

                searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.NSAStatus), x => x.NSAStatus == criteria.NSAStatus);

                if (!string.IsNullOrEmpty(criteria.Authority) && criteria.Authority != "_") // "_" means to search for records where Authority is totally missing
                {
                    // only look at authority status if the authority was also supplied - could reconsider this in the future if we want to search across multiple authorities
                    searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.AuthorityStatus), x => x.AuthorityStatus == criteria.AuthorityStatus);
                    searchQ = searchQ.AddCondition(() => true, x => x.Authority == criteria.Authority);
                    // left join the CaseArchives if AuthorityCaseId was supplied
                    if (!string.IsNullOrEmpty(criteria.AuthorityCaseId) && criteria.AuthorityCaseId != "{empty}")
                    {
                        int tmpAuthorityId = await _context.Authorities.Where(d => d.Key == criteria.Authority).Select(d => d.Id).SingleOrDefaultAsync();
                        if (tmpAuthorityId > 0)
                        {
                            searchQ = from d in searchQ
                                      from a in _context.Set<CaseArchive>().Where(ac => d.Id == ac.ArbitrationCaseId && ac.AuthorityId == tmpAuthorityId && ac.AuthorityCaseId == criteria.AuthorityCaseId).DefaultIfEmpty() // <- left join
                                      where d.AuthorityCaseId == criteria.AuthorityCaseId
                                      select d;
                        }
                    }
                }
                else if (criteria.Authority == "_")
                {
                    searchQ = searchQ.AddCondition(() => true, x => x.Authority == "");
                }

                // more criteria
                searchQ = searchQ.AddCondition(() => criteria.ArbitrationBriefDueDate.HasValue && criteria.ArbitrationBriefDueDate.Value > DateTime.MinValue, x => x.ArbitrationBriefDueDate <= criteria.ArbitrationBriefDueDate!.Value);
                // allow searches for unassigned cases
                searchQ = searchQ.AddCondition(() => criteria.AssignedUser.ToLower().Equals("(unassigned)"), x => x.AssignedUser == "");
                searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.AssignedUser) && !criteria.AssignedUser.ToLower().Equals("(unassigned)"), x => x.AssignedUser == criteria.AssignedUser);

                searchQ = searchQ.AddCondition(() => criteria.AssignmentDeadlineDate.HasValue && criteria.AssignmentDeadlineDate.Value > DateTime.MinValue, x => x.AssignmentDeadlineDate.HasValue && x.AssignmentDeadlineDate.Value.Date == criteria.AssignmentDeadlineDate!.Value.Date);

                searchQ = searchQ.AddCondition(() => criteria.Customer == "{empty}", x => x.Customer == "");
                searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.Customer) && criteria.Customer != "{empty}", x => x.Customer == criteria.Customer);

                searchQ = searchQ.AddCondition(() => criteria.DOB.HasValue && criteria.DOB.Value > DateTime.MinValue, x => x.DOB == criteria.DOB!.Value);

                searchQ = searchQ.AddCondition(() => criteria.EHRNumber == "{empty}", x => x.EHRNumber == "");
                searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.EHRNumber) && criteria.EHRNumber != "{empty}", x => x.EHRNumber == criteria.EHRNumber);

                if (criteria.Payor == "{empty}")
                {
                    searchQ = searchQ.AddCondition(() => true, x => x.Payor == "" || x.PayorId == null);
                }
                else if (!string.IsNullOrEmpty(criteria.Payor))
                {
                    var payor = await _context.Payors.FirstOrDefaultAsync(d => d.Name == criteria.Payor);
                    //searchQ = searchQ.AddCondition(() => payor != null, x => x.PayorId == payor!.Id || x.PayorId == );
                    var parentId = payor == null ? -1 : payor.Id;
                    searchQ = from d in searchQ
                              from p in _context.Set<Payor>().Where(pa => pa.ParentId == parentId)
                              where d.PayorId == p.Id
                              select d;
                }
                if (!string.IsNullOrWhiteSpace(criteria.Service))
                {
                    searchQ = searchQ.AddCondition(() => true, x => x.Service == criteria.Service);
                }

                searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.PatientName) && criteria.PatientName != "{empty}", x => x.PatientName.Contains(criteria.PatientName));
                searchQ = searchQ.AddCondition(() => criteria.PatientName == "{empty}", x => x.PatientName == "");

                searchQ = searchQ.AddCondition(() => criteria.PayorClaimNumber == "{empty}", x => x.PayorClaimNumber == "");
                searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.PayorClaimNumber) && criteria.PayorClaimNumber != "{empty}", x => x.PayorClaimNumber == criteria.PayorClaimNumber);

                searchQ = searchQ.AddCondition(() => criteria.RequestDate.HasValue && criteria.RequestDate.Value > DateTime.MinValue, x => x.RequestDate == criteria.RequestDate!.Value);

                //searchQ = searchQ.Include(b => b.Benchmarks);  // ?? what was this intended for?

                // to include tracking data -> searchQ.Include(j => j.Tracking); // NOTE: Expensive!
                // var SQL = searchQ.ToQueryString(); use this for debugging until Visual Studio fixes the immediate window string truncation bug

                if (tmp != null)
                {
                    // filter by Arbitrator
                    cases = await (from d in searchQ
                                   from cc in _context.Set<CaseArbitrator>().Where(cc => d.Id == cc.ArbitrationCaseId)
                                   from a in _context.Set<Arbitrator>().Where(a => cc.ArbitratorId == a.Id && (a.Name.Contains(tmp.Name) || a.Phone.Contains(tmp.Phone) || a.Email.Contains(tmp.Email)))  // join the master arbitrator list so we can search their name and phone number
                                   select d).AsNoTracking().AsSplitQuery().ToArrayAsync();
                }
                else
                {
                    // include all attached Arbitrators

                    cases = await (from d in searchQ select d).Include(x => x.Arbitrators).AsNoTracking().AsSplitQuery().ToArrayAsync();
                }

                int countLimit = user.IsSystem ? 10000 : 2000;
                if (cases.Count() > countLimit)
                    return BadRequest($@"Your search would return over {countLimit} records. Please add more criteria such as Authority, Status, Customer or Payor.");

                if (cases.Count() > 0)
                {
                    foreach (var result in cases)
                    {
                        foreach (var arb in result.Arbitrators)
                        {
                            arb.Arbitrator = await _context.Arbitrators.FindAsync(arb.ArbitratorId);
                        }
                    }

                    return Ok(cases);
                }
                else
                {
                    return NoContent();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);
#if DEBUG
                return BadRequest(ex.Message);
#else
                return BadRequest("Search error. Contact support if this search continues to fail.");
#endif
            }
        }

        /// <summary>
        /// Create a new Case
        /// </summary>
        /// <param name="arbCase"></param>
        /// <returns></returns>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ArbitrationCase>> CreateCaseAsync([FromBody] ArbitrationCase arbCase)
        {
            // User permission checks
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            try
            {
                var allowedCustomerIDs = new List<int>();
                string[] allowedCustomerNames = new string[] { };
                if (!user.HasGlobalCaseRole)
                {
                    allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer && (x.AccessLevel == UserAccessType.manager || x.AccessLevel == UserAccessType.negotiator)).Select(x => x.EntityId));
                    allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                    if (allowedCustomerNames.Count() == 0)
                        return Unauthorized("Customer records are not available to the current account.");
                }

                if (user.HasGlobalCaseRole && !user.IsManager && !user.IsNegotiator)
                    return Unauthorized("Insufficient privileges to create a Case");
                else if (!user.HasGlobalCaseRole && !allowedCustomerNames.Contains(arbCase.Customer))
                    return Unauthorized("Insufficient privileges to create a Case");

                // Case Validation and Creation
                Authority? nsa = await _context.Authorities.FirstOrDefaultAsync(d => d.Key == "nsa");
                if (nsa == null)
                    return NotFound("The NSA authority record is missing!");

                Authority? au = string.IsNullOrEmpty(arbCase.Authority) ? null : await _context.Authorities.FirstOrDefaultAsync(d => d.Key == arbCase.Authority);

                var messages = Utilities.ValidateArbitrationCaseForUI(_context, arbCase, true, nsa, au, false, user);
                if (messages.Count > 0)
                    return BadRequest(messages);

                var name = user.Email;
                var update = Utilities.GetCurrentUtcDate();

                // Clear the negotiator entities in case they were included - these are linked using the PayorNegotiatorId value
                arbCase.PayorNegotiator = null;
                arbCase.PayorEntity = null;

                // validate Payor and Negotiator selections 
                if (arbCase.PayorId.HasValue && arbCase.PayorId.Value > 0)
                {
                    var payor = await _context.Payors.Include(d => d.Negotiators).FirstOrDefaultAsync(d => d.Id == arbCase.PayorId.Value);
                    if (payor == null)
                        return BadRequest("Invalid Payor");
                    if (arbCase.PayorNegotiatorId.HasValue && arbCase.PayorNegotiatorId.Value > 0)
                    {
                        if (payor.Negotiators.Count(d => d.Id == arbCase.PayorNegotiatorId) == 0)
                            return NotFound("Invalid Negotiator");
                    }
                    arbCase.Payor = payor.Name;
                }
                else
                {
                    arbCase.PayorNegotiatorId = null;  // can't have a negotiator without its parent
                }

                arbCase.CreatedBy = name;
                arbCase.CreatedOn = update;
                arbCase.UpdatedOn = update;
                arbCase.UpdatedBy = name;

                // Add initial CaseLog 
                var entry = new CaseLog
                {
                    Action = "APICreate", // TODO: Make this an enum throughout the app
                    CreatedBy = name,
                    CreatedOn = update,
                    Details = "New claim created",
                    Id = 0
                };
                arbCase.Log.Add(entry);

                // Insert CaseTracking
                if (arbCase.Tracking != null)
                {
                    // Insert new CaseTracking record
                    arbCase.Tracking = new CaseTracking
                    {
                        TrackingValues = arbCase.Tracking.TrackingValues,
                        UpdatedBy = name,
                        UpdatedOn = update
                    };
                }

                foreach (var c in arbCase.CPTCodes)
                {
                    c.UpdatedOn = update;
                    c.UpdatedBy = name;
                    c.Id = 0;
                }

                foreach (var a in arbCase.Arbitrators)
                {
                    a.UpdatedOn = update;
                    a.UpdatedBy = name;
                    a.Id = 0;
                }

                foreach (var n in arbCase.Notes)
                {
                    n.Id = 0;
                    n.UpdatedOn = update;
                    n.UpdatedBy = name;
                }

                foreach (var g in arbCase.Log)
                {
                    g.Id = 0;
                    g.CreatedOn = update;
                    g.CreatedBy = name;
                }

                foreach (var childModel in arbCase.OfferHistory)
                {
                    childModel.Id = 0;
                    childModel.UpdatedOn = update;
                    childModel.UpdatedBy = name;
                }

                foreach (var h in arbCase.CaseSettlements)
                {
                    h.Id = 0;
                    h.CreatedOn = update;
                    h.CreatedBy = name;
                    h.UpdatedBy = name;
                    h.UpdatedOn = update;

                    foreach (var s in h.CaseSettlementDetails)
                    {
                        s.Id = 0;
                        s.CreatedOn = update;
                        s.CreatedBy = name;
                        s.UpdatedOn = update;
                        s.UpdatedBy = name;
                    }
                }

                await _context.ArbitrationCases.AddAsync(arbCase);
                await _context.SaveChangesAsync();
                return Ok(arbCase);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                    msg += " " + ex.InnerException.Message;
                return BadRequest(msg);
            }
        }

        // PUT api/<CasesController>/5
        [HttpPut("{id}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ArbitrationCase>> UpdateCaseAsync(int id, [FromBody] ArbitrationCase arbCase)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (arbCase.IsDeleted)
                return BadRequest("This endpoint does not support deleting Cases");

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer && (x.AccessLevel == UserAccessType.manager || x.AccessLevel == UserAccessType.negotiator)).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }

            if (user.HasGlobalCaseRole && !user.IsManager && !user.IsNegotiator)
                return Unauthorized("Insufficient global privileges to update a Case");
            else if (!user.HasGlobalCaseRole && !allowedCustomerNames.Contains(arbCase.Customer))
                return Unauthorized("Insufficient granular privileges to update a Case");

            if (id < 1 || arbCase.Id < 1 || arbCase.Id != id)
                return BadRequest("Missing or invalid ID parameter");

            // Make sure this is not going to create a duplicate via Authority
            if (!string.IsNullOrEmpty(arbCase.AuthorityCaseId) &&
                arbCase.Status != ArbitrationStatus.Ineligible &&
                arbCase.Status != ArbitrationStatus.ClosedPaymentWithdrawn &&
                await _context.ArbitrationCases.FirstOrDefaultAsync(d => d.Id != arbCase.Id && d.IsDeleted == false && d.Authority.Equals(arbCase.Authority) && d.AuthorityCaseId.Equals(arbCase.AuthorityCaseId)) != null)
                return BadRequest("AuthorityCaseId already exists on another claim. Search for that record and update it or mark this record Ineligible or Withdrawn.");

            // Make sure this is not going to create a duplicate record via Payor Claim Number
            // story 2000 disabled this check
            //if (!string.IsNullOrEmpty(arbCase.PayorClaimNumber) && !arbCase.IsDeleted) 
            //{

            //    var test = await _context.ArbitrationCases.FirstOrDefaultAsync(d => d.Id != arbCase.Id && d.IsDeleted == false &&
            //                                                        d.Payor.Equals(arbCase.Payor) &&
            //                                                        d.PayorClaimNumber.Equals(arbCase.PayorClaimNumber));
            //    if (test != null)
            //        return BadRequest("The PayorClaimNumber already exists on another EHR Claim. Search for that record and update it.");
            //}

            var nsaAuthority = await _context.Authorities.AsNoTracking().Include(d => d.TrackingDetails.Where(g => !g.IsDeleted)).FirstOrDefaultAsync(d => d.Key == "nsa");
            if (nsaAuthority == null)
                return NotFound("NSA Authority configuration is missing!");

            var stateAuthority = string.IsNullOrEmpty(arbCase.Authority) ? null : await _context.Authorities.AsNoTracking().Include(d => d.TrackingDetails.Where(g => !g.IsDeleted)).FirstOrDefaultAsync(d => d.Key == arbCase.Authority);
            var messages = Utilities.ValidateArbitrationCaseForUI(_context, arbCase, true, nsaAuthority, stateAuthority, true, user);

            if (messages.Count == 1 && messages.Count(x => x.Contains("is on an exclusion list for Payor")) == 1)
            {
                //this exception we will skip
            }
            else if (messages.Count > 0)
            {
                return BadRequest(messages);
            }
            var name = user.Email;
            var update = Utilities.GetCurrentUtcDate();

            try
            {
                var orig = await _context.ArbitrationCases
                        .Include(c => c.Arbitrators)
                        //.Include(d => d.SettlementDetails.Where(g => !g.IsDeleted))
                        .Include(c => c.CPTCodes)
                        .Include(c => c.Tracking)
                        .Include(d => d.Notes)
                        .Include(d => d.OfferHistory)
                        .FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == arbCase.Id);

                if (orig == null)
                    return NotFound("No such record exists");

                if (!user.HasGlobalCaseRole && !allowedCustomerNames.Contains(orig.Customer))
                    return Unauthorized("Insufficient privileges to update the Case. Verify ownership of the record.");

                // See if the Authority info changed and, if so, if we're supposed to archive what is already there
                if (!string.IsNullOrEmpty(orig.AuthorityCaseId)
                    && (!orig.AuthorityCaseId.Equals(arbCase.AuthorityCaseId, StringComparison.CurrentCultureIgnoreCase) || !orig.Authority.Equals(arbCase.Authority, StringComparison.CurrentCultureIgnoreCase))
                    && arbCase.KeepAuthorityInfo)
                {
                    // upsert the CaseArchive info before overwriting what's on the orig record
                    var upmsg = await UpsertCaseArchive(orig, user);
                    if (!string.IsNullOrEmpty(upmsg))
                        return BadRequest(upmsg);

                    arbCase.KeepAuthorityInfo = false;
                }


                // null out the Payor and Negotiator objects in case they exist since we use the foreign keys for linking
                arbCase.PayorEntity = null;
                arbCase.PayorNegotiator = null;

                _context.Entry(orig).CurrentValues.SetValues(arbCase);

                if (_context.Entry(orig).State == EntityState.Modified)
                {
                    orig.UpdatedBy = name;
                    orig.UpdatedOn = update;
                }

                Utilities.ValidateAuthorityScheme(orig.NSATracking, nsaAuthority.TrackingDetails);

                // Update or Insert CaseTracking because it is a separate object, not a string like NSATracking

                // TODO: Need to unify the approach to tracking such that both NSA and Authority (even Texas)
                // both create the JSON strings saved into the CaseTracking table. This will permit multiple
                // tracking schedules to be linked to an AuthorityCase, not an ArbitrationCase (aka a "Claim")
                // Why? For tracking batches. See DevOps feature #1518

                if (arbCase.Tracking != null)
                {
                    if (orig.Tracking != null)
                    {
                        arbCase.Tracking.Id = orig.Tracking.Id;

                        // Update existing CaseTracking record
                        Utilities.ValidateAuthorityScheme(arbCase.Tracking.TrackingValues, stateAuthority!.TrackingDetails);

                        _context.Entry(orig.Tracking).CurrentValues.SetValues(arbCase.Tracking);
                        if (_context.Entry(orig.Tracking).State == EntityState.Modified) // ideally, this would do what it appears to do but EF is just broken when it comes to change detection
                        {
                            orig.Tracking.UpdatedBy = name;
                            orig.Tracking.UpdatedOn = update;
                        }
                    }
                    else
                    {
                        // Insert new Case Tracking record
                        orig.Tracking = new CaseTracking
                        {
                            TrackingValues = arbCase.Tracking.TrackingValues,
                            UpdatedBy = name,
                            UpdatedOn = update
                        };
                    }
                }

                // Update and Insert CPT
                foreach (var childModel in arbCase.CPTCodes.Where(d => !string.IsNullOrEmpty(d.CPTCode)).ToArray())
                {
                    var existingChild = orig.CPTCodes
                        .Where(c => c.Id > 0 && c.Id == childModel.Id)
                        .SingleOrDefault();

                    if (existingChild != null)
                    {
                        // Update existing CPT record
                        _context.Entry(existingChild).CurrentValues.SetValues(childModel);
                        if (_context.Entry(existingChild).State == EntityState.Modified) // ideally, this would do what it appears to do but EF is just broken when it comes to change detection
                        {
                            existingChild.UpdatedBy = name;
                            existingChild.UpdatedOn = update;
                        }
                    }
                    else
                    {
                        // Insert new CPT record
                        childModel.Id = 0;
                        childModel.UpdatedOn = update;
                        childModel.UpdatedBy = name;
                        orig.CPTCodes.Add(childModel);
                    }
                }

                // Update and Insert Arbitrators
                foreach (var childModel in arbCase.Arbitrators)
                {
                    var existingChild = orig.Arbitrators
                        .Where(c => c.Id > 0 && c.Id == childModel.Id)
                        .SingleOrDefault();

                    if (existingChild != null)
                    {
                        // Update existing Arbitrator record
                        _context.Entry(existingChild).CurrentValues.SetValues(childModel);
                        if (_context.Entry(existingChild).State == EntityState.Modified)
                        {
                            existingChild.UpdatedBy = name;
                            existingChild.UpdatedOn = update;
                        }
                    }
                    else
                    {
                        // Insert new Arbitrator record
                        childModel.Id = 0;
                        childModel.UpdatedOn = update;
                        childModel.UpdatedBy = name;
                        orig.Arbitrators.Add(childModel);
                    }
                }


                // Update and Insert OfferHistory

                // NOTE: Per DevOps task 1376, change this behavior: Only process OfferHistory if an offer wasn't already accepted.
                // The business now allows offer history to be rewritten. -with effect 6Jun2023
                //if (orig.OfferHistory.FirstOrDefault(x => x.WasOfferAccepted) == null)
                //{
                bool offerAccepted = false;
                foreach (var childModel in arbCase.OfferHistory)
                {
                    var existingChild = orig.OfferHistory
                        .Where(c => childModel.Id > 0 && c.Id == childModel.Id)
                        .SingleOrDefault();

                    if (existingChild != null && (childModel.OfferAmount != existingChild.OfferAmount || childModel.WasOfferAccepted != existingChild.WasOfferAccepted || childModel.Notes != existingChild.Notes))
                    {
                        // add a Note 
                        string details = $@"Updated {existingChild.OfferType} Offer of {existingChild.OfferAmount.ToString("C")}.";
                        if (childModel.OfferAmount != existingChild.OfferAmount)
                            details += $@" New Amount is {childModel.OfferAmount.ToString("C")}";
                        if (childModel.WasOfferAccepted && !existingChild.WasOfferAccepted)
                            details += " Offer Accepted.";
                        else if (!childModel.WasOfferAccepted && existingChild.WasOfferAccepted)
                            details += " Offer Revoked.";
                        if (!string.IsNullOrEmpty(childModel.Notes))
                            details += " Notes: " + childModel.Notes;

                        var nn = new Note { Details = details, Id = 0 };
                        arbCase.Notes.Add(nn);

                        // Update existing OfferHistory record
                        _context.Entry(existingChild).CurrentValues.SetValues(childModel);
                        if (_context.Entry(existingChild).State == EntityState.Modified) // ideally, this would do what it appears to do but EF is just broken when it comes to change detection
                        {
                            existingChild.UpdatedBy = name;
                            existingChild.UpdatedOn = update;
                        }
                        offerAccepted = offerAccepted || existingChild.WasOfferAccepted;

                    }
                    else if (existingChild == null)
                    {
                        // Insert new OfferHistory record
                        childModel.Id = 0;
                        childModel.UpdatedOn = update;
                        childModel.UpdatedBy = name;
                        orig.OfferHistory.Add(childModel);

                        // add a Note 
                        string details = $@"Added {childModel.OfferType} Offer of ${childModel.OfferAmount}.";
                        if (childModel.WasOfferAccepted)
                            details += " Offer Accepted.";
                        if (!string.IsNullOrEmpty(childModel.Notes))
                            details += " Note: " + childModel.Notes;
                        var nn = new Note { Details = details, Id = 0 };
                        arbCase.Notes.Add(nn);
                    }
                }

                // update the work flow status to "close" this case
                if (offerAccepted)
                {
                    orig.Status = orig.Status switch
                    {
                        ArbitrationStatus.ActiveArbitrationBriefCreated
                            or ArbitrationStatus.ActiveArbitrationBriefNeeded
                            or ArbitrationStatus.ActiveArbitrationBriefSubmitted
                            or ArbitrationStatus.PendingArbitration => ArbitrationStatus.SettledOutsidePendingPayment,
                        ArbitrationStatus.DetermineAuthority
                            or ArbitrationStatus.Ineligible
                            or ArbitrationStatus.InformalInProgress
                            or ArbitrationStatus.New
                            or ArbitrationStatus.Open => ArbitrationStatus.SettledInformalPendingPayment,
                        _ => orig.Status
                    };

                    orig.NSAWorkflowStatus = orig.NSAWorkflowStatus switch
                    {
                        ArbitrationStatus.ActiveArbitrationBriefCreated
                            or ArbitrationStatus.ActiveArbitrationBriefNeeded
                            or ArbitrationStatus.ActiveArbitrationBriefSubmitted
                            or ArbitrationStatus.PendingArbitration => ArbitrationStatus.SettledOutsidePendingPayment,
                        ArbitrationStatus.DetermineAuthority
                            or ArbitrationStatus.Ineligible
                            or ArbitrationStatus.InformalInProgress
                            or ArbitrationStatus.New
                            or ArbitrationStatus.Open => ArbitrationStatus.SettledInformalPendingPayment,
                        _ => orig.NSAWorkflowStatus
                    };
                }
                //}

                // Insert any new CaseLog entries - existing CaseLog entries are ignored because they cannot be modified
                foreach (var childModel in arbCase.Log.Where(x => x.Id == 0))
                {
                    // Insert new Log record
                    childModel.CreatedOn = update;
                    childModel.CreatedBy = name;
                    orig.Log.Add(childModel);
                }

                // Insert any new Notes entry - existing Notes entries are ignored because they cannot be modified
                foreach (var childModel in arbCase.Notes.Where(d => d.Id == 0))
                {
                    // Insert new Log record
                    childModel.Id = 0;
                    childModel.UpdatedOn = update;
                    childModel.UpdatedBy = name;
                    orig.Notes.Add(childModel);
                }

                bool isChanged = Utilities.FixStateArbitrationCaseDates(arbCase);

                await _context.SaveChangesAsync();
                orig.Log = new List<CaseLog>(); // removes log entries for speed - it can get big!
                return Ok(orig);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // work flow endpoints
        [HttpPost("wf")]
        public async Task<ActionResult> DoWorkflow([FromBody] CaseWorkflowParams p)
        {
            if (p.caseId < 1)
                return BadRequest("Invalid parameters");

            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (user.HasGlobalCaseRole && !user.IsManager && !user.IsNegotiator && !user.IsSystem)
                return Unauthorized("Insufficient privileges for current user context");

            if (user.IsSystem && p.action != CaseWorkflowAction.NotificationFailed && p.action != CaseWorkflowAction.NSARequestSentToPayor)
                return Unauthorized("Action denied to current user context");

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole && !user.IsSystem)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer && (x.AccessLevel == UserAccessType.manager || x.AccessLevel == UserAccessType.negotiator)).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("No Customer records available to the current account.");
            }

            var orig = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == p.caseId);
            if (orig == null)
                return NotFound(p.caseId);

            if (!user.HasGlobalCaseRole && !user.IsSystem && !allowedCustomerNames.Contains(orig.Customer))
                return Unauthorized("Insufficient granular privileges for current user context");

            if (p.action == CaseWorkflowAction.MarkRead)
            {
                orig.IsUnread = false;
            }
            else if (p.action == CaseWorkflowAction.MarkUnread)
            {
                orig.IsUnread = true;
            }
            else if (p.action == CaseWorkflowAction.AssignUser)
            {
                if (p.assignToId < 0)
                    return BadRequest("Invalid user assignment");

                // requesting user must be in manager role
                bool isManager = user.IsManager || user.AllAppRoles.FirstOrDefault(d => d.RoleType == UserRoleType.Customer && d.AccessLevel == UserAccessType.manager && allowedCustomerIDs.Contains(d.EntityId)) != null;
                bool isNegotiator = user.IsNegotiator || user.AllAppRoles.FirstOrDefault(d => d.RoleType == UserRoleType.Customer && d.AccessLevel == UserAccessType.negotiator && allowedCustomerIDs.Contains(d.EntityId)) != null;
                if (!isManager && !isNegotiator)
                    return Unauthorized("Only Managers and Negotiators can assign Cases to Users");

                string email = string.Empty;
                if (p.assignToId > 0)
                {
                    AppUser? u = await _context.AppUsers.FirstOrDefaultAsync(d => d.IsActive && d.Id == p.assignToId);
                    if (u == null)
                        return NotFound("Target user not found or is marked inactive");
                    email = u.Email.ToLower();
                }

                orig.AssignedUser = email;
            }
            else if (p.action == CaseWorkflowAction.AssignCustomer)
            {
                if (p.customerId <= 0)
                    return BadRequest("Invalid customer assignment");

                if (!string.IsNullOrEmpty(orig.Customer))
                    return BadRequest("The Case is already assigned to a Customer");

                // requesting user must be in global manager role
                bool isManager = user.IsManager; // || user.AllAppRoles.FirstOrDefault(d => d.RoleType == UserRoleType.Customer && d.AccessLevel == UserAccessType.manager && allowedCustomerIDs.Contains(d.EntityId)) != null;
                if (!isManager)
                    return Unauthorized("Only Managers can assign a Customer to a Case");

                var u = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == p.customerId);
                if (u == null)
                    return NotFound("Invalid Customer Id");

                orig.Customer = u.Name;
            }
            else if (p.action == CaseWorkflowAction.NSARequestSentToPayor)
            {
                if (!user.IsSystem)
                    return Unauthorized($@"Only the system account can update notification statuses.");

                var nsaAuth = await _context.Authorities.AsNoTracking().Include(d => d.TrackingDetails.Where(g => !g.IsDeleted)).FirstOrDefaultAsync(h => h.Key == "nsa");
                if (nsaAuth == null)
                    return BadRequest("Could not find NSA configuration information");

                var notification = await _context.Notifications.FirstOrDefaultAsync(d => !d.IsDeleted &&
                                                                              d.Id == p.assignToId && // overloaded
                                                                              d.ArbitrationCaseId == p.caseId &&
                                                                              d.NotificationType == NotificationType.NSANegotiationRequest &&
                                                                              d.SentOn == null &&
                                                                              d.Status == "pending");
                if (notification == null)
                    return NotFound("No active Notifications found for the given criteria.");

                var sync = DateTime.Now;
                if (notification.JSON == "")
                    notification.JSON = "{}";

                var wfJson = JsonNode.Parse(string.IsNullOrEmpty(p.JSON) ? "{}" : p.JSON);
                if (wfJson!["attachments"] is JsonArray == false)
                {
                    wfJson["attachments"] = new JsonArray();
                }

                var goodies = JsonNode.Parse(string.IsNullOrEmpty(notification.JSON) ? "{}" : notification.JSON);
                if (goodies == null)
                    goodies = JsonNode.Parse("{}");

                try
                {
                    DateTime? edate = null;
                    goodies!["delivery"] = new JsonObject();
                    goodies!["delivery"]!["deliveredOn"] = edate;
                    goodies!["delivery"]!["deliveryId"] = "";
                    goodies!["delivery"]!["deliveryMethod"] = "";
                    goodies!["delivery"]!["message"] = "";
                    goodies!["delivery"]!["messageId"] = wfJson["messageId"]!.ToString();
                    goodies!["delivery"]!["processedOn"] = sync;
                    goodies!["delivery"]!["sender"] = wfJson["sender"]!.ToString();
                    goodies!["delivery"]!["status"] = "queued";

                    var node = JsonNode.Parse(wfJson["attachments"]!.ToJsonString());
                    if (goodies!["delivery"]!.AsObject().TryAdd("attachments", node))
                    {
                        // add attachment entries to the EMRAttachments table
                        var att = goodies!["delivery"]!["attachments"]!.AsArray();
                        foreach (var att2 in att)
                        {
                            string fn = att2!.AsObject()["fileName"]!.ToString();
                            _context.EMRClaimAttachments.Add(new EMRClaimAttachment()
                            {
                                ArbitrationCaseId = p.caseId,
                                BLOBLink = $@"{_containerClient.Uri.AbsoluteUri}/{fn}",
                                BLOBName = fn,
                                CreatedBy = user.Email,
                                CreatedOn = DateTime.UtcNow,
                                DocType = Enum.GetName(CaseDocumentType.NSARequestAttachment),
                                UpdatedBy = user.Email,
                                UpdatedOn = DateTime.UtcNow
                            });
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding delivery info to sent Notification");
                }

                notification.JSON = goodies!.ToJsonString(); // JsonSerializer.Serialize<Dictionary<string,object>>(goodies);
                notification.SentOn = sync; // NOTE: Sent to the delivery service - redundant with the JSON for the moment - needed for easier PowerBI reporting or the like
                notification.Status = "success"; // Successfully processed by the daemon - more granular detail in the JSON values
                notification.UpdatedBy = user.Email;
                notification.UpdatedOn = sync;

                if (orig.NSAStatus.Equals("pending nsa negotiation request", StringComparison.CurrentCultureIgnoreCase))
                {
                    orig.NSAStatus = "Submitted NSA Negotiation Request";
                    orig.UpdatedBy = user.Email;
                    orig.UpdatedOn = sync;
                    if (nsaAuth.TrackingDetails.Count > 0)
                        orig.NSATracking = Utilities.SetTrackingValue(Utilities.GetCurrentUtcDate(), nsaAuth.TrackingDetails, orig.NSATracking, "DateNegotiationSent", orig);

                    // add a change tracking record
                    var childModel = new CaseLog();
                    childModel.Id = 0;
                    childModel.Action = "MPNotifyNSASent";  // 20 chars max!
                    childModel.CreatedOn = sync;
                    childModel.CreatedBy = user.Email;
                    childModel.Details = @"{""NSAStatus"":""Submitted NSA Negotiation Request""}";
                    orig.Log.Add(childModel);
                }
            }
            else if (p.action == CaseWorkflowAction.NotificationFailed)
            {
                if (!user.IsSystem)
                    return Unauthorized($@"Only the system account can update notification statuses.");

                var n = await _context.Notifications.FirstOrDefaultAsync(d => d.Id == p.assignToId && // overloaded
                                                                              !d.IsDeleted &&
                                                                              d.ArbitrationCaseId == p.caseId &&
                                                                              d.NotificationType == NotificationType.NSANegotiationRequest &&
                                                                              d.SentOn == null &&
                                                                              d.Status == null || d.Status == "pending");
                if (n == null)
                    return BadRequest("No active Notification matched the parameters");

                var sync = Utilities.GetCurrentUtcDate();
                if (n.JSON == "")
                    n.JSON = "{}";

                Dictionary<string, object>? goodies = null;

                try
                {
                    goodies = JsonSerializer.Deserialize<Dictionary<string, object>>(n.JSON);
                }
                catch
                {
                    goodies = new Dictionary<string, object>();
                }

                // NOTE: "Processed" means picked up by the MPNotify daemon, analyzed, and queued to a delivery service or rejected due to validation issues.
                // This does not mean delivered to the final recipient. See notes in the Notification class that expand on this.

                if (goodies == null)
                    goodies = new Dictionary<string, object>();

                if (goodies.ContainsKey("delivery"))
                    goodies.Remove("delivery");

                DateTime? edate = null;
                goodies.Add("delivery", new
                {
                    deliveredOn = edate, // TODO: The delivery service daemon will be responsible for updating these attributes after delivered/failed
                    deliveryId = string.Empty,
                    deliveryMethod = string.Empty,
                    message = p.message,
                    processedOn = sync,  // when the Notification was processed by daemon 
                    status = "failed"
                });

                n.JSON = JsonSerializer.Serialize<Dictionary<string, object>>(goodies);
                n.Status = "failed"; // Not successfully processed by the daemon - more granular detail in the JSON
                n.UpdatedBy = user.Email;
                n.UpdatedOn = sync;

                // Ye old way
                // var processedOn = n.UpdatedOn?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                // n.JSON = $@"{{""delivery"":{{""deliveredOn"": null,""deliveryId"":"""",""deliveryMethod"":"""",""message"":""{p.message}"",""processedOn"":""{processedOn}"",""status"":""failed""}} }}";
            }
            else
            {
                return BadRequest("Unexpected DoWorkflow error");
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return Problem(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }

        }

        [HttpPost]
        [Route("blob")]
        public async Task<ActionResult<string>> AttachFileAsync([FromQuery] int id, [FromQuery] string? cdt, [FromQuery] string? docType, [FromForm] IFormFile file)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (file == null)
                return BadRequest("No content detected");

            if (string.IsNullOrEmpty(docType) && string.IsNullOrEmpty(cdt))
                return BadRequest("Missing Case Document Type");

            if (string.IsNullOrEmpty(docType) && !string.IsNullOrEmpty(cdt))
            {
                docType = cdt.Trim();
            }

            if (!file.FileName.ToLower().EndsWith(".pdf"))
                return BadRequest("Invalid file type");

            if (file != null && file.Length > 30000000)
                return BadRequest("File size is too large. Split into multiple uploads or contact support.");

            if (id < 1)
                return BadRequest("Invalid case identifier");


            if (Enum.TryParse<CaseDocumentType>(docType, true, out CaseDocumentType parseResult) == false)
                return BadRequest("Unsupported document metadata");

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer && (x.AccessLevel == UserAccessType.manager || x.AccessLevel == UserAccessType.negotiator)).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }

            if (user.HasGlobalCaseRole && !user.IsManager && !user.IsNegotiator && !user.IsSystem)
                return Unauthorized("Insufficient privileges to attach Files to a Case");

            var arb = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == id && (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)));
            if (arb == null)
                return BadRequest("Record not found or unauthorized to attach Files to this Case");

            var uploadedOn = Utilities.GetCurrentUtcDate();
            var uploadedBy = GetUsername();
            var log = new StringBuilder($@"{uploadedOn:G}: Attaching file to case id {id}.");
            string blobURL = "";

            try
            {
                using (var reader = file!.OpenReadStream())
                {
                    string blobName = $@"{id}-{docType}-{file.FileName.ToLower()}";
                    try
                    {
                        var blob = _containerClient.GetBlobClient(blobName);

                        _logger.LogInformation($@"Attempting to upload file {blobName} to BLOB store...");
                        var response = await blob.UploadAsync(reader, true);
                        if (response.GetRawResponse().ReasonPhrase != "Created")
                            throw new Exception("Unexpected result from BLOB upload");

                        // add tags to new BLOB
                        var tags = new Dictionary<string, string>();
                        if (!string.IsNullOrEmpty(arb.AuthorityCaseId))
                            tags.Add("AuthorityCaseId", arb.AuthorityCaseId);
                        tags.Add("Id", id.ToString());
                        tags.Add("UpdatedBy", uploadedBy);
                        tags.Add("DocumentType", docType.ToLower());
                        if (!string.IsNullOrEmpty(arb.EHRNumber))
                            tags.Add("EHRNumber", arb.EHRNumber);

                        await blob.SetTagsAsync(tags);

                        blobURL = $@"{_containerClient.Uri.ToString()}/{blobName}";
                        _logger.LogInformation($@"BLOB uploaded to {blobURL}");
                        try
                        {
                            // save entry into database to facilitate reporting on claims that do not have certain attachments per Megan R. request 2023-9-28
                                var emrFile = await _context.EMRClaimAttachments.FirstOrDefaultAsync(d => !d.IsDeleted && d.ArbitrationCaseId == arb.Id && EF.Functions.Like(d.DocType, docType) && d.BLOBName == blobName);
                            if (emrFile != null)
                            {
                                // update existing entry - records could easily get purged in Azure and de sync these records so...
                                emrFile.UpdatedOn = Utilities.GetCurrentUtcDate();
                                emrFile.UpdatedBy = uploadedBy;
                            }
                            else
                            {
                                var uploadDate = Utilities.GetCurrentUtcDate();
                                emrFile = new EMRClaimAttachment { BLOBLink = blobURL, ArbitrationCaseId = arb.Id, BLOBName = blobName, CreatedBy = uploadedBy, CreatedOn = uploadDate, DocType = docType, UpdatedBy = uploadedBy, UpdatedOn = uploadDate };
                                await _context.EMRClaimAttachments.AddAsync(emrFile);
                            }

                            await _context.SaveChangesAsync();
                        }
                        catch (SqlException ex)
                        {
                            var message = $"File uploaded to Blob Storage, Unable attach file to EMRClaimAttachment for ArbitId: {id}; Filename: {blobName} " + ex.Message;
                            _logger.LogError(ex.Message);
                            return StatusCode(StatusCodes.Status500InternalServerError, new { message = message });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Unable to write to BLOB storage. " + ex.Message);
                        _logger.LogError(ex.Message);
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
                    }
                }

                log.AppendLine($@"Uploaded by {uploadedBy}");

                return Ok(blobURL);
            }
            catch (Exception ex)
            {
                log.AppendLine(ex.Message);
                log.Append(ex.StackTrace);
#if DEBUG
                return BadRequest(ex.Message);
#else
                return BadRequest("Unable to process file upload or connect to BLOB store!");
#endif
            }
        }

        [HttpGet]
        [Route("blob")]
        public async Task<IActionResult> ViewFile([FromQuery] int id, [FromQuery] string name)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (string.IsNullOrEmpty(name))
                return BadRequest("Empty document name");

            var n = name.ToLower();
            if (!name.StartsWith($@"{id}-") || !(n.EndsWith(".pdf") || n.EndsWith(".tif") || n.EndsWith(".tiff")))
                return BadRequest("Invalid document name");

            ArbitrationCase? orig = null;

            if (!user.HasGlobalCaseRole && !user.IsSystem)
            {
                var allowedCustomerIDs = new List<int>();
                string[] allowedCustomerNames = new string[] { };
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
                orig = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == id && allowedCustomerNames.Contains(d.Customer));
            }
            else
            {
                orig = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == id);
            }

            if (orig == null)
                return NotFound("Record not found or unauthorized to view Files for the Case");

            var v = name.ToLower().Split('.');
            var mt = v[v.Length - 1];
            string mimeType = "application/pdf";
            if (mt == "tif" || mt == "tiff")
                mimeType = "image/tiff";

            try
            {
                BlobClient blob = _containerClient.GetBlobClient(name);
                var result = await blob.DownloadContentAsync();
                Stream stream = result.Value.Content.ToStream();

                var r = new FileStreamResult(stream, mimeType)
                {
                    FileDownloadName = name
                };
                return r;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);
                _logger.LogError($@"Error retrieving {name} from BLOB storage");
#if DEBUG
                _logger.LogError(ex.Message);
#endif
                return BadRequest($@"Error retrieving {name} from storage");
            }

        }

        [HttpDelete]
        public async Task<ActionResult<bool>> DeleteClaimAsync([FromQuery] int id)
        {
            var user = await GetCurrentUser();
            if (user == null || !user.IsManager)
                return Unauthorized("Only Managers can delete an ArbitrationCase.");
            try
            {
                var orig = await _context.ArbitrationCases
                    .Include(d => d.Arbitrators)
                    .Include(d => d.CPTCodes.Where(g => !g.isDeleted))
                    .Include(d => d.Notifications.Where(g => !g.IsDeleted))
                    .Include(d => d.OfferHistory)
                    .Include(d => d.SettlementDetails.Where(g => !g.IsDeleted))
                    .FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == id);

                if (orig == null)
                    return NotFound("Record not found or already deleted");
                if (orig.Notifications.Count() > 0)
                    return BadRequest("Cannot delete a record with a Notification attached to it.");

                var updated = Utilities.GetCurrentUtcDate();
                var email = user.Email;

                orig.IsDeleted = true;
                orig.UpdatedOn = updated;
                orig.UpdatedBy = email;

                foreach (var a in orig.Arbitrators)
                    _context.CaseArbitrators.Remove(a);
                foreach (var p in orig.CPTCodes)
                {
                    p.isDeleted = true;
                    p.UpdatedOn = updated;
                    p.UpdatedBy = email;
                }
                foreach (var h in orig.OfferHistory)
                    _context.OfferHistory.Remove(h);
                foreach (var p in orig.SettlementDetails)
                {
                    p.IsDeleted = true;
                    p.UpdatedOn = updated;
                    p.UpdatedBy = email;
                }
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("blob")]
        public async Task<IActionResult> DeleteFileAsync([FromQuery] int id, [FromQuery] string name)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var allowedCustomerIDs = new List<int>();
            string[] allowedCustomerNames = new string[] { };
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer && (x.AccessLevel == UserAccessType.manager || x.AccessLevel == UserAccessType.negotiator)).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }

            if (user.HasGlobalCaseRole && !user.IsManager && !user.IsNegotiator)
                return Unauthorized("Insufficient privileges to update a Case");

            if (string.IsNullOrEmpty(name))
                return BadRequest("Empty document name");

            // find Case and verify permissions
            var orig = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == id && (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)));
            if (orig == null)
                return NotFound("Record not found or unauthorized to delete Files from this Case");

            var n = name.ToLower();
            if (!name.StartsWith($@"{id}-") || !(n.EndsWith(".pdf") || n.EndsWith(".tif") || n.EndsWith(".tiff")))
                return BadRequest("Invalid document name");

            try
            {
                BlobClient blob = _containerClient.GetBlobClient(name);
                var result = await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
                if (result)
                {
                    try
                    {
                        var emrFile = await _context.EMRClaimAttachments.FirstOrDefaultAsync(d => !d.IsDeleted && d.ArbitrationCaseId == orig.Id && d.BLOBName == name);
                        if (emrFile != null)
                        {
                            emrFile.IsDeleted = true;
                            emrFile.UpdatedOn = Utilities.GetCurrentUtcDate();
                            emrFile.UpdatedBy = user.Email;
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message, ex);
                        // swallow error - the EMRClaimAttachments list can never be definitive due to cloud storage so why worry about EF error after BLOB was removed
                    }
                    return Ok();
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);
                _logger.LogError($@"Error deleting {name} from BLOB storage");
#if DEBUG
                _logger.LogError(ex.Message);
#endif
                return BadRequest($@"Error deleting {name} from storage");
            }

        }

        [HttpGet]
        [Route("codes/{id}")]
        public async Task<ActionResult<IEnumerable<ProcedureCode>>> GetCaseCodes(int id)
        {
            if (id < 1)
                return BadRequest("Invalid parameter");
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            try
            {
                var q = from d in _context.Set<ArbitrationCase>().Where(d => !d.IsDeleted && d.Id == id)
                        from k in _context.Set<ClaimCPT>().Where(k => k.ArbitrationCaseId == d.Id && !k.isDeleted)
                        join p in _context.Set<ProcedureCode>() on k.CPTCode equals p.Code
                        select p;

                return Ok(await q.AsNoTracking().ToArrayAsync());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("files/{id}")]
        public async Task<ActionResult<IEnumerable<CaseFile>>> GetCaseFiles(int id, [FromQuery] string? docType = null)
        {
            if (id < 1)
                return BadRequest("Invalid parameter");

            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            ArbitrationCase? orig = null;

            if (!user.HasGlobalCaseRole && !user.IsSystem)
            {
                var allowedCustomerIDs = new List<int>();
                string[] allowedCustomerNames = new string[] { };
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
                orig = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == id && allowedCustomerNames.Contains(d.Customer));
            }
            else
            {
                orig = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == id);
            }

            if (orig == null)
                return NotFound("Record not found or unauthorized to view Files for the Case");

            if (docType == null)
                docType = "";

            try
            {
                var dt = docType.ToLower();
                var cf = await Utilities.GetBlobLinksAsync(_containerClient, "Id", id, dt);
                return Ok(cf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);
                _logger.LogError($@"Error retrieving files for ArbitrationCase {id}");
#if DEBUG
                _logger.LogError(ex.Message);
#endif
                return BadRequest($@"Error retrieving files for ArbitrationCase {id}");
            }
        }


        [HttpPost]
        [Route("files")]
        public async Task<ActionResult<IEnumerable<EMRClaimAttachment>>> GetFileListForMultipleClaimsAsync([FromBody] int[] values)
        {
            if (values.Length == 0)
                return BadRequest("Invalid parameter");

            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            int[]? claims = null;
            try
            {
                if (!user.HasGlobalCaseRole && !user.IsSystem)
                {
                    var allowedCustomerIDs = new List<int>();
                    string[] allowedCustomerNames = new string[] { };
                    allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                    allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                    if (allowedCustomerNames.Count() == 0)
                        return Unauthorized("Customer records are not available to the current account.");
                    claims = await _context.ArbitrationCases.Where(d => !d.IsDeleted && values.Contains(d.Id) && allowedCustomerNames.Contains(d.Customer)).Select(d => d.Id).ToArrayAsync();
                }
                else
                {
                    claims = await _context.ArbitrationCases.Where(d => !d.IsDeleted && values.Contains(d.Id)).Select(d => d.Id).ToArrayAsync();
                }

                if (claims == null || claims.Length == 0)
                    return Ok(new List<EMRClaimAttachment>());

                return Ok(await _context.EMRClaimAttachments.AsNoTracking().Where(d => claims.Contains(d.ArbitrationCaseId)).ToArrayAsync());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("log/{id}")]
        public async Task<ActionResult<IEnumerable<CaseLog>>> GetCaseLogs(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            ArbitrationCase? orig = null;

            if (!user.HasGlobalCaseRole)
            {
                var allowedCustomerIDs = new List<int>();
                string[] allowedCustomerNames = new string[] { };
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
                orig = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == id && allowedCustomerNames.Contains(d.Customer));
            }
            else
            {
                orig = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == id);
            }

            if (orig == null)
                return NotFound("Record not found or unauthorized to view Case information");

            try
            {
                return await _context.CaseLog.Where(d => d.ArbitrationCaseId == id).ToListAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<string> UpsertCaseArchive(ArbitrationCase orig, AppUser user)
        {
            if (Utilities.IsActiveStateCase(orig))
                return "Cannot archive a case with an active Authority Status. You may need to reload the record to make changes.";
            if (Utilities.IsActiveWorkflow(orig.Status))
                return "Cannot archive a case with an active work flow Status. You may need to reload the record to make changes.";

            if (string.IsNullOrEmpty(orig.AuthorityCaseId))
                return "";

            if (string.IsNullOrEmpty(orig.Authority))
                return "Cannot archive an Authority case without an assigned Authority.";

            var auth = await _context.Authorities.FirstOrDefaultAsync(d => d.Key == orig.Authority);
            if (auth == null)
                return "Cannot find an Authority record for key " + orig.Authority;

            var caseArchive = await _context.CaseArchives.AsNoTracking().FirstOrDefaultAsync(d => d.AuthorityCaseId == orig.AuthorityCaseId && d.AuthorityId == auth.Id);
            if (caseArchive != null)
            {
                if (caseArchive.ArbitrationCaseId != orig.Id)
                    return $@"An archive for {orig.Authority}-{orig.AuthorityCaseId} already exists but is attached to record id {caseArchive.ArbitrationCaseId}!";

                // TODO: Overwrite old archive data with new? Not sure how we'd have this scenario so I'll wait to see if the need arises
            }
            else
            {
                try
                {
                    var archive = new CaseArchive
                    {
                        ArbitrationCaseId = orig.Id,
                        AuthorityCaseId = orig.AuthorityCaseId,
                        AuthorityId = auth.Id,
                        AuthorityStatus = orig.AuthorityStatus,
                        AuthorityWorkflowStatus = orig.Status,
                        CreatedBy = user.Email,
                        CreatedOn = Utilities.GetCurrentUtcDate(),
                        Id = 0
                    };

                    string json = $@"{{""IneligibilityAction"":""{orig.IneligibilityAction}"",""IneligibilityReasons"":""{orig.IneligibilityReasons}"""; // }}";
                    if (orig.Tracking != null && !string.IsNullOrEmpty(orig.Tracking.TrackingValues))
                        json += ",tracking:{" + orig.Tracking.TrackingValues + "}";
                    json += "}";
                    archive.JSON = json;

                    // capture any Notes or Rejection / Ineligible info before clearing
                    _context.CaseArchives.Add(archive);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return "Unable to archive authority case info:" + ex.Message;
                }
            }
            return "";
        }

        /* Disable merging routines for now. This shouldn't be needed again if the db design is holding up.
        [HttpPost("merge")]
        public async Task<ActionResult<List<ArbitrationCase>>> MergeClaimsAsync(ArbitrationCase claim)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            if (!user.IsAdmin && !user.IsSystem)
                return Unauthorized("Method reserved for administrators");

            var nsa = await GetNSA();
            if (nsa == null)
                return BadRequest("Unable to locate NSA configuration");

            // merges all claims w/same encounter keys
            var mergeResult = await Utilities.MergeCaseDataAsync(_context, _synchronizer, user, claim, nsa);

            if (!string.IsNullOrEmpty(mergeResult.Message))
                return BadRequest(mergeResult.Message);
            else if (mergeResult.MergedRecord == null)
                return NotFound("Criteria did not match an ArbitrationCase record");

            return OK(mergeResult.MergedRecord);
        }

        [HttpGet("MergeAll")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MergeAllDuplicatesAsync()
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Problem("No active User context!");
            if (!user.IsAdmin && !user.IsSystem)
                return Unauthorized("Method reserved for administrators");


            try
            {
                var nsa = await GetNSA();
                if (nsa == null)
                    return BadRequest("Unable to locate NSA configuration");
                var recsQ = _context.Set<CaseUtility>().FromSqlRaw<CaseUtility>($@";with Duplicates(PatientName,ServiceDate,ProviderNPI)
             AS
             (
                 select PatientName,format(ServiceDate,'yyyy-MM-dd') as ServiceDate,ProviderNPI 
                 from dbo.ArbitrationCases a
                 where IsDeleted = 0 and
                 concatenate(PatientName, '|', format(ServiceDate, 'yyyy-MM-dd'), '|', ProviderNPI) IN
                 (
                    select concatenate(PatientName, '|', format(ServiceDate, 'yyyy-MM-dd'), '|', ProviderNPI)
                    from dbo.ArbitrationCases
                    where IsDeleted = 0 and Customer<>''
                    group by concatenate(PatientName, '|', format(ServiceDate, 'yyyy-MM-dd'), '|', ProviderNPI)
                    having count(*) > 1
                 )
              )
            SELECT ROW_NUMBER() OVER(ORDER BY concatenate(PatientName, '|', ServiceDate, '|', ProviderNPI)) as Id, NULL as DOB, '' as PayorClaimNumber, 
            '' as Payor, NULL as PayorId, PatientName, convert(DateTime,ServiceDate) as ServiceDate, ProviderNPI
            FROM(select distinct PatientName, ServiceDate, ProviderNPI FROM Duplicates) x
            order by PatientName, ServiceDate, ProviderNPI");
                var claims = await recsQ.ToArrayAsync();
                bool noRecords = claims.Count() == 0;
                var log = new StringBuilder();
                log.AppendLine($@"Began run: {DateTime.Now.ToShortTimeString()}");

                foreach (var baseline in claims)
                {
                    // merges all claims w/same encounter keys
                    var mergeResult = await Utilities.MergeCaseDataAsync(_context, _synchronizer, user, baseline, nsa);
                    if (string.IsNullOrEmpty(mergeResult.Message))
                        mergeResult.Message = "OK";
                    log.AppendLine($@"{baseline.PatientName} | {baseline.ServiceDate} | {baseline.ProviderNPI}: {mergeResult.Message}");
                }
                log.AppendLine($@"End run: {DateTime.Now.ToShortTimeString()}");
                return OK(log.ToString());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        */

        #region Lookup methods to support Batch preparation among other things - moved to BatchingController
        // Moved to BatchingController.cs
        #endregion
    }
}
