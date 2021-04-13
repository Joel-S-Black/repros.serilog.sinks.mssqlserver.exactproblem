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
                TableName = "logs",
                SchemaName = "dbo",
                AutoCreateSqlTable = false,
                BatchPeriod = TimeSpan.FromSeconds(1),
                BatchPostingLimit = 50,
                EagerlyEmitFirstEvent = true,
                UseAzureManagedIdentity = false,
                AzureServiceTokenProviderResource = null
            };

            var columnMappings = new ColumnOptions();

            // Clear the default columns
            columnMappings.Store.Clear();
            columnMappings.Store.Add(StandardColumn.TimeStamp);

            // Setup custom column list
            columnMappings.AdditionalColumns = new Collection<SqlColumn>();

            // Add & configure columns as they exist in the table, going from left to right

            //      Add & configure 'type'
            columnMappings.AdditionalColumns.Add(new SqlColumn { ColumnName = "type", AllowNull = false, DataType = SqlDbType.VarChar, DataLength = 50, PropertyName = MagicValues.LogPropertyNames.Type });

            //      Add & configure 'logDate'
            columnMappings.TimeStamp.ConvertToUtc = true;
            columnMappings.TimeStamp.ColumnName = "logDate";
            columnMappings.TimeStamp.DataType = SqlDbType.DateTime;

            //      Add & configure 'eventid'
            columnMappings.AdditionalColumns.Add(new SqlColumn { ColumnName = "eventid", AllowNull = false, DataType = SqlDbType.Int, PropertyName = MagicValues.LogPropertyNames.EventId });

            //      Add & configure 'title'
            columnMappings.AdditionalColumns.Add(new SqlColumn { ColumnName = "title", AllowNull = true, DataType = SqlDbType.VarChar, DataLength = 100, PropertyName = MagicValues.LogPropertyNames.Title });

            //      Add & configure 'category'
            columnMappings.AdditionalColumns.Add(new SqlColumn { ColumnName = "category", AllowNull = true, DataType = SqlDbType.VarChar, DataLength = 50, PropertyName = MagicValues.LogPropertyNames.Category });

            //      Add & configure 'message'
            columnMappings.AdditionalColumns.Add(new SqlColumn { ColumnName = "message", AllowNull = true, DataType = SqlDbType.VarChar, DataLength = 1000, PropertyName = MagicValues.LogPropertyNames.Message });

            //      Add & configure 'exceptionText'
            //columnMappings.Properties.ColumnName = "exceptionText";
            //columnMappings.Properties.DataType = SqlDbType.VarChar;

            columnMappings.AdditionalColumns.Add(new SqlColumn { ColumnName = "exceptionText", AllowNull = true, DataType = SqlDbType.VarChar, PropertyName = MagicValues.LogPropertyNames.ExceptionText });

            //      Add & configure 'computer'
            columnMappings.AdditionalColumns.Add(new SqlColumn { ColumnName = "computer", AllowNull = true, DataType = SqlDbType.VarChar, DataLength = 50, PropertyName = MagicValues.LogPropertyNames.Computer });

            //      Add & configure 'registeredAppId'
            columnMappings.AdditionalColumns.Add(new SqlColumn { ColumnName = "registeredAppId", AllowNull = true, DataType = SqlDbType.VarChar, DataLength = 50, PropertyName = MagicValues.LogPropertyNames.RegisteredAppId });

            return new LoggerConfiguration()
                .ReadFrom.Configuration(originalConfig)
                .Enrich.FromLogContext()
                .Enrich.WithProperty(MagicValues.LogPropertyNames.RegisteredAppId, originalConfig["DatabaseLoggerIdentifier"])
                .Enrich.With<LogTableEnricher>()
                .WriteTo.Debug()
                .WriteTo.MSSqlServer(runtimeConfig[MagicValues.LoggingConnectionStringName], sinkOptions: sinkOptions, columnOptions: columnMappings, restrictedToMinimumLevel: LogEventLevel.Verbose)
                .CreateLogger();
        }
    }
}
