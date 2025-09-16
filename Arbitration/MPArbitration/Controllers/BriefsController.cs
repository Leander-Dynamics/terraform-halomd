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
using MPArbitration.Utility;

namespace MPArbitration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BriefsController : MPBaseController
    {
        private readonly ILogger<BriefsController> _logger;
        private const string NSA_PENDING = "Pending NSA Negotiation Request";
        private const string NSA_SUBMITTED = "Submitted NSA Negotiation Request";

        public BriefsController(ILogger<BriefsController> logger, ArbitrationDbContext context, IConfiguration configuration) : base(context, configuration)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("assets/incomplete")]
        public async Task<ActionResult<IEnumerable<AppHealthDetail>>> GetClaimsNeedingAssetConstruction()
        {
            var user = await GetCurrentUser();
            if (user == null)
                return Unauthorized("No active User context!");

            if (!user.IsSystem && !user.HasGlobalCaseRole)
                return Unauthorized("Insufficient privileges for current user context");

            var Q = _context.ArbitrationCases.Where(d => !d.IsDeleted && (d.NSAWorkflowStatus == ArbitrationStatus.ActiveArbitrationBriefNeeded || d.Status == ArbitrationStatus.ActiveArbitrationBriefNeeded));
            return await Q.Include(g => g.PayorEntity).Select(d => new AppHealthDetail { Id = d.Id, NSARequestEmail = (d.PayorEntity == null ? "": d.PayorEntity.NSARequestEmail), Payor = (d.PayorEntity == null ? "" : d.PayorEntity.Name), PayorClaimNumber = d.PayorClaimNumber, PatientName = d.PatientName, ProviderNPI = d.ProviderNPI, ServiceDate = d.ServiceDate }).ToArrayAsync();
        }
    }
}
