﻿using System.Collections.Generic;
using System.Linq;
using Vostok.Logging.Abstractions;

// ReSharper disable PossibleNullReferenceException

namespace Vostok.Logging.Configuration.Helpers
{
    internal class RuleBasedLog : ILog
    {
        private readonly ILog log;
        private readonly LogConfigurationRule[] rules;
        private readonly LogConfigurationRule[] enrichingRules;
        private readonly bool enabled;
        private readonly LogLevel minLevel;

        public RuleBasedLog(ILog log, IEnumerable<LogConfigurationRule> rules)
        {
            this.log = log;
            this.rules = RuleOrderingHelper.Order(rules).ToArray();

            enrichingRules = this.rules.Where(rule => rule.HasProperties).ToArray();
            
            (enabled, minLevel) = Analyze(rules);
        }

        public void Log(LogEvent @event)
        {
            if (@event == null || !IsEnabledFor(@event.Level) || !CanLog(@event))
                return;

            if (enrichingRules.Length > 0)
                Enrich(ref @event);

            log.Log(@event);
        }

        public bool IsEnabledFor(LogLevel level) =>
            enabled && level >= minLevel && log.IsEnabledFor(level);

        public ILog ForContext(string context) =>
            log.ForContext(context);

        private bool CanLog(LogEvent @event)
        {
            foreach (var rule in rules)
            {
                if (rule.Matches(@event))
                    return rule.Allows(@event);
            }

            return true;
        }

        private void Enrich(ref LogEvent @event)
        {
            foreach (var rule in enrichingRules)
            {
                if (rule.Matches(@event))
                {
                    foreach (var pair in rule.Properties)
                        @event = @event.WithPropertyIfAbsent(pair.Key, pair.Value);
                }
            }
        }

        private static (bool enabled, LogLevel minLevel) Analyze(IEnumerable<LogConfigurationRule> rules)
        {
            var firstRuleWithoutScope = rules.FirstOrDefault(rule => !rule.HasSourceScope && !rule.HasOperationScope);
            if (firstRuleWithoutScope == null)
                return (true, LogLevel.Debug);

            var enabled = firstRuleWithoutScope.Enabled;
            var minLevel = firstRuleWithoutScope.MinimumLevel ?? LogLevel.Debug;

            foreach (var rule in rules.Where(r => r.HasSourceScope || r.HasOperationScope))
            {
                var isMorePermissive = !enabled && rule.Enabled || rule.MinimumLevel == null || rule.MinimumLevel < minLevel;
                if (isMorePermissive)
                    return (true, LogLevel.Debug);
            }

            return (enabled, minLevel);
        }
    }
}
