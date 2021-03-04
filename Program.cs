using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ExactReproduction
{
    public class Program
    {
        private static string configFilePath = "appsettings.json";

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configFilePath, optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        public static void Main(string[] args)
        {
            var defaultLogging = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .WriteTo.Console();

            try
            {
                if (File.Exists(configFilePath))
                {
                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Debug()
                        .CreateLogger();
                }
                else
                {
                    Log.Logger = defaultLogging.CreateLogger();
                }

                Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));

                Log.Logger.Information("Begin - Loading runtime configuration into the application");

                var environmentSpecificValues = SecretsReader.GetConnectionStrings(new Dictionary<string, string>(), new SerilogLoggerFactory(Log.Logger).CreateLogger("SecretsReader"));

                environmentSpecificValues = RuntimeTimeSettings.UpdateConfigurationFromDatabase(environmentSpecificValues, new SerilogLoggerFactory(Log.Logger).CreateLogger("RuntimeSettings"));

                Log.Logger.Information("End - Loading runtime configuration into the application");

                Log.Logger = RebootLoggerWithDatabaseLoggingIncluded(Configuration, environmentSpecificValues);

                CreateHostBuilder(args, environmentSpecificValues).Build().Run();
            }
            catch (Exception ex)
            {

                Log.Fatal("The app had an error and has to shutdown: {ex}", ex);
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }

        public static IHostBuilder CreateHostBuilder(string[] args, Dictionary<string,string> additionalValues) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureAppConfiguration((hostingContext, config) => 
                {
                    config.AddInMemoryCollection(additionalValues);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });


        private static Serilog.ILogger RebootLoggerWithDatabaseLoggingIncluded(IConfiguration configuration, Dictionary<string, string> environmentSpecificValues)
        {
            throw new NotImplementedException();
        }
    }
}
