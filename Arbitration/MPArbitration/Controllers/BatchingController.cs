using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using MPArbitration.Model;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using System.Linq;
using System.Net.Mime;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Extensions = MPArbitration.Model.Extensions;
using ObjectsComparator.Comparator.Helpers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MPArbitration.Utility;

namespace MPArbitration.Controllers
{
    /// <summary>
    /// This class applies batching rules for the given Authority anytime it returns data.
    /// This is a quick way to start capturing (prototyping) rules that will eventually make it into
    /// a Factory pattern or other more scalable middle tier framework.
    /// </summary>

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BatchingController : MPBaseController
    {
        private readonly ILogger<BatchingController> _logger;
        private readonly ArbitrationStatus[] ACTIVE_WORKFLOW_STATES = { ArbitrationStatus.ActiveArbitrationBriefCreated, ArbitrationStatus.ActiveArbitrationBriefNeeded, ArbitrationStatus.ActiveArbitrationBriefSubmitted, ArbitrationStatus.PendingArbitration, ArbitrationStatus.InformalInProgress };

        public BatchingController(ILogger<BatchingController> logger, ArbitrationDbContext context, IConfiguration configuration) : base(context, configuration)
        {
            _logger = logger;
        }

        #region GET Routes
        /// <summary>
        /// Returns a list of Customers with active ArbitrationCase records that are at the appropriate Authority status
        /// </summary>
        /// <param name="a">Authority Id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<AuthorityDispute>> GetDisputeAsync(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.HasGlobalCaseRole)
                return Unauthorized("Only global data access roles are currently supported");

            /* TODO: Enforce Customer-level access if needed
            var allowedCustomerIDs = new List<int>();
            List<string> allowedCustomerNames = new List<string>();
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }
            */
            try
            {
                var dispute = await _context.AuthorityDisputes.AsNoTracking().AsSplitQuery()
                            .Include(d => d.Arbitrator)
                            .Include(d => d.Attachments.Where(b => !b.IsDeleted))
                            .Include(d => d.Authority).ThenInclude(c => c!.TrackingDetails.Where(m => !m.IsDeleted))
                            .Include(d => d.Fees)
                            .Include(d => d.DisputeCPTs).ThenInclude(b => b.ClaimCPT)
                            .Include(d => d.Notes)
                            .Where(d => d.Id == id) //&& (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)))
                            .FirstOrDefaultAsync();
                
                if(dispute == null)
                    return NotFound();
                
                
                foreach (var cpt in dispute.DisputeCPTs)
                {
                    var claim = await GetClaimForDisputeCPTAsync(cpt);

                    if (claim == null)
                        return BadRequest("One of the referenced ArbitrationCase records for the AuthorityDispute is no longer available!");

                    dispute.CPTViewmodels.Add(CreateDisputeCPTVM(dispute.Authority!, cpt, claim));
                }

                dispute.DisputeCPTs = new List<AuthorityDisputeCPT>();

                foreach (var fee in dispute.Fees)
                {
                    if(fee.FeeRecipient == FeeRecipient.Arbitrator)
                        fee.BaseFee = await _context.ArbitratorFees.FindAsync(fee.BaseFeeId);
                    if(fee.FeeRecipient == FeeRecipient.Authority)
                        fee.BaseFee = await _context.AuthorityFees.FindAsync(fee.BaseFeeId);
                }
                return dispute;
            }
            catch (Exception ex)
            {
                if (ex.InnerException == null)
                    return BadRequest(ex.Message);
                return BadRequest(ex.InnerException.Message);
            }
        }

