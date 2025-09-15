using Microsoft.AspNetCore.Authentication;

namespace MPExternalDisputeAPI.Model
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "ApiKey";
        public string Scheme => DefaultScheme;
        public string ApiKeyName { get; set; } = "X-API-KEY";
    }
}
