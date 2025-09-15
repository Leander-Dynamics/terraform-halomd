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
using CsvHelper;
using CsvHelper.Configuration;
using MPArbitration.Utility;

namespace MPArbitration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProcedureCodesController : MPBaseController
    {
        private readonly ILogger<ProcedureCodesController> _logger;

        public ProcedureCodesController(ILogger<ProcedureCodesController> logger, ArbitrationDbContext context, IConfiguration configuration) : base(context, configuration)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("import")]
        public async Task<ActionResult<string>> ImportData([FromForm] IFormFile file, [FromQuery]DateTime? ed = null)
        {
            // TODO: Switch to using CsvHelper and see this: https://dotnetcoretutorials.com/2018/08/04/csv-parsing-in-net-core/
            // https://joshclose.github.io/CsvHelper/getting-started/

            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin && !user.IsManager && !user.IsSystem)
                return Unauthorized("Your role is not authorized to import data.");

            StringBuilder log = new StringBuilder("Starting processing...  ");

            try
            {
                if (file == null)
                    return BadRequest("No file detected!");
                else if (file.Length > 5000000)
                    return BadRequest("File size is too large. 5MB is the maximum allowed size.");
                else if (!file.FileName.ToLower().EndsWith(".csv"))
                    return BadRequest("Only CSV files allowed");

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header.ToLower(),
                    BadDataFound = args => log.AppendLine($"Row {args.Context.Reader.Parser.Row}: BadDataFound"),
                    HasHeaderRecord = true,
                    HeaderValidated = null, //args => Console.Write(string.Join("', '", args.InvalidHeaders?.FirstOrDefault()?.Names)),
                    MissingFieldFound = null, //args => Console.WriteLine($"Field with names ['{string.Join("', '", args.HeaderNames)}'] at index '{args.Index}' was not found. ")
                };
                
                IEnumerable<ProcedureCode>? records = null;

                using (var reader = new StreamReader(file.OpenReadStream()))
                using(var csv = new CsvReader(reader, config))
                {   
                    records = csv.GetRecords<ProcedureCode>().ToList();
                }

                if (records.FirstOrDefault(d => string.IsNullOrEmpty(d.Description) || string.IsNullOrEmpty(d.Group) || string.IsNullOrEmpty(d.Code)) != null)
                    throw new Exception("One or more records has an empty Code, Description, or Group value.");
                
                var pc = _context.Model.FindEntityType("MPArbitration.Model.ProcedureCode");
                var code = pc?.GetProperties().FirstOrDefault(x => x.Name == "Code");
                var codeType = pc?.GetProperties().FirstOrDefault(x => x.Name == "CodeType");
                var description = pc?.GetProperties().FirstOrDefault(x => x.Name == "Description");
                var group = pc?.GetProperties().FirstOrDefault(x => x.Name == "Group");
                if (pc == null || code == null || codeType == null || description == null || group == null)
                    throw new Exception("Unexpected Error! One or more ProcedureCode entity properties could not be found!");

                int codeMax = code.GetMaxLength() ?? 5;
                int descrMax = description.GetMaxLength() ?? 300;
                int groupMax = group.GetMaxLength() ?? 100;

                var batchUploadDate = Utilities.GetCurrentUtcDate();

                foreach(var rec in records)
                {    
                    rec.UpdatedOn = batchUploadDate;
                    rec.UpdatedBy = user.Email;
                    rec.EffectiveDate = ed;
                    if (rec.Code.Length > codeMax)
                        throw new Exception($"Code value {rec.Code} is too wide! Aborting all changes.");

                    if(rec.Group.Length > groupMax)
                        rec.Group = rec.Group.Substring(0, groupMax);

                    if (rec.Description.Length > descrMax)
                        rec.Description = rec.Description.Substring(0, descrMax);
                }
                
                // purge records with same effective date and then import
                var purge = _context.ProcedureCodes.Where(d => d.EffectiveDate == ed).AsEnumerable();
                var removed = purge.Count();
                _context.RemoveRange(purge);
                await _context.SaveChangesAsync();
                log.AppendLine($"Removed {removed} old records. ");
                _context.AddRange(records);
                await _context.SaveChangesAsync();
                log.Append($"Added {records.Count()} new records. ");
                log.AppendLine("Upload successful!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);
#if DEBUG
                if (ex.InnerException != null)
                    log.AppendLine(ex.InnerException.Message);
                else
                    log.AppendLine(ex.Message);
#else
                log.AppendLine("An unexpected exception occurred.");
#endif
                return BadRequest(log.ToString());
            }

            return Ok(log.ToString());
        }
    }
}
