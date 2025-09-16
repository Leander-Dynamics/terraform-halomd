using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MPExternalDisputeAPI.Model;
using MPExternalDisputeAPI.Utility;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json;

internal class Program
{
    private static void Main(string[] args)
    {
        const string corsPolicyName = "CorsPolicy";

        var builder = WebApplication.CreateBuilder(args);
        SecureConfigurationHelper.ConfigureKeyVault(builder);
        // The following line enables Application Insights telemetry collection.
//        builder.Services.AddApplicationInsightsTelemetry();
        var configuration = builder.Configuration;
        // increase file upload size to handle the benchmark files
        // https://bartwullems.blogspot.com/2022/01/aspnet-core-configure-file-upload-size.html
        // https://stackoverflow.com/questions/38698350/increase-upload-file-size-in-asp-net-core

        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateOnlyConverter());

        builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = int.MaxValue);
        builder.Services.Configure<FormOptions>(o =>
        {
            o.ValueLengthLimit = int.MaxValue;
            o.MultipartBodyLengthLimit = int.MaxValue;
            o.MultipartBoundaryLengthLimit = int.MaxValue;
            o.MultipartHeadersCountLimit = int.MaxValue;
            o.MultipartHeadersLengthLimit = int.MaxValue;
            o.BufferBodyLengthLimit = int.MaxValue;
            o.BufferBody = true;
            o.ValueCountLimit = int.MaxValue;
        });
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(corsPolicyName, policy =>
            {
                policy.AllowAnyOrigin();
                policy.AllowAnyMethod();
                policy.AllowAnyHeader();
            });
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Arbit API Documentation", Version = "v1" });
            opt.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Name = "X-API-KEY",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "ApiKeyScheme",
                Description = "Enter API Key",
            });

            opt.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                },
               new string[]{}
            }
            });
            // Set the comments path for the Swagger JSON and UI.
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            opt.IncludeXmlComments(xmlPath);
        });

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Limits.MaxRequestBodySize = 100_000_000;
        });

        // Add services to the container.
        //Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

       // builder.Services.AddMicrosoftIdentityWebAppAuthentication(configuration);

        // Add API key authentication
        builder.Services.AddAuthentication("ApiKey").AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);


        builder.Services.AddControllersWithViews();
        //    .AddJsonOptions(c => c.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve);

        builder.Services.AddHttpContextAccessor();
        //builder.Services.AddTransient<IPrincipal>(provider => provider.GetService<IHttpContextAccessor>().HttpContext?.User);
        builder.Services.AddTransient<IPrincipal>(provider =>
        {
            var httpContextAccessor = provider.GetService<IHttpContextAccessor>();
            if (httpContextAccessor != null && httpContextAccessor.HttpContext != null)
            {
                return httpContextAccessor.HttpContext.User;
            }
            return new ClaimsPrincipal(new ClaimsIdentity()); // Provide a default value if needed
        });

        // Best article on configuring Angular SPA on the entire WWW ;)
        // https://roma-rathi17.medium.com/msal2-0-errors-and-their-resolution-9a776e254a2c
        // This was a good one, too
        // https://github.com/AzureAD/microsoft-identity-web/issues/549

        builder.Services.AddTransient<IImportDataSynchronizer, ImportDataSynchronizer>();

        var arbitrationConnectionString = SecureConfigurationHelper.GetRequiredSqlConnectionString(configuration, "ConnStr");
        builder.Services.AddDbContext<ArbitrationDbContext>(options =>
        {
            options.UseSqlServer(arbitrationConnectionString, sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(60),
                    errorNumbersToAdd: null);
            });
        });

        var idrConnectionString = SecureConfigurationHelper.GetRequiredSqlConnectionString(configuration, "IDRConnStr");
        builder.Services.AddDbContext<DisputeIdrDbContext>(options =>
        {
            options.UseSqlServer(idrConnectionString, sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(60),
                    errorNumbersToAdd: null);
            });
        });

        SecureConfigurationHelper.EnsureApiKeyConfigured(configuration);

        //------------------- BUILD AND USE MIDDLEWARE ----------------------------------------------
        var app = builder.Build();
        if (!app.Environment.IsProduction())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.MapSwagger().RequireAuthorization();
        app.UseCors(corsPolicyName);
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        
        app.UseAuthorization();
        // NOTE: If you come here because a recently-added controller is 
        // returning a 404 Not Found error, check your proxy.conf.js configuration!
        // Service endpoints have to be explicitly-configured in the proxy
        // because Microsoft is stupid that way :)
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller}/{action=Index}/{id?}");

        app.MapFallbackToFile("index.html");
        
        app.Run();
    }
}
