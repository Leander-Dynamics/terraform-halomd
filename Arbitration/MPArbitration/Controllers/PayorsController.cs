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
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Text.Json.Nodes;

namespace MPArbitration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PayorsController : MPBaseController
    {
        private readonly ILogger<PayorsController> _logger;

        public PayorsController(ILogger<PayorsController> logger, ArbitrationDbContext context, IConfiguration configuration) : base(context, configuration)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<Payor[]>> GetAllAsync([FromQuery] bool active = true, [FromQuery] bool groups = false, [FromQuery] bool templates = true)
        {
            /* how to use caching if needed at some point
            var key = "distinct_payors";
            string[] payors;
            if (!_memoryCache.TryGetValue(key, out payors))
            {
                payors = await _context.ArbitrationCases.Select(d => d.Payor).Distinct().ToArrayAsync();
                if (payors.Length > 0)
                {
                    _memoryCache.Set(key, payors, new TimeSpan(0, 30, 0)); // cache for 30 minutes
                }
            }
            return payors;
            */
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            Payor[]? payors = null;
            if (!templates)
            {
                var pp = from p in _context.Set<Payor>().AsNoTracking().Include(p => p.PayorGroups).Include(a => a.Addresses)
                         select new Payor
                         {
                             Id = p.Id,
                             IsActive = p.IsActive,
                             JSON = @"{""exclusions"":[],""templates"":[]}",
                             Name = p.Name,
                             NSARequestEmail = p.NSARequestEmail,
                             ParentId = p.ParentId,
                             SendNSARequests = p.SendNSARequests,
                             UpdatedBy = p.UpdatedBy,
                             UpdatedOn = p.UpdatedOn
                         };
                payors = await pp.ToArrayAsync();
            }
            else
            {
                var pp = from p in _context.Set<Payor>().AsNoTracking().Include(p => p.PayorGroups).Include(a => a.Addresses)
                         select new Payor
                         {
                             Id = p.Id,
                             IsActive = p.IsActive,
                             JSON = p.JSON,
                             Name = p.Name,
                             NSARequestEmail = p.NSARequestEmail,
                             ParentId = p.ParentId,
                             SendNSARequests = p.SendNSARequests,
                             UpdatedBy = p.UpdatedBy,
                             UpdatedOn = p.UpdatedOn
                         };
                payors = await pp.ToArrayAsync();
            }

            return Ok(payors);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payor>> GetByIdAsync(int id, [FromQuery] bool activeOnly = true)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var payor = await _context.Payors
                                        .Include(a => a.Addresses)
                                        .Include(c => c.Negotiators.Where(g => !activeOnly || g.IsActive))
                                        .Include(d => d.PayorGroups)
                                        .AsSplitQuery().AsNoTracking()
                                        .FirstOrDefaultAsync(d => d.Id == id);
            if (payor == null)
                return NotFound();
            return Ok(payor);
        }

        [HttpGet("lookup/{name}")]
        public async Task<ActionResult<Payor>> GetByNameAsync(string name)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var payor = await _context.Payors.AsNoTracking().FirstOrDefaultAsync(d => d.Name == name);
            if (payor == null)
                return NotFound();
            return Ok(payor);
        }

        [HttpGet]
        [Route("{id}/templates")]
        public async Task<ActionResult<JsonNode>> GetTemplatesAsync(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var payor = await _context.Payors.FindAsync(id);
            string json = payor == null || !payor.JSON.Contains(@"""templates"":") ? @"{ ""templates"":[]}" : payor.JSON;
            return Ok(JsonNode.Parse(json));
        }

        /* Removed this because it seems unused and potentially returns gobs of template data stored in JSON
        [HttpGet("children/{id}")]
        public async Task<ActionResult<IEnumerable<Payor>>> GetByParentIdAsync(int id, [FromQuery] bool activeOnly = true)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var payors = await _context.Payors.Where(d=>d.ParentId == id && (!activeOnly || d.IsActive)).ToArrayAsync();
            if (payors == null)
                return NotFound();

            return Ok(payors);
        }
        */

        [HttpPost]
        [Route("blob")]
        public async Task<ActionResult<string>> AttachPayorFileAsync([FromQuery] int id, [FromQuery] string cdt, [FromForm] IFormFile file)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin)
                return Unauthorized("Insufficient privileges to attach Files to a Payor");

            if (id < 1)
                return NotFound("ID not found");

            if (string.IsNullOrEmpty(cdt))
                return BadRequest("Missing Payor Document Type");

            if (!file.FileName.ToLower().EndsWith(".pdf"))
                return BadRequest("Invalid file type");
            if (file != null && file.Length > 10000000)
                return BadRequest("File size is too large (>10MB). Reduce the file size or contact support.");
            if (file == null)
                return BadRequest("No content detected");

            var low = cdt.ToLower();
            var allowed = new string[] { "nsarequestattachment" };
            if (!allowed.Contains(cdt))
                return BadRequest("Unsupported document metadata");

            var payor = await _context.Payors.FindAsync(id);
            if (payor == null)
                return NotFound("Payor not found");

            var uploadedOn = Utilities.GetCurrentUtcDate();
            var uploadedBy = GetUsername();
            var log = new StringBuilder($@"{uploadedOn:G}: Storing auxillary file for Payor {id} ({payor.Name}).");
            string blobURL = "";

            try
            {

                using (var reader = file.OpenReadStream())
                {
                    // nsarequestattachment-payor-7-The filename.pdf
                    string blobName = $@"{low}-payor-{id}-{file.FileName.ToLower()}";
                    if (blobName.Length > 255)
                        blobName = blobName.Substring(0, 255);

                    try
                    {
                        BlobClient blob = _containerClient.GetBlobClient(blobName);

                        _logger.LogInformation($@"Attempting to upload file {blobName} to BLOB store...");
                        var response = blob.Upload(reader, true);
                        if (response.GetRawResponse().ReasonPhrase != "Created")
                            throw new Exception("Unexpected result from BLOB upload");

                        // add tags to new BLOB
                        var tags = new Dictionary<string, string>();
                        if (!string.IsNullOrEmpty(payor.Name))
                            tags.Add("Payor", payor.Name);

                        tags.Add("PayorId", id.ToString());
                        tags.Add("UpdatedBy", uploadedBy);
                        tags.Add("DocumentType", low);
                        blob.SetTags(tags);
                        blobURL = $@"{_containerClient.Uri.ToString()}/{blobName}";
                        _logger.LogInformation($@"BLOB uploaded to {blobURL}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Unable to write to BLOB storage. " + ex.Message);
                        _logger.LogError(ex.Message);
                    }
                }

                log.AppendLine($@"Uploaded by {uploadedBy}");

                return Ok();
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
        public async Task<IActionResult> ViewPayorFileAsync([FromQuery] int id, [FromQuery] string name)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No AppUser context!");
            if (!user.IsActive)
                return Unauthorized("No active AppUser context!");

            if (string.IsNullOrEmpty(name))
                return BadRequest("Empty document name");

            var n = name.ToLower();
            if (!name.Contains("-payor-") || !(n.EndsWith(".pdf") || n.EndsWith(".tif") || n.EndsWith(".tiff")))
                return BadRequest("Invalid document name");

            var orig = await _context.Payors.FindAsync(id);

            if (orig == null)
                return NotFound("Payor not found");

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
                return Ok(r);

            }
            catch (Exception ex)
            {
                _logger.LogError($@"Error retrieving {name} from BLOB storage");
                _logger.LogError(ex.Message);
                return BadRequest($@"Error retrieving {name} from storage");
            }

        }

        #region DELETE routes
        // DELETE - DELETE an existing AuthorityDisputeFee
        [HttpDelete("address")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<AuthorityDispute>> DeletePayorAddressAsync([FromQuery] int pid, [FromQuery] int aid)
        {
            try
            {
                var u = await GetCurrentUser();
                if (u == null)
                    return Unauthorized("No active User context!");

                if (!u.IsManager && !u.IsNegotiator && !u.IsSystem)
                    return BadRequest("Only Managers and Negotiators can delete AuthorityDisputeFees.");

                if (aid < 1 || pid < 1)
                    return BadRequest("Bad Id.");

                var addr = await _context.PayorAddresses.FirstOrDefaultAsync(d => d.PayorId == pid && d.Id == aid);
                if (addr == null)
                    return NotFound();
                _context.Set<PayorAddress>().Remove(addr);
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

        [HttpDelete]
        [Route("blob")]
        public async Task<IActionResult> DeletePayorFileAsync([FromQuery] int id, [FromQuery] string cdt, [FromQuery] string name)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin)
                return Unauthorized("Only admin can remove Payor attachments.");

            if (string.IsNullOrEmpty(name))
                return BadRequest("Empty document name");

            // verify record
            var orig = await _context.Payors.FindAsync(id);
            if (orig == null)
                return NotFound("Payor record not found");

            var n = name.ToLower();
            var low = cdt.ToLower();

            if (!name.StartsWith($@"{low}-payor-{id}") || !(n.EndsWith(".pdf") || n.EndsWith(".tif") || n.EndsWith(".tiff")))
                return BadRequest("Invalid document name");

            try
            {
                BlobClient blob = _containerClient.GetBlobClient(name);
                var result = await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
                if (result)
                    return Ok();

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError($@"Error deleting {name} from BLOB storage");
                _logger.LogError(ex.Message);
                return BadRequest($@"Error deleting {name} from storage");
            }

        }

        #endregion

        /// <summary>
        /// Get all attachments / files for a Payor
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("files/{id}")]
        public async Task<ActionResult<IEnumerable<CaseFile>>> GetPayorFilesAsync(int id, [FromQuery] string? docType = null)
        {
            if (id < 1)
                return BadRequest("Invalid parameter");

            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var orig = await _context.Payors.FindAsync(id);

            if (orig == null)
                return NotFound("Payor not found");

            if (docType == null)
                docType = "";

            try
            {
                var cf = await Utilities.GetBlobLinksAsync(_containerClient, "PayorId", id, docType.ToLower());
                return Ok(cf);
            }
            catch (Exception ex)
            {
                _logger.LogError($@"Error retrieving files for ArbitrationCase {id}");
                _logger.LogError(ex.Message);
                return BadRequest($@"Error retrieving files for ArbitrationCase {id}");
            }
        }

        [HttpGet]
        [Route("{id}/groups/{group}")]
        public async Task<ActionResult<PayorGroup?>> GetPayorGroupAsync(int id, string group)
        {
            try
            {
                var g = await _context.PayorGroups.AsNoTracking().FirstOrDefaultAsync(d => d.PayorId == id && d.GroupNumber == group);
                if (g == null)
                    return NotFound();
                return Ok(g);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("groups/import")]
        public async Task<ActionResult<PayorGroupResponse>> ImportPayorGroupsAsync([FromForm] IFormFile file)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsSystem && !u.IsAdmin && !u.IsManager)
                return Unauthorized("Only global Admins and Managers can add new Payors.");

            // Use CSV Library to handle EHR Imports
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header.ToLower(),
                    BadDataFound = null, //args => Console.Write(args.RawRecord.ToString()), <- this does not work
                    HasHeaderRecord = true,
                    HeaderValidated = null, //args => Console.Write(string.Join("', '", args.InvalidHeaders?.FirstOrDefault()?.Names)),
                    MissingFieldFound = null //args => Console.WriteLine($"Field with names ['{string.Join("', '", args.HeaderNames)}'] at index '{args.Index}' was not found. ")
                };

                IEnumerable<PayorGroupImport>? records = null;

                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, config))
                {
                    records = csv.GetRecords<PayorGroupImport>().ToList();
                }

                // add groups one by one if they don't already exist
                int groupsAdded = 0;
                int groupsSkipped = 0;
                var payorsSkipped = new List<string>();
                var uploadDate = Utilities.GetCurrentUtcDate();
                var log = new StringBuilder($@"{uploadDate.ToShortTimeString()} Successfully parsed {records.Count()} records.");
                log.AppendLine();
                log.AppendLine();

                var payorNames = records.Select(d => d.PayorName).Distinct();
                foreach (var name in payorNames)
                {
                    var payor = await Utilities.GetPayorByAliasAsync(_context, name);
                    if (payor != null)
                    {
                        log.AppendLine($@"* Found prime payor {payor.Name} using name {name}.");

                        foreach (var g in records.Where(n => n.PayorName == name).ToArray())
                        {
                            var group = payor.PayorGroups.FirstOrDefault(d => d.GroupNumber.Equals(g.GroupNumber, StringComparison.CurrentCultureIgnoreCase) || d.GroupName.Equals(g.GroupName, StringComparison.CurrentCultureIgnoreCase));
                            if (group == null)
                            {
                                groupsAdded++;
                                payor.PayorGroups.Add(new PayorGroup { GroupName = g.GroupName, GroupNumber = g.GroupNumber, IsNSAIneligible = g.IsNSAIneligible, IsStateIneligible = g.IsStateIneligible, PayorId = payor.Id, PlanType = g.PlanType, UpdatedBy = u.Email, UpdatedOn = uploadDate });
                            }
                            else
                            {
                                groupsSkipped++;
                                log.AppendLine($@"!! Group already exists: {g.GroupNumber} / {g.GroupName}");
                            }
                        }
                    }
                    else
                    {
                        payorsSkipped.Add(name);
                        log.AppendLine($@"?? Unable to locate a Payor with the name {name}. Records skipped.");
                    }
                    log.AppendLine();
                }

                if (_context.ChangeTracker.HasChanges())
                    await _context.SaveChangesAsync();

                string sm = payorsSkipped.Count > 0 ? String.Join(',', payorsSkipped.ToArray()) : "none";
                log.AppendLine($@"Groups Added: {groupsAdded}, Groups Skipped (duplicates): {groupsSkipped}, Payors Skipped (not found): {sm}");
                return Ok(new PayorGroupResponse { Message = log.ToString(), ItemsAdded = groupsAdded, ItemsSkipped = groupsSkipped, ItemsUpdated = 0, PayorsSkipped = payorsSkipped.ToArray() });
            }
            catch (TypeConverterException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<Payor>> AddPayorAsync([FromBody] Payor payor)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin && !u.IsManager)
                return Unauthorized("Only global Admins and Managers can add new Payors.");

            if (payor.Id != 0)
                return BadRequest("Object is not new!");

            if (string.IsNullOrEmpty(payor.Name))
                return BadRequest("Name is required");

            if (!string.IsNullOrEmpty(payor.NSARequestEmail) && !Utilities.IsValidEmail(payor.NSARequestEmail))
                return BadRequest("Invalid NSARequestEmail");

            try
            {
                // hard-coded default for any new Payor that provides no JSON / template info
                if (string.IsNullOrEmpty(payor.JSON) || payor.JSON == "{}")
                {
                    var bcbstx = await _context.Payors.FirstOrDefaultAsync(d => d.Name.Equals("BCBSTX"));
                    if (bcbstx != null)
                        payor.JSON = bcbstx.JSON;
                }


                // NOTE: PayorGroups is ignored - simply too large of a list to process
                if (payor.PayorGroups.Count > 0)
                    payor.PayorGroups = new List<PayorGroup>();

                payor.UpdatedBy = u.Email;
                payor.UpdatedOn = Utilities.GetCurrentUtcDate();

                await _context.Payors.AddAsync(payor);
                await _context.SaveChangesAsync();

                if (payor.ParentId <= 0)
                {
                    payor.ParentId = payor.Id;
                    await _context.SaveChangesAsync();
                }
                return Ok(payor);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<Payor>> UpdatePayorAsync([FromBody] Payor payor)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin && !u.IsManager && !u.IsSystem)
                return Unauthorized("Only Admins and Managers can update Payors.");

            if (payor.Id < 1)
                return NotFound("Bad record id. Did you mean to create?");

            if (payor.ParentId <= 0)
                payor.ParentId = payor.Id;

            try
            {
                // find existing payor
                var orig = await _context.Payors.Include(a => a.Addresses).FirstOrDefaultAsync(d => d.Id == payor.Id);
                if (orig == null)
                    return NotFound("Record not found");

                // prevent nesting of payors beyond one level
                if (payor.ParentId != payor.Id)
                {
                    var parent = await _context.Payors.FindAsync(payor.ParentId);
                    if (parent == null)
                        return BadRequest("Bad ParentId specified.");
                    else if (parent.Id != parent.ParentId)
                        return BadRequest("Only one level of nesting is allowed.");
                }

                // validate all email addresses
                foreach (var g in payor.NSARequestEmail.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!Utilities.IsValidEmail(g))
                        throw new Exception(g + " is not a valid email address");
                }

                // NOTE: PayorGroups is ignored - simply too large of a list to process
                if (payor.PayorGroups.Count > 0)
                    payor.PayorGroups = new List<PayorGroup>();

                bool hasUpdates = false;

                _context.Entry(orig).CurrentValues.SetValues(payor);

                foreach (var a in payor.Addresses)
                {
                    var og = orig.Addresses.FirstOrDefault(d => d.Id == a.Id);
                    if (og == null && a.Id == 0)
                    {
                        og = new PayorAddress();
                        og.PayorId = orig.Id;
                    }

                    if (og == null)
                        continue;

                    og.AddressLine1 = a.AddressLine1;
                    og.AddressLine2 = a.AddressLine2;
                    og.AddressType = a.AddressType;
                    og.City = a.City;
                    og.Email = a.Email;
                    og.Name = a.Name;
                    og.Phone = a.Phone;
                    og.StateCode = a.StateCode;
                    og.ZipCode = a.ZipCode;

                    if (og.Id == 0)
                    {
                        og.UpdatedBy = a.UpdatedBy;
                        og.UpdatedOn = DateTime.UtcNow;
                        orig.Addresses.Add(og);
                        hasUpdates = true;
                    }
                    else if (_context.Entry(a).State == EntityState.Modified)
                    {
                        og.UpdatedBy = a.UpdatedBy;
                        og.UpdatedOn = DateTime.UtcNow;
                        hasUpdates = true;
                    }
                }

                // update stats even when an address changed
                if (hasUpdates || _context.Entry(orig).State == EntityState.Modified)
                {
                    payor.UpdatedOn = DateTime.UtcNow;
                    payor.UpdatedBy = u.Email;
                    await _context.SaveChangesAsync();
                }

                return Ok(payor);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #region PayorGroups
        [HttpPost("{id}/groups")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<PayorGroup>> AddPayorGroupAsync(int id, [FromBody] PayorGroup group)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin && !u.IsManager)
                return Unauthorized("Only global Admins and Managers can add new Payor Groups.");

            if (group.Id != 0)
                return BadRequest("Object is not new!");

            if (group.PayorId != id)
                return BadRequest("PayorId does not match endpoint parameter");

            if (string.IsNullOrEmpty(group.GroupNumber) || string.IsNullOrEmpty(group.GroupName))
                return BadRequest("GroupName and GroupNumber are required");


            try
            {
                // get Parent payor - all groups are associated there
                var p = await _context.Payors.FindAsync(id);
                if (p == null)
                    return BadRequest("Payor not found");
                if (p.Id != p.ParentId)
                    p = await _context.Payors.FindAsync(p.ParentId);
                if (p == null)
                    return BadRequest("Parent Payor is missing!");

                var g = await _context.PayorGroups.FirstOrDefaultAsync(d => d.GroupNumber == group.GroupNumber || d.GroupName == group.GroupName);
                if (g != null)
                    return BadRequest("Group already exists");

                group.UpdatedOn = Utilities.GetCurrentUtcDate();
                group.UpdatedBy = u.Email;

                await _context.PayorGroups.AddAsync(group);
                await _context.SaveChangesAsync();

                return Ok(group);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/groups")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<PayorGroup>> UpdatePayorGroupAsync(int id, [FromBody] PayorGroup group)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin && !u.IsManager)
                return Unauthorized("Only global Admins and Managers can edit Payor Groups.");

            if (group.Id <= 0)
                return BadRequest("Invalid group id");

            if (group.PayorId != id)
                return BadRequest("PayorId does not match endpoint parameter");

            if (string.IsNullOrEmpty(group.GroupNumber) || string.IsNullOrEmpty(group.GroupName))
                return BadRequest("GroupName and GroupNumber are required");

            try
            {
                var p = await _context.Payors.Include(d => d.PayorGroups).FirstOrDefaultAsync(d => d.Id == id);
                if (p == null)
                    return BadRequest("Payor not found");

                // make sure this won't create a duplicate
                var x = p.PayorGroups.FirstOrDefault(d => d.Id != group.Id && (d.GroupNumber == group.GroupNumber || d.GroupName == group.GroupName));
                if (x != null)
                    return BadRequest("Update would create a duplicate PayorGroup");

                // make sure the PayorGroup record already exists
                x = p.PayorGroups.FirstOrDefault(d => d.Id == group.Id);
                if (x == null)
                    return BadRequest("Record not found. Did you mean to POST?");

                _context.Entry(x).CurrentValues.SetValues(group);

                if (_context.Entry(x).State == EntityState.Modified)
                {
                    x.UpdatedBy = u.Email;
                    u.UpdatedOn = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Ok(group);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion
    }
}
