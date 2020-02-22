using System;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Values;

// ReSharper disable AssignNullToNotNullAttribute

namespace Vostok.Logging.Configuration.Helpers
{
    internal static class RuleExtensions
    {
        public static bool Matches([NotNull] this LogConfigurationRule rule, [NotNull] LogEvent @event)
            => HasMatchingSourceContext(rule, @event) && HasMatchingOperationContext(rule, @event);

        public static bool Allows([NotNull] this LogConfigurationRule rule, [NotNull] LogEvent @event)
            => rule.Enabled && (rule.MinimumLevel == null || @event.Level >= rule.MinimumLevel.Value);

        private static bool HasMatchingSourceContext(LogConfigurationRule rule, LogEvent @event)
        {
            if (!rule.HasSourceScope)
                return true;

            if (@event.Properties == null)
                return false;

            if (!@event.Properties.TryGetValue(WellKnownProperties.SourceContext, out var sourceContextValue))
                return false;

            if (!(sourceContextValue is SourceContextValue sourceContext))
                return false;

            return sourceContext.Any(value => value.StartsWith(rule.Source, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasMatchingOperationContext(LogConfigurationRule rule, LogEvent @event)
        {
            if (!rule.HasOperationScope)
                return true;

            if (@event.Properties == null)
                return false;

            if (!@event.Properties.TryGetValue(WellKnownProperties.OperationContext, out var operationContextValue))
                return false;

            if (!(operationContextValue is OperationContextValue operationContext))
                return false;

            return operationContext.Any(value => value.StartsWith(rule.Operation, StringComparison.OrdinalIgnoreCase));
        }
    }
}
