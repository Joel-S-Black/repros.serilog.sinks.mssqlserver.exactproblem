namespace ExactReproduction
{
    public class MagicValues
    {
        private static LogPropertyNames _logPropertyNames;
        public static string LoggingConnectionStringName { get; } = "ConnectionStrings:DatabaseLogging";
        public static string ReportingConnectionStringName { get; } = "ConnectionStrings:Reporting";
        public static string AppConnectionStringName { get; } = "ConnectionStrings:AppServer";

        public static LogPropertyNames LogPropertyNames
        {
            get 
            { 
                if(_logPropertyNames == null)
                {
                    _logPropertyNames = new LogPropertyNames();
                }

                return _logPropertyNames; 
            }
        }
    }
}
