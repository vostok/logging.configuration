using System;
using System.Linq;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Values;

// ReSharper disable AssignNullToNotNullAttribute

namespace Vostok.Logging.Configuration.Helpers
{
    internal class ContextFilteringLog : ILog
    {
        private readonly ILog log;
        private readonly LogConfigurationRule rule;

        public ContextFilteringLog(ILog log, LogConfigurationRule rule)
        {
            this.log = log;
            this.rule = rule;
        }

        public void Log(LogEvent @event)
        {
            if (@event == null)
                return;

            if (HasMatchingSourceContext(@event) && HasMatchingOperationContext(@event))
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
            if (string.IsNullOrEmpty(rule.Source))
                return true;

            if (@event?.Properties == null)
                return false;

            if (!@event.Properties.TryGetValue(WellKnownProperties.SourceContext, out var sourceContextValue))
                return false;

            if (!(sourceContextValue is SourceContextValue sourceContext))
                return false;

            return sourceContext.Any(value => value.StartsWith(rule.Source, StringComparison.OrdinalIgnoreCase));
        }

        private bool HasMatchingOperationContext(LogEvent @event)
        {
            if (string.IsNullOrEmpty(rule.Operation))
                return true;

            if (@event?.Properties == null)
                return false;

            if (!@event.Properties.TryGetValue(WellKnownProperties.OperationContext, out var operationContextValue))
                return false;

            if (!(operationContextValue is OperationContextValue operationContext))
                return false;

            return operationContext.Any(value => value.StartsWith(rule.Operation, StringComparison.OrdinalIgnoreCase));
        }
    }
}
