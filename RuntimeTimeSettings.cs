using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ExactReproduction
{
    public class RuntimeTimeSettings
    {
        public static Dictionary<string, string> UpdateConfigurationFromDatabase(Dictionary<string, string> environmentSpecificValues, ILogger logger)
        {
            // In a real app, look up database values and load them .net core style into the dictionary, using MagicValues
            return environmentSpecificValues;
        }
    }
}