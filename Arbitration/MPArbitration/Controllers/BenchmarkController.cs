using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MPArbitration.Model;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace MPArbitration.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BenchmarkController : MPBaseController
    {
        private readonly ILogger<BenchmarkController> _logger;
        private readonly IImportDataSynchronizer _synchronizer;
        private readonly IMemoryCache _memoryCache;

        public BenchmarkController(ILogger<BenchmarkController> logger, ArbitrationDbContext context, IImportDataSynchronizer synchronizer, IMemoryCache cache, IConfiguration configuration) : base(context, configuration)
        {
            _logger = logger;
            _synchronizer = synchronizer;
            _memoryCache = cache;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="p">Procedure Code</param>
        /// <param name="g">GeoZip</param>
        /// <param name="m26">Modifier 26</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<BenchmarkDataItem?>> Get([FromQuery] int ds, [FromQuery] string p, [FromQuery] string g, [FromQuery] Boolean m26 = false)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (string.IsNullOrEmpty(p) || string.IsNullOrEmpty(g) || ds < 1)
                return NotFound();

            var query = _context.BenchmarkDataItems.Where(d => d.BenchmarkDatasetId == ds
                                                         && d.ProcedureCode == p
                                                         && d.GeoZip == g);

            try
            {
                // get the modified and unmodified benchmarks if they exist so we can fall back if necessary
                var items = await query.ToListAsync();
                if (items == null || items.Count() == 0)
                    return Ok(null);

                // try to use m26 as an additional filter if specified
                BenchmarkDataItem? result = m26 ? items.FirstOrDefault(d => d.Modifiers.Contains("26")) : items.FirstOrDefault(d => string.IsNullOrEmpty(d.Modifiers));
                if (result != null)
                    return Ok(result);

                return Ok(items.FirstOrDefault());  // fall back to any procedure code match if the exact one isn't available - this was requested by the business
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet]
        [Route("source")]
        public async Task<ActionResult<IEnumerable<BenchmarkDataset>>> GetAll()
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            try
            {
                return Ok(await _context.BenchmarkDatasets.ToArrayAsync());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("source/{id}")]
        public async Task<ActionResult<IEnumerable<BenchmarkDataset>>> GetById(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (id < 1)
                return NotFound();

            try
            {
                var s = await _context.BenchmarkDatasets.FindAsync(id);
                if (s == null)
                    return NotFound();
                return Ok(s);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("source/{id}/count")]
        public async Task<ActionResult<int>> GetCountById(int id)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (id < 1)
                return NotFound();

            try
            {
                var s = await _context.BenchmarkDatasets.FindAsync(id);
                if (s == null)
                    return NotFound();
                var count = await _context.BenchmarkDataItems.CountAsync(d => d.BenchmarkDatasetId == s.Id);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST api/benchmark/5 - Add a BenchmarkDatset
        [HttpPost]
        [Route("source")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BenchmarkDataset>> Post([FromBody] BenchmarkDataset value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin)
                return Unauthorized("Only administrators can manage Benchmarks");


            if (value == null || value.Id != 0 || value.DataYear < 2000 || value.DataYear > 2067 || string.IsNullOrEmpty(value.Key) || string.IsNullOrEmpty(value.Name) || string.IsNullOrEmpty(value.Vendor))
                return BadRequest("Validation failed");

            var g = await _context.BenchmarkDatasets.FirstOrDefaultAsync(d => d.Key == value.Key);
            if (g != null)
                return BadRequest("Benchmark Dataset already exists");

            var n = new BenchmarkDataset()
            {
                Id = 0,
                UpdatedBy = user.Email,
                UpdatedOn = Utilities.GetCurrentUtcDate(),
                IsActive = value.IsActive,
                Name = value.Name,
                DataYear = value.DataYear,
                Key = value.Key,
                ValueFields = value.ValueFields,
                Vendor = value.Vendor
            };

            try
            {
                _context.BenchmarkDatasets.Add(n);
                await _context.SaveChangesAsync();
                return Ok(n);
            }
            catch (Exception ex)
            {
                return BadRequest("Create failed. " + ex.Message);
            }
        }

        // PUT api/benchmark/5 - Update a BenchmarkDataset
        [HttpPut]
        [Route("source/{id}")]
        [Produces("application/json")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BenchmarkDataset>> Put(int id, [FromBody] BenchmarkDataset value)
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsAdmin)
                return Unauthorized("Only administrators can manage Benchmarks");

            if (value == null || value.Id <= 0 || id <= 0 || value.DataYear < 2000 || value.DataYear > 2067 || string.IsNullOrEmpty(value.Key) || string.IsNullOrEmpty(value.Name) || string.IsNullOrEmpty(value.Vendor) || string.IsNullOrEmpty(value.ValueFields))
                return BadRequest("Validation failed");

            var g = await _context.BenchmarkDatasets.FindAsync(id);
            if (g == null)
                return BadRequest("Unable to locate the Benchmark Dataset record");

            var h = await _context.BenchmarkDatasets.FirstOrDefaultAsync(d => d.Id != id && d.Key == value.Key);
            if (h != null)
                return BadRequest("Update would create duplicate records!");

            try
            {
                value.UpdatedBy = user.Email;
                value.UpdatedOn = Utilities.GetCurrentUtcDate();
                _context.Entry(g).CurrentValues.SetValues(value);

                await _context.SaveChangesAsync();

                return Ok(g);
            }
            catch (Exception ex)
            {
                return BadRequest("Update failed. " + ex.Message);
            }
        }

        // POST api/benchmark/5/import - Add BenchmarkDatsetItems
        [HttpPost]
        [RequestSizeLimit(100 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
        [Route("import/{id}")]
        public async Task<ActionResult<string>> ImportBenchmarkData([FromForm] IFormFile file, int id, [FromForm] string key)
        {
            try
            {
                var u = await GetCurrentUser();
                if (u == null)
                    return Unauthorized("No active User context!");

                if (!u.IsAdmin)
                    return Unauthorized("Only administrators can manage Benchmarks");

                // Make sure we aren't already processing an upload so we can avoid unforeseen performance or SQL locking issues
                var cachekey = "benchmarkUploadInProgress";
                _memoryCache.TryGetValue(cachekey, out bool uploadInProgress);
                if (uploadInProgress)
                {
                    return BadRequest("There is already an upload in the queue. Wait 2 minutes and try your upload again.");
                }

                var benchmarkId = Convert.ToInt32(id);
                if (benchmarkId < 1 || string.IsNullOrEmpty(key))
                    return BadRequest("Validation failed");

                var benchmark = await _context.BenchmarkDatasets.FindAsync(benchmarkId);
                if (benchmark == null)
                    return BadRequest("Benchmark Dataset not found.");
                if (benchmark.Key != key)
                    return BadRequest("Benchmark Dataset does not match given key");

                //var count = await _context.BenchmarkDataItems.CountAsync(d => d.BenchmarkDatasetId == benchmarkId);
                //if(count > 0)
                //    return BadRequest("The dataset already contains data. Please clear out the existing data before uploading new bencmarks.");

                var batchUploadDate = Utilities.GetCurrentUtcDate();

                if (file == null)
                    return BadRequest("No file detected!");
                else if (!file.FileName.ToLower().EndsWith(".csv"))
                    return BadRequest("Only CSV files allowed");

                /* this is an example of the the upload expects:
                 * 
                 * string raw = @"{""BenchmarkDataItems"":[
                                { ""geoZip"":""796"",""modifiers"":"""",""procedureCode"":""95812""},
                                { ""benchmarks"":{ ""FH50thPercentileCharges"":873.620000000,""FH80thPercentileCharges"":2671.110000000},""geoZip"":""796"",""modifiers"":"""",""procedureCode"":""24140""}
                                ]}";
                */

                var upload = new List<string>();

                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    while (reader.Peek() >= 0)
                    {
                        var row = await reader.ReadLineAsync();
                        if (!string.IsNullOrEmpty(row))
                        {
                            upload.Add(row);
                        }
                    }
                }

                if (upload.Count() == 0)
                    return BadRequest("Upload file appears to be empty!");

                var task = Task.Factory.StartNew(() =>
                {
                    _synchronizer.ImportBenchmarks(benchmark.Id, u.Email, String.Join("", upload), null);
                });

                while (task.Status != TaskStatus.Running && task.Status != TaskStatus.RanToCompletion && task.Status != TaskStatus.Canceled && task.Status != TaskStatus.Faulted)
                    task.Wait(500);

                return Ok($@"Benchmark upload queued for processing. Task is {task.Status}");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }
    }
}
