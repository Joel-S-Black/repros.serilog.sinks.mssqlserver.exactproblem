using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExactReproduction
{
    public class LogTableEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            SetType(logEvent, propertyFactory);
            SetEventId(logEvent, propertyFactory);
            SetTitle(logEvent, propertyFactory);
            SetCategory(logEvent, propertyFactory);
            SetMessage(logEvent, propertyFactory);
            SetExceptionText(logEvent, propertyFactory);
        }

        private void SetExceptionText(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var propertiesAsString = new StringBuilder();
            foreach(var item in logEvent.Properties)
            {
                propertiesAsString.Append($"{item.Key}:{item.Value.ToString()} | ");
            }

            var length = propertiesAsString.Length;
            propertiesAsString.Remove(length - 2, 2);

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(MagicValues.LogPropertyNames.ExceptionText, propertiesAsString.ToString()));
        }

        private void SetMessage(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var msg = logEvent.RenderMessage();

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(MagicValues.LogPropertyNames.Message, msg));
        }

        private void SetCategory(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(MagicValues.LogPropertyNames.Category, "Standard"));
        }

        private void SetTitle(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var template = logEvent.MessageTemplate.ToString();

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(MagicValues.LogPropertyNames.Title, template));
        }

        private void SetEventId(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(MagicValues.LogPropertyNames.EventId,1));
        }

        private void SetType(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var logLevel = Enum.GetName(typeof(LogEventLevel), logEvent.Level);

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(MagicValues.LogPropertyNames.Type, logLevel));
        }
    }
}
