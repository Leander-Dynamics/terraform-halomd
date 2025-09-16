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
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Net.Http.Headers;
using MPArbitration.Utility;

namespace MPArbitration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TemplatesController : MPBaseController
    {
        private readonly ILogger<CasesController> _logger;

        public TemplatesController(ILogger<CasesController> logger, ArbitrationDbContext context, IConfiguration configuration) : base(context, configuration)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns all templates but omits the HTML. Useful for supporting fast filtering and manipulation on the client side.
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        public async Task<ActionResult<IEnumerable<Template>>> GetAllTemplatesAsync()
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                var t = await _context.Templates.Select(x => new Template { CreatedBy = x.CreatedBy, CreatedOn = x.CreatedOn, Id = x.Id, JSON = x.JSON, Name = x.Name, UpdatedBy = x.UpdatedBy, UpdatedOn = x.CreatedOn }).ToArrayAsync();
                if (t == null)
                    return NotFound();
                return Ok(t);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Gets a Template record, including HTML
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Template>> GetTemplateAsync(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                var t = await _context.Templates.FindAsync(id);
                if (t == null)
                    return NotFound();
                return Ok(t);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<dynamic>> FindTemplatesAsync([FromBody]dynamic criteria)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                // how we gonna do this? see CasesController.Search() for dynamic criteria buildup
                return Ok(criteria);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// POST api/<TemplateController>/5 - Add a Template
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost("{id}")]
        public async Task<ActionResult<Template>> CreateTemplateAsync([FromBody] Template value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (value == null || value.Id != 0 || string.IsNullOrEmpty(value.ComponentType) || string.IsNullOrEmpty(value.HTML) || string.IsNullOrEmpty(value.Name))
                return BadRequest("Validation failed. ComponentType, HTML and Name are required.");

            try
            {
                value.CreatedOn = Utilities.GetCurrentUtcDate();
                value.CreatedBy = user.Email;
                value.UpdatedOn = Utilities.GetCurrentUtcDate();
                value.UpdatedBy = user.Email;
                _context.Set<Template>().Add(value);
                await _context.SaveChangesAsync();
                return Ok(value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// PUT api/<TemplateController>/5 - Update a Template
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<ActionResult<Template>> UpdateTemplateAsync(int id, [FromBody] Template value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Admins and Managers can save changes to Templates");

            if (value == null || value.Id < 1 || value.Id != id)
                return BadRequest();

            if(string.IsNullOrEmpty(value.ComponentType) || string.IsNullOrEmpty(value.HTML) || string.IsNullOrEmpty(value.Name))
                return BadRequest("Validation failed. ComponentType, HTML and Name are required.");

            var orig = await _context.Templates.FindAsync(value.Id);
            if (orig == null)
                return NotFound();

            try
            {
                _context.Entry(orig).CurrentValues.SetValues(value);
                await _context.SaveChangesAsync();
                return Ok(orig);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
