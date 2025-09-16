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
using MPArbitration.Utility;

namespace MPArbitration.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class MPBaseController : ControllerBase
    {
        internal static readonly string[] VALID_ROLES = { "admin", "briefapprover", "briefpreparer", "briefwriter", "manager", "negotiator", "nsa", "reporter", "state" };
        protected readonly ArbitrationDbContext _context;
        protected readonly BlobContainerClient _containerClient;
        protected IConfiguration _configuration;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        public MPBaseController(ArbitrationDbContext context, IConfiguration configuration)
        {
            _configuration = configuration;
            _context = context;
            string conn = configuration.GetSection("Storage").GetSection("Connection").Value;
            string name = configuration.GetSection("Storage").GetSection("Container").Value;
            _containerClient = new BlobContainerClient(conn, name);
        }

        #region Private Methods
        /*
        internal async Task<bool> CheckIsActive()
        {
            var name = User?.Identity?.Name ?? String.Empty;
            var cu = await _context.AppUsers.FirstOrDefaultAsync(d => d.Email == name);
            if (cu == null)
                return false;

            return cu.IsActive;
        }
        
        internal async Task<bool> CheckForRole(string role)
        {
            var name = User?.Identity?.Name ?? String.Empty;
            var cu = await _context.AppUsers.FirstOrDefaultAsync(d => d.Email == name);
            if (cu == null)
                return false;

            return cu.Roles.ToLower().Split(new char[] { ',', ';' }).Contains(role);
        }
        */


        internal async Task<AppUser?> GetCurrentUser()
        {
            if (User.Identity == null)
                return null;

            if (User.Identity.Name != null)
            {
                var currentUser = await _context.AppUsers.FirstOrDefaultAsync(d => d.Email == User.Identity.Name && d.IsActive);
                return currentUser;
            }
            else if(User.Identity is System.Security.Claims.ClaimsIdentity)
            {
                var r = HttpContext.User.Claims.FirstOrDefault(d => d.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
                if (r != null && r.Value.Equals("User_Read"))
                {
                    return new AppUser() { IsActive = true, Id = -1, Roles = "system", Email="noreply@appregistration.local" };
                }
            }
            return null;
        }

        internal async Task<Authority?> GetNSA()
        {
            if (_context == null)
                return null;

            return await _context.Authorities.FirstOrDefaultAsync(d => d.Key == "nsa");
        }

        internal string GetUsername()
        {
            var rgx = new Regex("[^a-zA-Z0-9 -]");
            var un = HttpContext.User.Claims.FirstOrDefault(d => d.Type == "name");
            if (un != null)
                return rgx.Replace(un.Value, "");
            un = HttpContext.User.Claims.FirstOrDefault(d => d.Type == "preferred_username");
            if (un != null)
                return rgx.Replace(un.Value, "");
            var r = HttpContext.User.Claims.FirstOrDefault(d => d.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
            return rgx.Replace(r != null && r.Value.Equals("User_Read") ? "app service" : "", "");
        }

        internal async Task<bool> SaveUploadLog(string authority, DateTime uploadDate, string log, ILogger logger, string documentType)
        {
            var uploadedOn = uploadDate;
            var uploadedBy = GetUsername();
            var success = false;
            try
            {
                using (var reader = new MemoryStream(Encoding.UTF8.GetBytes(log)))
                {
                    string blobName = $@"ImportLog-{authority}-{uploadDate.ToString("MM-dd-yyyy hh_mm_ss tt")}.log";

                    try
                    {
                        BlobClient blob = _containerClient.GetBlobClient(blobName);

                        logger.LogInformation($@"Attempting to upload file {blobName} to BLOB store...");
                        var response = await blob.UploadAsync(reader, true);
                        if (response.GetRawResponse().ReasonPhrase != "Created")
                            throw new Exception("Unexpected result from BLOB upload");

                        // add tags to new BLOB
                        var tags = new Dictionary<string, string>();
                        if(!string.IsNullOrEmpty(authority))
                            tags.Add("Authority", authority);
                        tags.Add("UploadedBy", uploadedBy);
                        tags.Add("BatchUploadDate", string.Format("{0:u}", uploadDate));
                        if (!string.IsNullOrEmpty(documentType))
                            tags.Add("DocumentType", documentType);
                        await blob.SetTagsAsync(tags);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Unable to write to BLOB storage. "+ ex.Message);
                        logger.LogError(ex.Message);
                    }
                }

                //log.AppendLine($@"Uploaded by {uploadedBy}");
                success = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                //log.AppendLine(ex.Message);
                //log.Append(ex.StackTrace);
            }

            return success;
        }
        #endregion
    }
}
