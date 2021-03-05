using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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


        private static ILogger RebootLoggerWithDatabaseLoggingIncluded(IConfiguration originalConfig, Dictionary<string, string> runtimeConfig)
        {
            var sinkOptions = new MSSqlServerSinkOptions
            {
                TableName = "Seriloglogs",
                SchemaName = "dbo",
                AutoCreateSqlTable = false,
                BatchPeriod = TimeSpan.FromSeconds(1),
                BatchPostingLimit = 50,
                EagerlyEmitFirstEvent = true,
                UseAzureManagedIdentity = false,
                AzureServiceTokenProviderResource = null
            };

            var columnMappings = new ColumnOptions();

            return new LoggerConfiguration()
                .ReadFrom.Configuration(originalConfig)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("LegacyEventTypeId",1)
                .Enrich.WithProperty("LegacyEventCategoryName","Standard")
                .Enrich.WithProperty("LegacyRegisteredAppId", originalConfig["DatabaseLoggerIdentifier"])
                .WriteTo.Debug()
                .WriteTo.MSSqlServer(runtimeConfig[MagicValues.LoggingConnectionStringName], sinkOptions: sinkOptions, columnOptions: columnMappings, restrictedToMinimumLevel: LogEventLevel.Verbose)
                .CreateLogger();
        }
    }
}
