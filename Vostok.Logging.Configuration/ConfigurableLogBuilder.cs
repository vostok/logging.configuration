using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Observable;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Configuration.Helpers;

namespace Vostok.Logging.Configuration
{
    [PublicAPI]
    public class ConfigurableLogBuilder
    {
        private readonly Dictionary<string, ILog> baseLogs;
        private volatile IObservable<LogConfigurationRule[]> rulesSource;

        public ConfigurableLogBuilder()
        {
            baseLogs = new Dictionary<string, ILog>(StringComparer.OrdinalIgnoreCase);
            rulesSource = new CachingObservable<LogConfigurationRule[]>(Array.Empty<LogConfigurationRule>());
        }

        [NotNull]
        public ConfigurableLog Build()
            => new ConfigurableLog(baseLogs, rulesSource);

        [NotNull]
        public ConfigurableLogBuilder AddLog([NotNull] string name, [NotNull] ILog log)
        {
            baseLogs[name ?? throw new ArgumentNullException(nameof(name))] = log ?? throw new ArgumentNullException(nameof(log));
            return this;
        }

        [NotNull]
        public ConfigurableLogBuilder SetRules([NotNull] LogConfigurationRule[] rules)
        {
            rulesSource = new CachingObservable<LogConfigurationRule[]>(rules);
            return this;
        }

        [NotNull]
        public ConfigurableLogBuilder SetRules([NotNull] Func<LogConfigurationRule[]> rulesProvider, TimeSpan updatePeriod)
        {
            rulesSource = new PeriodicObservable<LogConfigurationRule[]>(rulesProvider, updatePeriod);
            return this;
        }

        [NotNull]
        public ConfigurableLogBuilder SetRules([NotNull] IObservable<LogConfigurationRule[]> rulesProvider)
        {
            rulesSource = rulesProvider ?? throw new ArgumentNullException(nameof(rulesProvider));
            return this;
        }
    }
}
