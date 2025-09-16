using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MPExternalDisputeAPI.Utility
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _configuration;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IConfiguration configuration,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            _configuration = configuration;
        }

        /// <summary>
        ///  Handler for API key authendication
        /// </summary>
        /// <returns> valid token for successful validations else respective error message</returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // API key validations
            var apiKey = Request.Headers["X-API-KEY"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("API Key was not provided."));
            }

            // Validate the API key here (e.g., check it against a database or configuration)
            if (apiKey != _configuration.GetValue<string>("ApiKey"))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API Key provided."));
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "API Key User") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

