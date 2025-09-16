using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MPExternalDisputeAPI.Model;
using MPExternalDisputeAPI.Utility;
using System;
using System.Security.Principal;

// See https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-desktop-acquire-token-username-password?tabs=dotnet
// for information regarding token generation in case we want to support API access outside of this application scope
namespace MPExternalDisputeAPI.Controllers
{
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class DisputeController : MPBaseController
    {
        private readonly ILogger<DisputeController> _logger;
        private readonly IImportDataSynchronizer _synchronizer;
        private readonly ArbitrationDbContext _errorContext;
        private readonly DisputeIdrDbContext _idr_context;
        public readonly IPrincipal _principle;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="idr_context"></param>
        /// <param name="configuration"></param>
        /// <param name="synchronizer"></param>
        /// <param name="principal"></param>
        public DisputeController(ILogger<DisputeController> logger, ArbitrationDbContext context, DisputeIdrDbContext idr_context, IConfiguration configuration, IImportDataSynchronizer synchronizer, IPrincipal principal) : base(context, configuration)
        {
            //_memoryCache = cache;
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
        /// Action to get past 60 days dispute data's by customer name with search filter options
        /// </summary>
        /// <param name="searchInput"> Search filter input</param>
        /// <param name="paginationFilter"> Paging information</param>
        /// <returns>List of dispute data's with paging informations</returns>
        [HttpGet("GetDisputeData")]
        public async Task<IActionResult> GetDisputeData([FromQuery] SearchInput searchInput, [FromQuery] PaginationFilter paginationFilter)
        {

            var response = await GetDisputeResponse(searchInput, paginationFilter);
            return Ok(response);
        }

        /// <summary>
        /// Method to get past 60 days dispute data's by customer name with search filter options
        /// </summary>
        /// <param name="searchInput"> Search filter input</param>
        /// <param name="paginationFilter"> Paging information</param>
        /// <returns>List of dispute data's with paging informations</returns>
        private async Task<PagedResponse<DisputeData>> GetDisputeResponse(SearchInput searchInput, PaginationFilter paginationFilter)
        {
            List<DisputeData> disputeDatas = new List<DisputeData>();
            List<ArbitrationCase> arbitCases = new List<ArbitrationCase>();

            try
            {
                // During testing Impact / Fund should only see two customers MPower and AUDX as per email received from 
                // John (Email Subject: "Question regarding External Get APIs") on 07/24 at 12:14 pm CST.
                // Once in production remove this check
                if (!string.IsNullOrEmpty(searchInput.CustomerName) &&
                    (searchInput.CustomerName.ToUpper().Contains("MPOWER") || searchInput.CustomerName.ToUpper().Contains("AUDX")))
                {
                    var dateRangeStart = DateTime.UtcNow.AddDays(-60);
                    var dateRangeEnd = DateTime.UtcNow;

                    IQueryable<DisputeMaster> disputeMasters = _idr_context.DisputeMaster
                        .Where(disp => !string.IsNullOrEmpty(disp.Customer)
                                       && disp.Customer.Trim().ToUpper() == searchInput.CustomerName.Trim().ToUpper()
                                       && disp.UpdatedOn.Value >= dateRangeStart
                                       && disp.UpdatedOn.Value <= dateRangeEnd);

                    if (!string.IsNullOrEmpty(searchInput.DisputeStatus))
                    {
                        var disputeStatus = searchInput.DisputeStatus.Trim().ToUpper();
                        disputeMasters = disputeMasters.Where(disp => !string.IsNullOrEmpty(disp.DisputeStatus)
                                                   && disp.DisputeStatus.Trim().ToUpper() == disputeStatus);
                    }
                    if (!string.IsNullOrEmpty(searchInput.ProviderNpi))
                    {
                        var providerNpi = searchInput.ProviderNpi.Trim().ToUpper();
                        disputeMasters = disputeMasters.Where(disp => !string.IsNullOrEmpty(disp.EntityNPI)
                                                   && disp.EntityNPI.Trim().ToUpper() == providerNpi);
                    }
                    if (searchInput.InitiationDate.HasValue)
                    {
                        var initiationDate = searchInput.InitiationDate.Value.Date;
                        disputeMasters = disputeMasters.Where(disp => disp.SubmissionDate.HasValue
                                                                      && disp.SubmissionDate.Value.Date == initiationDate);
                    }

                    // Take total after applying search before applying page filter
                    var totalRecords = disputeMasters.Count();

                    // Fetch all necessary data into memory
                    var disputeMasterList = disputeMasters.Take(200).AsNoTracking().ToList();

                    if (!string.IsNullOrEmpty(searchInput.OperatingState))
                    {
                        var operatingStates = searchInput.OperatingState.Trim().ToUpper();
                        arbitCases = _context.ArbitrationCases.AsNoTracking()
                                             .Where(arbit => !string.IsNullOrEmpty(arbit.Authority)
                                                    && arbit.Authority.Trim().ToUpper() == operatingStates).ToList();
                    }
                    else
                    {
                        arbitCases = _context.ArbitrationCases.AsNoTracking().ToList();
                    }

                    var customerName = searchInput.CustomerName;
                    if (!string.IsNullOrEmpty(customerName) && arbitCases.Any())
                    {
                        var customers = _context.Customers.Where(cust => cust.Name == customerName &&
                                                       cust.AgreementStartDate <= DateTime.Now &&
                                                       cust.AgreementEndDate >= DateTime.Now &&
                                                       cust.ExternalPartnerName == "FUND").AsNoTracking().ToList();

                        arbitCases = arbitCases.Join(customers, arbit => arbit.Customer, cust => cust.Name, (arbit, cust) => arbit).ToList();
                    }


                    var disputeCPTs = _idr_context.DisputeCPT.AsNoTracking().ToList();
                    var certifiedEntities = _idr_context.REF_CertifiedEntity.AsNoTracking().ToList();
                    var emailRequests = _idr_context.RPT_EmailedFeePaymentRequests.AsNoTracking().ToList();

                    // Perform the join in memory
                    disputeDatas = disputeMasterList.Select(dispMaster => new DisputeData
                    {
                        Number = dispMaster.DisputeNumber,
                        Status = dispMaster.DisputeStatus,
                        CustomerName = dispMaster.Customer,
                        Entity = dispMaster.Entity,
                        EntityNPI = dispMaster.EntityNPI,
                        Payor = dispMaster.Payor,
                        InitiationDate = dispMaster.SubmissionDate,
                        IDRE = new IDRE
                        {
                            IDRESelectionDate = dispMaster.IDRESelectionDate,
                            CertifiedEntity = dispMaster.CertifiedEntity,
                            CertifiedEntityId = certifiedEntities
                            .Where(entity => entity.CertifiedEntityName == dispMaster.CertifiedEntity)
                            .Select(e => e.Id)
                            .SingleOrDefault(),

                        },
                        Fee = new Fee
                        {
                            FeeRequestDate = dispMaster.FeeRequestDate,
                            FeeDueDate = dispMaster.FeeDueDate,
                            FeePaidDate = dispMaster.FeePaidDate,
                            FeeInvoiceLink = dispMaster.FeeInvoiceLink,

                            FeeEmailBody = emailRequests
                                        .Where(email => email.DisputeNumber == dispMaster.DisputeNumber)
                                        .OrderByDescending(email => email.Emaildate).Select(e => e.FromEmail).FirstOrDefault(),

                            AdminFeeAmount = dispMaster.FeeAmountAdmin,
                            EntityFeeAmount = dispMaster.FeeAmountEntity,
                            TotalFeeAmount = dispMaster.FeeAmountTotal
                        },
                        Award = new Award
                        {
                            AwardDate = dispMaster.AwardDate,
                            TotalAwardAmount = disputeCPTs
                                .Where(dispCpt => dispCpt.DisputeNumber == dispMaster.DisputeNumber)
                                .Sum(dispCpt => dispCpt.AwardAmount),
                        },
                        DisputeDetailList = disputeCPTs
                            .Where(dispCpt => dispCpt.DisputeNumber == dispMaster.DisputeNumber)
                            .Join(arbitCases, dispCpt => dispCpt.ArbitID, arbitCase => arbitCase.Id,
                            (dispCpt, arbitCase) => new DisputeDetail
                            {
                                Determination = "TBD",
                                ArbitId = dispCpt.ArbitID,
                                CPTCode = dispCpt.CPTCode,
                                ProviderOfferAmount = (dispCpt.ProviderOfferAmount == null || dispCpt.ProviderOfferAmount == 0)
                                    && (dispCpt.BenchmarkAmount != null && dispCpt.BenchmarkAmount > 0)
                                    ? dispCpt.BenchmarkAmount
                                    : (int)Math.Floor(1000 + 2001 * new Random().NextDouble()),
                                PayorOfferAmount = dispCpt.PayorOfferAmount,
                                AwardAmount = dispCpt.AwardAmount,
                                PrevailingParty = dispCpt.PrevailingParty,
                                PrevailingAmount = -1,
                                NonPrevailingParty = "TBD",
//                                BenchmarkAmount = dispCpt.BenchmarkAmount,
                                EHRNumber = arbitCase.EHRNumber,
                                PayorClaimNumber = arbitCase.PayorClaimNumber,
                                ProviderType = arbitCase.ProviderType,
                                ProviderName = arbitCase.ProviderName,
                                PatientName = arbitCase.PatientName,
                                ServiceDate = arbitCase.ServiceDate,
                                OperatingState = arbitCase.Authority.ToUpper(),
                                
                            }).ToList(),
                        BriefDueDate = null,
                        Refund = null,
                    }).ToList();

                    // Apply page filters
                    if (paginationFilter != null && disputeMasters != null)
                    {

                        disputeMasters = disputeMasters.Skip(paginationFilter.PageNumber * paginationFilter.PageSize)
                                                        .Take(paginationFilter.PageSize);
                    }

                    if (paginationFilter == null || paginationFilter.PageSize < 1 || paginationFilter.PageSize > 5)
                    {
                        return new PagedResponse<DisputeData>(disputeDatas);
                    }
                    else
                    {
                        return PaginationHelper.CreatePagedResponse(paginationFilter, disputeDatas, totalRecords);

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return new PagedResponse<DisputeData>(disputeDatas);
        }
    }
}

#region commented code
/*
 * disputeMasters = disputeMasters.Take(200).AsNoTracking();
var arbitCases = _context.ArbitrationCases.AsNoTracking().ToList();
var disputeCPTs = _idr_context.DisputeCPT.Take(50).AsNoTracking().ToList();

disputeDatas = (from dispMaster in disputeMasters
                                  select new DisputeData
                                  {
                                      DisputeNumber = dispMaster.DisputeNumber,
                                      CustomerName = dispMaster.Customer,
                                      Entity = dispMaster.Entity,
                                      EntityNPI = dispMaster.EntityNPI,
                                      Payor = dispMaster.Payor,
                                      InitiationDate = dispMaster.SubmissionDate,
                                      FeePaidDate = dispMaster.FeePaidDate,
                                      AwardDate = dispMaster.AwardDate,
                                      DisputeDetail = (from dispCpt in disputeCPTs
                                                        join arbitCase in arbitCases
                                                        on dispCpt.ArbitID equals arbitCase.Id
                                                        where dispCpt.DisputeNumber == dispMaster.DisputeNumber
                                                        select new DisputeDetail
                                                        {
                                                            ArbitId = dispCpt.ArbitID,
                                                            CPTCode = dispCpt.CPTCode,
                                                            AwardAmount = dispCpt.AwardAmount,
                                                            BenchmarkAmount = dispCpt.BenchmarkAmount,
                                                            PrevailingParty = dispCpt.PrevailingParty,
                                                            PayorOfferAmount = dispCpt.PayorOfferAmount,
                                                            ProviderOfferAmount = dispCpt.ProviderOfferAmount,
                                                            EHRNumber = arbitCase.EHRNumber,
                                                            ServiceDate = arbitCase.ServiceDate
                                                        }).ToList(),
                                      CertifiedEntityName = dispMaster.CertifiedEntity,
                                      CertifiedEntityId = _idr_context.REF_CertifiedEntity
                                                            .Where(entity => entity.CertifiedEntityName == dispMaster.CertifiedEntity)
                                                            .Select(e => e.Id).SingleOrDefault(),
                                      DateSelected = dispMaster.IDRESelectionDate,
                                      FeeRequestDate = dispMaster.FeeRequestDate,
                                      FeeDueDate = dispMaster.FeeDueDate,
                                      FeeAmountAdmin = dispMaster.FeeAmountAdmin,
                                      FeeAmountEntity = dispMaster.FeeAmountEntity,
                                      FeeAmountTotal = dispMaster.FeeAmountTotal,
                                      FeeInvoiceLink = dispMaster.FeeInvoiceLink,
                                      OriginalEmailBody = _idr_context.RPT_EmailedFeePaymentRequests
                                                            .Where(email => email.DisputeNumber == dispMaster.DisputeNumber)
                                                            .OrderByDescending(email => email.Emaildate)
                                                            .Select(e => e.FromEmail).FirstOrDefault()
                                      // RefundAmount =Convert.Todecimal(0)
                                  }).ToList();
 * */
#endregion  commented code
