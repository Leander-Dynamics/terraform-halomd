using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MPArbitration.Model;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Metadata;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Security.Principal;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Nodes;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using System.Formats.Asn1;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Diagnostics;

// See https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-desktop-acquire-token-username-password?tabs=dotnet
// for information regarding token generation in case we want to support API access outside of this application scope
namespace MPArbitration.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ArbitrationController : MPBaseController
    {
        private readonly ILogger<ArbitrationController> _logger;
        private readonly IImportDataSynchronizer _synchronizer;
        //private readonly IMemoryCache _memoryCache;
        private readonly ArbitrationDbContext _errorContext;

        public ArbitrationController(ILogger<ArbitrationController> logger, ArbitrationDbContext context, IConfiguration configuration, IImportDataSynchronizer synchronizer) : base(context, configuration)
        {
            //_memoryCache = cache;
            _logger = logger;
            _synchronizer = synchronizer;


            var contextOptions = new DbContextOptionsBuilder<ArbitrationDbContext>()
                .UseSqlServer(_configuration.GetSection("ConnectionStrings").GetSection("ConnStr").Value)
                .Options;
            _errorContext = new ArbitrationDbContext(contextOptions);
        }

        /// <summary>
        /// Returns a set of variables as of a certain date
        /// </summary>
        /// <returns></returns>
        [HttpGet("app/vars")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<CalculatorVariable>>> GetCalculatorVariables([FromQuery] DateTime? AsOf = null)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            try
            {
                return Ok(await Utilities.GetCalculatorVariablesAsync(_context, AsOf));
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    return BadRequest(ex.InnerException.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Add a new set of vars for a service line
        /// </summary>
        /// <returns></returns>
        [HttpPost("app/vars")]
        [Produces("application/json")]
        public async Task<ActionResult<CalculatorVariable>> UpdateCalculatorVariables(CalculatorVariable variables)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin)
                return Unauthorized("Only global Admins can change the Calculator Variables.");

            if (variables.Id != 0)
                return BadRequest("Bad Id");

            if (string.IsNullOrEmpty(variables.ServiceLine))
                return BadRequest("Bad ServiceLine");

            variables.CreatedBy = u.Email;
            variables.CreatedOn = Utilities.GetCurrentUtcDate();
            if (string.IsNullOrEmpty(variables.NSAOfferBaseValueFieldname))
                variables.NSAOfferBaseValueFieldname = "fh80thPercentileExtendedCharges";

            _context.CalculatorVariables.Add(variables);
            await _context.SaveChangesAsync();
            return Ok(variables);
        }
        [HttpGet("app/FieldConfig")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<ImportFieldConfig>>> GetFieldConfigs([FromQuery] string source)
        {
            try
            {
                var u = await GetCurrentUser();
                if (u == null)
                    return Unauthorized("No active User context!");
                if (!u.IsAdmin && !u.IsManager)
                    return Unauthorized("Only Managers and Admins can view import configurations.");

                return Ok(await _context.ImportFieldConfigs.Where(d => d.Source.Equals(source)).ToArrayAsync());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("app/FieldConfig/{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<ImportFieldConfig>> UpdateFieldConfig(int id, [FromBody] ImportFieldConfig config)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Select(x => x!.Value!.Errors)
                           .Where(y => y.Count > 0).ToList();
                return BadRequest(errors);
            }
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin)
                return Unauthorized("Only Admin can modify import configurations");

            if (id != config.Id)
                return BadRequest("Identity mismatch");

            var ImportFieldConfig = _context.ImportFieldConfigs.Find(id);
            if (ImportFieldConfig == null)
                return NotFound("Record not found");

            if (ImportFieldConfig.TargetFieldname.Contains("status", StringComparison.CurrentCultureIgnoreCase))
                return BadRequest("Permission denied for status configurations.");

            try
            {
                config.UpdatedOn = DateTime.UtcNow;
                config.UpdatedBy = u.Email;

                _context.Entry(ImportFieldConfig).CurrentValues.SetValues(config);
                await _context.SaveChangesAsync();
                return Ok(config);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("app/currentuser")]
        [Produces("application/json")]
        public async Task<ActionResult<AppUser>> CurrentUser()
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            return Ok(u);
        }

        [HttpGet("app/emr/pos")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<PlaceOfServiceCode>>> GetPlaceOfServiceCodesAsync()
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            try
            {
                return Ok(await _context.PlaceOfServiceCodes.ToArrayAsync());
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("app/holidays")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Holiday>>> GetHolidays()
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            try
            {
                var h = await _context.Holidays.ToArrayAsync(); // dates will be GMT but should be CST
                return Ok(h);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("app/jobs/byId/{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<JobQueueItem>> GetJobQueueItemByIdAsync(int id)
        {
            try
            {
                var h = await _context.JobQueueItems.FindAsync(id);
                if (h == null)
                    return NotFound("Item not found");
                return Ok(h);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("app/jobs/byType/{jobType}")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<JobQueueItem>>> GetActiveJobQueueItemsByTypeAsync(string jobType)
        {

            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            // Only support one type at the moment.
            // This is intentionally "abstract" because the need for different types
            // of jobs and status reporting is still in flux.


            string low = jobType.ToLower();
            string Q = "";
            var serverName = this.Request.HttpContext.GetServerVariable("INSTANCE_ID");
            switch (low)
            {
                case "active":
                    Q = $@"SELECT Id, JSON, UpdatedOn, UpdatedBy FROM [dbo].[JobQueueItems] WHERE JSON_VALUE(JSON,'$.serverName') like '{serverName}' and JSON_VALUE(JSON,'$.status') not in ('error','finished')";
                    break;

                case "recalculate":
                    Q = $@"SELECT [Id],[JSON],[UpdatedBy],[UpdatedOn] FROM [dbo].[JobQueueItems] WHERE JSON_VALUE(JSON,'$.serverName') like '{serverName}' and JSON_VALUE(JSON,'$.jobType') like '{jobType}%' and JSON_VALUE(JSON,'$.status') not in ('error','finished')";
                    break;

                default:
                    return BadRequest("Invalid Job Type");
            }

            try
            {
                var recsQ = _context.Set<JobQueueItem>().FromSqlRaw<JobQueueItem>(Q);
                var recs = await recsQ.ToArrayAsync();
                if (recs == null)
                    recs = new JobQueueItem[] { };
                return Ok(recs);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("app/settings")]
        [Produces("application/json")]
        public async Task<ActionResult<AppSettings>> GetAppSettingsAsync()
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            // select and return data
            try
            {
                var result = await _context.AppSettings.OrderBy(d => d.Id).LastAsync();
                if (result == null)
                    return NotFound();

                return Ok(result);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("app/settings")]
        [Produces("application/json")]
        public async Task<ActionResult<AppSettings>> UpdateAppSettingsAsync(AppSettings settings)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin)
                return Unauthorized("Only Administrators can update Application Settings");
            if (settings.Id < 1)
                return NotFound("Id not found");
            if (!settings.JSON.StartsWith("{") || !settings.JSON.EndsWith("}"))
                return BadRequest("Invalid JSON value");


            // select and return data
            try
            {
                var goodies = JsonNode.Parse(settings.JSON);
                if (goodies == null || goodies.AsObject().Count() == 0)
                    return BadRequest("Empty JSON value is not allowed");

                var originalSettings = await _context.AppSettings.FirstOrDefaultAsync(d => d.Id == settings.Id);
                if (originalSettings == null)
                    return NotFound("Record not found");

                originalSettings.JSON = settings.JSON;
                originalSettings.UpdatedOn = Utilities.GetCurrentUtcDate();
                originalSettings.UpdatedBy = u.Email;

                await _context.SaveChangesAsync();
                return Ok(originalSettings);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("app/users")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetAppUsers()
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            // select and return data
            var result = await _context.AppUsers.ToListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Provides a download of individual items according to healt metrics
        /// </summary>
        /// <param name="r">Which report (metric) to generate</param>
        /// <param name="c">Optional Customer filter</param>
        /// <returns></returns>
        [HttpGet("app/health/items")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<AppHealthDetail>?>> GetHealthItemsAsync([FromQuery] string r, [FromQuery] string? c = "")
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (string.IsNullOrEmpty(r))
                return BadRequest();

            ArbitrationCase[]? m = null;
            IQueryable<ArbitrationCase>? q = null;
            IEnumerable<AppHealthDetail>? ret = null;
            bool hasCustomer = !string.IsNullOrEmpty(c);

            try
            {
                switch (r)
                {
                    case "chg":
                        q = from ac in _context.ArbitrationCases.Where(d => d.IsDeleted == false && (!hasCustomer || d.Customer == c))
                            join cpt in _context.ClaimCPT.Where(b => b.isDeleted == false)
                                    on ac.Id equals cpt.ArbitrationCaseId into LJ
                            from res in LJ.DefaultIfEmpty()
                            select ac;

                        m = await q.Where(d => d.CPTCodes.Count() == 0).ToArrayAsync();
                        break;

                    case "cust":
                        q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.Customer == "");
                        q = q.AddCondition(() => hasCustomer, d => d.Customer == c);
                        m = await q.ToArrayAsync();
                        break;

                    case "dob":
                        q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.ServiceLine != "" && d.DOB == null);
                        q = q.AddCondition(() => hasCustomer, d => d.Customer == c);
                        m = await q.ToArrayAsync();
                        break;

                    case "eob":
                        q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.ServiceLine != "" && d.EOBDate == null);
                        q = q.AddCondition(() => hasCustomer, d => d.Customer == c);
                        m = await q.ToArrayAsync();
                        break;

                    case "ent":
                        q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.ServiceLine != "" && (d.Entity == "" || d.EntityNPI == ""));
                        q = q.AddCondition(() => hasCustomer, d => d.Customer == c);
                        m = await q.ToArrayAsync();
                        break;

                    case "frd":
                        q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.ServiceLine != "" && d.FirstResponseDate == null);
                        q = q.AddCondition(() => hasCustomer, d => d.Customer == c);
                        m = await q.ToArrayAsync();
                        break;

                    case "pat":
                        q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.ServiceLine != "" && d.PatientName == "");
                        q = q.AddCondition(() => hasCustomer, d => d.Customer == c);
                        m = await q.ToArrayAsync();
                        break;

                    case "pcn":
                        q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.ServiceLine != "" && d.PayorClaimNumber.Length < 3);
                        q = q.AddCondition(() => hasCustomer, d => d.Customer == c);
                        m = await q.ToArrayAsync();
                        break;

                    case "prov":
                        q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.ServiceLine != "" && (d.ProviderName == "" || d.ProviderNPI == ""));
                        q = q.AddCondition(() => hasCustomer, d => d.Customer == c);
                        m = await q.ToArrayAsync();
                        break;

                    case "rfc":
                        q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.ServiceLine != "" && d.ReceivedFromCustomer == null);
                        q = q.AddCondition(() => hasCustomer, d => d.Customer == c);
                        m = await q.ToArrayAsync();
                        break;

                    case "svc":
                        q = _context.ArbitrationCases.Where(d => !d.IsDeleted && (d.Service == "" || d.ServiceDate == null || d.ServiceLine == ""));
                        q = q.AddCondition(() => hasCustomer, d => d.Customer == c);
                        m = await q.ToArrayAsync();
                        break;

                    default:
                        return BadRequest();
                }

                if (m != null)
                {
                    ret = from a in m
                          select new AppHealthDetail
                          {
                              Id = a.Id,
                              AuthorityStatus = a.AuthorityStatus,
                              Authority = a.Authority,
                              CreatedOn = a.CreatedOn,
                              Customer = a.Customer,
                              DOB = a.DOB,
                              EOBDate = a.EOBDate,
                              Entity = a.Entity,
                              EntityNPI = a.EntityNPI,
                              FirstResponseDate = a.FirstResponseDate,
                              NSAStatus = a.NSAStatus,
                              PatientName = a.PatientName,
                              PayorClaimNumber = a.PayorClaimNumber,
                              ProviderName = a.ProviderName,
                              ProviderNPI = a.ProviderNPI,
                              ReceivedFromCustomer = a.ReceivedFromCustomer,
                              Payor = a.Payor,
                              Service = a.Service,
                              ServiceDate = a.ServiceDate,
                              ServiceLine = a.ServiceLine
                          };
                }
                return Ok(ret);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        /// <summary>
        /// Generates system health statistics
        /// </summary>
        /// <param name="c">Optionally filter by Customer name</param>
        /// <returns></returns>
        [HttpGet("app/health")]
        [Produces("application/json")]
        public async Task<ActionResult<string>> GetSystemMetrics([FromQuery] string? c = "")
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            bool byCustomer = !string.IsNullOrEmpty(c);

            try
            {
                var mcust = await _context.ArbitrationCases.CountAsync(d => !d.IsDeleted && d.Customer == "");

                var q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.DOB == null);
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var mdob = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.EOBDate == null);
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var eob = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.PatientName == "");
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var mpat = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && !d.FirstResponseDate.HasValue);
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var frd = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.PayorClaimNumber.Length < 3);
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var pcn = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => d.PayorClaimNumber.Length < 3 &&
                                                                                !d.IsDeleted && d.ServiceLine != "ANES" &&
                                                                                d.Status != ArbitrationStatus.ClosedPaymentReceived &&
                                                                                d.Status != ArbitrationStatus.ClosedPaymentWithdrawn &&
                                                                                d.Status != ArbitrationStatus.Ineligible &&
                                                                                d.Status != ArbitrationStatus.SettledArbitrationPendingPayment &&
                                                                                d.Status != ArbitrationStatus.SettledInformalPendingPayment &&
                                                                                d.Status != ArbitrationStatus.SettledOutsidePendingPayment);
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var pcnActive = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => !d.IsDeleted && (string.IsNullOrEmpty(d.Service) || string.IsNullOrEmpty(d.ServiceLine)));
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var svc = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => !d.IsDeleted &&
                                                        (string.IsNullOrEmpty(d.Service) || string.IsNullOrEmpty(d.ServiceLine)) &&
                                                        d.Status != ArbitrationStatus.ClosedPaymentReceived &&
                                                        d.Status != ArbitrationStatus.ClosedPaymentWithdrawn &&
                                                        d.Status != ArbitrationStatus.Ineligible &&
                                                        d.Status != ArbitrationStatus.SettledArbitrationPendingPayment &&
                                                        d.Status != ArbitrationStatus.SettledInformalPendingPayment &&
                                                        d.Status != ArbitrationStatus.SettledOutsidePendingPayment);

                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var svcActive = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && (string.IsNullOrEmpty(d.Entity) || string.IsNullOrEmpty(d.EntityNPI)));
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var ent = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => (string.IsNullOrEmpty(d.Entity) || string.IsNullOrEmpty(d.EntityNPI)) &&
                                                                                !d.IsDeleted && d.ServiceLine != "ANES" &&
                                                                                d.Status != ArbitrationStatus.ClosedPaymentReceived &&
                                                                                d.Status != ArbitrationStatus.ClosedPaymentWithdrawn &&
                                                                                d.Status != ArbitrationStatus.Ineligible &&
                                                                                d.Status != ArbitrationStatus.SettledArbitrationPendingPayment &&
                                                                                d.Status != ArbitrationStatus.SettledInformalPendingPayment &&
                                                                                d.Status != ArbitrationStatus.SettledOutsidePendingPayment);
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var entActive = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && (string.IsNullOrEmpty(d.ProviderName) || string.IsNullOrEmpty(d.ProviderNPI)));
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var prv = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => !d.IsDeleted && d.ServiceLine != "ANES" && d.ReceivedFromCustomer == null);
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var rfc = await q.CountAsync();

                q = _context.ArbitrationCases.Where(d => (string.IsNullOrEmpty(d.ProviderName) || string.IsNullOrEmpty(d.ProviderNPI)) &&
                                                                                !d.IsDeleted && d.ServiceLine != "ANES" &&
                                                                                d.Status != ArbitrationStatus.ClosedPaymentReceived &&
                                                                                d.Status != ArbitrationStatus.ClosedPaymentWithdrawn &&
                                                                                d.Status != ArbitrationStatus.Ineligible &&
                                                                                d.Status != ArbitrationStatus.SettledArbitrationPendingPayment &&
                                                                                d.Status != ArbitrationStatus.SettledInformalPendingPayment &&
                                                                                d.Status != ArbitrationStatus.SettledOutsidePendingPayment);
                q = q.AddCondition(() => byCustomer, d => d.Customer == c);
                var prvActive = await q.CountAsync();

                q = from ac in _context.ArbitrationCases.Where(d => d.IsDeleted == false && (!byCustomer || d.Customer == c))
                    join cpt in _context.ClaimCPT.Where(b => b.isDeleted == false)
                            on ac.Id equals cpt.ArbitrationCaseId into LJ
                    from res in LJ.DefaultIfEmpty()
                    select ac;

                var cct = await q.CountAsync(m => m.CPTCodes.Count == 0);

                var json = new
                {
                    missingCharges = cct,
                    missingCustomer = mcust,
                    missingDOB = mdob,
                    missingEOBDate = eob,
                    missingEntity = ent,
                    missingEntityActive = entActive,
                    missingFirstResponseDate = frd,
                    missingPatientName = mpat,
                    missingPayorClaimNumber = pcn,
                    missingPayorClaimNumberActive = pcnActive,
                    missingProvider = prv,
                    missingProviderActive = prvActive,
                    missingReceivedFromCustomer = rfc,
                    missingService = svc,
                    missingServiceActive = svcActive
                };

                return Ok(json);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("app/users")]
        [Produces("application/json")]
        public async Task<ActionResult<ImportFieldConfig>> CreateAppUser([FromBody] AppUser newUser)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin)
                return Unauthorized("Only global Admins can add new Users.");

            if (newUser.Id != 0)
                return BadRequest("Cannot perform an Update on this endpoint. Check the object id.");

            if (string.IsNullOrEmpty(newUser.Email))
                return BadRequest("Validation failed: Email");

            if (!Utilities.IsValidEmail(newUser.Email))
                return BadRequest("Validation failed: Email");

            // TODO: Add the ability for Managers to update users but make sure they cannot elevate any user's security ABOVE manager!

            // prevent creating duplicate records
            var origAppUser = await _context.AppUsers.FirstOrDefaultAsync(d => d.Email == newUser.Email);
            if (origAppUser != null)
                return BadRequest("User record already exists!");

            // validate the data
            if (newUser.Roles.Length > 100)
                return BadRequest("What?"); // new user data seems like garbage - return nonsense
            if (newUser.Email.Length > 100)
                return BadRequest("What?"); // new user data seems like garbage - return nonsense

            var roles = newUser.Roles.ToLower().Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            string nope = "Invalid entry";
            foreach (var r in roles)
            {
                if (!r.Contains('|'))
                {
                    // validate global role
                    if (!VALID_ROLES.Contains(r))
                        return BadRequest(nope);
                }
                else
                {
                    // validate granular role
                    var p = r.Split('|');
                    if (p.Length != 3)
                        return BadRequest(nope);
                    if (p[0] != "a" && p[0] != "c")
                        return BadRequest(nope);
                    if (!int.TryParse(p[1], out int entityId))
                        return BadRequest(nope);
                    // c|1|manager    
                    if (!Enum.IsDefined(typeof(UserAccessType), p[2]))
                        return BadRequest(nope);
                    if (p[0] == "a" && await _context.Authorities.FindAsync(entityId) == null)
                        return BadRequest(nope);
                    if (p[0] == "c" && await _context.Customers.FindAsync(entityId) == null)
                        return BadRequest(nope);
                }
            }

            try
            {
                // create a tracked object
                origAppUser = new AppUser();

                origAppUser.Email = newUser.Email;
                origAppUser.IsActive = newUser.IsActive;
                origAppUser.Roles = newUser.Roles;
                origAppUser.UpdatedOn = DateTime.UtcNow;
                origAppUser.UpdatedBy = u.Email;

                await _context.AppUsers.AddAsync(origAppUser);
                await _context.SaveChangesAsync();
                return Ok(origAppUser);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("app/users/{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<ImportFieldConfig>> UpdateAppUser(int id, [FromBody] AppUser user)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin)
                return Unauthorized("Only global Admins can manage Users.");

            if (id <= 0 || user.Id <= 0)
                return BadRequest("Cannot Create on this endpoint");

            if (id != user.Id)
                return BadRequest("Identity mismatch");

            if (string.IsNullOrEmpty(user.Email))
                return BadRequest("Validation failed: Email");

            if (!Utilities.IsValidEmail(user.Email))
                return BadRequest("Validation failed: Email");

            // validate the data
            if (user.Roles.Length > 100)
                return BadRequest("What?");// new user data seems like garbage - return nonsense
            if (user.Email.Length > 100)
                return BadRequest("What?");// new user data seems like garbage - return nonsense

            // find existing record
            var orig = await _context.AppUsers.FindAsync(id);
            if (orig == null)
                return NotFound("Record not found");

            var roles = user.Roles.ToLower().Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            string nope = "Invalid entry";
            foreach (var r in roles)
            {
                if (!r.Contains('|'))
                {
                    // validate global role
                    if (!VALID_ROLES.Contains(r))
                        return BadRequest(nope);
                }
                else
                {
                    // validate granular role
                    var p = r.Split('|');
                    if (p.Length != 3)
                        return BadRequest(nope);
                    if (p[0] != "a" && p[0] != "c")
                        return BadRequest(nope);
                    if (!int.TryParse(p[1], out int entityId))
                        return BadRequest(nope);
                    // c|1|manager    
                    if (!Enum.IsDefined(typeof(UserAccessType), p[2]))
                        return BadRequest(nope);
                    if (p[0] == "a" && await _context.Authorities.FindAsync(entityId) == null)
                        return BadRequest(nope);
                    if (p[0] == "c" && await _context.Customers.FindAsync(entityId) == null)
                        return BadRequest(nope);
                }
            }

            try
            {
                orig.UpdatedOn = DateTime.UtcNow;
                orig.UpdatedBy = u.Email;
                orig.Roles = user.Roles;
                orig.IsActive = user.IsActive;

                // do not permit changing email address since that is connected to the AD user itself
                // email is set during the Create User process only
                await _context.SaveChangesAsync();
                return Ok(orig);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET - Return array of Negotiators
        /* Moved to the Payors controller
        [HttpGet("app/negotiators/{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Negotiator>>> GetNegotiators(int id, [FromQuery]bool activeOnly = true)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            
            return await _context.Negotiators.Where(d => d.PayorId == id && (!activeOnly || d.IsActive)).ToArrayAsync();
        }
        */

        // POST - Create Negotiator
        [HttpPost("app/negotiators")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Negotiator>> AddNegotiator([FromBody] Negotiator negotiator)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin && !u.IsManager)
                return Unauthorized("Only global Managers and Admins can create Negotiators");

            if (negotiator.Id != 0)
                return BadRequest("Validation failed: Object is not new!");

            if (negotiator.PayorId < 1)
                return BadRequest("Validation failed: Missing parent reference id");

            var p = await _context.Payors.FindAsync(negotiator.PayorId);
            if (p == null)
                return NotFound("Bad parent reference");

            Negotiator newNegotiator = new Negotiator();
            _context.Entry(newNegotiator).CurrentValues.SetValues(negotiator);

            newNegotiator.UpdatedOn = Utilities.GetCurrentUtcDate();
            newNegotiator.UpdatedBy = u.Email;
            p.Negotiators.Add(newNegotiator);
            await _context.SaveChangesAsync();
            return Ok(newNegotiator);
        }

        // PUT - Update a Negotiator
        [HttpPut("app/negotiators")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<Negotiator>> UpdateNegotiator([FromBody] Negotiator negotiator)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin && !u.IsManager)
                return Unauthorized("Only Managers and Admin can update Negotiators");

            if (negotiator == null || negotiator.Id < 1)
                return BadRequest("Validation failed");

            var origNegotiator = _context.Negotiators.Find(negotiator.Id);
            if (origNegotiator == null)
                return NotFound("Record not found");

            try
            {
                negotiator.UpdatedOn = DateTime.UtcNow;
                negotiator.UpdatedBy = u.Email;

                _context.Entry(origNegotiator).CurrentValues.SetValues(negotiator);
                await _context.SaveChangesAsync();
                return Ok(origNegotiator);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authority">The authority brokering the settlement, e.g. TX</param>
        /// <param name="id">The authority's case id number</param>
        /// <returns></returns>
        [HttpGet("authority/{authority}/{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<TDIRequestDetails?>> ByAuthorityCaseId(string authority, string id)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            if (string.IsNullOrEmpty(id) || id.Length > 20 || string.IsNullOrEmpty(authority))
                return BadRequest("Invalid parameter(s)");

            // fetch the case info, if available, and initialize an ArbitrationCase object the user can start with
            if (authority.ToLower().Equals("tx"))
            {
                var filter = from r in _context.TDIRequests.Where(x => x.RequestId == id)
                                .OrderByDescending(x => x.BatchUploadDate)
                                .Take(1)
                             select r;

                var recs = await filter.ToListAsync();
                var tdiRecord = recs.FirstOrDefault();

                if (tdiRecord == null)
                    return Ok(null); // NotFound($@"Case {id} is not available in the {authority} Authority import repository."); NotFound appears as a critical error on the client and causes confusion
                else
                {
                    Utilities.FixRawTDIDates(tdiRecord);
                    return Ok(tdiRecord);
                }
            }
            else
            {
                return Ok(null);
            }
        }

        /// <summary>
        /// Gets log files from Azure blob storage for the past 14 days
        /// </summary>
        /// <param name="authority"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("app/import/log/{authority}")]
        public async Task<ActionResult<IEnumerable<CaseFile>>> GetUploadLogs(string authority)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin && !u.IsManager)
                return Unauthorized("Upload logs are only available to Admins and Managers");

            if (string.IsNullOrEmpty(authority))
                return BadRequest("Bad parameter");

            try
            {
                List<CaseFile> cf = new List<CaseFile>();
                var start = DateTime.Today.AddDays(-30);
                var dt = "AuthorityUploadLog";

                // reuse this service endpoint to handle system uploads a little differently instead of
                // making a whole new scheme to handle these file types (overkill)
                if (authority.StartsWith("dsp-") || authority.StartsWith("ehr-"))
                {
                    if (authority == "ehr-h")
                    {
                        dt = "EHRHeader";
                    }
                    else if (authority == "ehr-d")
                    {
                        dt = "EHRDetail";
                    }
                    else if (authority == "dsp-d")
                    {
                        dt = "DisputeDetails";
                    }
                    else if (authority == "dsp-f")
                    {
                        dt = "DisputeFees";
                    }
                    else if (authority == "dsp-h")
                    {
                        dt = "DisputeHeaders";
                    }
                    else if (authority == "dsp-n")
                    {
                        dt = "DisputeNotes";
                    }
                    authority = "system";
                }

                // NOTE: Upload logs' filenames are never saved as pure lowercase. This is a significant difference from how Case, Payor and other attachments are saved.
                string sql = $"\"DocumentType\"='{dt}' AND \"Authority\"='{authority}'"; // TODO: BatchUploadDate need to use yyyy-MM-dd hh:mm:ssZ format since it will always be treated as a string
                await foreach (var page in _containerClient.FindBlobsByTagsAsync(sql).AsPages())
                {
                    foreach (TaggedBlobItem item in page.Values)
                    {
                        var b = _containerClient.GetBlobClient(item.BlobName);
                        var c = await b.GetPropertiesAsync();
                        var tags = await b.GetTagsAsync();
                        var created = DateTime.SpecifyKind(c.Value.CreatedOn.DateTime, DateTimeKind.Utc);
                        if (created.CompareTo(start) >= 0)
                        {
                            var f = new CaseFile { BLOBName = item.BlobName, Tags = tags.Value.Tags, CreatedOn = created };
                            cf.Add(f);
                        }
                    }
                }

                if (authority.Equals("tx"))
                {
                    sql = $"\"DocumentType\"='SyncTDIsToCases'";
                    await foreach (var page in _containerClient.FindBlobsByTagsAsync(sql).AsPages())
                    {
                        foreach (TaggedBlobItem item in page.Values)
                        {
                            var b = _containerClient.GetBlobClient(item.BlobName);
                            var c = await b.GetPropertiesAsync();
                            var tags = await b.GetTagsAsync();
                            var created = DateTime.SpecifyKind(c.Value.CreatedOn.DateTime, DateTimeKind.Utc);
                            if (created.CompareTo(start) >= 0)
                            {
                                var f = new CaseFile { BLOBName = item.BlobName, Tags = tags.Value.Tags, CreatedOn = created };
                                cf.Add(f);
                            }
                        }
                    }
                }
                return Ok(cf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);
                var m = $@"Error retrieving files for Authority {authority}";
                _logger.LogError(m);
#if DEBUG
                _logger.LogError(ex.Message);
#endif
                return BadRequest(m);
            }
        }

        [HttpGet]
        [Route("app/import/blob/{authority}")]
        public async Task<IActionResult> ViewFile(string authority, [FromQuery] string name)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin && !u.IsManager)
                return Unauthorized("Upload logs are only available to Admin and Managers");

            if (string.IsNullOrEmpty(name))
                return BadRequest("Empty document name");

            var n = name.ToLower();
            if (!n.StartsWith($@"importLog-", StringComparison.InvariantCultureIgnoreCase) || !n.EndsWith(".log")) // "importLog-{authority.ToLower()}"
                return BadRequest("Invalid document name");

            string mimeType = "text/plain";

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

        /* Replaced with Generic
        private IEnumerable<AuthorityDisputeDetailsCSV> ReadDisputeDetails(IFormFile file, out string message)
        {
            // Use CSV Library to handle EHR Imports
            message = "";
            AuthorityDisputeDetailsCSV[] records = new AuthorityDisputeDetailsCSV[] { };
            CsvReader? csvReader = null;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                //PrepareHeaderForMatch = args => args.Header.ToLower(),
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null,
                HasHeaderRecord = true,
                HeaderValidated = null, //args => Console.Write(string.Join("', '", args.InvalidHeaders?.FirstOrDefault()?.Names)),
                MissingFieldFound = null, //args => Console.WriteLine($"Field with names ['{string.Join("', '", args.HeaderNames)}'] at index '{args.Index}' was not found. ")
            };

            using (var reader = new StreamReader(file.OpenReadStream()))
            using (csvReader = new CsvReader(reader, config))
            {
                try
                {
                    records = csvReader.GetRecords<AuthorityDisputeDetailsCSV>().ToArray();
                }
                catch (CsvHelperException ex)
                {
                    message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                }
                catch (Exception ex)
                {
                    message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                }
            }

            return records.Where(d => !string.IsNullOrEmpty(d.AuthorityCaseId) && d.AuthorityId > 0 && d.ArbitrationCaseId > 0 && !string.IsNullOrEmpty(d.ClaimCPTCode));
        }
        */

        /* Replaced with Generic
        private IEnumerable<AuthorityDisputeCSV> ReadDisputeHeaders(IFormFile file, out string message)
        {
            // Use CSV Library to handle EHR Imports
            message = "";
            AuthorityDisputeCSV[] records = new AuthorityDisputeCSV[] { };
            CsvReader? csvReader = null;
            
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                //PrepareHeaderForMatch = args => args.Header.ToLower(),
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null,
                HasHeaderRecord = true,
                HeaderValidated = null, //args => Console.Write(string.Join("', '", args.InvalidHeaders?.FirstOrDefault()?.Names)),
                MissingFieldFound = null, //args => Console.WriteLine($"Field with names ['{string.Join("', '", args.HeaderNames)}'] at index '{args.Index}' was not found. ")
            };

            using (var reader = new StreamReader(file.OpenReadStream()))
            using (csvReader = new CsvReader(reader, config))
            {
                try {
                    records = csvReader.GetRecords<AuthorityDisputeCSV>().ToArray();
                }
                catch (CsvHelperException ex)
                {
                    message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    
                }
                catch (Exception ex)
                {
                    message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                }
            }
            
            return records.Where(d=>!string.IsNullOrEmpty(d.AuthorityCaseId) && d.SubmissionDate > DateTime.MinValue && d.SubmissionDate < DateTime.MaxValue);
        }
        */

        /// <summary>
        /// Not yet in use. Attempt to handle EHR upload using CSV library.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        //private ActionResult ImportEHRHeader(IFormFile file)
        //{
        //    // Use CSV Library to handle EHR Imports
        //    try 
        //    {                 
        //        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        //        {
        //            PrepareHeaderForMatch = args => args.Header.ToLower(),
        //            BadDataFound = null,
        //            HasHeaderRecord = true,
        //            HeaderValidated = null, //args => Console.Write(string.Join("', '", args.InvalidHeaders?.FirstOrDefault()?.Names)),
        //            MissingFieldFound = null, //args => Console.WriteLine($"Field with names ['{string.Join("', '", args.HeaderNames)}'] at index '{args.Index}' was not found. ")
        //        };


        //        IEnumerable<ArbitrationCase>? records = null;

        //        using (var reader = new StreamReader(file.OpenReadStream()))
        //        using (var csv = new CsvReader(reader, config))
        //        {
        //            records = csv.GetRecords<ArbitrationCase>().ToList();

        //        }
        //        return Ok($@"Successfully parsed {records.Count()} records. Check the log file in a few minutes to see more feedback.");
        //    }
        //    catch(TypeConverterException ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message); 
        //    }

        //}

        [HttpPost]
        [Route("import")]
        public async Task<ActionResult<JobQueueItem>> ImportDataAsync([FromForm] IFormFile file, [FromForm] string authority)
        {
            var user = await GetCurrentUser();
            if (user == null)

                return Unauthorized("No active User context!");
            if (!user.IsAdmin && !user.IsManager && !user.IsSystem)
                return Unauthorized("Your role is not authorized to import data.");

            if (file == null)
                return BadRequest("No file detected!");
            else if (file.Length > 20000000)
                return BadRequest("File size is too large. Split into multiple uploads.");
            else if (!file.FileName.ToLower().EndsWith(".csv"))
                return BadRequest("Only CSV files allowed");
            else if (string.IsNullOrEmpty(authority))
                return BadRequest("Authority is required");
            var serverName = this.Request.HttpContext.GetServerVariable("INSTANCE_ID");
            var jobs = await _context.JobQueueItems.FromSqlRaw($@"SELECT Id, JSON, UpdatedOn, UpdatedBy FROM [JobQueueItems] WHERE JSON_VALUE(JSON,'$.serverName') like '{serverName}' and JSON_VALUE(JSON,'$.status') not in ('error','finished')").ToArrayAsync();
            if (jobs.Count() > 0)
            {
                //var first = jobs.First();
                //var msgObj = new { first.Id, first.JSON, first.UpdatedOn, first.UpdatedBy, message = "An active server job is already running. Only one server job is allowed at a time. Check back in 5 minutes to see if has completed." };
                return BadRequest("An active server job is already running. Only one server job is allowed at a time. Check back in 5 minutes to see if has completed.");
            }

            string authorityKey = authority.ToLower();
            var batchUploadDate = DateTime.Now;
            var log = new StringBuilder();
            var job = new JobQueueItem { Id = 0, JSON = "", UpdatedBy = user.Email, UpdatedOn = Utilities.GetCurrentUtcDate() };
            var jobJSON = new JsonObject();

            jobJSON.Add("jobType", "import|" + authorityKey);
            jobJSON.Add("recordsAdded", 0);
            jobJSON.Add("recordsError", 0);
            jobJSON.Add("recordsProcessed", 0);
            jobJSON.Add("totalRecords", 0);
            jobJSON.Add("recordsSkipped", 0);
            jobJSON.Add("recordsUpdated", 0);
            jobJSON.Add("status", "initializing");
            jobJSON.Add("startTime", Utilities.GetCurrentUtcDate());
            jobJSON.Add("lastUpdated", Utilities.GetCurrentUtcDate());
            jobJSON.Add("serverName", serverName);
            job.JSON = jobJSON.ToJsonString();
            var csvLines = new List<string>();
            try
            {
                // used as a status record the client can use for fetching ongoing status
                _errorContext.JobQueueItems.Add(job);

                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    while (reader.Peek() >= 0)
                    {
                        var line = reader.ReadLine() ?? "";
                        csvLines.Add(line);
                    }
                }

                #region Disputes Upload Path - Grouped EHR data that comprise an Arbit Dispute


                if (authorityKey.StartsWith("dsp-"))
                {
                    //Disputes Upload Path - Grouped EHR data that comprise an Arbit Dispute
                    return await DisputesUploadAsync(user, file, authorityKey, job, jobJSON);
                }
                #endregion

                #region EHR Upload Path
                if (authorityKey.Equals("ehr-d") || authorityKey.Equals("ehr-h"))
                {
                    var recType = authorityKey.Equals("ehr-d") ? EHRRecordType.Detail : EHRRecordType.Header;
                    var docType = recType == EHRRecordType.Detail ? "Detail" : "Header";

                    // save this file so we can refer to it later when someone uploads a bunch of garbage and then can't figure out why their data isn't showing up
                    await this.SaveUploadLog("system", Utilities.GetCurrentCSTDate2(), String.Join('\n', csvLines), _logger, $@"EHR {docType}");

                    jobJSON.Add("message", $@"EHR {docType} import process queued for processing. All other uploads will be rejected while this file is being processed.");
                    job.JSON = jobJSON.ToJsonString();
                    await _errorContext.SaveChangesAsync();

                    _errorContext.Entry(job).State = EntityState.Detached;

                    var task = Task.Factory.StartNew(() =>
                    {
                        _synchronizer.ImportEHR(csvLines, recType, user, job);
                    });

                    // return the Job object to the client so it knows what to monitor
                    return Ok(job);
                }
                #endregion

                var authorityRecord = await _context.Authorities.FirstOrDefaultAsync(d => d.Key == authorityKey);
                if (authorityRecord == null)
                    throw new Exception("Invalid authority identifier");

                #region Non-TX Authority import - TODO: Merge with Texas code into one, consistent Dispute import
                if (!authorityKey.Equals("tx"))
                {
                    /****************************** 
                     * The non-TX short circuit is here but is not yet complete! 
                     * Data will be brought into the database but things like Claim 
                     * synchronization, Arbitrator synchronization and other fine
                     * details have not been worked out or explored because at this time
                     * we do not have any automation specs for states other than Texas.
                     ******************************/
                    // save this file so we can refer to it later when someone uploads a bunch of garbage and then can't figure out why their data isn't showing up
                    await this.SaveUploadLog(authority, Utilities.GetCurrentCSTDate2(), String.Join('\n', csvLines), _logger, "AuthorityUpload");

                    // return a status record the client can use for fetching ongoing status
                    jobJSON.Add("message", $@"Authority Import for {authorityKey} queued for processing. All other uploads will be rejected while this file is being processed.");
                    job.JSON = jobJSON.ToJsonString();
                    if (_errorContext.Entry(job).State == EntityState.Detached)
                        _errorContext.Entry(job).State = EntityState.Modified;
                    await _errorContext.SaveChangesAsync();
                    _errorContext.Entry(job).State = EntityState.Detached;

                    // thread for processing the imported records - allows us to return control to the client
                    var t2 = Task.Factory.StartNew(() =>
                    {
                        _synchronizer.ImportAuthorityCases(authorityRecord, csvLines, user, job);
                    });
                }
                #endregion
                if (authorityKey.Equals("tx"))
                {
                    // Texas-specific upload code - needs merging into generic Dispute upload code path
                    log.Append(await ProcessTXImportAsync(user, job, authorityKey, jobJSON
                        , csvLines, batchUploadDate, authorityRecord));
                }
                return Ok(job);
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                string errorMessage = ex.InnerException == null ? ex.Message : ex.InnerException.Message;

                string message = Utilities.GetCurrentUtcDate().ToString("u") + $@" : Error saving records into TDIRequests table. {errorMessage}\nAborting job.";

                if (job.Id == 0)
                {
                    jobJSON.Add("message", message);
                    _errorContext.JobQueueItems.Add(job);
                }
                else
                {
                    jobJSON["message"] = $@"{message}\n{jobJSON["message"]}";
                }

                jobJSON["lastUpdated"] = Utilities.GetCurrentUtcDate();
                jobJSON["status"] = "error";
                job.JSON = jobJSON.ToJsonString();

                try
                {
                    if (_errorContext.Entry(job).State == EntityState.Detached)
                        _errorContext.Entry(job).State = EntityState.Modified;
                    await _errorContext.SaveChangesAsync();
                }
                catch { }


                return BadRequest(ex.Message);
            }
            finally
            {
                if (log != null)
                    await this.SaveUploadLog(authority, batchUploadDate, log.ToString(), _logger, "AuthorityUploadLog");
                if (csvLines != null)
                    await this.SaveUploadLog(authority, Utilities.GetCurrentCSTDate2(), string.Join('\n', csvLines), _logger, "AuthorityUpload"); // TODO: when TX gets handled by the generic upload method later, this line will not be necessary

                //_memoryCache.Remove(CACHE_KEY);
            }
        }

        private async Task<ActionResult<JobQueueItem>> DisputesUploadAsync(AppUser user, IFormFile csvFile, string authorityKey, JobQueueItem job, JsonObject jobJSON)
        {
            var ConnString = _context.Database.GetConnectionString();
            if (!user.IsLocalHost(Request.Host) && ConnString != null && !ConnString.Contains("Express20") && !ConnString.Contains("tcp:de-arb"))
                return BadRequest("This feature is not yet available in production");

            // Dispute CPT import
            if (authorityKey.Equals("dsp-d"))
            {
                var Recs = Utilities.ReadGenericCSVRecord<AuthorityDisputeDetailsCSV>(csvFile, out string message);
                var FilteredRecs = Recs.Where(d => !string.IsNullOrEmpty(d.AuthorityCaseId) && !string.IsNullOrEmpty(d.AuthorityKey) && d.ArbitrationCaseId > 0 && !string.IsNullOrEmpty(d.ClaimCPTCode));
                if (message != "")
                {
                    return BadRequest(message);
                }
                else if (FilteredRecs.Count() == 0)
                {
                    return BadRequest("Unable to detect any usable content. (Note: AuthorityCaseId and SubmissionDate are required!)");
                }
                else if (Recs.Count() != FilteredRecs.Count())
                {
                    return BadRequest("One or more lines in the upload file have invalid key values (AuthorityCaseId, AuthorityId");
                }

                jobJSON.Add("message", $@"Dispute CPT import process queued for processing. All other uploads will be rejected while this file is being processed.");
                job.JSON = jobJSON.ToJsonString();
                _errorContext.Entry(job).State = EntityState.Detached;

                var task = Task.Factory.StartNew(() =>
                {
                    _synchronizer.ImportDisputeDetailsAsync(FilteredRecs, user, job);
                });
                return Ok(job);
            }
            // Dispute Fee import
            else if (authorityKey.Equals("dsp-f"))
            {
                var Recs = Utilities.ReadGenericCSVRecord<AuthorityDisputeFeeCSV>(csvFile, out string message);
                var FilteredRecs = Recs.Where(d => !string.IsNullOrEmpty(d.AuthorityCaseId) && !string.IsNullOrEmpty(d.AuthorityKey) && !string.IsNullOrEmpty(d.FeeName));
                if (message != "")
                {
                    return BadRequest(message);
                }
                else if (Recs.Count() == 0)
                {
                    return BadRequest("Unable to detect any usable content. (Note: AuthorityId and AuthorityCaseId are required!)");
                }
                else if (Recs.Count() != FilteredRecs.Count())
                {
                    return BadRequest("One or more lines in the upload file have invalid key values (AuthorityCaseId, AuthorityId");
                }

                jobJSON.Add("message", $@"Dispute Fee import process queued for processing. All other uploads will be rejected while this file is being processed.");
                job.JSON = jobJSON.ToJsonString();
                _errorContext.Entry(job).State = EntityState.Detached;

                var task = Task.Factory.StartNew(() =>
                {
                    _synchronizer.ImportDisputeFeesAsync(Recs, user, job);
                });
                return Ok(job);
            }
            // Dispute Header import
            else if (authorityKey.Equals("dsp-h"))
            {
                var Recs = Utilities.ReadGenericCSVRecord<AuthorityDisputeCSV>(csvFile, out string message);
                var FilteredRecs = Recs.Where(d => !string.IsNullOrEmpty(d.AuthorityKey) && !string.IsNullOrEmpty(d.AuthorityCaseId) && d.SubmissionDate > DateTime.MinValue && d.SubmissionDate < DateTime.MaxValue);
                if (message != "")
                {
                    return BadRequest(message);
                }
                else if (FilteredRecs.Count() == 0)
                {
                    return BadRequest("Unable to detect any usable content. (Note: AuthorityCaseId and SubmissionDate are required!)");
                }
                else if (Recs.Count() != FilteredRecs.Count())
                {
                    return BadRequest("One or more lines in the upload file have invalid key values (AuthorityId, AuthorityCaseId, SubmissionDate");
                }

                jobJSON.Add("message", $@"Dispute Header import process queued for processing. All other uploads will be rejected while this file is being processed.");
                job.JSON = jobJSON.ToJsonString();

                await _errorContext.SaveChangesAsync();
                _errorContext.Entry(job).State = EntityState.Detached;

                var task = Task.Factory.StartNew(() =>
                {
                    _synchronizer.ImportDisputeHeadersAsync(FilteredRecs, user, job);
                });

            }// Dispute Header import
            else if (authorityKey.Equals("dsp-n"))
            {
                var Recs = Utilities.ReadGenericCSVRecord<AuthorityDisputeNoteCSV>(csvFile, out string message);
                var FilteredRecs = Recs.Where(d => !string.IsNullOrEmpty(d.AuthorityKey) && !string.IsNullOrEmpty(d.AuthorityCaseId) && !string.IsNullOrEmpty(d.Details));
                if (message != "")
                {
                    return BadRequest(message);
                }
                else if (FilteredRecs.Count() == 0)
                {
                    return BadRequest("Unable to detect any usable content.");
                }
                else if (Recs.Count() != FilteredRecs.Count())
                {
                    return BadRequest("One or more lines in the upload file have invalid key values (AuthorityId, AuthorityCaseId, Details");
                }

                jobJSON.Add("message", $@"Dispute Notes import process queued for processing. All other uploads will be rejected while this file is being processed.");
                job.JSON = jobJSON.ToJsonString();

                await _errorContext.SaveChangesAsync();
                _errorContext.Entry(job).State = EntityState.Detached;

                var task = Task.Factory.StartNew(() =>
                {
                    _synchronizer.ImportDisputeNotesAsync(FilteredRecs, user, job);
                });

            }

            // return the Job object to the client for monitoring
            return Ok(job);
        }

        private async Task<string> ProcessTXImportAsync(
            AppUser user, JobQueueItem job, string authorityKey
            , JsonObject jobJSON, List<string> csvLines
            , DateTime batchUploadDate, Authority? authorityRecord)
        {
            var log = new StringBuilder();

            var importFieldConfigList = await _context.ImportFieldConfigs.Where(d => d.Source.Equals("TDIRequestDetails")).ToListAsync();
            if (importFieldConfigList.Count() == 0)
                throw new Exception("System error: There are no import configurations available for this type of file.");

            int recordsFound = 0;
            int recordsSkipped = 0;
            int rowCount = 0;

            log = new StringBuilder();
            log.AppendLine($@"Import Data from {authorityKey} on {batchUploadDate.ToString("MM/dd/yyyy hh:mm tt")}");

            var CSVParserRegex = new Regex("[,|](?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

            // save status job 
            jobJSON.Add("message", $@"Reading CSV data into system objects");
            job.JSON = jobJSON.ToJsonString();

            if (_errorContext.Entry(job).State == EntityState.Detached)
                _errorContext.Entry(job).State = EntityState.Modified;

            await _errorContext.SaveChangesAsync();

            var CSVHeaderList = Utilities.FindCSVHeaderRow(csvLines, importFieldConfigList);
            if (!CSVHeaderList.FoundHeader)
                throw new Exception(CSVHeaderList.Message);

            int reqFieldsFound = CSVHeaderList.requiredFields.Count();
            var TDIRequests = new List<TDIRequestDetails>();

            foreach (string row in csvLines)
            {
                rowCount++;
                if (rowCount <= CSVHeaderList.HeaderRowNumber)
                    continue;

                // try split
                if (string.IsNullOrEmpty(row))
                {
                    continue;
                }
                var csvColumns = CSVParserRegex.Split(row);

                if (csvColumns.Length < 3)
                {
                    continue;
                }
                /* check for presence of header row
                if (!headerFound)
                {
                    // is current line the header row?
                    foreach (var col in cols)
                    {
                        var low = CSVCellData.Trim().Replace(@"""","").ToLower();
                        if (fieldList.FirstOrDefault(d => d.SourceFieldname == low) != null)
                        {
                            matches.Add(low);
                            if (requiredFields != null)
                            {
                                reqFieldsFound += requiredFields.FirstOrDefault(d => d == low) == null ? 0 : 1;
                            }
                        }
                        else
                        {
                            matches.Add("");
                        }
                    }
                    // basic validation - if we find 3+ matching column names on this row then assume it is the header
                    // this will tolerate the government mucking about with the export format
                    // and not totally breaking our import immediately although some columns may not come in
                    headerFound = matches.Count(d => !string.IsNullOrEmpty(d)) > 2;
                    if (!headerFound)
                    {
                        matches.Clear();
                        reqFieldsFound = 0;
                    }
                    else if (reqFieldsFound != requiredFields.Count())
                    {
                        var rf = String.Join(',', requiredFields);
                        return NotFound("One or more of these required columns is missing:" + rf);
                    }
                    else
                    {
                        headerRow = rowCount;
                    }
                }
                else if (cols.Length == matches.Count()) 
                */

                // Process record rows that only have same number of columns as the header row (ignore garbage)
                if (csvColumns.Length != CSVHeaderList.matches.Count())
                {
                    recordsSkipped++;
                    log.AppendLine($@"One or more required values was empty. Skipped content to follow:");
                    log.AppendLine(row);
                    continue;
                }

                int ndx = 0;
                int chkSum = 0;
                var json = new StringBuilder("{"); // build a json object we can deserialize into an EF object for insertion

                foreach (var CSVCellData in csvColumns)
                {
                    if (!string.IsNullOrEmpty(CSVHeaderList.matches[ndx]))
                    {
                        string CSVHeader = CSVHeaderList.matches[ndx];
                        var importFieldConfig = importFieldConfigList.First(d => d.SourceFieldname == CSVHeader);
                        json.Append($@"""{importFieldConfig.TargetFieldname}"":");
                        chkSum += (CSVHeaderList.requiredFields.Contains(CSVHeader) && CSVCellData != "") ? 1 : 0;

                        /* Build out a JSON object using the TargetFieldNames in the Import Configuration file
                            * The JSON names correspond to the JSON property name attributes on the TDIRequestDetails class.
                            * This allows deserializing the JSON object directly into an TDIRequestDetails object which will
                            * then be synchronized with, or used to create, and ArbitrationCase record.
                            * The next generation of this approach will skip the intermediary, state-specific table and 
                            * store all Authority import data into a generic AuthorityImportDetails table that uses the
                            * JSON attributes of the ArbitrationCase class.
                            */

                        if (importFieldConfig.IsDate || importFieldConfig.TargetFieldname.EndsWith("Date"))
                        {
                            try
                            {
                                if (CSVCellData == "" || !(CSVCellData.Contains('/') || CSVCellData.Contains('-')))
                                {
                                    json.Append("null,");
                                }
                                else
                                {
                                    var dateValue = Convert.ToDateTime(CSVCellData.Replace("\"", "").Trim()).ToString("yyyy-MM-ddT00:00:00");
                                    json.Append($@"""{dateValue}"",");
                                }
                            }
                            catch
                            {
                                json.Append("null,");
                            }
                        }
                        else if (importFieldConfig.TargetFieldname.EndsWith("Count"))
                        {
                            int countValue = 0;
                            if (CSVCellData != "")
                                int.TryParse(CSVCellData.Replace("\"", "").Trim().Replace(",", ""), out countValue);
                            json.Append($@"{countValue},");  // cnt will be zero if TryParse fails. TODO: Make this nullable instead?
                        }
                        else if (importFieldConfig.IsNumeric || importFieldConfig.TargetFieldname.EndsWith("Amount"))
                        {
                            double amountValue = 0;
                            if (CSVCellData != "")
                                double.TryParse(CSVCellData.Replace("\"", "").Trim().Replace("$", ""), out amountValue);
                            json.Append($@"{amountValue},");
                        }
                        else if (importFieldConfig.IsBoolean)
                        {
                            var booleanValue = CSVCellData.Replace("\"", "").Trim();
                            if (booleanValue == "-1" || booleanValue == "1" || booleanValue.Equals("y", StringComparison.CurrentCultureIgnoreCase)
                                || booleanValue.Equals("yes", StringComparison.CurrentCultureIgnoreCase)
                                || booleanValue.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                                json.Append("true,");
                            else
                                json.Append("false,");
                        }
                        else
                        {
                            json.Append($@"""{CSVCellData.Replace("\"", "").Trim()}"",");
                        }
                    }
                    ndx++;
                }

                json.Length = json.Length - 1;
                json.Append("}");

                if (chkSum != reqFieldsFound)
                {
                    log.AppendLine($@"Error chkSum != reqFieldsFound skipping this record");
                    recordsSkipped++;
                    continue;
                }
                recordsFound++;
                // de-serialize this into an EF object and add to the database
                TDIRequestDetails? TDIRequestDetail = null;
                try
                {
                    TDIRequestDetail = JsonSerializer.Deserialize<TDIRequestDetails>(json.ToString());
                }
                catch
                {
                    recordsSkipped++;
                    log.AppendLine($@"Error deserializing TDIRequestDetails record. Skipped content to follow:");
                    log.AppendLine(json.ToString());
                }
                finally
                {
                    if (TDIRequestDetail != null)
                    {
                        /*** TODO: Add pre-filtering here to weed out records based on things like Payor exclusions and missing. Write it to log and add to recordsSkipped  ***/
                        TDIRequestDetail.BatchUploadDate = batchUploadDate.ToUniversalTime();
                        TDIRequestDetail.SubmittedBy = user.Email;
                        //_context.TDIRequests.Add(TDIRequestDetail);
                        TDIRequests.Add(TDIRequestDetail);
                    }
                    else
                    {
                        recordsSkipped++;
                        log.AppendLine($@"Unexpected NULL record! Skipped content to follow:");
                        log.AppendLine(json.ToString());
                    }
                }
                // reset
                json.Clear();
            }

            // log what happened
            log.AppendLine($@"Read {rowCount} total lines from upload file");
            if (!CSVHeaderList.FoundHeader)
            {
                log.AppendLine("Header row not found! Nothing to do.");
            }
            else
            {
                log.AppendLine($@"Header row found on row {CSVHeaderList.HeaderRowNumber}");
                log.AppendLine($@"Imported {recordsFound} records");
                log.AppendLine($@"Skipped {recordsSkipped} rows / records");
            }


            if (recordsFound > 0)
            {
                // return a status record the client can use for fetching ongoing status
                jobJSON["message"] = $@"Saving {recordsFound} rows into TDIRequests table. This could take a few minutes. Please be patient.";
                job.JSON = jobJSON.ToJsonString();
                if (_errorContext.Entry(job).State == EntityState.Detached)
                    _errorContext.Entry(job).State = EntityState.Modified;

                await _errorContext.SaveChangesAsync();


                /* Set up event handler to catch any failures during db processing
                _context.SaveChangesFailed += (object? sender, SaveChangesFailedEventArgs e) =>
                {
                    log.AppendLine(e.Exception.Message);
                };
                */

                // Save all of the TDI records to the holding table at once - this can be slow
                // TODO: Could we send the _context and Job over to the other thread, return from this method, save all the changes, dispose of the 
                // "worker" context and keep pretty much everything else over in the Sync method the same?
                //await _context.SaveChangesAsync();

                _logger.LogWarning(log.ToString());

                // Detach the Job's context so the new thread can grab the Job and keep updating it
                _errorContext.Entry(job).State = EntityState.Detached;

                var task = Task.Factory.StartNew(() =>
                {
                    _synchronizer.SyncTDIsToCases(authorityRecord!.Id, TDIRequests, job);
                });
            }
            else
            {
                _logger.LogWarning(log.ToString());
                throw new Exception("No valid data found in upload!");
            }
            return log.ToString();
        }
    }
}
