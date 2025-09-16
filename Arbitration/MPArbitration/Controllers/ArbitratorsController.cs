using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using MPArbitration.Model;
using System.Text;
using System.Dynamic;
using System.Net.Mime;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace MPArbitration.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class ArbitratorsController : MPBaseController
    {
        private readonly ILogger<ArbitratorsController> _logger;

        public ArbitratorsController(ILogger<ArbitratorsController> logger, ArbitrationDbContext context, IConfiguration configuration) : base(context, configuration)
        {
            _logger = logger;
        }

        #region Arbitrator endpoints
        /// <summary>
        /// Fetch all Arbitrator records
        /// </summary>
        /// <param name="f">Include Fees</param>
        /// <param name="t">Limit by ArbitratorType</param>
        /// <param name="a">ActiveOnly. True excludes inactive records.</param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Arbitrator>>> GetArbitratorsAsync([FromQuery] bool f = false, [FromQuery] ArbitratorType? t = null, [FromQuery] Boolean a = false)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            try
            {
                var Q = _context.Arbitrators.Include(d => d.Fees.Where(j => f))
                                            .Where(b => (!t.HasValue || b.ArbitratorType == t.Value) && (!a || b.IsActive));
                var arbs = await Q.ToArrayAsync();
                if (arbs.Length == 0)
                    return Ok(new Arbitrator[] { });
                return Ok(arbs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Arbitrator>>> GetArbitratorByIdAsync(int id)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            try
            {
                var arb = await _context.Arbitrators.Include(d => d.Fees).FirstOrDefaultAsync(d => d.Id == id);
                if (arb == null)
                    return NotFound();
                return Ok(arb);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // PUT - Update an Arbitrator
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<Arbitrator>> UpdateArbitrator([FromBody] Arbitrator current)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            if (!u.IsAdmin && !u.IsManager && !u.IsSystem)
                return Unauthorized("Only system admins can modify Arbitrators.");

            if (current == null || current.Id < 1)
                return BadRequest("Missing or invalid data");

            var orig = _context.Arbitrators.Find(current.Id);
            if (orig == null)
                return NotFound("No such record exists");

            try
            {
                _context.Entry(orig).CurrentValues.SetValues(current);
                if (_context.Entry(orig).State == EntityState.Modified)
                {
                    orig.UpdatedOn = DateTime.UtcNow;
                    orig.UpdatedBy = u.Email;
                    await _context.SaveChangesAsync();
                }
                return Ok(orig);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST - Import Arbitrators from a File
        [HttpPost("import")]
        [RequestSizeLimit(1 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 4 * 1024 * 1024)]
        public async Task<ActionResult> ImportArbitrators([FromForm] IFormFile file) //<IEnumerable<Arbitrator>>
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin)
                return Unauthorized("Only Administrators can import new Arbitrator data");

            var log = new StringBuilder("New Arbitrators file received. Validating...");
            StringBuilder json = new StringBuilder();
            var updatedOn = Utilities.GetCurrentUtcDate();

            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Upload file appears empty!");
                else if (file.Length > 8000000)
                    return BadRequest("File size is over 8MB. Split into multiple uploads or push new Arbitrator data directly to the database.");
                else if (!file.FileName.ToLower().EndsWith(".json"))
                    return BadRequest("Only .json files allowed");

                json = new StringBuilder();
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    while (reader.Peek() >= 0)
                        json.Append(reader.ReadLine() ?? "");
                }

                log.AppendLine($@"Read {json.Length} characters. Parsing to JSON object...");

                var parsed = JsonSerializer.Deserialize<ImportArbitrators>(json: json.ToString());
                if (parsed == null || parsed.Arbitrators.Count() == 0)
                    return BadRequest("No Arbitrators detected!");

                log.AppendLine("Begin update process...");

                int index = 0;
                foreach (var arb in parsed.Arbitrators)
                {
                    string? stats = arb.statistics.HasValue ? arb.statistics.ToString() : "";
                    stats = stats == null ? "" : stats;
                    var first = await _context.Arbitrators.FirstOrDefaultAsync(d => d.Name == arb.name);
                    if (first == null)
                    {
                        log.AppendLine($@"Updated Arbitrator for record {index}.");
                        var n = new Arbitrator { IsActive = true, Name = arb.name, Statistics = stats, UpdatedBy = user.Email, UpdatedOn = updatedOn };
                        _context.Arbitrators.Add(n);
                    }
                    else
                    {
                        first.Statistics = stats;
                        log.AppendLine($@"Added Arbitrator for record {index}");

                    }
                    index++;
                }

                await _context.SaveChangesAsync();
                log.AppendLine($@"Finished. Processed {index} records successfully.");

                try
                {
                    // upload log file
                    await this.SaveUploadLog("ArbitratorUpdate", updatedOn, log.ToString(), _logger, "ArbitratorUpdate");
                }
                catch { } // just swallow the message for now

                return Ok("Arbitrators updates successfully!");
            }
            catch (Exception ex)
            {
                log.AppendLine(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        // POST - Add an Arbitrator
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<Arbitrator>> AddArbitrator([FromBody] Arbitrator arb)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            if (!u.IsAdmin)
                return Unauthorized("Only system admins can create Arbitrators");

            if (arb.Id != 0)
                return BadRequest("Object is not new!");

            if (string.IsNullOrEmpty(arb.Email))
                return BadRequest("Email is required");

            var p = await _context.Arbitrators.FirstOrDefaultAsync(d => d.Email == arb.Email);
            if (p != null)
                return BadRequest("Request would create a duplicate");

            Arbitrator newArb = new Arbitrator();
            _context.Entry(newArb).CurrentValues.SetValues(arb);

            newArb.UpdatedOn = Utilities.GetCurrentUtcDate();
            newArb.UpdatedBy = u.Email;
            _context.Arbitrators.Add(newArb);
            await _context.SaveChangesAsync();
            return Ok(newArb);
        }

        #endregion

        #region Fee endpoints

        // POST api/arbitrators/fees/5 - Add an ArbitratorFee
        [HttpPost("fees")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ArbitratorFee>> AddArbitratorFeeAsync([FromBody] ArbitratorFee value)
        {
            try
            {
                var user = await GetCurrentUser();
                if (user == null)
                    return Unauthorized("No active User context!");

                if (!user.IsAdmin && !user.IsManager)
                    return Unauthorized("Only Managers and Administrators can add new arbitrator fees");

                if (value == null || value.ArbitratorId < 1 || value.Id != 0 || string.IsNullOrEmpty(value.FeeName))
                    return BadRequest("Validation failed");

                if (!string.IsNullOrEmpty(value.ReferenceColumnName))
                {
                    // verify that the column value matches an actual dispute column
                    var t = Type.GetType("MPArbitration.Model.AuthorityDispute");
                    var targetProps = t!.GetProperties().Where(d => d.GetIndexParameters().Length == 0 && d.Name == value.ReferenceColumnName);
                    if (targetProps.Count() == 0)
                        return BadRequest("ReferenceColumnName value does not match a known AuthorityDispute property.");
                }

                var g = await _context.Arbitrators.Include(d => d.Fees).FirstOrDefaultAsync(d => d.Id == value.ArbitratorId);
                if (g == null)
                    return BadRequest("Arbitrator not found");

                _context.Entry(value).State = EntityState.Added;

                value.CreatedOn = Utilities.GetCurrentUtcDate();
                value.UpdatedOn = value.CreatedOn;

                value.CreatedBy = user.Email;
                value.UpdatedBy = user.Email;

                await _context.SaveChangesAsync();
                return Ok(value);
            }
            catch (Exception ex)
            {
                var msg = new StringBuilder("Save failed. " + ex.Message);
                var e = ex.InnerException;
                while (e != null)
                {
                    msg.AppendLine(e.Message);
                    e = e.InnerException;
                }

                return BadRequest(msg.ToString());
            }
        }

        // PUT api/arbitrators/fees/5 - Update an ArbitratorFee
        [HttpPut("fees")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ArbitratorFee>> UpdateArbitratorFeeAsync([FromBody] ArbitratorFee value)
        {
            try
            {
                var user = await GetCurrentUser();
                if (user == null)
                    return Unauthorized("No active User context!");

                if (!user.IsAdmin && !user.IsManager && !user.IsSystem)
                    return Unauthorized("Only Managers and Administrators can update Arbitrator Fee configuration");

                if (value.Id < 1)
                    return BadRequest("Invalid identifier");

                if (value.ArbitratorId < 1)
                    return BadRequest("Invalid Arbitrator identifier");

                if (value.DueDaysAfterColumnName < 0)
                    return BadRequest("Due Days cannot be negative");

                var fee = await _context.ArbitratorFees.FindAsync(value.Id);
                if (fee == null)
                    return NotFound();

                // validation
                if (value.ArbitratorId != fee.ArbitratorId)
                    return BadRequest("Identifier mismatch!");

                if (!string.IsNullOrEmpty(value.ReferenceColumnName))
                {
                    // verify that the column value matches an actual dispute column
                    var t = Type.GetType("MPArbitration.Model.AuthorityDispute");
                    var targetProps = t!.GetProperties().Where(d => d.GetIndexParameters().Length == 0 && d.Name == value.ReferenceColumnName);
                    if (targetProps.Count() == 0)
                        return BadRequest("ReferenceColumnName value does not match a known AuthorityDispute property.");
                }

                _context.Entry(fee).CurrentValues.SetValues(value);
                await _context.SaveChangesAsync();

                return Ok(fee);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion
    }
}
