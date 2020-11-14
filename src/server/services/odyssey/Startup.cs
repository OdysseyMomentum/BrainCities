using Autofac;
using Autofac.Extensions.DependencyInjection;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Serilog;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Threading.Tasks;
using Odyssey.API.Infrastructure;
using Odyssey.API.Infrastructure.Filters;
using Odyssey.API.Tasks;

namespace Odyssey.API
{
    public class Startup
    {
        public IWebHostEnvironment CurrentEnvironment { get; }
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Log.Logger = CreateSerilogLogger(configuration);
            CurrentEnvironment = env;
            BuildAppSettingsProvider();
        }

        private void BuildAppSettingsProvider()
        {
            AppSettingsProvider.IsDevelopment = CurrentEnvironment.IsDevelopment();
        }

        private static Serilog.ILogger CreateSerilogLogger(IConfiguration configuration)
        {
            var seqServerUrl = configuration["Serilog:SeqServerUrl"];
            var logstashUrl = configuration["Serilog:LogstashgUrl"];

            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithProperty("ApplicationContext", Program.AppName)
                .Enrich.WithProperty("Type", "Log")
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Seq(string.IsNullOrWhiteSpace(seqServerUrl) ? "http://seq" : seqServerUrl)
                .WriteTo.Http(string.IsNullOrWhiteSpace(logstashUrl) ? "http://logstash:8080" : logstashUrl)
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        [Obsolete]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddAppInsight(Configuration)
                .AddGrpc().Services
                .AddCustomMVC()
                .AddApplicationServices()
                .AddCustomAuthentication(Configuration)
                .AddCustomOptions(Configuration)
                .AddSwagger(Configuration)
                .AddCustomHealthCheck()
                .AddHostedService<QueueManagerService>();

            var container = new ContainerBuilder();
            container.Populate(services);

            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = CurrentEnvironment.IsDevelopment();

            return new AutofacServiceProvider(container.Build());
        }

        private void ConfigureAuthService(IApplicationBuilder services)
        {
            services.UseAuthentication();
            services.UseAuthorization();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            //Configure logs
            loggerFactory.AddSerilog();

            var pathBase = Configuration["PATH_BASE"];

            if (!string.IsNullOrEmpty(pathBase))
            {
                loggerFactory.CreateLogger<Startup>().LogDebug("Using PATH BASE '{pathBase}'", pathBase);
                app.UsePathBase(pathBase);
            }

            app.UseSwagger()
             .UseSwaggerUI(c =>
             {
                 c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "Odyssey.API V1");
                 c.OAuthClientId("odysseyswaggerui");
                 c.OAuthAppName("Odyssey Swagger UI");
             });

            app.UseRouting();
            app.UseCors("CorsPolicy");

            ConfigureAuthService(app);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapControllers();
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
                {
                    Predicate = r => r.Name.Contains("self")
                });
            });

            ConfigureEventBus(app);
        }

        protected virtual void ConfigureEventBus(IApplicationBuilder app)
        {
            //var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            //eventBus.Subscribe<UpdateReferenceIntegrationEvent, UpdateReferenceIntegrationEventHandler>();
        }
    }

    public static class CustomExtensionMethods
    {
        public static IServiceCollection AddAppInsight(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplicationInsightsTelemetry(configuration);
            services.AddApplicationInsightsKubernetesEnricher();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddTransient<HttpClientAuthorizationDelegatingHandler>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            //services.AddHttpClient<IExternalService, ExternalService>().AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>();

            return services;
        }

        public static IServiceCollection AddCustomMVC(this IServiceCollection services)
        {
            services
                .AddControllers(options =>
                {
                    options.Filters.Add(typeof(HttpGlobalExceptionFilter));
                })
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                    .SetIsOriginAllowed((host) => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // prevent from mapping "sub" claim to nameidentifier.
            //JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            var identityUrl = configuration.GetValue<string>("IdentityUrl");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Authority = identityUrl;
                options.RequireHttpsMetadata = false;
                options.Audience = "odyssey";
            });

            return services;
        }

        public static IServiceCollection AddCustomHealthCheck(this IServiceCollection services)
        {            
            var hcBuilder = services.AddHealthChecks();

            hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());

            return services;
        }


        public static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<OdysseySettings>(configuration);
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetails = new ValidationProblemDetails(context.ModelState)
                    {
                        Instance = context.HttpContext.Request.Path,
                        Status = StatusCodes.Status400BadRequest,
                        Detail = "Please refer to the errors property for additional details."
                    };

                    return new BadRequestObjectResult(problemDetails)
                    {
                        ContentTypes = { "application/problem+json", "application/problem+xml" }
                    };
                };
            });

            return services;
        }

        [Obsolete]
        public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.OperationFilter<SwaggerFileOperationFilter>();
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Braincities @ Odyssey Momentum HTTP API Service",
                    Version = "v1",
                    Description = "The Odyssey IoT Microservice"
                });

                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows()
                    {                       
                        ClientCredentials = new OpenApiOAuthFlow()
                        {
                            AuthorizationUrl = new Uri($"{configuration.GetValue<string>("IdentityUrlExternal")}/connect/authorize"),
                            TokenUrl = new Uri($"{configuration.GetValue<string>("IdentityUrlExternal")}/connect/token"),
                            Scopes = new Dictionary<string, string>()
                            {
                                { "odyssey", "Odyssey API" }
                            }
                        }
                    }
                });

                options.OperationFilter<AuthorizeCheckOperationFilter>();
            });

            return services;
        }       
    }
}
