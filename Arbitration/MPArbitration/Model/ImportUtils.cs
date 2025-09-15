using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MPArbitration.Model
{
    public static class ImportUtils
    {

        /// <summary>
        /// If an unresolved MasteDataExcption matching the parameters is not found, this
        /// method adds a new object to the EF context and persists it.
        /// </summary>
        /// <param name="mdeType"></param>
        /// <param name="data"></param>
        /// <param name="message"></param>
        /// <param name="user"></param>
        /// <returns>Returns the newly added exception or the existing unresolved one.</returns>
        public static async Task<MasterDataException?> AddMasterDataException(ILogger<ImportDataSynchronizer> _logger, MasterDataExceptionType mdeType, string data, string message, ArbitrationDbContext Context)
        {
            // only add when one is not already in the table
            try
            {
                _logger.LogInformation(message);
                var md = await Context.MasterDataExceptions.FirstOrDefaultAsync(d => d.ExceptionType == mdeType && d.Data == data && !d.IsResolved);

                if (md == null)
                {
                    md = new MasterDataException
                    {
                        CreatedOn = Utilities.GetCurrentUtcDate(),
                        Data = data,
                        Message = message,
                        ExceptionType = mdeType,
                        Id = 0,
                        IsResolved = false
                    };
                    Context.MasterDataExceptions.Add(md);
                    await Context.SaveChangesAsync();
                }
                return md;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return null;
            }
        }

        public static async Task UpdateJob(ArbitrationDbContext _errorContext, JobQueueItem? job, string message, string status)
        {
            if (job != null)
            {
                try
                {
                    var json = JsonObject.Parse(job.JSON)?.AsObject();
                    if (json != null)
                    {
                        var lastUpdated = json["lastUpdated"] == null ? Utilities.GetCurrentUtcDate() : json["lastUpdated"]!.GetValue<DateTime>();
                        json["message"] = lastUpdated.ToString("u") + " : " + message + "\n" + json["message"];
                        json["status"] = status;
                        json["lastUpdated"] = Utilities.GetCurrentUtcDate();
                        job.JSON = json.ToJsonString();
                        await _errorContext.SaveChangesAsync();
                    }
                }
                catch { }
            }
        }

        public static async Task UpdateJob(ArbitrationDbContext _errorContext, JobQueueItem? job, string message, string status, int processed, int total, string lastError = "")
        {
            if (job != null)
            {
                try
                {
                    var json = JsonObject.Parse(job.JSON)?.AsObject();
                    if (json != null)
                    {
                        json["lastError"] = lastError;
                        json["message"] = message;
                        json["recordsProcessed"] = processed;
                        json["totalRecords"] = total;
                        json["status"] = status;
                        json["lastUpdated"] = Utilities.GetCurrentUtcDate();
                        job.JSON = json.ToJsonString();
                        job.UpdatedOn = Utilities.GetCurrentUtcDate();
                        await _errorContext.SaveChangesAsync();
                    }
                }
                catch { }
            }
        }

        public static async Task UpdateJob(ArbitrationDbContext _errorContext, JobQueueItem? job, string message, string status, int added, int errors, int processed, int skipped, int total, int updates)
        {
            if (job != null)
            {
                try
                {
                    var json = JsonObject.Parse(job.JSON)?.AsObject();
                    if (json != null)
                    {
                        json["message"] = message;
                        json["recordsAdded"] = added;
                        json["recordsError"] = errors;
                        json["recordsProcessed"] = processed;
                        json["totalRecords"] = total;
                        json["status"] = status;
                        json["recordsSkipped"] = skipped;
                        json["recordsUpdated"] = updates;
                        json["lastUpdated"] = Utilities.GetCurrentUtcDate();
                        job.JSON = json.ToJsonString();
                        job.UpdatedOn = Utilities.GetCurrentUtcDate();
                        if (_errorContext.Entry(job).State == EntityState.Detached)
                            _errorContext.Entry(job).State = EntityState.Modified;
                        await _errorContext.SaveChangesAsync();
                    }
                }
                catch { }
            }
        }
        
    }
}
