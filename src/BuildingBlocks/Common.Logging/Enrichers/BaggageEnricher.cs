using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Common.Logging.Enrichers
{
    internal class BaggageEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (Activity.Current == null)
                return;

            foreach (var (key, value) in Activity.Current.Baggage)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(key, value));
            }
        }
    }
}