        [HttpGet]
        [Route("codes")]
        public async Task<ActionResult<IEnumerable<ProcedureCode>>> GetCaseCodes([FromQuery]int disp)
        {
            if (disp < 1)
                return BadRequest("Invalid parameter");
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            try
            {
                var q = from c in _context.Set<AuthorityDisputeCPT>().Where(c => c.AuthorityDisputeId == disp)
                        join k in _context.Set<ClaimCPT>().Where(k => !k.isDeleted) on c.ClaimCPTId equals k.Id
                        join p in _context.Set<ProcedureCode>() on k.CPTCode equals p.Code
                        select p;

                var results = await q.AsNoTracking().ToArrayAsync();
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("log/{id}")]
        public async Task<ActionResult<IEnumerable<AuthorityDisputeLog>>> GetChangeLogsAsync(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            AuthorityDispute? orig = null;

            if (!user.HasGlobalCaseRole)
            {
                /*
                var allowedCustomerIDs = new List<int>();
                string[] allowedCustomerNames = new string[] { };
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
                orig = await _context.AuthorityDisputes.FirstOrDefaultAsync(d => d.Id == id && allowedCustomerNames.Contains(d.Customer));
                */
                return Unauthorized("AuthorityDisputes are limited to users with global roles only. Granular Customer-only roles may need to be added at some point.");
            }
            else
            {
                orig = await _context.AuthorityDisputes.FindAsync(id);
            }

            if (orig == null)
                return NotFound("Record not found or unauthorized to view AuthorityDispute information");

            try
            {
                return await _context.AuthorityDisputeLog.Where(d => d.AuthorityDisputeId == id).ToListAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Returns a list of Customers with active ArbitrationCase records that are at the appropriate Authority status
        /// </summary>
        /// <param name="a">Authority Id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("find/{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<AuthorityDispute>> GetDisputeByCaseIdAsync(string id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.HasGlobalCaseRole)
                return Unauthorized("Only global data access roles are currently supported");

            /* TODO: Enforce Customer-level access if needed
            var allowedCustomerIDs = new List<int>();
            List<string> allowedCustomerNames = new List<string>();
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }
            */
            try
            {
                var dispute = await _context.AuthorityDisputes
                            .Include(d => d.Arbitrator)
                            .Include(d => d.Attachments.Where(b => !b.IsDeleted))
                            .Include(d => d.Authority).ThenInclude(c => c!.TrackingDetails.Where(m => !m.IsDeleted))
                            .Include(d => d.Fees)
                            .Include(d => d.DisputeCPTs).ThenInclude(b => b.ClaimCPT)
                            .Include(d => d.Notes)
                            .Where(d => d.AuthorityCaseId == id) //&& (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)))
                            .AsNoTracking()
                            .AsSplitQuery()
                            .FirstOrDefaultAsync();

                if (dispute == null)
                    return NotFound("Unable to find the Dispute");

                if (dispute.Authority == null)
                    return NotFound("Invalid Authority value for the Dispute!");

                foreach (var cpt in dispute.DisputeCPTs)
                {
                    var claim = await GetClaimForDisputeCPTAsync(cpt);
                    if (claim == null || claim.PayorEntity == null)
                        return NotFound("One or more ArbitrationCase or Payor records could not be located for the Dispute!");

                    dispute.CPTViewmodels.Add(CreateDisputeCPTVM(dispute.Authority!, cpt, claim));
                }

                dispute.DisputeCPTs = new List<AuthorityDisputeCPT>();

                foreach (var fee in dispute.Fees)
                {
                    if (fee.FeeRecipient == FeeRecipient.Arbitrator)
                        fee.BaseFee = await _context.ArbitratorFees.FindAsync(fee.BaseFeeId);
                    if (fee.FeeRecipient == FeeRecipient.Authority)
                        fee.BaseFee = await _context.AuthorityFees.FindAsync(fee.BaseFeeId);
                }


                return dispute;
            }
            catch (Exception ex)
            {
                if (ex.InnerException == null)
                    return BadRequest(ex.Message);
                return BadRequest(ex.InnerException.Message);
            }
        }

        // GET - Get all items assigned to a user
        /// <summary>
        /// 
        /// </summary>
        /// <param name="aid">Authority Id</param>
        /// <param name="rid">Role Id</param>
        /// <param name="at">(Optional) Assign To</param>
        /// <returns>Instance of AuthorityDispute with the requested role assignment.</returns>
        [HttpGet]
        [Route("queue/current/{AuthorityId}/{RoleId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<AuthorityDispute>>> GetAllAssignedItemsAsync(int AuthorityId, int RoleId, [FromQuery] string? at = null)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            if (!Enum.IsDefined(typeof(WorkQueueName), RoleId))
                return BadRequest("No such role is defined");

            string AssignedTo = u.Email;

            if (!string.IsNullOrEmpty(at))
            {
                if (!u.IsManager)
                    return Unauthorized("Only managers may see other item assignments");
                AssignedTo = at;
            }

            if (AuthorityId < 1)
                AuthorityId = 0;

            try
            {
                var Q = _context.Set<AuthorityDispute>().Include(d => d.Arbitrator).Include(d => d.Authority).Where(d => d.AuthorityId == AuthorityId || AuthorityId == 0);
                Q = Q.AddCondition(() => RoleId == (int)WorkQueueName.All, d => (d.BriefWriter == AssignedTo && d.BriefWriterCompletedOn == null && d.WorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded) || 
                                                                                (d.BriefPreparer == AssignedTo && d.BriefPreparationCompletedOn == null && d.WorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded) || 
                                                                                ((u.IsManager || u.IsBriefApprover) && d.WorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefCreated && d.BriefApprovedOn == null));
                Q = Q.AddCondition(() => RoleId == (int)WorkQueueName.DisputeBriefPreparer, d => d.WorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded && d.BriefPreparer == AssignedTo && d.BriefPreparationCompletedOn == null);
                Q = Q.AddCondition(() => RoleId == (int)WorkQueueName.DisputeBriefWriter, d => d.WorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded && d.BriefWriter == AssignedTo && d.BriefWriterCompletedOn == null);

                //var SQL = Q.ToQueryString();

                var Results = await Q.ToArrayAsync();
                return Ok(Results);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message: ex.Message, ex);
#if DEBUG
                if (ex.InnerException != null)
                    return BadRequest(ex.InnerException.Message);
                return BadRequest(ex.Message);
#else
                
                return BadRequest("An error occurred during the GetAllAssignedItems action. Please contact technical support.");
#endif
            }
        }

        // GET - Assign next item to Calling User
        /// <summary>
        /// 
        /// </summary>
        /// <param name="AuthorityId">Authority Id</param>
        /// <param name="RoleId">Role Id</param>
        /// <param name="at">(Optional) Assign To</param>
        /// <returns>Instance of AuthorityDispute with the requested role assignment.</returns>
        [HttpGet]
        [Route("queue/next/{AuthorityId}/{RoleId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public async Task<ActionResult<AuthorityDispute>> AutoAssignUserToDisputeRoleAsync(int AuthorityId, WorkQueueName RoleId, [FromQuery] string? at = null)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            if (!u.IsManager && !u.IsNegotiator)
                return BadRequest("Only Managers and Negotiators can be assigned to work on an AuthorityDispute.");

            var AssignTo = u.Email;
            AppUser? Assignee = null;

            if (!string.IsNullOrEmpty(at))
            {
                if(!u.IsManager)
                    return BadRequest("Only Managers can assigned work to other users.");

                Assignee = await _context.AppUsers.FirstAsync(u => u.IsActive && u.Email == at);
                if (Assignee == null) 
                    return BadRequest("Assign To target user could be found. Is their profile active?");

                AssignTo = Assignee.Email;
            }

            if (AuthorityId < 1)
                return BadRequest("Invalid Authority Id");

            var Authority = await _context.Authorities.FindAsync(AuthorityId);
            if (Authority == null)
                return BadRequest("Authority Id does not match a valid Authority");

            try
            {
                switch (RoleId)
                {
                    case WorkQueueName.DisputeBriefPreparer:
                        if (Assignee != null && !Assignee.IsBriefPreparer && !Assignee.IsManager && !Assignee.IsNegotiator)
                            return BadRequest("The targeted user does not have the necessary permissions for the targeted role.");

                        var Count = await _context.AuthorityDisputes.CountAsync(d => d.BriefPreparer == AssignTo && d.BriefPreparationCompletedOn == null);
                        if (Count >= 5)
                            return BadRequest("The maximum number of Brief Preparation assignments was reached.");

                        var Dispute = await _context.AuthorityDisputes.Where(d => d.AuthorityId == AuthorityId &&
                                                                                  d.BriefPreparer == "" &&
                                                                                  d.BriefPreparationCompletedOn == null &&
                                                                                  d.BriefApprovedOn == null &&
                                                                                  d.WorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded)
                                                                                  .OrderBy(d => d.SubmissionDate)
                                                                                  .FirstOrDefaultAsync();
                        if (Dispute == null)
                            return NotFound("There are no available Disputes for assignment at this time.");

                        Dispute.BriefPreparer = AssignTo;
                        Dispute.UpdatedBy = u.Email;
                        Dispute.UpdatedOn = Utilities.GetCurrentUtcDate();
                        await _context.SaveChangesAsync();
                        return Ok(Dispute);

                    case WorkQueueName.DisputeBriefWriter:
                        if (Assignee != null && !Assignee.IsBriefWriter && !Assignee.IsManager && !Assignee.IsNegotiator)
                            return BadRequest("The targeted user does not have the necessary permissions for the targeted role.");

                        Count = await _context.AuthorityDisputes.CountAsync(d => d.BriefWriter == AssignTo && d.BriefWriterCompletedOn == null);
                        if (Count >= 5)
                            return BadRequest("The maximum number of Brief Writer assignments was reached.");

                        Dispute = await _context.AuthorityDisputes.Where(d => d.AuthorityId == AuthorityId &&
                                                                              d.BriefWriter == "" &&
                                                                              d.BriefWriterCompletedOn == null &&
                                                                              d.BriefApprovedOn == null &&
                                                                              d.BriefPreparationCompletedOn != null &&
                                                                              d.WorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded)
                                                                              .OrderBy(d => d.SubmissionDate)
                                                                              .FirstOrDefaultAsync();
                        if (Dispute == null)
                            return NotFound("There are no available Disputes for assignment at this time.");

                        Dispute.BriefWriter = AssignTo;
                        Dispute.UpdatedBy = u.Email;
                        Dispute.UpdatedOn = Utilities.GetCurrentUtcDate();
                        await _context.SaveChangesAsync();
                        return Ok(Dispute);

                    default:
                        return BadRequest("That role is not supported");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);
#if DEBUG
                if (ex.InnerException != null)
                    return BadRequest(ex.InnerException.Message);
                return BadRequest(ex.Message);
#else
                return BadRequest("An error occurred during the AutoAssignUser action. Please contact technical support.");
#endif
            }
        }

        [HttpPost("rel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<IEnumerable<AuthorityDispute>?>> FindRelatedAsync([FromBody] Int32[] d, [FromQuery] bool all = true)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (d.Length == 0)
                return BadRequest();
            try
            {
                var Q = (from ad in _context.Set<AuthorityDispute>()
                         join a in _context.Set<AuthorityDisputeCPT>() on ad.Id equals a.AuthorityDisputeId
                         join t in _context.Set<ClaimCPT>().Where(v => d.Contains(v.Id)) on a.ClaimCPTId equals t.Id
                         select ad);

                Q = Q.Include(v => v.DisputeCPTs).AsSplitQuery().AsNoTracking();

                // return a minimal AuthorityDispute w/o related recs
                var result = await Q.ToArrayAsync();
                if (result.Length > 0)
                {
                    var r = result.DistinctBy(v=>v.Id);
                    return Ok(r);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);
#if DEBUG
                if (ex.InnerException != null)
                    return BadRequest(ex.InnerException.Message);
                return BadRequest(ex.Message);
#else
                return BadRequest("An error occurred during the Find action. Please contact technical support.");
#endif
            }
        }
        #endregion

        #region Claim Builder routes
        /// <summary>
        /// Returns a list of Customers with active ArbitrationCase records that are at the appropriate Authority status
        /// </summary>
        /// <param name="a">Authority Id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("customers")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomersAsync([FromQuery] int a)
        {
            try
            {
                var authority = await _context.Authorities.FindAsync(a);
                if (authority == null || authority.Key.ToLower() != "nsa")
                    return BadRequest("Invalid or unsupported Authority for batching.");
                /*
                var values = await _context.ArbitrationCases.Include(d=>d.CPTCodes.Where(d => !d.isDeleted && d.IsIncluded))
                                                    .Where(d => !d.IsDeleted
                                                        && d.EntityNPI != ""
                                                        && d.ProviderNPI != ""
                                                        && (d.NSAWorkflowStatus == ArbitrationStatus.New || d.NSAWorkflowStatus == ArbitrationStatus.Open || d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress)
                                                        && (d.Status == ArbitrationStatus.New || d.Status == ArbitrationStatus.Open || d.Status == ArbitrationStatus.InformalInProgress || d.Status == ArbitrationStatus.Ineligible)
                                                        // TODO: When adding more Authority support in the future, remove next line and replace with valid AuthorityStatus values
                                                        && (d.NSAStatus == "Pending NSA Negotiation Request" || d.NSAStatus == "Submitted NSA Negotiation Request"))
                                              .Select(b => b.Customer).Distinct().ToListAsync();
                */
                var values = await (from d in _context.ArbitrationCases
                                    join t in _context.ClaimCPT on d.Id equals t.ArbitrationCaseId
                                    where !t.isDeleted && t.IsIncluded && !d.IsDeleted && d.EntityNPI != "" && d.ProviderNPI != ""
                                    && (d.NSAWorkflowStatus == ArbitrationStatus.New || d.NSAWorkflowStatus == ArbitrationStatus.Open || d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress)
                                    && (d.Status == ArbitrationStatus.New || d.Status == ArbitrationStatus.Open || d.Status == ArbitrationStatus.InformalInProgress || d.Status == ArbitrationStatus.Ineligible)
                                    // TODO: When adding more Authority support in the future, remove next line and replace with valid AuthorityStatus values
                                    && (d.NSAStatus == "Pending NSA Negotiation Request" || d.NSAStatus == "Submitted NSA Negotiation Request")
                                    select d.Customer).Distinct().ToListAsync();

                var customers = await _context.Customers.Where(c => values.Contains(c.Name)).ToListAsync();
                return Ok(customers);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a">Authority Id</param>
        /// <param name="c">Customer Id</param>
        /// <param name="e">Entity Id</param>
        /// <param name="pv">ProviderNPI</param>
        /// <returns>IEnumerable[Payor]</returns>
        [HttpGet]
        [Route("claims")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<ArbitrationCase>>> GetClaimsAsync([FromQuery] int a, [FromQuery] int c, [FromQuery] int pay, [FromQuery] string pv)
        {
            try
            {
                var authority = await _context.Authorities.FindAsync(a);
                if (authority == null || authority.Key.ToLower() != "nsa")
                    return BadRequest("Invalid or unsupported Authority for batching.");

                var customer = await _context.Customers.FindAsync(c);
                if (customer == null)
                    return BadRequest("Invalid Customer identifier");

                var payor = await _context.Payors.FindAsync(pay);
                if (payor == null)
                    return BadRequest("Invalid Payor identifier");

                var parentId = payor.ParentId;

                var Q1 = _context.ArbitrationCases.Include(d => d.CPTCodes.Where(b => !b.isDeleted && b.IsIncluded)).DefaultIfEmpty().AsSplitQuery()
                                 .Where(d => !d.IsDeleted
                                            && d.Customer == customer.Name
                                            && (d.PayorId == payor.Id || d.PayorId == payor.ParentId)
                                            && d.ProviderNPI == pv
                                            // TODO: When adding more Authority support in the future, remove next line and add conditionally later
                                            && (d.NSAWorkflowStatus == ArbitrationStatus.New || d.NSAWorkflowStatus == ArbitrationStatus.Open || d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress)
                                            && (d.Status == ArbitrationStatus.New || d.Status == ArbitrationStatus.Open || d.Status == ArbitrationStatus.InformalInProgress || d.Status == ArbitrationStatus.Ineligible)
                                            // TODO: When adding more Authority support in the future, remove next line and replace with valid AuthorityStatus values
                                            && (d.NSAStatus == "Pending NSA Negotiation Request" || d.NSAStatus == "Submitted NSA Negotiation Request"));

                // TODO: When adding more Authority support in the future, adjust next line
                if (authority.Key.ToLower() != "nsa")
                    Q1 = Q1.AddCondition(() => true, d => d.Authority == authority.Key);

                var claims = await Q1.ToListAsync();

                return Ok(claims.Where(d => d.CPTCodes.Count() > 0).ToList());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Returns a list of Customers with active ArbitrationCase records that are at the appropriate Authority status
        /// </summary>
        /// <param name="a">Authority Id</param>
        /// <param name="c">Customer Id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("entities")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Entity>>> GetEntitiesAsync([FromQuery] int a, [FromQuery] int c)
        {
            try
            {
                var authority = await _context.Authorities.FindAsync(a);
                if (authority == null || authority.Key.ToLower() != "nsa")
                    return BadRequest("Invalid or unsupported Authority for batching.");

                var customer = await _context.Customers.FindAsync(c);
                if (customer == null)
                    return BadRequest("Invalid Customer identifier");
                /*
                var values = await _context.ArbitrationCases
                                            .Where(d => !d.IsDeleted
                                                        && d.Customer == customer.Name
                                                        && d.EntityNPI != ""
                                                        && d.ProviderNPI != ""
                                                        && (d.NSAWorkflowStatus == ArbitrationStatus.New || d.NSAWorkflowStatus == ArbitrationStatus.Open || d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress)
                                                        && (d.Status == ArbitrationStatus.New || d.Status == ArbitrationStatus.Open || d.Status == ArbitrationStatus.InformalInProgress || d.Status == ArbitrationStatus.Ineligible)
                                                        // TODO: When adding more Authority support in the future, remove next line and replace with valid AuthorityStatus values
                                                        && (d.NSAStatus == "Pending NSA Negotiation Request" || d.NSAStatus == "Submitted NSA Negotiation Request"))
                                            .Select(b => b.EntityNPI).Distinct().ToListAsync();
                */
                var values = await (from d in _context.ArbitrationCases
                                    join t in _context.ClaimCPT on d.Id equals t.ArbitrationCaseId
                                    where d.Customer == customer.Name
                                    && !t.isDeleted && t.IsIncluded
                                    && !d.IsDeleted && d.EntityNPI != "" && d.ProviderNPI != ""
                                    && (d.NSAWorkflowStatus == ArbitrationStatus.New || d.NSAWorkflowStatus == ArbitrationStatus.Open || d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress)
                                    && (d.Status == ArbitrationStatus.New || d.Status == ArbitrationStatus.Open || d.Status == ArbitrationStatus.InformalInProgress || d.Status == ArbitrationStatus.Ineligible)
                                    // TODO: When adding more Authority support in the future, remove next line and replace with valid AuthorityStatus values
                                    && (d.NSAStatus == "Pending NSA Negotiation Request" || d.NSAStatus == "Submitted NSA Negotiation Request")
                                    select d.EntityNPI).Distinct().ToListAsync();

                var entities = await _context.Entities.Where(c => values.Contains(c.NPINumber)).ToListAsync();
                return Ok(entities);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a">Authority Id</param>
        /// <param name="c">Customer Id</param>
        /// <param name="e">Entity Id</param>
        /// <param name="pv">ProviderNPI</param>
        /// <returns>IEnumerable[Payor]</returns>
        [HttpGet]
        [Route("payors")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Payor>>> GetPayorsAsync([FromQuery] int a, [FromQuery] int c, [FromQuery] int e, [FromQuery] string pv)
        {
            try
            {
                var authority = await _context.Authorities.FindAsync(a);
                if (authority == null || authority.Key.ToLower() != "nsa")
                    return BadRequest("Invalid or unsupported Authority for batching.");

                var customer = await _context.Customers.FindAsync(c);
                if (customer == null)
                    return BadRequest("Invalid Customer identifier");

                var entity = await _context.Entities.FindAsync(e);
                if (entity == null)
                    return BadRequest("Invalid Entity identifier");
                /*
                var Q1 = _context.ArbitrationCases.Where(d => !d.IsDeleted
                                                                && d.Customer == customer.Name
                                                                && d.EntityNPI == entity.NPINumber
                                                                && d.ProviderNPI == pv
                                                                // TODO: When adding more Authority support in the future, remove next line and add conditionally later
                                                                && (d.NSAWorkflowStatus == ArbitrationStatus.New || d.NSAWorkflowStatus == ArbitrationStatus.Open || d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress)
                                                                && (d.Status == ArbitrationStatus.New || d.Status == ArbitrationStatus.Open || d.Status == ArbitrationStatus.InformalInProgress || d.Status == ArbitrationStatus.Ineligible)
                                                                // TODO: When adding more Authority support in the future, remove next line and replace with valid AuthorityStatus values
                                                                && (d.NSAStatus == "Pending NSA Negotiation Request" || d.NSAStatus == "Submitted NSA Negotiation Request"));

                if (authority.Key.ToLower() != "nsa")
                    Q1 = Q1.AddCondition(() => true, d => d.Authority == authority.Key);

                var Q2 = Q1.Select(v => v.PayorId).Distinct();
                */
                var Q1 = (from d in _context.ArbitrationCases
                          join t in _context.ClaimCPT on d.Id equals t.ArbitrationCaseId
                          where !t.isDeleted && t.IsIncluded
                                  && !d.IsDeleted
                                  && d.Customer == customer.Name
                                  && d.EntityNPI == entity.NPINumber
                                  && d.ProviderNPI == pv
                                  && (d.NSAWorkflowStatus == ArbitrationStatus.New || d.NSAWorkflowStatus == ArbitrationStatus.Open || d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress)
                                  && (d.Status == ArbitrationStatus.New || d.Status == ArbitrationStatus.Open || d.Status == ArbitrationStatus.InformalInProgress || d.Status == ArbitrationStatus.Ineligible)
                                  // TODO: When adding more Authority support in the future, remove next line and replace with valid AuthorityStatus values
                                  && (d.NSAStatus == "Pending NSA Negotiation Request" || d.NSAStatus == "Submitted NSA Negotiation Request")
                          select d);

                if (authority.Key.ToLower() != "nsa")
                    Q1 = Q1.AddCondition(() => true, d => d.Authority == authority.Key);

                var Q2 = Q1.Select(v => v.PayorId).Distinct();
                var values = await Q2.ToListAsync();
                var payors = await _context.Payors.Where(c => values.Contains(c.Id)).ToListAsync();

                return Ok(payors);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Returns a list of Providers derived from active ArbitrationCase records that are at the appropriate Authority Status.
        /// </summary>
        /// <param name="a">Authority Id</param>
        /// <param name="c">Customer Id</param>
        /// <param name="e">Entity Id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("providers")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<ProviderVM>>> GetProvidersAsync([FromQuery] int a, [FromQuery] int c, [FromQuery] int e)
        {
            try
            {
                var authority = await _context.Authorities.FindAsync(a);
                if (authority == null || authority.Key.ToLower() != "nsa")
                    return BadRequest("Invalid or unsupported Authority for batching.");

                var customer = await _context.Customers.FindAsync(c);
                if (customer == null)
                    return BadRequest("Invalid Customer identifier");

                var entity = await _context.Entities.FindAsync(e);
                if (entity == null)
                    return BadRequest("Invalid Entity identifier");

                var Q1 = _context.ArbitrationCases
                                 .Where(d => !d.IsDeleted
                                            && d.Customer == customer.Name
                                            && d.EntityNPI == entity.NPINumber
                                            && d.ProviderNPI != ""
                                            // TODO: When adding more Authority support in the future, remove next line and add conditionally later
                                            && (d.NSAWorkflowStatus == ArbitrationStatus.New || d.NSAWorkflowStatus == ArbitrationStatus.Open || d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress)
                                            && (d.Status == ArbitrationStatus.New || d.Status == ArbitrationStatus.Open || d.Status == ArbitrationStatus.InformalInProgress || d.Status == ArbitrationStatus.Ineligible)
                                            // TODO: When adding more Authority support in the future, remove next line and replace with valid AuthorityStatus values
                                            && (d.NSAStatus == "Pending NSA Negotiation Request" || d.NSAStatus == "Submitted NSA Negotiation Request"));

                if (authority.Key.ToLower() != "nsa")
                    Q1 = Q1.AddCondition(() => true, d => d.Authority == authority.Key);

                var Q2 = Q1.Select(v => new ProviderVM { EntityNPI = v.EntityNPI, ProviderName = v.ProviderName, ProviderNPI = v.ProviderNPI }).Distinct();

                var providers = await Q2.ToListAsync();

                return Ok(providers);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region POST Routes
        [HttpPost]
        [Route("blob")]
        public async Task<ActionResult<AuthorityDisputeAttachment>> AddAttachmentAsync([FromQuery] int id, [FromQuery] string cdt, [FromForm] IFormFile file)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsManager && !user.IsNegotiator && !user.IsSystem)
                return Unauthorized("Insufficient privileges to attach Files to a Case");

            if (id < 1)
                return BadRequest("Invalid case identifier");

            if (string.IsNullOrEmpty(cdt))
                return BadRequest("Missing Case Document Type");

            if (!file.FileName.ToLower().EndsWith(".pdf"))
                return BadRequest("Invalid file type");

            if (file != null && file.Length > 30000000)
                return BadRequest("File size is too large. Split into multiple uploads or contact support.");

            if (file == null)
                return BadRequest("No content detected");

            if (Enum.TryParse<CaseDocumentType>(cdt, true, out CaseDocumentType parseResult) == false)
                return BadRequest("Unsupported document metadata");

            var dispute = await _context.AuthorityDisputes.AsSplitQuery().AsNoTracking()
                                        .Include(d => d.Authority)
                                        .Include(d => d.Attachments)
                                        .Include(d => d.DisputeCPTs).ThenInclude(b => b.ClaimCPT)
                                        .FirstOrDefaultAsync(d => d.Id == id);

            if (dispute == null || dispute.Authority == null)
                return BadRequest("Record not found");
            if (string.IsNullOrEmpty(dispute.AuthorityCaseId))
                return BadRequest("Cannot attach files to Disputes missing their AuthorityCaseId");
            if (dispute.DisputeCPTs.Count == 0)
                return BadRequest("Cannot attach files to Disputes without CPTs");

            var cpt = dispute.DisputeCPTs.First().ClaimCPT;
            var claim = await _context.ArbitrationCases.FirstOrDefaultAsync(b => b.Id == cpt.ArbitrationCaseId);
            if (claim == null)
                return BadRequest("Unable to locate the Payor associated with the first CPT!");

            string payor = claim.Payor;
            var uploadedOn = Utilities.GetCurrentUtcDate();
            var uploadedBy = GetUsername();
            var log = new StringBuilder($@"{uploadedOn:G}: Attaching file to case id {id}.");
            string blobURL = "";

            try
            {
                using (var reader = file.OpenReadStream())
                {
                    string blobName = $@"{dispute.Authority.Key}-{dispute.AuthorityCaseId.Replace("-","_")}-{cdt}-{file.FileName.ToLower()}";
                    try
                    {
                        BlobClient blob = _containerClient.GetBlobClient(blobName);

                        log.AppendLine($@"Attempting to upload file {blobName} to BLOB store...");
                        var response = await blob.UploadAsync(reader, true);
                        if (response.GetRawResponse().ReasonPhrase != "Created")
                            throw new Exception("Unexpected result from BLOB upload. " + response.GetRawResponse().ReasonPhrase);
                        
                        log.AppendLine($@"BLOB successfully uploaded to {blobURL}");

                        // add tags to new BLOB
                        var tags = new Dictionary<string, string>();
                        tags.Add("AuthorityCaseId", dispute.AuthorityCaseId);
                        tags.Add("AuthorityDisputeId", dispute.Id.ToString());
                        tags.Add("UpdatedBy", uploadedBy);
                        tags.Add("DocumentType", cdt.ToLower());
                        tags.Add("Payor", payor);

                        await blob.SetTagsAsync(tags);
                        log.AppendLine($@"Tags added successfully");

                        blobURL = $@"{_containerClient.Uri.ToString()}/{blobName}";

                        // save entry into database to facilitate reporting on claims that do not have certain attachments per Megan R. request 2023-9-28
                        var ModDate = Utilities.GetCurrentUtcDate();
                        var attachment = dispute.Attachments.FirstOrDefault(d => d.AuthorityDisputeId == dispute.Id && d.DocType.Equals(cdt, StringComparison.InvariantCultureIgnoreCase) && d.BLOBName == blobName);
                        if (attachment != null)
                        {
                            // update existing entry - records could easily get purged in Azure and de-sync these records so...
                            attachment.IsDeleted = false;
                            attachment.CreatedBy = uploadedBy;
                            attachment.CreatedOn = ModDate;
                            attachment.UpdatedOn = ModDate;
                            attachment.UpdatedBy = uploadedBy;
                        }
                        else
                        {
                            attachment = new AuthorityDisputeAttachment { BLOBLink = blobURL, AuthorityDisputeId = dispute.Id, BLOBName = blobName, CreatedBy = uploadedBy, CreatedOn = ModDate, DocType = cdt, UpdatedBy = uploadedBy, UpdatedOn = ModDate };
                            await _context.AuthorityDisputeAttachments.AddAsync(attachment);
                        }

                        log.AppendLine($@"Uploaded by {uploadedBy}");

                        try
                        {
                            await _context.SaveChangesAsync();
                            log.AppendLine($@"AuthorityDisputeAttachment record created successfully");
                            return Ok(attachment);
                        }
                        catch { } // swallow error - file is already in storage
                    }
                    catch (Exception ex)
                    {
                        log.AppendLine("ERROR: Unable to write to BLOB storage. " + ex.Message);
                        log.Append(ex.StackTrace);
                        _logger.LogError(ex.Message);
                    }
                }
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
            finally
            {
                _logger.LogInformation(log.ToString());
            }

            return BadRequest("File data missing or unreadable");
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
            if (!(name.StartsWith($@"{id}-") || name.StartsWith($@"nsa-"))
                || !(n.EndsWith(".pdf") || n.EndsWith(".tif") || n.EndsWith(".tiff")))
                return BadRequest("Invalid document name");

            AuthorityDispute? authorityDispute = null;

            if (!user.HasGlobalCaseRole && !user.IsSystem)
            {
                var allowedCustomerIDs = new List<int>();
                string[] allowedCustomerNames = new string[] { };
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.AsNoTracking().Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToArrayAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
                authorityDispute = await _context.AuthorityDisputes.FirstOrDefaultAsync(d => d.Id == id);
            }
            else
            {
                authorityDispute = await _context.AuthorityDisputes.FirstOrDefaultAsync(d => d.Id == id);
            }

            if (authorityDispute == null)
                return NotFound("Record not found or unauthorized to view Files for the Case");

            try
            {
                //AuthorityCaseId
                //AuthorityDisputeId
                //DocumentType
                var blob = _containerClient.GetBlobClient(name);

                //checked file is really linked to this dispute
                var tags = blob.GetTags().Value.Tags;
                var authorityCaseId = tags.First(x => x.Key.Equals("AuthorityCaseId", StringComparison.InvariantCultureIgnoreCase));
                var authorityDisputeId = tags.First(x => x.Key.Equals("AuthorityDisputeId", StringComparison.InvariantCultureIgnoreCase));

                if(authorityDisputeId.Value != id.ToString())
                {

                }
                var result = await blob.DownloadContentAsync();
                var stream = result.Value.Content.ToStream();

                var fileNameParts = name.ToLower().Split('.');
                var fileNameExtension = fileNameParts[fileNameParts.Length - 1];
                string mimeType = "application/pdf";
                if (fileNameExtension == "tif" || fileNameExtension == "tiff")
                    mimeType = "image/tiff";
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

        // POST - Initialize a new AuthorityDispute
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<AuthorityDispute>> CreateDisputeAsync([FromBody] AuthorityDispute dispute)
        {
            try
            {
                var u = await GetCurrentUser();
                if (u == null)
                    return Unauthorized("No active User context!");

                if (!u.IsManager && !u.IsNegotiator && !u.IsSystem)
                    return BadRequest("Only Managers and Negotiators can create new AuthorityDisputes.");

                if (dispute.AuthorityId < 1 || string.IsNullOrEmpty(dispute.AuthorityCaseId))
                    return BadRequest("Authority Disputes require an AuthorityId and AuthorityCaseId because you must first submit the dispute to the Authority and obtain a case number.");
                
                if (dispute.CPTViewmodels.Count == 0)
                    return BadRequest("CPTViewmodels are required.");

                if (dispute.DisputeCPTs.Count > 0)
                    return BadRequest("DisputeCPTs are not used for creating new AuthorityDisputes. Please submit an AuthorityDisputeCPTVM collection along with the new AuthorityDispute.");

                if (dispute.CPTViewmodels.FirstOrDefault(d => d.ClaimCPT == null || (d.ClaimCPT != null && d.ClaimCPT.Id < 1)) != null)
                    return BadRequest("Each DisputeCPT must have a valid ClaimCPT attached.");

                var test = await _context.AuthorityDisputes.FirstOrDefaultAsync(d => d.AuthorityId == dispute.AuthorityId && d.AuthorityCaseId == dispute.AuthorityCaseId);
                if (test != null)
                    return BadRequest("The AuthorityCaseId aka Dispute # is not unique for the specified Authority.");

                var authority = await _context.Authorities.Include(d => d.TrackingDetails.Where(b => !b.IsDeleted && (b.Scope == AuthorityTrackingDetailScope.All || b.Scope == AuthorityTrackingDetailScope.AuthorityDispute))).FirstOrDefaultAsync(d => d.Id == dispute.AuthorityId && d.IsActive);
                if (authority == null)
                    return BadRequest("Unable to locate a matching Authority that has an active Arbitration process.");

                if (authority.TrackingDetails.Count == 0)
                    return BadRequest("The Authority tracking configurations are missing. Please contact technical support.");

                var values = authority.StatusValues.ToLower().Split(new char[] { ',', ';' });
                if (!values.Contains(dispute.AuthorityStatus.ToLower()))
                    return BadRequest("Invalid Authority Status");

                bool isNSA = authority.Key.ToLower() == "nsa";

                if (isNSA && !u.IsNSA && !u.IsSystem)
                    return BadRequest("Your account is not authorized to work with NSA claims.");

                if (authority.Key.ToLower() != "nsa" && !u.IsState && !u.IsSystem)
                    return BadRequest("Your account is not authorized to work with State claims.");

                int[]? arbitIdNumbers = dispute.CPTViewmodels.Select(d => d.ClaimCPT.ArbitrationCaseId).ToArray();

                if (arbitIdNumbers == null || arbitIdNumbers.Count(d => d < 1) > 0)
                    return BadRequest("Invalid ClaimCPT object. Are you being cheeky with the API?");

                var cptCodes = dispute.CPTViewmodels.Select(d => d.ClaimCPT.CPTCode).Distinct();
                var cptCode = cptCodes.Count() > 1 ? "*" : cptCodes.First();

                var existing = await GetActiveDisputeIdsForASetOfClaimNumbersAsync(arbitIdNumbers, cptCode);
                if (existing != null && existing.Count() > 0)
                    return BadRequest($@"One or more items specified in the criteria are already attached to active (ongoing) formal Disputes. See AuthorityDispute Id(s) {string.Join(',', existing)}.");

                foreach(var fee in dispute.Fees)
                {
                    if (fee.Id != 0) return BadRequest("Cannot attach existing Fee to a new Dispute.");
                    if (fee.AuthorityDisputeId != 0) return BadRequest("Invalid AuthorityDisputeId");
                    if (fee.BaseFeeId == 0) return BadRequest("Invalid BaseFeeId");
                    if (fee.AmountDue == 0) return BadRequest("AmountDue for a fee cannot be zero!");
                    
                    if(fee.FeeRecipient == FeeRecipient.Arbitrator)
                    {
                        if (await _context.ArbitratorFees.FindAsync(fee.BaseFeeId) == null) 
                            return BadRequest("Could not locate ForeignKey for BaseFee");
                    } else
                    {
                        if (await _context.AuthorityFees.FindAsync(fee.BaseFeeId) == null)
                            return BadRequest("Could not locate ForeignKey for BaseFee");
                    }
                }

                await Extensions.EnsureHolidays(_context);

                var updatedOn = Utilities.GetCurrentUtcDate();
                var claims = await _context.ArbitrationCases.Where(d=> !d.IsDeleted && arbitIdNumbers.Contains(d.Id)).Include(d => d.CPTCodes.Where(g => !g.isDeleted && g.IsIncluded)).ToArrayAsync();
                ArbitrationCase? claim = null;
                int ParentPayorId = 0;

                // Validate the Claims and ClaimCPTs, and make the DisputeCPT objects for submission to the data store
                foreach (var vm in dispute.CPTViewmodels.OrderBy(d => d.ClaimCPT.ArbitrationCaseId)) // performance enhancement
                {
                    var cpt = vm.ClaimCPT;
                    claim = claim == null || claim.Id != cpt.ArbitrationCaseId ? claims.FirstOrDefault(d => d.Id == cpt.ArbitrationCaseId) : claim;
                    if (claim == null)
                        return BadRequest($@"Unable to find an active ArbitrationCase record with an id of {cpt.ArbitrationCaseId}");

                    var origCPT = claim.CPTCodes.FirstOrDefault(d => d.Id == cpt.Id && d.CPTCode.Equals(cpt.CPTCode, StringComparison.CurrentCultureIgnoreCase));
                    if (origCPT == null)
                        return BadRequest($@"Unable to find all of the attached ClaimCPTs. (Are they all associated with active Claims and marked eligible?)");

                    if (claim.PayorId == null)
                        return BadRequest($@"ArbitrationCase Id {claim.Id} does not have a valid PayorId!");

                    if (ParentPayorId == 0)
                        ParentPayorId = claim.PayorId.Value;

                    if (ParentPayorId != claim.PayorId.Value)
                        ParentPayorId = (await _context.Payors.FirstOrDefaultAsync(d => d.Id == ParentPayorId))!.ParentId;

                    if (ParentPayorId != claim.PayorId.Value)
                        return BadRequest("The requested claims span more than one prime Payor. This is not allowed.");

                    var dcpt = new AuthorityDisputeCPT
                    {
                        BenchmarkAmount = vm.BenchmarkAmount,
                        AddedBy = u.Email, 
                        AddedOn = updatedOn,
                        AuthorityDisputeId = 0,
                        BenchmarkDataItemId = 0,
                        BenchmarkDatasetId = 0,
                        BenchmarkOverrideAmount = vm.BenchmarkOverrideAmount, // Math.Ceiling(cpt.ProviderCharges, BenchmarkAmount) as a default
                        CalculatedOfferAmount = vm.CalculatedOfferAmount,
                        FinalOfferAmount = vm.FinalOfferAmount <= cpt.ProviderChargeAmount ? vm.FinalOfferAmount : cpt.ProviderChargeAmount,
                        ClaimCPTId = origCPT.Id,
                        ServiceLineDiscount = vm.ServiceLineDiscount,
                        UpdatedBy = u.Email,
                        UpdatedOn = updatedOn
                    };
                    dispute.DisputeCPTs.Add(dcpt);

                    // Add initial DisputeLog 
                    var entry = new AuthorityDisputeLog
                    {
                        Action = "APICreate", // TODO: Make this an enum throughout the app
                        CreatedBy = u.Email,
                        CreatedOn = updatedOn,
                        Details = "New Dispute created",
                        Id = 0
                    };
                    dispute.ChangeLog.Add(entry);
                }

                foreach(var f in dispute.Fees)
                {
                    f.DueOn = await CalculateFeeDueDateAsync(dispute, f);
                    f.UpdatedBy = u.Email;
                    f.UpdatedOn = u.UpdatedOn;
                    f.Id = 0;
                }

                // Init tracking info
                var ValueNode = JsonNode.Parse(dispute.TrackingValues);
                if (ValueNode == null)
                {
                    dispute.TrackingValues = "{}";
                    ValueNode = JsonNode.Parse(dispute.TrackingValues);
                }

                Utilities.UpdateTrackingCalculations(ValueNode!, authority.TrackingDetails, dispute);
                dispute.TrackingValues = ValueNode!.ToJsonString();

                // Save changes
                dispute.Authority = null;
                dispute.CreatedBy = u.Email;
                dispute.CreatedOn = Utilities.GetCurrentUtcDate();
                dispute.WorkflowStatus = ArbitrationStatus.ActiveArbitrationBriefNeeded;

                _context.AuthorityDisputes.Add(dispute);
                await _context.SaveChangesAsync();

                // update the VMs to send back
                foreach (var cpt in dispute.CPTViewmodels)
                {
                    var dcpt = dispute.DisputeCPTs.FirstOrDefault(d => d.ClaimCPTId == cpt.ClaimCPTId); 
                    if (dcpt == null) 
                        continue; // shouldn't happen

                    claim = claim == null ||claim.Id != cpt.ClaimCPT.ArbitrationCaseId ? claims.FirstOrDefault(d => d.Id == cpt.ClaimCPT.ArbitrationCaseId) : claim;

                    cpt.AddedBy = dcpt.AddedBy;
                    cpt.AddedOn = dcpt.AddedOn;
                    cpt.AuthorityDisputeId = dispute.Id;
                    cpt.FinalOfferAmount = dcpt.FinalOfferAmount;
                    cpt.Id = dcpt.Id;
                    cpt.PayorClaimNumber = claim?.PayorClaimNumber ?? "";
                    cpt.UpdatedBy = dcpt.UpdatedBy;
                    cpt.UpdatedOn = dcpt.UpdatedOn;
                    
                }

                dispute.DisputeCPTs = new List<AuthorityDisputeCPT>(); // the client side does not get the raw DisputeCPTs via this endpoint
                await AttachBaseFeeObjectsAsync(dispute.Fees);
                return dispute;
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                if(ex.InnerException == null)
                    return BadRequest(ex.Message);
                return BadRequest(ex.InnerException.Message);
            }
        }

        // POST - Initialize a new AuthorityDispute
        [HttpPost]
        [Route("init")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<AuthorityDispute>> InitializeDisputeAsync([FromBody] AuthorityDisputeInit values)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            if (!u.IsManager && !u.IsNegotiator && !u.IsSystem)
                return BadRequest("Only Managers and Negotiators can create new AuthorityDisputes.");

            if (values == null || string.IsNullOrEmpty(values.Claims) || string.IsNullOrEmpty(values.CPT) || string.IsNullOrEmpty(values.Auth))
                return BadRequest("Invalid or missing parameters");

            var authority = await _context.Authorities.Include(d=>d.Fees.Where(f => f.IsActive)).FirstOrDefaultAsync(d => d.Key == values.Auth && d.IsActive);
            if (authority == null)
                return BadRequest("Unable to locate a matching Authority that has an active Arbitration process.");

            bool isNSA = authority.Key.ToLower() == "nsa";

            if (isNSA && !u.IsNSA && !u.IsSystem)
                return BadRequest("Your account is not authorized to work with NSA claims.");

            if (authority.Key.ToLower() != "nsa" && !u.IsState && !u.IsSystem)
                return BadRequest("Your account is not authorized to work with State claims.");

            if (values.CPT != "*" && (values.CPT.Length > 10 || Regex.Matches(values.CPT, @"\W").Count() > 0))
                return BadRequest("Invalid parameters");

            values.CPT = values.CPT.ToLower();

            int[]? ArbitIdNumbers = null;
            try
            {
                ArbitIdNumbers = values.Claims.Split(',').Select(v => Convert.ToInt32(v)).ToArray();
            }
            catch { }

            if (ArbitIdNumbers == null)
                return BadRequest("Unable to parse the list of claim identifiers");

            if (!string.IsNullOrEmpty(values.AuthorityCaseId) && await _context.AuthorityDisputes.FirstOrDefaultAsync(d => d.AuthorityCaseId == values.AuthorityCaseId) != null)
                return BadRequest(("The specified AuthorityCaseId is already in use"));
                
            var existing = await GetActiveDisputeIdsForASetOfClaimNumbersAsync(ArbitIdNumbers, values.CPT);
            if (existing != null && existing.Count() > 0)
                return BadRequest($@"One or more items specified in the criteria are already attached to active (ongoing) formal Disputes. See AuthorityDispute Id(s): {string.Join(',',existing.Distinct())}.");

            existing = await CheckArbitIdsForActiveCasesAsync(ArbitIdNumbers);
            if (existing != null && existing.Count() > 0)
                return BadRequest($@"One or more items specified in the criteria are already attached to ACTIVE (ongoing) formal Cases in SOME jurisdiction. See ArbitrationCase Id(s) {string.Join(',', existing.Distinct())}.");

            // NOTE: Currently, the system does not validate a CPT against prior settlements. Thus, a CPT could be disputed again if the rest of the work chain permits it.
            // Still, despite, the lack of detail in the current specifications, the current algorithm (here) still goes above and beyond what the business has really documented or specified.
            var rightNow = Utilities.GetCurrentUtcDate();
            var result = new AuthorityDispute { AuthorityId = authority.Id, AuthorityCaseId = values.AuthorityCaseId, Id = 0, AuthorityStatus = "Not Submitted", SubmissionDate = rightNow, TrackingValues = "{}", WorkflowStatus = ArbitrationStatus.New };
            
            if (isNSA)
                result.AuthorityStatus = "Submitted NSA Negotiation Request";

            // Find the desired claims and CPTs and attach them to the return object (no saving)
            var claims = await GetAuthorityClaimsForBatchAsync(authority, ArbitIdNumbers!, values);
            if (claims.Count() != ArbitIdNumbers.Count())
                return BadRequest("Could not find the ArbitrationCase (claim) records supplied. Verify the claim IDs, the AUTHORITY STATUS (Submitted?), and that there are eligible CPTs.");
            
            var customers = claims.Select(d => d.Customer).Distinct();

            if (customers.Count() > 1)
                return BadRequest("The list of claims provided spans multiple Customers. This is not allowed!");

            var payors = claims.Where(d => d.PayorEntity != null);
            if (payors.Count() != claims.Count)
                return BadRequest("One or more of the selected claims is not assigned to a valid Payor.");

            // Get latest calculator vars
            var varsFilter = from r in _context.CalculatorVariables.Where(x => x.CreatedOn <= rightNow)
                         group r by r.ServiceLine into op
                         select op.OrderByDescending(x => x.CreatedOn).First();

            var calcVars = await varsFilter.ToListAsync();

            // The "pre-initialized" Dispute will have a bunch of view models to support the UI.
            // When these are posted back to the Create endpoint, they'll be re-examined and converted into a proper Dispute
            // if they pass routine validation again.
            foreach (var claim in claims)
            {
                var calcs = calcVars.FirstOrDefault(d => d.ServiceLine == claim.ServiceLine);
                if (calcs == null)
                    return BadRequest($@"One of the specified claims has an unknown ServiceLine ({claim.ServiceLine})! Compare to CalculatorVariables configuration.");

                foreach(var cpt in claim.CPTCodes.Where(d => values.CPT == "*" || d.CPTCode == values.CPT))
                {
                    var benchmarkAmount = GetBenchmarkValueFromCPT(authority, calcs, claim, cpt);
                    var serviceLineDiscount = Utilities.GetDefaultServiceLineDiscount(authority, calcs);
                    var offerAmount = Math.Round(benchmarkAmount * (1 - serviceLineDiscount), 2); // GetDefaultOfferAmountForAuthority(authority, calculation, claim, CPT);
                    /*
                    var dCPT = new AuthorityDisputeCPTVM { ClaimCPTId = cpt.Id, GeoZip = claim.LocationGeoZip, CPTCode = cpt.CPTCode, 
                                                            Customer = claim.Customer, Entity = claim.Entity, EntityNPI = claim.EntityNPI,
                                                            FinalOfferCalculatedAmount = offerAmount, PaidAmount = cpt.PaidAmount, 
                                                            PatientRespAmount = cpt.PatientRespAmount, Payor = claim.Payor, PlanType = claim.PlanType,
                                                            ProviderChargeAmount = cpt.ProviderChargeAmount, ProviderName = claim.ProviderName, 
                                                            ProviderNPI = claim.ProviderNPI, Units = Convert.ToInt32(cpt.Units)};
                    */

                    // We only make View models on this Init endpoint. The client will pass an array of AuthorityDisputeCPT when doing the actual create or update
                    var dcpt = new AuthorityDisputeCPTVM
                    {
                        BenchmarkAmount = benchmarkAmount,
                        BenchmarkDataItemId = 0,
                        BenchmarkDatasetId = 0,
                        BenchmarkOverrideAmount = 0, // Math.Ceiling(cpt.ProviderCharges, BenchmarkAmount) as a default
                        CalculatedOfferAmount = offerAmount,
                        FinalOfferAmount = offerAmount <= cpt.ProviderChargeAmount ? offerAmount : cpt.ProviderChargeAmount,
                        GeoRegion = GetGeoRegion(claim.LocationGeoZip),
                        NotificationDate = GetNotificationDate(authority, claim),
                        ServiceDate = claim.ServiceDate,
                        ClaimCPTId = cpt.Id,
                        GeoZip = claim.LocationGeoZip,
                        ClaimCPT = cpt,
                        Customer = claim.Customer,
                        Entity = claim.Entity,
                        EntityNPI = claim.EntityNPI,
                        Payor = claim.PayorEntity!.Name,
                        PayorId = claim.PayorEntity!.Id,
                        PlanType = claim.PlanType,
                        ProviderName = claim.ProviderName,
                        ProviderNPI = claim.ProviderNPI,
                        ServiceLine = claim.ServiceLine,
                        ServiceLineDiscount = serviceLineDiscount
                    };
                    result.CPTViewmodels.Add(dcpt);
                }
            }

            if (result.CPTViewmodels.Count == 0)
                return BadRequest("No available CPTs matched your request");

            if (result.CPTViewmodels.Count < ArbitIdNumbers.Length)
                return BadRequest($@"Not all claims contain eligible CPTs. Review each Arbit Id manually: {String.Join(',',ArbitIdNumbers)}");

            // Find the fees and attach them
            foreach(var fee in authority.Fees)
            {
                // NOTE: DueOn cannot be calculated until the Dispute is saved - Dispute submission date will be required or assumed at that time
                // The FeeRecipient + BaseFeeId are used to look up the algorithm used to calculate due date
                result.Fees.Add(new AuthorityDisputeFee { AmountDue = fee.FeeAmount, BaseFeeId = fee.Id, IsRefundable = fee.IsRefundable, IsRequired = fee.IsRequired, FeeRecipient = FeeRecipient.Authority });
            }
            
            // NOTE: Arbitrator fee(s) not assigned at this time since this is unknown when a Dispute is first created
            result.Authority = authority;

            await AttachBaseFeeObjectsAsync(result.Fees);

            return result;
        }

        /// <summary>
        /// This method replaces the old, slow Search method.
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="inactive"></param>
        /// <param name="closed"></param>
        /// <returns></returns>
        [HttpPost("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<IEnumerable<AuthorityDispute>>> SearchAsync([FromBody] ArbitrationCase criteria, [FromQuery] bool inactive = false, [FromQuery] bool closed = false)
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
                    return new AuthorityDispute[] { };  // user has no access to the Customer's Cases so short-circuit the request
            }

            criteria.NSAWorkflowStatus = criteria.Status;

            // short-circuit the Authority search if the Authority is NSA
            criteria.Authority = criteria.Authority.ToLower();

            if (criteria.Authority == "*")
            {
                criteria.Authority = "";
            }

            var cases = new List<AuthorityDispute>();

            try
            {

                var tmp = criteria.Arbitrators.Count() > 0 ? criteria.Arbitrators.First() : null;

                // Dynamic criteria construction
                IQueryable<AuthorityDispute> searchQ = _context.AuthorityDisputes.AsQueryable(); // exclude deleted recs

                // By default, only return "active" Disputes
                if (criteria.Status == ArbitrationStatus.Search && criteria.NSAWorkflowStatus == ArbitrationStatus.Search)
                {
                    // criteria.Status of Search and NSAWorkflowStatus of Search means the user is asking for any match regardless of status (and may also want to search inactive or closed disputes, as well)
                    // these appear so redundant but result in a better query string sent to SQL  (possibly a better way but no time to poke LinqToSql to find it)
                    searchQ = searchQ.AddCondition(() => inactive && !closed, x => Utilities.OPEN_STATUSES.Contains(x.WorkflowStatus) || x.WorkflowStatus == ArbitrationStatus.Ineligible);
                    searchQ = searchQ.AddCondition(() => !inactive && !closed, x => Utilities.OPEN_STATUSES.Contains(x.WorkflowStatus));
                    searchQ = searchQ.AddCondition(() => !inactive && closed, x => Utilities.CLOSED_STATUSES.Contains(x.WorkflowStatus));
                }
                else
                {
                    searchQ = searchQ.AddCondition(() => true, x => x.WorkflowStatus == criteria.Status);
                }

                /* Add other criteria if provided */
                //searchQ = searchQ.AddCondition(() => criteria.AuthorityCaseId == "{empty}", x => x.AuthorityCaseId == ""); // defunct - would now violate db rules
                //searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.Authority) && !string.IsNullOrEmpty(criteria.AuthorityCaseId) && criteria.AuthorityCaseId != "{empty}", x => x.AuthorityCaseId == criteria.AuthorityCaseId);

                if (!string.IsNullOrEmpty(criteria.Authority) && criteria.Authority != "_") // "_" means to search for records where Authority is totally missing - no longer matters for db
                {
                    var AuthorityId = await _context.Authorities.Where(v => v.Key == criteria.Authority).Select(v => v.Id).FirstOrDefaultAsync();
                    searchQ = searchQ.AddCondition(() => true, x => x.AuthorityId == AuthorityId);
                    string AuthStatus = (criteria.Authority.ToLower() == "nsa") ? criteria.NSAStatus : criteria.AuthorityStatus;
                    searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(AuthStatus), x => x.AuthorityStatus == AuthStatus);
                }
                
                // more criteria - needs implementation of TrackingFields
                //searchQ = searchQ.AddCondition(() => criteria.ArbitrationBriefDueDate.HasValue && criteria.ArbitrationBriefDueDate.Value > DateTime.MinValue, x => x.BriefDueDate <= criteria.ArbitrationBriefDueDate!.Value);
                
                // allow searches for unassigned cases
                searchQ = searchQ.AddCondition(() => criteria.AssignedUser.ToLower().Equals("(unassigned)"), x => x.BriefWriter == "" || x.BriefPreparer == "");
                searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.AssignedUser) && !criteria.AssignedUser.ToLower().Equals("(unassigned)"), x => x.BriefPreparer == criteria.AssignedUser || x.BriefWriter == criteria.AssignedUser);

                //searchQ = searchQ.AddCondition(() => criteria.AssignmentDeadlineDate.HasValue && criteria.AssignmentDeadlineDate.Value > DateTime.MinValue, x => x.AssignmentDeadlineDate.HasValue && x.AssignmentDeadlineDate.Value.Date == criteria.AssignmentDeadlineDate!.Value.Date);

                //searchQ = searchQ.AddCondition(() => criteria.Customer == "{empty}", x => x.Customer == "");
                //searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.Customer) && criteria.Customer != "{empty}", x => x.Customer == criteria.Customer);

                //searchQ = searchQ.AddCondition(() => criteria.EHRNumber == "{empty}", x => x.EHRNumber == "");
                //searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.EHRNumber) && criteria.EHRNumber != "{empty}", x => x.EHRNumber == criteria.EHRNumber);
                /*
                if (!string.IsNullOrEmpty(criteria.Payor) && criteria.Payor != "{empty}")
                {
                    var payor = await _context.Payors.FirstOrDefaultAsync(d => d.Name == criteria.Payor);
                    var parentId = payor == null ? -1 : payor.Id;
                    searchQ = from d in searchQ
                              from p in _context.Set<Payor>().Where(pa => pa.ParentId == parentId)
                              where d.PayorId == p.Id
                              select d;
                }
                */
                //searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.PatientName) && criteria.PatientName != "{empty}", x => x.PatientName.Contains(criteria.PatientName));
                //searchQ = searchQ.AddCondition(() => criteria.PayorClaimNumber == "{empty}", x => x.PayorClaimNumber == "");
                //searchQ = searchQ.AddCondition(() => !string.IsNullOrEmpty(criteria.PayorClaimNumber) && criteria.PayorClaimNumber != "{empty}", x => x.PayorClaimNumber == criteria.PayorClaimNumber);
                searchQ = searchQ.AddCondition(() => criteria.RequestDate.HasValue && criteria.RequestDate.Value > DateTime.MinValue, x => x.SubmissionDate == criteria.RequestDate!.Value);

                // var SQL = searchQ.ToQueryString(); use this for debugging until Visual Studio fixes the immediate window string truncation bug

                /* current disputes approach only allows for one arbitrator i.e. "certified entity" instead of the multiple-arbitrator-elimination sequence 
                if (temp != null)
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

                    cases = await (from d in searchQ select d).Include(x => x.Arbitrator).AsNoTracking().AsSplitQuery().ToArrayAsync();
                }
                */

                // Take the query we've constructed and add some aggregation
                var SQL = searchQ.ToQueryString();
                int countLimit = user.IsSystem ? 10000 : 2000;

                // This SqlRaw approach using server-side aggregation functions is much faster and much simpler
                // than a complex LINQ expression tree if one can even be constructed that works.
                // Ain't nobody got time for date
                var RawQ = new StringBuilder();
                RawQ.AppendLine(SQL.Split("SELECT ")[0]);
                RawQ.Append(" ");
                RawQ.Append($@"SELECT TOP {countLimit + 1} a.[Id]
	                          ,a.ArbitratorId
	                          ,a.ArbitratorSelectedOn
	                          ,a.ArbitrationResult
                              ,a.[AuthorityId]
	                          ,a.[AuthorityCaseId]
                              ,a.[AuthorityStatus]
                              ,a.BriefApprovedBy
	                          ,a.BriefApprovedOn
	                          ,a.BriefPreparationCompletedOn
	                          ,a.BriefPreparer
	                          ,a.BriefWriter
	                          ,a.BriefWriterCompletedOn
	                          ,a.CreatedBy
	                          ,a.CreatedOn
	                          ,a.IneligibilityAction
	                          ,a.SubmissionDate
	                          ,a.TrackingValues
                              ,a.UpdatedBy
	                          ,a.UpdatedOn
	                          ,a.WorkflowStatus
	                          ,STRING_AGG(ac.Customer,';') as Customers
	                          ,STRING_AGG(ac.Id,';') as LinkedClaimIDs
                              ,STRING_AGG(ac.PatientName,';') as PatientNames
 
                          FROM [dbo].[AuthorityDisputes] a
                          INNER JOIN dbo.AuthorityDisputeCPTs c on a.Id = c.AuthorityDisputeId
                          INNER JOIN dbo.ClaimCPT t on c.ClaimCPTId = t.Id
                          INNER JOIN dbo.ArbitrationCases ac on t.ArbitrationCaseId = ac.Id
                          WHERE ");

                RawQ.Append(SQL.Split("WHERE ")[1]);
                RawQ.AppendLine(" ");
                RawQ.AppendLine(@"GROUP BY a.[Id]
	                          ,a.ArbitratorId
	                          ,a.ArbitratorSelectedOn
	                          ,a.ArbitrationResult
                              ,a.[AuthorityId]
	                          ,a.[AuthorityCaseId]
                              ,a.[AuthorityStatus]
                              ,a.BriefApprovedBy
	                          ,a.BriefApprovedOn
	                          ,a.BriefPreparationCompletedOn
	                          ,a.BriefPreparer
	                          ,a.BriefWriter
	                          ,a.BriefWriterCompletedOn
	                          ,a.CreatedBy
	                          ,a.CreatedOn
	                          ,a.IneligibilityAction
	                          ,a.SubmissionDate
	                          ,a.TrackingValues
                              ,a.UpdatedBy
	                          ,a.UpdatedOn
	                          ,a.WorkflowStatus");

                //cases = await _context.AuthorityDisputeVMs.FromSqlRaw(RawQ.ToString()).IgnoreQueryFilters().AsNoTracking().ToArrayAsync();
                //cases = await (from d in searchQ select d).Include(x => x.Arbitrator).AsNoTracking().AsSplitQuery().ToArrayAsync();

                using (var conn = new SqlConnection(_context.Database.GetConnectionString()))
                using (var cmd = conn.CreateCommand())
                {
                    await conn.OpenAsync();
                    cmd.CommandText = RawQ.ToString();
                    cmd.CommandType = System.Data.CommandType.Text;
                    
                    var rdr = await cmd.ExecuteReaderAsync();
                    while (await rdr.ReadAsync())
                    {
                        cases.Add(new AuthorityDispute
                        {
                            Id = rdr.GetInt32(0),
                            ArbitratorId = rdr.IsDBNull(1) ? null : rdr.GetInt32(1),
                            ArbitratorSelectedOn = rdr.IsDBNull(2) ? null : rdr.GetDateTime(2),
                            ArbitrationResult = Enum.Parse<ArbitrationResult>(rdr.GetString(3)),
                            AuthorityId = rdr.GetInt32(4),
                            AuthorityCaseId = rdr.GetString(5),
                            AuthorityStatus = rdr.GetString(6),
                            BriefApprovedBy = rdr.GetString(7),
                            BriefApprovedOn = rdr.IsDBNull(8) ? null : rdr.GetDateTime(8),
                            BriefPreparationCompletedOn = rdr.IsDBNull(9) ? null : rdr.GetDateTime(9),
                            BriefPreparer = rdr.GetString(10),
                            BriefWriter = rdr.GetString(11),
                            BriefWriterCompletedOn = rdr.IsDBNull(12) ? null : rdr.GetDateTime(12),
                            CreatedBy = rdr.GetString(13),
                            CreatedOn = rdr.IsDBNull(14) ? null : rdr.GetDateTime(14),
                            IneligibilityAction = rdr.GetString(15),
                            SubmissionDate = rdr.GetDateTime(16),
                            TrackingValues = rdr.GetString(17),
                            UpdatedBy = rdr.GetString(18),
                            UpdatedOn = rdr.IsDBNull(19) ? null : rdr.GetDateTime(19),
                            WorkflowStatus = Enum.Parse<ArbitrationStatus>(rdr.GetString(20)),
                            Customers = rdr.GetString(21),
                            LinkedClaimIDs = rdr.GetString(22),
                            PatientNames = rdr.GetString(23)
                        });
                    }
                }

                if (cases.Count() > countLimit)
                    return BadRequest($@"Your search would return over {countLimit} records. Please add more specific criteria.");

                if (cases.Count() > 0)
                {
                    /*
                    foreach (var result in cases)
                    {
                        foreach (var arbitrator in result.Arbitrators)
                        {
                            arb.Arbitrator = await _context.Arbitrators.FindAsync(arbitrator.ArbitratorId);
                        }
                    }
                    */
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
        #endregion

        #region DELETE routes
        // DELETE - DELETE an existing AuthorityDisputeFee
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<AuthorityDispute>> DeleteDisputeFeeAsync(int id)
        {
            try
            {
                var u = await GetCurrentUser();
                if (u == null)
                    return Unauthorized("No active User context!");

                if (!u.IsManager && !u.IsNegotiator && !u.IsSystem)
                    return BadRequest("Only Managers and Negotiators can delete AuthorityDisputeFees.");

                if (id < 1)
                    return BadRequest("Bad Id.");

                var fee = await _context.AuthorityDisputeFees.FindAsync(id);
                if (fee == null)
                    return NotFound();
                _context.Set<AuthorityDisputeFee>().Remove(fee);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    return BadRequest(ex.InnerException.Message);
                return BadRequest(ex.Message);
            }
        }
        
        /// <summary>
        /// Delete the BLOB and the db entry
        /// </summary>
        /// <param name="aaid">AuthorityAttachmentId</param>
        /// <param name="did">Dispute Id</param>
        /// <param name="dt">Document Type</param>
        /// <param name="name">Document Name</param>
        /// <returns></returns>
        [HttpDelete]
        [Route("blob")]
        public async Task<IActionResult> DeleteAttachmentAsync([FromQuery] int aaid, [FromQuery] int did, [FromQuery] string dt, [FromQuery] string name)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (aaid < 1 || did < 1 || string.IsNullOrEmpty(dt) || string.IsNullOrEmpty(name))
                return BadRequest("Invalid parameters");

            if (!user.IsManager && !user.IsNegotiator && !user.IsSystem)
                return Unauthorized("Insufficient privileges to update a Dispute");
            
            // verify permissions and enforce some integrity checks to prevent wild card deleting
            var orig = await _context.AuthorityDisputes.Include(d => d.Attachments.Where(b => b.Id == aaid && b.DocType == dt)).FirstOrDefaultAsync(d => d.Id == did);
            if (orig == null || orig.Attachments.Count != 1)
                return NotFound("Attachment record not found");

            var attachment = orig.Attachments.First();
            if (!attachment.BLOBName.Equals(name))
                return BadRequest("The specified name no longer matches the name on file.");

            try
            {
                BlobClient blob = _containerClient.GetBlobClient(name);
                var result = await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
                if (result)
                {
                    try
                    {
                        attachment.IsDeleted = true;
                        attachment.UpdatedOn = Utilities.GetCurrentUtcDate();
                        attachment.UpdatedBy = user.Email;
                        await _context.SaveChangesAsync();   
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message, ex);
                        // swallow error - the AuthorityDisputeAttachments list can never be definitive due to cloud storage so why worry about EF error after BLOB was removed?
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
        #endregion

        #region PUT routes
        // PUT - Update an existing AuthorityDispute
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<AuthorityDispute>> UpdateDisputeAsync([FromBody] AuthorityDispute dispute)
        {
            try
            {
                var u = await GetCurrentUser();
                if (u == null)
                    return Unauthorized("No active User context!");

                if (!u.IsManager && !u.IsNegotiator && !u.IsSystem)
                    return BadRequest("Only Managers and Negotiators can update AuthorityDisputes.");

                if (dispute.Id < 1)
                    return BadRequest("Cannot create new AuthorityDispute on this endpoint.");

                if (dispute.AuthorityId < 1 || string.IsNullOrEmpty(dispute.AuthorityCaseId))
                    return BadRequest("Authority Disputes require an AuthorityId and AuthorityCaseId.");

                if (dispute.CPTViewmodels.Count == 0)
                    return BadRequest("CPTViewmodels are required.");

                if (dispute.DisputeCPTs.Count > 0)
                    return BadRequest("DisputeCPTs are not used for updating AuthorityDisputes. Please submit an AuthorityDisputeCPTVM collection attached to the AuthorityDispute.");

                if (dispute.CPTViewmodels.FirstOrDefault(d => d.ClaimCPTId < 1) != null)
                    return BadRequest("Each CPTViewmodel must reference an existing ClaimCPT record.");

                var original = await _context.AuthorityDisputes.Include(d => d.DisputeCPTs).ThenInclude(b => b.ClaimCPT)
                                                            .Include(d => d.Attachments.Where(b => !b.IsDeleted))
                                                            .Include(d => d.Fees)
                                                            .Include(d => d.Notes)
                                                            .FirstOrDefaultAsync(d => ACTIVE_WORKFLOW_STATES.Contains(d.WorkflowStatus)
                                                                                     && d.AuthorityId == dispute.AuthorityId
                                                                                     && d.AuthorityCaseId == dispute.AuthorityCaseId
                                                                                     && d.Id == dispute.Id);
                
                if (original == null)
                    return NotFound("Cannot locate an active AuthorityDispute matching the specified criteria.");

                if (original.DisputeCPTs.Count != dispute.CPTViewmodels.Count)
                    return BadRequest("CPTViewmodel mismatch!");


                var authority = await _context.Authorities.Include(d => d.TrackingDetails.Where(b => !b.IsDeleted && (b.Scope == AuthorityTrackingDetailScope.All || b.Scope == AuthorityTrackingDetailScope.AuthorityDispute))).FirstOrDefaultAsync(d => d.Id == original.AuthorityId && d.IsActive);
                if (authority == null)
                    return BadRequest("Unable to locate the original Authority or the Authority's Arbitration process is no longer active.");

                if(authority.TrackingDetails.Count == 0)
                    return BadRequest("Authority tracking configuration is not available. Cannot save changes.");

                var values = authority.StatusValues.ToLower().Split(new char[] { ',', ';' });
                if (!values.Contains(dispute.AuthorityStatus.ToLower()))
                    return BadRequest("Invalid Authority Status");

                bool isNSA = authority.Key.ToLower() == "nsa";

                if (isNSA && !u.IsNSA && !u.IsSystem)
                    return BadRequest("Your account is not authorized to work with NSA claims.");

                if (authority.Key.ToLower() != "nsa" && !u.IsState && !u.IsSystem)
                    return BadRequest("Your account is not authorized to work with State claims.");

                var clone = original.Clone();
                
                if (dispute.Arbitrator != null)
                {
                    var arb = await _context.Arbitrators.FirstOrDefaultAsync(d => d.Id == dispute.Arbitrator.Id && d.IsActive);
                    if (arb == null) 
                        return BadRequest("Unable to locate an active Arbitrator for the specified Id.");
                    if (!dispute.ArbitratorSelectedOn.HasValue)
                        dispute.ArbitratorSelectedOn = Utilities.GetCurrentUtcDate();
                }

                await AttachBaseFeeObjectsAsync(dispute.Fees);
                if (dispute.Fees.Count(d => d.BaseFee == null) > 0)
                    return BadRequest("Invalid BaseFeeId");

                foreach (var fee in dispute.Fees)
                {
                    if (fee.AuthorityDisputeId == 0) return BadRequest("AuthorityDisputeId is required when adding an AuthorityDisputeFee");
                    if (fee.BaseFeeId == 0) return BadRequest("Invalid BaseFeeId");
                    if (fee.AmountDue == 0) return BadRequest("AmountDue for a fee cannot be zero!");
                }
                // End of validation

                // Persist block
                await Extensions.EnsureHolidays(_context);
                var updatedOn = Utilities.GetCurrentUtcDate();

                try
                {
                    original.ArbitratorId = dispute.Arbitrator == null ? null : dispute.Arbitrator.Id;
                    original.ArbitratorSelectedOn = dispute.Arbitrator == null ? null : dispute.ArbitratorSelectedOn;
                    original.AuthorityStatus = dispute.AuthorityStatus;
                    original.BriefApprovedBy = dispute.BriefApprovedBy;
                    original.BriefApprovedOn = dispute.BriefApprovedOn;
                    original.BriefPreparationCompletedOn = dispute.BriefPreparationCompletedOn;
                    original.BriefPreparer = dispute.BriefPreparer;
                    original.BriefWriter = dispute.BriefWriter;
                    original.BriefWriterCompletedOn = dispute.BriefWriterCompletedOn;
                    original.IneligibilityAction = dispute.IneligibilityAction;
                    original.IneligibilityReasons = dispute.IneligibilityReasons;
                    original.WorkflowStatus = dispute.WorkflowStatus;

                    var ValueNode = JsonNode.Parse(dispute.TrackingValues);
                    if (ValueNode == null)
                    {
                        dispute.TrackingValues = "{}";
                        ValueNode = JsonNode.Parse(dispute.TrackingValues);
                    }

                    Utilities.UpdateTrackingCalculations(ValueNode!, authority.TrackingDetails, original);
                    original.TrackingValues = ValueNode!.ToJsonString();

                    // Update Dispute CPTs (offers)
                    foreach (var c in dispute.CPTViewmodels)
                    {
                        var orig = original.DisputeCPTs.FirstOrDefault(d => d.ClaimCPTId == c.ClaimCPTId);
                        if (orig == null)
                            throw new Exception("One or more attached ClaimCPTs does not match the existing dispute.");

                        if (c.BenchmarkOverrideAmount != orig.BenchmarkOverrideAmount || c.FinalOfferAmount != orig.FinalOfferAmount || c.CalculatedOfferAmount != orig.CalculatedOfferAmount)
                        {
                            orig.UpdatedBy = u.Email;
                            orig.UpdatedOn = updatedOn;
                            orig.BenchmarkOverrideAmount = c.BenchmarkOverrideAmount;
                            orig.CalculatedOfferAmount = c.CalculatedOfferAmount;
                            orig.FinalOfferAmount = c.FinalOfferAmount;
                            _context.Entry(original).State = EntityState.Modified;
                        }
                    }

                    // Update Fees
                    foreach (var f in dispute.Fees)
                    {
                        var orig = original.Fees.FirstOrDefault(d => d.Id == f.Id);
                        if (orig == null && f.Id == 0 && f.BaseFeeId > 0)
                        {
                            orig = new AuthorityDisputeFee();
                            orig.AuthorityDisputeId = original.Id;
                            orig.BaseFeeId = f.BaseFeeId;
                        }

                        if (orig == null)
                            continue;

                        orig.AmountDue = f.AmountDue;
                        orig.DueOn = f.DueOn;
                        orig.InvoiceLink = f.InvoiceLink;
                        orig.IsRefundable = f.IsRefundable;
                        orig.IsRequired = f.IsRequired;
                        orig.PaidBy = f.PaidBy;
                        orig.PaidOn = f.PaidOn;
                        orig.PaymentMethod = f.PaymentMethod;
                        orig.PaymentReferenceNumber = f.PaymentReferenceNumber;
                        orig.PaymentRequestedOn = f.PaymentRequestedOn;
                        orig.RefundAmount = f.RefundAmount;
                        orig.RefundableAmount = f.RefundableAmount;
                        orig.RefundDueOn = f.RefundDueOn;
                        orig.RefundedOn = f.RefundedOn;
                        orig.RefundedTo = f.RefundedTo;
                        orig.RefundMethod = f.RefundMethod;
                        orig.RefundReferenceNumber = f.RefundReferenceNumber;
                        orig.RefundRequestedBy = f.RefundRequestedBy;
                        orig.RefundRequestedOn = f.RefundRequestedOn;
                        orig.WasRefunded = f.WasRefunded;
                        orig.WasRefundRequested = f.WasRefundRequested;

                        if (orig.Id == 0)
                        {
                            orig.DueOn = await CalculateFeeDueDateAsync(original, orig);
                            original.Fees.Add(orig);
                            _context.Entry(original).State = EntityState.Modified;
                        } 
                        else if(_context.Entry(orig).State == EntityState.Modified)
                        {
                            orig.UpdatedBy = u.Email;
                            orig.UpdatedOn = updatedOn;
                            _context.Entry(original).State = EntityState.Modified;
                        }
                    }

                    // Save changes and change log
                    if (_context.Entry(original).State == EntityState.Modified)
                    {
                        // NOTE: Intentionally letting the change capture section potentially fail and block the saving of changes to see if it can handle null values properly
                        var deep = new Dictionary<string,object?>(clone.DeeplyEquals(original)
                            .Where(d =>
                            {
                                if (d is null)
                                {
                                    throw new ArgumentNullException(nameof(d));
                                }

                                return !d.Path.EndsWith("UpdatedBy") && !d.Path.EndsWith("UpdatedOn") && !d.Path.StartsWith("Property ") && !d.ActualValue.ToString().StartsWith("MPArbitration.");
                            })
                            .Select(d => new KeyValuePair<string, object?>(d.Path.Replace('.','_'), d.ActualValue)));
                        var changes = JsonSerializer.Serialize(deep);
                        var changelog = new AuthorityDisputeLog
                        {
                            Action = "APIUpdate",
                            CreatedBy = u.Email,
                            CreatedOn = updatedOn,
                            Details = changes,
                            Id = 0
                        };
                        original.ChangeLog.Add(changelog);
                        // update the dispute itself
                        await _context.SaveChangesAsync();
                    }
                    dispute.Fees = original.Fees;

                    // Since dispute.Fees serves as a viewmodel for the client ui, attach any missing BaseFee objects
                    await AttachBaseFeeObjectsAsync(dispute.Fees);
                }
                catch (Exception ex)
                {
                    _context.ChangeTracker.Clear();
                    return BadRequest(ex.Message);
                }


                // Return the view models that were sent in since they've passed all the validation checks and we aren't calculating on the server-side at this point

                dispute.DisputeCPTs = new List<AuthorityDisputeCPT>(); // the client side does not get the raw DisputeCPTs back
                return dispute;

                /*
                // fetch the claims associated with the CPTs so we can build the view models that are returned on this route
                int[]? cptIDs = dispute.CPTViewmodels.Select(d => d.ClaimCPTId).ToArray();
                int[]? arbitIdNumbers = await _context.ClaimCPT.Where(d => cptIDs.Contains(d.Id)).Select(b=> b.ArbitrationCaseId).ToArrayAsync();
                var claims = await _context.ArbitrationCases.Where(d => arbitIdNumbers.Contains(d.Id)).ToArrayAsync();

                //var cptCodes = origCPTs.Select(d => d.CPTCode).Distinct();
                //var cptCode = cptCodes.Count() > 1 ? "*" : cptCodes.First();

                ArbitrationCase? claim = null;

                // validate the Claims and CPTs while making the DisputeCPT objects for submission
                foreach (var vm in dispute.CPTViewmodels.OrderBy(d => d.ClaimCPT.ArbitrationCaseId)) // performance enhancement
                {
                    var CPT = vm.ClaimCPT;
                    claim = claim == null || claim.Id != cpt.ArbitrationCaseId ? await _context.ArbitrationCases.Include(d => d.CPTCodes.Where(g => !g.isDeleted && g.IsIncluded)).FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == cpt.ArbitrationCaseId) : claim;
                    if (claim == null)
                        return BadRequest($@"Unable to find an active ArbitrationCase record with an id of {cpt.ArbitrationCaseId}");

                    var origCPT = claim.CPTCodes.FirstOrDefault(d => d.Id == cpt.Id && d.CPTCode.Equals(cpt.CPTCode, StringComparison.CurrentCultureIgnoreCase));
                    if (origCPT == null)
                        return BadRequest($@"Unable to find all of the attached ClaimCPTs. (Are they all associated with active Claims and marked eligible?)");

                    //referencedClaims.Add(claim);

                    // We only make AuthorityDisputeCPT on this Create endpoint. The client will pass an array of AuthorityDisputeCPT when doing the actual create or update
                    var dCPT = new AuthorityDisputeCPT
                    {
                        BenchmarkAmount = vm.BenchmarkAmount,
                        AddedBy = u.Email,
                        AddedOn = updatedOn,
                        AuthorityDisputeId = 0,
                        BenchmarkDataItemId = 0,
                        BenchmarkDatasetId = 0,
                        BenchmarkOverrideAmount = vm.BenchmarkOverrideAmount, // Math.Ceiling(cpt.ProviderCharges, BenchmarkAmount) as a default
                        CalculatedOfferAmount = vm.CalculatedOfferAmount,
                        FinalOfferAmount = vm.FinalOfferAmount <= cpt.ProviderChargeAmount ? vm.FinalOfferAmount : cpt.ProviderChargeAmount,
                        ClaimCPTId = origCPT.Id,
                        UpdatedBy = u.Email,
                        UpdatedOn = updatedOn
                    };
                    dispute.DisputeCPTs.Add(dCPT);
                }

                foreach (var f in dispute.Fees)
                {
                    f.DueOn = await CalculateFeeDueDate(dispute, f);
                    f.UpdatedBy = u.Email;
                    f.UpdatedOn = u.UpdatedOn;
                    f.Id = 0;
                }

                // Save changes
                dispute.Authority = null;
                dispute.CreatedBy = u.Email;
                dispute.CreatedOn = Utilities.GetCurrentUtcDate();
                dispute.CPTViewmodels = new List<AuthorityDisputeCPTVM>(); // EF should ignore this
                dispute.WorkflowStatus = ArbitrationStatus.ActiveArbitrationBriefNeeded;

                _context.AuthorityDisputes.Add(dispute);
                await _context.SaveChangesAsync();

                // update the VMs to send back
                foreach (var CPT in dispute.CPTViewmodels)
                {
                    var dCPT = dispute.DisputeCPTs.FirstOrDefault(d => d.ClaimCPTId == cpt.ClaimCPTId);
                    if (dCPT == null)
                        continue; // shouldn't happen

                    cpt.AddedBy = dcpt.AddedBy;
                    cpt.AddedOn = dcpt.AddedOn;
                    cpt.FinalOfferAmount = dcpt.FinalOfferAmount;
                    cpt.Id = dcpt.Id;
                    cpt.UpdatedBy = dcpt.UpdatedBy;
                    cpt.UpdatedOn = dcpt.UpdatedOn;

                }
                
                dispute.DisputeCPTs = new List<AuthorityDisputeCPT>(); // the client side does not get the raw DisputeCPTs via this endpoint
                return dispute;
                */
            }
            catch (Exception ex)
            {
                if (ex.InnerException == null)
                    return BadRequest(ex.Message);
                return BadRequest(ex.InnerException.Message);
            }
        }

        [HttpPut("queue/complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult> UpdateDisputeQueueItemAsync([FromBody] AuthorityDisputeWorkItem Item)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            if (!u.IsManager && !Item.AssignedUser.Equals(u.Email, StringComparison.CurrentCultureIgnoreCase))
                return Unauthorized();

            if (Item.DisputeId < 1 || string.IsNullOrEmpty(Item.AssignedUser) || Item.WorkQueue == WorkQueueName.All || Item.WorkQueue == WorkQueueName.None)
                return BadRequest("Invalid or missing parameter");

            try
            {
                var Orig = await _context.AuthorityDisputes.FindAsync(Item.DisputeId);
                if (Orig == null)
                    return NotFound("AuthorityDispute not found");

                bool hasNote = (Item.Note != null && !string.IsNullOrEmpty(Item.Note.Details));

                if (hasNote && Item.Note!.AuthorityDisputeId != Orig.Id)
                    return BadRequest("Attached note has an invalid AuthorityDisputeId.");

                var UpdateTime = Utilities.GetCurrentUtcDate();

                switch (Item.WorkQueue)
                {
                    case WorkQueueName.DisputeApprover:
                        if (!u.IsManager && !u.IsBriefApprover)
                            return Unauthorized();
                        
                        if (Utilities.CLOSED_STATUSES.Contains(Orig.WorkflowStatus))
                            return BadRequest("Cannot approve briefs for closed disputes.");

                        Orig.BriefApprovedBy = u.Email;
                        Orig.BriefApprovedOn = UpdateTime;
                        break;

                    case WorkQueueName.DisputeBriefPreparer:
                        if (!Orig.BriefPreparer.Equals(Item.AssignedUser, StringComparison.CurrentCultureIgnoreCase))
                            return BadRequest("The currently-assigned user does not match.");

                        if (Orig.BriefPreparationCompletedOn != null)
                            return BadRequest("This item was previously completed!");

                        if (Orig.WorkflowStatus != ArbitrationStatus.ActiveArbitrationBriefNeeded)
                            return BadRequest("This item is no longer in the Brief Needed queue");

                        Orig.BriefPreparationCompletedOn = Utilities.GetCurrentUtcDate();
                        Orig.UpdatedBy = u.Email;
                        Orig.UpdatedOn = UpdateTime;
                        break;

                    case WorkQueueName.DisputeBriefWriter:
                        if (!Orig.BriefWriter.Equals(Item.AssignedUser, StringComparison.CurrentCultureIgnoreCase))
                            return BadRequest("The currently-assigned user does not match.");

                        if (Orig.BriefWriterCompletedOn!= null)
                            return BadRequest("This item was previously completed!");

                        if (Orig.WorkflowStatus != ArbitrationStatus.ActiveArbitrationBriefNeeded)
                            return BadRequest("This item is no longer in the Brief Needed queue");

                        Orig.WorkflowStatus = ArbitrationStatus.ActiveArbitrationBriefCreated;
                        Orig.BriefWriterCompletedOn = Utilities.GetCurrentUtcDate();
                        Orig.UpdatedBy = u.Email;
                        Orig.UpdatedOn = UpdateTime;
                        break;

                    default:
                        return BadRequest("Unsupported action");
                }

                // add note
                if (hasNote)
                {
                    Item.Note!.UpdatedBy = u.Email;
                    Item.Note!.UpdatedOn = UpdateTime;
                    _context.AuthorityDisputeNotes.Add(Item.Note!);

                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch(Exception ex)
            {
                if (ex.InnerException != null)
                    return BadRequest(ex.InnerException.Message);

                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Lookup methods to support Batch preparation among other things - moved to BatchingController
        /* Temporarily disabling until certain there aren't any non-Arbit users hitting these endpoints. If they are,
         * tell them to use the new BatchingController endpoints.
        /// <summary>
        /// CurrentCases refer to cases that haven't been Settled, Closed or marked Ineligible, as well as cases
        /// without the IsDeleted flag set to true.
        /// </summary>
        /// <param name="c">Customer Id</param>
        /// <param name="p">Payor Id</param>
        /// <returns></returns>
        [HttpGet("lookup/providers")]
        public async Task<ActionResult<IEnumerable<string>>> GetProviderNPIsForOpenClaimsAsync([FromQuery] int c, [FromQuery] int p)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (c < 1 || p < 1)
                return BadRequest();

            var allowedCustomerIDs = new List<int>();
            List<string> allowedCustomerNames = new List<string>();
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }
            var eobDate = Utilities.GetCurrentUtcDate();
            var startDate = eobDate.AddWorkDays(-26);
            var endDate = eobDate.AddWorkDays(3);
            try
            {
                var cases = from s in _context.Set<ArbitrationCase>().Where(d =>
                            !d.IsDeleted &&
                            (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)) &&
                            string.IsNullOrEmpty(d.NSACaseId) &&
                            d.EOBDate >= startDate && 
                            d.EOBDate <= endDate &&
                            (d.NSAStatus == "Pending NSA Negotiation Request" || d.NSAStatus == "Submitted NSA Negotiation Request") && 
                            (d.NSAWorkflowStatus == ArbitrationStatus.DetermineAuthority ||
                             d.NSAWorkflowStatus == ArbitrationStatus.MissingInformation ||
                             d.NSAWorkflowStatus == ArbitrationStatus.New ||
                             d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress ||
                             d.NSAWorkflowStatus == ArbitrationStatus.Open) && 
                             !string.IsNullOrEmpty(d.ProviderNPI))
                            .Include(g => g.CPTCodes.Where(g => !g.isDeleted))
                            .Include(g => g.SettlementDetails.Where(t => !t.IsDeleted))
                            select s;

                cases.AddCondition(() => true, x => x.CPTCodes.Count > 0 && x.SettlementDetails.Count == 0);
                var results = await cases.Select(b => b.ProviderNPI).ToListAsync();
                return Ok(results.Distinct());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("lookup/claims")]
        public async Task<ActionResult<IEnumerable<ArbitrationCase>>> GetOpenClaimsForBatchBuildingAsync([FromQuery] int c, [FromQuery] int p, [FromQuery] string r)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (c < 1 || p < 1)
                return BadRequest();

            var allowedCustomerIDs = new List<int>();
            List<string> allowedCustomerNames = new List<string>();
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account.");
            }
            var eobDate = Utilities.GetCurrentUtcDate();
            var startDate = eobDate.AddWorkDays(-26);
            var endDate = eobDate.AddWorkDays(3);
            try
            {
                var cases = from s in _context.Set<ArbitrationCase>().Where(d =>
                            !d.IsDeleted &&
                            (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)) &&
                            string.IsNullOrEmpty(d.NSACaseId) &&
                            d.EOBDate >= startDate &&
                            d.EOBDate <= endDate &&
                            d.ProviderNPI == r &&
                            (d.NSAStatus == "Pending NSA Negotiation Request" || d.NSAStatus == "Submitted NSA Negotiation Request") &&
                            (d.NSAWorkflowStatus == ArbitrationStatus.DetermineAuthority ||
                             d.NSAWorkflowStatus == ArbitrationStatus.MissingInformation ||
                             d.NSAWorkflowStatus == ArbitrationStatus.New ||
                             d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress ||
                             d.NSAWorkflowStatus == ArbitrationStatus.Open) &&
                             !string.IsNullOrEmpty(d.ProviderNPI))
                            .Include(g => g.CPTCodes.Where(g => !g.isDeleted))
                            .Include(g => g.SettlementDetails.Where(t => !t.IsDeleted))
                            select s;

                cases.AddCondition(() => true, x => x.CPTCodes.Count > 0 && x.SettlementDetails.Count == 0);
                var results = await cases.ToListAsync();
                return Ok(results.Distinct());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        */
        #endregion

        #region Private methods
        private async Task AttachBaseFeeObjectsAsync(IEnumerable<IDisputeFee> fees)
        {
            foreach (var fee in fees.Where(d => d.BaseFeeId >0))
            {
                if (fee.FeeRecipient == FeeRecipient.Arbitrator)
                {
                    fee.BaseFee = await _context.ArbitratorFees.FindAsync(fee.BaseFeeId);
                }
                else if(fee.FeeRecipient == FeeRecipient.Authority)
                {
                    fee.BaseFee = await _context.AuthorityFees.FindAsync(fee.BaseFeeId);
                }
            }
        }

        private async Task<DateTime?> CalculateFeeDueDateAsync(AuthorityDispute dispute, AuthorityDisputeFee fee)
        {
            if (fee.BaseFeeId < 1)
                return fee.DueOn;

            var feeDef = fee.FeeRecipient == FeeRecipient.Authority ? await _context.AuthorityFees.AsNoTracking().Where(d => d.Id == fee.BaseFeeId).FirstOrDefaultAsync<BaseFee>() : await _context.ArbitratorFees.AsNoTracking().Where(d => d.Id == fee.BaseFeeId).FirstOrDefaultAsync<BaseFee>();

            if (feeDef == null || feeDef.DueDaysAfterColumnName < 1 || !feeDef.ReferenceColumnName.Equals("SubmissionDate"))
                return fee.DueOn;

            return feeDef.DueDayType == DeadlineType.CalendarDays ? dispute.SubmissionDate.AddDays(feeDef.DueDaysAfterColumnName) : dispute.SubmissionDate.AddWorkDays(feeDef.DueDaysAfterColumnName);
        }

        private AuthorityDisputeCPTVM CreateDisputeCPTVM(Authority authority, AuthorityDisputeCPT cpt, ArbitrationCase claim)
        {
            var vm = new AuthorityDisputeCPTVM
            {
                AddedBy = cpt.AddedBy,
                AddedOn = cpt.AddedOn,
                AuthorityDisputeId = cpt.AuthorityDisputeId,
                BenchmarkAmount = cpt.BenchmarkAmount,
                BenchmarkDataItemId = cpt.BenchmarkDataItemId,
                BenchmarkDatasetId = cpt.BenchmarkDatasetId,
                BenchmarkOverrideAmount = cpt.BenchmarkOverrideAmount,
                CalculatedOfferAmount = cpt.CalculatedOfferAmount,
                ClaimCPT = cpt.ClaimCPT,
                ClaimCPTId = cpt.ClaimCPTId,
                FinalOfferAmount = cpt.FinalOfferAmount,
                Id = cpt.Id,
                ServiceLineDiscount = cpt.ServiceLineDiscount,
                UpdatedBy = cpt.UpdatedBy,
                UpdatedOn = cpt.UpdatedOn
            };

            vm.Customer = claim.Customer;
            vm.GeoRegion = GetGeoRegion(claim.LocationGeoZip);
            vm.NotificationDate = GetNotificationDate(authority, claim);
            vm.Entity = claim.Entity;
            vm.EntityNPI = claim.EntityNPI;
            vm.GeoZip = claim.LocationGeoZip;
            vm.Payor = claim.PayorEntity?.Name ?? claim.Payor;
            vm.PayorClaimNumber = claim.PayorClaimNumber;
            vm.PayorId = claim.PayorEntity?.Id ?? null;
            vm.PlanType = claim.PlanType;
            vm.ProviderName = claim.ProviderName;
            vm.ProviderNPI = claim.ProviderNPI;
            vm.ServiceDate = claim.ServiceDate;
            vm.ServiceLine = claim.ServiceLine;

            return vm;
        }

        /// <summary>
        /// Returns a list of AuthorityDisputes across all Authorities for a given set of EHR Claims. Active means the WorkflowStatus 
        /// has not reached a terminal condition.
        /// </summary>
        /// <param name="EHRClaims">The ArbitrationCaseId(s) (EHR data) that are in dispute.</param>
        /// <param name="CPTCode">The individual CPT in question or asterisk for all </param>
        /// <returns></returns>
        private async Task<string[]?> GetActiveDisputeIdsForASetOfClaimNumbersAsync(IEnumerable<int> EHRClaims, string CPTCode)
        {
            
            var disputes = (from c in _context.AuthorityDisputeCPTs
                            join d in _context.AuthorityDisputes on c.AuthorityDisputeId equals d.Id
                            join t in _context.ClaimCPT on c.ClaimCPTId equals t.Id
                            where EHRClaims.Contains(t.ArbitrationCaseId)
                                && ACTIVE_WORKFLOW_STATES.Contains(d.WorkflowStatus)
                                && (CPTCode == "*" || c.ClaimCPT.CPTCode == CPTCode)
                            select d.AuthorityCaseId);

            return await disputes.ToArrayAsync(); 
        }

        /// <summary>
        /// Checks a list of ArbitrationCase record numbers (aka EHR data) and determines which
        /// one(s) have an AuthorityCaseId or NSACaseId plus a Workflow Status that is active.
        /// </summary>
        /// <param name="EHRClaims"></param>
        /// <returns></returns>
        private async Task<string[]?> CheckArbitIdsForActiveCasesAsync(IEnumerable<int> EHRClaims)
        {
            ArbitrationStatus[] activeStates = { ArbitrationStatus.ActiveArbitrationBriefCreated, ArbitrationStatus.ActiveArbitrationBriefNeeded, ArbitrationStatus.ActiveArbitrationBriefSubmitted, ArbitrationStatus.PendingArbitration, ArbitrationStatus.InformalInProgress };
            var headers = (from c in _context.ArbitrationCases
                           .Where(d => !d.IsDeleted && ( (d.AuthorityCaseId != "" && activeStates.Contains(d.Status))
                                                        || (d.NSACaseId != "" && activeStates.Contains(d.NSAWorkflowStatus))))
                           where EHRClaims.Contains(c.Id)
                           select c.Id.ToString());

            return await headers.ToArrayAsync();
        }

        /// <summary>
        /// Authority-specific queries to return the list of claims and associated CPTs
        /// </summary>
        /// <param name="authority"></param>
        /// <param name="ArbitIdNumbers"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private async Task<List<ArbitrationCase>> GetAuthorityClaimsForBatchAsync(Authority authority, int[] ArbitIdNumbers, AuthorityDisputeInit values)
        {
            List<ArbitrationCase>? claims = null;
            if (authority.Key.ToLower() == "nsa")
            {
                claims = await _context.ArbitrationCases.Include(d => d.CPTCodes.Where(c => !c.isDeleted && c.IsIncluded && (values.CPT == "*" || c.CPTCode == values.CPT)))
                                                        .Include(d => d.PayorEntity)
                                                        .AsSplitQuery().AsNoTracking()
                                                        .Where(d => !d.IsDeleted && d.NSAStatus == "Submitted NSA Negotiation Request" && ArbitIdNumbers.Contains(d.Id))
                                                        .ToListAsync();
            }
            else
            {
                // TODO: Next query is probably just pseudo-code considering the uncertain nature of the AuthorityStatus and authority rules in the various state jurisdictions
                claims = await _context.ArbitrationCases.Include(d => d.CPTCodes.Where(c => !c.isDeleted && c.IsIncluded && (values.CPT == "*" || c.CPTCode == values.CPT)))
                                                        .Include(d => d.PayorEntity)
                                                        .AsSplitQuery().AsNoTracking()
                                                        .Where(d => !d.IsDeleted && d.Authority == authority.Key && d.AuthorityStatus == "Submitted Negotiation Request" && ArbitIdNumbers.Contains(d.Id))
                                                        .ToListAsync();
            }
            foreach(var c in claims)
            {
                if (c.PayorEntity == null || c.PayorEntity?.Id == c.PayorEntity?.ParentId)
                    continue;
                c.PayorEntity = await _context.Payors.FindAsync(c.PayorEntity!.ParentId);
            }
            return claims;
        }

        private double GetBenchmarkValueFromCPT(Authority authority, CalculatorVariable calcs, ArbitrationCase claim, ClaimCPT cpt)
        {
            var srcProps = cpt.GetType().GetProperties().Where(d => d.GetIndexParameters().Length == 0);

            // Explicit NSA path
            if (authority.Key.ToLower() == "nsa")
            {
                if (string.IsNullOrEmpty(calcs.NSAOfferBaseValueFieldname))
                    return 0;

                var sourceProp = srcProps.FirstOrDefault(d => d.Name.Equals(calcs.NSAOfferBaseValueFieldname, StringComparison.CurrentCultureIgnoreCase));
                if (sourceProp == null)
                    return 0;

                var baseCharge = (double?)sourceProp.GetValue(cpt);

                if (baseCharge.HasValue)
                    return Math.Round(baseCharge.Value, 2);

                return 0;
            }
            else
            {
                // TODO: Figure out how to create default 
                return 0;
            }
        }

        private async Task<ArbitrationCase?> GetClaimForDisputeCPTAsync(AuthorityDisputeCPT cpt)
        {
            var Q = (from d in _context.ArbitrationCases.Where(d => d.Id == cpt.ClaimCPT.ArbitrationCaseId)
                     join pc in _context.Set<Payor>() on d.PayorId equals pc.Id
                     join pp in _context.Set<Payor>() on pc.ParentId equals pp.Id
                     //join pa in _context.Set<PayorAddress>() on pp.Id equals pa.PayorId
                     select new ArbitrationCase
                     {
                         Customer = d.Customer,
                         Entity = d.Entity,
                         EntityNPI = d.EntityNPI,
                         LocationGeoZip = d.LocationGeoZip,
                         NSATracking = d.NSATracking,
                         PayorEntity = pp,
                         PayorClaimNumber = d.PayorClaimNumber,
                         PayorId = pp.Id,
                         PlanType = d.PlanType,
                         ProviderName = d.ProviderName,
                         ProviderNPI = d.ProviderNPI,
                         ServiceDate = d.ServiceDate,
                         ServiceLine = d.ServiceLine,
                     });

            return await Q.AsNoTracking().FirstOrDefaultAsync();
        }

        /* This uses the terrible "discount override" value stored on the ArbitrationCase (claim). For a batch, we will now simply 
         * present the user with the configured global discount and they can override either the benchmark or the offer itself
         * when creating a dispute 
        private double GetDefaultOfferAmountForAuthority(Authority authority, CalculatorVariable calcs, ArbitrationCase claim, ClaimCPT cpt)
        {
            if (authority.Key.ToLower() == "nsa")
            {
                var baseCharge = GetBenchmarkValueFromCPT(authority, calcs, claim, cpt);

                double nsaDiscount = claim.NSARequestDiscount > 0 ? claim.NSARequestDiscount : calcs.NSAOfferDiscount;
                
                if (baseCharge > 0)
                {
                    var disc = 1 - nsaDiscount; // e.g. 1 - .3 = 70% of fh80th
                    return Math.Round(baseCharge * disc, 2);
                }

                return 0;
            } 
            else
            {
                // TODO: Figure out how to create default 
                return 0;
            }
        }
        */


        /// <summary>
        /// Not implemented. Waiting for this data to become available and loaded into the data store.
        /// </summary>
        /// <param name="zip"></param>
        /// <returns></returns>
        private string GetGeoRegion(string zip)
        {
            return string.Empty;
        }

        private DateTime? GetNotificationDate(Authority authority, ArbitrationCase claim)
        {
            if(authority.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase))
            {
                return Utilities.GetTrackingValue(claim.NSATracking, "DateNegotiationSent");
            } 
            else
            {
                return null;
            }
        }

        #endregion



        [HttpDelete]
        [Route("AuthorityDisputeCPT")]
        public async Task<IActionResult> DeleteAuthorityDisputeCPT([FromQuery] int authorityDisputeId, [FromQuery] int claimCPTId)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

              if (!user.IsManager && !user.IsNegotiator && !user.IsSystem)
                return Unauthorized("Insufficient privileges to update a Dispute");

            if (authorityDisputeId < 1 || claimCPTId < 1)
                return BadRequest("Invalid parameters");

            try
            {
                if(!_context.AuthorityDisputeCPTs.Any(x => x.AuthorityDisputeId == authorityDisputeId && x.ClaimCPTId == claimCPTId))
                    return NotFound();

                AuthorityDisputeCPT? rec = _context.AuthorityDisputeCPTs.FirstOrDefault(x => x.AuthorityDisputeId == authorityDisputeId && x.ClaimCPTId == claimCPTId);

                if (rec != null)
                {
                    _logger.LogInformation($"Deleting AuthorityDisputeCPT record for AuthorityDisputeId: {authorityDisputeId}, ClaimCPTId: {claimCPTId}");
                    var returned = _context.AuthorityDisputeCPTs.Remove(rec);
                    _context.SaveChanges();
                }
                
                return StatusCode(202);
            }
            catch (Exception ex)
            {
                _logger.LogError($@"Error deleting AuthorityDisputeCPT, AuthorityDisputeId: {authorityDisputeId} ClaimCPTId: {claimCPTId}");
                _logger.LogError(ex.Message);

                return BadRequest($@"Error deleting  from storage");
            }
        }
    }
}
