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
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using NuGet.Protocol;
using Newtonsoft.Json.Linq;
using System.Configuration;
using Microsoft.IdentityModel.Tokens;
using MPArbitration.Utility;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Runtime.Intrinsics.Arm;

// See https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-desktop-acquire-token-username-password?tabs=dotnet
// for information regarding token generation in case we want to support API access outside of this application scope
namespace MPArbitration.Controllers
{
    /// <summary>
    /// Work flow controller
    /// </summary>
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowController : MPBaseController
    {
        private readonly ILogger<WorkflowController> _logger;
        private readonly IImportDataSynchronizer _synchronizer;
        private readonly ArbitrationDbContext _errorContext;
        private readonly DisputeIdrDbContext _idr_context;
        private readonly IPrincipal _principle;

        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="idr_context"></param>
        /// <param name="configuration"></param>
        /// <param name="synchronizer"></param>
        /// <param name="principal"></param>
        public WorkflowController(ILogger<WorkflowController> logger, ArbitrationDbContext context, DisputeIdrDbContext idr_context, IConfiguration configuration, IImportDataSynchronizer synchronizer, IPrincipal principal) : base(context, configuration)
        {
            _logger = logger;
            _synchronizer = synchronizer;
            this._principle = principal;
            this._idr_context = idr_context;

            var contextOptions = new DbContextOptionsBuilder<ArbitrationDbContext>()
                .UseSqlServer(_configuration.GetSection("ConnectionStrings").GetSection("ConnStr").Value)
                .Options;
            _errorContext = new ArbitrationDbContext(contextOptions);
        }

        /// <summary>
        /// Retrieves a list of arbitration cases based on the given Dispute Number.
        /// </summary>
        /// <param name="disputeNumber"> Dispute Number (string, required) param: The Dispute Number of the arbitration cases to retrieve. 
        /// Must start with "DISP-".</param>
        /// <returns></returns>
        [HttpGet("Dispute/{disputeNumber}")]
        [Produces("application/json")]
        public async Task<ActionResult<List<WorkflowNSA>>> GetArbitIdFromDisputeNumber(string disputeNumber)
        {
            var u = await GetCurrentUser();
            if (u == null)
                // return Unauthorized("No active User context!");
                Console.WriteLine("No active User context!");

            if (disputeNumber == null || !disputeNumber.ToUpper().Trim().StartsWith("DISP-"))
                return BadRequest("Invalid Dispute Number. It should be non null and start with 'DISP-'");
            try
            {
                var response = await GetNSAResponse(disputeNumber);

                if (response == null || response.Count <= 0)
                {
                    return BadRequest("Data not found for this Dispute Number: " + disputeNumber);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, (ex.InnerException != null) ? ex.InnerException.Message : "");
                throw ex;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Retrieves a list of arbitration cases based on the given authority case ID.
        /// </summary>
        /// <param name="authorityCaseId">authorityCaseId (string, required) param: The authority case ID of the arbitration cases to retrieve.</param>
        /// <returns></returns>
        [HttpGet("ArbitInfo/{authorityCaseId}")]
        [Produces("application/json")]
        public async Task<ActionResult<List<WorkflowState>>> GetArbitIdFromAuthorityCaseID(string authorityCaseId)
        {
            var u = await GetCurrentUser();
            if (u == null)
                Console.WriteLine("No active User context!");

            try
            {
                var response = await GetStateResponse(authorityCaseId);
               
                if (response == null || response.Count <= 0)
                {
                    return BadRequest("Data not found for this Authority Case Id: " + authorityCaseId);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Retrieves a list of arbitration cases based on the given payor claim number.
        /// </summary>
        /// <param name="payorClaimNumber"> Payor Claim Number (string, required) param: The payor claim number of the arbitration cases to retrieve.</param>
        /// <returns></returns>
        [HttpGet("ArbitCaseByPayorClaimNumber/{payorClaimNumber}")]
        [Produces("application/json")]
        public async Task<ActionResult<List<WorkflowPayorClaim>>> GetArbitIdFromPayorClaimNumber(string payorClaimNumber)
        {
            var u = await GetCurrentUser();
            if (u == null)
                Console.WriteLine("No active User context!");

            try
            {
                var response = await GetPayorClaimNumberResponse(payorClaimNumber);
                if (response == null || response.Count <= 0)
                {
                    return BadRequest("Data not found for this Payor Claim Number: " + payorClaimNumber);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, (ex.InnerException != null) ? ex.InnerException.Message : "");
                throw ex;
            }
            finally
            {
            }
        }


        /// <summary>
        /// Update the arbitration case based on the given authority case ID.
        /// </summary>
        /// <param name="authorityCasePayLoad"> Update authority case payLoad</param>
        /// <returns> Updated arbitration case object</returns>
        [HttpPut("updateByAuthorityCaseId")]
        [Produces("application/json")]
        public async Task<ActionResult<string>> UpdateAuthorityCase(UpdateAuthorityCasePayLoad authorityCasePayLoad)
        {
            ArbitrationCase objArbitrationCase = new ArbitrationCase();

            try
            {
                objArbitrationCase = _context.ArbitrationCases.Where(arbit => arbit.AuthorityCaseId == authorityCasePayLoad.AuthorityCaseId).FirstOrDefault()!;

                if (objArbitrationCase == null)
                {
                    return BadRequest("Data not found for this Aithority Case ID: " + authorityCasePayLoad.AuthorityCaseId);
                }

                ArbitrationStatus wfstatus;
                if (Enum.TryParse(authorityCasePayLoad.NSAWorkflowStatus, out wfstatus))
                {
                    objArbitrationCase.NSAWorkflowStatus = wfstatus;
                }

                if (authorityCasePayLoad.InformalTeleconferenceDate != null)
                    objArbitrationCase.InformalTeleconferenceDate = authorityCasePayLoad.InformalTeleconferenceDate;

                if (authorityCasePayLoad.AuthorityStatus != null)
                    objArbitrationCase.AuthorityStatus = authorityCasePayLoad.AuthorityStatus;

                if (authorityCasePayLoad.ArbitrationBriefDueDate != null)
                    objArbitrationCase.ArbitrationBriefDueDate = authorityCasePayLoad.ArbitrationBriefDueDate;

                _context.ArbitrationCases.Update(objArbitrationCase);
                await _context.SaveChangesAsync();

                return Ok("Arbitration case has been updated successfully");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Update the Arbitration case based on the given Payor Claim Number.
        /// </summary>
        /// <param name="payorClaimNumberPayload"> Update Payor Claim Number payLoad</param>
        /// <returns> Updated arbitration case object </returns>
        [HttpPut("updateByPayorClaimNumber")]
        [Produces("application/json")]
        public async Task<ActionResult<string>> updateByPayorClaimNumber(UpdateByPayorClaimNumberPayload payorClaimNumberPayload)
        {
            ArbitrationCase objArbitrationCase = new ArbitrationCase();
            DisputeMaster objDisputeMaster = new DisputeMaster();

            try
            {
                objArbitrationCase = _context.ArbitrationCases.Where(arbit => arbit.PayorClaimNumber == payorClaimNumberPayload.PayorClaimNumber).FirstOrDefault()!;
                ArbitrationStatus wfstatus;
                if (objArbitrationCase == null)
                {
                    return BadRequest("Data not found for this Payor Claim Number: " + payorClaimNumberPayload.PayorClaimNumber);
                }
                else
                {
                    if (Enum.TryParse(payorClaimNumberPayload?.NSAWorkflowStatus, out wfstatus))
                    {
                        objArbitrationCase.NSAWorkflowStatus = wfstatus;
                    }

                    if (payorClaimNumberPayload?.AuthorityStatus != null)
                        objArbitrationCase.AuthorityStatus = payorClaimNumberPayload?.AuthorityStatus!;

                    _context.ArbitrationCases.Update(objArbitrationCase);

                    await _context.SaveChangesAsync();


                    // Collect dispute number by orbitId
                    var disputeNumber = _idr_context.DisputeCPT.Where(d => d.ArbitId == objArbitrationCase.Id)
                                .Select(d => d.DisputeNumber).FirstOrDefault();

                    if (disputeNumber != null && payorClaimNumberPayload?.DisputeStatus != null)
                    {
                        // Update dispute master 
                        objDisputeMaster = _idr_context.DisputeMaster.Where(d => d.DisputeNumber == disputeNumber).FirstOrDefault()!;
                        if (objDisputeMaster != null && payorClaimNumberPayload != null && payorClaimNumberPayload.DisputeStatus != null)
                        {
                            objDisputeMaster.DisputeStatus = payorClaimNumberPayload.DisputeStatus;

                            _idr_context.DisputeMaster.Update(objDisputeMaster);
                            _idr_context.SaveChanges();
                        }
                    }

                    return Ok("Arbitration case and dispute(If exist) has been updated successfully");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
            }
        }


        /// <summary>
        /// Update the arbitration case and disputes based on the given Dispute Number.
        /// </summary>
        /// <param name="disputeNumberPayload"> Update Dispute Number payLoad</param>
        /// <returns>Updated arbitration case object</returns>
        [HttpPut("updateByDisputeNumber")]
        [Produces("application/json")]
        public async Task<ActionResult> updateByDisputeNumber(UpdateByDisputeNumberPayload disputeNumberPayload)
        {
            ArbitrationCase objArbitrationCase = new ArbitrationCase();
            DisputeMaster objDisputeMaster = new DisputeMaster();
            try
            {
                // Update dispute master 
                objDisputeMaster = _idr_context.DisputeMaster.Where(d => d.DisputeNumber == disputeNumberPayload.DisputeNumber).FirstOrDefault()!;


                if (objDisputeMaster != null)
                {
                    // update the respective fileds only if value presents in payload.
                    // there is a possiblity to have the payload without some field. Those may have values in DB that should not be updated
                    if(disputeNumberPayload.DisputeStatus != null)
                        objDisputeMaster.DisputeStatus = disputeNumberPayload.DisputeStatus;

                    if (disputeNumberPayload.FeeAmountAdmin != null)
                        objDisputeMaster.FeeAmountAdmin = disputeNumberPayload.FeeAmountAdmin;

                    if (disputeNumberPayload.FeeAmountEntity != null)
                        objDisputeMaster.FeeAmountEntity = disputeNumberPayload.FeeAmountEntity;

                    if (disputeNumberPayload?.FeeAmountAdmin + disputeNumberPayload?.FeeAmountEntity > 0)
                        objDisputeMaster.FeeAmountTotal = disputeNumberPayload?.FeeAmountAdmin + disputeNumberPayload?.FeeAmountEntity;

                    if (disputeNumberPayload?.FeeDueDate != null)
                        objDisputeMaster.FeeDueDate = disputeNumberPayload.FeeDueDate;

                    if(disputeNumberPayload?.FormalReceivedDate!= null)
                    objDisputeMaster.FormalReceivedDate = disputeNumberPayload.FormalReceivedDate;

                    if (disputeNumberPayload?.BriefDueDate != null)
                    objDisputeMaster.BriefDueDate = disputeNumberPayload.BriefDueDate;

                    _idr_context.DisputeMaster.Update(objDisputeMaster);
                   await  _idr_context.SaveChangesAsync();
                    return Ok("Dispute has been updated successfully");
                }
                else
                {
                    return BadRequest("Data not found for this Dispute Number: " + disputeNumberPayload.DisputeNumber);
                }

              
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
            }
        }


        /// <summary>
        /// To upload document
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="400">Bad Request</response>
        /// <response code="500">Internal Server error</response>
        /// <returns>  Returns action status</returns>
        [HttpPost("uploadDocument")]
        public async Task<IActionResult> UploadDocument(IFormFile file, [FromForm] UploadDocumentInput documentInputs)
        {
            if (documentInputs.PayorClaimNumber == null && documentInputs.AuthorityCaseId == null)
            {
                return BadRequest("Invalid request both Payor Claim Number and Authority Case Id Cannot be null ");
            }

            if (!file.FileName.ToLower().EndsWith(".pdf"))
            {
                return BadRequest("Invalid file type");
            }

            if (file != null && file.Length > 30000000)
            {
                return BadRequest("File size is too large. Split into multiple uploads or contact support.");
            }

            if (Enum.TryParse<CaseDocumentType>(documentInputs.DocumentType, true, out CaseDocumentType parseResult) == false)
            {
                return BadRequest("Unsupported document metadata");
            }

            var uploadedOn = Utilities.GetCurrentUtcDate();
            var uploadedBy = "Workflow App";
            string blobURL = "";

            ArbitrationCase? arbitCase = new ArbitrationCase();

            if (documentInputs.PayorClaimNumber != null)
            {
                arbitCase = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted
                       && d.PayorClaimNumber == documentInputs.PayorClaimNumber);
            }
            else if (documentInputs.AuthorityCaseId != null)
            {
                arbitCase = await _context.ArbitrationCases.FirstOrDefaultAsync(d => !d.IsDeleted
                      && d.AuthorityCaseId == documentInputs.AuthorityCaseId);
            }

            if (arbitCase == null)
            {
                return BadRequest("Record not found or unauthorized to attach Files to this Case");
            }

            using (var reader = file!.OpenReadStream())
            {
                string blobName = $@"{arbitCase.Id}-{documentInputs.DocumentType}-{file.FileName.ToLower()}";

                var blob = _containerClient.GetBlobClient(blobName);

                _logger.LogInformation($@"Attempting to upload file {blobName} to BLOB store...");

                var response = await blob.UploadAsync(reader, true);
                if (response.GetRawResponse().ReasonPhrase != "Created")
                {
                    throw new Exception("Unexpected result from BLOB upload");
                }

                // add tags to new BLOB
                var tags = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(arbitCase.AuthorityCaseId))
                {
                    tags.Add("AuthorityCaseId", arbitCase.AuthorityCaseId);
                }
                tags.Add("Id", arbitCase.Id.ToString());
                tags.Add("UpdatedBy", uploadedBy);
                tags.Add("DocumentType", documentInputs.DocumentType.ToLower());

                if (!string.IsNullOrEmpty(arbitCase.EHRNumber))
                {
                    tags.Add("EHRNumber", arbitCase.EHRNumber);
                }

                await blob.SetTagsAsync(tags);

                blobURL = $@"{_containerClient.Uri.ToString()}/{blobName}";
                _logger.LogInformation($@"BLOB uploaded to {blobURL}");

                // save entry into database to facilitate reporting on claims that do not have certain attachments per Megan R. request 2023-9-28
                var emrFile = await _context.EMRClaimAttachments.FirstOrDefaultAsync(d => !d.IsDeleted && d.ArbitrationCaseId == arbitCase.Id
                    && d.DocType == documentInputs.DocumentType && d.BLOBName == blobName);
                if (emrFile != null)
                {
                    // update existing entry - records could easily get purged in Azure and de-sync these records so...
                    emrFile.UpdatedOn = Utilities.GetCurrentUtcDate();
                    emrFile.UpdatedBy = uploadedBy;
                }
                else
                {
                    var uploadDate = Utilities.GetCurrentUtcDate();
                    emrFile = new EMRClaimAttachment { BLOBLink = blobURL, ArbitrationCaseId = arbitCase.Id, BLOBName = blobName, CreatedBy = uploadedBy, CreatedOn = uploadDate, DocType = documentInputs.DocumentType, UpdatedBy = uploadedBy, UpdatedOn = uploadDate };
                    await _context.EMRClaimAttachments.AddAsync(emrFile);
                }

                await _context.SaveChangesAsync();

            }
            return Ok("Document has been uploaded successfully");
        }

        /// <summary>
        /// Get State Response
        /// </summary>
        /// <param name="authorityCaseId"> AuthorityCaseId input</param>
        /// <returns> List of WorkflowState</returns>
        private async Task<List<WorkflowState>> GetStateResponse(string authorityCaseId)
        {
            List<WorkflowState> liststate = new List<WorkflowState>();
            try
            {
                // Collect all entities mapped with authorityCaseId and process this collection in result selection
                // to avoid reduntant result.
                var entities = from arbitCase in _context.ArbitrationCases
                               join entity in _context.Entities on arbitCase.EntityNPI equals entity.NPINumber
                               where arbitCase.AuthorityCaseId == authorityCaseId
                               select entity;

                var result = from c in _context.CaseSettlementDetails
                             join a in _context.ArbitrationCases on c.ArbitrationCaseId equals a.Id
                             where c.AuthorityCaseId == authorityCaseId
                             select new
                             {
                                 a.Id,
                                 c.AuthorityCaseId,
                                 a.Payor,
                                 a.Customer,
                                 StateCode = !a.Authority.IsNullOrEmpty() ? a.Authority.ToUpper().Trim() : "",
                                 c.IsDeleted,
                                 c.CaseSettlementId,
                                 a.NSATracking,
                                 a.FirstResponseDate,
                                 certifiedentityname = entities.Where(entity => entity.Name == a.Entity && entity.NPINumber == a.EntityNPI).Select(e => e.Name).SingleOrDefault(),
                                 certifiedentityid = entities.Where(entity => entity.Name == a.Entity && entity.NPINumber == a.EntityNPI).Select(e => e.Id).SingleOrDefault()
                             };

                var resultList = await result.Distinct().ToListAsync();

                foreach (var arbit in resultList)
                {
                    var payor = _context.Payors.Where(p => p.Name.ToLower().Trim() == arbit.Payor.ToLower().Trim()).FirstOrDefault();
                    var customer = _context.Customers.Where(c => c.Name.ToLower().Trim() == arbit.Payor.ToLower().Trim()).FirstOrDefault();
                    if (payor != null)
                        payor.JSON = "";
                    if (customer != null) 
                        customer.JSON = "";
                    DateTime? dateNegotiationSent = GetNegotiationDate(arbit.NSATracking);
                    DateTime? firstResponseDate = FirstResponseDateOnly(arbit.FirstResponseDate);
                    liststate.Add(new WorkflowState(arbit.Id, payor, customer, arbit.StateCode, dateNegotiationSent, 
                                                        firstResponseDate, authorityCaseId, arbit.IsDeleted, arbit.CaseSettlementId, arbit.certifiedentityname, arbit.certifiedentityid));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }

            return liststate;
        }

        /// <summary>
        /// Get Payor Claim Number Response
        /// </summary>
        /// <param name="payorClaimNumber"> Payor Claim Number</param>
        /// <returns></returns>
        private async Task<List<WorkflowPayorClaim>> GetPayorClaimNumberResponse(string payorClaimNumber)
        {
            List<WorkflowPayorClaim> listPayor = new List<WorkflowPayorClaim>();
            try
            {
                // Collect all entities mapped with Payor Claim Number and process this collection in result selection
                // to avoid reduntant result.
                var entities = from arbitCase in _context.ArbitrationCases
                               join entity in _context.Entities on arbitCase.EntityNPI equals entity.NPINumber
                               where arbitCase.PayorClaimNumber == payorClaimNumber && !arbitCase.IsDeleted
                               select entity;

                var result = await _context.ArbitrationCases
                          .Where(a => a.PayorClaimNumber == payorClaimNumber && !a.IsDeleted)
                          .Select(a => new
                          {
                              a.Id,
                              a.Payor,
                              a.Customer,
                              StateCode = !a.Authority.IsNullOrEmpty() ? a.Authority.ToUpper().Trim() : "",
                              a.NSATracking,
                              a.FirstResponseDate,
                              certifiedentityname = entities.Where(entity => entity.Name == a.Entity && entity.NPINumber == a.EntityNPI).Select(e => e.Name).SingleOrDefault(),
                              certifiedentityid = entities.Where(entity => entity.Name == a.Entity && entity.NPINumber == a.EntityNPI).Select(e => e.Id).SingleOrDefault()
                          })
                          .ToListAsync();

                foreach (var arbit in result)
                {
                    var payor = _context.Payors.Where(p => p.Name.ToLower().Trim() == arbit.Payor.ToLower().Trim()).FirstOrDefault();
                    var customer = _context.Customers.Where(c => c.Name.ToLower().Trim() == arbit.Payor.ToLower().Trim()).FirstOrDefault();
                    if (payor != null)
                        payor.JSON = "";
                    if (customer != null)
                        customer.JSON = "";
                    DateTime? dateNegotiationSent = GetNegotiationDate(arbit.NSATracking);
                    DateTime? firstResponseDate = FirstResponseDateOnly(arbit.FirstResponseDate);
                    listPayor.Add(new WorkflowPayorClaim(arbit.Id, payor, customer, arbit.StateCode, dateNegotiationSent, firstResponseDate, payorClaimNumber, arbit.certifiedentityname, arbit.certifiedentityid));
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }

            return listPayor;
        }

        /// <summary>
        /// Get NSA Response
        /// </summary>
        /// <param name="disputeNumber"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<List<WorkflowNSA>?> GetNSAResponse(string disputeNumber)
        {
            List<WorkflowNSA> listnsa = new List<WorkflowNSA>();
            try
            {

                List<int> colArbitId = (from dispmaster in _idr_context.DisputeMaster
                                         join dispcpt in _idr_context.DisputeCPT on dispmaster.DisputeNumber equals dispcpt.DisputeNumber
                                         where dispmaster.DisputeNumber == disputeNumber
                                         orderby dispcpt.ArbitId
                                         select dispcpt.ArbitId)
                                        .Distinct().ToList();

                if (colArbitId == null || colArbitId.Count()<=0)
                {
                    return null;
                }


                var colArbitration = await _context.ArbitrationCases
                    .Where(a => colArbitId.Contains(a.Id))
                    .ToListAsync(); // Execute query and convert to list

                // Collect all entities mapped with orbit case id's and process this collection in result selection
                // to avoid reduntant result.
                var entities = from arbitCase in _context.ArbitrationCases
                               join entity in _context.Entities on arbitCase.EntityNPI equals entity.NPINumber
                               where colArbitId.Contains (arbitCase.Id)
                               select entity;

                foreach (var arbit in colArbitration)
                {
                    var payor = _context.Payors.Where(p => p.Name.ToLower().Trim() == arbit.Payor.ToLower().Trim()).FirstOrDefault();
                    var customer = _context.Customers.Where(c => c.Name.ToLower().Trim() == arbit.Payor.ToLower().Trim()).FirstOrDefault();
                    if (payor != null)
                        payor.JSON = "";
                    if (customer != null)
                        customer.JSON = "";
                    DateTime? dateNegotiationSent = GetNegotiationDate(arbit.NSATracking);
                    DateTime? firstResponseDate = FirstResponseDateOnly(arbit.FirstResponseDate);
                    var stateCode = !arbit.Authority.IsNullOrEmpty() ? arbit.Authority.ToUpper().Trim() : "";
                    var certifiedentityname = entities.Where(entity => entity.Name == arbit.Entity && entity.NPINumber == arbit.EntityNPI).Select(e => e.Name).SingleOrDefault();
                    var certifiedentityid = entities.Where(entity => entity.Name == arbit.Entity && entity.NPINumber == arbit.EntityNPI).Select(e => e.Id).SingleOrDefault();
                    listnsa.Add(new WorkflowNSA(arbit.Id, payor, customer, stateCode, dateNegotiationSent, firstResponseDate, disputeNumber, certifiedentityname, certifiedentityid));
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }
            return listnsa;
        }
       
        /// <summary>
        /// Get negotiation date from nsaTracjking json
        /// </summary>
        /// <param name="nsaTracking"></param>
        /// <returns></returns>
        private DateTime? GetNegotiationDate(string nsaTracking)
        {
            DateTime? dateNegotiationSent = null;

            if (nsaTracking != null)
            {
                JObject jObject = JObject.Parse(nsaTracking);
                if (jObject != null)
                {
                    var dateNg = jObject.SelectToken("DateNegotiationSent");
                    if (dateNg != null && dateNg.Type != JTokenType.Null)
                    {
                        var dt = dateNg.Value<DateTime>();
                        dateNegotiationSent = new DateTime(dt.Year, dt.Month, dt.Day);
                    }
                }
            }
            return dateNegotiationSent;
        }

        /// <summary>
        /// first response convert to dateonly from datetime
        /// </summary>
        /// <param name="firstRespononseDt"></param>
        /// <returns></returns>
        private static DateTime? FirstResponseDateOnly(DateTime? firstRespononseDt)
        {
            DateTime? firstResponseDate = null;
            if (firstRespononseDt.HasValue)
            {
                var fr = firstRespononseDt.Value;
                firstResponseDate = new DateTime(fr.Year, fr.Month, fr.Day);
            }

            return firstResponseDate;
        }

    }
}
