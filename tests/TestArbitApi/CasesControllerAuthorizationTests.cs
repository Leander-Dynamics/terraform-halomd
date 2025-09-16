using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MPArbitration.Controllers;
using MPArbitration.Model;
using Xunit;

namespace TestArbitApi
{
    public class CasesControllerAuthorizationTests
    {
        [Fact]
        public async Task GetCaseArchivesAsync_WithAuthorizedUser_ReturnsArchives()
        {
            using var context = CreateContext();
            var caseId = SeedCaseData(context);
            var user = SeedUser(context, "allowed@example.com", "c|1|manager");
            var controller = CreateController(context, user.Email);

            var result = await controller.GetCaseArchivesAsync(caseId);

            var archives = Assert.IsAssignableFrom<IEnumerable<CaseArchive>>(result.Value);
            var archive = Assert.Single(archives);
            Assert.Equal(caseId, archive.ArbitrationCaseId);
        }

        [Fact]
        public async Task GetCaseArchivesAsync_WithUnauthorizedUser_ReturnsUnauthorized()
        {
            using var context = CreateContext();
            var caseId = SeedCaseData(context);
            var user = SeedUser(context, "unauthorized@example.com", "c|2|manager");
            var controller = CreateController(context, user.Email);

            var result = await controller.GetCaseArchivesAsync(caseId);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Insufficient granular privileges for current user context", unauthorized.Value);
        }

        private static ArbitrationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ArbitrationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ArbitrationDbContext(options);
        }

        private static int SeedCaseData(ArbitrationDbContext context)
        {
            var allowedCustomer = new Customer { Id = 1, Name = "Allowed Customer" };
            var otherCustomer = new Customer { Id = 2, Name = "Other Customer" };
            context.Customers.AddRange(allowedCustomer, otherCustomer);

            var caseRecord = new ArbitrationCase
            {
                Authority = "nsa",
                Customer = allowedCustomer.Name,
                CreatedBy = "seed"
            };

            context.ArbitrationCases.Add(caseRecord);
            context.SaveChanges();

            context.CaseArchives.Add(new CaseArchive
            {
                ArbitrationCaseId = caseRecord.Id,
                AuthorityId = 10,
                AuthorityCaseId = "A1",
                CreatedBy = "seed"
            });

            context.SaveChanges();

            return caseRecord.Id;
        }

        private static AppUser SeedUser(ArbitrationDbContext context, string email, string roles)
        {
            var user = new AppUser
            {
                Email = email,
                IsActive = true,
                Roles = roles
            };

            context.AppUsers.Add(user);
            context.SaveChanges();

            return user;
        }

        private static CasesController CreateController(ArbitrationDbContext context, string email)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:Connection"] = "UseDevelopmentStorage=true",
                    ["Storage:Container"] = "unit-tests"
                })
                .Build();

            var controller = new CasesController(
                NullLogger<CasesController>.Instance,
                context,
                configuration,
                new NoopImportDataSynchronizer());

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, email) }, "TestAuthType");
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        private sealed class NoopImportDataSynchronizer : IImportDataSynchronizer
        {
            public List<Authority> Authorities { get; set; } = new();
            public List<Customer> Customers { get; set; } = new();
            public List<Payor> Payors { get; set; } = new();
            public List<ProcedureCode> ProcedureCodes { get; set; } = new();

            public Task<string> ArchiveCaseAsync(ArbitrationCase orig, AppUser user, Authority? au = null, bool resetOrig = true, bool saveInstantly = false)
                => Task.FromResult(string.Empty);

            public Task<ArchiveCaseResult> ArchiveIfNecessaryAsync(IAuthorityCase newArbCase, ArbitrationCase orig, AppUser runAs)
                => Task.FromResult(new ArchiveCaseResult());

            public Task BatchQueueNotificationsAsync(IEnumerable<Notification> Notifications, AppUser User, string FullUserName)
                => Task.CompletedTask;

            public Task EnsureAuthorities() => Task.CompletedTask;

            public Task EnsureCalculatorVariables() => Task.CompletedTask;

            public Task EnsureCustomers() => Task.CompletedTask;

            public Task EnsurePayors(bool ExcludeJSON = true) => Task.CompletedTask;

            public Task EnsureProcedureCodes() => Task.CompletedTask;

            public void ImportAuthorityCases(Authority authority, IEnumerable<string> upload, AppUser initiator, JobQueueItem? job)
            {
            }

            public void ImportBenchmarks(int benchmarkId, string username, string upload, JobQueueItem? job)
            {
            }

            public void ImportDisputeDetailsAsync(IEnumerable<AuthorityDisputeDetailsCSV> Records, AppUser CurrentUser, JobQueueItem? CurrentJob)
            {
            }

            public void ImportDisputeFeesAsync(IEnumerable<AuthorityDisputeFeeCSV> Records, AppUser CurrentUser, JobQueueItem? CurrentJob)
            {
            }

            public void ImportDisputeHeadersAsync(IEnumerable<AuthorityDisputeCSV> records, AppUser runAs, JobQueueItem? job)
            {
            }

            public void ImportDisputeNotesAsync(IEnumerable<AuthorityDisputeNoteCSV> HeaderRecords, AppUser CurrentUser, JobQueueItem? CurrentJob)
            {
            }

            public void ImportEHR(IEnumerable<string> upload, EHRRecordType recordType, AppUser runAs, JobQueueItem? job)
            {
            }

            public void ImportIDRDisputeDetailsAsync(IEnumerable<DisputeCPT> disputeCPT, AppUser currentUser, JobQueueItem? CurrentJob)
            {
            }

            public void RecalculateAuthorityDates(DbContextOptions<ArbitrationDbContext> contextOptions, int jobId, AppUser user, Authority nsa, bool activeOnly)
            {
            }

            public Task<string> SaveUploadLog(string docType, string updatedBy, DateTime updatedOn, string log)
                => Task.FromResult(string.Empty);

            public void SyncTDIsToCases(int authorityId, List<TDIRequestDetails> TDIRequests, JobQueueItem? job)
            {
            }

            public Task<string> ValidateArbitrationCase(ArbitrationCase caseRecord, bool skipDOBCheck, Authority nsa, Authority? au, bool isUpdating, AppUser? caller, bool calledByImport = true)
                => Task.FromResult(string.Empty);
        }
    }
}
