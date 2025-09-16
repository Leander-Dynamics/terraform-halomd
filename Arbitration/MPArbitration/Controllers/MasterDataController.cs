using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MPArbitration.Model;
using System.Net.Mime;
using System.Security.Principal;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MPArbitration.Utility;

namespace MPArbitration.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/mde")]
    public class MasterDataController : MPBaseController
    {
        private readonly ILogger<MasterDataController> _logger;

        public MasterDataController(ILogger<MasterDataController> logger, ArbitrationDbContext context, IConfiguration configuration) : base(context, configuration)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns MasterDataException records
        /// </summary>
        /// <returns></returns>
        [HttpGet("items")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<MasterDataException>>> GetMasterDataExceptionsAsync([FromQuery] bool resolved = false)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.HasGlobalCaseRole)
                return Unauthorized("Access denied");

            try
            {
                return Ok(await _context.MasterDataExceptions.Where(d => resolved == true || !d.IsResolved).ToArrayAsync());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("items/{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<MasterDataException>> UpdateMasterDataExceptionAsync(int id, [FromBody] MasterDataException data)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (u.IsAdmin && !u.IsManager && !u.IsNegotiator)
                return Unauthorized("Access denied.");
            if (id < 1 || id != data.Id)
                return BadRequest("Bad record Id");
            if (data.ExceptionType == MasterDataExceptionType.Unknown)
                return BadRequest("Cannot update Unknown exception types");
            
            try
            {

                var orig = await _context.MasterDataExceptions.FirstOrDefaultAsync(d => d.Id == data.Id);
                if (orig == null)
                    return NotFound("Could not find a record with the given Id.");
                orig.UpdatedOn = Utilities.GetCurrentUtcDate();
                orig.UpdatedBy = u.Email;
                orig.IsResolved = data.IsResolved;
                await _context.SaveChangesAsync();
                return orig;
            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
