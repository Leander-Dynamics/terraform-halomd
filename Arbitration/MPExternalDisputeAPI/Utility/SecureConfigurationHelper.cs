using System;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MPExternalDisputeAPI.Utility
{
    internal static class SecureConfigurationHelper
    {
        public static void ConfigureKeyVault(WebApplicationBuilder builder)
        {
            var keyVaultUriValue = builder.Configuration["KeyVault:Uri"];
            if (string.IsNullOrWhiteSpace(keyVaultUriValue))
            {
                return;
            }

            if (!Uri.TryCreate(keyVaultUriValue, UriKind.Absolute, out var keyVaultUri))
            {
                throw new InvalidOperationException("The KeyVault:Uri configuration value is not a valid absolute URI.");
            }

            builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
        }

        public static string GetRequiredSqlConnectionString(IConfiguration configuration, string connectionName)
        {
            var connectionString = configuration.GetConnectionString(connectionName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"The connection string '{connectionName}' was not found. Provide it via Azure Key Vault or environment variables.");
            }

            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                Encrypt = true,
                TrustServerCertificate = false,
            };

            return connectionStringBuilder.ConnectionString;
        }

        public static void EnsureApiKeyConfigured(IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration["ApiKey"]))
            {
                throw new InvalidOperationException("An API key was not configured. Provide the value via Azure Key Vault or environment variables.");
            }
        }
    }
}
