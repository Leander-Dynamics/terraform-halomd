using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MPArbitration.Model;
using MPArbitration.Utility;
using System.Reflection;

namespace MPArbitration
{
    internal class Program
    {
        private const string CorsPolicyName = "CorsPolicy";

        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplicationInsightsTelemetry();

            var configuration = builder.Configuration;

            builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = int.MaxValue);
            builder.Services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;
                options.MultipartBoundaryLengthLimit = int.MaxValue;
                options.MultipartHeadersCountLimit = int.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue;
                options.BufferBody = true;
                options.BufferBodyLengthLimit = int.MaxValue;
                options.ValueCountLimit = int.MaxValue;
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "MP Arbitration API", Version = "v1" });
                options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Name = "X-API-KEY",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = ApiKeyAuthenticationOptions.DefaultScheme,
                    Description = "API key needed to access specific endpoints."
                });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            builder.Services.AddCors(cors =>
            {
                var allowedOrigins = GetConfiguredValues(configuration, "Cors:AllowedOrigins");
                var allowedMethods = GetConfiguredValues(configuration, "Cors:AllowedMethods");
                var allowedHeaders = GetConfiguredValues(configuration, "Cors:AllowedHeaders");

                cors.AddPolicy(CorsPolicyName, policy =>
                {
                    ConfigureCorsPolicy(policy, allowedOrigins, allowedMethods, allowedHeaders);
                    policy.WithExposedHeaders("Content-Disposition");
                });
            });

            builder.Services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });
            builder.Services.AddRazorPages();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<IPrincipal>(provider =>
            {
                var httpContext = provider.GetService<IHttpContextAccessor>();
                return httpContext?.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
            });

            builder.Services.AddMemoryCache();
            builder.Services.AddTransient<IImportDataSynchronizer, ImportDataSynchronizer>();

            var arbitrationConnectionString = configuration.GetConnectionString("ConnStr");
            if (!string.IsNullOrWhiteSpace(arbitrationConnectionString))
            {
                builder.Services.AddDbContext<ArbitrationDbContext>(options =>
                {
                    options.UseSqlServer(arbitrationConnectionString, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(60), null);
                    });
                });
            }

            var idrConnectionString = configuration.GetConnectionString("IDRConnStr");
            if (!string.IsNullOrWhiteSpace(idrConnectionString))
            {
                builder.Services.AddDbContext<DisputeIdrDbContext>(options =>
                {
                    options.UseSqlServer(idrConnectionString, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(60), null);
                    });
                });
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var instance = configuration["AzureAd:Instance"];
                var tenantId = configuration["AzureAd:TenantId"];
                if (!string.IsNullOrWhiteSpace(instance) && !string.IsNullOrWhiteSpace(tenantId))
                {
                    options.Authority = $"{instance}{tenantId}/v2.0";
                }

                options.Audience = configuration["AzureAd:Audience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Authority,
                    ValidAudience = configuration["AzureAd:Audience"],
                    NameClaimType = "preferred_username"
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.DefaultScheme, _ => { });

            builder.Services.AddAuthorization();

            CopyConfigurationToLegacySettings(configuration);

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors(CorsPolicyName);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller}/{action=Index}/{id?}");

            app.MapFallbackToFile("index.html");

            app.Run();
        }

        private static void CopyConfigurationToLegacySettings(IConfiguration configuration)
        {
            static void TrySet(string key, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    System.Configuration.ConfigurationManager.AppSettings.Set(key, value);
                }
            }

            TrySet("DEFAULT_PAYOR_JSON", configuration["DEFAULT_PAYOR_JSON"]);
            TrySet("DEFAULT_ENTITY_ADDRESS", configuration["DEFAULT_ENTITY_ADDRESS"]);
            TrySet("DEFAULT_ENTITY_CITY", configuration["DEFAULT_ENTITY_CITY"]);
            TrySet("DEFAULT_ENTITY_STATE", configuration["DEFAULT_ENTITY_STATE"]);
            TrySet("DEFAULT_ENTITY_ZIP", configuration["DEFAULT_ENTITY_ZIP"]);
            TrySet("SendGridApiKey", configuration["SendGridApiKey"]);
            TrySet("FromAddress", configuration["FromAddress"]);
            TrySet("ReplyToAddress", configuration["ReplyToAddress"]);
        }

        private static void ConfigureCorsPolicy(CorsPolicyBuilder policyBuilder, string[] allowedOrigins, string[] allowedMethods, string[] allowedHeaders)
        {
            if (allowedOrigins.Length == 0 || allowedOrigins.Any(origin => string.Equals(origin, "*", StringComparison.Ordinal)))
            {
                policyBuilder.AllowAnyOrigin();
            }
            else
            {
                policyBuilder.WithOrigins(allowedOrigins);
            }

            if (allowedMethods.Length == 0 || allowedMethods.Any(method => string.Equals(method, "*", StringComparison.Ordinal)))
            {
                policyBuilder.AllowAnyMethod();
            }
            else
            {
                policyBuilder.WithMethods(allowedMethods);
            }

            if (allowedHeaders.Length == 0 || allowedHeaders.Any(header => string.Equals(header, "*", StringComparison.Ordinal)))
            {
                policyBuilder.AllowAnyHeader();
            }
            else
            {
                policyBuilder.WithHeaders(allowedHeaders);
            }
        }

        private static string[] GetConfiguredValues(IConfiguration configuration, string key)
        {
            var section = configuration.GetSection(key);
            var values = section.Get<string[]>() ?? Array.Empty<string>();

            if (values.Length > 0)
            {
                values = values
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim())
                    .ToArray();
            }

            if (values.Length == 0 && !string.IsNullOrWhiteSpace(section.Value))
            {
                values = section.Value
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => value.Trim())
                    .ToArray();
            }

            return values;
        }
    }
}
