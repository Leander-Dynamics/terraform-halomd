using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MPArbitration.Model;
using ObjectsComparator.Helpers.Extensions;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MPArbitration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CustomersController : MPBaseController
    {
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ILogger<CustomersController> logger, ArbitrationDbContext context, IConfiguration configuration) : base(context, configuration)
        {
            _logger = logger;
        }

        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetAllAsync(bool countCustomers = false)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                Customer[]? customers = null;
                if (user.HasGlobalCaseRole)
                {
                    customers = await _context.Customers.Include(d => d.Entities).ToArrayAsync();
                }
                else
                {
                    var allowedCustomerIDs = user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId).ToArray();
                    if (allowedCustomerIDs.Count() == 0)
                        return new Customer[0];

                    var allowedCustomerNames = await _context.Customers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                    customers = await _context.Customers.Where(x => allowedCustomerNames.Contains(x.Name)).ToArrayAsync();
                }
                if (customers != null)
                {
                    if (countCustomers)
                    {
                        var customersCount = _context.ArbitrationCases.Where(x => x.NegotiationNoticeDeadline >= DateTime.Today)
                            .GroupBy(x => x.Customer).Select(grp => new { CustomerName = grp.Key, Count = grp.Count() });
                        customers.ForEach(x =>
                        {
                            var customerFound = customersCount.First(y => y.CustomerName.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase));
                            if (customerFound != null)
                            {
                                x.ArbitCasesCount = (short)customerFound.Count;
                            }
                        }
                        );
                    }
                    /*
                    foreach (var customer in customers)
                    {
                        var stats = new JsonObject();

                        // Add customer stats to a JSON node 

                        // Total (non-deleted) Cases
                        var c = await _context.ArbitrationCases.CountAsync(d => !d.IsDeleted && d.Customer == customer.Name);
                        stats.Add("TotalCases", c);

                        // Open State
                        c = await _context.ArbitrationCases.CountAsync(d => !d.IsDeleted
                                                                            && d.Customer == customer.Name
                                                                            && (d.Status == ArbitrationStatus.ActiveArbitrationBriefCreated ||
                                                                                 d.Status == ArbitrationStatus.ActiveArbitrationBriefNeeded ||
                                                                                 d.Status == ArbitrationStatus.ActiveArbitrationBriefSubmitted ||
                                                                                 d.Status == ArbitrationStatus.DetermineAuthority ||
                                                                                 d.Status == ArbitrationStatus.InformalInProgress ||
                                                                                 d.Status == ArbitrationStatus.MissingInformation ||
                                                                                 d.Status == ArbitrationStatus.PendingArbitration ||
                                                                                 d.Status == ArbitrationStatus.New ||
                                                                                 d.Status == ArbitrationStatus.Open));
                        stats.Add("OpenStateCases", c);

                        //Open NSA
                        c = await _context.ArbitrationCases.CountAsync(d => !d.IsDeleted
                                                                            && d.Customer == customer.Name
                                                                            && (d.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefCreated ||
                                                                                 d.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded ||
                                                                                 d.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefSubmitted ||
                                                                                 d.NSAWorkflowStatus == ArbitrationStatus.DetermineAuthority ||
                                                                                 d.NSAWorkflowStatus == ArbitrationStatus.InformalInProgress ||
                                                                                 d.NSAWorkflowStatus == ArbitrationStatus.MissingInformation ||
                                                                                 d.NSAWorkflowStatus == ArbitrationStatus.PendingArbitration ||
                                                                                 d.NSAWorkflowStatus == ArbitrationStatus.New ||
                                                                                 d.NSAWorkflowStatus == ArbitrationStatus.Open));
                        stats.Add("OpenNSACases", c);
                        // Settled State
                        c = await _context.ArbitrationCases.CountAsync(d => !d.IsDeleted
                                                                            && d.Customer == customer.Name
                                                                            && (d.Status == ArbitrationStatus.ClosedPaymentReceived ||
                                                                                 d.Status == ArbitrationStatus.ClosedPaymentWithdrawn ||
                                                                                 d.Status == ArbitrationStatus.SettledArbitrationPendingPayment ||
                                                                                 d.Status == ArbitrationStatus.SettledInformalPendingPayment ||
                                                                                 d.Status == ArbitrationStatus.SettledOutsidePendingPayment));
                        stats.Add("SettledStateCases", c);
                        // Ineligible State
                        c = await _context.ArbitrationCases.CountAsync(d => !d.IsDeleted
                                                                            && d.Customer == customer.Name
                                                                            && d.Status == ArbitrationStatus.Ineligible);
                        stats.Add("IneligibleStateCases", c);

                        // Ineligible NSA
                        c = await _context.ArbitrationCases.CountAsync(d => !d.IsDeleted
                                                                            && d.Customer == customer.Name
                                                                            && d.NSAWorkflowStatus == ArbitrationStatus.Ineligible);
                        stats.Add("IneligibleNSACases", c);

                        // Settled NSA
                        c = await _context.ArbitrationCases.CountAsync(d => !d.IsDeleted
                                                                            && d.Customer == customer.Name
                                                                            && (d.NSAWorkflowStatus == ArbitrationStatus.ClosedPaymentReceived ||
                                                                                d.NSAWorkflowStatus == ArbitrationStatus.ClosedPaymentWithdrawn ||
                                                                                d.NSAWorkflowStatus == ArbitrationStatus.SettledArbitrationPendingPayment ||
                                                                                d.NSAWorkflowStatus == ArbitrationStatus.SettledInformalPendingPayment ||
                                                                                d.NSAWorkflowStatus == ArbitrationStatus.SettledOutsidePendingPayment));
                        stats.Add("SettledNSACases", c);
                        customer.Stats = stats;
                    }
                    */
                }
                return Ok(customers);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.StackTrace);
            }
        }

        [HttpGet("entity/items")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Entity>>> GetAllEntitiesAsync()
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                return await _context.Entities.ToListAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("entity/byid/{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<Entity>> GetEntityById(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                var c = await _context.Entities.FindAsync(id);
                if (c == null)
                    return NotFound();
                return Ok(c);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// This method assumes (per the Business spec) that an NPI is only ever associated with a single Customer.
        /// NPINumber is a unique key in the Entities table.
        /// </summary>
        /// <param name="npi"></param>
        /// <returns></returns>
        [HttpGet("entity/bynpi")]
        [Produces("application/json")]
        public async Task<ActionResult<Entity>> GetEntityById([FromQuery] string npi)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                var c = await _context.Entities.FirstOrDefaultAsync(d => d.NPINumber == npi);
                if (c == null)
                    return NotFound();
                return Ok(c);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        /// <summary>
        /// Find all Entities whose name contains the name parameter.
        /// </summary>
        /// <param name="npi"></param>
        /// <returns></returns>
        [HttpGet("entity/find")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Entity>>> GetEntitiesByName([FromQuery] string name)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                return Ok(await _context.Entities.Where(d => d.Name.Contains(name)).ToListAsync());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("find")]
        [Produces("application/json")]
        public async Task<ActionResult<Customer>> GetByName([FromQuery] string name)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                if (!user.HasGlobalCaseRole)
                {
                    var allowedCustomerIDs = user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId).ToArray();
                    if (allowedCustomerIDs.Count() == 0)
                        return Unauthorized("No Customers by that name are available to you.");

                    var customer = await _context.Customers.Include(d => d.Entities).FirstOrDefaultAsync(x => allowedCustomerIDs.Contains(x.Id) && x.Name == name);
                    if (customer == null)
                        return NotFound();

                    return Ok(customer);
                }
                else
                {
                    return Ok(await _context.Customers.Include(d => d.Entities).FirstOrDefaultAsync(x => x.Name == name));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST api/<CustomersController>/5 - Add a Customer
        [HttpPost("")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Customer>> Post([FromBody] Customer value)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context.");
            if (!u.IsAdmin)
                return Unauthorized("Only Admin can add Customers");

            if (value == null || value.Id != 0 || string.IsNullOrEmpty(value.Name))
                return BadRequest("Validation failed");

            var g = await _context.Customers.FirstOrDefaultAsync(d => d.Name == value.Name);
            if (g != null)
                return BadRequest("Customer already exists");

            try
            {
                var n = new Customer()
                {
                    Id = 0,
                    CreatedBy = u.Email,
                    CreatedOn = Utilities.GetCurrentUtcDate(),
                    DefaultAuthority = value.DefaultAuthority,
                    EHRSystem = value.EHRSystem,
                    Entities = value.Entities,
                    IsActive = value.IsActive,
                    Name = value.Name,
                    UpdatedBy = u.Email,
                    UpdatedOn = Utilities.GetCurrentUtcDate(),
                };

                foreach (var c in n.Entities)
                {
                    c.UpdatedOn = n.CreatedOn;
                    c.UpdatedBy = n.CreatedBy;
                    c.Id = 0;
                }
                _context.Customers.Add(n);
                await _context.SaveChangesAsync();

                return Ok(n);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                    msg += " " + ex.InnerException.Message;
                return BadRequest(msg);
            }
        }


        // PUT api/<CustomersController>/5 - Update a customer
        [HttpPut("{id}")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Customer>> Put(int id, [FromBody] Customer value)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context.");
            if (!u.IsAdmin && !u.IsManager)
                return Unauthorized("Only Managers and Admin can update Customers");

            if (value == null || value.Id <= 0 || id <= 0 || value.Id != id || string.IsNullOrEmpty(value.Name))
                return BadRequest("Validation failed");

            var orig = await _context.Customers.Include(d => d.Entities).FirstOrDefaultAsync(d => d.Id == id);
            if (orig == null)
                return BadRequest("Unable to locate the Customer record");

            var h = await _context.Customers.FirstOrDefaultAsync(d => d.Id != id && d.Name == value.Name);
            if (h != null)
                return BadRequest("Update would create duplicate records!");

            if (!u.IsAdmin && !orig.Name.Equals(value.Name, StringComparison.CurrentCultureIgnoreCase))
                return BadRequest("Only Admin can change the name of a Customer.");

            try
            {
                value.UpdatedBy = u.Email;
                value.UpdatedOn = Utilities.GetCurrentUtcDate();

                _context.Entry(orig).CurrentValues.SetValues(value);

                //Update the Entities
                foreach (var childModel in value.Entities.Where(d => !string.IsNullOrEmpty(d.Name) && !string.IsNullOrEmpty(d.NPINumber)))
                {
                    var existingChild = orig.Entities
                        .Where(c => c.Id == childModel.Id)
                        .SingleOrDefault();

                    if (existingChild != null)
                    {
                        // Update existing record
                        _context.Entry(existingChild).CurrentValues.SetValues(childModel);
                        if (_context.Entry(existingChild).State == EntityState.Modified) // ideally, this would do what it appears to do but EF is just broken when it comes to change detection
                        {
                            existingChild.UpdatedBy = value.UpdatedBy;
                            existingChild.UpdatedOn = value.UpdatedOn;
                        }
                    }
                    else
                    {
                        // Insert new Entity record
                        childModel.Id = 0;
                        childModel.UpdatedOn = value.UpdatedOn;
                        childModel.UpdatedBy = value.UpdatedBy;
                        orig.Entities.Add(childModel);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(orig);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                    msg += " " + ex.InnerException.Message;
                return BadRequest(msg);
            }
        }

        // DELETE api/<CustomersController>/{id}/entity/{entityId}
        [HttpDelete("{id}/entity/{entityId}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> DeleteEntityAsync(int id, int entityId)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context.");
            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Managers and Admin can delete Entities");
            if (id < 1 || entityId < 1)
                return BadRequest("Invalid identifiers");
            try
            {
                var n = await _context.Entities.FirstOrDefaultAsync(d => d.Id == entityId && d.CustomerId == id);
                if (n == null)
                    return NotFound();

                _context.Entities.Remove(n);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST api/<CustomersController>/entity
        [HttpPost("entity")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Entity>> CreateEntityAsync([FromBody] Entity value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context.");
            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Managers and Admin can create Customers and Entities");
            if (value.Id != 0 || value.CustomerId < 1 || string.IsNullOrEmpty(value.Name) || string.IsNullOrEmpty(value.NPINumber))
                return BadRequest("Validation failed. CustomerId, Name and NPINumber are required. Id must be zero.");

            try
            {
                var n = await _context.Entities.FirstOrDefaultAsync(d => d.NPINumber == value.NPINumber);
                if (n != null)
                    return BadRequest("The NPINumber is already assigned to another Customer.");

                value.UpdatedOn = Utilities.GetCurrentUtcDate();
                value.UpdatedBy = user.Email;
                _context.Entities.Add(value);
                await _context.SaveChangesAsync();
                return value;

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT api/<CustomersController>/entity/5 - Update an Entity
        [HttpPut("entity/{id}")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Customer>> UpdateEntity(int id, [FromBody] Entity value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context.");
            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Managers and Admin can update Customers and Entities");

            if (value == null || value.Id <= 0 || id <= 0 || value.Id != id || string.IsNullOrEmpty(value.Name) || string.IsNullOrEmpty(value.NPINumber))
                return BadRequest("Validation failed. ID, Name and NPINumber are required.");

            var orig = await _context.Entities.FirstOrDefaultAsync(d => d.Id == id);
            if (orig == null)
                return NotFound();

            var n = await _context.Entities.FirstOrDefaultAsync(d => d.NPINumber == value.NPINumber && d.Id != value.Id);
            if (n != null)
                return BadRequest("The NPINumber is already assigned to another Customer. Did you mean to change the NPI?");

            var allowedCustomerIDs = new List<int>();
            if (!user.HasGlobalCaseRole)
            {
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
                if (!orig.CustomerId.HasValue || !allowedCustomerIDs.Contains(orig.CustomerId.Value))
                    return Unauthorized("Customer and Entity records are not available to the current account.");
            }

            var h = await _context.Customers.FirstOrDefaultAsync(d => d.Id != id && d.Name == value.Name);
            if (h != null)
                return BadRequest("Update would create duplicate records!");

            if (!user.IsAdmin && !orig.Name.Equals(value.Name, StringComparison.CurrentCultureIgnoreCase))
                return BadRequest("Only Admin can change the name of a Customer.");

            try
            {
                value.UpdatedBy = user.Email;
                value.UpdatedOn = Utilities.GetCurrentUtcDate();

                // Update existing record
                _context.Entry(orig).CurrentValues.SetValues(value);

                await _context.SaveChangesAsync();

                return Ok(orig);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                    msg += " " + ex.InnerException.Message;
                return BadRequest(msg);
            }
        }
    }
}
