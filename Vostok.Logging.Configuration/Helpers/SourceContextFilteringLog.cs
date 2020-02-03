using System;
using System.Linq;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Values;

// ReSharper disable AssignNullToNotNullAttribute

namespace Vostok.Logging.Configuration.Helpers
{
    internal class SourceContextFilteringLog : ILog
    {
        private readonly ILog log;
        private readonly LogConfigurationRule rule;

        public SourceContextFilteringLog(ILog log, LogConfigurationRule rule)
        {
            this.log = log;
            this.rule = rule;

            if (rule.Source == null)
                throw new ArgumentNullException(nameof(rule.Source));
        }

        public void Log(LogEvent @event)
        {
            if (@event == null)
                return;

            if (HasMatchingSourceContext(@event))
            {
                if (!rule.Enabled)
                    return;

                if (rule.MinimumLevel.HasValue && @event.Level < rule.MinimumLevel.Value)
                    return;

                if (rule.Properties?.Count > 0)
                    foreach (var pair in rule.Properties)
                        @event = @event.WithPropertyIfAbsent(pair.Key, pair.Value);
            }

            log.Log(@event);
        }

        public bool IsEnabledFor(LogLevel level) =>
            log.IsEnabledFor(level);

        public ILog ForContext(string context) =>
            log.ForContext(context);

        private bool HasMatchingSourceContext(LogEvent @event)
        {
            if (@event?.Properties == null)
                return false;

            if (!@event.Properties.TryGetValue(WellKnownProperties.SourceContext, out var sourceContextValue))
                return false;

            if (!(sourceContextValue is SourceContextValue sourceContext))
                return false;

            return sourceContext.Any(value => value.StartsWith(rule.Source, StringComparison.OrdinalIgnoreCase));
        }
    }
}
