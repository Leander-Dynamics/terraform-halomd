using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MPArbitration.Model;
using MPArbitration.Utility;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using ObjectsComparator.Comparator.Helpers;
using System.Globalization;
using System.Linq;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace MPArbitration.Controllers
{
    /// <summary>
    /// Controller to handle dispute actions
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class DisputeController : MPBaseController
    {
        private readonly ILogger<DisputeController> _logger;
        private readonly DisputeIdrDbContext _idr_context;

        /// <summary>
        /// Contstructor to handle initializations
        /// </summary>
        /// <param name="logger">Ilogger to inject DisputeController</param>
        /// <param name="context">ArbitrationDbContext  instance</param>
        /// <param name="idr_context">DisputeIdrDbContext instance</param>
        /// <param name="configuration">IConfiguration instance</param>
        public DisputeController(ILogger<DisputeController> logger, ArbitrationDbContext context, DisputeIdrDbContext idr_context, IConfiguration configuration) : base(context, configuration)
        {
            _logger = logger;
            this._idr_context = idr_context;
        }


        /// <summary>
        /// Action to get dispute data's with search filter options
        /// </summary>s
        /// <param name="searchInput"> Search filter input</param>
        /// <param name="paginationFilter"> Paging information</param>
        /// <returns>List of dispute data's with paging informations</returns>
        [HttpGet("getDisputeList")]
        public async Task<IActionResult> getDisputeList([FromQuery] DisputeSearchInput searchInput, [FromQuery] PaginationFilter paginationFilter)
        {
            var response = await getDisputeListResponse(searchInput, paginationFilter);
            return Ok(response);
        }


        /// <summary>
        /// Action to get dispute data's by dispute number
        /// </summary>s
        /// <param name="disputeNumber"> Dispute number input</param>
        /// <returns>Dispute detail object with data's </returns>
        [HttpGet("getDisputeByDisputeNumber/{disputeNumber}")]
        public async Task<ActionResult<APIResponse>> getDisputeByDisputeNumber(string disputeNumber)
        {
            var response = await getDisputeDetailsResponse(disputeNumber);

            switch (response.statusCode)
            {
                case (int)DataConstants.StatusCodes.NotFound:
                    return NotFound(response);
                case (int)DataConstants.StatusCodes.BadRequest:
                    return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Action to get dispute entity master datas
        /// </summary>
        /// <returns>List of dispute entity master datas</returns>
        [HttpGet("getDisputeMasterEntity")]
        public IActionResult getDisputeMasterEntity()
        {
            var entityList = (from entity in _idr_context.REF_DisputeMasterEntity
                              where entity.Entity != null
                              select entity.Entity).Distinct().ToList();
            return Ok(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "Dispute entity retrieved successfully", data = entityList });
        }

        /// <summary>
        /// Action to get service line master datas
        /// </summary>
        /// <returns>List of service line master datas</returns>
        [HttpGet("getDisputeMasterServiceLine")]
        public IActionResult getDisputeMasterServiceLine()
        {
            var servicelineList = (from entity in _idr_context.REF_DisputeMasterServiceLine
                                   where entity.ServiceLine != null && entity.ServiceLine != string.Empty
                                   select entity.ServiceLine).Distinct().ToList();
            return Ok(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "Dispute service line retrieved successfully", data = servicelineList });
        }

        /// <summary>
        /// Action to get dispute certified entity master datas
        /// </summary>
        /// <returns>List of dispute certified entity master datas</returns>
        [HttpGet("getDisputeMasterCertifiedEntity")]
        public IActionResult getDisputeMasterCertifiedEntity()
        {
            var certifiedEntityList = (from certifiedEntity in _idr_context.REF_DisputeMasterCertifiedEntity
                                       where certifiedEntity.CertifiedEntity != null
                                       select certifiedEntity.CertifiedEntity).Distinct().ToList();
            return Ok(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "Dispute certified entity retrieved successfully", data = certifiedEntityList });

        }


        /// <summary>
        /// Action to get dispute status master datas
        /// </summary>
        /// <returns>List of dispute status master datas</returns>
        [HttpGet("getDisputeMasterDisputeStatus")]
        public IActionResult getDisputeMasterDisputeStatus()
        {
            var disputeStatusList = (from disputeStatus in _idr_context.REF_DisputeMasterDisputeStatus
                                     where disputeStatus.DisputeStatus != null
                                     select disputeStatus.DisputeStatus).Distinct().ToList();
            return Ok(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "Dispute status retrieved successfully", data = disputeStatusList });

        }


        /// <summary>
        /// Action to get dispute customer master datas
        /// </summary>
        /// <returns>List of dispute customer master datas</returns>
        [HttpGet("getDisputeMasterCustomer")]
        public IActionResult getDisputeMasterCustomer()
        {
            var disputeCustomerList = (from dispcumtomer in _idr_context.REF_DisputeMasterCustomer
                                       where dispcumtomer.Customer != null
                                       select dispcumtomer.Customer).Distinct().ToList();
            return Ok(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "Dispute customer's retrieved successfully", data = disputeCustomerList });

        }


        /// <summary>
        /// Action to get dispute provider NPI  master datas
        /// </summary>
        /// <returns>List of dispute provider NPI master datas</returns>
        [HttpGet("getDisputeMasterProviderNPI")]
        public IActionResult getDisputeMasterProviderNPI()
        {
            var providerNPIList = (from dispmaster in _idr_context.DisputeMaster
                                   select dispmaster.EntityNPI).Distinct().ToList();
            return Ok(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "ProviderNPI's retrieved successfully", data = providerNPIList });

        }


        /// <summary>
        /// Action to get dispute arbitId's with search 
        /// </summary>
        /// <returns>List of all dispute ArbitId's if search condition met, 
        ///  If search input not given will returns top 500 latest ArbitId's
        /// </returns>
        [HttpGet("getDisputeArbitIds")]
        public IActionResult getDisputeArbitIds(string? searchInput)
        {
            List<int> arbitIds = new List<int>();
            if (!string.IsNullOrEmpty(searchInput))
            {
                arbitIds = _idr_context.DisputeCPT.Where(p => p.ArbitId.ToString().StartsWith(searchInput))
                                    .Select(a => a.ArbitId).Distinct().ToList();
            }
            else
            {
                arbitIds = _idr_context.DisputeCPT.OrderByDescending(ord => ord.ArbitId).Skip(0).Take(500)
                                        .Select(a => a.ArbitId).Distinct().ToList();
            }
            return Ok(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "Dispute arbitId's retrieved successfully", data = arbitIds });

        }

        /// <summary>
        /// Action to get dispute log data's by id
        /// </summary>s
        /// <param name="id"> Id of dispute table(DisputMaster/DisputeCPT)</param>
        /// <returns>List of dispute log data's </returns>
        [HttpGet("getDisputeLogsById/{id}")]
        public async Task<IActionResult> getDisputeLogsById(int id)
        {
            var response = await _idr_context.XLog_ChangeLog.Where(p => p.TransactionID == id)
                .OrderByDescending(y => y.CreatedDate).ToListAsync();
            return Ok(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "Dispute arbitId's retrieved successfully", data = response });

        }

        /// <summary>
        /// Action method to get all brief approver users from arbit appusers
        /// </summary>
        /// <returns> List of brief approver users</returns>

        [HttpGet("getBriefApprover/users")]
        public async Task<IActionResult> getBriefApprover()
        {
            var u = await GetCurrentUser();
            if (u == null)
                return Unauthorized("No active User context!");

            // select and return data       
            var result = await _context.AppUsers.Where(au => au.IsActive == true && au.Roles.Contains("briefapprover"))
                                .Select(u => new
                                {
                                    Email = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(u.Email.Substring(0, u.Email.LastIndexOf("@")).Replace(".", " ")),
                                    Id = u.Email
                                }).ToListAsync();
            return Ok(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "Brief approvers retrieved successfully", data = result });

        }

        /// <summary>
        /// Action to update dispute status by disputeId
        /// </summary>
        /// <param name="disputeId"> Dispute Id input</param>
        /// <param name="disputeStatus"> Dispute status input</param>
        /// <param name="briefApprover"> Brief Approver input</param>
        /// <returns>Update success message</returns>
        [HttpPut("updateDisputeStatusByID/{disputeId}")]
        public IActionResult UpdateDisputeStatusByID(int disputeId, string? disputeStatus, string? briefApprover)
        {
            // Get current logged in user from the context for log purpose
            var user = GetCurrentUser();
            if (user == null)
            {
                return Unauthorized("No active User context!");
            }
            DisputeLog disputeLog = new DisputeLog();
            DisputeMaster? dispMaster = _idr_context.DisputeMaster.Where(disp => disp.Id == disputeId).SingleOrDefault();
            if (dispMaster == null)
            {
                return NotFound("Record not found");
            }
            // Create log information
            disputeLog.PreviousValue = dispMaster.ToJson();
            disputeLog.Activity = "Search page";
            disputeLog.TableName = "DisputeMaster";
            disputeLog.CreatedDate = Utilities.GetCurrentUtcDate();
            disputeLog.CreatedBy = user?.Result?.Email;
            disputeLog.TransactionID = dispMaster.Id;

            dispMaster.DisputeStatus = disputeStatus != null ? disputeStatus : dispMaster.DisputeStatus;

            if (briefApprover != null && briefApprover != dispMaster.BriefApprover)
            {
                dispMaster.BriefApprover = briefApprover;
                dispMaster.BriefAssignedDate = Utilities.GetCurrentUtcDate();
            }
            _idr_context.DisputeMaster.Update(dispMaster);
            _idr_context.SaveChanges();

            // save log information
            disputeLog.NewValue = dispMaster.ToJson();
            _idr_context.XLog_ChangeLog.Add(disputeLog);
            _idr_context.SaveChanges();


            return Ok(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = dispMaster.DisputeNumber + ": Dispute status has been updated successfully", data = null });

        }

        /// <summary>
        /// Action to update dispute data's 
        /// </summary>s
        /// <param name="disputeDetail"> Dispute detail object input</param>
        /// <returns>Update success message</returns>
        [HttpPut("updateDispute")]
        public async Task<ActionResult> updateDispute(DisputeDetail disputeDetail)
        {
            var response = await updateDisputeDetail(disputeDetail, disputeDetail.BriefAssignedDate);

            switch (response.statusCode)
            {
                case (int)DataConstants.StatusCodes.NotFound:
                    return NotFound(response);
                case (int)DataConstants.StatusCodes.BadRequest:
                    return BadRequest(response);
                case (int)DataConstants.StatusCodes.Unauthorized:
                    return Unauthorized(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// To delete a DisputeCPT data by id
        /// </summary>
        /// <param name="id"> DisputeCPT id</param>
        /// <returns>Delete success message</returns>
        [HttpDelete("deleteDisputeCPTbyId/{id}")]
        public async Task<ActionResult> deleteDisputeCPTbyId(int id)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return Unauthorized("No active User context!");
            }

            int disputeMastersId = (from dispmaster in _idr_context.DisputeMaster
                                    join dispcpt in _idr_context.DisputeCPT on dispmaster.DisputeNumber equals dispcpt.DisputeNumber
                                    where dispcpt.Id.Equals(id)
                                    select dispmaster.Id).FirstOrDefault();

            DisputeCPT? disputeCPTToRemove = await _idr_context.DisputeCPT.FindAsync(id);
            if (disputeCPTToRemove != null)
            {
                _idr_context.ChangeTracker.AutoDetectChangesEnabled = false;
                _idr_context.DisputeCPT.Remove(disputeCPTToRemove!);
                _idr_context.SaveChanges();
                // Create disputeCPT levellog information
                DisputeLog disputeCPTLog = new DisputeLog();
                disputeCPTLog.PreviousValue = disputeCPTToRemove.ToJson();
                disputeCPTLog.NewValue = "DisputeCPT has been deleted";
                disputeCPTLog.Activity = "Dispute page";
                disputeCPTLog.TableName = "DisputeCPT";
                disputeCPTLog.CreatedDate = Utilities.GetCurrentUtcDate();
                disputeCPTLog.CreatedBy = user?.Result?.Email;
                disputeCPTLog.TransactionID = disputeMastersId!;
                _idr_context.XLog_ChangeLog.Add(disputeCPTLog);
                await _idr_context.SaveChangesAsync();
                return Ok(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "DisputeCPT data has been deleted successfully", data = null });
            }
            else
            {
                return BadRequest(new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "DisputeCPT not found", data = null });

            }
        }

        /// <summary>
        /// Method to get dispute data's with search filter options
        /// </summary>
        /// <param name="searchInput"> Search filter input</param>
        /// <param name="paginationFilter"> Paging information</param>
        /// <returns>List of dispute data's with paging informations</returns>
        private async Task<PagedResponse<DisputeList>> getDisputeListResponse(DisputeSearchInput searchInput, PaginationFilter paginationFilter)
        {

            try
            {
                IQueryable<DisputeMaster> disputeMasters;

                disputeMasters = (from dispMaster in _idr_context.DisputeMaster
                                  orderby dispMaster.UpdatedOn descending
                                  select dispMaster).AsQueryable();

                // Add the search conditions if searh input found
                if (!string.IsNullOrEmpty(searchInput.DisputeStatus))
                {
                    var statusList = searchInput.DisputeStatus.Split(",");
                    disputeMasters = disputeMasters.Where(disp => statusList.Contains(disp.DisputeStatus));
                }

                if (!string.IsNullOrEmpty(searchInput.Customer))
                {
                    disputeMasters = disputeMasters.Where(disp => disp.Customer!.Equals(searchInput.Customer));
                }

                if (!string.IsNullOrEmpty(searchInput.DisputeNumber))
                {
                    disputeMasters = disputeMasters.Where(disp => disp.DisputeNumber!.Contains(searchInput.DisputeNumber));
                }

                if (!string.IsNullOrEmpty(searchInput.Entity))
                {
                    disputeMasters = disputeMasters.Where(disp => disp.Entity!.Equals(searchInput.Entity));
                }

                if (!string.IsNullOrEmpty(searchInput.CertifiedEntity))
                {
                    disputeMasters = disputeMasters.Where(disp => disp.CertifiedEntity!.Equals(searchInput.CertifiedEntity));
                }

                if (!string.IsNullOrEmpty(searchInput.EntityNPI))
                {
                    disputeMasters = disputeMasters.Where(disp => disp.EntityNPI!.Equals(searchInput.EntityNPI));
                }

                if (searchInput.ArbitID > 0)
                {
                    disputeMasters = from dispmaster in disputeMasters
                                     join dispcpt in _idr_context.DisputeCPT on dispmaster.DisputeNumber equals dispcpt.DisputeNumber
                                     where dispcpt.ArbitId.Equals(searchInput.ArbitID)
                                     select dispmaster;
                }

                if (searchInput.BriefDueDateFrom != null && searchInput.BriefDueDateFrom != DateTime.MinValue
                    && searchInput.BriefDueDateTo != null && searchInput.BriefDueDateTo != DateTime.MinValue)
                {
                    disputeMasters = disputeMasters.Where(disp => disp.BriefDueDate >= searchInput.BriefDueDateFrom
                                                                && disp.BriefDueDate <= searchInput.BriefDueDateTo);
                }
                else if (searchInput.BriefDueDateFrom != null && searchInput.BriefDueDateFrom != DateTime.MinValue)
                {
                    disputeMasters = disputeMasters.Where(disp => disp.BriefDueDate >= searchInput.BriefDueDateFrom);
                }

                if (!string.IsNullOrEmpty(searchInput.BriefApprover))
                {
                    disputeMasters = disputeMasters.Where(disp => disp.BriefApprover!.Equals(searchInput.BriefApprover));
                }
                // Take total after applying search before applying page filter
                var totalRecords = disputeMasters?.Select(disp => disp.DisputeNumber).Count();

                // Apply page filters
                if (paginationFilter != null)
                {

                    disputeMasters = disputeMasters!.Skip(paginationFilter.PageNumber * paginationFilter.PageSize)
                                                    .Take(paginationFilter.PageSize);
                }


                List<DisputeList> disputeList = await (from dispMaster in disputeMasters
                                                       select new DisputeList
                                                       {
                                                           Id = dispMaster.Id,
                                                           DisputeNumber = dispMaster.DisputeNumber,
                                                           Customer = dispMaster.Customer,
                                                           Entity = dispMaster.Entity,
                                                           CertifiedEntity = dispMaster.CertifiedEntity,
                                                           DisputeStatus = dispMaster.DisputeStatus,
                                                           FeeAmountAdmin = dispMaster.FeeAmountAdmin,
                                                           FeeAmountEntity = dispMaster.FeeAmountEntity,
                                                           FeeAmountTotal = dispMaster.FeeAmountTotal,
                                                           BriefApprover = dispMaster.BriefApprover,
                                                           NumberOfCPTs = (from dispCpt in _idr_context.DisputeCPT
                                                                           where dispCpt.DisputeNumber == dispMaster.DisputeNumber
                                                                           select dispCpt.Id).Count()

                                                       }).ToListAsync();

                if (paginationFilter == null || paginationFilter.PageSize < 1)
                {
                    return new PagedResponse<DisputeList>(disputeList);
                }
                else
                {
                    return PaginationHelper.CreatePagedResponse(paginationFilter, disputeList, totalRecords);

                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }

        }

        /// <summary>
        /// Method to get dispute data's by disputeNumber
        /// </summary>
        /// <param name="disputeNumber"> DisputeNumber input</param>
        /// <returns>Dispute object with data's </returns>
        private async Task<APIResponse> getDisputeDetailsResponse(string disputeNumber)
        {

            try
            {
                List<DisputeCPT> disputeCPTList = new List<DisputeCPT>();
                DisputeMaster? objDisputeMaster = new DisputeMaster();
                DisputeDetail objDisputeDetail = new DisputeDetail();

                objDisputeMaster = (from dispMaster in _idr_context.DisputeMaster
                                    where dispMaster.DisputeNumber == disputeNumber
                                    select dispMaster).SingleOrDefault();

                if (objDisputeMaster == null)
                {
                    return new APIResponse() { statusCode = (int)DataConstants.StatusCodes.NotFound, message = "Record not found", data = null };
                }

                disputeCPTList = await (from dispcpt in _idr_context.DisputeCPT
                                        where dispcpt.DisputeNumber == disputeNumber
                                        select dispcpt).ToListAsync();

                var cptArbitIds = disputeCPTList.Select(cpt => cpt.ArbitId).Distinct().ToList();

                var arbitCases = (from arbit in _context.ArbitrationCases
                                  where cptArbitIds.Contains(arbit.Id)
                                  select arbit).ToList();

                var disputeCPTWithArbitInfo = (from cpt in disputeCPTList
                                               join arbit in arbitCases on cpt?.ArbitId equals arbit?.Id into disputeCPTGrouped
                                               from arbit in disputeCPTGrouped.DefaultIfEmpty()
                                               select new DisputeCPT(cpt.Id, cpt.ArbitId, cpt.DisputeNumber)
                                               {
                                                   CPTCode = cpt.CPTCode,
                                                   BenchmarkAmount = cpt.BenchmarkAmount,
                                                   ProviderOfferAmount = cpt.ProviderOfferAmount,
                                                   PayorOfferAmount = cpt.PayorOfferAmount,
                                                   PrevailingParty = cpt.PrevailingParty,
                                                   AwardAmount = cpt.AwardAmount,
                                                   PayorClaimNumber = arbit?.PayorClaimNumber
                                               }).ToList();


                objDisputeDetail.Id = objDisputeMaster.Id;
                objDisputeDetail.DisputeNumber = objDisputeMaster?.DisputeNumber;
                objDisputeDetail.DisputeStatus = objDisputeMaster?.DisputeStatus;
                objDisputeDetail.DisputeWorkFlowStatus = objDisputeMaster?.DisputeWorkFlowStatus;
                objDisputeDetail.Customer = objDisputeMaster?.Customer;
                objDisputeDetail.Entity = objDisputeMaster?.Entity;
                objDisputeDetail.EntityNPI = objDisputeMaster?.EntityNPI;
                objDisputeDetail.Payor = objDisputeMaster?.Payor;
                objDisputeDetail.CertifiedEntity = objDisputeMaster?.CertifiedEntity;
                objDisputeDetail.Comments = objDisputeMaster?.Comments;
                objDisputeDetail.ServiceLine = objDisputeMaster?.ServiceLine;
                objDisputeDetail.SubmissionDate = objDisputeMaster?.SubmissionDate;
                objDisputeDetail.IDRESelectionDate = objDisputeMaster?.IDRESelectionDate;
                objDisputeDetail.FormalReceivedDate = objDisputeMaster?.FormalReceivedDate;
                objDisputeDetail.AwardDate = objDisputeMaster?.AwardDate;
                objDisputeDetail.FeeRequestDate = objDisputeMaster?.FeeRequestDate;
                objDisputeDetail.FeeDueDate = objDisputeMaster?.FeeDueDate;
                objDisputeDetail.FeeAmountAdmin = objDisputeMaster?.FeeAmountAdmin;
                objDisputeDetail.FeeAmountEntity = objDisputeMaster?.FeeAmountEntity;
                objDisputeDetail.FeeAmountTotal = objDisputeMaster?.FeeAmountAdmin + objDisputeMaster?.FeeAmountEntity;
                objDisputeDetail.FeePaidAmount = objDisputeMaster?.FeePaidAmount;
                objDisputeDetail.BriefDueDate = objDisputeMaster?.BriefDueDate;

                if (objDisputeDetail.BriefApprover != objDisputeMaster?.BriefApprover)
                {
                    objDisputeDetail.BriefApprover = objDisputeMaster?.BriefApprover;
                    objDisputeDetail.BriefAssignedDate = objDisputeMaster?.BriefAssignedDate;
                }

                objDisputeDetail.FeeInvoiceLink = objDisputeMaster?.FeeInvoiceLink;
                objDisputeDetail.BriefSubmissionLink = objDisputeMaster?.BriefSubmissionLink;
                objDisputeDetail.FeePaidDate = objDisputeMaster?.FeePaidDate;
                objDisputeDetail.CreatedOn = objDisputeMaster?.CreatedOn;
                objDisputeDetail.CreatedBy = objDisputeMaster?.CreatedBy;
                objDisputeDetail.UpdatedOn = objDisputeMaster?.UpdatedOn;
                objDisputeDetail.UpdatedBy = objDisputeMaster?.UpdatedBy;
                objDisputeDetail.DisputeCPTs = disputeCPTWithArbitInfo;
                return new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "Dispute data retrieved successfully", data = objDisputeDetail };
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }

        }

        /// <summary>
        /// Method to update dispute data's
        /// </summary>
        /// <param name="disputeDetail"> disputeDetail input</param>
        /// <returns>Success message if recors updated else respective error message</returns>
        private async Task<APIResponse> updateDisputeDetail(DisputeDetail disputeDetail, DateTime? briefAssignedDate)
        {

            try
            {
                // Get current logged in user from the context for log purpose
                var user = GetCurrentUser();

                if (user == null)
                {
                    return new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Unauthorized, message = "No active User context!", data = null };
                }


                DisputeMaster? objDisputeMaster = new DisputeMaster();

                objDisputeMaster = (from dispMaster in _idr_context.DisputeMaster
                                    where dispMaster.Id == disputeDetail.Id
                                    select dispMaster).SingleOrDefault();

                List<DisputeCPT> disputeCPTList_Before = new List<DisputeCPT>();

                disputeCPTList_Before = (from dispcpt in _idr_context.DisputeCPT
                                         where dispcpt.DisputeNumber == disputeDetail.DisputeNumber
                                         select dispcpt).ToList();



                if (objDisputeMaster == null)
                {
                    return new APIResponse() { statusCode = (int)DataConstants.StatusCodes.NotFound, message = "Record not found", data = null };
                }

                // Create log information
                DisputeLog disputeMasterLog = new DisputeLog();
                disputeMasterLog.PreviousValue = objDisputeMaster.ToJson();
                disputeMasterLog.Activity = "Dispute page";
                disputeMasterLog.TableName = "DisputeMaster";
                disputeMasterLog.CreatedDate = Utilities.GetCurrentUtcDate();
                disputeMasterLog.CreatedBy = user?.Result?.Email;
                disputeMasterLog.TransactionID = objDisputeMaster.Id;

                objDisputeMaster.DisputeStatus = disputeDetail?.DisputeStatus;
                objDisputeMaster.DisputeWorkFlowStatus = disputeDetail?.DisputeWorkFlowStatus;
                objDisputeMaster.Customer = disputeDetail?.Customer;
                objDisputeMaster.Entity = disputeDetail?.Entity;
                objDisputeMaster.EntityNPI = disputeDetail?.EntityNPI;
                objDisputeMaster.Payor = disputeDetail?.Payor;
                objDisputeMaster.CertifiedEntity = disputeDetail?.CertifiedEntity;
                objDisputeMaster.Comments = disputeDetail?.Comments;
                objDisputeMaster.ServiceLine = disputeDetail?.ServiceLine;
                objDisputeMaster.SubmissionDate = disputeDetail?.SubmissionDate;
                objDisputeMaster.IDRESelectionDate = disputeDetail?.IDRESelectionDate;
                objDisputeMaster.FormalReceivedDate = disputeDetail?.FormalReceivedDate;
                objDisputeMaster.AwardDate = disputeDetail?.AwardDate;
                objDisputeMaster.FeeRequestDate = disputeDetail?.FeeRequestDate;
                objDisputeMaster.FeeDueDate = disputeDetail?.FeeDueDate;
                objDisputeMaster.FeeAmountAdmin = disputeDetail?.FeeAmountAdmin;
                objDisputeMaster.FeeAmountEntity = disputeDetail?.FeeAmountEntity;
                objDisputeMaster.FeeAmountTotal = disputeDetail?.FeeAmountAdmin + disputeDetail?.FeeAmountEntity;
                objDisputeMaster.FeeInvoiceLink = disputeDetail?.FeeInvoiceLink;
                objDisputeMaster.FeePaidDate = disputeDetail?.FeePaidDate;
                objDisputeMaster.FeePaidAmount = disputeDetail?.FeePaidAmount;
                objDisputeMaster.BriefDueDate = disputeDetail?.BriefDueDate;

                if (disputeDetail?.BriefApprover != objDisputeMaster.BriefApprover)
                {
                    objDisputeMaster.BriefApprover = disputeDetail?.BriefApprover;
                    objDisputeMaster.BriefAssignedDate = Utilities.GetCurrentUtcDate();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    disputeDetail.BriefAssignedDate = objDisputeMaster.BriefAssignedDate;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }

                objDisputeMaster.BriefSubmissionLink = disputeDetail?.BriefSubmissionLink;
                objDisputeMaster.CreatedOn = disputeDetail?.CreatedOn;
                objDisputeMaster.CreatedBy = disputeDetail?.CreatedBy;
                objDisputeMaster.UpdatedOn = disputeDetail?.UpdatedOn;
                objDisputeMaster.UpdatedBy = disputeDetail?.UpdatedBy;

                _idr_context.DisputeMaster.Update(objDisputeMaster);
                disputeMasterLog.NewValue = objDisputeMaster.ToJson();

                if (disputeDetail?.DisputeCPTs?.Count > 0)
                {
                    _idr_context.ChangeTracker.AutoDetectChangesEnabled = false;
                    _idr_context.DisputeCPT.RemoveRange(disputeCPTList_Before);
                    _idr_context.DisputeCPT.UpdateRange(disputeDetail?.DisputeCPTs!);
                }

                await _idr_context.SaveChangesAsync();

                List<DisputeCPT> disputeCPTList_After = new List<DisputeCPT>();

                disputeCPTList_After = await (from dispcpt in _idr_context.DisputeCPT
                                              where dispcpt.DisputeNumber == objDisputeMaster.DisputeNumber
                                              select dispcpt).ToListAsync();

                var changes = disputeCPTList_Before.DeeplyEquals(disputeCPTList_After);

                if (changes.Count() > 0)
                {
                    // Create disputeCPT levellog information
                    DisputeLog disputeCPTLog = new DisputeLog();
                    disputeCPTLog.PreviousValue = disputeCPTList_Before.ToJson();
                    disputeCPTLog.NewValue = disputeCPTList_After.ToJson();
                    disputeCPTLog.Activity = "Dispute page";
                    disputeCPTLog.TableName = "DisputeCPT";
                    disputeCPTLog.CreatedDate = Utilities.GetCurrentUtcDate();
                    disputeCPTLog.CreatedBy = user?.Result?.Email;
                    disputeCPTLog.TransactionID = objDisputeMaster.Id;
                    _idr_context.XLog_ChangeLog.Add(disputeCPTLog);
                    await _idr_context.SaveChangesAsync();
                }
                _idr_context.XLog_ChangeLog.Add(disputeMasterLog);
                await _idr_context.SaveChangesAsync();
                return new APIResponse() { statusCode = (int)DataConstants.StatusCodes.Success, message = "Dispute has been updated successfully", data = disputeDetail };
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }

        }

    }
}
