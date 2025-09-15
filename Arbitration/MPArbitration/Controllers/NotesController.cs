using Microsoft.AspNetCore.Mvc;
using MPArbitration.Model;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using MPArbitration.Utility;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MPArbitration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class NotesController : MPBaseController
    {
        private readonly ILogger<NotesController> _logger;
        
        public NotesController(ILogger<NotesController> logger, ArbitrationDbContext context, IConfiguration configuration) : base(context, configuration)
        {
            _logger = logger;
        }

        // GET notes/5
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<Note>> GetCaseNote(int id)
        {
            if (id < 1)
                return BadRequest();

            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var n = await _context.Notes.FindAsync(id);
            if(n == null)
                return NotFound();

            if(user.HasGlobalCaseRole)
                return Ok(n);

            // verify access to the ArbitrationCase that contains the Note
            var allowedCustomerIDs = new List<int>();
            List<string> allowedCustomerNames = new List<string>();
            allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer).Select(x => x.EntityId));
            allowedCustomerNames = await _context.Customers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToListAsync();
            if (allowedCustomerNames.Count() == 0)
                return Unauthorized("Customer records are not available to the current account.");

            var orig = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == id && !d.IsDeleted && allowedCustomerNames.Contains(d.Customer));
            
            if (orig == null)
                return NotFound("Record not found or unauthorized to view Case information");

            return Ok(n);
        }

        /* PUT api/<NotesController> - not allowing update of Notes at this time
        [HttpPut]
        public async Task<ActionResult<Note>> Post([FromBody] Note value)
        {
            if (value == null || value.Id < 1)
                return BadRequest();
            var orig = await _context.Notes.FindAsync(value.Id);
            if (orig == null)
                return NotFound();

            _context.Entry(orig).CurrentValues.SetValues(value);
            await _context.SaveChangesAsync();
            return Ok(orig);
        }
        */

        /// <summary>
        /// POST api/<NotesController>/5 - Add a Note to an ArbitrationCase
        /// </summary>
        /// <param name="caseId">ArbitrationCaseId</param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost("claim/{caseId}")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Note>> CreateNoteAsync(int caseId, [FromBody] Note value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (value == null || value.Id !=0 || string.IsNullOrEmpty(value.Details))
                return BadRequest("Validation failed");

            List<string> allowedCustomerNames = new List<string>();

            if (!user.HasGlobalCaseRole)
            {
                var allowedCustomerIDs = new List<int>();
                allowedCustomerIDs.AddRange(user.AllAppRoles.Where(x => x.RoleType == UserRoleType.Customer && (x.AccessLevel == UserAccessType.manager || x.AccessLevel == UserAccessType.negotiator)).Select(x => x.EntityId));
                allowedCustomerNames = await _context.Customers.Where(x => allowedCustomerIDs.Contains(x.Id)).Select(x => x.Name).ToListAsync();
                if (allowedCustomerNames.Count() == 0)
                    return Unauthorized("Customer records are not available to the current account or isufficient access to add Notes.");
            }

            // find the case to add to
            var c = await _context.ArbitrationCases.Include(d => d.Notes).FirstOrDefaultAsync(d => !d.IsDeleted && d.Id == caseId && (user.HasGlobalCaseRole || allowedCustomerNames.Contains(d.Customer)));
            if (c == null)
                return NotFound();

            var n = new Note()
            {
                Id = 0,
                Details = value.Details,
                UpdatedBy = user.Email  ,
                UpdatedOn = Utilities.GetCurrentUtcDate()
            };
            c.Notes.Add(n);
            await _context.SaveChangesAsync();

            return Ok(n);
        }

        /// <summary>
        /// POST api/<NotesController>/5 - Add a Note to an ArbitrationCase
        /// </summary>
        /// <param name="caseId">ArbitrationCaseId</param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost("dispute")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Note>> CreateAuthorityDisputeNoteAsync([FromBody] AuthorityDisputeNote value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsManager && !user.IsSystem && !user.IsNegotiator && !user.IsAdmin)
                return Unauthorized("Only Managers and Negotiators can add notes.");
            
            if (value == null || value.Id != 0 || string.IsNullOrEmpty(value.Details) || value.AuthorityDisputeId < 1)
                return BadRequest("Validation failed");

            var dispute = await _context.AuthorityDisputes.FirstOrDefaultAsync(d => d.Id == value.AuthorityDisputeId);
            if (dispute == null)
                return NotFound();

            var n = new AuthorityDisputeNote()
            {
                AuthorityDisputeId = value.AuthorityDisputeId,
                Id = 0,
                Details = value.Details,
                UpdatedBy = user.Email,
                UpdatedOn = Utilities.GetCurrentUtcDate()
            };
            dispute.Notes.Add(n);
            await _context.SaveChangesAsync();

            return Ok(n);
        }
        /* DELETE api/<NotesController>/5 - not supporting note delete at the moment
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
        */
    }
}
