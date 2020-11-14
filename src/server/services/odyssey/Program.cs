using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Odyssey.API
{
    public class Program
    {
        public static readonly string Namespace = typeof(Program).Namespace;
        public static readonly string AppName = Namespace.Substring(Namespace.LastIndexOf('.', Namespace.LastIndexOf('.') - 1) + 1);

        [Obsolete]
        public static int Main(string[] args)
        {
            var configuration = GetConfiguration();

            Log.Logger = CreateSerilogLogger(configuration);

            try
            {
                Log.Information("Configuring web host ({ApplicationContext})...", AppName);
                var host = CreateHostBuilder(configuration, args);                

                using (var serviceScope = host.Services.CreateScope())
                {
                    var services = serviceScope.ServiceProvider;

                    try
                    {
                       
                        Log.Information("Seeding ({ApplicationContext})...", AppName);

                        var env = services.GetService<IWebHostEnvironment>();
                        var settings = services.GetService<IOptions<OdysseySettings>>();
                        var logger = services.GetService<ILogger<Seeder>>();
                        var loggerFactory = services.GetService<ILoggerFactory>();

                        new Seeder().SeedAsync().Wait();
                    }
                    catch (Exception ex)
                    {
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "An error occurred.");
                    }
                }               
                
                Log.Information("Starting web host ({ApplicationContext})...", AppName);
                host.Run();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Program terminated unexpectedly ({ApplicationContext})!", AppName);
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        [Obsolete]
        private static IWebHost CreateHostBuilder(IConfiguration configuration, string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(configuration)
                .CaptureStartupErrors(false)
                .ConfigureKestrel(options =>
                {
                    var (httpPort, grpcPort) = GetDefinedPorts(configuration);
                    options.Listen(IPAddress.Any, httpPort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });
                    options.Listen(IPAddress.Any, grpcPort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });

                })
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .UseContentRoot(Directory.GetCurrentDirectory())              
                .UseSerilog()
                .Build();

        private static Serilog.ILogger CreateSerilogLogger(IConfiguration configuration)
        {
            var seqServerUrl = configuration["Serilog:SeqServerUrl"];
            var logstashUrl = configuration["Serilog:LogstashgUrl"];
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithProperty("ApplicationContext", AppName)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Seq(string.IsNullOrWhiteSpace(seqServerUrl) ? "http://seq" : seqServerUrl)
                .WriteTo.Http(string.IsNullOrWhiteSpace(logstashUrl) ? "http://logstash:8080" : logstashUrl)
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        private static (int httpPort, int grpcPort) GetDefinedPorts(IConfiguration config)
        {
            var grpcPort = config.GetValue("GRPC_PORT", 81);
            var port = config.GetValue("PORT", 80);
            return (port, grpcPort);
        }

        private static IConfiguration GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }
    }
}
