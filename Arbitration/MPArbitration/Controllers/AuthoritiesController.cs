using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MPArbitration.Model;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Net.Http.Headers;
using System.Data.SqlClient;
using System.Text.Json.Nodes;
using System.Data.Common;
using System.Data;
using System.Security.Claims;
using System;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using CsvHelper;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace MPArbitration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthoritiesController : MPBaseController
    {
        private readonly ILogger<AuthoritiesController> _logger;
        private readonly IImportDataSynchronizer _synchronizer;

        public AuthoritiesController(ILogger<AuthoritiesController> logger, ArbitrationDbContext context, IConfiguration configuration, IImportDataSynchronizer synchronizer) : base(context, configuration)
        {
            _logger = logger;
            _synchronizer = synchronizer;
        }


        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Authority>>> GetAll()
        {
            try
            {
                var user = await GetCurrentUser();
                if (user == null)
                    return Unauthorized("No active User context!");

                var authorities = await _context.Authorities
                    .AsSplitQuery()
                    .Include(d => d.Fees)
                    .Include(d => d.TrackingDetails.Where(g => !g.IsDeleted))
                    .Include(d => d.Benchmarks)
                    .ToListAsync();

                return Ok(authorities);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("item/byid/{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<Authority>> GetById(int id, [FromQuery] Boolean stats = false)
        {
            if (id < 1)
                return BadRequest();

            var a = await _context.Authorities
                                    .Include(d => d.Fees)
                                    .Include(d => d.TrackingDetails.Where(g => !g.IsDeleted))
                                    .Include(d => d.Benchmarks)
                                    .Include(d => d.AuthorityGroupExclusions)
                                    .FirstOrDefaultAsync(d => d.Id == id);
            if (a == null)
                return NotFound();

            if (stats && a.IsActive) // IsActive means the Authority has an active arbitration process
                a.Stats = await AddStatsAsync(a);

            return Ok(a);
        }

        [HttpGet]
        [Route("item/bykey/{key}")]
        [Produces("application/json")]
        public async Task<ActionResult<Authority>> GetByKey(string key, [FromQuery] Boolean stats = false)
        {
            if (string.IsNullOrEmpty(key))
                return BadRequest();
            try
            {
                var a = await _context.Authorities
                                        .Include(d => d.Fees)
                                        .Include(d => d.TrackingDetails.Where(g => !g.IsDeleted))
                                        .Include(d => d.Benchmarks)
                                        .Include(d => d.AuthorityGroupExclusions)
                                        .FirstOrDefaultAsync(d => d.Key == key);
                if (a == null)
                    return NotFound();

                if (stats && a.IsActive) // IsActive means the Authority has an active arbitration process
                    a.Stats = await AddStatsAsync(a);

                return Ok(a);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<JsonNode?> AddStatsAsync(Authority a)
        {
            var authStats = new JsonObject();
            // ad-hoc query to gather stats
            string sql = "";
            if (a.Key.Equals("nsa", StringComparison.CurrentCultureIgnoreCase))
            {
                sql = @"SELECT AuthorityStatus, Sum(CountOfStatus) AS CountOfStatus
                                FROM
	                                (SELECT NSAStatus as AuthorityStatus, Count(*) AS CountOfStatus
	                                FROM dbo.ArbitrationCases
	                                WHERE IsDeleted=0 
	                                GROUP BY NSAStatus

	                                UNION ALL

	                                SELECT c.AuthorityStatus AS AuthorityStatus, Count(*) AS CountOfStatus
	                                FROM CaseArchives c
	                                INNER JOIN Authorities a ON c.authorityid = a.id
	                                WHERE a.[Key]=@authority
	                                GROUP BY c.AuthorityStatus
	                                ) as tbl
                                GROUP BY AuthorityStatus";
            }
            else
            {
                sql = @"SELECT AuthorityStatus, Sum(CountOfStatus) AS CountOfStatus
                                FROM
	                                (SELECT AuthorityStatus, Count(*) AS CountOfStatus
	                                FROM dbo.ArbitrationCases
	                                WHERE IsDeleted=0 AND AuthorityCaseId<>'' AND Authority = @authority
	                                GROUP BY AuthorityStatus

	                                UNION ALL

	                                SELECT c.AuthorityStatus AS AuthorityStatus, Count(*) AS CountOfStatus
	                                FROM CaseArchives c
	                                INNER JOIN Authorities a ON c.authorityid = a.id
	                                WHERE a.[Key]=@authority
	                                GROUP BY c.AuthorityStatus
	                                ) as tbl
                                GROUP BY AuthorityStatus";
            }


            using (var conn = new SqlConnection(_context.Database.GetConnectionString()))
            using (var cmd = conn.CreateCommand())
            {
                await conn.OpenAsync();
                cmd.CommandText = sql;
                cmd.CommandType = System.Data.CommandType.Text;
                var prm = cmd.CreateParameter();
                prm.ParameterName = "@authority";
                prm.Value = a.Key;
                cmd.Parameters.Add(prm);

                var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    authStats.Add(rdr.GetString(0), rdr.GetInt32(1));
                }
            }

            return authStats.Count() > 0 ? authStats : null;
        }

        // POST api/authorities/5 - Add an Authority
        [HttpPost]
        [Route("item")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Authority>> AddAuthority([FromBody] Authority value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Managers and Administrators can create a new Authority");

            if (value == null || value.Id != 0 || string.IsNullOrEmpty(value.Key) || string.IsNullOrEmpty(value.Name))
                return BadRequest("Validation failed");

            var g = await _context.Authorities.FirstOrDefaultAsync(d => d.Name == value.Name || d.Key == value.Key);
            if (g != null)
                return BadRequest("Authority already exists");

            var n = new Authority()
            {
                Id = 0,
                UpdatedBy = user.Email,
                UpdatedOn = Utilities.GetCurrentUtcDate(),
                IsActive = value.IsActive,
                Name = value.Name,
                //Benchmark = BenchmarkSource.FairHealth, // remove this as soon as new multi-benchmark features are added
                Key = value.Key,
                Website = value.Website
            };

            _context.Authorities.Add(n);
            await _context.SaveChangesAsync();

            return Ok(n);
        }

        // PUT api/authorities/item/byid/5 - Update an Authority
        [HttpPut]
        [Route("item/byid/{id}")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Authority>> UpdateAuthority(int id, [FromBody] Authority value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Managers and Administrators can update Authority configuration");

            if (value == null || value.Id <= 0 || id <= 0 || value.Id != id || string.IsNullOrEmpty(value.Key) || string.IsNullOrEmpty(value.Name))
                return BadRequest("Validation failed");

            var g = await _context.Authorities.FindAsync(id);
            if (g == null)
                return BadRequest("Unable to locate the Authority record");

            var h = await _context.Authorities.FirstOrDefaultAsync(d => d.Id != id && (d.Key == value.Key || d.Name == value.Name));
            if (h != null)
                return BadRequest("Update would create a record with a duplicate key or name!");

            _context.Entry(g).CurrentValues.SetValues(value);
            await _context.SaveChangesAsync();

            return Ok(g);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key">Authority lookup key</param>
        /// <param name="aa">ActiveOnly. Only update records in an "active" state. Default is True.</param>
        /// <returns></returns>
        [HttpGet("item/bykey/{key}/recalc")]
        public async Task<ActionResult<JobQueueItem>> RecalculateAuthorityTrackingAsync(string key, [FromQuery] bool aa = true)
        {
            string keyLow = key.ToLower();

            //if (keyLow.Equals("tx"))
            //    return BadRequest("TX is not a valid option");

            var user = await GetCurrentUser();
            if (user == null)
                return Problem("No active User context!");

            if (!user.IsAdmin && !user.IsSystem)
                return Unauthorized("Method reserved for administrators");

            var authority = await _context.Authorities.Include(d => d.TrackingDetails.Where(g => !g.IsDeleted)).FirstOrDefaultAsync(a => a.Key == key);
            if (authority == null)
                return NotFound();

            string message = keyLow == "nsa" ? "Recalculating NSA dates and deadlines" : $@"Recalculating dates and deadlines for {authority.Name}";
            if (!aa && keyLow == "nsa")
                message += " (Pending and Submitted Negotiation Request only)";

            var serverName = this.Request.HttpContext.GetServerVariable("INSTANCE_ID");
            var job = new JobQueueItem { Id = 0, JSON = "", UpdatedBy = user.Email, UpdatedOn = Utilities.GetCurrentUtcDate() };
            var jobJSON = new JsonObject();
            jobJSON.Add("jobType", "recalculate|" + keyLow);
            jobJSON.Add("recordsProcessed", 0);
            jobJSON.Add("totalRecords", 0);
            jobJSON.Add("status", "initializing");
            jobJSON.Add("startTime", Utilities.GetCurrentUtcDate());
            jobJSON.Add("lastUpdated", Utilities.GetCurrentUtcDate());
            jobJSON.Add("serverName", serverName);
            jobJSON.Add("message", message);
            job.JSON = jobJSON.ToJsonString();

            _context.JobQueueItems.Add(job);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            int jobId = job.Id;
            _context.Entry(job).State = EntityState.Detached;

            var contextOptions = new DbContextOptionsBuilder<ArbitrationDbContext>()
                .UseSqlServer(_configuration.GetSection("ConnectionStrings").GetSection("ConnStr").Value)
                .Options;

            if (contextOptions == null)
                return BadRequest("Unable to connect to storage");

            var task = Task.Factory.StartNew(() =>
            {
                _synchronizer.RecalculateAuthorityDates(contextOptions, jobId, user, authority, aa);
            });

            return Ok(job);
        }

        #region Fee endpoints

        // POST api/authorities/fees/5 - Add an AuthorityFee
        [HttpPost]
        [Route("fees")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthorityFee>> AddAuthorityFeeAsync([FromBody] AuthorityFee value)
        {
            try
            {
                var user = await GetCurrentUser();
                if (user == null)
                    return Unauthorized("No active User context!");

                if (!user.IsAdmin && !user.IsManager)
                    return Unauthorized("Only Managers and Administrators can add new authority fees");

                if (value == null || value.AuthorityId < 1 || value.Id != 0 || string.IsNullOrEmpty(value.FeeName))
                    return BadRequest("Validation failed");

                if (!string.IsNullOrEmpty(value.ReferenceColumnName))
                {
                    // verify that the column value matches an actual dispute column
                    var t = Type.GetType("MPArbitration.Model.AuthorityDispute");
                    var targetProps = t!.GetProperties().Where(d => d.GetIndexParameters().Length == 0 && d.Name == value.ReferenceColumnName);
                    if (targetProps.Count() == 0)
                        return BadRequest("ReferenceColumnName value does not match a known AuthorityDispute property.");
                }

                var g = await _context.Authorities.Include(d => d.Fees).FirstOrDefaultAsync(d => d.Id == value.AuthorityId);
                if (g == null)
                    return BadRequest("Authority not found");

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

        // PUT api/authorities/fees/5 - Update an AuthorityFee
        [HttpPut]
        [Route("fees")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthorityFee>> UpdateAuthorityFeeAsync([FromBody] AuthorityFee value)
        {
            try
            {
                var user = await GetCurrentUser();
                if (user == null)
                    return Unauthorized("No active User context!");

                if (!user.IsAdmin && !user.IsManager)
                    return Unauthorized("Only Managers and Administrators can update Authority Fee configuration");

                if (value.Id < 1)
                    return BadRequest("Invalid identifier");

                if (value.AuthorityId < 1)
                    return BadRequest("Invalid Authority identifier");

                if (value.DueDaysAfterColumnName < 0)
                    return BadRequest("Due Days cannot be negative");

                var fee = await _context.AuthorityFees.FindAsync(value.Id);
                if (fee == null)
                    return NotFound();

                // validation
                if (value.AuthorityId != fee.AuthorityId)
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
                fee.UpdatedOn = Utilities.GetCurrentUtcDate();
                fee.UpdatedBy = user.Email;

                await _context.SaveChangesAsync();

                return Ok(fee);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion

        #region Benchmark endpoints

        [HttpDelete]
        [Route("item/byid/{id}/benchmarks/{authorityBenchmarkId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> RemoveBenchmark(int id, int authorityBenchmarkId)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Managers and Administrators can delete a Benchmark");

            if (id < 1 || authorityBenchmarkId < 1)
                return BadRequest("Benchmark not found");

            var g = await _context.Authorities.Include(d => d.Benchmarks).FirstOrDefaultAsync(d => d.Id == id);
            if (g == null)
                return BadRequest("Authority not found");

            var t = g.Benchmarks.FirstOrDefault(d => d.Id == authorityBenchmarkId);
            if (t == null)
                return BadRequest("Authority-Benchmark relationship is missing or invalid");

            try
            {
                g.Benchmarks.Remove(t);
                _context.Entry(t).State = EntityState.Deleted;
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST api/authorities/5 - Add an AuthorityBenchmarkDetails record to authority 5
        [HttpPost]
        [Route("item/byid/{id}/benchmarks")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Authority>> AddBenchmark(int id, [FromBody] AuthorityBenchmarkDetails value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Managers and Administrators can add Benchmarks to an Authority");

            if (id <= 0
                || value == null
                || value.BenchmarkDatasetId < 1
                || value.Id != 0
                || string.IsNullOrEmpty(value.PayorAllowedField)
                || string.IsNullOrEmpty(value.ProviderChargesField))
                return BadRequest("Validation failed");

            var g = await _context.Authorities.Include(d => d.Benchmarks).FirstOrDefaultAsync(d => d.Id == id);
            if (g == null)
                return BadRequest("Authority not found");

            var t = g.Benchmarks.FirstOrDefault(d => d.BenchmarkDatasetId == value.BenchmarkDatasetId && d.Service == value.Service);
            if (t != null)
                return BadRequest("Benchmark already added to Authority");

            var n = new AuthorityBenchmarkDetails()
            {
                Id = 0,
                IsDefault = value.IsDefault,
                AdditionalAllowedFields = value.AdditionalAllowedFields,
                AdditionalChargesFields = value.AdditionalChargesFields,
                BenchmarkDatasetId = value.BenchmarkDatasetId,
                PayorAllowedField = value.PayorAllowedField,
                ProviderChargesField = value.ProviderChargesField,
                Service = value.Service,
                UpdatedBy = user.Email,
                UpdatedOn = Utilities.GetCurrentUtcDate(),
            };
            try
            {
                g.Benchmarks.Add(n);
                await _context.SaveChangesAsync();
                return Ok(n);
            }
            catch (Exception ex)
            {
                return BadRequest("Save failed. " + ex.Message);
            }
        }

        // PUT api/authorities/5/benchmarks - Update an AuthorityBenchmarkDetails record
        [HttpPut]
        [Route("item/byid/{id}/benchmarks")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Authority>> UpdateBenchmark(int id, [FromBody] AuthorityBenchmarkDetails value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Managers and Administrators can update Benchmarks");

            if (id < 1
                || value == null
                || value.BenchmarkDatasetId < 1
                || value.Id < 1
                || value.AuthorityId != id
                || string.IsNullOrEmpty(value.PayorAllowedField)
                || string.IsNullOrEmpty(value.ProviderChargesField)
                || string.IsNullOrEmpty(value.Service))
                return BadRequest("Validation failed");

            var g = await _context.Authorities.Include(d => d.Benchmarks).FirstOrDefaultAsync(d => d.Id == id);
            if (g == null)
                return BadRequest("Authority not found");

            var t = g.Benchmarks.FirstOrDefault(d => d.AuthorityId == id && d.Id == value.Id);
            if (t == null)
                return BadRequest("Referenced Benchmark is not assigned to referenced Authority");

            try
            {
                value.UpdatedBy = user.Email;
                value.UpdatedOn = Utilities.GetCurrentUtcDate();
                _context.Entry(t).CurrentValues.SetValues(value);
                await _context.SaveChangesAsync();
                return Ok(t);
            }
            catch (Exception ex)
            {
                return BadRequest("Save failed. " + ex.Message);
            }
        }

        #endregion

        #region Tracking endpoints

        // POST api/authorities/5 - Add a tracking detail record
        [HttpPost]
        [Route("item/byid/{id}/tracking")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Authority>> AddTracking(int id, [FromBody] AuthorityTrackingDetail value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Managers and Administrators can add new Tracking details");

            if (id <= 0 || value == null || value.AuthorityId != id || value.Id != 0 || string.IsNullOrEmpty(value.TrackingFieldName) || string.IsNullOrEmpty(value.TrackingLabel) || string.IsNullOrEmpty(value.TrackingFieldType) || value.TrackingFieldName == value.ReferenceFieldName)
                return BadRequest("Validation failed");

            var g = await _context.Authorities.Include(d => d.TrackingDetails.Where(g => !g.IsDeleted)).FirstOrDefaultAsync(d => d.Id == id);
            if (g == null)
                return BadRequest("Authority not found");

            var t = g.TrackingDetails.FirstOrDefault(d => d.TrackingFieldName == value.TrackingFieldName);
            if (t != null)
                return BadRequest("Tracking Field already added to Authority");

            var n = new AuthorityTrackingDetail()
            {
                Id = 0,
                DisplayColumn = value.DisplayColumn,
                HelpText = value.HelpText,
                MapToCaseField = value.MapToCaseField,
                Order = value.Order,
                ReferenceFieldName = value.ReferenceFieldName,
                TrackingFieldName = value.TrackingFieldName,
                TrackingLabel = value.TrackingLabel,
                TrackingFieldType = value.TrackingFieldType,
                UnitsFromReference = value.UnitsFromReference,
                UnitsType = value.UnitsType,
                UnlockForStatuses = value.UnlockForStatuses,
                UpdatedBy = user.Email,
                UpdatedOn = Utilities.GetCurrentUtcDate(),
            };

            try
            {
                g.TrackingDetails.Add(n);
                await _context.SaveChangesAsync();
                return Ok(n);
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

        // PUT api/authorities/5 - Update a tracking detail record
        [HttpPut]
        [Route("item/byid/{id}/tracking")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Authority>> UpdateTracking(int id, [FromBody] AuthorityTrackingDetail value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Managers and Administrators can make changes to Authority Tracking data");

            if (id <= 0 || value == null || value.AuthorityId != id || value.Id <= 0 || string.IsNullOrEmpty(value.TrackingFieldName) || string.IsNullOrEmpty(value.TrackingLabel) || string.IsNullOrEmpty(value.TrackingFieldType) || value.TrackingFieldName == value.ReferenceFieldName)
                return BadRequest("Validation failed");

            var g = await _context.Authorities.Include(d => d.TrackingDetails.Where(g => !g.IsDeleted)).FirstOrDefaultAsync(d => d.Id == id);
            if (g == null)
                return BadRequest("Authority not found");

            var t = g.TrackingDetails.FirstOrDefault(d => d.Id != value.Id && d.TrackingFieldName == value.TrackingFieldName);
            if (t != null)
                return BadRequest("This operation would create duplicate records.");

            t = g.TrackingDetails.FirstOrDefault(d => d.Id == value.Id);
            if (t == null)
                return BadRequest("TrackingDetail not found");

            value.UpdatedBy = user.Email;
            value.UpdatedOn = Utilities.GetCurrentUtcDate();
            _context.Entry(t).CurrentValues.SetValues(value);
            await _context.SaveChangesAsync();

            return Ok(t);
        }

        /// <summary>
        /// Does a soft delete so we can see who screwed the pooch.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ndx"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("item/byid/{id}/tracking/{ndx}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteTrackingAsync(int id, int ndx)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Only Managers and Administrators can delete Authority Tracking data");

            if (id < 1 || ndx < 1)
                return BadRequest("Validation failed");

            var g = await _context.Authorities.Include(d => d.TrackingDetails.Where(g => !g.IsDeleted && g.Id == ndx)).FirstOrDefaultAsync(d => d.Id == id);
            if (g == null)
                return BadRequest("Authority not found");

            if (g.TrackingDetails.Count != 1)
                return BadRequest("Tracking entry not found");

            try
            {
                AuthorityTrackingDetail t = g.TrackingDetails.First();

                t.IsDeleted = true;
                t.UpdatedBy = user.Email;
                t.UpdatedOn = Utilities.GetCurrentUtcDate();

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch
            {
                return BadRequest("Unable to delete the tracking detail record");
            }
        }
        #endregion

        #region File handling endpoints

        /// <summary>
        /// Get all attachments / files for an Authority
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("files/{id}")]
        public async Task<ActionResult<IEnumerable<CaseFile>>> GetFilesAsync(int id, [FromQuery] string? docType = null)
        {
            if (id < 1)
                return BadRequest("Invalid parameter");
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");
            var orig = await _context.Authorities.FindAsync(id);
            if (orig == null)
                return NotFound("Authority not found");
            if (docType == null)
                docType = "";

            try
            {
                var cf = await Utilities.GetBlobLinksAsync(_containerClient, "AuthorityId", id, docType.ToLower());
                return Ok(cf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);
                _logger.LogError($@"Error retrieving files for Authority {id}");
#if DEBUG
                _logger.LogError(ex.Message);
#endif
                return BadRequest($@"Error retrieving files for Authority {id}");
            }
        }

        /// <summary>
        /// Get all attachments / files for an Authority using the Authority key 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("item/bykey/{key}/files")]
        public async Task<ActionResult<IEnumerable<CaseFile>>> GetFilesByKeyAsync(string key, [FromQuery] string? docType = null)
        {
            if (string.IsNullOrEmpty(key))
                return BadRequest("Invalid parameter");

            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            var orig = await _context.Authorities.FirstOrDefaultAsync(d => d.Key == key);

            if (orig == null)
                return NotFound("Authority not found");

            if (docType == null)
                docType = "";

            try
            {
                var cf = await Utilities.GetBlobLinksAsync(_containerClient, "AuthorityId", orig.Id, docType.ToLower());
                return Ok(cf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);
                _logger.LogError($@"Error retrieving files for Authority {key}");
#if DEBUG
                _logger.LogError(ex.Message);
#endif
                return BadRequest($@"Error retrieving files for Authority {key}");
            }
        }

        [HttpPost]
        [Route("blob")]
        public async Task<ActionResult<string>> AttachFileAsync([FromQuery] int id, [FromQuery] string cdt, [FromForm] IFormFile file)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Insufficient privileges to attach Files to an Authority");

            if (id < 1)
                return NotFound("ID not found");

            if (string.IsNullOrEmpty(cdt))
                return BadRequest("Missing Document Type");

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

            var authority = await _context.Authorities.FindAsync(id);
            if (authority == null)
                return NotFound("Authority not found");

            var uploadedOn = Utilities.GetCurrentUtcDate();
            var uploadedBy = GetUsername();
            var log = new StringBuilder($@"{uploadedOn:G}: Storing auxillary file for Authority {id} ({authority.Key}).");
            string blobURL = "";

            try
            {

                using (var reader = file.OpenReadStream())
                {
                    // nsarequestattachment-auth_tx-7-The filename.pdf
                    string blobName = $@"{low}-auth_{authority.Key.ToLower()}-{id}-{file.FileName.ToLower()}";
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
                        tags.Add("Authority", authority.Key);
                        tags.Add("AuthorityId", id.ToString());
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
        public async Task<IActionResult> ViewFileAsync([FromQuery] int id, [FromQuery] string name, [FromQuery] string key = "")
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No AppUser context!");
            if (!user.IsActive)
                return Unauthorized("No active AppUser context!");

            if (string.IsNullOrEmpty(name))
                return BadRequest("Empty document name");

            var orig = !string.IsNullOrEmpty(key) ? await _context.Authorities.FirstOrDefaultAsync(d => d.Key == key) : await _context.Authorities.FindAsync(id);
            if (orig == null)
                return NotFound("Authority not found");

            var n = name.ToLower();
            if (!name.Contains($@"-auth_{orig.Key.ToLower()}-") || !(n.EndsWith(".pdf") || n.EndsWith(".tif") || n.EndsWith(".tiff")))
                return BadRequest("Invalid document name");


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

        [HttpDelete]
        [Route("blob")]
        public async Task<IActionResult> DeleteFileAsync([FromQuery] int id, [FromQuery] string cdt, [FromQuery] string name)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin)
                return Unauthorized("Insufficient privileges to remove Authority attachments.");

            if (string.IsNullOrEmpty(name))
                return BadRequest("Empty document name");

            // verify record
            var orig = await _context.Authorities.FindAsync(id);
            if (orig == null)
                return NotFound("Authority record not found");

            var n = name.ToLower();
            var low = cdt.ToLower();

            if (!name.StartsWith($@"{low}-auth_{orig.Key.ToLower()}-{id}") || !(n.EndsWith(".pdf") || n.EndsWith(".tif") || n.EndsWith(".tiff")))
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
                _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);
                _logger.LogError($@"Error deleting {name} from BLOB storage");
#if DEBUG
                _logger.LogError(ex.Message);
#endif
                return BadRequest($@"Error deleting {name} from storage");
            }

        }
        #endregion

        #region Payor Group Exceptions

        [HttpPost("item/byid/{id}/pge")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<ActionResult<AuthorityPayorGroupExclusion>> AddPayorGroupExclusionAsync(int id, [FromBody] AuthorityPayorGroupExclusion group)
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");
            if (!u.IsAdmin && !u.IsManager)
                return Unauthorized("Only global Admins and Managers can add new Payor Group Exceptions.");

            if (id < 1 || id != group.AuthorityId)
                return BadRequest("Invalid Authority Id");

            if (group.Id != 0)
                return BadRequest("Object is not new!");

            if (group.PayorId < 1)
                return BadRequest("Invalid PayorId");

            if (string.IsNullOrEmpty(group.GroupNumber))
                return BadRequest("GroupNumber required");


            try
            {
                var auth = await _context.Authorities.Include(d => d.AuthorityGroupExclusions.Where(b => b.GroupNumber == group.GroupNumber)).FirstOrDefaultAsync(d => d.Id == id);

                if (auth == null)
                    return BadRequest("Authority not found");
                if (auth.AuthorityGroupExclusions.Count() != 0)
                    return BadRequest("This would create a duplicate AuthorityGroupException. Did you mean to edit?");

                var p = await _context.Payors.Include(d => d.PayorGroups.Where(g => g.GroupNumber == group.GroupNumber)).FirstOrDefaultAsync(d => d.Id == group.PayorId);
                if (p == null)
                    return BadRequest("Payor not found");

                if (p.PayorGroups.Count() == 0)
                    return BadRequest("Invalid Payor GroupNumber");

                group.UpdatedOn = Utilities.GetCurrentUtcDate();
                group.UpdatedBy = u.Email;

                await _context.AuthorityPayorGroupExclusions.AddAsync(group);
                await _context.SaveChangesAsync();

                return Ok(group);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("pge/import")]
        public async Task<ActionResult<PayorGroupResponse>> ImportPayorGroupExclusionsAsync([FromForm] IFormFile file)
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

                IEnumerable<PayorGroupExclusionImport>? records = null;

                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, config))
                {
                    records = csv.GetRecords<PayorGroupExclusionImport>().ToList();
                }

                // add groups one by one if they don't already exist
                int exclusionsAdded = 0;
                int exclusionsUpdated = 0;
                var payorsSkipped = new List<string>();
                var uploadDate = Utilities.GetCurrentUtcDate();
                var log = new StringBuilder($@"{uploadDate.ToShortTimeString()} Successfully parsed {records.Count()} records.");
                log.AppendLine();
                log.AppendLine();

                var authorities = records.Select(d => d.Authority).Distinct();
                foreach (var auth in authorities)
                {
                    var authority = await _context.Authorities.Include(g => g.AuthorityGroupExclusions).FirstOrDefaultAsync(d => d.Key == auth);
                    if (authority == null)
                    {
                        log.AppendLine($@"* Unknown authority {auth}. Skipping records!");
                        continue;
                    }

                    log.AppendLine($@"* Processing exclusions for {authority.Name}");
                    var newExclusions = records.Where(d => d.Authority == auth).ToList();
                    var payorNames = newExclusions.Select(d => d.PayorName).Distinct();

                    foreach (var name in payorNames)
                    {
                        var payor = await Utilities.GetPayorByAliasAsync(_context, name);
                        if (payor != null)
                        {
                            log.AppendLine($@"++ Found prime payor {payor.Name} using name {name}.");

                            foreach (var g in newExclusions.Where(n => n.PayorName == name).ToArray())
                            {
                                // either update existing exclusion or add new
                                var group = authority.AuthorityGroupExclusions.FirstOrDefault(d => d.PayorId == payor.Id && d.GroupNumber.Equals(g.GroupNumber, StringComparison.CurrentCultureIgnoreCase));
                                if (group == null)
                                {
                                    exclusionsAdded++;
                                    authority.AuthorityGroupExclusions.Add(new AuthorityPayorGroupExclusion { AuthorityId = authority.Id, GroupNumber = g.GroupNumber, IsNSAIneligible = g.IsNSAIneligible, IsStateIneligible = g.IsStateIneligible, PayorId = payor.Id, UpdatedBy = u.Email, UpdatedOn = uploadDate });
                                }
                                else
                                {
                                    group.IsNSAIneligible = g.IsNSAIneligible;
                                    group.IsStateIneligible = g.IsStateIneligible;
                                    group.UpdatedOn = uploadDate;
                                    group.UpdatedBy = u.Email;
                                    exclusionsUpdated++;
                                }

                                if ((exclusionsAdded + exclusionsUpdated) % 10 == 0)
                                    await _context.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            payorsSkipped.Add(name);
                            log.AppendLine($@"?? Unable to locate a Payor or alias with the name {name}. Records skipped.");
                        }
                        log.AppendLine();
                    }
                }

                if (_context.ChangeTracker.HasChanges())
                    await _context.SaveChangesAsync();

                string sm = payorsSkipped.Count > 0 ? String.Join(',', payorsSkipped.ToArray()) : "none";
                log.AppendLine();
                log.AppendLine($@"Exclusions Added: {exclusionsAdded}, Exclusions Updated: {exclusionsUpdated}, Payors Skipped (not found): {sm}");
                return Ok(new PayorGroupResponse { Message = log.ToString(), ItemsAdded = exclusionsAdded, ItemsSkipped = 0, ItemsUpdated = exclusionsUpdated, PayorsSkipped = payorsSkipped.ToArray() });
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

        [HttpDelete]
        [Route("item/byid/{id}/pge/{gid}")]
        public async Task<IActionResult> DeleteAuthorityPayorGroupExclusionAsync(int id, int gid)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin && !user.IsManager)
                return Unauthorized("Insufficient privileges to remove Authority Payor Group Exclusions.");

            // verify record
            var orig = await _context.Authorities.Include(d => d.AuthorityGroupExclusions.Where(g => g.Id == gid)).FirstOrDefaultAsync(d => d.Id == id);
            if (orig == null)
                return NotFound("Authority record not found");

            if (orig.AuthorityGroupExclusions.Count() == 0)
                return NotFound("Exclusion record not found");
            try
            {
                _context.Set<AuthorityPayorGroupExclusion>().Remove(orig.AuthorityGroupExclusions.First());
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion
    }
}
