using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ExactReproduction
{
    public class SecretsReader
    {
        public static Dictionary<string, string> GetConnectionStrings(Dictionary<string, string> runtimeValues, ILogger logger)
        {
            try
            {
                runtimeValues[MagicValues.LoggingConnectionStringName] = "Data Source=.;initial catalog=Adhoc;Trusted_Connection=True";
                runtimeValues[MagicValues.ReportingConnectionStringName] = "Data Source=.;initial catalog=Adhoc;Trusted_Connection=True";
                runtimeValues[MagicValues.AppConnectionStringName] = "Data Source=.;initial catalog=Adhoc;Trusted_Connection=True";
                return runtimeValues;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred trying to read the secrets store: {@ex}", ex);
                throw ex;
            }
        }
    }
}