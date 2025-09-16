using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MPArbitration.Model;
using MPArbitration.Utility;
using NuGet.Configuration;
using System.Runtime.Intrinsics.Arm;

namespace MPArbitration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SettlementsController : MPBaseController
    {
        private readonly ILogger<CasesController> _logger;
        private IImportDataSynchronizer _synchonizer;

        #region Constructor
        public SettlementsController(ILogger<CasesController> logger, ArbitrationDbContext context, IConfiguration configuration, IImportDataSynchronizer synchronizer) : base(context, configuration)
        {
            _logger = logger;
            _synchonizer = synchronizer;
        }
        #endregion

        [HttpGet("{id}")]
        public async Task<ActionResult<CaseSettlement>> GetCaseSettlementAsync(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                var s = await _context.CaseSettlements.Include(d => d.CaseSettlementDetails).Include(d => d.CaseSettlementCPTs).FirstOrDefaultAsync(d => d.Id == id);
                if (s == null)
                    return NotFound();

                if (Utilities.FixRawCaseSettlementDates(new CaseSettlement[] { s }))
                {
                    await _context.SaveChangesAsync();
                }

                return Ok(s);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("find")]
        public async Task<ActionResult<IEnumerable<CaseSettlement>>> GetCaseSettlementsByCaseIdAsync([FromQuery] int arbId) // need a new Endpoint GetCaseSettlementsByAuthCaseId -> [FromQuery]string? authCaseId
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                var s = await _context.CaseSettlements.Include(d => d.CaseSettlementDetails).Include(d => d.CaseSettlementCPTs).Where(d => d.ArbitrationCaseId == arbId).ToArrayAsync();

                if (Utilities.FixRawCaseSettlementDates(s))
                {
                    await _context.SaveChangesAsync();
                }
                return Ok(s);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<CaseSettlement>> CreateCaseSettlementAsync(CaseSettlement settlement)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            if (settlement.ArbitrationCaseId < 1 || settlement.Id != 0 || settlement.PayorId < 1 || settlement.IsDeleted)
                return BadRequest("Bad parameters");
            if (settlement.AuthorityId == null || settlement.AuthorityId < 1)
                return BadRequest("Missing AuthorityId");

            // ignoring details for the moment since those are really "payments" and have their own path -> .Include(d => d.CaseSettlementDetails)
            try
            {
                var authority = await _context.Authorities.FindAsync(settlement.AuthorityId.Value);
                if (authority == null)
                    return BadRequest("Bad AuthorityId");

                // find the referenced parent and use it for some validation
                var arbCase = await _context.ArbitrationCases.FindAsync(settlement.ArbitrationCaseId);

                if (arbCase == null || arbCase.IsDeleted)
                    return BadRequest("Invalid parent reference");

                var allowedCustomerIDs = new List<int>();
                List<string> allowedCustomerNames = new List<string>();
                if (!user.HasGlobalCaseRole)
                {
                    allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer && (x.AccessLevel == UserAccessType.manager || x.AccessLevel == UserAccessType.negotiator)).Select(x => x.EntityId));
                    allowedCustomerNames = await _context.Customers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                    if (allowedCustomerNames.Count() == 0)
                        return Unauthorized("Customer records are not available to the current account.");
                }

                if (user.HasGlobalCaseRole && !user.IsManager && !user.IsNegotiator)
                    return Unauthorized("Insufficient global privileges to update a Case");
                else if (!user.HasGlobalCaseRole && !allowedCustomerNames.Contains(arbCase.Customer))
                    return Unauthorized("Insufficient granular privileges to update a Case");

                var name = user.Email;
                var update = Utilities.GetCurrentUtcDate();
                if (settlement.AuthorityCaseId == string.Empty)
                    settlement.AuthorityCaseId = null;

                // Accepting an existing offer? Find it and update it
                if (settlement.Offer != null)
                {
                    if (settlement.Offer.Id == 0)
                        return BadRequest("Unable to add new Offers through this endpoint.");
                    if(settlement.Offer.CaseSettlementId != settlement.Id)
                        return BadRequest("Invalid CaseSettlementId");
                    if (!settlement.Offer.WasOfferAccepted)
                        return BadRequest("When an OfferHistory object is attached to a CaseSettlement, WasOfferAccepted must be true.");

                    var offer = await _context.OfferHistory.FirstOrDefaultAsync(d => d.Id == settlement.Offer.Id);
                    if (offer == null)
                        return BadRequest("Invalid OfferHistory reference");
                    if (offer.CaseSettlementId != 0 && offer.CaseSettlementId != settlement.Id)
                        return BadRequest("The OfferHistory record appears to be linked to a different Settlement already.");

                    offer.WasOfferAccepted = true;
                    offer.Authority = authority.Key;
                    offer.UpdatedBy = name;
                    offer.UpdatedOn = update;
                    //offer.CaseSettlementId = settlement.Id;
                    settlement.Offer = offer;
                }

                settlement.UpdatedBy = name;
                settlement.UpdatedOn = update;

                // add a Log entry for the ArbitrationCase
                var entry = new CaseLog
                {
                    Action = "AddSettlement", // TODO: Make this an enum throughout the app
                    CreatedBy = name,
                    CreatedOn = update,
                    Details = "New CaseSettlement added",
                    Id = 0
                };
                arbCase.Log.Add(entry);
                

                // Update or Insert Settlement CPTs
                foreach (var childModel in settlement.CaseSettlementCPTs.Where(d => !d.IsDeleted))
                {
                    childModel.Id = 0; //childModel.ClaimCPTId this should get validated by db ForeignKeys
                    childModel.UpdatedOn = update;
                    childModel.UpdatedBy = name;
                }

                // cleanup
                settlement.CaseSettlementCPTs.RemoveAll(d => d.IsDeleted);

                // save
                _context.CaseSettlements.Add(settlement);
                await _context.SaveChangesAsync();
                if(settlement.Offer != null && settlement.Offer.CaseSettlementId != settlement.Id)
                {
                    settlement.Offer.CaseSettlementId = settlement.Id;
                    await _context.SaveChangesAsync();
                }
                return Ok(settlement);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settlement">CPTs belonging to multiple ArbitrationCase records will result in the creation of multiple settlements</param>
        /// <returns></returns>
        [HttpPost("multi")]
        public async Task<ActionResult<IEnumerable<CaseSettlement>>> CreateMultipleCaseSettlementsAsync(CaseSettlement[] settlements)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (user.HasGlobalCaseRole && !user.IsManager && !user.IsNegotiator && !user.IsSystem)
                return Unauthorized("Insufficient global privileges for this operation");

            if (settlements.Length == 0)
                return BadRequest();

            // validate them all first
            int authorityId = 0;
            foreach (var s in settlements)
            {
                if (string.IsNullOrEmpty(s.AuthorityCaseId))
                    return BadRequest("This endpoint only supports formal settlements. Plase supply the AuthorityCaseId for each.");
                if (s.Offer != null)
                    return BadRequest("Associating Offers formal settlements not supported on this endpoint");
                if (s.ArbitrationCaseId < 1 || s.Id != 0 || s.PayorId < 1 || s.IsDeleted)
                    return BadRequest("Bad parameters on one or more CaseSettlement objects");
                if (s.AuthorityId == null || s.AuthorityId < 1)
                    return BadRequest("Missing AuthorityId on one ore more CaseSettlement objects");
                if (authorityId != 0 && authorityId != s.AuthorityId)
                    return BadRequest("Endpoint only supports single-Authority batching.");
                if (s.CaseSettlementCPTs.Count == 0)
                    return BadRequest("Missing CaseSettlementCPTs for one or more CaseSettlement objects");
                if (s.CaseSettlementCPTs.FirstOrDefault(d => d.ClaimCPTId < 1) != null) 
                    return BadRequest("CaseSettlementCPT requires a linked ClaimCPTId");
                authorityId = s.AuthorityId.Value;
            }

            var authority = await _context.Authorities.FindAsync(authorityId);
            if (authority == null)
                return BadRequest("Bad AuthorityId");

            var ClaimIDs = settlements.Select(d => d.ArbitrationCaseId).Distinct();

            try
            {
                var allowedCustomerIDs = new List<int>();
                List<string> allowedCustomerNames = new List<string>();
                if (!user.HasGlobalCaseRole)
                {
                    allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer && (x.AccessLevel == UserAccessType.manager || x.AccessLevel == UserAccessType.negotiator)).Select(x => x.EntityId));
                    allowedCustomerNames = await _context.Customers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                    if (allowedCustomerNames.Count() == 0)
                        return Unauthorized("Customer records are not available to the current account.");
                }
                var Claims = await _context.ArbitrationCases.Where(d=>!d.IsDeleted && ClaimIDs.Contains(d.Id)).ToListAsync();
                if (ClaimIDs.Count() != Claims.Count) 
                    return BadRequest("One or more ArbitrationCase records could not be located or is deleted");

                var name = user.Email;
                var update = Utilities.GetCurrentUtcDate();

                foreach (var s in settlements)
                {
                    // find the referenced parent and use it for some validation
                    var arbCase = Claims.FirstOrDefault(d => d.Id == s.ArbitrationCaseId);

                    if (!user.HasGlobalCaseRole && !allowedCustomerNames.Contains(arbCase!.Customer))
                        return Unauthorized("Insufficient granular privileges to update a Case");

                    /* Accepting an existing offer? Find it and update it
                    if (settlement.Offer != null)
                    {
                        if (settlement.Offer.Id == 0)
                            return BadRequest("Unable to add new Offers through this endpoint.");
                        if (settlement.Offer.CaseSettlementId != settlement.Id)
                            return BadRequest("Invalid CaseSettlementId");
                        if (!settlement.Offer.WasOfferAccepted)
                            return BadRequest("When an OfferHistory object is attached to a CaseSettlement, WasOfferAccepted must be true.");

                        var offer = await _context.OfferHistory.FirstOrDefaultAsync(d => d.Id == settlement.Offer.Id);
                        if (offer == null)
                            return BadRequest("Invalid OfferHistory reference");
                        if (offer.CaseSettlementId != 0 && offer.CaseSettlementId != settlement.Id)
                            return BadRequest("The OfferHistory record appears to be linked to a different Settlement already.");

                        offer.WasOfferAccepted = true;
                        offer.Authority = authority.Key;
                        offer.UpdatedBy = name;
                        offer.UpdatedOn = update;
                        //offer.CaseSettlementId = settlement.Id;
                        settlement.Offer = offer;
                    }
                    */


                    s.UpdatedBy = name;
                    s.UpdatedOn = update;

                    var entry = new CaseLog
                    {
                        Action = "AddSettlement", // TODO: Make this an enum throughout the app
                        CreatedBy = name,
                        CreatedOn = update,
                        Details = "New CaseSettlement added",
                        Id = 0
                    };

                    arbCase!.Log.Add(entry);

                    foreach (var childModel in s.CaseSettlementCPTs.Where(d => !d.IsDeleted))
                    {
                        childModel.Id = 0; //childModel.ClaimCPTId this should get validated by db ForeignKeys
                        childModel.CaseSettlementId = 0;
                        childModel.UpdatedOn = update;
                        childModel.UpdatedBy = name;
                    }
                    // cleanup
                    s.CaseSettlementCPTs.RemoveAll(d => d.IsDeleted);
                    // save
                    _context.CaseSettlements.Add(s);

                }                


                await _context.SaveChangesAsync();
                /* Linked offers not yet supported
                if (settlement.Offer != null && settlement.Offer.CaseSettlementId != settlement.Id)
                {
                    settlement.Offer.CaseSettlementId = settlement.Id;
                    await _context.SaveChangesAsync();
                }
                */
                return Ok(settlements);
            }
            catch (Exception ex)
            {
                if(ex.InnerException!=null)
                    return BadRequest(ex.InnerException.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public async Task<ActionResult<CaseSettlement>> UpdateCaseSettlementAsync(CaseSettlement settlement)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            if (settlement.ArbitrationCaseId < 1 || settlement.Id < 1 || settlement.PayorId < 1 || settlement.IsDeleted)
                return BadRequest("Bad parameters");

            // ignoring details for the moment since those are really "payments" and have their own path -> .Include(d => d.CaseSettlementDetails)
            try
            {
                var orig = await _context.CaseSettlements.Include(d => d.CaseSettlementCPTs).FirstOrDefaultAsync(d => d.Id == settlement.Id);
                if (orig == null)
                    return NotFound("Settlement not found");

                // find the referenced parent and use it for some validation
                var arbCase = await _context.ArbitrationCases.FindAsync(settlement.ArbitrationCaseId);

                if (arbCase == null || arbCase.IsDeleted)
                    return BadRequest("Invalid parent reference");

                var allowedCustomerIDs = new List<int>();
                List<string> allowedCustomerNames = new List<string>();
                if (!user.HasGlobalCaseRole)
                {
                    allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer && (x.AccessLevel == UserAccessType.manager || x.AccessLevel == UserAccessType.negotiator)).Select(x => x.EntityId));
                    allowedCustomerNames = await _context.Customers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                    if (allowedCustomerNames.Count() == 0)
                        return Unauthorized("Customer records are not available to the current account.");
                }

                if (user.HasGlobalCaseRole && !user.IsManager && !user.IsNegotiator)
                    return Unauthorized("Insufficient global privileges to update a Case");
                else if (!user.HasGlobalCaseRole && !allowedCustomerNames.Contains(arbCase.Customer))
                    return Unauthorized("Insufficient granular privileges to update a Case");

                var name = user.Email;
                var update = Utilities.GetCurrentUtcDate();

                settlement.UpdatedBy = orig.UpdatedBy;
                settlement.UpdatedOn = orig.UpdatedOn; // make sure something other than the updated values changed

                // add a Log entry for the ArbitrationCase
                var changes = Utilities.DetectDifferencesBetweenObjects(orig, settlement);
                if (!string.IsNullOrEmpty(changes))
                {
                    var entry = new CaseLog
                    {
                        Action = "SettlementUpdate", // TODO: Make this an enum throughout the app
                        CreatedBy = name,
                        CreatedOn = update,
                        Details = changes,
                        Id = 0
                    };
                    arbCase.Log.Add(entry);
                }

                _context.Entry(orig).CurrentValues.SetValues(settlement);
                if (_context.Entry(orig).State == EntityState.Modified)
                {
                    orig.UpdatedBy = name;
                    orig.UpdatedOn = update;
                }


                // Update or Insert Settlement CPTs
                foreach (var childModel in settlement.CaseSettlementCPTs)
                {
                    var existingChild = orig.CaseSettlementCPTs
                        .Where(c => c.Id > 0 && c.Id == childModel.Id)
                        .SingleOrDefault();

                    if (existingChild != null)
                    {
                        if (childModel.IsDeleted)
                        {
                            _context.Remove(existingChild);
                        }
                        else
                        {
                            // Update existing CPT record
                            _context.Entry(existingChild).CurrentValues.SetValues(childModel);
                            if (_context.Entry(existingChild).State == EntityState.Modified)
                            {
                                existingChild.UpdatedBy = name;
                                existingChild.UpdatedOn = update;
                            }
                        }
                    }
                    else if(!childModel.IsDeleted)
                    {
                        // TODO? : Disallow adding a CPT to this settlement if it was already in another?
                        // Note that there may be a legimate reason to allow same CPT on multiple settlements because...healthcare industry :/

                        // Insert new CPT record
                        childModel.Id = 0;
                        childModel.UpdatedOn = update;
                        childModel.UpdatedBy = name;
                        orig.CaseSettlementCPTs.Add(childModel);
                    }
                }

                // cleanup
                foreach (var cpt in orig.CaseSettlementCPTs.Where(d => d.Id > 0))
                {
                    if (cpt.IsDeleted)
                        _context.Remove(cpt);
                }

                await _context.SaveChangesAsync();

                
                return Ok(orig);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
